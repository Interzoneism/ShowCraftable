using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class IconComponent : RichTextComponentBase
{
	protected ICoreClientAPI capi;

	protected ElementBounds parentBounds;

	public double offY;

	public double sizeMulSvg = 0.7;

	protected string iconName;

	protected string iconPath;

	protected CairoFont font;

	public IconComponent(ICoreClientAPI capi, string iconName, string iconPath, CairoFont font)
		: base(capi)
	{
		this.capi = capi;
		this.iconName = iconName;
		this.iconPath = iconPath;
		this.font = font;
		BoundsPerLine = new LineRectangled[1]
		{
			new LineRectangled(0.0, 0.0, GuiElement.scaled(font.UnscaledFontsize), GuiElement.scaled(font.UnscaledFontsize))
		};
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		double num = GuiElement.scaled(font.UnscaledFontsize);
		IAsset asset = null;
		if (iconPath != null)
		{
			asset = capi.Assets.TryGet(new AssetLocation(iconPath).WithPathPrefixOnce("textures/"));
		}
		if (asset != null)
		{
			num *= sizeMulSvg;
			FontExtents fontExtents = font.GetFontExtents();
			double ascent = ((FontExtents)(ref fontExtents)).Ascent;
			capi.Gui.DrawSvg(asset, surface, (int)BoundsPerLine[0].X, (int)(BoundsPerLine[0].Y + ascent - (double)(int)num) + 2, (int)num, (int)num, (int?)ColorUtil.ColorFromRgba(font.Color));
		}
		else
		{
			capi.Gui.Icons.DrawIcon(ctx, iconName, BoundsPerLine[0].X, BoundsPerLine[0].Y, num, num, font.Color);
		}
	}

	public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		TextFlowPath currentFlowPathSection = GetCurrentFlowPathSection(flowPath, lineY);
		offsetX += GuiElement.scaled(PaddingLeft);
		bool flag = offsetX + BoundsPerLine[0].Width > currentFlowPathSection.X2;
		BoundsPerLine[0].X = (flag ? 0.0 : offsetX);
		BoundsPerLine[0].Y = lineY + (flag ? currentLineHeight : 0.0);
		nextOffsetX = (flag ? 0.0 : offsetX) + BoundsPerLine[0].Width;
		if (!flag)
		{
			return EnumCalcBoundsResult.Continue;
		}
		return EnumCalcBoundsResult.Nextline;
	}

	public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
	}

	public override void Dispose()
	{
		base.Dispose();
	}
}
