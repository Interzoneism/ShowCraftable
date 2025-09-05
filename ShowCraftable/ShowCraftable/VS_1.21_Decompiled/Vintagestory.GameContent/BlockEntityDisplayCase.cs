using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityDisplayCase : BlockEntityDisplay, IRotatable
{
	protected InventoryGeneric inventory;

	private bool haveCenterPlacement;

	private float[] rotations = new float[4];

	public override string InventoryClassName => "displaycase";

	public override InventoryBase Inventory => inventory;

	public BlockEntityDisplayCase()
	{
		inventory = new InventoryDisplayed(this, 4, "displaycase-0", null);
	}

	internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot.Empty)
		{
			if (TryTake(byPlayer, blockSel))
			{
				return true;
			}
			return false;
		}
		CollectibleObject collectible = activeHotbarSlot.Itemstack.Collectible;
		if (collectible.Attributes != null && collectible.Attributes["displaycaseable"].AsBool())
		{
			AssetLocation assetLocation = activeHotbarSlot.Itemstack?.Block?.Sounds?.Place;
			if (TryPut(activeHotbarSlot, blockSel, byPlayer))
			{
				Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				int selectionBoxIndex = blockSel.SelectionBoxIndex;
				Api.World.Logger.Audit("{0} Put 1x{1} into DisplayCase slotid {2} at {3}.", byPlayer.PlayerName, inventory[selectionBoxIndex].Itemstack?.Collectible.Code, selectionBoxIndex, Pos);
				return true;
			}
			return false;
		}
		(Api as ICoreClientAPI)?.TriggerIngameError(this, "doesnotfit", Lang.Get("This item does not fit into the display case."));
		return true;
	}

	private bool TryPut(ItemSlot slot, BlockSelection blockSel, IPlayer player)
	{
		int selectionBoxIndex = blockSel.SelectionBoxIndex;
		bool flag = inventory.Empty && Math.Abs(blockSel.HitPosition.X - 0.5) < 0.1 && Math.Abs(blockSel.HitPosition.Z - 0.5) < 0.1;
		if ((slot.Itemstack.ItemAttributes?["displaycase"]["minHeight"]?.AsFloat(0.25f)).GetValueOrDefault() > (base.Block as BlockDisplayCase)?.height)
		{
			(Api as ICoreClientAPI)?.TriggerIngameError(this, "tootall", Lang.Get("This item is too tall to fit in this display case."));
			return false;
		}
		haveCenterPlacement = flag;
		if (inventory[selectionBoxIndex].Empty)
		{
			int num = slot.TryPutInto(Api.World, inventory[selectionBoxIndex]);
			if (num > 0)
			{
				BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
				double y = player.Entity.Pos.X - ((double)blockPos.X + blockSel.HitPosition.X);
				double x = (double)(float)player.Entity.Pos.Z - ((double)blockPos.Z + blockSel.HitPosition.Z);
				float num2 = (float)Math.Atan2(y, x);
				float num3 = (float)Math.PI / 2f;
				rotations[selectionBoxIndex] = (float)(int)Math.Round(num2 / num3) * num3;
				MarkDirty();
			}
			return num > 0;
		}
		return false;
	}

	private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
	{
		int num = blockSel.SelectionBoxIndex;
		if (haveCenterPlacement)
		{
			for (int i = 0; i < inventory.Count; i++)
			{
				if (!inventory[i].Empty)
				{
					num = i;
				}
			}
		}
		if (!inventory[num].Empty)
		{
			ItemStack itemStack = inventory[num].TakeOut(1);
			if (byPlayer.InventoryManager.TryGiveItemstack(itemStack))
			{
				AssetLocation assetLocation = itemStack.Block?.Sounds?.Place;
				Api.World.PlaySoundAt((assetLocation != null) ? assetLocation : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
				Api.World.Logger.Audit("{0} Took 1x{1} from DisplayCase slotid {2} at {3}.", byPlayer.PlayerName, itemStack.Collectible.Code, num, Pos);
			}
			if (itemStack.StackSize > 0)
			{
				Api.World.SpawnItemEntity(itemStack, Pos);
			}
			updateMesh(num);
			MarkDirty(redrawOnClient: true);
			return true;
		}
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
		sb.AppendLine();
		if (forPlayer?.CurrentBlockSelection != null)
		{
			int selectionBoxIndex = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
			if (selectionBoxIndex < inventory.Count && !inventory[selectionBoxIndex].Empty)
			{
				sb.AppendLine(inventory[selectionBoxIndex].Itemstack.GetName());
			}
		}
	}

	protected override float[][] genTransformationMatrices()
	{
		float[][] array = new float[4][];
		for (int i = 0; i < 4; i++)
		{
			float num = ((i % 2 == 0) ? 0.3125f : 0.6875f);
			float y = 0.063125f;
			float num2 = ((i > 1) ? 0.6875f : 0.3125f);
			int num3 = GameMath.MurmurHash3Mod(Pos.X, Pos.Y + i * 50, Pos.Z, 30) - 15;
			JsonObject jsonObject = inventory[i]?.Itemstack?.Collectible?.Attributes;
			if (jsonObject != null && !jsonObject["randomizeInDisplayCase"].AsBool(defaultValue: true))
			{
				num3 = 0;
			}
			float degY = rotations[i] * (180f / (float)Math.PI) + 45f + (float)num3;
			if (haveCenterPlacement)
			{
				num = 0.5f;
				num2 = 0.5f;
			}
			array[i] = new Matrixf().Translate(0.5f, 0f, 0.5f).Translate(num - 0.5f, y, num2 - 0.5f).RotateYDeg(degY)
				.Scale(0.75f, 0.75f, 0.75f)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return array;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		haveCenterPlacement = tree.GetBool("haveCenterPlacement");
		rotations = new float[4]
		{
			tree.GetFloat("rotation0"),
			tree.GetFloat("rotation1"),
			tree.GetFloat("rotation2"),
			tree.GetFloat("rotation3")
		};
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("haveCenterPlacement", haveCenterPlacement);
		tree.SetFloat("rotation0", rotations[0]);
		tree.SetFloat("rotation1", rotations[1]);
		tree.SetFloat("rotation2", rotations[2]);
		tree.SetFloat("rotation3", rotations[3]);
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		int[] array = new int[4] { 0, 1, 3, 2 };
		float[] array2 = new float[4];
		ITreeAttribute treeAttribute = tree.GetTreeAttribute("inventory");
		inventory.FromTreeAttributes(treeAttribute);
		ItemSlot[] array3 = new ItemSlot[4];
		int num = degreeRotation / 90 % 4;
		for (int i = 0; i < 4; i++)
		{
			array2[i] = tree.GetFloat("rotation" + i);
			array3[i] = inventory[i];
		}
		for (int j = 0; j < 4; j++)
		{
			int num2 = GameMath.Mod(j - num, 4);
			rotations[array[j]] = array2[array[num2]] - (float)degreeRotation * ((float)Math.PI / 180f);
			inventory[array[j]] = array3[array[num2]];
			tree.SetFloat("rotation" + array[j], rotations[array[j]]);
		}
		inventory.ToTreeAttributes(treeAttribute);
		tree["inventory"] = treeAttribute;
	}
}
