using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common.Database;
using Vintagestory.Server;

namespace Vintagestory.Common;

public abstract class WorldMap
{
	public const int chunksize = 32;

	public int index3dMulX;

	public int chunkMapSizeY;

	public int index3dMulZ;

	public float[] BlockLightLevels;

	public byte[] BlockLightLevelsByte;

	public byte[] hueLevels;

	public byte[] satLevels;

	public float[] SunLightLevels;

	public byte[] SunLightLevelsByte;

	public int SunBrightness;

	public Dictionary<long, List<LandClaim>> LandClaimByRegion = new Dictionary<long, List<LandClaim>>();

	private LandClaim ServerLandClaim = new LandClaim
	{
		LastKnownOwnerName = "Server"
	};

	public abstract IWorldAccessor World { get; }

	public abstract ILogger Logger { get; }

	public abstract IList<Block> Blocks { get; }

	public abstract Dictionary<AssetLocation, Block> BlocksByCode { get; }

	public abstract int MapSizeX { get; }

	public abstract int MapSizeY { get; }

	public abstract int MapSizeZ { get; }

	public abstract int RegionMapSizeX { get; }

	public abstract int RegionMapSizeY { get; }

	public abstract int RegionMapSizeZ { get; }

	public abstract int ChunkSize { get; }

	public abstract int ChunkSizeMask { get; }

	public abstract Vec3i MapSize { get; }

	public abstract int RegionSize { get; }

	public abstract List<LandClaim> All { get; }

	public abstract bool DebugClaimPrivileges { get; }

	public int ChunkMapSizeX => MapSizeX / 32;

	public int ChunkMapSizeY => chunkMapSizeY;

	public int ChunkMapSizeZ => MapSizeZ / 32;

	public int GetLightRGBsAsInt(int posX, int posY, int posZ)
	{
		int chunkX = posX / 32;
		int chunkY = posY / 32;
		int chunkZ = posZ / 32;
		if (!IsValidPos(posX, posY, posZ))
		{
			return ColorUtil.HsvToRgba(0, 0, 0, (int)(SunLightLevels[SunBrightness] * 255f));
		}
		IWorldChunk chunk = GetChunk(chunkX, chunkY, chunkZ);
		if (chunk == null)
		{
			return ColorUtil.HsvToRgba(0, 0, 0, (int)(SunLightLevels[SunBrightness] * 255f));
		}
		int index = MapUtil.Index3d(posX & ChunkSizeMask, posY & ChunkSizeMask, posZ & ChunkSizeMask, 32, 32);
		int lightSat;
		ushort num = chunk.Unpack_AndReadLight(index, out lightSat);
		int num2 = num & 0x1F;
		int num3 = (num >> 5) & 0x1F;
		int num4 = num >> 10;
		int a = (int)(SunLightLevels[num2] * 255f);
		byte h = hueLevels[num4];
		int s = satLevels[lightSat];
		int v = (int)(BlockLightLevels[num3] * 255f);
		return ColorUtil.HsvToRgba(h, s, v, a);
	}

	public Vec4f GetLightRGBSVec4f(int posX, int posY, int posZ)
	{
		int num = LoadLightHSVLevels(posX, posY, posZ);
		byte h = hueLevels[(num >> 16) & 0xFF];
		int s = satLevels[(num >> 24) & 0xFF];
		int v = (int)(BlockLightLevels[(num >> 8) & 0xFF] * 255f);
		int num2 = ColorUtil.HsvToRgb(h, s, v);
		return new Vec4f((float)(num2 >> 16) / 255f, (float)((num2 >> 8) & 0xFF) / 255f, (float)(num2 & 0xFF) / 255f, SunLightLevels[num & 0xFF]);
	}

	public int[] GetLightHSVLevels(int posX, int posY, int posZ)
	{
		int[] array = new int[4];
		int num = LoadLightHSVLevels(posX, posY, posZ);
		array[0] = num & 0xFF;
		array[1] = (num >> 8) & 0xFF;
		array[2] = (num >> 16) & 0xFF;
		array[3] = (num >> 24) & 0xFF;
		return array;
	}

	public int LoadLightHSVLevels(int posX, int posY, int posZ)
	{
		int chunkX = posX / 32;
		int chunkY = posY / 32;
		int chunkZ = posZ / 32;
		if (!IsValidPos(posX, posY, posZ))
		{
			return SunBrightness;
		}
		IWorldChunk chunk = GetChunk(chunkX, chunkY, chunkZ);
		if (chunk == null)
		{
			return SunBrightness;
		}
		int index = MapUtil.Index3d(posX & ChunkSizeMask, posY & ChunkSizeMask, posZ & ChunkSizeMask, 32, 32);
		int lightSat;
		int num = chunk.Unpack_AndReadLight(index, out lightSat);
		return (num & 0x1F) | ((num & 0x3E0) << 3) | ((num & 0xFC00) << 6) | (lightSat << 24);
	}

	public LandClaim[] Get(BlockPos pos)
	{
		List<LandClaim> list = new List<LandClaim>();
		long key = MapRegionIndex2D(pos.X / RegionSize, pos.Z / RegionSize);
		if (!LandClaimByRegion.ContainsKey(key))
		{
			return null;
		}
		foreach (LandClaim item in LandClaimByRegion[key])
		{
			if (item.PositionInside(pos))
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}

	public bool TryAccess(IPlayer player, BlockPos pos, EnumBlockAccessFlags accessFlag)
	{
		string claimant;
		EnumWorldAccessResponse enumWorldAccessResponse = TestBlockAccess(player, new BlockSelection
		{
			Position = pos
		}, accessFlag, out claimant);
		if (enumWorldAccessResponse == EnumWorldAccessResponse.Granted)
		{
			return true;
		}
		if (player != null)
		{
			string text = "noprivilege-" + ((accessFlag == EnumBlockAccessFlags.Use) ? "use" : "buildbreak") + "-" + enumWorldAccessResponse.ToString().ToLowerInvariant();
			string text2 = claimant;
			if (claimant.StartsWithOrdinal("custommessage-"))
			{
				text = "noprivilege-buildbreak-" + claimant.Substring("custommessage-".Length);
			}
			if (World.Side == EnumAppSide.Server)
			{
				(player as IServerPlayer).SendIngameError(text, null, text2);
			}
			else
			{
				(World as ClientMain).api.TriggerIngameError(this, text, Lang.Get("ingameerror-" + text, claimant));
			}
			player?.InventoryManager.ActiveHotbarSlot?.MarkDirty();
			World.BlockAccessor.MarkBlockEntityDirty(pos);
			World.BlockAccessor.MarkBlockDirty(pos);
		}
		return false;
	}

	public EnumWorldAccessResponse TestAccess(IPlayer player, BlockPos pos, EnumBlockAccessFlags accessFlag)
	{
		string claimant;
		return TestBlockAccess(player, new BlockSelection
		{
			Position = pos
		}, accessFlag, out claimant);
	}

	public EnumWorldAccessResponse TestBlockAccess(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType)
	{
		string claimant;
		return TestBlockAccess(player, blockSel, accessType, out claimant);
	}

	public EnumWorldAccessResponse TestBlockAccess(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, out string claimant)
	{
		LandClaim claim;
		EnumWorldAccessResponse response = testBlockAccessInternal(player, blockSel, accessType, out claimant, out claim);
		if (World.Side == EnumAppSide.Client)
		{
			return ((ClientEventAPI)World.Api.Event).TriggerTestBlockAccess(player, blockSel, accessType, ref claimant, claim, response);
		}
		return ((ServerEventAPI)World.Api.Event).TriggerTestBlockAccess(player, blockSel, accessType, ref claimant, claim, response);
	}

	private EnumWorldAccessResponse testBlockAccessInternal(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, out string claimant, out LandClaim claim)
	{
		claim = null;
		EnumWorldAccessResponse enumWorldAccessResponse = testBlockAccess(player, accessType, out claimant);
		if (enumWorldAccessResponse != EnumWorldAccessResponse.Granted)
		{
			return enumWorldAccessResponse;
		}
		bool flag = player.HasPrivilege(Privilege.useblockseverywhere) && player.WorldData.CurrentGameMode == EnumGameMode.Creative;
		bool flag2 = player.HasPrivilege(Privilege.buildblockseverywhere) && player.WorldData.CurrentGameMode == EnumGameMode.Creative;
		if (DebugClaimPrivileges)
		{
			Logger.VerboseDebug("Privdebug: type: {3}, player: {0}, canUseClaimed: {1}, canBreakClaimed: {2}", player?.PlayerName, flag, flag2, accessType);
		}
		ServerMain serverMain = World as ServerMain;
		if (accessType == EnumBlockAccessFlags.Use)
		{
			if (!flag && (claim = GetBlockingLandClaimant(player, blockSel.Position, EnumBlockAccessFlags.Use)) != null)
			{
				claimant = claim.LastKnownOwnerName;
				return EnumWorldAccessResponse.LandClaimed;
			}
			if (serverMain != null && !serverMain.EventManager.TriggerCanUse(player as IServerPlayer, blockSel))
			{
				return EnumWorldAccessResponse.DeniedByMod;
			}
			return EnumWorldAccessResponse.Granted;
		}
		if (!flag2 && (claim = GetBlockingLandClaimant(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak)) != null)
		{
			claimant = claim.LastKnownOwnerName;
			return EnumWorldAccessResponse.LandClaimed;
		}
		if (serverMain != null && !serverMain.EventManager.TriggerCanPlaceOrBreak(player as IServerPlayer, blockSel, out claimant))
		{
			return EnumWorldAccessResponse.DeniedByMod;
		}
		return EnumWorldAccessResponse.Granted;
	}

	private EnumWorldAccessResponse testBlockAccess(IPlayer player, EnumBlockAccessFlags accessType, out string claimant)
	{
		if (player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
		{
			claimant = "custommessage-inspectatormode";
			return EnumWorldAccessResponse.InSpectatorMode;
		}
		if (!player.Entity.Alive)
		{
			claimant = "custommessage-dead";
			return EnumWorldAccessResponse.PlayerDead;
		}
		if (accessType == EnumBlockAccessFlags.BuildOrBreak)
		{
			if (player.WorldData.CurrentGameMode == EnumGameMode.Guest)
			{
				claimant = "custommessage-inguestmode";
				return EnumWorldAccessResponse.InGuestMode;
			}
			if (!player.HasPrivilege(Privilege.buildblocks))
			{
				claimant = "custommessage-nobuildprivilege";
				return EnumWorldAccessResponse.NoPrivilege;
			}
			claimant = null;
			return EnumWorldAccessResponse.Granted;
		}
		if (!player.HasPrivilege(Privilege.useblock))
		{
			claimant = "custommessage-nouseprivilege";
			return EnumWorldAccessResponse.NoPrivilege;
		}
		claimant = null;
		return EnumWorldAccessResponse.Granted;
	}

	public LandClaim GetBlockingLandClaimant(IPlayer forPlayer, BlockPos pos, EnumBlockAccessFlags accessFlag)
	{
		long key = MapRegionIndex2D(pos.X / RegionSize, pos.Z / RegionSize);
		if (!LandClaimByRegion.ContainsKey(key))
		{
			if (DebugClaimPrivileges)
			{
				Logger.VerboseDebug("Privdebug: No land claim in this region. Pos: {0}/{1}", pos.X, pos.Z);
			}
			return null;
		}
		if (DebugClaimPrivileges && LandClaimByRegion[key].Count == 0)
		{
			Logger.VerboseDebug("Privdebug: Land claim list in this region is empty. Pos: {0}/{1}", pos.X, pos.Z);
		}
		if (accessFlag == EnumBlockAccessFlags.Use)
		{
			Block block = World.BlockAccessor.GetBlock(pos);
			IMultiblockOffset multiblockOffset = block.GetInterface<IMultiblockOffset>(World, pos);
			if (multiblockOffset != null)
			{
				pos = multiblockOffset.GetControlBlockPos(pos);
				block = World.BlockAccessor.GetBlock(pos);
			}
			IClaimTraverseable claimTraverseable = block.GetInterface<IClaimTraverseable>(World, pos);
			if (claimTraverseable != null && claimTraverseable.AllowTraverse())
			{
				accessFlag = EnumBlockAccessFlags.Traverse;
			}
		}
		foreach (LandClaim item in LandClaimByRegion[key])
		{
			if (DebugClaimPrivileges)
			{
				Logger.VerboseDebug("Privdebug: posinside: {0}, claim owned by: {3}, forplayer: {1}, canaccess: {2}", item.PositionInside(pos), forPlayer?.PlayerName, (forPlayer != null) ? item.TestPlayerAccess(forPlayer, accessFlag) : EnumPlayerAccessResult.Denied, item.LastKnownOwnerName);
			}
			if (accessFlag == EnumBlockAccessFlags.Traverse)
			{
				if (item.PositionInside(pos) && (forPlayer == null || item.TestPlayerAccess(forPlayer, accessFlag) == EnumPlayerAccessResult.Denied) && !item.AllowTraverseEveryone && !item.AllowUseEveryone)
				{
					return item;
				}
			}
			else if (item.PositionInside(pos) && (forPlayer == null || item.TestPlayerAccess(forPlayer, accessFlag) == EnumPlayerAccessResult.Denied) && (!item.AllowUseEveryone || accessFlag != EnumBlockAccessFlags.Use))
			{
				return item;
			}
		}
		if (forPlayer != null && forPlayer.Role.PrivilegeLevel >= 0)
		{
			return null;
		}
		return ServerLandClaim;
	}

	public void RebuildLandClaimPartitions()
	{
		if (RegionSize == 0)
		{
			Logger.Warning("Call to RebuildLandClaimPartitions, but RegionSize is 0. Wrong startup sequence? Will ignore for now.");
			return;
		}
		HashSet<long> hashSet = new HashSet<long>();
		LandClaimByRegion.Clear();
		foreach (LandClaim item in All)
		{
			hashSet.Clear();
			foreach (Cuboidi area in item.Areas)
			{
				int num = area.MinX / RegionSize;
				int num2 = area.MaxX / RegionSize;
				int num3 = area.MinZ / RegionSize;
				int num4 = area.MaxZ / RegionSize;
				for (int i = num; i <= num2; i++)
				{
					for (int j = num3; j <= num4; j++)
					{
						hashSet.Add(MapRegionIndex2D(i, j));
					}
				}
			}
			foreach (long item2 in hashSet)
			{
				if (!LandClaimByRegion.TryGetValue(item2, out var value))
				{
					value = (LandClaimByRegion[item2] = new List<LandClaim>());
				}
				value.Add(item);
			}
		}
	}

	public long MapRegionIndex2D(int regionX, int regionZ)
	{
		return ((long)regionZ << 32) + regionX;
	}

	[Obsolete("Use dimension aware versions instead, or else the BlockPos or EntityPos overloads")]
	public long ChunkIndex3D(int chunkX, int chunkY, int chunkZ)
	{
		return ((long)chunkY * (long)index3dMulZ + chunkZ) * index3dMulX + chunkX;
	}

	public long ChunkIndex3D(int chunkX, int chunkY, int chunkZ, int dim)
	{
		return ((long)(chunkY + dim * 1024) * (long)index3dMulZ + chunkZ) * index3dMulX + chunkX;
	}

	public long ChunkIndex3D(EntityPos pos)
	{
		ChunkPos cpos = ChunkPos.FromPosition((int)pos.X, (int)pos.Y, (int)pos.Z, pos.Dimension);
		return ChunkIndex3D(cpos);
	}

	public long ChunkIndex3D(ChunkPos cpos)
	{
		return ((long)(cpos.Y + cpos.Dimension * 1024) * (long)index3dMulZ + cpos.Z) * index3dMulX + cpos.X;
	}

	public long MapChunkIndex2D(int chunkX, int chunkZ)
	{
		return (long)chunkZ * (long)ChunkMapSizeX + chunkX;
	}

	public ChunkPos ChunkPosFromChunkIndex3D(long chunkIndex3d)
	{
		int num = (int)(chunkIndex3d / ((long)index3dMulX * (long)index3dMulZ));
		return new ChunkPos((int)(chunkIndex3d % index3dMulX), num % 1024, (int)(chunkIndex3d / index3dMulX % index3dMulZ), num / 1024);
	}

	public ChunkPos ChunkPosFromChunkIndex2D(long index2d)
	{
		return new ChunkPos((int)(index2d % ChunkMapSizeX), 0, (int)(index2d / ChunkMapSizeX), 0);
	}

	public int ChunkSizedIndex3D(int lX, int lY, int lZ)
	{
		return (lY * 32 + lZ) * 32 + lX;
	}

	public bool IsValidPos(BlockPos pos)
	{
		if ((pos.X | pos.Y | pos.Z) >= 0)
		{
			if (pos.X >= MapSizeX || pos.Z >= MapSizeZ)
			{
				return pos.InternalY >= 32768;
			}
			return true;
		}
		return false;
	}

	public bool IsValidPos(int posX, int posY, int posZ)
	{
		if ((posX | posY | posZ) >= 0)
		{
			if (posX >= MapSizeX || posZ >= MapSizeZ)
			{
				return posY >= 32768;
			}
			return true;
		}
		return false;
	}

	public bool IsValidChunkPos(int chunkX, int chunkY, int chunkZ)
	{
		if ((chunkX | chunkY | chunkZ) >= 0)
		{
			if (chunkX >= ChunkMapSizeX || chunkZ >= ChunkMapSizeZ)
			{
				return chunkY >= 1024;
			}
			return true;
		}
		return false;
	}

	public abstract void MarkChunkDirty(int chunkX, int chunkY, int chunkZ, bool priority = false, bool sunRelight = false, Action OnRetesselated = null, bool fireDirtyEvent = true, bool edgeOnly = false);

	public abstract void TriggerNeighbourBlockUpdate(BlockPos pos);

	public abstract void MarkBlockModified(BlockPos pos, bool doRelight = true);

	public abstract void MarkBlockDirty(BlockPos pos, Action OnRetesselated);

	public abstract void MarkBlockDirty(BlockPos pos, IPlayer skipPlayer = null);

	public abstract void MarkBlockEntityDirty(BlockPos pos);

	public bool IsMovementRestrictedPos(double posX, double posY, double posZ, int dimension)
	{
		if (posX < 0.0 || posZ < 0.0 || posX >= (double)MapSizeX || posZ >= (double)MapSizeZ)
		{
			return World.Config.GetString("worldEdge") == "blocked";
		}
		if (posY >= 0.0 && posY < (double)MapSizeY)
		{
			return GetChunkAtPos((int)posX, (int)posY + dimension * 32768, (int)posZ) == null;
		}
		return false;
	}

	internal bool IsPosLoaded(BlockPos pos)
	{
		return GetChunkAtPos(pos.X, pos.Y, pos.Z) != null;
	}

	internal bool AnyLoadedChunkInMapRegion(int chunkx, int chunkz)
	{
		int num = RegionSize / 32;
		for (int i = -1; i < num + 1; i++)
		{
			for (int j = -1; j < num + 1; j++)
			{
				if (IsValidChunkPos(chunkx + i, 0, chunkz + j) && GetMapChunk(chunkx + i, chunkz + j) != null)
				{
					return true;
				}
			}
		}
		return false;
	}

	public abstract IWorldChunk GetChunk(long chunkIndex3D);

	public abstract IWorldChunk GetChunkNonLocking(int chunkX, int chunkY, int chunkZ);

	public abstract IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ);

	public abstract IMapRegion GetMapRegion(int regionX, int regionZ);

	public abstract IMapChunk GetMapChunk(int chunkX, int chunkZ);

	public abstract IWorldChunk GetChunkAtPos(int posX, int posY, int posZ);

	public abstract WorldChunk GetChunk(BlockPos pos);

	public abstract void MarkDecorsDirty(BlockPos pos);

	public virtual void PrintChunkMap(Vec2i markChunkPos = null)
	{
	}

	public abstract void SendSetBlock(int blockId, int posX, int posY, int posZ);

	public abstract void SendExchangeBlock(int blockId, int posX, int posY, int posZ);

	public abstract void UpdateLighting(int oldblockid, int newblockid, BlockPos pos);

	public abstract void RemoveBlockLight(byte[] oldLightHsV, BlockPos pos);

	public abstract void UpdateLightingAfterAbsorptionChange(int oldAbsorption, int newAbsorption, BlockPos pos);

	public abstract void SendBlockUpdateBulk(IEnumerable<BlockPos> blockUpdates, bool doRelight);

	public abstract void SendBlockUpdateBulkMinimal(Dictionary<BlockPos, BlockUpdate> blockUpdates);

	public abstract void UpdateLightingBulk(Dictionary<BlockPos, BlockUpdate> blockUpdates);

	public abstract void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null);

	public abstract void SpawnBlockEntity(BlockEntity be);

	public abstract void RemoveBlockEntity(BlockPos position);

	public abstract BlockEntity GetBlockEntity(BlockPos position);

	public abstract ClimateCondition GetClimateAt(BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.WorldGenValues, double totalDays = 0.0);

	public abstract ClimateCondition GetClimateAt(BlockPos pos, ClimateCondition baseClimate, EnumGetClimateMode mode, double totalDays);

	public abstract ClimateCondition GetClimateAt(BlockPos pos, int climate);

	public abstract Vec3d GetWindSpeedAt(BlockPos pos);

	public abstract Vec3d GetWindSpeedAt(Vec3d pos);

	public abstract void DamageBlock(BlockPos pos, BlockFacing facing, float damage, IPlayer dualCallByPlayer = null);

	public abstract void SendDecorUpdateBulk(IEnumerable<BlockPos> updatedDecorPositions);
}
