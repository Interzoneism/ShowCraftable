using System;
using Cairo;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementMainMenuCell : GuiElementTextBase, IGuiElementCell, IDisposable
{
	public static double unscaledRightBoxWidth = 40.0;

	private static int unscaledDepth = 4;

	public SavegameCellEntry cellEntry;

	private double titleTextheight;

	public bool ShowModifyIcons = true;

	private LoadedTexture releasedButtonTexture;

	private LoadedTexture pressedButtonTexture;

	private LoadedTexture leftHighlightTexture;

	private LoadedTexture rightHighlightTexture;

	private double pressedYOffset;

	public double MainTextWidthSub;

	public Action<int> OnMouseDownOnCellLeft;

	public Action<int> OnMouseDownOnCellRight;

	public double? FixedHeight;

	ElementBounds IGuiElementCell.Bounds => Bounds;

	public GuiElementMainMenuCell(ICoreClientAPI capi, SavegameCellEntry cell, ElementBounds bounds)
		: base(capi, "", null, bounds)
	{
		cellEntry = cell;
		leftHighlightTexture = new LoadedTexture(capi);
		rightHighlightTexture = new LoadedTexture(capi);
		releasedButtonTexture = new LoadedTexture(capi);
		pressedButtonTexture = new LoadedTexture(capi);
		if (cell.TitleFont == null)
		{
			cell.TitleFont = CairoFont.WhiteSmallishText();
		}
		if (cell.DetailTextFont == null)
		{
			cell.DetailTextFont = CairoFont.WhiteSmallText();
			cell.DetailTextFont.Color[3] *= 0.8;
			cell.DetailTextFont.LineHeightMultiplier = 1.1;
		}
	}

	public void Compose()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		Bounds.CalcWorldBounds();
		ImageSurface val = new ImageSurface((Format)0, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
		Context val2 = new Context((Surface)(object)val);
		ComposeButton(val2, val, pressed: false);
		generateTexture(val, ref releasedButtonTexture);
		val2.Operator = (Operator)0;
		val2.Paint();
		val2.Operator = (Operator)2;
		ComposeButton(val2, val, pressed: true);
		generateTexture(val, ref pressedButtonTexture);
		val2.Dispose();
		((Surface)val).Dispose();
		ComposeHover(left: true, ref leftHighlightTexture);
		if (ShowModifyIcons)
		{
			ComposeHover(left: false, ref rightHighlightTexture);
		}
	}

	private void ComposeButton(Context ctx, ImageSurface surface, bool pressed)
	{
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		double num = (ShowModifyIcons ? GuiElement.scaled(unscaledRightBoxWidth) : 0.0);
		pressedYOffset = 0.0;
		if (cellEntry.DrawAsButton)
		{
			GuiElement.RoundRectangle(ctx, 0.0, 0.0, Bounds.OuterWidthInt, Bounds.OuterHeightInt, 1.0);
			ctx.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
			ctx.Fill();
			if (pressed)
			{
				pressedYOffset = GuiElement.scaled(unscaledDepth) / 2.0;
			}
			EmbossRoundRectangleElement(ctx, 0.0, 0.0, Bounds.OuterWidthInt, Bounds.OuterHeightInt, pressed, (int)GuiElement.scaled(unscaledDepth));
		}
		Font = cellEntry.TitleFont;
		titleTextheight = textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, cellEntry.Title, Bounds.absPaddingX, Bounds.absPaddingY + Bounds.absPaddingY + GuiElement.scaled(cellEntry.LeftOffY) + pressedYOffset, Bounds.InnerWidth - num - MainTextWidthSub);
		Font = cellEntry.DetailTextFont;
		textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, cellEntry.DetailText, Bounds.absPaddingX, Bounds.absPaddingY + cellEntry.DetailTextOffY + titleTextheight + 2.0 + Bounds.absPaddingY + GuiElement.scaled(cellEntry.LeftOffY) + pressedYOffset, Bounds.InnerWidth - num - MainTextWidthSub);
		if (cellEntry.RightTopText != null)
		{
			TextExtents textExtents = Font.GetTextExtents(cellEntry.RightTopText);
			textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, cellEntry.RightTopText, Bounds.absPaddingX + Bounds.InnerWidth - ((TextExtents)(ref textExtents)).Width - num - GuiElement.scaled(10.0), Bounds.absPaddingY + Bounds.absPaddingY + GuiElement.scaled(cellEntry.RightTopOffY) + pressedYOffset, ((TextExtents)(ref textExtents)).Width + 1.0, EnumTextOrientation.Right);
		}
		if (ShowModifyIcons)
		{
			ctx.LineWidth = GuiElement.scaled(1.0);
			double size = GuiElement.scaled(20.0);
			double lineWidth = GuiElement.scaled(5.0);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
			ctx.NewPath();
			ctx.MoveTo(Bounds.InnerWidth - num, GuiElement.scaled(1.0));
			ctx.LineTo(Bounds.InnerWidth - num, Bounds.OuterHeight - GuiElement.scaled(2.0));
			ctx.ClosePath();
			ctx.Stroke();
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.3);
			ctx.NewPath();
			ctx.MoveTo(Bounds.InnerWidth - num + GuiElement.scaled(1.0), GuiElement.scaled(1.0));
			ctx.LineTo(Bounds.InnerWidth - num + GuiElement.scaled(1.0), Bounds.OuterHeight - GuiElement.scaled(2.0));
			ctx.ClosePath();
			ctx.Stroke();
			double num2 = Bounds.absPaddingX + Bounds.InnerWidth - num + GuiElement.scaled(5.0);
			double absPaddingY = Bounds.absPaddingY;
			ctx.Operator = (Operator)1;
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.8);
			api.Gui.Icons.DrawPen(ctx, num2 - 1.0, absPaddingY - 1.0 + GuiElement.scaled(5.0), lineWidth, size);
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.5);
			api.Gui.Icons.DrawPen(ctx, num2 + 1.0, absPaddingY + 1.0 + GuiElement.scaled(5.0), lineWidth, size);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
			api.Gui.Icons.DrawPen(ctx, num2, absPaddingY + GuiElement.scaled(5.0), lineWidth, size);
			ctx.Operator = (Operator)2;
		}
		if (cellEntry.DrawAsButton && pressed)
		{
			GuiElement.RoundRectangle(ctx, 0.0, 0.0, Bounds.OuterWidthInt, Bounds.OuterHeightInt, 1.0);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.15);
			ctx.Fill();
		}
	}

	private void ComposeHover(bool left, ref LoadedTexture texture)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context val2 = genContext(val);
		double num = GuiElement.scaled(unscaledRightBoxWidth);
		if (!ShowModifyIcons)
		{
			num = 0.0 - Bounds.OuterWidth + Bounds.InnerWidth;
		}
		if (left)
		{
			val2.NewPath();
			val2.LineTo(0.0, 0.0);
			val2.LineTo(Bounds.InnerWidth - num, 0.0);
			val2.LineTo(Bounds.InnerWidth - num, Bounds.OuterHeight);
			val2.LineTo(0.0, Bounds.OuterHeight);
			val2.ClosePath();
		}
		else
		{
			val2.NewPath();
			val2.LineTo(Bounds.InnerWidth - num, 0.0);
			val2.LineTo(Bounds.OuterWidth, 0.0);
			val2.LineTo(Bounds.OuterWidth, Bounds.OuterHeight);
			val2.LineTo(Bounds.InnerWidth - num, Bounds.OuterHeight);
			val2.ClosePath();
		}
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.15);
		val2.Fill();
		generateTexture(val, ref texture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public void UpdateCellHeight()
	{
		Bounds.CalcWorldBounds();
		if (FixedHeight.HasValue)
		{
			Bounds.fixedHeight = FixedHeight.Value;
			return;
		}
		double num = Bounds.absPaddingY / (double)RuntimeEnv.GUIScale;
		double innerWidth = Bounds.InnerWidth;
		Font = cellEntry.TitleFont;
		text = cellEntry.Title;
		titleTextheight = textUtil.GetMultilineTextHeight(Font, cellEntry.Title, innerWidth - MainTextWidthSub) / (double)RuntimeEnv.GUIScale;
		Font = cellEntry.DetailTextFont;
		text = cellEntry.DetailText;
		double num2 = textUtil.GetMultilineTextHeight(Font, cellEntry.DetailText, innerWidth - MainTextWidthSub) / (double)RuntimeEnv.GUIScale;
		Bounds.fixedHeight = num + titleTextheight + num + num2 + num;
		if (ShowModifyIcons && Bounds.fixedHeight < 73.0)
		{
			Bounds.fixedHeight = 73.0;
		}
	}

	public void OnRenderInteractiveElements(ICoreClientAPI api, float deltaTime)
	{
		if (pressedButtonTexture.TextureId == 0)
		{
			Compose();
		}
		if (cellEntry.Selected)
		{
			api.Render.Render2DTexturePremultipliedAlpha(pressedButtonTexture.TextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
		}
		else
		{
			api.Render.Render2DTexturePremultipliedAlpha(releasedButtonTexture.TextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
		}
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		Vec2d vec2d = Bounds.PositionInside(mouseX, mouseY);
		if (!(vec2d == null) && IsPositionInside(api.Input.MouseX, api.Input.MouseY))
		{
			if (ShowModifyIcons && vec2d.X > Bounds.InnerWidth - GuiElement.scaled(unscaledRightBoxWidth))
			{
				api.Render.Render2DTexturePremultipliedAlpha(rightHighlightTexture.TextureId, Bounds.absX, Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			}
			else
			{
				api.Render.Render2DTexturePremultipliedAlpha(leftHighlightTexture.TextureId, Bounds.absX, Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		leftHighlightTexture.Dispose();
		rightHighlightTexture.Dispose();
		releasedButtonTexture.Dispose();
		pressedButtonTexture.Dispose();
	}

	public void OnMouseUpOnElement(MouseEvent args, int elementIndex)
	{
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		Vec2d vec2d = Bounds.PositionInside(mouseX, mouseY);
		api.Gui.PlaySound("toggleswitch");
		if (vec2d.X > Bounds.InnerWidth - GuiElement.scaled(unscaledRightBoxWidth))
		{
			OnMouseDownOnCellRight?.Invoke(elementIndex);
			args.Handled = true;
		}
		else
		{
			OnMouseDownOnCellLeft?.Invoke(elementIndex);
			args.Handled = true;
		}
	}

	public void OnMouseMoveOnElement(MouseEvent args, int elementIndex)
	{
	}

	public void OnMouseDownOnElement(MouseEvent args, int elementIndex)
	{
	}
}
