using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class ItemNugget : Item
{
	public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
	{
		ItemSlot itemSlot = allInputslots.FirstOrDefault((ItemSlot slot) => slot.Itemstack?.Collectible is ItemOre);
		if (itemSlot != null)
		{
			int num = itemSlot.Itemstack.ItemAttributes["metalUnits"].AsInt(5);
			string text = itemSlot.Itemstack.Collectible.Variant["ore"].Replace("quartz_", "").Replace("galena_", "");
			ItemStack itemStack = new ItemStack(api.World.GetItem(new AssetLocation("nugget-" + text)));
			itemStack.StackSize = Math.Max(1, num / 5);
			outputSlot.Itemstack = itemStack;
		}
		base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		if (CombustibleProps?.SmeltedStack == null)
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			return;
		}
		_ = CombustibleProps;
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string text = CombustibleProps.SmeltingType.ToString().ToLowerInvariant();
		int smeltedRatio = CombustibleProps.SmeltedRatio;
		float num = (float)CombustibleProps.SmeltedStack.ResolvedItemstack.StackSize * 100f / (float)smeltedRatio;
		string text2 = CombustibleProps.SmeltedStack.ResolvedItemstack.Collectible?.Variant?["metal"];
		string text3 = Lang.Get("material-" + text2);
		if (text2 == null)
		{
			text3 = CombustibleProps.SmeltedStack.ResolvedItemstack.GetName();
		}
		string value = Lang.Get("game:smeltdesc-" + text + "ore-plural", num.ToString("0.#"), text3);
		dsc.AppendLine(value);
	}
}
