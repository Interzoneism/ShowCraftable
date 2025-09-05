using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class BlockEntityOmokTable : BlockEntityDisplay
{
	private InventoryGeneric inv;

	private int size = 15;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "omoktable";

	public override string AttributeTransformCode => "onshelfTransform";

	public float MeshAngleRad { get; set; }

	public BlockEntityOmokTable()
	{
		inv = new InventoryGeneric(size * size, "omoktable-0", null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
	}

	internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		CollectibleObject collectibleObject = activeHotbarSlot.Itemstack?.Collectible;
		bool flag = collectibleObject?.Attributes != null && collectibleObject.Attributes["omokpiece"].AsBool();
		if (activeHotbarSlot.Empty || !flag)
		{
			if (TryTake(byPlayer, blockSel))
			{
				return true;
			}
			return false;
		}
		if (flag)
		{
			AssetLocation assetLocation = activeHotbarSlot.Itemstack?.Block?.Sounds?.Place;
			if (TryPut(activeHotbarSlot, blockSel))
			{
				Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				return true;
			}
			return false;
		}
		return false;
	}

	private bool TryPut(ItemSlot slot, BlockSelection blockSel)
	{
		int selectionBoxIndex = blockSel.SelectionBoxIndex;
		if (selectionBoxIndex < 0 || selectionBoxIndex >= inv.Count)
		{
			return false;
		}
		if (inv[selectionBoxIndex].Empty)
		{
			int num = slot.TryPutInto(Api.World, inv[selectionBoxIndex]);
			MarkDirty();
			return num > 0;
		}
		return false;
	}

	private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
	{
		int selectionBoxIndex = blockSel.SelectionBoxIndex;
		if (selectionBoxIndex < 0 || selectionBoxIndex >= inv.Count)
		{
			return false;
		}
		if (!inv[selectionBoxIndex].Empty)
		{
			ItemStack itemStack = inv[selectionBoxIndex].TakeOut(1);
			if (byPlayer.InventoryManager.TryGiveItemstack(itemStack))
			{
				AssetLocation assetLocation = itemStack.Block?.Sounds?.Place;
				Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
			}
			if (itemStack.StackSize > 0)
			{
				Api.World.SpawnItemEntity(itemStack, Pos);
			}
			MarkDirty();
			return true;
		}
		return false;
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		Matrixf matrixf = new Matrixf();
		for (int i = 0; i < size * size; i++)
		{
			ItemSlot itemSlot = Inventory[i];
			if (!itemSlot.Empty)
			{
				matrixf.Identity();
				int num = i % size;
				int num2 = i / size;
				matrixf.Translate((0.6f + (float)num) / 16f, 0f, (0.6f + (float)num2) / 16f);
				mesher.AddMeshData(getMesh(itemSlot.Itemstack), matrixf.Values);
			}
		}
		return false;
	}

	public override void updateMeshes()
	{
		for (int i = 0; i < DisplayedItems; i++)
		{
			updateMesh(i);
		}
	}

	protected override float[][] genTransformationMatrices()
	{
		throw new NotImplementedException();
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
	}
}
