using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ItemPressedMash : Item
{
	public override string GetHeldItemName(ItemStack itemStack)
	{
		string text = (((float)Math.Round(itemStack.Attributes.GetDecimal("juiceableLitresLeft"), 2) > 0f) ? "wet" : "dry");
		string text2 = ItemClass.Name();
		return Lang.GetMatching(Code?.Domain + ":" + text2 + "-" + Code?.Path + "-" + text);
	}

	public override List<ItemStack> GetHandBookStacks(ICoreClientAPI capi)
	{
		List<ItemStack> handBookStacks = base.GetHandBookStacks(capi);
		if (handBookStacks != null)
		{
			foreach (ItemStack item in handBookStacks)
			{
				JuiceableProperties obj = item?.ItemAttributes?["juiceableProperties"]?.AsObject<JuiceableProperties>(null, item.Collectible.Code.Domain);
				obj?.LiquidStack?.Resolve(api.World, "juiceable properties liquidstack");
				obj?.PressedStack?.Resolve(api.World, "juiceable properties pressedstack");
				obj?.ReturnStack?.Resolve(api.World, "juiceable properties returnstack");
				if (obj?.ReturnStack?.ResolvedItemstack != null)
				{
					item.Attributes.SetDouble("juiceableLitresLeft", 1.0);
				}
			}
		}
		return handBookStacks;
	}

	public override ItemStack OnTransitionNow(ItemSlot slot, TransitionableProperties props)
	{
		float num = slot.Itemstack.ItemAttributes["juiceableProperties"]["pressedDryRatio"].AsFloat(1f);
		double num2 = slot.Itemstack.Attributes.GetDouble("juiceableLitresLeft") + slot.Itemstack.Attributes.GetDouble("juiceableLitresTransfered");
		TransitionableProperties transitionableProperties = props.Clone();
		if (num2 > 0.0)
		{
			transitionableProperties.TransitionRatio = props.TransitionRatio * (float)(int)((float)GameMath.RoundRandom(api.World.Rand, (float)num2) * num);
		}
		return base.OnTransitionNow(slot, transitionableProperties);
	}
}
