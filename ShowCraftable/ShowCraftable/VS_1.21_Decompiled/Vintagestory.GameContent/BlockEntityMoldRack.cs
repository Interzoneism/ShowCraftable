using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class BlockEntityMoldRack : BlockEntityDisplay
{
	private InventoryGeneric inv;

	private Block block;

	private Matrixf mat = new Matrixf();

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "moldrack";

	public override string AttributeTransformCode => "onmoldrackTransform";

	public BlockEntityMoldRack()
	{
		inv = new InventoryGeneric(5, "moldrack-0", null);
	}

	public override void Initialize(ICoreAPI api)
	{
		block = api.World.BlockAccessor.GetBlock(Pos);
		base.Initialize(api);
		if (api is ICoreClientAPI)
		{
			mat.RotateYDeg(block.Shape.rotateY);
			api.Event.RegisterEventBusListener(OnEventBusEvent);
		}
	}

	private void OnEventBusEvent(string eventname, ref EnumHandling handling, IAttribute data)
	{
		if ((eventname != "genjsontransform" && eventname != "oncloseedittransforms" && eventname != "onapplytransforms") || Inventory.Empty)
		{
			return;
		}
		for (int i = 0; i < DisplayedItems; i++)
		{
			if (!Inventory[i].Empty)
			{
				string meshCacheKey = getMeshCacheKey(Inventory[i].Itemstack);
				base.MeshCache.Remove(meshCacheKey);
			}
		}
		updateMeshes();
		MarkDirty(redrawOnClient: true);
	}

	internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		CollectibleObject collectibleObject = activeHotbarSlot.Itemstack?.Collectible;
		bool flag = collectibleObject?.Attributes != null && collectibleObject.Attributes["moldrackable"].AsBool();
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
			if (TryPut(activeHotbarSlot, blockSel))
			{
				Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				Api.World.Logger.Audit("{0} Put 1x{1} into Rack at {2}.", byPlayer.PlayerName, assetLocation2, Pos);
				return true;
			}
			return false;
		}
		return false;
	}

	private bool TryPut(ItemSlot slot, BlockSelection blockSel)
	{
		int selectionBoxIndex = blockSel.SelectionBoxIndex;
		for (int i = 0; i < inv.Count; i++)
		{
			int slotId = (selectionBoxIndex + i) % inv.Count;
			if (inv[slotId].Empty)
			{
				int num = slot.TryPutInto(Api.World, inv[slotId]);
				MarkDirty();
				return num > 0;
			}
		}
		return false;
	}

	private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
	{
		int selectionBoxIndex = blockSel.SelectionBoxIndex;
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
			Api.World.Logger.Audit("{0} Took 1x{1} from Rack at {2}.", byPlayer.PlayerName, itemStack.Collectible.Code, Pos);
			MarkDirty();
			return true;
		}
		return false;
	}

	protected override float[][] genTransformationMatrices()
	{
		float[][] array = new float[Inventory.Count][];
		for (int i = 0; i < array.Length; i++)
		{
			float x = 0.1875f + 0.1875f * (float)i - 1f;
			float y = 0f;
			float z = 0f;
			array[i] = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(block.Shape.rotateY).Translate(x, y, z)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return array;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		if (forPlayer.CurrentBlockSelection == null)
		{
			base.GetBlockInfo(forPlayer, sb);
			return;
		}
		int selectionBoxIndex = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
		ItemSlot itemSlot = inv[selectionBoxIndex];
		if (itemSlot.Empty)
		{
			sb.AppendLine(Lang.Get("Empty"));
		}
		else
		{
			sb.AppendLine(itemSlot.Itemstack.GetName());
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}
}
