using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementTextBase : GuiElementControl
{
	public TextDrawUtil textUtil;

	protected string text;

	public bool textPathMode;

	public CairoFont Font;

	protected float RightPadding;

	public string Text
	{
		get
		{
			return text;
		}
		set
		{
			text = value;
		}
	}

	public GuiElementTextBase(ICoreClientAPI capi, string text, CairoFont font, ElementBounds bounds)
		: base(capi, bounds)
	{
		Font = font;
		textUtil = new TextDrawUtil();
		this.text = text;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		Font.SetupContext(ctx);
		Bounds.CalcWorldBounds();
		ComposeTextElements(ctx, surface);
	}

	public virtual void ComposeTextElements(Context ctx, ImageSurface surface)
	{
	}

	public double GetMultilineTextHeight()
	{
		return textUtil.GetMultilineTextHeight(Font, text, Bounds.InnerWidth - (double)RightPadding);
	}

	public double DrawMultilineTextAt(Context ctx, double posX, double posY, EnumTextOrientation orientation = EnumTextOrientation.Left)
	{
		Font.SetupContext(ctx);
		TextLine[] array = textUtil.Lineize(Font, text, Bounds.InnerWidth - (double)RightPadding);
		ctx.Save();
		Matrix matrix = ctx.Matrix;
		matrix.Translate(posX, posY);
		ctx.Matrix = matrix;
		textUtil.DrawMultilineText(ctx, Font, array, orientation);
		ctx.Restore();
		if (array.Length != 0)
		{
			return array[^1].Bounds.Y + array[^1].Bounds.Height;
		}
		return 0.0;
	}

	public void DrawTextLineAt(Context ctx, string text, double posX, double posY, bool textPathMode = false)
	{
		textUtil.DrawTextLine(ctx, Font, text, posX, posY, textPathMode);
	}

	public virtual string GetText()
	{
		return text;
	}

	internal virtual void setFont(CairoFont font)
	{
		Font = font;
	}
}
