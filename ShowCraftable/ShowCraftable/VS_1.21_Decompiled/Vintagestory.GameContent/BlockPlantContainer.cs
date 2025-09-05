using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockPlantContainer : Block
{
	private WorldInteraction[] interactions = Array.Empty<WorldInteraction>();

	public string ContainerSize => Attributes["plantContainerSize"].AsString();

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		LoadColorMapAnyway = true;
		List<ItemStack> list = new List<ItemStack>();
		if (Variant["contents"] != "empty")
		{
			return;
		}
		foreach (Block block in api.World.Blocks)
		{
			if (!block.IsMissing)
			{
				JsonObject attributes = block.Attributes;
				if (attributes != null && attributes["plantContainable"].Exists)
				{
					list.Add(new ItemStack(block));
				}
			}
		}
		foreach (Item item in api.World.Items)
		{
			if (!(item.Code == null) && !item.IsMissing)
			{
				JsonObject attributes2 = item.Attributes;
				if (attributes2 != null && attributes2["plantContainable"].Exists)
				{
					list.Add(new ItemStack(item));
				}
			}
		}
		interactions = new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-flowerpot-plant",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = list.ToArray()
			}
		};
	}

	public ItemStack GetContents(IWorldAccessor world, BlockPos pos)
	{
		return GetBlockEntity<BlockEntityPlantContainer>(pos)?.GetContents();
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		base.OnDecalTesselation(world, decalMesh, pos);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityPlantContainer blockEntityPlantContainer)
		{
			decalMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, blockEntityPlantContainer.MeshAngle, 0f);
		}
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityPlantContainer blockEntityPlantContainer)
		{
			BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)blockPos.X + blockSel.HitPosition.X);
			double x = (double)(float)byPlayer.Entity.Pos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
			float num2 = (float)Math.Atan2(y, x);
			float num3 = (float)Math.PI / 8f;
			float meshAngle = (float)(int)Math.Round(num2 / num3) * num3;
			blockEntityPlantContainer.MeshAngle = meshAngle;
		}
		return num;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		base.OnBlockBroken(world, pos, byPlayer);
		ItemStack contents = GetContents(world, pos);
		if (contents != null)
		{
			world.SpawnItemEntity(contents, pos);
		}
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return base.OnPickBlock(world, pos);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntityPlantContainer blockEntityPlantContainer = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityPlantContainer;
		IPlayerInventoryManager inventoryManager = byPlayer.InventoryManager;
		if (inventoryManager != null && inventoryManager.ActiveHotbarSlot?.Empty == false && blockEntityPlantContainer != null)
		{
			return blockEntityPlantContainer.TryPutContents(byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer);
		}
		return false;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
