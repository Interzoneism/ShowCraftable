using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockMicroBlock : Block
{
	public int snowLayerBlockId;

	private bool IsSnowCovered;

	public ThreadLocal<MicroBlockSounds> MBSounds = new ThreadLocal<MicroBlockSounds>(() => new MicroBlockSounds());

	public static int BlockLayerMetaBlockId;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		notSnowCovered = api.World.GetBlock(AssetLocation.Create(FirstCodePart(), Code.Domain));
		snowCovered1 = api.World.GetBlock(AssetLocation.Create(FirstCodePart() + "-snow", Code.Domain));
		snowCovered2 = api.World.GetBlock(AssetLocation.Create(FirstCodePart() + "-snow2", Code.Domain));
		snowCovered3 = api.World.GetBlock(AssetLocation.Create(FirstCodePart() + "-snow3", Code.Domain));
		if (this == snowCovered1)
		{
			snowLevel = 1f;
		}
		if (this == snowCovered2)
		{
			snowLevel = 2f;
		}
		if (this == snowCovered3)
		{
			snowLevel = 3f;
		}
		CustomBlockLayerHandler = true;
		BlockLayerMetaBlockId = api.World.GetBlock(new AssetLocation("meta-blocklayer")).Id;
		snowLayerBlockId = api.World.GetBlock(new AssetLocation("snowlayer-1")).Id;
		IsSnowCovered = Id != notSnowCovered.Id;
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		MBSounds.Dispose();
	}

	public override BlockSounds GetSounds(IBlockAccessor blockAccessor, BlockSelection blockSel, ItemStack stack = null)
	{
		return (blockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock)?.GetSounds() ?? base.GetSounds(blockAccessor, blockSel, stack);
	}

	public override bool DisplacesLiquids(IBlockAccessor blockAccess, BlockPos pos)
	{
		if (blockAccess.GetBlockEntity(pos) is BlockEntityMicroBlock blockEntityMicroBlock)
		{
			return blockEntityMicroBlock.DisplacesLiquid();
		}
		return base.DisplacesLiquids(blockAccess, pos);
	}

	public override float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos)
	{
		if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock blockEntityMicroBlock)
		{
			return blockEntityMicroBlock.sideAlmostSolid[face.Index] ? 1 : 0;
		}
		return base.GetLiquidBarrierHeightOnSide(face, pos);
	}

	public override bool SideIsSolid(BlockPos pos, int faceIndex)
	{
		return SideIsSolid(api.World.BlockAccessor, pos, faceIndex);
	}

	public override bool SideIsSolid(IBlockAccessor blockAccessor, BlockPos pos, int faceIndex)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock blockEntityMicroBlock)
		{
			return blockEntityMicroBlock.sideAlmostSolid[faceIndex];
		}
		return false;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (pos.X != neibpos.X || pos.Z != neibpos.Z || pos.Y + 1 != neibpos.Y || world.BlockAccessor.GetBlock(neibpos).Id == 0)
		{
			return;
		}
		BEBehaviorMicroblockSnowCover bEBehavior = GetBEBehavior<BEBehaviorMicroblockSnowCover>(pos);
		if (bEBehavior != null)
		{
			bool flag = bEBehavior.SnowLevel != 0 || bEBehavior.SnowCuboids.Count > 0 || bEBehavior.GroundSnowCuboids.Count > 0;
			if (Id != notSnowCovered.Id)
			{
				world.BlockAccessor.ExchangeBlock(notSnowCovered.Id, pos);
				flag = true;
			}
			bEBehavior.SnowLevel = 0;
			bEBehavior.SnowCuboids.Clear();
			bEBehavior.GroundSnowCuboids.Clear();
			if (flag)
			{
				world.BlockAccessor.GetBlockEntity(pos).MarkDirty(redrawOnClient: true);
			}
		}
	}

	public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
	{
		if (!(extra is string) || !((string)extra == "melt"))
		{
			return;
		}
		BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
		BEBehaviorMicroblockSnowCover bEBehaviorMicroblockSnowCover = blockEntity?.GetBehavior<BEBehaviorMicroblockSnowCover>();
		if (bEBehaviorMicroblockSnowCover != null)
		{
			if (this == snowCovered3)
			{
				world.BlockAccessor.ExchangeBlock(snowCovered2.Id, pos);
				bEBehaviorMicroblockSnowCover.SnowLevel = 0;
				blockEntity.MarkDirty(redrawOnClient: true);
			}
			else if (this == snowCovered2)
			{
				world.BlockAccessor.ExchangeBlock(snowCovered1.Id, pos);
				bEBehaviorMicroblockSnowCover.SnowLevel = 0;
				blockEntity.MarkDirty(redrawOnClient: true);
			}
			else if (this == snowCovered1)
			{
				world.BlockAccessor.ExchangeBlock(notSnowCovered.Id, pos);
				bEBehaviorMicroblockSnowCover.SnowLevel = 0;
				blockEntity.MarkDirty(redrawOnClient: true);
			}
		}
	}

	public override float GetSnowLevel(BlockPos pos)
	{
		if (!IsSnowCovered)
		{
			return 0f;
		}
		return 0.5f;
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos)
	{
		if (!(capi.World.Player.InventoryManager.ActiveHotbarSlot?.Itemstack?.Item is ItemChisel) && !BlockEntityChisel.ForceDetailingMode)
		{
			return base.GetSelectionColor(capi, pos);
		}
		BlockEntityMicroBlock blockEntityMicroBlock = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
		if (blockEntityMicroBlock?.BlockIds == null || blockEntityMicroBlock.BlockIds.Length == 0)
		{
			return new Vec4f(0f, 0f, 0f, 0.6f);
		}
		int color = api.World.GetBlock(blockEntityMicroBlock.BlockIds[0]).GetColor(capi, pos);
		if ((double)((float)((color & 0xFF) + ((color >> 8) & 0xFF) + ((color >> 16) & 0xFF)) / 3f) < 102.0)
		{
			return new Vec4f(1f, 1f, 1f, 0.6f);
		}
		return new Vec4f(0f, 0f, 0f, 0.5f);
	}

	public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type)
	{
		BlockEntityMicroBlock blockEntityMicroBlock = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
		if (blockEntityMicroBlock?.BlockIds != null && (blockEntityMicroBlock.sideAlmostSolid[facing.Index] || blockEntityMicroBlock.sideAlmostSolid[facing.Opposite.Index]) && blockEntityMicroBlock.BlockIds.Length != 0 && blockEntityMicroBlock.VolumeRel >= 0.5f)
		{
			if (type == EnumRetentionType.Sound)
			{
				return 10;
			}
			EnumBlockMaterial blockMaterial = api.World.GetBlock(blockEntityMicroBlock.BlockIds[0]).BlockMaterial;
			if (blockMaterial == EnumBlockMaterial.Ore || blockMaterial == EnumBlockMaterial.Stone || blockMaterial == EnumBlockMaterial.Soil || blockMaterial == EnumBlockMaterial.Ceramic)
			{
				return -1;
			}
			return 1;
		}
		return base.GetRetention(pos, facing, type);
	}

	public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		if (pos != null)
		{
			return (blockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock)?.GetLightHsv(blockAccessor) ?? ((byte[])LightHsv);
		}
		int[] array = (stack.Attributes?["materials"] as IntArrayAttribute)?.value;
		byte[] array2 = new byte[3];
		if (array == null)
		{
			return array2;
		}
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			Block block = blockAccessor.GetBlock(array[i]);
			if (block.LightHsv[2] > 0)
			{
				array2[0] += block.LightHsv[0];
				array2[1] += block.LightHsv[1];
				array2[2] += block.LightHsv[2];
				num++;
			}
		}
		if (num == 0)
		{
			return array2;
		}
		array2[0] = (byte)(array2[0] / num);
		array2[1] = (byte)(array2[1] / num);
		array2[2] = (byte)(array2[2] / num);
		return array2;
	}

	public override bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
	{
		if (((inputStack.Attributes["materials"] as StringArrayAttribute)?.value?.Length).GetValueOrDefault() > 2)
		{
			return false;
		}
		return base.MatchesForCrafting(inputStack, gridRecipe, ingredient);
	}

	public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
	{
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		bool flag = false;
		foreach (ItemSlot itemSlot in allInputslots)
		{
			if (itemSlot.Empty)
			{
				continue;
			}
			if (!flag)
			{
				flag = true;
				outputSlot.Itemstack.Attributes = itemSlot.Itemstack.Attributes.Clone();
			}
			int[] array = (itemSlot.Itemstack.Attributes?["materials"] as IntArrayAttribute)?.value;
			if (array != null)
			{
				list.AddRange(array);
			}
			string[] array2 = (itemSlot.Itemstack.Attributes?["materials"] as StringArrayAttribute)?.value;
			if (array2 != null)
			{
				string[] array3 = array2;
				foreach (string domainAndPath in array3)
				{
					Block block = api.World.GetBlock(new AssetLocation(domainAndPath));
					if (block != null)
					{
						list.Add(block.Id);
					}
				}
			}
			int[] array4 = (itemSlot.Itemstack.Attributes?["availMaterialQuantities"] as IntArrayAttribute)?.value;
			if (array4 != null)
			{
				list2.AddRange(array4);
			}
		}
		outputSlot.Itemstack.Attributes["materials"] = new IntArrayAttribute(list.ToArray());
		outputSlot.Itemstack.Attributes["availMaterialQuantities"] = new IntArrayAttribute(list2.ToArray());
		base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe);
	}

	public override int GetLightAbsorption(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return GetLightAbsorption(blockAccessor.GetChunkAtBlockPos(pos), pos);
	}

	public override int GetLightAbsorption(IWorldChunk chunk, BlockPos pos)
	{
		return (chunk?.GetLocalBlockEntityAtBlockPos(pos) as BlockEntityMicroBlock)?.GetLightAbsorption() ?? 0;
	}

	public override EnumBlockMaterial GetBlockMaterial(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		if (blockAccessor is IWorldGenBlockAccessor)
		{
			return base.GetBlockMaterial(blockAccessor, pos, stack);
		}
		if (pos != null)
		{
			if (IsSoilNonSoilMix(blockAccessor, pos))
			{
				return EnumBlockMaterial.Soil;
			}
			BlockEntityMicroBlock blockEntityMicroBlock = blockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
			if (blockEntityMicroBlock?.BlockIds != null && blockEntityMicroBlock.BlockIds.Length != 0)
			{
				return api.World.Blocks[blockEntityMicroBlock.GetMajorityMaterialId()].BlockMaterial;
			}
		}
		else
		{
			int[] array = (stack.Attributes?["materials"] as IntArrayAttribute)?.value;
			if (array != null && array.Length != 0)
			{
				return api.World.GetBlock(array[0]).BlockMaterial;
			}
		}
		return base.GetBlockMaterial(blockAccessor, pos, stack);
	}

	public virtual bool IsSoilNonSoilMix(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return IsSoilNonSoilMix(GetBlockEntity<BlockEntityMicroBlock>(pos));
	}

	public virtual bool IsSoilNonSoilMix(BlockEntityMicroBlock be)
	{
		if (be?.BlockIds == null)
		{
			return false;
		}
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < be.BlockIds.Length; i++)
		{
			Block block = api.World.Blocks[be.BlockIds[i]];
			flag |= block.BlockMaterial == EnumBlockMaterial.Soil || block.BlockMaterial == EnumBlockMaterial.Sand || block.BlockMaterial == EnumBlockMaterial.Gravel;
			flag2 |= block.BlockMaterial != EnumBlockMaterial.Soil && block.BlockMaterial != EnumBlockMaterial.Sand && block.BlockMaterial != EnumBlockMaterial.Gravel;
		}
		return flag && flag2;
	}

	public override bool DoEmitSideAo(IGeometryTester caller, BlockFacing facing)
	{
		return (caller.GetCurrentBlockEntityOnSide(facing.Opposite) as BlockEntityMicroBlock)?.DoEmitSideAo(facing.Index) ?? base.DoEmitSideAo(caller, facing);
	}

	public override bool DoEmitSideAoByFlag(IGeometryTester caller, Vec3iAndFacingFlags vec, int flags)
	{
		return (caller.GetCurrentBlockEntityOnSide(vec) as BlockEntityMicroBlock)?.DoEmitSideAoByFlag(flags) ?? base.DoEmitSideAoByFlag(caller, vec, flags);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock blockEntityMicroBlock))
		{
			return null;
		}
		TreeAttribute treeAttribute = new TreeAttribute();
		blockEntityMicroBlock.ToTreeAttributes(treeAttribute);
		treeAttribute.RemoveAttribute("posx");
		treeAttribute.RemoveAttribute("posy");
		treeAttribute.RemoveAttribute("posz");
		treeAttribute.RemoveAttribute("snowcuboids");
		return new ItemStack(notSnowCovered.Id, EnumItemClass.Block, 1, treeAttribute, world);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		JsonObject attributes = Attributes;
		if (attributes != null && attributes.IsTrue("dropSelf"))
		{
			return new ItemStack[1] { OnPickBlock(world, pos) };
		}
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock blockEntityMicroBlock)
		{
			Block block = world.Blocks[blockEntityMicroBlock.GetMajorityMaterialId()];
			string text = block.Variant["rock"];
			if (block.BlockMaterial == EnumBlockMaterial.Stone && text != null)
			{
				int num = GameMath.RoundRandom(world.Rand, blockEntityMicroBlock.VolumeRel * 4f * dropQuantityMultiplier);
				if (num <= 0)
				{
					return Array.Empty<ItemStack>();
				}
				if (world.GetItem(AssetLocation.Create("stone-" + text, Code.Domain)) != null)
				{
					ItemStack itemStack = new ItemStack(world.GetItem(AssetLocation.Create("stone-" + text, Code.Domain)));
					while (num-- > 0)
					{
						world.SpawnItemEntity(itemStack.Clone(), pos);
					}
				}
			}
		}
		return Array.Empty<ItemStack>();
	}

	public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		if (world.GetBlockEntity(pos) is BlockEntityMicroBlock blockEntityMicroBlock)
		{
			return blockEntityMicroBlock.CanAttachBlockAt(blockFace, attachmentArea);
		}
		return base.CanAttachBlockAt(world, block, pos, blockFace);
	}

	public override bool AllowSnowCoverage(IWorldAccessor world, BlockPos blockPos)
	{
		if (world.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityMicroBlock blockEntityMicroBlock)
		{
			return blockEntityMicroBlock.sideAlmostSolid[BlockFacing.UP.Index];
		}
		return base.AllowSnowCoverage(world, blockPos);
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack)
	{
		base.OnBlockPlaced(world, blockPos, byItemStack);
		if (world.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityMicroBlock blockEntityMicroBlock && byItemStack != null)
		{
			ITreeAttribute treeAttribute = byItemStack.Attributes.Clone();
			treeAttribute.SetInt("posx", blockPos.X);
			treeAttribute.SetInt("posy", blockPos.InternalY);
			treeAttribute.SetInt("posz", blockPos.Z);
			blockEntityMicroBlock.FromTreeAttributes(treeAttribute, world);
			if (world.Side == EnumAppSide.Client)
			{
				blockEntityMicroBlock.MarkMeshDirty();
			}
			blockEntityMicroBlock.MarkDirty(redrawOnClient: true);
			blockEntityMicroBlock.RegenSelectionBoxes(world, null);
		}
	}

	public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		return base.OnGettingBroken(player, blockSel, itemslot, remainingResistance, dt, counter);
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (!TryToRemoveSoilFirst(world, pos, byPlayer))
		{
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		}
	}

	public virtual bool TryToRemoveSoilFirst(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
	{
		if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			BlockEntityMicroBlock blockEntity = GetBlockEntity<BlockEntityMicroBlock>(pos);
			if (blockEntity == null)
			{
				return false;
			}
			bool flag = false;
			Block block = null;
			if (blockEntity.BlockIds.Any((int bid) => world.Blocks[bid].BlockMaterial != EnumBlockMaterial.Soil))
			{
				for (int num = 0; num < blockEntity.BlockIds.Length; num++)
				{
					block = world.Blocks[blockEntity.BlockIds[num]];
					if (block.BlockMaterial == EnumBlockMaterial.Soil)
					{
						blockEntity.RemoveMaterial(block);
						flag = true;
					}
				}
				if (flag)
				{
					world.PlaySoundAt(block.Sounds?.GetBreakSound(byPlayer), pos, 0.0, byPlayer);
					SpawnBlockBrokenParticles(pos);
					blockEntity.MarkDirty(redrawOnClient: true);
					return true;
				}
			}
		}
		return false;
	}

	public override float GetResistance(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockEntityMicroBlock blockEntityMicroBlock = blockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
		if (blockEntityMicroBlock?.BlockIds != null && blockEntityMicroBlock.BlockIds.Length != 0)
		{
			return api.World.GetBlock(blockEntityMicroBlock.BlockIds[0]).Resistance;
		}
		return base.GetResistance(blockAccessor, pos);
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		MicroBlockModelCache modSystem = capi.ModLoader.GetModSystem<MicroBlockModelCache>();
		renderinfo.ModelRef = modSystem.GetOrCreateMeshRef(itemstack);
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock blockEntityMicroBlock)
		{
			blockModelData = blockEntityMicroBlock.GenMesh();
			decalModelData = blockEntityMicroBlock.CreateDecalMesh(decalTexSource);
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock blockEntityMicroBlock)
		{
			return blockEntityMicroBlock.GetSelectionBoxes(blockAccessor, pos);
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock blockEntityMicroBlock)
		{
			return blockEntityMicroBlock.GetCollisionBoxes(blockAccessor, pos);
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock blockEntityMicroBlock)
		{
			return blockEntityMicroBlock.GetCollisionBoxes(blockAccessor, pos);
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		BlockEntityMicroBlock blockEntityMicroBlock = capi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
		if (blockEntityMicroBlock?.BlockIds != null && blockEntityMicroBlock.BlockIds.Length != 0)
		{
			int majorityMaterialId = blockEntityMicroBlock.GetMajorityMaterialId((int blockid) => capi.World.Blocks[blockid].BlockMaterial != EnumBlockMaterial.Meta);
			Block block = capi.World.Blocks[majorityMaterialId];
			if (block is BlockMicroBlock)
			{
				return 0;
			}
			return block.GetRandomColor(capi, pos, facing, rndIndex);
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}

	public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
	{
		List<string> list = ((ignoreAttributeSubTrees == null) ? new List<string>() : new List<string>(ignoreAttributeSubTrees));
		list.Add("meshId");
		return base.Equals(thisStack, otherStack, list.ToArray());
	}

	public override int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
	{
		BlockEntityMicroBlock blockEntityMicroBlock = capi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
		if (blockEntityMicroBlock?.BlockIds != null && blockEntityMicroBlock.BlockIds.Length != 0)
		{
			Block block = capi.World.GetBlock(blockEntityMicroBlock.BlockIds[0]);
			if (block is BlockMicroBlock)
			{
				return 0;
			}
			return block.GetColor(capi, pos);
		}
		return base.GetColorWithoutTint(capi, pos);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		if (itemStack.Attributes.HasAttribute("blockName"))
		{
			string text = itemStack.Attributes.GetString("blockName");
			if (text != "")
			{
				return text.Split('\n')[0];
			}
		}
		int[] blockIds = BlockEntityMicroBlock.MaterialIdsFromAttributes(itemStack.Attributes, api.World);
		int majorityMaterial = BlockEntityMicroBlock.getMajorityMaterial(new List<uint>(BlockEntityMicroBlock.GetVoxelCuboids(itemStack.Attributes)), blockIds);
		Block block = api.World.Blocks[majorityMaterial];
		return block.GetHeldItemName(new ItemStack(block));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		string text = inSlot.Itemstack.Attributes.GetString("blockName");
		int num = text.IndexOf('\n');
		if (num > 0)
		{
			dsc.AppendLine(text.Substring(num + 1));
		}
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine();
		dsc.AppendLine("<font color=\"#bbbbbb\">" + Lang.Get("block-chiseledblock") + "</font>");
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock blockEntityMicroBlock)
		{
			return blockEntityMicroBlock.GetPlacedBlockName();
		}
		return base.GetPlacedBlockName(world, pos);
	}

	public override void PerformSnowLevelUpdate(IBulkBlockAccessor ba, BlockPos pos, Block newBlock, float snowLevel)
	{
		if (newBlock.Id != Id && (BlockMaterial == EnumBlockMaterial.Snow || BlockId == 0 || FirstCodePart() == newBlock.FirstCodePart()))
		{
			ba.ExchangeBlock(newBlock.Id, pos);
		}
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, ItemSlot inSlot, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, bool resolveImports)
	{
		int[] array = BlockEntityMicroBlock.MaterialIdsFromAttributes(inSlot.Itemstack.Attributes, worldForResolve);
		foreach (KeyValuePair<int, AssetLocation> item in oldBlockIdMapping)
		{
			int num = array.IndexOf(item.Key);
			if (num != -1)
			{
				Block block = worldForResolve.GetBlock(item.Value);
				if (block != null)
				{
					array[num] = block.Id;
				}
			}
		}
		inSlot.Itemstack.Attributes["materials"] = new IntArrayAttribute(array);
	}
}
