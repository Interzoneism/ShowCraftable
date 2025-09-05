using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class ItemTongs : Item, IHeldHandAnimOverrider
{
	public bool AllowHeldIdleHandAnim(Entity forEntity, ItemSlot slot, EnumHand hand)
	{
		return !isHoldingHotItem(forEntity);
	}

	public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
	{
		if (isHoldingHotItem(forEntity))
		{
			return "holdbothhands-tongs1";
		}
		return base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand);
	}

	private static bool isHoldingHotItem(Entity forEntity)
	{
		if (forEntity is EntityPlayer entityPlayer && !entityPlayer.RightHandItemSlot.Empty)
		{
			ItemStack itemstack = entityPlayer.RightHandItemSlot.Itemstack;
			if (itemstack.Collectible.GetTemperature(forEntity.World, itemstack) > 200f)
			{
				return true;
			}
		}
		return false;
	}
}
