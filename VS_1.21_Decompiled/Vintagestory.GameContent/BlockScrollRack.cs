using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockScrollRack : BlockShapeMaterialFromAttributes
{
	public Cuboidf[] slotsHitBoxes;

	public string[] slotSide;

	public int[] oppositeSlotIndex;

	public Dictionary<string, int[]> slotsBySide = new Dictionary<string, int[]>();

	public override string MeshKey { get; } = "ScrollrackMeshes";

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		LoadTypes();
	}

	public override void LoadTypes()
	{
		base.LoadTypes();
		slotsHitBoxes = Attributes["slotsHitBoxes"].AsObject<Cuboidf[]>();
		slotSide = Attributes["slotSide"].AsObject<string[]>();
		oppositeSlotIndex = Attributes["oppositeSlotIndex"].AsObject<int[]>();
		for (int i = 0; i < slotSide.Length; i++)
		{
			string key = slotSide[i];
			int[] value = ((!slotsBySide.TryGetValue(key, out value)) ? new int[1] { i } : value.Append(i));
			slotsBySide[key] = value;
		}
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return GetBlockEntity<BlockEntityScrollRack>(pos)?.getOrCreateSelectionBoxes() ?? base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		BlockEntityScrollRack blockEntity = GetBlockEntity<BlockEntityScrollRack>(pos);
		if (blockEntity != null)
		{
			float[] values = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(blockEntity.MeshAngleRad)
				.Translate(-0.5f, -0.5f, -0.5f)
				.Values;
			blockModelData = GetOrCreateMesh(blockEntity.Type, blockEntity.Material).Clone().MatrixTransform(values);
			decalModelData = GetOrCreateMesh(blockEntity.Type, blockEntity.Material, null, decalTexSource).Clone().MatrixTransform(values);
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		GetBlockEntity<BlockEntityScrollRack>(pos)?.clearUsableSlots();
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityScrollRack blockEntityScrollRack)
		{
			return blockEntityScrollRack.OnInteract(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		return Lang.Get("block-scrollrack-" + itemStack.Attributes.GetString("material"));
	}
}
