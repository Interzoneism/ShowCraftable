using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityScrollRack : BlockEntityDisplay
{
	private InventoryGeneric? inv;

	private int[]? UsableSlots;

	private Cuboidf[] UsableSelectionBoxes;

	private BEBehaviorShapeMaterialFromAttributes? bh;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "scrollrack";

	public override string AttributeTransformCode => "onscrollrackTransform";

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

	public BlockEntityScrollRack()
	{
		inv = new InventoryGeneric(12, "scrollrack-0", null);
	}

	public int[]? getOrCreateUsableSlots()
	{
		if (UsableSlots == null)
		{
			genUsableSlots();
		}
		return UsableSlots;
	}

	public Cuboidf[] getOrCreateSelectionBoxes()
	{
		getOrCreateUsableSlots();
		return UsableSelectionBoxes;
	}

	private void genUsableSlots()
	{
		bool num = isRack(BEBehaviorDoor.getAdjacentOffset(-1, 0, 0, MeshAngleRad, invertHandles: false));
		Dictionary<string, int[]> slotsBySide = ((BlockScrollRack)base.Block).slotsBySide;
		List<int> list = new List<int>();
		list.AddRange(slotsBySide["mid"]);
		list.AddRange(slotsBySide["top"]);
		if (num)
		{
			list.AddRange(slotsBySide["left"]);
		}
		UsableSlots = list.ToArray();
		Cuboidf[] slotsHitBoxes = ((BlockScrollRack)base.Block).slotsHitBoxes;
		UsableSelectionBoxes = new Cuboidf[slotsHitBoxes.Length];
		for (int i = 0; i < slotsHitBoxes.Length; i++)
		{
			UsableSelectionBoxes[i] = slotsHitBoxes[i].RotatedCopy(0f, MeshAngleRad * (180f / (float)Math.PI), 0f, new Vec3d(0.5, 0.5, 0.5));
		}
	}

	private bool isRack(Vec3i offset)
	{
		BEBehaviorShapeMaterialFromAttributes bEBehaviorShapeMaterialFromAttributes = Api.World.BlockAccessor.GetBlockEntity<BlockEntityScrollRack>(Pos.AddCopy(offset))?.GetBehavior<BEBehaviorShapeMaterialFromAttributes>();
		if (bEBehaviorShapeMaterialFromAttributes != null)
		{
			return bEBehaviorShapeMaterialFromAttributes.MeshAngleY == MeshAngleRad;
		}
		return false;
	}

	public override void Initialize(ICoreAPI api)
	{
		bh = GetBehavior<BEBehaviorShapeMaterialFromAttributes>();
		base.Initialize(api);
	}

	internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		getOrCreateUsableSlots();
		BlockScrollRack obj = (BlockScrollRack)base.Block;
		string[] slotSide = obj.slotSide;
		int[] oppositeSlotIndex = obj.oppositeSlotIndex;
		string text = slotSide[blockSel.SelectionBoxIndex];
		if (text == "bot" || text == "right")
		{
			BlockPos position = ((text == "bot") ? Pos.DownCopy() : Pos.AddCopy(BEBehaviorDoor.getAdjacentOffset(1, 0, 0, MeshAngleRad, invertHandles: false)));
			BlockEntityScrollRack blockEntity = Api.World.BlockAccessor.GetBlockEntity<BlockEntityScrollRack>(position);
			if (blockEntity == null)
			{
				return false;
			}
			float num = GameMath.NormaliseAngleRad(blockEntity.MeshAngleRad);
			float num2 = GameMath.NormaliseAngleRad(MeshAngleRad);
			if (num % (float)Math.PI == num2 % (float)Math.PI)
			{
				if (num != num2 && text == "right")
				{
					return false;
				}
				BlockSelection blockSelection = blockSel.Clone();
				blockSelection.SelectionBoxIndex = oppositeSlotIndex[(num == num2) ? blockSelection.SelectionBoxIndex : (blockSelection.SelectionBoxIndex ^ 1)];
				return blockEntity.OnInteract(byPlayer, blockSelection);
			}
			return false;
		}
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		CollectibleObject collectibleObject = activeHotbarSlot.Itemstack?.Collectible;
		bool flag = collectibleObject?.Attributes != null && collectibleObject.Attributes["scrollrackable"].AsBool();
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
				Api.World.Logger.Audit("{0} Put 1x{1} into Scroll rack at {2}.", byPlayer.PlayerName, assetLocation2, Pos);
				return true;
			}
		}
		return false;
	}

	private bool TryPut(ItemSlot slot, BlockSelection blockSel)
	{
		int selectionBoxIndex = blockSel.SelectionBoxIndex;
		if (selectionBoxIndex < 0 || selectionBoxIndex >= Inventory.Count)
		{
			return false;
		}
		if (!UsableSlots.Contains(selectionBoxIndex))
		{
			return false;
		}
		int slotId = selectionBoxIndex;
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
		int selectionBoxIndex = blockSel.SelectionBoxIndex;
		if (selectionBoxIndex < 0 || selectionBoxIndex >= Inventory.Count)
		{
			return false;
		}
		int slotId = selectionBoxIndex;
		if (!Inventory[slotId].Empty)
		{
			ItemStack itemStack = Inventory[slotId].TakeOut(1);
			if (byPlayer.InventoryManager.TryGiveItemstack(itemStack))
			{
				AssetLocation assetLocation = itemStack.Block?.Sounds?.Place;
				Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
			}
			if (itemStack.StackSize > 0)
			{
				Api.World.SpawnItemEntity(itemStack, Pos);
			}
			Api.World.Logger.Audit("{0} Took 1x{1} from Scroll rack at {2}.", byPlayer.PlayerName, itemStack.Collectible.Code, Pos);
			MarkDirty();
			return true;
		}
		return false;
	}

	protected override float[][] genTransformationMatrices()
	{
		tfMatrices = new float[Inventory.Count][];
		Cuboidf[] slotsHitBoxes = ((BlockScrollRack)base.Block).slotsHitBoxes;
		for (int i = 0; i < Inventory.Count; i++)
		{
			Cuboidf obj = slotsHitBoxes[i];
			float midX = obj.MidX;
			float midY = obj.MidY;
			float midZ = obj.MidZ;
			Vec3f vec3f = new Vec3f(midX, midY, midZ);
			vec3f = new Matrixf().RotateY(MeshAngleRad).TransformVector(vec3f.ToVec4f(0f)).XYZ;
			tfMatrices[i] = new Matrixf().Translate(vec3f.X, vec3f.Y, vec3f.Z).Translate(0.5f, 0f, 0.5f).RotateY(MeshAngleRad - (float)Math.PI / 2f)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return tfMatrices;
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("usableSlotsDirty", UsableSlots == null);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		if (tree.GetBool("usableSlotsDirty"))
		{
			UsableSlots = null;
		}
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
		int selectionBoxIndex = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
		if (selectionBoxIndex < 0 || selectionBoxIndex >= Inventory.Count)
		{
			base.GetBlockInfo(forPlayer, sb);
			return;
		}
		ItemSlot itemSlot = Inventory[selectionBoxIndex];
		if (itemSlot.Empty)
		{
			string text = ((BlockScrollRack)base.Block).slotSide[selectionBoxIndex];
			if (text == "bot")
			{
				BlockEntityScrollRack blockEntity = Api.World.BlockAccessor.GetBlockEntity<BlockEntityScrollRack>(Pos.DownCopy());
				if (blockEntity != null)
				{
					float num = GameMath.NormaliseAngleRad(blockEntity.MeshAngleRad);
					float num2 = GameMath.NormaliseAngleRad(MeshAngleRad);
					if (num % (float)Math.PI == num2 % (float)Math.PI)
					{
						itemSlot = blockEntity.inv[(num == num2) ? (selectionBoxIndex + 10) : (11 - selectionBoxIndex)];
					}
				}
			}
			else if (text == "right")
			{
				BlockEntityScrollRack blockEntity2 = Api.World.BlockAccessor.GetBlockEntity<BlockEntityScrollRack>(Pos.AddCopy(BEBehaviorDoor.getAdjacentOffset(1, 0, 0, MeshAngleRad, invertHandles: false)));
				if (blockEntity2 != null)
				{
					float num3 = GameMath.NormaliseAngleRad(blockEntity2.MeshAngleRad);
					float num4 = GameMath.NormaliseAngleRad(MeshAngleRad);
					if (num3 == num4)
					{
						itemSlot = blockEntity2.Inventory[selectionBoxIndex - 2];
					}
				}
			}
			sb.AppendLine(itemSlot.Empty ? Lang.Get("Empty") : itemSlot.Itemstack.GetName());
		}
		else
		{
			sb.AppendLine(itemSlot.Itemstack.GetName());
		}
	}

	internal void clearUsableSlots()
	{
		genUsableSlots();
		for (int i = 0; i < Inventory.Count; i++)
		{
			if (!UsableSlots.Contains(i))
			{
				ItemSlot itemSlot = Inventory[i];
				if (!itemSlot.Empty)
				{
					Vec3d vec3d = Pos.ToVec3d();
					vec3d.Add(0.5 - (double)GameMath.Cos(MeshAngleRad) * 0.6, 0.15, 0.5 + (double)GameMath.Sin(MeshAngleRad) * 0.6);
					Api.World.SpawnItemEntity(itemSlot.Itemstack, vec3d);
					itemSlot.Itemstack = null;
				}
			}
		}
		MarkDirty(redrawOnClient: true);
	}
}
