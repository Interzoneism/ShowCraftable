using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityNameTagRendererRegistry : ModSystem
{
	public class DefaultEntitlementTagRenderer
	{
		public double[] color;

		public TextBackground background;

		public LoadedTexture renderTag(ICoreClientAPI capi, Entity entity)
		{
			string text = entity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName;
			if (text != null && text.Length > 0)
			{
				return capi.Gui.TextTexture.GenUnscaledTextTexture(text, CairoFont.WhiteMediumText().WithColor(color), background);
			}
			return null;
		}
	}

	public static NameTagRendererDelegate DefaultNameTagRenderer = delegate(ICoreClientAPI capi, Entity entity)
	{
		string text = entity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName;
		return (text != null && text.Length > 0) ? capi.Gui.TextTexture.GenUnscaledTextTexture(Lang.GetIfExists("nametag-" + text.ToLowerInvariant()) ?? text, CairoFont.WhiteMediumText().WithColor(ColorUtil.WhiteArgbDouble), new TextBackground
		{
			FillColor = GuiStyle.DialogLightBgColor,
			Padding = 3,
			Radius = GuiStyle.ElementBGRadius,
			Shade = true,
			BorderColor = GuiStyle.DialogBorderColor,
			BorderWidth = 3.0
		}) : null;
	};

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
	}

	internal NameTagRendererDelegate GetNameTagRenderer(Entity entity)
	{
		List<Entitlement> list = (entity as EntityPlayer)?.Player?.Entitlements;
		if (list != null && list.Count > 0)
		{
			Entitlement entitlement = list[0];
			if (GlobalConstants.playerColorByEntitlement.TryGetValue(entitlement.Code, out var value))
			{
				GlobalConstants.playerTagBackgroundByEntitlement.TryGetValue(entitlement.Code, out var value2);
				return new DefaultEntitlementTagRenderer
				{
					color = value,
					background = value2
				}.renderTag;
			}
		}
		return DefaultNameTagRenderer;
	}
}
