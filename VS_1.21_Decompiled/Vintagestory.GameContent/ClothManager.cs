using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ClothManager : ModSystem, IRenderer, IDisposable
{
	private int nextClothId = 1;

	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	private ICoreAPI api;

	private Dictionary<int, ClothSystem> clothSystems = new Dictionary<int, ClothSystem>();

	internal ParticlePhysics partPhysics;

	private MeshRef ropeMeshRef;

	private MeshData updateMesh;

	private IShaderProgram prog;

	private ILoadedSound stretchSound;

	public float accum3s;

	public float accum100ms;

	private IServerNetworkChannel clothSystemChannel;

	public double RenderOrder => 1.0;

	public int RenderRange => 12;

	public override double ExecuteOrder()
	{
		return 0.4;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		this.api = api;
		partPhysics = new ParticlePhysics(api.World.GetLockFreeBlockAccessor());
		partPhysics.PhysicsTickTime = 1f / 60f;
		partPhysics.MotionCap = 10f;
		api.Network.RegisterChannel("clothphysics").RegisterMessageType<UnregisterClothSystemPacket>().RegisterMessageType<ClothSystemPacket>()
			.RegisterMessageType<ClothPointPacket>()
			.RegisterMessageType<ClothLengthPacket>();
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.ChatCommands.GetOrCreate("debug").BeginSubCommand("clothtest").WithDescription("Commands to test the cloth system")
			.BeginSubCommand("clear")
			.WithDescription("clears")
			.HandleWith(onClothTestClear)
			.EndSubCommand()
			.EndSubCommand();
		api.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "clothsimu");
		api.Event.BlockTexturesLoaded += Event_BlockTexturesLoaded;
		api.Network.GetChannel("clothphysics").SetMessageHandler<UnregisterClothSystemPacket>(onUnregPacketClient).SetMessageHandler<ClothSystemPacket>(onRegPacketClient)
			.SetMessageHandler<ClothPointPacket>(onPointPacketClient)
			.SetMessageHandler<ClothLengthPacket>(onLengthPacketClient);
		api.Event.LeaveWorld += Event_LeaveWorld;
	}

	public ClothSystem GetClothSystem(int clothid)
	{
		clothSystems.TryGetValue(clothid, out var value);
		return value;
	}

	public ClothSystem GetClothSystemAttachedToBlock(BlockPos pos)
	{
		foreach (ClothSystem value in clothSystems.Values)
		{
			if (value.FirstPoint.PinnedToBlockPos == pos || value.LastPoint.PinnedToBlockPos == pos)
			{
				return value;
			}
		}
		return null;
	}

	public void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		if (updateMesh == null)
		{
			return;
		}
		dt = Math.Min(dt, 0.5f);
		if (!capi.IsGamePaused)
		{
			tickPhysics(dt);
		}
		accum100ms += dt;
		if ((double)accum100ms > 0.1)
		{
			accum100ms = 0f;
			if (clothSystems.Count > 0)
			{
				float num = -1f;
				ClothSystem clothSystem = null;
				float num2 = 0.4f;
				foreach (KeyValuePair<int, ClothSystem> clothSystem2 in clothSystems)
				{
					ClothSystem value = clothSystem2.Value;
					if (value.MaxExtension > (double)value.StretchWarn)
					{
						value.secondsOverStretched += dt;
					}
					else
					{
						value.secondsOverStretched = 0f;
					}
					if (value.MaxExtension > (double)num)
					{
						num = (float)value.MaxExtension;
						clothSystem = value;
						num2 = value.StretchWarn;
					}
				}
				if (num > num2 && (double)clothSystem.secondsOverStretched > 0.2)
				{
					float num3 = 10f * (num - num2);
					if (!stretchSound.IsPlaying)
					{
						stretchSound.Start();
					}
					stretchSound.SetPosition((float)clothSystem.CenterPosition.X, (float)clothSystem.CenterPosition.Y, (float)clothSystem.CenterPosition.Z);
					stretchSound.SetVolume(GameMath.Clamp(num3, 0.5f, 1f));
					stretchSound.SetPitch(GameMath.Clamp(num3 + 0.7f, 0.7f, 1.2f));
				}
				else
				{
					stretchSound.Stop();
				}
			}
			else
			{
				stretchSound.Stop();
			}
		}
		int num4 = 0;
		updateMesh.CustomFloats.Count = 0;
		foreach (KeyValuePair<int, ClothSystem> clothSystem3 in clothSystems)
		{
			if (clothSystem3.Value.Active)
			{
				num4 += clothSystem3.Value.UpdateMesh(updateMesh, dt);
				updateMesh.CustomFloats.Count = num4 * 20;
			}
		}
		if (num4 > 0)
		{
			if (prog.Disposed)
			{
				prog = capi.Shader.GetProgramByName("instanced");
			}
			capi.Render.GlToggleBlend(blend: false);
			prog.Use();
			prog.BindTexture2D("tex", capi.ItemTextureAtlas.Positions[0].atlasTextureId, 0);
			prog.Uniform("rgbaFogIn", capi.Render.FogColor);
			prog.Uniform("rgbaAmbientIn", capi.Render.AmbientColor);
			prog.Uniform("fogMinIn", capi.Render.FogMin);
			prog.Uniform("fogDensityIn", capi.Render.FogDensity);
			prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
			prog.UniformMatrix("modelViewMatrix", capi.Render.CameraMatrixOriginf);
			updateMesh.CustomFloats.Count = num4 * 20;
			capi.Render.UpdateMesh(ropeMeshRef, updateMesh);
			capi.Render.RenderMeshInstanced(ropeMeshRef, num4);
			prog.Stop();
		}
		foreach (KeyValuePair<int, ClothSystem> clothSystem4 in clothSystems)
		{
			if (clothSystem4.Value.Active)
			{
				clothSystem4.Value.CustomRender(dt);
			}
		}
	}

	private void tickPhysics(float dt)
	{
		foreach (KeyValuePair<int, ClothSystem> clothSystem2 in clothSystems)
		{
			if (clothSystem2.Value.Active)
			{
				clothSystem2.Value.updateFixedStep(dt);
			}
		}
		if (sapi == null)
		{
			return;
		}
		List<int> list = new List<int>();
		accum100ms += dt;
		if (accum100ms > 0.1f)
		{
			accum100ms = 0f;
			List<ClothPointPacket> list2 = new List<ClothPointPacket>();
			foreach (KeyValuePair<int, ClothSystem> clothSystem3 in clothSystems)
			{
				ClothSystem value = clothSystem3.Value;
				value.CollectDirtyPoints(list2);
				if (value.MaxExtension > (double)value.StretchRip)
				{
					value.secondsOverStretched += 0.1f;
					if ((double)value.secondsOverStretched > 4.0 - value.MaxExtension * 2.0)
					{
						Vec3d vec3d = value.CenterPosition;
						if (value.FirstPoint.PinnedToEntity != null)
						{
							vec3d = value.FirstPoint.PinnedToEntity.Pos.XYZ;
						}
						sapi.World.PlaySoundAt(new AssetLocation("sounds/effect/roperip"), vec3d.X, vec3d.Y, vec3d.Z);
						ClothPoint firstPoint = value.FirstPoint;
						Vec3d vec3d2 = value.LastPoint.Pos - firstPoint.Pos;
						double num = vec3d2.Length();
						for (float num2 = 0f; (double)num2 < num; num2 += 0.15f)
						{
							Vec3d vec3d3 = new Vec3d(firstPoint.Pos.X + vec3d2.X * (double)num2 / num, firstPoint.Pos.Y + vec3d2.Y * (double)num2 / num, firstPoint.Pos.Z + vec3d2.Z * (double)num2 / num);
							sapi.World.SpawnParticles(2f, ColorUtil.ColorFromRgba(60, 97, 115, 255), vec3d3, vec3d3, new Vec3f(-4f, -1f, -4f), new Vec3f(4f, 2f, 4f), 2f, 1f, 0.5f, EnumParticleModel.Cube);
						}
						list.Add(clothSystem3.Key);
					}
				}
				else
				{
					value.secondsOverStretched = 0f;
				}
			}
			foreach (ClothPointPacket item in list2)
			{
				clothSystemChannel.BroadcastPacket(item);
			}
		}
		accum3s += dt;
		if (accum3s > 3f)
		{
			accum3s = 0f;
			foreach (KeyValuePair<int, ClothSystem> clothSystem4 in clothSystems)
			{
				if (!clothSystem4.Value.PinnedAnywhere)
				{
					list.Add(clothSystem4.Key);
				}
				else
				{
					clothSystem4.Value.slowTick3s();
				}
			}
		}
		foreach (int id in list)
		{
			bool spawnitem = true;
			ClothSystem clothSystem = clothSystems[id];
			spawnitem &= (clothSystem.FirstPoint.PinnedToEntity as EntityItem)?.Itemstack?.Collectible.Code.Path != "rope" && (clothSystem.LastPoint.PinnedToEntity as EntityItem)?.Itemstack?.Collectible.Code.Path != "rope";
			if (clothSystem.FirstPoint.PinnedToEntity is EntityAgent entityAgent)
			{
				entityAgent.WalkInventory(delegate(ItemSlot slot)
				{
					if (slot.Empty)
					{
						return true;
					}
					if ((slot.Itemstack.Attributes?.GetInt("clothId") ?? 0) == id)
					{
						spawnitem = false;
						slot.Itemstack.Attributes.RemoveAttribute("clothId");
						slot.Itemstack.Attributes.RemoveAttribute("ropeHeldByEntityId");
						return false;
					}
					return true;
				});
			}
			if (clothSystem.LastPoint.PinnedToEntity is EntityAgent entityAgent2)
			{
				entityAgent2.WalkInventory(delegate(ItemSlot slot)
				{
					if (slot.Empty)
					{
						return true;
					}
					if ((slot.Itemstack.Attributes?.GetInt("clothId") ?? 0) == id)
					{
						spawnitem = false;
						slot.Itemstack.Attributes.RemoveAttribute("clothId");
						slot.Itemstack.Attributes.RemoveAttribute("ropeHeldByEntityId");
						return false;
					}
					return true;
				});
			}
			if (spawnitem)
			{
				sapi.World.SpawnItemEntity(new ItemStack(sapi.World.GetItem(new AssetLocation("rope"))), clothSystems[id].CenterPosition);
			}
			else if (clothSystem.FirstPoint.PinnedToEntity is EntityItem && clothSystem.LastPoint.PinnedToEntity is EntityPlayer)
			{
				clothSystem.FirstPoint.PinnedToEntity.Die(EnumDespawnReason.Removed);
			}
			UnregisterCloth(id);
		}
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	private void Event_LeaveWorld()
	{
		ropeMeshRef?.Dispose();
	}

	private void onPointPacketClient(ClothPointPacket msg)
	{
		if (clothSystems.TryGetValue(msg.ClothId, out var value))
		{
			value.updatePoint(msg);
		}
	}

	private void onLengthPacketClient(ClothLengthPacket msg)
	{
		if (clothSystems.TryGetValue(msg.ClothId, out var value))
		{
			value.ChangeRopeLength(msg.LengthChange);
		}
	}

	private void onRegPacketClient(ClothSystemPacket msg)
	{
		ClothSystem[] array = msg.ClothSystems;
		foreach (ClothSystem clothSystem in array)
		{
			clothSystem.Init(capi, this);
			clothSystem.restoreReferences();
			clothSystems[clothSystem.ClothId] = clothSystem;
		}
	}

	private void onUnregPacketClient(UnregisterClothSystemPacket msg)
	{
		int[] clothIds = msg.ClothIds;
		foreach (int clothId in clothIds)
		{
			UnregisterCloth(clothId);
		}
	}

	private void Event_BlockTexturesLoaded()
	{
		if (stretchSound == null)
		{
			stretchSound = capi.World.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/ropestretch"),
				DisposeOnFinish = false,
				ShouldLoop = true,
				SoundType = EnumSoundType.Sound,
				Volume = 0.5f,
				ReferenceDistance = 5f
			});
		}
		prog = capi.Shader.GetProgramByName("instanced");
		Item item = capi.World.GetItem(new AssetLocation("rope"));
		Shape shape = Shape.TryGet(capi, "shapes/item/ropesection.json");
		if (item != null && shape != null)
		{
			capi.Tesselator.TesselateShape(item, shape, out var modeldata);
			updateMesh = new MeshData(initialiseArrays: false);
			updateMesh.CustomFloats = new CustomMeshDataPartFloat(202000)
			{
				Instanced = true,
				InterleaveOffsets = new int[5] { 0, 16, 32, 48, 64 },
				InterleaveSizes = new int[5] { 4, 4, 4, 4, 4 },
				InterleaveStride = 80,
				StaticDraw = false
			};
			updateMesh.CustomFloats.SetAllocationSize(202000);
			modeldata.CustomFloats = updateMesh.CustomFloats;
			ropeMeshRef = capi.Render.UploadMesh(modeldata);
			updateMesh.CustomFloats.Count = 0;
		}
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		base.StartServerSide(api);
		api.Event.RegisterGameTickListener(tickPhysics, 30);
		api.Event.MapRegionLoaded += Event_MapRegionLoaded;
		api.Event.MapRegionUnloaded += Event_MapRegionUnloaded;
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, onNowRunGame);
		api.Event.PlayerJoin += Event_PlayerJoin;
		clothSystemChannel = api.Network.GetChannel("clothphysics");
		api.ChatCommands.GetOrCreate("debug").BeginSubCommand("clothtest").WithDescription("Commands to test the cloth system")
			.BeginSubCommand("cloth")
			.WithDescription("cloth")
			.HandleWith(onClothTestCloth)
			.EndSubCommand()
			.BeginSubCommand("rope")
			.WithDescription("rope")
			.HandleWith(onClothTestRope)
			.EndSubCommand()
			.BeginSubCommand("clear")
			.WithDescription("clears")
			.HandleWith(onClothTestClearServer)
			.EndSubCommand()
			.BeginSubCommand("deleteloaded")
			.WithDescription("deleteloaded")
			.HandleWith(onClothTestDeleteloaded)
			.EndSubCommand()
			.EndSubCommand();
	}

	private void onNowRunGame()
	{
		foreach (ClothSystem value in clothSystems.Values)
		{
			value.updateActiveState(EnumActiveStateChange.Default);
		}
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		if (clothSystems.Values.Count > 0)
		{
			clothSystemChannel.BroadcastPacket(new ClothSystemPacket
			{
				ClothSystems = clothSystems.Values.ToArray()
			});
		}
	}

	private void Event_GameWorldSave()
	{
		byte[] data = sapi.WorldManager.SaveGame.GetData("nextClothId");
		if (data != null)
		{
			nextClothId = SerializerUtil.Deserialize<int>(data);
		}
	}

	private void Event_SaveGameLoaded()
	{
		sapi.WorldManager.SaveGame.StoreData("nextClothId", SerializerUtil.Serialize(nextClothId));
	}

	private void Event_MapRegionUnloaded(Vec2i mapCoord, IMapRegion region)
	{
		List<ClothSystem> list = new List<ClothSystem>();
		int regionSize = sapi.WorldManager.RegionSize;
		foreach (ClothSystem value in clothSystems.Values)
		{
			BlockPos asBlockPos = value.FirstPoint.Pos.AsBlockPos;
			int num = asBlockPos.X / regionSize;
			int num2 = asBlockPos.Z / regionSize;
			if (num == mapCoord.X && num2 == mapCoord.Y)
			{
				list.Add(value);
			}
		}
		region.SetModdata("clothSystems", SerializerUtil.Serialize(list));
		if (list.Count == 0)
		{
			return;
		}
		int[] array = new int[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			clothSystems.Remove(list[i].ClothId);
			array[i] = list[i].ClothId;
		}
		foreach (ClothSystem value2 in clothSystems.Values)
		{
			value2.updateActiveState(EnumActiveStateChange.RegionNowUnloaded);
		}
		if (!sapi.Server.IsShuttingDown)
		{
			clothSystemChannel.BroadcastPacket(new UnregisterClothSystemPacket
			{
				ClothIds = array
			});
		}
	}

	private void Event_MapRegionLoaded(Vec2i mapCoord, IMapRegion region)
	{
		byte[] moddata = region.GetModdata("clothSystems");
		if (moddata != null && moddata.Length != 0)
		{
			List<ClothSystem> list = SerializerUtil.Deserialize<List<ClothSystem>>(moddata);
			if (sapi.Server.CurrentRunPhase < EnumServerRunPhase.RunGame)
			{
				foreach (ClothSystem item in list)
				{
					item.Active = false;
					item.Init(api, this);
					clothSystems[item.ClothId] = item;
				}
				return;
			}
			foreach (ClothSystem value in clothSystems.Values)
			{
				value.updateActiveState(EnumActiveStateChange.RegionNowLoaded);
			}
			foreach (ClothSystem item2 in list)
			{
				item2.Init(api, this);
				item2.restoreReferences();
				clothSystems[item2.ClothId] = item2;
			}
			if (list.Count > 0)
			{
				clothSystemChannel.BroadcastPacket(new ClothSystemPacket
				{
					ClothSystems = list.ToArray()
				});
			}
		}
		else
		{
			if (sapi.Server.CurrentRunPhase < EnumServerRunPhase.RunGame)
			{
				return;
			}
			foreach (ClothSystem value2 in clothSystems.Values)
			{
				value2.updateActiveState(EnumActiveStateChange.RegionNowLoaded);
			}
		}
	}

	private TextCommandResult onClothTestClearServer(TextCommandCallingArgs args)
	{
		int count = clothSystems.Count;
		int[] array = clothSystems.Select((KeyValuePair<int, ClothSystem> s) => s.Value.ClothId).ToArray();
		if (array.Length != 0)
		{
			clothSystemChannel.BroadcastPacket(new UnregisterClothSystemPacket
			{
				ClothIds = array
			});
		}
		clothSystems.Clear();
		nextClothId = 1;
		return TextCommandResult.Success(count + " cloth sims removed");
	}

	private TextCommandResult onClothTestDeleteloaded(TextCommandCallingArgs args)
	{
		int num = 0;
		foreach (KeyValuePair<long, IMapRegion> allLoadedMapRegion in sapi.WorldManager.AllLoadedMapRegions)
		{
			allLoadedMapRegion.Value.RemoveModdata("clothSystems");
			num++;
		}
		clothSystems.Clear();
		nextClothId = 1;
		return TextCommandResult.Success($"Ok, deleted in {num} regions");
	}

	public void RegisterCloth(ClothSystem sys)
	{
		if (api.Side != EnumAppSide.Client)
		{
			sys.ClothId = nextClothId++;
			clothSystems[sys.ClothId] = sys;
			sys.updateActiveState(EnumActiveStateChange.Default);
			clothSystemChannel.BroadcastPacket(new ClothSystemPacket
			{
				ClothSystems = new ClothSystem[1] { sys }
			});
		}
	}

	public void UnregisterCloth(int clothId)
	{
		if (sapi != null)
		{
			clothSystemChannel.BroadcastPacket(new UnregisterClothSystemPacket
			{
				ClothIds = new int[1] { clothId }
			});
		}
		clothSystems.Remove(clothId);
	}

	private TextCommandResult onClothTestClear(TextCommandCallingArgs textCommandCallingArgs)
	{
		int count = clothSystems.Count;
		clothSystems.Clear();
		nextClothId = 1;
		return TextCommandResult.Success(count + " cloth sims removed");
	}

	private TextCommandResult onClothTestCloth(TextCommandCallingArgs args)
	{
		float x = 0.5f + (float)api.World.Rand.NextDouble() * 3f;
		float y = 0.5f + (float)api.World.Rand.NextDouble() * 3f;
		float z = 0.5f + (float)api.World.Rand.NextDouble() * 3f;
		Vec3d vec3d = args.Caller.Entity.Pos.AheadCopy(2.0).XYZ.Add(0.0, 1.0, 0.0);
		ClothSystem clothSystem = ClothSystem.CreateCloth(api, this, vec3d, vec3d.AddCopy(x, y, z));
		RegisterCloth(clothSystem);
		clothSystem.FirstPoint.PinTo(args.Caller.Entity, new Vec3f(0f, 0.5f, 0f));
		return TextCommandResult.Success();
	}

	private TextCommandResult onClothTestRope(TextCommandCallingArgs args)
	{
		float num = 0.5f + (float)api.World.Rand.NextDouble() * 3f;
		float y = 0.5f + (float)api.World.Rand.NextDouble() * 3f;
		num = 5f;
		Vec3d vec3d = args.Caller.Entity.Pos.AheadCopy(2.0).XYZ.Add(0.0, 1.0, 0.0);
		ClothSystem clothSystem = ClothSystem.CreateRope(api, this, vec3d, vec3d.AddCopy(num, y, num), null);
		clothSystem.FirstPoint.PinTo(args.Caller.Entity, new Vec3f(0f, 0.5f, 0f));
		RegisterCloth(clothSystem);
		return TextCommandResult.Success();
	}
}
