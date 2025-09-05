using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class GuiDialogHairStyling : GuiDialogCreateCharacter
{
	public Dictionary<string, int> hairStylingCost;

	private Dictionary<string, string> currentSkin = new Dictionary<string, string>();

	private GuiComposer chcomposer;

	private long entityId;

	protected override bool AllowClassSelection => false;

	protected override bool AllowKeepCurrent => true;

	protected override bool AllowedSkinPartSelection(string code)
	{
		switch (code)
		{
		default:
			return code == "beard";
		case "hairbase":
		case "hairextra":
		case "mustache":
			return true;
		}
	}

	public GuiDialogHairStyling(ICoreClientAPI capi, long entityId, string[] categorycodes, Dictionary<string, int> dictionary)
		: base(capi, null)
	{
		variantCategories = categorycodes;
		hairStylingCost = dictionary;
		this.entityId = entityId;
		currentSkin = getCurrentSkin();
		onBeforeCompose = delegate(GuiComposer composer)
		{
			chcomposer = composer;
			ElementBounds bounds = ElementBounds.Fixed(0, dlgHeight - 25).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(12.0, 6.0);
			ElementBounds bounds2 = ElementBounds.Fixed(0.0, dlgHeight - 55, 130.0, 30.0).WithAlignment(EnumDialogArea.RightFixed);
			composer.AddSmallButton(Lang.Get("Cancel"), TryClose, bounds);
			composer.AddRichtext("Cost: 0 gears", CairoFont.WhiteSmallText(), bounds2, "costline");
		};
	}

	private Dictionary<string, string> getCurrentSkin()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		EntityBehaviorExtraSkinnable behavior = capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
		SkinnablePart[] availableSkinParts = behavior.AvailableSkinParts;
		foreach (SkinnablePart skinnablePart in availableSkinParts)
		{
			if (AllowedSkinPartSelection(skinnablePart.Code))
			{
				string code = skinnablePart.Code;
				AppliedSkinnablePartVariant appliedSkinnablePartVariant = behavior.AppliedSkinParts.FirstOrDefault((AppliedSkinnablePartVariant sp) => sp.PartCode == code);
				dictionary[code] = appliedSkinnablePartVariant.Code;
			}
		}
		return dictionary;
	}

	protected override bool OnNext()
	{
		int playerAssets = InventoryTrader.GetPlayerAssets(capi.World.Player.Entity);
		if (getCost() > playerAssets)
		{
			capi.TriggerIngameError(this, "notenoughmoney", Lang.Get("Not enough money"));
			return false;
		}
		capi.Network.GetChannel("hairstyling").SendPacket(new PacketHairStyle
		{
			HairstylingNpcEntityId = entityId,
			Hairstyle = getCurrentSkin()
		});
		didSelect = true;
		TryClose();
		capi.Gui.PlaySound(new AssetLocation("sounds/effect/cashregister"), randomizePitch: false, 0.25f);
		return true;
	}

	public override void OnGuiClosed()
	{
		if (didSelect)
		{
			return;
		}
		EntityBehaviorExtraSkinnable behavior = capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
		foreach (KeyValuePair<string, string> item in currentSkin)
		{
			behavior.selectSkinPart(item.Key, item.Value);
		}
	}

	protected override void onToggleSkinPart(string partCode, string variantCode)
	{
		base.onToggleSkinPart(partCode, variantCode);
		chcomposer.GetRichtext("costline").SetNewText(Lang.Get("Cost: {0} gears", getCost()), CairoFont.WhiteSmallText());
	}

	protected override void onToggleSkinPart(string partCode, int index)
	{
		base.onToggleSkinPart(partCode, index);
		chcomposer.GetRichtext("costline").SetNewText(Lang.Get("Cost: {0} gears", getCost()), CairoFont.WhiteSmallText());
	}

	public int getCost()
	{
		int num = 0;
		EntityBehaviorExtraSkinnable behavior = capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
		SkinnablePart[] availableSkinParts = behavior.AvailableSkinParts;
		foreach (SkinnablePart skinnablePart in availableSkinParts)
		{
			if (AllowedSkinPartSelection(skinnablePart.Code))
			{
				string code = skinnablePart.Code;
				AppliedSkinnablePartVariant appliedSkinnablePartVariant = behavior.AppliedSkinParts.FirstOrDefault((AppliedSkinnablePartVariant sp) => sp.PartCode == code);
				if (currentSkin[code] != appliedSkinnablePartVariant.Code)
				{
					num += hairStylingCost[code];
				}
			}
		}
		return num;
	}
}
