using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class Timeswitch : ModSystem
{
	public const float CooldownTime = 3f;

	private const GlKeys TimeswitchHotkey = GlKeys.Y;

	private const int OtherDimension = 2;

	private ICoreServerAPI sapi;

	private IServerNetworkChannel serverChannel;

	private Dictionary<string, TimeSwitchState> timeswitchStatesByPlayerUid = new Dictionary<string, TimeSwitchState>();

	private bool dim2ChunksLoaded;

	private bool allowTimeswitch;

	private bool posEnabled;

	private int baseChunkX;

	private int baseChunkZ;

	private int size = 3;

	private int deactivateRadius = 2;

	private Vec3d centerpos = new Vec3d();

	private CollisionTester collTester;

	private ICoreClientAPI capi;

	private IClientNetworkChannel clientChannel;

	private StoryStructureLocation genStoryStructLoc;

	private GenStoryStructures genGenStoryStructures;

	private int storyTowerBaseY;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		allowTimeswitch = api.World.Config.GetBool("loreContent", defaultValue: true) || api.World.Config.GetBool("allowTimeswitch");
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		if (allowTimeswitch)
		{
			capi.Input.RegisterHotKey("timeswitch", Lang.Get("Time switch"), GlKeys.Y);
			capi.Input.SetHotKeyHandler("timeswitch", OnHotkeyTimeswitch);
			clientChannel = api.Network.RegisterChannel("timeswitch").RegisterMessageType(typeof(TimeSwitchState)).SetMessageHandler<TimeSwitchState>(OnStateReceived)
				.RegisterMessageType(typeof(DimensionSwitchForEntity))
				.SetMessageHandler<DimensionSwitchForEntity>(OnEntityDimensionSwitchPacketReceived);
		}
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		new CmdTimeswitch(api);
		sapi = api;
		if (allowTimeswitch)
		{
			api.Event.PlayerJoin += OnPlayerJoin;
			api.Event.SaveGameLoaded += OnSaveGameLoaded;
			api.Event.GameWorldSave += OnGameGettingSaved;
			serverChannel = api.Network.RegisterChannel("timeswitch").RegisterMessageType(typeof(TimeSwitchState)).RegisterMessageType(typeof(DimensionSwitchForEntity));
			api.Event.RegisterGameTickListener(PlayerEntryCheck, 500);
			collTester = new CollisionTester();
		}
	}

	private void PlayerEntryCheck(float dt)
	{
		if (!posEnabled)
		{
			return;
		}
		ItemStack itemstack = new ItemStack(sapi.World.GetItem("timeswitch"));
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IServerPlayer serverPlayer = (IServerPlayer)allOnlinePlayers[i];
			if (serverPlayer.ConnectionState != EnumClientState.Playing)
			{
				continue;
			}
			ItemSlot itemSlot = serverPlayer.InventoryManager.GetHotbarInventory()[10];
			if (WithinRange(serverPlayer.Entity.ServerPos, deactivateRadius - 1))
			{
				if (!timeswitchStatesByPlayerUid.TryGetValue(serverPlayer.PlayerUID, out var value))
				{
					value = new TimeSwitchState(serverPlayer.PlayerUID);
					timeswitchStatesByPlayerUid[serverPlayer.PlayerUID] = value;
				}
				if (itemSlot.Empty && serverPlayer.Entity.Alive)
				{
					itemSlot.Itemstack = itemstack;
					itemSlot.MarkDirty();
				}
				if (!value.Enabled)
				{
					value.Enabled = true;
					OnStartCommand(serverPlayer);
					serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.GetL(serverPlayer.LanguageCode, "message-timeswitch-detected"), EnumChatType.Notification);
					serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.GetL(serverPlayer.LanguageCode, "message-timeswitch-controls"), EnumChatType.Notification);
				}
			}
			else if (!WithinRange(serverPlayer.Entity.ServerPos, deactivateRadius))
			{
				if (!itemSlot.Empty)
				{
					itemSlot.Itemstack = null;
					itemSlot.MarkDirty();
				}
				if (serverPlayer.Entity.ServerPos.Dimension == 2)
				{
					ActivateTimeswitchServer(serverPlayer, raiseToWorldSurface: true, out var _);
				}
				if (!timeswitchStatesByPlayerUid.TryGetValue(serverPlayer.PlayerUID, out var value2))
				{
					value2 = new TimeSwitchState(serverPlayer.PlayerUID);
					timeswitchStatesByPlayerUid[serverPlayer.PlayerUID] = value2;
				}
				value2.Enabled = false;
			}
		}
	}

	private bool OnHotkeyTimeswitch(KeyCombination comb)
	{
		ItemSkillTimeswitch.UseTimeSwitchSkillClient(capi);
		return true;
	}

	private void OnGameGettingSaved()
	{
		int[] data = new int[3] { baseChunkX, baseChunkZ, size };
		sapi.WorldManager.SaveGame.StoreData("timeswitchPos", SerializerUtil.Serialize(data));
	}

	private void OnSaveGameLoaded()
	{
		if (timeswitchStatesByPlayerUid == null)
		{
			timeswitchStatesByPlayerUid = new Dictionary<string, TimeSwitchState>();
		}
		try
		{
			byte[] data = sapi.WorldManager.SaveGame.GetData("timeswitchPos");
			if (data != null)
			{
				int[] array = SerializerUtil.Deserialize<int[]>(data);
				if (array.Length >= 3)
				{
					baseChunkX = array[0];
					baseChunkZ = array[1];
					size = array[2];
					SetupCenterPos();
				}
			}
		}
		catch (Exception e)
		{
			sapi.World.Logger.Error("Failed loading timeswitchPos. Maybe not yet worldgenned, or else use /timeswitch setpos to set it manually.");
			sapi.World.Logger.Error(e);
		}
	}

	private void OnPlayerJoin(IServerPlayer byPlayer)
	{
		if (!timeswitchStatesByPlayerUid.TryGetValue(byPlayer.PlayerUID, out var _))
		{
			timeswitchStatesByPlayerUid[byPlayer.PlayerUID] = new TimeSwitchState(byPlayer.PlayerUID);
		}
		if (byPlayer.Entity.Pos.Dimension == 2)
		{
			OnStartCommand(byPlayer);
			int chunkX = (int)byPlayer.Entity.Pos.X / 32;
			int chunkY = (int)byPlayer.Entity.Pos.Y / 32;
			int chunkZ = (int)byPlayer.Entity.Pos.Z / 32;
			sapi.WorldManager.SendChunk(chunkX, chunkY, chunkZ, byPlayer, onlyIfInRange: false);
		}
	}

	public void OnStartCommand(IServerPlayer player)
	{
		if (allowTimeswitch && posEnabled)
		{
			LoadChunkColumns();
			if (player != null)
			{
				ForceSendChunkColumns(player);
			}
		}
	}

	private void ActivateTimeswitchClient(TimeSwitchState tsState)
	{
		EntityPlayer entity = capi.World.Player.Entity;
		if (tsState.forcedY != 0)
		{
			entity.SidedPos.Y = tsState.forcedY;
		}
		entity.ChangeDimension(tsState.Activated ? 2 : 0);
	}

	private bool WithinRange(EntityPos pos, int radius)
	{
		return pos.HorDistanceTo(centerpos) < (double)radius;
	}

	public bool ActivateTimeswitchServer(IServerPlayer player, bool raiseToWorldSurface, out string failurereason)
	{
		bool flag = ActivateTimeswitchInternal(player, raiseToWorldSurface, out failurereason);
		if (!flag && failurereason != null)
		{
			TimeSwitchState timeSwitchState = new TimeSwitchState();
			timeSwitchState.failureReason = failurereason;
			serverChannel.SendPacket(timeSwitchState, player);
		}
		return flag;
	}

	private bool ActivateTimeswitchInternal(IServerPlayer byPlayer, bool forced, out string failurereason)
	{
		failurereason = null;
		if (!allowTimeswitch || !posEnabled)
		{
			return false;
		}
		if (byPlayer.Entity.MountedOn != null)
		{
			failurereason = "mounted";
			return false;
		}
		if (byPlayer.Entity.ServerPos.Dimension == 0)
		{
			if (!timeswitchStatesByPlayerUid.TryGetValue(byPlayer.PlayerUID, out var value))
			{
				value = new TimeSwitchState(byPlayer.PlayerUID);
				timeswitchStatesByPlayerUid[byPlayer.PlayerUID] = value;
			}
			if (!value.Enabled)
			{
				return false;
			}
			if (!WithinRange(byPlayer.Entity.ServerPos, deactivateRadius))
			{
				failurereason = "outofrange";
				return false;
			}
			if (!dim2ChunksLoaded)
			{
				failurereason = "wait";
				return false;
			}
			if (genStoryStructLoc != null && !genStoryStructLoc.DidGenerateAdditional)
			{
				failurereason = "wait";
				return false;
			}
		}
		bool flag = forced;
		if (genStoryStructLoc != null)
		{
			double val = Math.Max(0.0, Math.Abs(byPlayer.Entity.ServerPos.X - (double)genStoryStructLoc.CenterPos.X - 0.5) - 9.5);
			double val2 = Math.Max(0.0, Math.Abs(byPlayer.Entity.ServerPos.Z - (double)genStoryStructLoc.CenterPos.Z - 0.5) - 9.5);
			int num = storyTowerBaseY + (int)Math.Max(val, val2) * 3;
			forced |= byPlayer.Entity.ServerPos.Y <= (double)num && !byPlayer.Entity.Controls.IsFlying && !byPlayer.Entity.Controls.Gliding && byPlayer.Entity.ServerPos.Motion.Y > -0.19;
		}
		bool flag2 = !WithinRange(byPlayer.Entity.ServerPos, deactivateRadius + 64);
		int num2 = ((byPlayer.Entity.Pos.Dimension == 0) ? 2 : 0);
		if (timeswitchStatesByPlayerUid.TryGetValue(byPlayer.PlayerUID, out var value2))
		{
			value2.forcedY = 0;
			if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival && !flag2)
			{
				if (flag && (byPlayer.Entity.OnGround || OtherDimensionPositionWouldCollide(byPlayer.Entity, num2, allowTolerance: false)))
				{
					RaisePlayerToTerrainSurface(byPlayer.Entity, num2, value2);
				}
				else if (OtherDimensionPositionWouldCollide(byPlayer.Entity, num2, allowTolerance: true))
				{
					failurereason = "blocked";
					return false;
				}
			}
			value2.Activated = num2 == 2;
			byPlayer.Entity.ChangeDimension(num2);
			value2.baseChunkX = baseChunkX;
			value2.baseChunkZ = baseChunkZ;
			value2.size = size;
			serverChannel.BroadcastPacket(value2);
			spawnTeleportParticles(byPlayer.Entity.ServerPos);
			return true;
		}
		return false;
	}

	private void spawnTeleportParticles(EntityPos pos)
	{
		int num = 53;
		int num2 = 221;
		int num3 = 172;
		SimpleParticleProperties simpleParticleProperties = new SimpleParticleProperties(150f, 200f, (num << 16) | (num2 << 8) | num3 | 0x64000000, new Vec3d(pos.X - 0.5, pos.Y, pos.Z - 0.5), new Vec3d(pos.X + 0.5, pos.Y + 1.8, pos.Z + 0.5), new Vec3f(-0.7f, -0.7f, -0.7f), new Vec3f(1.4f, 1.4f, 1.4f), 2f, 0f, 0.1f, 0.2f, EnumParticleModel.Quad);
		simpleParticleProperties.addLifeLength = 1f;
		simpleParticleProperties.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -10f);
		int dimension = pos.Dimension;
		sapi.World.SpawnParticles(simpleParticleProperties);
		sapi.World.PlaySoundAt(new AssetLocation("sounds/effect/timeswitch"), pos.X, pos.Y, pos.Z, null, randomizePitch: false, 16f);
		simpleParticleProperties.MinPos.Y += dimension * 32768;
		sapi.World.SpawnParticles(simpleParticleProperties);
		sapi.World.PlaySoundAt(new AssetLocation("sounds/effect/timeswitch"), pos.X, pos.Y + (double)(dimension * 32768), pos.Z, null, randomizePitch: false, 16f);
	}

	private void OnStateReceived(TimeSwitchState state)
	{
		if (capi.World?.Player == null)
		{
			return;
		}
		if (state.failureReason.Length > 0)
		{
			capi.TriggerIngameError(capi.World, state.failureReason, Lang.Get("ingameerror-timeswitch-" + state.failureReason));
			if (state.failureReason == "blocked")
			{
				ItemSkillTimeswitch.timeSwitchCooldown = 0f;
			}
		}
		else if (state.playerUID == capi.World.Player.PlayerUID)
		{
			ActivateTimeswitchClient(state);
			MakeChunkColumnsVisibleClient(state.baseChunkX, state.baseChunkZ, state.size, state.Activated ? 2 : 0);
		}
		else
		{
			capi.World.PlayerByUid(state.playerUID)?.Entity?.ChangeDimension(state.Activated ? 2 : 0);
		}
	}

	public void ChangeEntityDimensionOnClient(Entity entity, int dimension)
	{
		serverChannel.BroadcastPacket(new DimensionSwitchForEntity(entity.EntityId, dimension));
	}

	private void OnEntityDimensionSwitchPacketReceived(DimensionSwitchForEntity packet)
	{
		Entity entityById = capi.World.GetEntityById(packet.entityId);
		if (entityById != null)
		{
			entityById.Pos.Dimension = packet.dimension;
			entityById.ServerPos.Dimension = packet.dimension;
			long newChunkIndex3d = capi.World.ChunkProvider.ChunkIndex3D(entityById.Pos);
			capi.World.UpdateEntityChunk(entityById, newChunkIndex3d);
		}
	}

	public void SetPos(BlockPos pos)
	{
		baseChunkX = pos.X / 32;
		baseChunkZ = pos.Z / 32;
		SetupCenterPos();
	}

	private void SetupCenterPos()
	{
		centerpos.Set(baseChunkX * 32 + 16, 0.0, baseChunkZ * 32 + 16);
		deactivateRadius = (size - 1) * 32 + 1;
		posEnabled = true;
	}

	public void CopyBlocksToAltDimension(IBlockAccessor sourceblockAccess, IServerPlayer player)
	{
		if (allowTimeswitch && posEnabled)
		{
			BlockPos blockPos = new BlockPos((baseChunkX - size + 1) * 32, 0, (baseChunkZ - size + 1) * 32);
			BlockPos blockPos2 = blockPos.AddCopy(32 * (size * 2 - 1), 0, 32 * (size * 2 - 1));
			blockPos.Y = Math.Max(0, (sapi.World.SeaLevel - 8) / 32 * 32);
			blockPos2.Y = sapi.WorldManager.MapSizeY;
			BlockSchematic blockSchematic = new BlockSchematic(sapi.World, sourceblockAccess, blockPos, blockPos2, notLiquids: false);
			CreateChunkColumns();
			BlockPos startPos = blockPos.AddCopy(0, 65536, 0);
			IBlockAccessor blockAccessor = sapi.World.BlockAccessor;
			blockSchematic.Init(blockAccessor);
			blockSchematic.Place(blockAccessor, sapi.World, startPos, EnumReplaceMode.ReplaceAll);
			blockSchematic.PlaceDecors(blockAccessor, startPos);
			if (player != null)
			{
				blockPos.dimension = 2;
				blockPos2.dimension = 2;
				sapi.WorldManager.FullRelight(blockPos, blockPos2, sendToClients: false);
				ForceSendChunkColumns(player);
			}
		}
	}

	public void RelightCommand(IBlockAccessor sourceblockAccess, IServerPlayer player)
	{
		RelightAltDimension();
		ForceSendChunkColumns(player);
	}

	private void CreateChunkColumns()
	{
		for (int i = 0; i <= size * 2; i++)
		{
			for (int j = 0; j <= size * 2; j++)
			{
				int cx = baseChunkX - size + i;
				int cz = baseChunkZ - size + j;
				sapi.WorldManager.CreateChunkColumnForDimension(cx, cz, 2);
			}
		}
	}

	public void LoadChunkColumns()
	{
		if (!allowTimeswitch || !posEnabled || dim2ChunksLoaded)
		{
			return;
		}
		for (int i = 0; i < size * 2 - 1; i++)
		{
			for (int j = 0; j < size * 2 - 1; j++)
			{
				int cx = baseChunkX - size + 1 + i;
				int cz = baseChunkZ - size + 1 + j;
				sapi.WorldManager.LoadChunkColumnForDimension(cx, cz, 2);
			}
		}
		dim2ChunksLoaded = true;
	}

	private void ForceSendChunkColumns(IServerPlayer player)
	{
		if (!allowTimeswitch || !posEnabled)
		{
			return;
		}
		int num = size * 2;
		int num2 = baseChunkZ - size;
		for (int i = 0; i <= num; i++)
		{
			int cx = baseChunkX - size + i;
			for (int j = 0; j <= num; j++)
			{
				sapi.WorldManager.ForceSendChunkColumn(player, cx, num2 + j, 2);
			}
		}
	}

	private void MakeChunkColumnsVisibleClient(int baseChunkX, int baseChunkZ, int size, int dimension)
	{
		for (int i = 0; i <= size * 2; i++)
		{
			for (int j = 0; j <= size * 2; j++)
			{
				int cx = baseChunkX - size + i;
				int cz = baseChunkZ - size + j;
				capi.World.SetChunkColumnVisible(cx, cz, dimension);
			}
		}
	}

	public int SetupDim2TowerGeneration(StoryStructureLocation structureLocation, GenStoryStructures genStoryStructures)
	{
		genStoryStructLoc = structureLocation;
		genGenStoryStructures = genStoryStructures;
		storyTowerBaseY = structureLocation.CenterPos.Y + 10;
		sapi.Logger.VerboseDebug("Setup dim2 " + baseChunkX * 32 + ", " + baseChunkZ * 32);
		return size;
	}

	public void AttemptGeneration(IWorldGenBlockAccessor worldgenBlockAccessor)
	{
		if (genStoryStructLoc == null || genStoryStructLoc.DidGenerateAdditional || !AreAllDim0ChunksGenerated(worldgenBlockAccessor))
		{
			return;
		}
		sapi.Logger.VerboseDebug("Timeswitch dim 2 generation: finished stage 1");
		BlockPos asBlockPos = genStoryStructLoc.Location.Start.AsBlockPos;
		asBlockPos.dimension = 2;
		PlaceSchematic(sapi.World.BlockAccessor, "story/" + genStoryStructLoc.Code + "-past", asBlockPos);
		sapi.Logger.VerboseDebug("Timeswitch dim 2 generation: finished stage 2");
		RelightAltDimension();
		AddClaimForDim(2);
		genStoryStructLoc.DidGenerateAdditional = true;
		genGenStoryStructures.StoryStructureInstancesDirty = true;
		sapi.Logger.VerboseDebug("Timeswitch dim 2 generation: finished stage 3");
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IServerPlayer serverPlayer = (IServerPlayer)allOnlinePlayers[i];
			if (serverPlayer.ConnectionState == EnumClientState.Playing && WithinRange(serverPlayer.Entity.ServerPos, deactivateRadius + 2))
			{
				ForceSendChunkColumns(serverPlayer);
			}
		}
		sapi.Logger.VerboseDebug("Timeswitch dim 2 generation: finished stage 4");
	}

	private void RelightAltDimension()
	{
		if (size > 0)
		{
			BlockPos blockPos = new BlockPos((baseChunkX - size) * 32, 0, (baseChunkZ - size) * 32, 2);
			BlockPos maxPos = blockPos.AddCopy(32 * (size * 2 + 1) - 1, sapi.WorldManager.MapSizeY - 1, 32 * (size * 2 + 1) - 1);
			blockPos.Y = (sapi.World.SeaLevel - 8) / 32 * 32;
			sapi.WorldManager.FullRelight(blockPos, maxPos, sendToClients: false);
		}
	}

	private void AddClaimForDim(int dim)
	{
		int num = baseChunkX * 32 + 16;
		int num2 = baseChunkZ * 32 + 16;
		int num3 = size * 32 + 16;
		int num4 = dim * 32768;
		Cuboidi cuboidi = new Cuboidi(num - num3, num4, num2 - num3, num + num3, num4 + sapi.WorldManager.MapSizeY, num2 + num3);
		LandClaim[] array = sapi.World.Claims.Get(cuboidi.Center.AsBlockPos);
		if (array == null || array.Length == 0)
		{
			WorldGenStoryStructure worldGenStoryStructure = genGenStoryStructures.scfg.Structures.First((WorldGenStoryStructure s) => s.Code == genStoryStructLoc.Code);
			sapi.World.Claims.Add(new LandClaim
			{
				Areas = new List<Cuboidi> { cuboidi },
				Description = "Past Dimension",
				ProtectionLevel = worldGenStoryStructure.ProtectionLevel,
				LastKnownOwnerName = "custommessage-thepast",
				AllowUseEveryone = worldGenStoryStructure.AllowUseEveryone,
				AllowTraverseEveryone = worldGenStoryStructure.AllowTraverseEveryone
			});
		}
	}

	private void PlaceSchematic(IBlockAccessor blockAccessor, string genSchematicName, BlockPos start)
	{
		BlockSchematicPartial blockSchematicPartial = LoadSchematic(sapi, genSchematicName);
		if (blockSchematicPartial != null)
		{
			blockSchematicPartial.Init(blockAccessor);
			blockSchematicPartial.blockLayerConfig = genGenStoryStructures.blockLayerConfig;
			blockSchematicPartial.Place(blockAccessor, sapi.World, start, EnumReplaceMode.ReplaceAllNoAir);
			blockSchematicPartial.PlaceDecors(blockAccessor, start);
		}
	}

	private bool AreAllDim0ChunksGenerated(IBlockAccessor wgenBlockAccessor)
	{
		IBlockAccessor blockAccessor = sapi.World.BlockAccessor;
		for (int i = baseChunkX - size + 1; i < baseChunkX + size; i++)
		{
			for (int j = baseChunkZ - size + 1; j < baseChunkZ + size; j++)
			{
				IMapChunk mapChunk = blockAccessor.GetMapChunk(i, j);
				if (mapChunk == null)
				{
					return false;
				}
				if (mapChunk.CurrentPass <= EnumWorldGenPass.Vegetation)
				{
					return false;
				}
			}
		}
		return true;
	}

	private BlockSchematicPartial LoadSchematic(ICoreServerAPI sapi, string schematicName)
	{
		IAsset asset = sapi.Assets.Get(new AssetLocation("worldgen/schematics/" + schematicName + ".json"));
		if (asset == null)
		{
			return null;
		}
		BlockSchematicPartial blockSchematicPartial = asset.ToObject<BlockSchematicPartial>();
		if (blockSchematicPartial == null)
		{
			sapi.World.Logger.Warning("Could not load timeswitching schematic {0}", schematicName);
			return null;
		}
		blockSchematicPartial.FromFileName = ((asset.Location.Domain == "game") ? asset.Name : (asset.Location.Domain + ":" + asset.Name));
		return blockSchematicPartial;
	}

	private bool OtherDimensionPositionWouldCollide(EntityPlayer entity, int otherDim, bool allowTolerance)
	{
		Vec3d xYZ = entity.ServerPos.XYZ;
		xYZ.Y = entity.ServerPos.Y + (double)(otherDim * 32768);
		Cuboidf cuboidf = entity.CollisionBox.Clone();
		if (allowTolerance)
		{
			cuboidf.OmniNotDownGrowBy(-0.0625f);
		}
		return collTester.IsColliding(sapi.World.BlockAccessor, cuboidf, xYZ, alsoCheckTouch: false);
	}

	private void RaisePlayerToTerrainSurface(EntityPlayer entity, int targetDimension, TimeSwitchState tss)
	{
		double x = entity.ServerPos.X;
		double y = entity.ServerPos.Y;
		double z = entity.ServerPos.Z;
		Cuboidd cuboidd = entity.CollisionBox.ToDouble().Translate(x, y, z);
		int num = (int)cuboidd.X1;
		int num2 = (int)cuboidd.Z1;
		int num3 = (int)cuboidd.X2;
		int num4 = (int)cuboidd.Z2;
		int num5 = 0;
		BlockPos blockPos = new BlockPos(targetDimension);
		for (int i = num; i <= num3; i++)
		{
			for (int j = num2; j <= num4; j++)
			{
				blockPos.Set(i, num5, j);
				int num6;
				if (targetDimension == 0)
				{
					num6 = entity.World.BlockAccessor.GetRainMapHeightAt(blockPos);
					if (num6 > storyTowerBaseY)
					{
						num6 = getWorldSurfaceHeight(entity.World.BlockAccessor, blockPos);
					}
				}
				else
				{
					num6 = getWorldSurfaceHeight(entity.World.BlockAccessor, blockPos);
				}
				if (num6 > num5)
				{
					num5 = num6;
				}
			}
		}
		if (num5 > 0)
		{
			tss.forcedY = num5 + 1;
		}
	}

	private int getWorldSurfaceHeight(IBlockAccessor blockAccessor, BlockPos bp)
	{
		while (bp.Y < blockAccessor.MapSizeY)
		{
			if (!blockAccessor.GetBlock(bp, 1).SideIsSolid(bp, BlockFacing.UP.Index))
			{
				return bp.Y - 1;
			}
			bp.Up();
		}
		return 0;
	}
}
