using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

internal class GuiElementEngravedText : GuiElementTextBase
{
	private EnumTextOrientation orientation;

	public GuiElementEngravedText(ICoreClientAPI capi, string text, CairoFont font, ElementBounds bounds, EnumTextOrientation orientation = EnumTextOrientation.Left)
		: base(capi, text, font, bounds)
	{
		this.orientation = orientation;
	}

	public override void ComposeTextElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Expected O, but got Unknown
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Expected O, but got Unknown
		Font.SetupContext(ctxStatic);
		Bounds.CalcWorldBounds();
		ImageSurface val = new ImageSurface((Format)0, Bounds.ParentBounds.OuterWidthInt, Bounds.ParentBounds.OuterHeightInt);
		Context val2 = new Context((Surface)(object)val);
		val2.SetSourceRGB(0.0, 0.0, 0.0);
		val2.Paint();
		Font.Color = new double[4] { 20.0, 20.0, 20.0, 0.3499999940395355 };
		Font.SetupContext(val2);
		DrawMultilineTextAt(val2, Bounds.drawX + GuiElement.scaled(2.0), Bounds.drawY + GuiElement.scaled(2.0), orientation);
		SurfaceTransformBlur.BlurFull(val, 7.0);
		ImageSurface val3 = new ImageSurface((Format)0, Bounds.ParentBounds.OuterWidthInt, Bounds.ParentBounds.OuterHeightInt);
		Context val4 = new Context((Surface)(object)val3);
		val4.Operator = (Operator)1;
		val4.Antialias = (Antialias)6;
		Font.Color = new double[4] { 0.0, 0.0, 0.0, 0.4 };
		Font.SetupContext(val4);
		val4.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
		DrawMultilineTextAt(val4, Bounds.drawX - GuiElement.scaled(0.5), Bounds.drawY - GuiElement.scaled(0.5), orientation);
		val4.SetSourceRGBA(1.0, 1.0, 1.0, 1.0);
		DrawMultilineTextAt(val4, Bounds.drawX + GuiElement.scaled(1.0), Bounds.drawY + GuiElement.scaled(1.0), orientation);
		val4.Operator = (Operator)5;
		val4.SetSourceSurface((Surface)(object)val, 0, 0);
		val4.Paint();
		val2.Dispose();
		((Surface)val).Dispose();
		val4.Operator = (Operator)2;
		Font.Color = new double[4] { 0.0, 0.0, 0.0, 0.35 };
		Font.SetupContext(val4);
		DrawMultilineTextAt(val4, (int)Bounds.drawX, (int)Bounds.drawY, orientation);
		ctxStatic.Antialias = (Antialias)6;
		ctxStatic.Operator = (Operator)21;
		ctxStatic.SetSourceSurface((Surface)(object)val3, 0, 0);
		ctxStatic.Paint();
		((Surface)val3).Dispose();
		val4.Dispose();
	}

	internal void TextWithSpacing(Context ctx, string text, double x, double y, float spacing)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			TextExtents val = ctx.TextExtents(c.ToString() ?? "");
			ctx.MoveTo(x - ((TextExtents)(ref val)).XBearing, x - ((TextExtents)(ref val)).YBearing);
			ctx.ShowText(c.ToString() ?? "");
			x += ((TextExtents)(ref val)).Width + (double)(spacing * RuntimeEnv.GUIScale);
		}
	}
}
