using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ChunkMapLayer : RGBMapLayer
{
	public static Dictionary<EnumBlockMaterial, string> defaultMapColorCodes = new Dictionary<EnumBlockMaterial, string>
	{
		{
			EnumBlockMaterial.Soil,
			"land"
		},
		{
			EnumBlockMaterial.Sand,
			"desert"
		},
		{
			EnumBlockMaterial.Ore,
			"land"
		},
		{
			EnumBlockMaterial.Gravel,
			"desert"
		},
		{
			EnumBlockMaterial.Stone,
			"land"
		},
		{
			EnumBlockMaterial.Leaves,
			"forest"
		},
		{
			EnumBlockMaterial.Plant,
			"plant"
		},
		{
			EnumBlockMaterial.Wood,
			"forest"
		},
		{
			EnumBlockMaterial.Snow,
			"glacier"
		},
		{
			EnumBlockMaterial.Liquid,
			"lake"
		},
		{
			EnumBlockMaterial.Ice,
			"glacier"
		},
		{
			EnumBlockMaterial.Lava,
			"lava"
		}
	};

	public static OrderedDictionary<string, string> hexColorsByCode = new OrderedDictionary<string, string>
	{
		{ "ink", "#483018" },
		{ "settlement", "#856844" },
		{ "wateredge", "#483018" },
		{ "land", "#AC8858" },
		{ "desert", "#C4A468" },
		{ "forest", "#98844C" },
		{ "road", "#805030" },
		{ "plant", "#808650" },
		{ "lake", "#CCC890" },
		{ "ocean", "#CCC890" },
		{ "glacier", "#E0E0C0" },
		{ "devastation", "#755c3c" }
	};

	public OrderedDictionary<string, int> colorsByCode = new OrderedDictionary<string, int>();

	private int[] colors;

	public byte[] block2Color;

	private const int chunksize = 32;

	private IWorldChunk[] chunksTmp;

	private object chunksToGenLock = new object();

	private UniqueQueue<FastVec2i> chunksToGen = new UniqueQueue<FastVec2i>();

	private ConcurrentDictionary<FastVec2i, MultiChunkMapComponent> loadedMapData = new ConcurrentDictionary<FastVec2i, MultiChunkMapComponent>();

	private HashSet<FastVec2i> curVisibleChunks = new HashSet<FastVec2i>();

	private ConcurrentQueue<ReadyMapPiece> readyMapPieces = new ConcurrentQueue<ReadyMapPiece>();

	private MapDB mapdb;

	private ICoreClientAPI capi;

	private bool colorAccurate;

	private float mtThread1secAccum;

	private float genAccum;

	private float diskSaveAccum;

	private Dictionary<FastVec2i, MapPieceDB> toSaveList = new Dictionary<FastVec2i, MapPieceDB>();

	[ThreadStatic]
	private static byte[] shadowMapReusable;

	[ThreadStatic]
	private static byte[] shadowMapBlurReusable;

	public override MapLegendItem[] LegendItems
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override EnumMinMagFilter MinFilter => EnumMinMagFilter.Linear;

	public override EnumMinMagFilter MagFilter => EnumMinMagFilter.Nearest;

	public override string Title => "Terrain";

	public override EnumMapAppSide DataSide => EnumMapAppSide.Client;

	public override string LayerGroupCode => "terrain";

	public string getMapDbFilePath()
	{
		string text = Path.Combine(GamePaths.DataPath, "Maps");
		GamePaths.EnsurePathExists(text);
		return Path.Combine(text, api.World.SavegameIdentifier + ".db");
	}

	public ChunkMapLayer(ICoreAPI api, IWorldMapManager mapSink)
		: base(api, mapSink)
	{
		foreach (KeyValuePair<string, string> item in hexColorsByCode)
		{
			colorsByCode[item.Key] = ColorUtil.ReverseColorBytes(ColorUtil.Hex2Int(item.Value));
		}
		api.Event.ChunkDirty += Event_OnChunkDirty;
		capi = api as ICoreClientAPI;
		if (api.Side == EnumAppSide.Server)
		{
			(api as ICoreServerAPI).Event.DidPlaceBlock += Event_DidPlaceBlock;
		}
		if (api.Side == EnumAppSide.Client)
		{
			api.World.Logger.Notification("Loading world map cache db...");
			mapdb = new MapDB(api.World.Logger);
			string errorMessage = null;
			string mapDbFilePath = getMapDbFilePath();
			mapdb.OpenOrCreate(mapDbFilePath, ref errorMessage, requireWriteAccess: true, corruptionProtection: true, doIntegrityCheck: false);
			if (errorMessage != null)
			{
				throw new Exception($"Cannot open {mapDbFilePath}, possibly corrupted. Please fix manually or delete this file to continue playing");
			}
			api.ChatCommands.GetOrCreate("map").BeginSubCommand("purgedb").WithDescription("purge the map db")
				.HandleWith(delegate
				{
					mapdb.Purge();
					return TextCommandResult.Success("Ok, db purged");
				})
				.EndSubCommand()
				.BeginSubCommand("redraw")
				.WithDescription("Redraw the map")
				.HandleWith(OnMapCmdRedraw)
				.EndSubCommand();
		}
	}

	private TextCommandResult OnMapCmdRedraw(TextCommandCallingArgs args)
	{
		foreach (MultiChunkMapComponent value in loadedMapData.Values)
		{
			value.ActuallyDispose();
		}
		loadedMapData.Clear();
		lock (chunksToGenLock)
		{
			foreach (FastVec2i curVisibleChunk in curVisibleChunks)
			{
				chunksToGen.Enqueue(curVisibleChunk.Copy());
			}
		}
		return TextCommandResult.Success("Redrawing map...");
	}

	private void Event_DidPlaceBlock(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack)
	{
		IMapChunk mapChunkAtBlockPos = api.World.BlockAccessor.GetMapChunkAtBlockPos(blockSel.Position);
		if (mapChunkAtBlockPos != null)
		{
			int num = blockSel.Position.X % 32;
			int num2 = blockSel.Position.Z % 32;
			int num3 = mapChunkAtBlockPos.RainHeightMap[num2 * 32 + num];
			int num4 = num3 % 32;
			IWorldChunk chunkAtBlockPos = api.World.BlockAccessor.GetChunkAtBlockPos(blockSel.Position.X, num3, blockSel.Position.Z);
			if (chunkAtBlockPos != null && chunkAtBlockPos.UnpackAndReadBlock((num4 * 32 + num2) * 32 + num, 3) == 0)
			{
				int num5 = blockSel.Position.X / 32;
				int num6 = blockSel.Position.Z / 32;
				api.World.Logger.Notification("Huh. Found air block in rain map at chunk pos {0}/{1}. That seems invalid, will regenerate rain map", num5, num6);
				rebuildRainmap(num5, num6);
			}
		}
	}

	private void Event_OnChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
	{
		lock (chunksToGenLock)
		{
			if (mapSink.IsOpened)
			{
				FastVec2i key = new FastVec2i(chunkCoord.X / 3, chunkCoord.Z / 3);
				FastVec2i item = new FastVec2i(chunkCoord.X, chunkCoord.Z);
				if (loadedMapData.ContainsKey(key) || curVisibleChunks.Contains(item))
				{
					chunksToGen.Enqueue(new FastVec2i(chunkCoord.X, chunkCoord.Z));
					chunksToGen.Enqueue(new FastVec2i(chunkCoord.X, chunkCoord.Z - 1));
					chunksToGen.Enqueue(new FastVec2i(chunkCoord.X - 1, chunkCoord.Z));
					chunksToGen.Enqueue(new FastVec2i(chunkCoord.X, chunkCoord.Z + 1));
					chunksToGen.Enqueue(new FastVec2i(chunkCoord.X + 1, chunkCoord.Z + 1));
				}
			}
		}
	}

	public override void OnLoaded()
	{
		if (api.Side == EnumAppSide.Server)
		{
			return;
		}
		chunksTmp = new IWorldChunk[api.World.BlockAccessor.MapSizeY / 32];
		colors = new int[colorsByCode.Count];
		for (int i = 0; i < colors.Length; i++)
		{
			colors[i] = colorsByCode.GetValueAtIndex(i);
		}
		IList<Block> blocks = api.World.Blocks;
		block2Color = new byte[blocks.Count];
		for (int j = 0; j < block2Color.Length; j++)
		{
			Block block = blocks[j];
			string value = "land";
			if (block?.Attributes != null)
			{
				value = block.Attributes["mapColorCode"].AsString();
				if (value == null && !defaultMapColorCodes.TryGetValue(block.BlockMaterial, out value))
				{
					value = "land";
				}
			}
			block2Color[j] = (byte)colorsByCode.IndexOfKey(value);
			if (colorsByCode.IndexOfKey(value) < 0)
			{
				throw new Exception("No color exists for color code " + value);
			}
		}
	}

	public override void OnMapOpenedClient()
	{
		colorAccurate = api.World.Config.GetAsBool("colorAccurateWorldmap") || capi.World.Player.Privileges.IndexOf("colorAccurateWorldmap") != -1;
	}

	public override void OnMapClosedClient()
	{
		lock (chunksToGenLock)
		{
			chunksToGen.Clear();
		}
		curVisibleChunks.Clear();
	}

	public override void Dispose()
	{
		if (loadedMapData != null)
		{
			foreach (MultiChunkMapComponent value in loadedMapData.Values)
			{
				value?.ActuallyDispose();
			}
		}
		MultiChunkMapComponent.DisposeStatic();
		base.Dispose();
	}

	public override void OnShutDown()
	{
		MultiChunkMapComponent.tmpTexture?.Dispose();
		mapdb?.Dispose();
	}

	public override void OnOffThreadTick(float dt)
	{
		genAccum += dt;
		if ((double)genAccum < 0.1)
		{
			return;
		}
		genAccum = 0f;
		int num = chunksToGen.Count;
		while (num > 0 && !mapSink.IsShuttingDown)
		{
			num--;
			FastVec2i fastVec2i;
			lock (chunksToGenLock)
			{
				if (chunksToGen.Count == 0)
				{
					break;
				}
				fastVec2i = chunksToGen.Dequeue();
				goto IL_0091;
			}
			IL_0091:
			if (!api.World.BlockAccessor.IsValidPos(fastVec2i.X * 32, 1, fastVec2i.Y * 32))
			{
				continue;
			}
			IMapChunk mapChunk = api.World.BlockAccessor.GetMapChunk(fastVec2i.X, fastVec2i.Y);
			if (mapChunk == null)
			{
				try
				{
					MapPieceDB mapPiece = mapdb.GetMapPiece(fastVec2i);
					if (mapPiece?.Pixels != null)
					{
						loadFromChunkPixels(fastVec2i, mapPiece.Pixels);
					}
				}
				catch (ProtoException)
				{
					api.Logger.Warning("Failed loading map db section {0}/{1}, a protobuf exception was thrown. Will ignore.", fastVec2i.X, fastVec2i.Y);
				}
				catch (OverflowException)
				{
					api.Logger.Warning("Failed loading map db section {0}/{1}, a overflow exception was thrown. Will ignore.", fastVec2i.X, fastVec2i.Y);
				}
				continue;
			}
			int[] array = GenerateChunkImage(fastVec2i, mapChunk, colorAccurate);
			if (array == null)
			{
				lock (chunksToGenLock)
				{
					chunksToGen.Enqueue(fastVec2i);
				}
			}
			else
			{
				toSaveList[fastVec2i.Copy()] = new MapPieceDB
				{
					Pixels = array
				};
				loadFromChunkPixels(fastVec2i, array);
			}
		}
		if (toSaveList.Count > 100 || diskSaveAccum > 4f)
		{
			diskSaveAccum = 0f;
			mapdb.SetMapPieces(toSaveList);
			toSaveList.Clear();
		}
	}

	public override void OnTick(float dt)
	{
		if (!readyMapPieces.IsEmpty)
		{
			int num = Math.Min(readyMapPieces.Count, 200);
			List<MultiChunkMapComponent> list = new List<MultiChunkMapComponent>();
			while (num-- > 0)
			{
				if (readyMapPieces.TryDequeue(out var result))
				{
					FastVec2i key = new FastVec2i(result.Cord.X / 3, result.Cord.Y / 3);
					FastVec2i baseChunkCord = new FastVec2i(key.X * 3, key.Y * 3);
					if (!loadedMapData.TryGetValue(key, out var value))
					{
						value = (loadedMapData[key] = new MultiChunkMapComponent(api as ICoreClientAPI, baseChunkCord));
					}
					value.setChunk(result.Cord.X - baseChunkCord.X, result.Cord.Y - baseChunkCord.Y, result.Pixels);
					list.Add(value);
				}
			}
			foreach (MultiChunkMapComponent item in list)
			{
				item.FinishSetChunks();
			}
		}
		mtThread1secAccum += dt;
		if (!(mtThread1secAccum > 1f))
		{
			return;
		}
		List<FastVec2i> list2 = new List<FastVec2i>();
		foreach (KeyValuePair<FastVec2i, MultiChunkMapComponent> loadedMapDatum in loadedMapData)
		{
			MultiChunkMapComponent value2 = loadedMapDatum.Value;
			if (!value2.AnyChunkSet || !value2.IsVisible(curVisibleChunks))
			{
				value2.TTL -= 1f;
				if (value2.TTL <= 0f)
				{
					FastVec2i key2 = loadedMapDatum.Key;
					list2.Add(key2);
					value2.ActuallyDispose();
				}
			}
			else
			{
				value2.TTL = MultiChunkMapComponent.MaxTTL;
			}
		}
		foreach (FastVec2i item2 in list2)
		{
			loadedMapData.TryRemove(item2, out var _);
		}
		mtThread1secAccum = 0f;
	}

	public override void Render(GuiElementMap mapElem, float dt)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (KeyValuePair<FastVec2i, MultiChunkMapComponent> loadedMapDatum in loadedMapData)
		{
			loadedMapDatum.Value.Render(mapElem, dt);
		}
	}

	public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (KeyValuePair<FastVec2i, MultiChunkMapComponent> loadedMapDatum in loadedMapData)
		{
			loadedMapDatum.Value.OnMouseMove(args, mapElem, hoverText);
		}
	}

	public override void OnMouseUpClient(MouseEvent args, GuiElementMap mapElem)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (KeyValuePair<FastVec2i, MultiChunkMapComponent> loadedMapDatum in loadedMapData)
		{
			loadedMapDatum.Value.OnMouseUpOnElement(args, mapElem);
		}
	}

	private void loadFromChunkPixels(FastVec2i cord, int[] pixels)
	{
		readyMapPieces.Enqueue(new ReadyMapPiece
		{
			Pixels = pixels,
			Cord = cord
		});
	}

	public override void OnViewChangedClient(List<FastVec2i> nowVisible, List<FastVec2i> nowHidden)
	{
		foreach (FastVec2i item in nowVisible)
		{
			curVisibleChunks.Add(item);
		}
		foreach (FastVec2i item2 in nowHidden)
		{
			curVisibleChunks.Remove(item2);
		}
		lock (chunksToGenLock)
		{
			foreach (FastVec2i item3 in nowVisible)
			{
				FastVec2i key = new FastVec2i(item3.X / 3, item3.Y / 3);
				int num = item3.X % 3;
				int num2 = item3.Y % 3;
				if (num >= 0 && num2 >= 0 && (!loadedMapData.TryGetValue(key, out var value) || !value.IsChunkSet(num, num2)))
				{
					chunksToGen.Enqueue(item3.Copy());
				}
			}
		}
		foreach (FastVec2i item4 in nowHidden)
		{
			if (item4.X >= 0 && item4.Y >= 0)
			{
				FastVec2i key2 = new FastVec2i(item4.X / 3, item4.Y / 3);
				if (loadedMapData.TryGetValue(key2, out var value2))
				{
					value2.unsetChunk(item4.X % 3, item4.Y % 3);
				}
			}
		}
	}

	private static bool isLake(Block block)
	{
		if (block.BlockMaterial != EnumBlockMaterial.Liquid)
		{
			if (block.BlockMaterial == EnumBlockMaterial.Ice)
			{
				return block.Code.Path != "glacierice";
			}
			return false;
		}
		return true;
	}

	public int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc, bool colorAccurate = false)
	{
		BlockPos blockPos = new BlockPos();
		Vec2i vec2i = new Vec2i();
		for (int i = 0; i < chunksTmp.Length; i++)
		{
			chunksTmp[i] = capi.World.BlockAccessor.GetChunk(chunkPos.X, i, chunkPos.Y);
			if (chunksTmp[i] == null || !(chunksTmp[i] as IClientChunk).LoadedFromServer)
			{
				return null;
			}
		}
		int[] array = new int[1024];
		IMapChunk mapChunk = capi.World.BlockAccessor.GetMapChunk(chunkPos.X - 1, chunkPos.Y - 1);
		IMapChunk mapChunk2 = capi.World.BlockAccessor.GetMapChunk(chunkPos.X - 1, chunkPos.Y);
		IMapChunk mapChunk3 = capi.World.BlockAccessor.GetMapChunk(chunkPos.X, chunkPos.Y - 1);
		if (shadowMapReusable == null)
		{
			shadowMapReusable = new byte[array.Length];
		}
		byte[] array2 = shadowMapReusable;
		for (int j = 0; j < array2.Length; j += 4)
		{
			array2[j] = 128;
			array2[j + 1] = 128;
			array2[j + 2] = 128;
			array2[j + 3] = 128;
		}
		for (int k = 0; k < array.Length; k++)
		{
			int num = mc.RainHeightMap[k];
			int num2 = num / 32;
			if (num2 >= chunksTmp.Length)
			{
				continue;
			}
			MapUtil.PosInt2d(k, 32L, vec2i);
			int x = vec2i.X;
			int y = vec2i.Y;
			float num3 = 1f;
			IMapChunk mapChunk4 = mc;
			IMapChunk mapChunk5 = mc;
			IMapChunk mapChunk6 = mc;
			int num4 = x - 1;
			int num5 = x;
			int num6 = y - 1;
			int num7 = y;
			if (num4 < 0 && num6 < 0)
			{
				mapChunk4 = mapChunk;
				mapChunk5 = mapChunk2;
				mapChunk6 = mapChunk3;
			}
			else
			{
				if (num4 < 0)
				{
					mapChunk4 = mapChunk2;
					mapChunk5 = mapChunk2;
				}
				if (num6 < 0)
				{
					mapChunk4 = mapChunk3;
					mapChunk6 = mapChunk3;
				}
			}
			num4 = GameMath.Mod(num4, 32);
			num6 = GameMath.Mod(num6, 32);
			int value = ((mapChunk4 != null) ? (num - mapChunk4.RainHeightMap[num6 * 32 + num4]) : 0);
			int value2 = ((mapChunk5 != null) ? (num - mapChunk5.RainHeightMap[num7 * 32 + num4]) : 0);
			int value3 = ((mapChunk6 != null) ? (num - mapChunk6.RainHeightMap[num6 * 32 + num5]) : 0);
			float num8 = Math.Sign(value) + Math.Sign(value2) + Math.Sign(value3);
			float num9 = Math.Max(Math.Max(Math.Abs(value), Math.Abs(value2)), Math.Abs(value3));
			int index = chunksTmp[num2].UnpackAndReadBlock(MapUtil.Index3d(x, num % 32, y, 32, 32), 3);
			Block block = api.World.Blocks[index];
			if (num8 > 0f)
			{
				num3 = 1.08f + Math.Min(0.5f, num9 / 10f) / 1.25f;
			}
			if (num8 < 0f)
			{
				num3 = 0.92f - Math.Min(0.5f, num9 / 10f) / 1.25f;
			}
			if (block.BlockMaterial == EnumBlockMaterial.Snow && !colorAccurate)
			{
				num--;
				num2 = num / 32;
				index = chunksTmp[num2].UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, num % 32, vec2i.Y, 32, 32), 3);
				block = api.World.Blocks[index];
			}
			blockPos.Set(32 * chunkPos.X + vec2i.X, num, 32 * chunkPos.Y + vec2i.Y);
			if (colorAccurate)
			{
				int color = block.GetColor(capi, blockPos);
				int randomColor = block.GetRandomColor(capi, blockPos, BlockFacing.UP, GameMath.MurmurHash3Mod(blockPos.X, blockPos.Y, blockPos.Z, 30));
				randomColor = ((randomColor & 0xFF) << 16) | (((randomColor >> 8) & 0xFF) << 8) | ((randomColor >> 16) & 0xFF);
				int num10 = ColorUtil.ColorOverlay(color, randomColor, 0.6f);
				array[k] = num10;
				array2[k] = (byte)((float)(int)array2[k] * num3);
			}
			else if (isLake(block))
			{
				IWorldChunk worldChunk = chunksTmp[num2];
				IWorldChunk worldChunk2 = worldChunk;
				IWorldChunk worldChunk3 = worldChunk;
				IWorldChunk worldChunk4 = worldChunk;
				int num11 = vec2i.X - 1;
				int num12 = vec2i.X + 1;
				int num13 = vec2i.Y - 1;
				int num14 = vec2i.Y + 1;
				if (num11 < 0)
				{
					worldChunk = capi.World.BlockAccessor.GetChunk(chunkPos.X - 1, num2, chunkPos.Y);
				}
				if (num12 >= 32)
				{
					worldChunk2 = capi.World.BlockAccessor.GetChunk(chunkPos.X + 1, num2, chunkPos.Y);
				}
				if (num13 < 0)
				{
					worldChunk3 = capi.World.BlockAccessor.GetChunk(chunkPos.X, num2, chunkPos.Y - 1);
				}
				if (num14 >= 32)
				{
					worldChunk4 = capi.World.BlockAccessor.GetChunk(chunkPos.X, num2, chunkPos.Y + 1);
				}
				if (worldChunk != null && worldChunk2 != null && worldChunk3 != null && worldChunk4 != null)
				{
					num11 = GameMath.Mod(num11, 32);
					num12 = GameMath.Mod(num12, 32);
					num13 = GameMath.Mod(num13, 32);
					num14 = GameMath.Mod(num14, 32);
					Block block2 = api.World.Blocks[worldChunk.UnpackAndReadBlock(MapUtil.Index3d(num11, num % 32, vec2i.Y, 32, 32), 3)];
					Block block3 = api.World.Blocks[worldChunk2.UnpackAndReadBlock(MapUtil.Index3d(num12, num % 32, vec2i.Y, 32, 32), 3)];
					Block block4 = api.World.Blocks[worldChunk3.UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, num % 32, num13, 32, 32), 3)];
					Block block5 = api.World.Blocks[worldChunk4.UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, num % 32, num14, 32, 32), 3)];
					if (isLake(block2) && isLake(block3) && isLake(block4) && isLake(block5))
					{
						array[k] = getColor(block, vec2i.X, num, vec2i.Y);
					}
					else
					{
						array[k] = colorsByCode["wateredge"];
					}
				}
				else
				{
					array[k] = getColor(block, vec2i.X, num, vec2i.Y);
				}
			}
			else
			{
				array2[k] = (byte)((float)(int)array2[k] * num3);
				array[k] = getColor(block, vec2i.X, num, vec2i.Y);
			}
		}
		if (shadowMapBlurReusable == null)
		{
			shadowMapBlurReusable = new byte[array2.Length];
		}
		byte[] array3 = shadowMapBlurReusable;
		for (int l = 0; l < array3.Length; l += 4)
		{
			array3[l] = array2[l];
			array3[l + 1] = array2[l + 1];
			array3[l + 2] = array2[l + 2];
			array3[l + 3] = array2[l + 3];
		}
		BlurTool.Blur(array2, 32, 32, 2);
		float num15 = 1f;
		for (int m = 0; m < array2.Length; m++)
		{
			float num16 = (float)(int)(((float)(int)array2[m] / 128f - 1f) * 5f) / 5f;
			num16 += ((float)(int)array3[m] / 128f - 1f) * 5f % 1f / 5f;
			array[m] = ColorUtil.ColorMultiply3Clamped(array[m], num16 * num15 + 1f) | -16777216;
		}
		for (int n = 0; n < chunksTmp.Length; n++)
		{
			chunksTmp[n] = null;
		}
		return array;
	}

	private int getColor(Block block, int x, int y1, int y2)
	{
		byte b = block2Color[block.Id];
		return colors[b];
	}

	private void rebuildRainmap(int cx, int cz)
	{
		ICoreServerAPI coreServerAPI = api as ICoreServerAPI;
		int num = coreServerAPI.WorldManager.MapSizeY / coreServerAPI.WorldManager.ChunkSize;
		IServerChunk[] array = new IServerChunk[num];
		int chunkSize = coreServerAPI.WorldManager.ChunkSize;
		IMapChunk mapChunk = null;
		for (int i = 0; i < num; i++)
		{
			array[i] = coreServerAPI.WorldManager.GetChunk(cx, i, cz);
			array[i]?.Unpack_ReadOnly();
			mapChunk = array[i]?.MapChunk;
		}
		if (mapChunk == null)
		{
			return;
		}
		for (int j = 0; j < chunkSize; j++)
		{
			for (int k = 0; k < chunkSize; k++)
			{
				for (int num2 = coreServerAPI.WorldManager.MapSizeY - 1; num2 >= 0; num2--)
				{
					IServerChunk serverChunk = array[num2 / chunkSize];
					if (serverChunk != null)
					{
						int index3d = (num2 % chunkSize * chunkSize + k) * chunkSize + j;
						if (!coreServerAPI.World.Blocks[serverChunk.Data.GetBlockId(index3d, 3)].RainPermeable || num2 == 0)
						{
							mapChunk.RainHeightMap[k * chunkSize + j] = (ushort)num2;
							break;
						}
					}
				}
			}
		}
		coreServerAPI.WorldManager.ResendMapChunk(cx, cz, onlyIfInRange: true);
		mapChunk.MarkDirty();
	}
}
