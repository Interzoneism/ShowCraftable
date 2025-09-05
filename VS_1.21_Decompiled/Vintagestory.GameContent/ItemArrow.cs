using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class ItemArrow : Item
{
	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		if (inSlot.Itemstack.Collectible.Attributes != null)
		{
			float num = inSlot.Itemstack.Collectible.Attributes["damage"].AsFloat();
			if (num != 0f)
			{
				dsc.AppendLine(Lang.Get("arrow-piercingdamage", ((num > 0f) ? "+" : "") + num));
			}
			float num2 = inSlot.Itemstack.Collectible.Attributes["breakChanceOnImpact"].AsFloat(0.5f);
			dsc.AppendLine(Lang.Get("breakchanceonimpact", (int)(num2 * 100f)));
		}
	}
}
