using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class BlockEntityAntlerMount : BlockEntityDisplay
{
	private InventoryGeneric inv;

	private BEBehaviorShapeMaterialFromAttributes? bh;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "antlermount";

	public override string AttributeTransformCode => "onAntlerMountTransform";

	public float MeshAngleRad
	{
		get
		{
			return bh?.MeshAngleY ?? 0f;
		}
		set
		{
			bh.MeshAngleY = value;
		}
	}

	public string? Type => bh?.Type;

	public string? Material => bh?.Material;

	public BlockEntityAntlerMount()
	{
		inv = new InventoryGeneric(1, "antlermount-0", null);
	}

	public override void Initialize(ICoreAPI api)
	{
		bh = GetBehavior<BEBehaviorShapeMaterialFromAttributes>();
		base.Initialize(api);
	}

	internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		CollectibleObject collectibleObject = activeHotbarSlot.Itemstack?.Collectible;
		bool flag = collectibleObject?.Attributes != null && collectibleObject.Attributes["antlerMountable"].AsBool();
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
			AssetLocation assetLocation2 = activeHotbarSlot.Itemstack?.Collectible.Code;
			if (TryPut(activeHotbarSlot))
			{
				Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				Api.World.Logger.Audit("{0} Put 1x{1} on to AntlerMount at {2}.", byPlayer.PlayerName, assetLocation2, blockSel.Position);
				return true;
			}
		}
		return false;
	}

	private bool TryPut(ItemSlot slot)
	{
		int slotId = 0;
		if (inv[slotId].Empty)
		{
			int num = slot.TryPutInto(Api.World, inv[slotId]);
			MarkDirty();
			return num > 0;
		}
		return false;
	}

	private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
	{
		int slotId = 0;
		if (!inv[slotId].Empty)
		{
			ItemStack itemStack = inv[slotId].TakeOut(1);
			if (byPlayer.InventoryManager.TryGiveItemstack(itemStack))
			{
				AssetLocation assetLocation = itemStack.Block?.Sounds?.Place;
				Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
			}
			if (itemStack.StackSize > 0)
			{
				Api.World.SpawnItemEntity(itemStack, Pos);
			}
			Api.World.Logger.Audit("{0} Took 1x{1} from AntlerMount at {2}.", byPlayer.PlayerName, itemStack.Collectible.Code, blockSel.Position);
			MarkDirty();
			return true;
		}
		return false;
	}

	protected override float[][] genTransformationMatrices()
	{
		tfMatrices = new float[Inventory.Count][];
		for (int i = 0; i < Inventory.Count; i++)
		{
			tfMatrices[i] = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateY(MeshAngleRad - (float)Math.PI / 2f).Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return tfMatrices;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		bh?.Init();
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		if (forPlayer.CurrentBlockSelection == null)
		{
			base.GetBlockInfo(forPlayer, sb);
			return;
		}
		int num = 0;
		if (num >= inv.Count)
		{
			base.GetBlockInfo(forPlayer, sb);
			return;
		}
		ItemSlot itemSlot = inv[num];
		if (itemSlot.Empty)
		{
			sb.AppendLine(Lang.Get("Empty"));
		}
		else
		{
			sb.AppendLine(itemSlot.Itemstack.GetName());
		}
	}
}
