using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementStaticText : GuiElementTextBase
{
	internal EnumTextOrientation orientation;

	public double offsetX;

	public double offsetY;

	public GuiElementStaticText(ICoreClientAPI capi, string text, EnumTextOrientation orientation, ElementBounds bounds, CairoFont font)
		: base(capi, text, font, bounds)
	{
		this.orientation = orientation;
	}

	public double GetTextHeight()
	{
		return textUtil.GetMultilineTextHeight(Font, text, Bounds.InnerWidth);
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		Bounds.absInnerHeight = textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, text, (int)(offsetX + Bounds.drawX), (int)(offsetY + Bounds.drawY), Bounds.InnerWidth, orientation);
	}

	public void AutoBoxSize(bool onlyGrow = false)
	{
		Font.AutoBoxSize(text, Bounds, onlyGrow);
	}

	public void SetValue(string text)
	{
		base.text = text;
	}

	public void AutoFontSize(bool onlyShrink = true)
	{
		Bounds.CalcWorldBounds();
		Font.AutoFontSize(text, Bounds, onlyShrink);
	}
}
