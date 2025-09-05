using System;
using Cairo;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

public class LinkTextComponent : RichTextComponent
{
	private Action<LinkTextComponent> onLinkClicked;

	public string Href;

	private bool clickable = true;

	private LoadedTexture normalText;

	private LoadedTexture hoverText;

	private double leftMostX;

	private double topMostY;

	private bool isHover;

	private bool wasMouseDown;

	public bool Clickable
	{
		get
		{
			return clickable;
		}
		set
		{
			clickable = value;
			base.MouseOverCursor = (clickable ? "linkselect" : null);
		}
	}

	public LinkTextComponent(string href)
		: base(null, "", null)
	{
		Href = href;
	}

	public LinkTextComponent(ICoreClientAPI api, string displayText, CairoFont font, Action<LinkTextComponent> onLinkClicked)
		: base(api, displayText, font)
	{
		this.onLinkClicked = onLinkClicked;
		base.MouseOverCursor = "linkselect";
		Font = Font.Clone().WithColor(GuiStyle.ActiveButtonTextColor);
		hoverText = new LoadedTexture(api);
		normalText = new LoadedTexture(api);
	}

	public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		return base.CalcBounds(flowPath, currentLineHeight, offsetX, lineY, out nextOffsetX);
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Expected O, but got Unknown
		leftMostX = 999999.0;
		topMostY = 999999.0;
		double num = 0.0;
		double num2 = 0.0;
		for (int i = 0; i < Lines.Length; i++)
		{
			TextLine textLine = Lines[i];
			leftMostX = Math.Min(leftMostX, textLine.Bounds.X);
			topMostY = Math.Min(topMostY, textLine.Bounds.Y);
			num = Math.Max(num, textLine.Bounds.X + textLine.Bounds.Width);
			num2 = Math.Max(num2, textLine.Bounds.Y + textLine.Bounds.Height);
		}
		ImageSurface val = new ImageSurface((Format)0, (int)(num - leftMostX), (int)(num2 - topMostY));
		Context val2 = new Context((Surface)(object)val);
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		val2.Paint();
		val2.Save();
		Matrix matrix = val2.Matrix;
		matrix.Translate((double)(int)(0.0 - leftMostX), (double)(int)(0.0 - topMostY));
		val2.Matrix = matrix;
		CairoFont font = Font;
		ComposeFor(val2, val);
		api.Gui.LoadOrUpdateCairoTexture(val, linearMag: false, ref normalText);
		val2.Operator = (Operator)0;
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		val2.Paint();
		val2.Operator = (Operator)2;
		Font = Font.Clone();
		Font.Color[0] = Math.Min(1.0, Font.Color[0] * 1.2);
		Font.Color[1] = Math.Min(1.0, Font.Color[1] * 1.2);
		Font.Color[2] = Math.Min(1.0, Font.Color[2] * 1.2);
		ComposeFor(val2, val);
		Font = font;
		val2.Restore();
		api.Gui.LoadOrUpdateCairoTexture(val, linearMag: false, ref hoverText);
		((Surface)val).Dispose();
		val2.Dispose();
	}

	private void ComposeFor(Context ctx, ImageSurface surface)
	{
		textUtil.DrawMultilineText(ctx, Font, Lines);
		ctx.LineWidth = 1.0;
		ctx.SetSourceRGBA(Font.Color);
		for (int i = 0; i < Lines.Length; i++)
		{
			TextLine textLine = Lines[i];
			ctx.MoveTo(textLine.Bounds.X, textLine.Bounds.Y + textLine.Bounds.AscentOrHeight + 2.0);
			ctx.LineTo(textLine.Bounds.X + textLine.Bounds.Width, textLine.Bounds.Y + textLine.Bounds.AscentOrHeight + 2.0);
			ctx.Stroke();
		}
	}

	public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
		base.RenderInteractiveElements(deltaTime, renderX, renderY, renderZ);
		isHover = false;
		double fontOrientOffsetX = GetFontOrientOffsetX();
		if (clickable)
		{
			LineRectangled[] boundsPerLine = BoundsPerLine;
			for (int i = 0; i < boundsPerLine.Length; i++)
			{
				if (boundsPerLine[i].PointInside((double)api.Input.MouseX - renderX - fontOrientOffsetX, (double)api.Input.MouseY - renderY))
				{
					isHover = true;
					break;
				}
			}
		}
		api.Render.Render2DTexturePremultipliedAlpha(isHover ? hoverText.TextureId : normalText.TextureId, (int)(renderX + leftMostX + fontOrientOffsetX), (int)(renderY + topMostY), hoverText.Width, hoverText.Height, (float)renderZ + 50f);
	}

	public override bool UseMouseOverCursor(ElementBounds richtextBounds)
	{
		return isHover;
	}

	public override void OnMouseDown(MouseEvent args)
	{
		if (!clickable)
		{
			return;
		}
		double fontOrientOffsetX = GetFontOrientOffsetX();
		wasMouseDown = false;
		LineRectangled[] boundsPerLine = BoundsPerLine;
		for (int i = 0; i < boundsPerLine.Length; i++)
		{
			if (boundsPerLine[i].PointInside((double)args.X - fontOrientOffsetX, args.Y))
			{
				wasMouseDown = true;
			}
		}
	}

	public override void OnMouseUp(MouseEvent args)
	{
		if (!clickable || !wasMouseDown)
		{
			return;
		}
		double fontOrientOffsetX = GetFontOrientOffsetX();
		LineRectangled[] boundsPerLine = BoundsPerLine;
		for (int i = 0; i < boundsPerLine.Length; i++)
		{
			if (boundsPerLine[i].PointInside((double)args.X - fontOrientOffsetX, args.Y))
			{
				args.Handled = true;
				Trigger();
			}
		}
	}

	public LinkTextComponent SetHref(string href)
	{
		Href = href;
		return this;
	}

	public void Trigger()
	{
		if (onLinkClicked == null)
		{
			if (Href != null)
			{
				HandleLink();
			}
		}
		else
		{
			onLinkClicked(this);
		}
	}

	public void HandleLink()
	{
		if (Href.StartsWithOrdinal("hotkey://"))
		{
			api.Input.GetHotKeyByCode(Href.Substring("hotkey://".Length))?.Handler?.Invoke(null);
			return;
		}
		string[] array = Href.Split(new string[1] { "://" }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 0 && api.LinkProtocols != null && api.LinkProtocols.ContainsKey(array[0]))
		{
			api.LinkProtocols[array[0]](this);
		}
		else if (array.Length != 0 && array[0].StartsWithOrdinal("http"))
		{
			api.Gui.OpenLink(Href);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		hoverText?.Dispose();
		normalText?.Dispose();
	}
}
