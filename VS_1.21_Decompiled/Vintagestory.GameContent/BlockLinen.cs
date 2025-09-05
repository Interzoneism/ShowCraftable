using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class BlockLinen : BlockSimpleCoating
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (api.World.BlockAccessor.GetBlockEntity(blockSel.Position.AddCopy(blockSel.Face.Opposite)) is BlockEntityBarrel)
		{
			return false;
		}
		return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel != null)
		{
			BlockEntityBarrel blockEntityBarrel = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBarrel;
			ItemSlot itemSlot = blockEntityBarrel?.Inventory[1];
			if (blockEntityBarrel != null && !itemSlot.Empty && itemSlot.Itemstack.Item?.Code?.Path == "cottagecheeseportion")
			{
				float num = BlockLiquidContainerBase.GetContainableProps(itemSlot.Itemstack)?.ItemsPerLitre ?? 1f;
				if ((float)itemSlot.Itemstack.StackSize / num < 25f)
				{
					(api as ICoreClientAPI)?.TriggerIngameError(this, "notenough", Lang.Get("Need at least 25 litres to create a roll of cheese"));
					handHandling = EnumHandHandling.PreventDefault;
					return;
				}
				if (api.World.Side == EnumAppSide.Server)
				{
					ItemStack contents = blockEntityBarrel.Inventory[1].TakeOut((int)(25f * num));
					BlockCheeseCurdsBundle obj = api.World.GetBlock(new AssetLocation("curdbundle")) as BlockCheeseCurdsBundle;
					ItemStack itemStack = new ItemStack(obj);
					obj.SetContents(itemStack, contents);
					slot.TakeOut(1);
					slot.MarkDirty();
					blockEntityBarrel.MarkDirty(redrawOnClient: true);
					if (!byEntity.TryGiveItemStack(itemStack))
					{
						api.World.SpawnItemEntity(itemStack, byEntity.Pos.XYZ.AddCopy(0.0, 0.5, 0.0));
					}
					api.World.Logger.Audit("{0} Took 1x{1} from Barrel at {2}.", byEntity.GetName(), itemStack.Collectible.Code, blockSel.Position);
				}
				handHandling = EnumHandHandling.PreventDefault;
				return;
			}
		}
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
	}
}
