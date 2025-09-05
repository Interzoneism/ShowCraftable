using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class ItemOre : ItemPileable
{
	public bool IsCoal
	{
		get
		{
			if (!(Variant["ore"] == "lignite") && !(Variant["ore"] == "bituminouscoal"))
			{
				return Variant["ore"] == "anthracite";
			}
			return true;
		}
	}

	public override bool IsPileable => IsCoal;

	protected override AssetLocation PileBlockCode => new AssetLocation("coalpile");

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		if (CombustibleProps?.SmeltedStack?.ResolvedItemstack == null)
		{
			JsonObject attributes = Attributes;
			if (attributes != null && attributes["metalUnits"].Exists)
			{
				float num = Attributes["metalUnits"].AsInt();
				string text = LastCodePart(1);
				if (text.Contains("_"))
				{
					text = text.Split('_')[1];
				}
				AssetLocation itemCode = new AssetLocation("nugget-" + text);
				Item item = api.World.GetItem(itemCode);
				if (item?.CombustibleProps?.SmeltedStack?.ResolvedItemstack != null)
				{
					string text2 = item.CombustibleProps.SmeltingType.ToString().ToLowerInvariant();
					string text3 = item.CombustibleProps.SmeltedStack.ResolvedItemstack.Collectible?.Variant?["metal"];
					string text4 = Lang.Get("material-" + text3);
					if (text3 == null)
					{
						text4 = item.CombustibleProps.SmeltedStack.ResolvedItemstack.GetName();
					}
					dsc.AppendLine(Lang.Get("game:smeltdesc-" + text2 + "ore-plural", num.ToString("0.#"), text4));
				}
				dsc.AppendLine(Lang.Get("Parent Material: {0}", Lang.Get("rock-" + LastCodePart())));
				dsc.AppendLine();
				dsc.AppendLine(Lang.Get("Crush with hammer to extract nuggets"));
			}
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			return;
		}
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		if (CombustibleProps.SmeltedStack.ResolvedItemstack.Collectible.FirstCodePart() == "ingot")
		{
			string text5 = CombustibleProps.SmeltingType.ToString().ToLowerInvariant();
			int smeltedRatio = CombustibleProps.SmeltedRatio;
			float num2 = (float)CombustibleProps.SmeltedStack.ResolvedItemstack.StackSize * 100f / (float)smeltedRatio;
			string text6 = CombustibleProps.SmeltedStack.ResolvedItemstack.Collectible?.Variant?["metal"];
			string text7 = Lang.Get("material-" + text6);
			if (text6 == null)
			{
				text7 = CombustibleProps.SmeltedStack.ResolvedItemstack.GetName();
			}
			string value = Lang.Get("game:smeltdesc-" + text5 + "ore-plural", num2.ToString("0.#"), text7);
			dsc.AppendLine(value);
		}
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		JsonObject attributes = Attributes;
		if (attributes != null && attributes["metalUnits"].Exists)
		{
			string text = LastCodePart(1);
			LastCodePart();
			if (FirstCodePart() == "crystalizedore")
			{
				return Lang.Get(LastCodePart(2) + "-crystallizedore-chunk", Lang.Get("ore-" + text));
			}
			return Lang.Get(LastCodePart(2) + "-ore-chunk", Lang.Get("ore-" + text));
		}
		return base.GetHeldItemName(itemStack);
	}
}
