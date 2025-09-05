using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityBookshelf : BlockEntityDisplay
{
	private InventoryGeneric inv;

	private Block block;

	private MeshData? mesh;

	private float[]? mat;

	private BEBehaviorShapeMaterialFromAttributes? bh;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "bookshelf";

	public override string AttributeTransformCode => "onshelfTransform";

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

	public int[]? UsableSlots
	{
		get
		{
			if (!(block is BlockBookshelf blockBookshelf))
			{
				return Array.Empty<int>();
			}
			blockBookshelf.UsableSlots.TryGetValue(Type, out int[] value);
			return value;
		}
	}

	public BlockEntityBookshelf()
	{
		inv = new InventoryGeneric(14, "bookshelf-0", null);
	}

	private void initShelf()
	{
		if (Api == null || Type == null || !(base.Block is BlockBookshelf blockBookshelf))
		{
			return;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			mesh = blockBookshelf.GetOrCreateMesh(Type, Material);
			mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(MeshAngleRad)
				.Translate(-0.5f, -0.5f, -0.5f)
				.Values;
		}
		if (!blockBookshelf.UsableSlots.ContainsKey(Type))
		{
			bh.Type = blockBookshelf.UsableSlots.First().Key;
		}
		int[] usableSlots = UsableSlots;
		for (int i = 0; i < Inventory.Count; i++)
		{
			if (!usableSlots.Contains(i))
			{
				Inventory[i].MaxSlotStackSize = 0;
			}
		}
	}

	public override void Initialize(ICoreAPI api)
	{
		bh = GetBehavior<BEBehaviorShapeMaterialFromAttributes>();
		block = api.World.BlockAccessor.GetBlock(Pos);
		base.Initialize(api);
		if (mesh == null && Type != null)
		{
			initShelf();
		}
	}

	public override void OnBlockPlaced(ItemStack? byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		initShelf();
	}

	internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		CollectibleObject collectibleObject = activeHotbarSlot.Itemstack?.Collectible;
		bool flag = collectibleObject?.Attributes != null && collectibleObject.Attributes["bookshelveable"].AsBool();
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
				int num = blockSel.SelectionBoxIndex - 5;
				Api.World.Logger.Audit("{0} Put 1x{1} into Bookshelf slotid {2} at {3}.", byPlayer.PlayerName, inv[num].Itemstack.Collectible.Code, num, Pos);
				return true;
			}
		}
		return false;
	}

	private bool TryPut(ItemSlot slot, BlockSelection blockSel)
	{
		int num = blockSel.SelectionBoxIndex - 5;
		if (num < 0 || num >= inv.Count)
		{
			return false;
		}
		if (!UsableSlots.Contains(num))
		{
			return false;
		}
		for (int i = 0; i < inv.Count; i++)
		{
			int slotId = (num + i) % inv.Count;
			if (inv[slotId].Empty)
			{
				int num2 = slot.TryPutInto(Api.World, inv[slotId]);
				MarkDirty();
				return num2 > 0;
			}
		}
		return false;
	}

	private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
	{
		int num = blockSel.SelectionBoxIndex - 5;
		if (num < 0 || num >= inv.Count)
		{
			return false;
		}
		if (!inv[num].Empty)
		{
			ItemStack itemStack = inv[num].TakeOut(1);
			if (byPlayer.InventoryManager.TryGiveItemstack(itemStack))
			{
				AssetLocation assetLocation = itemStack.Block?.Sounds?.Place;
				Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
			}
			Api.World.Logger.Audit("{0} Took 1x{1} from Bookshelf slotid {2} at {3}.", byPlayer.PlayerName, itemStack.Collectible.Code, num, Pos);
			if (itemStack.StackSize > 0)
			{
				Api.World.SpawnItemEntity(itemStack, Pos);
			}
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
			float x = (float)(i % 7) * 2f / 16f + 0.0625f - 0.5f + 0.0625f;
			float y = (float)(i / 7) * 7.5f / 16f + 0.0625f;
			float z = -0.25f;
			Vec3f vec3f = new Vec3f(x, y, z);
			vec3f = new Matrixf().RotateY(MeshAngleRad).TransformVector(vec3f.ToVec4f(0f)).XYZ;
			tfMatrices[i] = new Matrixf().Translate(vec3f.X, vec3f.Y, vec3f.Z).Translate(0.5f, 0f, 0.5f).RotateY(MeshAngleRad)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return tfMatrices;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		initShelf();
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		if (forPlayer.CurrentBlockSelection == null)
		{
			base.GetBlockInfo(forPlayer, sb);
			return;
		}
		int num = forPlayer.CurrentBlockSelection.SelectionBoxIndex - 5;
		if (num < 0 || num >= inv.Count)
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
