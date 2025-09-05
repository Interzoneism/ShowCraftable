using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public abstract class BlockEntityBehavior
{
	public BlockEntity Blockentity;

	public JsonObject properties;

	public ICoreAPI Api;

	public BlockPos Pos => Blockentity.Pos;

	public Block Block => Blockentity.Block;

	public BlockEntityBehavior(BlockEntity blockentity)
	{
		Blockentity = blockentity;
	}

	public virtual void Initialize(ICoreAPI api, JsonObject properties)
	{
		Api = api;
	}

	public virtual void OnBlockRemoved()
	{
	}

	public virtual void OnBlockUnloaded()
	{
	}

	public virtual void OnBlockBroken(IPlayer byPlayer = null)
	{
	}

	public virtual void OnBlockPlaced(ItemStack byItemStack = null)
	{
	}

	public virtual void ToTreeAttributes(ITreeAttribute tree)
	{
	}

	public virtual void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
	}

	public virtual void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
	{
	}

	public virtual void OnReceivedServerPacket(int packetid, byte[] data)
	{
	}

	public virtual void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
	}

	public virtual void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
	}

	[Obsolete("Use the variant with resolveImports parameter")]
	public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
	{
		OnLoadCollectibleMappings(worldForNewMappings, oldItemIdMapping, oldItemIdMapping, schematicSeed, resolveImports: true);
	}

	public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
	}

	public virtual bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		return false;
	}

	public virtual void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
	{
	}
}
