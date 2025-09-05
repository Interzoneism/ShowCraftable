using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ProperVersion;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common.Collectible.Block;

namespace Vintagestory.API.Common;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class BlockSchematic
{
	[JsonProperty]
	public string GameVersion;

	[JsonProperty]
	public int SizeX;

	[JsonProperty]
	public int SizeY;

	[JsonProperty]
	public int SizeZ;

	[JsonProperty]
	public Dictionary<int, AssetLocation> BlockCodes = new Dictionary<int, AssetLocation>();

	[JsonProperty]
	public Dictionary<int, AssetLocation> ItemCodes = new Dictionary<int, AssetLocation>();

	[JsonProperty]
	public List<uint> Indices = new List<uint>();

	[JsonProperty]
	public List<int> BlockIds = new List<int>();

	[JsonProperty]
	public List<uint> DecorIndices = new List<uint>();

	[JsonProperty]
	public List<long> DecorIds = new List<long>();

	[JsonProperty]
	public Dictionary<uint, string> BlockEntities = new Dictionary<uint, string>();

	[JsonProperty]
	public List<string> Entities = new List<string>();

	[JsonProperty]
	public EnumReplaceMode ReplaceMode = EnumReplaceMode.ReplaceAllNoAir;

	[JsonProperty]
	public int EntranceRotation = -1;

	[JsonProperty]
	public BlockPos OriginalPos;

	public Dictionary<BlockPos, int> BlocksUnpacked = new Dictionary<BlockPos, int>();

	public Dictionary<BlockPos, int> FluidsLayerUnpacked = new Dictionary<BlockPos, int>();

	public Dictionary<BlockPos, string> BlockEntitiesUnpacked = new Dictionary<BlockPos, string>();

	public List<Entity> EntitiesUnpacked = new List<Entity>();

	public Dictionary<BlockPos, Dictionary<int, Block>> DecorsUnpacked = new Dictionary<BlockPos, Dictionary<int, Block>>();

	public FastVec3i PackedOffset;

	public List<BlockPosFacing> PathwayBlocksUnpacked;

	public static int FillerBlockId;

	public static int PathwayBlockId;

	public static int UndergroundBlockId;

	public static int AbovegroundBlockId;

	protected ushort empty;

	public bool OmitLiquids;

	public BlockFacing[] PathwaySides;

	public BlockPos[] PathwayStarts;

	public BlockPos[][] PathwayOffsets;

	public BlockPos[] UndergroundCheckPositions;

	public BlockPos[] AbovegroundCheckPositions;

	private static BlockPos Zero = new BlockPos(0, 0, 0);

	public const uint PosBitMask = 1023u;

	private BlockPos curPos = new BlockPos();

	public static Dictionary<string, Dictionary<string, string>> BlockRemaps { get; set; }

	public static Dictionary<string, Dictionary<string, string>> ItemRemaps { get; set; }

	public BlockSchematic()
	{
		GameVersion = "1.21.0";
	}

	public BlockSchematic(IServerWorldAccessor world, BlockPos start, BlockPos end, bool notLiquids)
		: this(world, world.BlockAccessor, start, end, notLiquids)
	{
	}

	public BlockSchematic(IServerWorldAccessor world, IBlockAccessor blockAccess, BlockPos start, BlockPos end, bool notLiquids)
	{
		BlockPos startPos = new BlockPos(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Min(start.Z, end.Z));
		OmitLiquids = notLiquids;
		AddArea(world, blockAccess, start, end);
		Pack(world, startPos);
	}

	public virtual void Init(IBlockAccessor blockAccessor)
	{
		Remap();
	}

	public bool TryGetVersionFromRemapKey(string remapKey, out SemVer remapVersion)
	{
		string[] array = remapKey.Split(":");
		if (remapKey.Length < 2)
		{
			remapVersion = null;
			return false;
		}
		if (array[1].StartsWithFast("v"))
		{
			array[1] = array[1].Substring(1, array[1].Length - 1);
		}
		SemVer.TryParse(array[1], out remapVersion);
		return true;
	}

	public void Remap()
	{
		SemVer.TryParse(GameVersion ?? "0.0.0", out var result);
		foreach (KeyValuePair<string, Dictionary<string, string>> blockRemap in BlockRemaps)
		{
			if (TryGetVersionFromRemapKey(blockRemap.Key, out var remapVersion) && remapVersion <= result)
			{
				continue;
			}
			foreach (KeyValuePair<int, AssetLocation> blockCode in BlockCodes)
			{
				if (blockRemap.Value.TryGetValue(blockCode.Value.Path, out var value))
				{
					BlockCodes[blockCode.Key] = new AssetLocation(value);
				}
			}
		}
		foreach (KeyValuePair<string, Dictionary<string, string>> itemRemap in ItemRemaps)
		{
			if (TryGetVersionFromRemapKey(itemRemap.Key, out var remapVersion2) && remapVersion2 <= result)
			{
				continue;
			}
			foreach (KeyValuePair<int, AssetLocation> itemCode in ItemCodes)
			{
				if (itemRemap.Value.TryGetValue(itemCode.Value.Path, out var value2))
				{
					ItemCodes[itemCode.Key] = new AssetLocation(value2);
				}
			}
		}
	}

	public void LoadMetaInformationAndValidate(IBlockAccessor blockAccessor, IWorldAccessor worldForResolve, string fileNameForLogging)
	{
		List<BlockPos> list = new List<BlockPos>();
		List<BlockPos> list2 = new List<BlockPos>();
		Queue<BlockPos> queue = new Queue<BlockPos>();
		HashSet<AssetLocation> hashSet = new HashSet<AssetLocation>();
		for (int i = 0; i < Indices.Count; i++)
		{
			uint num = Indices[i];
			int key = BlockIds[i];
			int x = (int)(num & 0x3FF);
			int y = (int)((num >> 20) & 0x3FF);
			int z = (int)((num >> 10) & 0x3FF);
			AssetLocation assetLocation = BlockCodes[key];
			Block block = blockAccessor.GetBlock(assetLocation);
			if (block == null)
			{
				hashSet.Add(assetLocation);
				continue;
			}
			BlockPos item = new BlockPos(x, y, z);
			if (block.Id == PathwayBlockId)
			{
				queue.Enqueue(item);
			}
			else if (block.Id == UndergroundBlockId)
			{
				list.Add(item);
			}
			else if (block.Id == AbovegroundBlockId)
			{
				list2.Add(item);
			}
		}
		for (int j = 0; j < DecorIds.Count; j++)
		{
			int key2 = (int)DecorIds[j] & 0xFFFFFF;
			AssetLocation assetLocation2 = BlockCodes[key2];
			if (blockAccessor.GetBlock(assetLocation2) == null)
			{
				hashSet.Add(assetLocation2);
			}
		}
		if (hashSet.Count > 0)
		{
			worldForResolve.Logger.Warning("Block schematic file {0} uses blocks that could no longer be found. These will turn into air blocks! (affected: {1})", fileNameForLogging, string.Join(",", hashSet));
		}
		HashSet<AssetLocation> hashSet2 = new HashSet<AssetLocation>();
		foreach (KeyValuePair<int, AssetLocation> itemCode in ItemCodes)
		{
			if (worldForResolve.GetItem(itemCode.Value) == null)
			{
				hashSet2.Add(itemCode.Value);
			}
		}
		if (hashSet2.Count > 0)
		{
			worldForResolve.Logger.Warning("Block schematic file {0} uses items that could no longer be found. These will turn into unknown items! (affected: {1})", fileNameForLogging, string.Join(",", hashSet2));
		}
		UndergroundCheckPositions = list.ToArray();
		AbovegroundCheckPositions = list2.ToArray();
		List<List<BlockPos>> list3 = new List<List<BlockPos>>();
		if (queue.Count == 0)
		{
			PathwayStarts = Array.Empty<BlockPos>();
			PathwayOffsets = Array.Empty<BlockPos[]>();
			PathwaySides = Array.Empty<BlockFacing>();
			return;
		}
		while (queue.Count > 0)
		{
			List<BlockPos> list4 = new List<BlockPos> { queue.Dequeue() };
			list3.Add(list4);
			int count = queue.Count;
			while (count-- > 0)
			{
				BlockPos blockPos = queue.Dequeue();
				bool flag = false;
				for (int k = 0; k < list4.Count; k++)
				{
					BlockPos blockPos2 = list4[k];
					if (Math.Abs(blockPos.X - blockPos2.X) + Math.Abs(blockPos.Y - blockPos2.Y) + Math.Abs(blockPos.Z - blockPos2.Z) == 1)
					{
						flag = true;
						list4.Add(blockPos);
						break;
					}
				}
				if (!flag)
				{
					queue.Enqueue(blockPos);
				}
				else
				{
					count = queue.Count;
				}
			}
		}
		PathwayStarts = new BlockPos[list3.Count];
		PathwayOffsets = new BlockPos[list3.Count][];
		PathwaySides = new BlockFacing[list3.Count];
		for (int l = 0; l < PathwayStarts.Length; l++)
		{
			Vec3f vec3f = new Vec3f();
			List<BlockPos> list5 = list3[l];
			for (int m = 0; m < list5.Count; m++)
			{
				BlockPos blockPos3 = list5[m];
				vec3f.X += (float)blockPos3.X - (float)SizeX / 2f;
				vec3f.Y += (float)blockPos3.Y - (float)SizeY / 2f;
				vec3f.Z += (float)blockPos3.Z - (float)SizeZ / 2f;
			}
			vec3f.Normalize();
			PathwaySides[l] = BlockFacing.FromNormal(vec3f);
			BlockPos pos = (PathwayStarts[l] = list3[l][0].Copy());
			PathwayOffsets[l] = new BlockPos[list3[l].Count];
			for (int n = 0; n < list3[l].Count; n++)
			{
				PathwayOffsets[l][n] = list3[l][n].Sub(pos);
			}
		}
	}

	public virtual void AddArea(IWorldAccessor world, BlockPos start, BlockPos end)
	{
		AddArea(world, world.BlockAccessor, start, end);
	}

	public virtual void AddArea(IWorldAccessor world, IBlockAccessor blockAccess, BlockPos start, BlockPos end)
	{
		BlockPos blockPos = new BlockPos(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Min(start.Z, end.Z), start.dimension);
		BlockPos blockPos2 = new BlockPos(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y), Math.Max(start.Z, end.Z), start.dimension);
		OriginalPos = start;
		BlockPos blockPos3 = new BlockPos(start.dimension);
		using FastMemoryStream ms = new FastMemoryStream();
		for (int i = blockPos.X; i < blockPos2.X; i++)
		{
			for (int j = blockPos.Y; j < blockPos2.Y; j++)
			{
				for (int k = blockPos.Z; k < blockPos2.Z; k++)
				{
					blockPos3.Set(i, j, k);
					int num = blockAccess.GetBlock(blockPos3, 1).BlockId;
					int num2 = blockAccess.GetBlock(blockPos3, 2).BlockId;
					if (num2 == num)
					{
						num = 0;
					}
					if (OmitLiquids)
					{
						num2 = 0;
					}
					if (num == 0 && num2 == 0)
					{
						continue;
					}
					BlockPos key = new BlockPos(i, j, k);
					BlocksUnpacked[key] = num;
					FluidsLayerUnpacked[key] = num2;
					BlockEntity blockEntity = blockAccess.GetBlockEntity(blockPos3);
					if (blockEntity != null)
					{
						if (blockEntity.Api == null)
						{
							blockEntity.Initialize(world.Api);
						}
						BlockEntitiesUnpacked[key] = EncodeBlockEntityData(blockEntity, ms);
						blockEntity.OnStoreCollectibleMappings(BlockCodes, ItemCodes);
					}
					Dictionary<int, Block> subDecors = blockAccess.GetSubDecors(blockPos3);
					if (subDecors != null)
					{
						DecorsUnpacked[key] = subDecors;
					}
				}
			}
		}
		EntitiesUnpacked.AddRange(world.GetEntitiesInsideCuboid(start, end, (Entity e) => !(e is EntityPlayer)));
		foreach (Entity item in EntitiesUnpacked)
		{
			item.OnStoreCollectibleMappings(BlockCodes, ItemCodes);
		}
	}

	public virtual bool Pack(IWorldAccessor world, BlockPos startPos)
	{
		Indices.Clear();
		BlockIds.Clear();
		BlockEntities.Clear();
		Entities.Clear();
		DecorIndices.Clear();
		DecorIds.Clear();
		SizeX = 0;
		SizeY = 0;
		SizeZ = 0;
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		int num3 = int.MaxValue;
		foreach (KeyValuePair<BlockPos, int> item in BlocksUnpacked)
		{
			num = Math.Min(num, item.Key.X);
			num2 = Math.Min(num2, item.Key.Y);
			num3 = Math.Min(num3, item.Key.Z);
			int num4 = item.Key.X - startPos.X;
			int num5 = item.Key.Y - startPos.Y;
			int num6 = item.Key.Z - startPos.Z;
			if (num4 >= 1024 || num5 >= 1024 || num6 >= 1024)
			{
				world.Logger.Warning("Export format does not support areas larger than 1024 blocks in any direction. Will not pack.");
				PackedOffset = new FastVec3i(0, 0, 0);
				return false;
			}
		}
		foreach (KeyValuePair<BlockPos, int> item2 in BlocksUnpacked)
		{
			if (!FluidsLayerUnpacked.TryGetValue(item2.Key, out var value))
			{
				value = 0;
			}
			int value2 = item2.Value;
			if (value2 != 0 || value != 0)
			{
				if (value2 != 0)
				{
					BlockCodes[value2] = world.BlockAccessor.GetBlock(value2).Code;
				}
				if (value != 0)
				{
					BlockCodes[value] = world.BlockAccessor.GetBlock(value).Code;
				}
				int num7 = item2.Key.X - num;
				int num8 = item2.Key.Y - num2;
				int num9 = item2.Key.Z - num3;
				SizeX = Math.Max(num7, SizeX);
				SizeY = Math.Max(num8, SizeY);
				SizeZ = Math.Max(num9, SizeZ);
				Indices.Add((uint)((num8 << 20) | (num9 << 10) | num7));
				if (value == 0)
				{
					BlockIds.Add(value2);
					continue;
				}
				if (value2 == 0)
				{
					BlockIds.Add(value);
					continue;
				}
				BlockIds.Add(value2);
				Indices.Add((uint)((num8 << 20) | (num9 << 10) | num7));
				BlockIds.Add(value);
			}
		}
		BlockPos key;
		int key2;
		foreach (KeyValuePair<BlockPos, int> item3 in FluidsLayerUnpacked)
		{
			item3.Deconstruct(out key, out key2);
			BlockPos blockPos = key;
			int num10 = key2;
			if (!BlocksUnpacked.ContainsKey(blockPos))
			{
				if (num10 != 0)
				{
					BlockCodes[num10] = world.BlockAccessor.GetBlock(num10).Code;
				}
				int num11 = blockPos.X - num;
				int num12 = blockPos.Y - num2;
				int num13 = blockPos.Z - num3;
				SizeX = Math.Max(num11, SizeX);
				SizeY = Math.Max(num12, SizeY);
				SizeZ = Math.Max(num13, SizeZ);
				Indices.Add((uint)((num12 << 20) | (num13 << 10) | num11));
				BlockIds.Add(num10);
			}
		}
		foreach (KeyValuePair<BlockPos, Dictionary<int, Block>> item4 in DecorsUnpacked)
		{
			item4.Deconstruct(out key, out var value3);
			BlockPos blockPos2 = key;
			Dictionary<int, Block> dictionary = value3;
			int num14 = blockPos2.X - num;
			int num15 = blockPos2.Y - num2;
			int num16 = blockPos2.Z - num3;
			SizeX = Math.Max(num14, SizeX);
			SizeY = Math.Max(num15, SizeY);
			SizeZ = Math.Max(num16, SizeZ);
			foreach (KeyValuePair<int, Block> item5 in dictionary)
			{
				item5.Deconstruct(out key2, out var value4);
				int num17 = key2;
				Block block = value4;
				BlockCodes[block.BlockId] = block.Code;
				DecorIndices.Add((uint)((num15 << 20) | (num16 << 10) | num14));
				DecorIds.Add(((long)num17 << 24) + block.BlockId);
			}
		}
		SizeX++;
		SizeY++;
		SizeZ++;
		foreach (KeyValuePair<BlockPos, string> item6 in BlockEntitiesUnpacked)
		{
			int num18 = item6.Key.X - num;
			int num19 = item6.Key.Y - num2;
			int num20 = item6.Key.Z - num3;
			BlockEntities[(uint)((num19 << 20) | (num20 << 10) | num18)] = item6.Value;
		}
		BlockPos startPos2 = new BlockPos(num, num2, num3, startPos.dimension);
		using (FastMemoryStream fastMemoryStream = new FastMemoryStream())
		{
			foreach (Entity item7 in EntitiesUnpacked)
			{
				fastMemoryStream.Reset();
				BinaryWriter binaryWriter = new BinaryWriter(fastMemoryStream);
				binaryWriter.Write(world.ClassRegistry.GetEntityClassName(item7.GetType()));
				item7.WillExport(startPos2);
				item7.ToBytes(binaryWriter, forClient: false);
				item7.DidImportOrExport(startPos2);
				Entities.Add(Ascii85.Encode(fastMemoryStream.ToArray()));
			}
		}
		if (PathwayBlocksUnpacked != null)
		{
			foreach (BlockPosFacing item8 in PathwayBlocksUnpacked)
			{
				item8.Position.X -= num;
				item8.Position.Y -= num2;
				item8.Position.Z -= num3;
			}
		}
		PackedOffset = new FastVec3i(num - startPos.X, num2 - startPos.Y, num3 - startPos.Z);
		BlocksUnpacked.Clear();
		FluidsLayerUnpacked.Clear();
		DecorsUnpacked.Clear();
		BlockEntitiesUnpacked.Clear();
		EntitiesUnpacked.Clear();
		return true;
	}

	public virtual int Place(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos startPos, bool replaceMetaBlocks = true)
	{
		int result = Place(blockAccessor, worldForCollectibleResolve, startPos, ReplaceMode, replaceMetaBlocks);
		PlaceDecors(blockAccessor, startPos);
		return result;
	}

	public virtual int Place(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos startPos, EnumReplaceMode mode, bool replaceMetaBlocks = true)
	{
		BlockPos blockPos = new BlockPos(startPos.dimension);
		int num = 0;
		PlaceBlockDelegate placeBlockDelegate = null;
		switch (mode)
		{
		case EnumReplaceMode.ReplaceAll:
		{
			placeBlockDelegate = PlaceReplaceAll;
			for (int i = 0; i < SizeX; i++)
			{
				for (int j = 0; j < SizeY; j++)
				{
					for (int k = 0; k < SizeZ; k++)
					{
						blockPos.Set(i + startPos.X, j + startPos.Y, k + startPos.Z);
						if (blockAccessor.IsValidPos(blockPos))
						{
							blockAccessor.SetBlock(0, blockPos);
						}
					}
				}
			}
			break;
		}
		case EnumReplaceMode.Replaceable:
			placeBlockDelegate = PlaceReplaceable;
			break;
		case EnumReplaceMode.ReplaceAllNoAir:
			placeBlockDelegate = PlaceReplaceAllNoAir;
			break;
		case EnumReplaceMode.ReplaceOnlyAir:
			placeBlockDelegate = PlaceReplaceOnlyAir;
			break;
		}
		for (int l = 0; l < Indices.Count; l++)
		{
			uint num2 = Indices[l];
			int key = BlockIds[l];
			int num3 = (int)(num2 & 0x3FF);
			int num4 = (int)((num2 >> 20) & 0x3FF);
			int num5 = (int)((num2 >> 10) & 0x3FF);
			AssetLocation code = BlockCodes[key];
			Block block = blockAccessor.GetBlock(code);
			if (block == null || (replaceMetaBlocks && (block.Id == UndergroundBlockId || block.Id == AbovegroundBlockId)))
			{
				continue;
			}
			blockPos.Set(num3 + startPos.X, num4 + startPos.Y, num5 + startPos.Z);
			if (blockAccessor.IsValidPos(blockPos))
			{
				num += placeBlockDelegate(blockAccessor, blockPos, block, replaceMetaBlocks);
				if (block.LightHsv[2] > 0 && blockAccessor is IWorldGenBlockAccessor)
				{
					Block block2 = blockAccessor.GetBlock(blockPos);
					((IWorldGenBlockAccessor)blockAccessor).ScheduleBlockLightUpdate(blockPos, block2.BlockId, block.BlockId);
				}
			}
		}
		if (!(blockAccessor is IBlockAccessorRevertable))
		{
			PlaceEntitiesAndBlockEntities(blockAccessor, worldForCollectibleResolve, startPos, BlockCodes, ItemCodes, replaceBlockEntities: false, null, 0, null, replaceMetaBlocks);
		}
		return num;
	}

	public virtual void PlaceDecors(IBlockAccessor blockAccessor, BlockPos startPos)
	{
		curPos.dimension = startPos.dimension;
		for (int i = 0; i < DecorIndices.Count; i++)
		{
			uint num = DecorIndices[i];
			int posX = startPos.X + (int)(num & 0x3FF);
			int posY = startPos.Y + (int)((num >> 20) & 0x3FF);
			int posZ = startPos.Z + (int)((num >> 10) & 0x3FF);
			long storedBlockIdAndDecoPos = DecorIds[i];
			PlaceOneDecor(blockAccessor, posX, posY, posZ, storedBlockIdAndDecoPos);
		}
	}

	public virtual void PlaceDecors(IBlockAccessor blockAccessor, BlockPos startPos, Rectanglei rect)
	{
		int num = -1;
		foreach (uint decorIndex in DecorIndices)
		{
			num++;
			int num2 = startPos.X + (int)(decorIndex & 0x3FF);
			int num3 = startPos.Z + (int)((decorIndex >> 10) & 0x3FF);
			if (rect.Contains(num2, num3))
			{
				int posY = startPos.Y + (int)((decorIndex >> 20) & 0x3FF);
				long storedBlockIdAndDecoPos = DecorIds[num];
				PlaceOneDecor(blockAccessor, num2, posY, num3, storedBlockIdAndDecoPos);
			}
		}
	}

	private void PlaceOneDecor(IBlockAccessor blockAccessor, int posX, int posY, int posZ, long storedBlockIdAndDecoPos)
	{
		int decorIndex = (int)(storedBlockIdAndDecoPos >> 24);
		storedBlockIdAndDecoPos &= 0xFFFFFF;
		AssetLocation code = BlockCodes[(int)storedBlockIdAndDecoPos];
		Block block = blockAccessor.GetBlock(code);
		if (block != null)
		{
			curPos.Set(posX, posY, posZ);
			if (blockAccessor.IsValidPos(curPos))
			{
				blockAccessor.SetDecor(block, curPos, decorIndex);
			}
		}
	}

	public virtual void TransformWhilePacked(IWorldAccessor worldForResolve, EnumOrigin aroundOrigin, int angle, EnumAxis? flipAxis = null, bool isDungeon = false)
	{
		BlockPos startPos = new BlockPos(1024, 1024, 1024);
		BlocksUnpacked.Clear();
		FluidsLayerUnpacked.Clear();
		BlockEntitiesUnpacked.Clear();
		DecorsUnpacked.Clear();
		EntitiesUnpacked.Clear();
		angle = GameMath.Mod(angle, 360);
		if (angle == 0)
		{
			return;
		}
		if (EntranceRotation != -1)
		{
			EntranceRotation = GameMath.Mod(EntranceRotation + angle, 360);
		}
		for (int i = 0; i < Indices.Count; i++)
		{
			uint num = Indices[i];
			int key = BlockIds[i];
			int num2 = (int)(num & 0x3FF);
			int num3 = (int)((num >> 20) & 0x3FF);
			int num4 = (int)((num >> 10) & 0x3FF);
			AssetLocation assetLocation = BlockCodes[key];
			Block block = worldForResolve.GetBlock(assetLocation);
			if (block == null)
			{
				BlockEntities.Remove(num);
				continue;
			}
			if (flipAxis.HasValue)
			{
				if (flipAxis == EnumAxis.Y)
				{
					num3 = SizeY - num3;
					AssetLocation verticallyFlippedBlockCode = block.GetVerticallyFlippedBlockCode();
					block = worldForResolve.GetBlock(verticallyFlippedBlockCode);
				}
				if (flipAxis == EnumAxis.X)
				{
					num2 = SizeX - num2;
					AssetLocation horizontallyFlippedBlockCode = block.GetHorizontallyFlippedBlockCode(flipAxis.Value);
					block = worldForResolve.GetBlock(horizontallyFlippedBlockCode);
				}
				if (flipAxis == EnumAxis.Z)
				{
					num4 = SizeZ - num4;
					AssetLocation horizontallyFlippedBlockCode2 = block.GetHorizontallyFlippedBlockCode(flipAxis.Value);
					block = worldForResolve.GetBlock(horizontallyFlippedBlockCode2);
				}
			}
			if (angle != 0)
			{
				AssetLocation rotatedBlockCode = block.GetRotatedBlockCode(angle);
				Block block2 = worldForResolve.GetBlock(rotatedBlockCode);
				if (block2 != null)
				{
					block = block2;
				}
				else
				{
					worldForResolve.Logger.Warning("Schematic rotate: Unable to rotate block {0} - its GetRotatedBlockCode() method call returns an invalid block code: {1}! Will use unrotated variant.", assetLocation, rotatedBlockCode);
				}
			}
			BlockPos rotatedPos = GetRotatedPos(aroundOrigin, angle, num2, num3, num4);
			if (block.ForFluidsLayer)
			{
				FluidsLayerUnpacked[rotatedPos] = block.BlockId;
			}
			else
			{
				BlocksUnpacked[rotatedPos] = block.BlockId;
			}
		}
		for (int j = 0; j < DecorIndices.Count; j++)
		{
			uint num5 = DecorIndices[j];
			long num6 = DecorIds[j];
			int num7 = (int)(num6 >> 24);
			int num8 = num7 % 6;
			int key2 = (int)(num6 & 0xFFFFFF);
			BlockFacing blockFacing = BlockFacing.ALLFACES[num8];
			int num9 = (int)(num5 & 0x3FF);
			int num10 = (int)((num5 >> 20) & 0x3FF);
			int num11 = (int)((num5 >> 10) & 0x3FF);
			AssetLocation blockCode = BlockCodes[key2];
			Block block3 = worldForResolve.GetBlock(blockCode);
			if (block3 == null)
			{
				continue;
			}
			if (flipAxis.HasValue)
			{
				if (flipAxis == EnumAxis.Y)
				{
					num10 = SizeY - num10;
					AssetLocation verticallyFlippedBlockCode2 = block3.GetVerticallyFlippedBlockCode();
					block3 = worldForResolve.GetBlock(verticallyFlippedBlockCode2);
					if (blockFacing.IsVertical)
					{
						blockFacing = blockFacing.Opposite;
					}
				}
				if (flipAxis == EnumAxis.X)
				{
					num9 = SizeX - num9;
					AssetLocation horizontallyFlippedBlockCode3 = block3.GetHorizontallyFlippedBlockCode(flipAxis.Value);
					block3 = worldForResolve.GetBlock(horizontallyFlippedBlockCode3);
					if (blockFacing.Axis == EnumAxis.X)
					{
						blockFacing = blockFacing.Opposite;
					}
				}
				if (flipAxis == EnumAxis.Z)
				{
					num11 = SizeZ - num11;
					AssetLocation horizontallyFlippedBlockCode4 = block3.GetHorizontallyFlippedBlockCode(flipAxis.Value);
					block3 = worldForResolve.GetBlock(horizontallyFlippedBlockCode4);
					if (blockFacing.Axis == EnumAxis.Z)
					{
						blockFacing = blockFacing.Opposite;
					}
				}
			}
			if (angle != 0)
			{
				AssetLocation rotatedBlockCode2 = block3.GetRotatedBlockCode(angle);
				block3 = worldForResolve.GetBlock(rotatedBlockCode2);
			}
			BlockPos rotatedPos2 = GetRotatedPos(aroundOrigin, angle, num9, num10, num11);
			DecorsUnpacked.TryGetValue(rotatedPos2, out var value);
			if (value == null)
			{
				value = new Dictionary<int, Block>();
				DecorsUnpacked[rotatedPos2] = value;
			}
			value[num7 / 6 * 6 + blockFacing.GetHorizontalRotated(angle).Index] = block3;
		}
		using FastMemoryStream ms = new FastMemoryStream();
		foreach (KeyValuePair<uint, string> blockEntity2 in BlockEntities)
		{
			uint key3 = blockEntity2.Key;
			int num12 = (int)(key3 & 0x3FF);
			int num13 = (int)((key3 >> 20) & 0x3FF);
			int num14 = (int)((key3 >> 10) & 0x3FF);
			if (flipAxis == EnumAxis.Y)
			{
				num13 = SizeY - num13;
			}
			if (flipAxis == EnumAxis.X)
			{
				num12 = SizeX - num12;
			}
			if (flipAxis == EnumAxis.Z)
			{
				num14 = SizeZ - num14;
			}
			BlockPos rotatedPos3 = GetRotatedPos(aroundOrigin, angle, num12, num13, num14);
			string value2 = blockEntity2.Value;
			Block block4 = worldForResolve.GetBlock(BlocksUnpacked[rotatedPos3]);
			string entityClass = block4.EntityClass;
			if (entityClass != null)
			{
				BlockEntity blockEntity = worldForResolve.ClassRegistry.CreateBlockEntity(entityClass);
				ITreeAttribute treeAttribute = DecodeBlockEntityData(value2);
				if (blockEntity is IRotatable rotatable)
				{
					blockEntity.Pos = rotatedPos3;
					blockEntity.CreateBehaviors(block4, worldForResolve);
					rotatable.OnTransformed(worldForResolve, treeAttribute, angle, BlockCodes, ItemCodes, flipAxis);
				}
				treeAttribute.SetString("blockCode", block4.Code.ToShortString());
				value2 = StringEncodeTreeAttribute(treeAttribute, ms);
				BlockEntitiesUnpacked[rotatedPos3] = value2;
			}
		}
		foreach (string entity2 in Entities)
		{
			using MemoryStream input = new MemoryStream(Ascii85.Decode(entity2));
			BinaryReader binaryReader = new BinaryReader(input);
			string entityClass2 = binaryReader.ReadString();
			Entity entity = worldForResolve.ClassRegistry.CreateEntity(entityClass2);
			entity.FromBytes(binaryReader, isSync: false);
			EntityPos serverPos = entity.ServerPos;
			double num15 = 0.0;
			double num16 = 0.0;
			if (aroundOrigin != EnumOrigin.StartPos)
			{
				num15 = (double)SizeX / 2.0;
				num16 = (double)SizeZ / 2.0;
			}
			serverPos.X -= num15;
			serverPos.Z -= num16;
			double x = serverPos.X;
			double z = serverPos.Z;
			switch (angle)
			{
			case 90:
				serverPos.X = 0.0 - z;
				serverPos.Z = x;
				break;
			case 180:
				serverPos.X = 0.0 - x;
				serverPos.Z = 0.0 - z;
				break;
			case 270:
				serverPos.X = z;
				serverPos.Z = 0.0 - x;
				break;
			}
			if (aroundOrigin != EnumOrigin.StartPos)
			{
				serverPos.X += num15;
				serverPos.Z += num16;
			}
			serverPos.Yaw -= (float)angle * ((float)Math.PI / 180f);
			entity.Pos.SetPos(serverPos);
			entity.ServerPos.SetPos(serverPos);
			entity.PositionBeforeFalling.X = serverPos.X;
			entity.PositionBeforeFalling.Z = serverPos.Z;
			EntitiesUnpacked.Add(entity);
		}
		Pack(worldForResolve, startPos);
	}

	public BlockPos GetRotatedPos(EnumOrigin aroundOrigin, int angle, int dx, int dy, int dz)
	{
		if (aroundOrigin != EnumOrigin.StartPos)
		{
			dx -= SizeX / 2;
			dz -= SizeZ / 2;
		}
		BlockPos blockPos = new BlockPos(dx, dy, dz);
		switch (angle)
		{
		case 90:
			blockPos.Set(-dz, dy, dx);
			break;
		case 180:
			blockPos.Set(-dx, dy, -dz);
			break;
		case 270:
			blockPos.Set(dz, dy, -dx);
			break;
		}
		if (aroundOrigin != EnumOrigin.StartPos)
		{
			blockPos.X += SizeX / 2;
			blockPos.Z += SizeZ / 2;
		}
		return blockPos;
	}

	public void PlaceEntitiesAndBlockEntities(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos startPos, Dictionary<int, AssetLocation> blockCodes, Dictionary<int, AssetLocation> itemCodes, bool replaceBlockEntities = false, Dictionary<int, Dictionary<int, int>> replaceBlocks = null, int centerrockblockid = 0, Dictionary<BlockPos, Block> layerBlockForBlockEntities = null, bool resolveImports = true)
	{
		BlockPos blockPos = startPos.Copy();
		int schematicSeed = worldForCollectibleResolve.Rand.Next();
		foreach (KeyValuePair<uint, string> blockEntity2 in BlockEntities)
		{
			uint key = blockEntity2.Key;
			int num = (int)(key & 0x3FF);
			int num2 = (int)((key >> 20) & 0x3FF);
			int num3 = (int)((key >> 10) & 0x3FF);
			blockPos.Set(num + startPos.X, num2 + startPos.Y, num3 + startPos.Z);
			if (!blockAccessor.IsValidPos(blockPos))
			{
				continue;
			}
			BlockEntity blockEntity = blockAccessor.GetBlockEntity(blockPos);
			if ((blockEntity == null || replaceBlockEntities) && blockAccessor is IWorldGenBlockAccessor)
			{
				Block block = blockAccessor.GetBlock(blockPos, 1);
				if (block.EntityClass != null)
				{
					blockAccessor.SpawnBlockEntity(block.EntityClass, blockPos);
					blockEntity = blockAccessor.GetBlockEntity(blockPos);
				}
			}
			if (blockEntity == null)
			{
				continue;
			}
			if (!replaceBlockEntities)
			{
				Block block2 = blockAccessor.GetBlock(blockPos, 1);
				if (block2.EntityClass != worldForCollectibleResolve.ClassRegistry.GetBlockEntityClass(blockEntity.GetType()))
				{
					worldForCollectibleResolve.Logger.Warning("Could not import block entity data for schematic at {0}. There is already {1}, expected {2}. Probably overlapping ruins.", blockPos, blockEntity.GetType(), block2.EntityClass);
					continue;
				}
			}
			ITreeAttribute treeAttribute = DecodeBlockEntityData(blockEntity2.Value);
			string text = treeAttribute.GetString("blockCode");
			if (blockEntity.Block != null && text != null)
			{
				Block block3 = worldForCollectibleResolve.GetBlock(new AssetLocation(text));
				if (block3 != null && block3.GetType() != blockEntity.Block.GetType())
				{
					foreach (KeyValuePair<string, Dictionary<string, string>> blockRemap in BlockRemaps)
					{
						if (blockRemap.Value.TryGetValue(text, out var value))
						{
							block3 = worldForCollectibleResolve.GetBlock(new AssetLocation(value));
							break;
						}
					}
					if (block3 != null && block3.GetType() != blockEntity.Block.GetType())
					{
						worldForCollectibleResolve.Logger.Warning("Could not import block entity data for schematic at {0}. There is already {1}, expected {2}. Possibly overlapping ruins, or is this schematic from an old game version?", blockPos, blockEntity.Block, text);
						continue;
					}
				}
			}
			treeAttribute.SetInt("posx", blockPos.X);
			treeAttribute.SetInt("posy", blockPos.InternalY);
			treeAttribute.SetInt("posz", blockPos.Z);
			blockEntity.FromTreeAttributes(treeAttribute, worldForCollectibleResolve);
			blockEntity.OnLoadCollectibleMappings(worldForCollectibleResolve, blockCodes, itemCodes, schematicSeed, resolveImports);
			Block value2 = null;
			layerBlockForBlockEntities?.TryGetValue(blockPos, out value2);
			if (value2 != null && value2.Id == 0)
			{
				value2 = null;
			}
			blockEntity.OnPlacementBySchematic(worldForCollectibleResolve.Api as ICoreServerAPI, blockAccessor, blockPos, replaceBlocks, centerrockblockid, value2, resolveImports);
			if (!(blockAccessor is IWorldGenBlockAccessor))
			{
				blockEntity.MarkDirty();
			}
		}
		if (blockAccessor is IMiniDimension)
		{
			return;
		}
		foreach (string entity2 in Entities)
		{
			using MemoryStream input = new MemoryStream(Ascii85.Decode(entity2));
			BinaryReader binaryReader = new BinaryReader(input);
			string text2 = binaryReader.ReadString();
			try
			{
				Entity entity = worldForCollectibleResolve.ClassRegistry.CreateEntity(text2);
				entity.FromBytes(binaryReader, isSync: false, ((IServerWorldAccessor)worldForCollectibleResolve).RemappedEntities);
				entity.DidImportOrExport(startPos);
				if (OriginalPos != null)
				{
					BlockPos blockPos2 = entity.WatchedAttributes.GetBlockPos("importOffset", Zero);
					entity.WatchedAttributes.SetBlockPos("importOffset", startPos - OriginalPos + blockPos2);
				}
				if (worldForCollectibleResolve.GetEntityType(entity.Code) != null)
				{
					if (blockAccessor is IWorldGenBlockAccessor worldGenBlockAccessor)
					{
						worldGenBlockAccessor.AddEntity(entity);
						entity.OnInitialized += delegate
						{
							entity.OnLoadCollectibleMappings(worldForCollectibleResolve, BlockCodes, ItemCodes, schematicSeed, resolveImports);
						};
						continue;
					}
					worldForCollectibleResolve.SpawnEntity(entity);
					if (blockAccessor is IBlockAccessorRevertable blockAccessorRevertable)
					{
						blockAccessorRevertable.StoreEntitySpawnToHistory(entity);
					}
					entity.OnLoadCollectibleMappings(worldForCollectibleResolve, BlockCodes, ItemCodes, schematicSeed, resolveImports);
				}
				else
				{
					worldForCollectibleResolve.Logger.Error("Couldn't import entity {0} with id {1} and code {2} - it's Type is null! Maybe from an older game version or a missing mod.", entity.GetType(), entity.EntityId, entity.Code);
				}
			}
			catch (Exception)
			{
				worldForCollectibleResolve.Logger.Error("Couldn't import entity with classname {0} - Maybe from an older game version or a missing mod.", text2);
			}
		}
	}

	public virtual BlockPos[] GetJustPositions(BlockPos origin)
	{
		BlockPos[] array = new BlockPos[Indices.Count];
		for (int i = 0; i < Indices.Count; i++)
		{
			uint num = Indices[i];
			int x = (int)(num & 0x3FF);
			int y = (int)((num >> 20) & 0x3FF);
			int z = (int)((num >> 10) & 0x3FF);
			BlockPos blockPos = new BlockPos(x, y, z);
			array[i] = blockPos.Add(origin);
		}
		return array;
	}

	public virtual BlockPos GetStartPos(BlockPos pos, EnumOrigin origin)
	{
		return AdjustStartPos(pos.Copy(), origin);
	}

	public virtual BlockPos AdjustStartPos(BlockPos startpos, EnumOrigin origin)
	{
		if (origin == EnumOrigin.TopCenter)
		{
			startpos.X -= SizeX / 2;
			startpos.Y -= SizeY;
			startpos.Z -= SizeZ / 2;
		}
		if (origin == EnumOrigin.BottomCenter)
		{
			startpos.X -= SizeX / 2;
			startpos.Z -= SizeZ / 2;
		}
		if (origin == EnumOrigin.MiddleCenter)
		{
			startpos.X -= SizeX / 2;
			startpos.Y -= SizeY / 2;
			startpos.Z -= SizeZ / 2;
		}
		return startpos;
	}

	public static BlockSchematic LoadFromFile(string infilepath, ref string error)
	{
		if (!File.Exists(infilepath) && File.Exists(infilepath + ".json"))
		{
			infilepath += ".json";
		}
		if (!File.Exists(infilepath))
		{
			error = "Can't import " + infilepath + ", it does not exist";
			return null;
		}
		BlockSchematic blockSchematic = null;
		try
		{
			using TextReader textReader = new StreamReader(infilepath);
			blockSchematic = JsonConvert.DeserializeObject<BlockSchematic>(textReader.ReadToEnd());
			textReader.Close();
			return blockSchematic;
		}
		catch (Exception ex)
		{
			error = "Failed loading " + infilepath + " : " + ex.Message;
			return null;
		}
	}

	public static BlockSchematic LoadFromString(string jsoncode, ref string error)
	{
		try
		{
			return JsonConvert.DeserializeObject<BlockSchematic>(jsoncode);
		}
		catch (Exception ex)
		{
			error = "Failed loading schematic from json code : " + ex.Message;
			return null;
		}
	}

	public virtual string Save(string outfilepath)
	{
		if (!outfilepath.EndsWithOrdinal(".json"))
		{
			outfilepath += ".json";
		}
		try
		{
			using TextWriter textWriter = new StreamWriter(outfilepath);
			textWriter.Write(JsonConvert.SerializeObject((object)this, (Formatting)0));
			textWriter.Close();
		}
		catch (IOException ex)
		{
			return "Failed exporting: " + ex.Message;
		}
		return null;
	}

	public virtual string ToJson()
	{
		return JsonConvert.SerializeObject((object)this, (Formatting)0);
	}

	public virtual string EncodeBlockEntityData(BlockEntity be)
	{
		using FastMemoryStream ms = new FastMemoryStream();
		return EncodeBlockEntityData(be, ms);
	}

	public virtual string EncodeBlockEntityData(BlockEntity be, FastMemoryStream ms)
	{
		TreeAttribute tree = new TreeAttribute();
		be.ToTreeAttributes(tree);
		return StringEncodeTreeAttribute(tree, ms);
	}

	public virtual string StringEncodeTreeAttribute(ITreeAttribute tree)
	{
		using FastMemoryStream ms = new FastMemoryStream();
		return StringEncodeTreeAttribute(tree, ms);
	}

	public virtual string StringEncodeTreeAttribute(ITreeAttribute tree, FastMemoryStream ms)
	{
		ms.Reset();
		BinaryWriter stream = new BinaryWriter(ms);
		tree.ToBytes(stream);
		return Ascii85.Encode(ms.ToArray());
	}

	public virtual TreeAttribute DecodeBlockEntityData(string data)
	{
		byte[] buffer = Ascii85.Decode(data);
		TreeAttribute treeAttribute = new TreeAttribute();
		using MemoryStream input = new MemoryStream(buffer);
		BinaryReader stream = new BinaryReader(input);
		treeAttribute.FromBytes(stream);
		return treeAttribute;
	}

	public bool IsFillerOrPath(Block newBlock)
	{
		if (newBlock.Id != FillerBlockId)
		{
			return newBlock.Id == PathwayBlockId;
		}
		return true;
	}

	protected virtual int PlaceReplaceAll(IBlockAccessor blockAccessor, BlockPos pos, Block newBlock, bool replaceMeta)
	{
		int layer = ((!newBlock.ForFluidsLayer) ? 1 : 2);
		blockAccessor.SetBlock((replaceMeta && IsFillerOrPath(newBlock)) ? empty : newBlock.BlockId, pos, layer);
		return 1;
	}

	protected virtual int PlaceReplaceable(IBlockAccessor blockAccessor, BlockPos pos, Block newBlock, bool replaceMeta)
	{
		if (newBlock.ForFluidsLayer || blockAccessor.GetBlock(pos, 4).Replaceable > newBlock.Replaceable)
		{
			int layer = ((!newBlock.ForFluidsLayer) ? 1 : 2);
			blockAccessor.SetBlock((replaceMeta && IsFillerOrPath(newBlock)) ? empty : newBlock.BlockId, pos, layer);
			return 1;
		}
		return 0;
	}

	protected virtual int PlaceReplaceAllNoAir(IBlockAccessor blockAccessor, BlockPos pos, Block newBlock, bool replaceMeta)
	{
		if (newBlock.BlockId != 0)
		{
			int layer = ((!newBlock.ForFluidsLayer) ? 1 : 2);
			blockAccessor.SetBlock((replaceMeta && IsFillerOrPath(newBlock)) ? empty : newBlock.BlockId, pos, layer);
			return 1;
		}
		return 0;
	}

	protected virtual int PlaceReplaceOnlyAir(IBlockAccessor blockAccessor, BlockPos pos, Block newBlock, bool replaceMeta)
	{
		if (blockAccessor.GetMostSolidBlock(pos).BlockId == 0)
		{
			int layer = ((!newBlock.ForFluidsLayer) ? 1 : 2);
			blockAccessor.SetBlock((replaceMeta && IsFillerOrPath(newBlock)) ? empty : newBlock.BlockId, pos, layer);
			return 1;
		}
		return 0;
	}

	public virtual BlockSchematic ClonePacked()
	{
		return new BlockSchematic
		{
			SizeX = SizeX,
			SizeY = SizeY,
			SizeZ = SizeZ,
			GameVersion = GameVersion,
			BlockCodes = new Dictionary<int, AssetLocation>(BlockCodes),
			ItemCodes = new Dictionary<int, AssetLocation>(ItemCodes),
			Indices = new List<uint>(Indices),
			BlockIds = new List<int>(BlockIds),
			BlockEntities = new Dictionary<uint, string>(BlockEntities),
			Entities = new List<string>(Entities),
			DecorIndices = new List<uint>(DecorIndices),
			DecorIds = new List<long>(DecorIds),
			ReplaceMode = ReplaceMode,
			EntranceRotation = EntranceRotation,
			OriginalPos = OriginalPos
		};
	}

	public void PasteToMiniDimension(ICoreServerAPI sapi, IBlockAccessor blockAccess, IMiniDimension miniDimension, BlockPos originPos, bool replaceMetaBlocks)
	{
		Init(blockAccess);
		Place(miniDimension, sapi.World, originPos, EnumReplaceMode.ReplaceAll, replaceMetaBlocks);
		PlaceDecors(miniDimension, originPos);
	}
}
