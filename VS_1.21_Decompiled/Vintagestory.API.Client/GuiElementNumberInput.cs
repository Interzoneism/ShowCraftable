using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class GuiElementNumberInput : GuiElementTextInput
{
	public float Interval = 1f;

	public LoadedTexture buttonHighlightTexture;

	private bool focusable = true;

	public bool DisableButtonFocus;

	public bool IntMode { get; set; }

	public override bool Focusable
	{
		get
		{
			if (focusable)
			{
				return enabled;
			}
			return false;
		}
	}

	public GuiElementNumberInput(ICoreClientAPI capi, ElementBounds bounds, Action<string> OnTextChanged, CairoFont font)
		: base(capi, bounds, OnTextChanged, font)
	{
		buttonHighlightTexture = new LoadedTexture(capi);
	}

	public float GetValue()
	{
		float.TryParse(GetText(), NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result);
		return result;
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
		rightSpacing = GuiElement.scaled(17.0);
		EmbossRoundRectangleElement(ctx, Bounds, inverse: true, 2, 1);
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
		ElementRoundRectangle(ctx, Bounds, isBackground: false, 1.0);
		ctx.Fill();
		GenTextHighlightTexture();
		GenButtonHighlightTexture();
		if (!enabled)
		{
			Font.Color[3] = 0.3499999940395355;
		}
		highlightBounds = Bounds.CopyOffsetedSibling().WithFixedPadding(0.0, 0.0).FixedGrow(2.0 * Bounds.absPaddingX, 2.0 * Bounds.absPaddingY);
		highlightBounds.CalcWorldBounds();
		RecomposeText();
		double num = Bounds.OuterHeight / 2.0 - 1.0;
		double[] array = GuiStyle.DialogHighlightColor.ToArray();
		if (!enabled)
		{
			array[3] = 0.315;
		}
		ctx.SetSourceRGBA(array);
		GuiElement.RoundRectangle(ctx, Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(18.0) * Scale, Bounds.drawY, rightSpacing * Scale, num, 1.0);
		ctx.Fill();
		EmbossRoundRectangleElement(ctx, Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(18.0) * Scale, Bounds.drawY, rightSpacing * Scale, num, inverse: false, 2, 1);
		ctx.NewPath();
		ctx.LineTo(Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(9.0) * Scale, Bounds.drawY + GuiElement.scaled(1.0) * Scale);
		ctx.LineTo(Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(14.0) * Scale, Bounds.drawY + (num - GuiElement.scaled(2.0)) * Scale);
		ctx.LineTo(Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(4.0) * Scale, Bounds.drawY + (num - GuiElement.scaled(2.0)) * Scale);
		ctx.ClosePath();
		ctx.SetSourceRGBA(1.0, 1.0, 1.0, enabled ? 0.4 : 0.14);
		ctx.Fill();
		ctx.SetSourceRGBA(array);
		GuiElement.RoundRectangle(ctx, Bounds.drawX + Bounds.InnerWidth - (rightSpacing + GuiElement.scaled(1.0)) * Scale, Bounds.drawY + num + GuiElement.scaled(1.0) * Scale, rightSpacing * Scale, num, 1.0);
		ctx.Fill();
		EmbossRoundRectangleElement(ctx, Bounds.drawX + Bounds.InnerWidth - (rightSpacing + GuiElement.scaled(1.0)) * Scale, Bounds.drawY + num + GuiElement.scaled(1.0) * Scale, rightSpacing * Scale, num, inverse: false, 2, 1);
		ctx.NewPath();
		ctx.LineTo(Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(14.0) * Scale, Bounds.drawY + (num + GuiElement.scaled(3.0)) * Scale);
		ctx.LineTo(Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(4.0) * Scale, Bounds.drawY + (num + GuiElement.scaled(3.0)) * Scale);
		ctx.LineTo(Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(9.0) * Scale, Bounds.drawY + num * 2.0 * Scale);
		ctx.ClosePath();
		ctx.SetSourceRGBA(1.0, 1.0, 1.0, enabled ? 0.4 : 0.14);
		ctx.Fill();
		highlightBounds.fixedWidth -= rightSpacing / (double)RuntimeEnv.GUIScale;
		highlightBounds.CalcWorldBounds();
	}

	private void GenButtonHighlightTexture()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		double num = Bounds.OuterHeight / 2.0 - 1.0;
		ImageSurface val = new ImageSurface((Format)0, (int)rightSpacing, (int)num);
		Context obj = genContext(val);
		obj.SetSourceRGBA(1.0, 1.0, 1.0, 0.2);
		obj.Paint();
		generateTexture(val, ref buttonHighlightTexture);
		obj.Dispose();
		((Surface)val).Dispose();
	}

	private void GenTextHighlightTexture()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, (int)(Bounds.OuterWidth - rightSpacing), (int)Bounds.OuterHeight);
		Context obj = genContext(val);
		obj.SetSourceRGBA(1.0, 1.0, 1.0, 0.2);
		obj.Paint();
		generateTexture(val, ref highlightTexture);
		obj.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		base.RenderInteractiveElements(deltaTime);
		if (!enabled)
		{
			return;
		}
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		MouseOverCursor = "textselect";
		if ((double)mouseX >= Bounds.absX + Bounds.InnerWidth - GuiElement.scaled(21.0) && (double)mouseX <= Bounds.absX + Bounds.OuterWidth && (double)mouseY >= Bounds.absY && (double)mouseY <= Bounds.absY + Bounds.OuterHeight)
		{
			MouseOverCursor = null;
			double num = Bounds.OuterHeight / 2.0 - 1.0;
			if ((double)mouseY > Bounds.absY + num + 1.0)
			{
				api.Render.Render2DTexturePremultipliedAlpha(buttonHighlightTexture.TextureId, Bounds.renderX + Bounds.OuterWidth - rightSpacing - 1.0, Bounds.renderY + num + 1.0, rightSpacing, num);
			}
			else
			{
				api.Render.Render2DTexturePremultipliedAlpha(buttonHighlightTexture.TextureId, Bounds.renderX + Bounds.OuterWidth - rightSpacing - 1.0, Bounds.renderY, rightSpacing, num);
			}
		}
	}

	public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
	{
		if (enabled && IsPositionInside(api.Input.MouseX, api.Input.MouseY))
		{
			rightSpacing = GuiElement.scaled(17.0);
			float num = ((args.deltaPrecise > 0f) ? 1 : (-1));
			num *= Interval;
			if (api.Input.KeyboardKeyState[1])
			{
				num /= 10f;
			}
			if (api.Input.KeyboardKeyState[3])
			{
				num /= 100f;
			}
			UpdateValue(num);
			args.SetHandled();
		}
	}

	private void UpdateValue(float size)
	{
		if (IntMode)
		{
			size = (int)((size > 0f) ? Math.Ceiling(size) : Math.Floor(size));
		}
		double.TryParse(lines[0], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result);
		result += (double)size;
		lines[0] = Math.Round(result, 4).ToString(GlobalConstants.DefaultCultureInfo);
		SetValue(lines[0]);
	}

	public override void LoadValue(List<string> newLines)
	{
		if (newLines.Any((string line) => !isValidText(line)))
		{
			linesStaging = lines.ToList();
		}
		else
		{
			base.LoadValue(newLines);
		}
	}

	private bool isValidText(string text)
	{
		if (text == string.Empty)
		{
			return true;
		}
		if (!IntMode && !double.TryParse(text, NumberStyles.Float, GlobalConstants.DefaultCultureInfo, out var _))
		{
			return false;
		}
		if (IntMode && !int.TryParse(text, NumberStyles.Integer, GlobalConstants.DefaultCultureInfo, out var _))
		{
			return false;
		}
		return true;
	}

	public override void OnFocusLost()
	{
		base.OnFocusLost();
		if (GetText() == string.Empty)
		{
			SetValue(0f);
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		rightSpacing = GuiElement.scaled(17.0);
		int x = args.X;
		int y = args.Y;
		float num = Interval;
		if (api.Input.KeyboardKeyState[1])
		{
			num /= 10f;
		}
		if (api.Input.KeyboardKeyState[3])
		{
			num /= 100f;
		}
		if ((double)x >= Bounds.absX + Bounds.OuterWidth - rightSpacing && (double)x <= Bounds.absX + Bounds.OuterWidth && (double)y >= Bounds.absY && (double)y <= Bounds.absY + Bounds.OuterHeight)
		{
			if (DisableButtonFocus)
			{
				focusable = false;
			}
			double num2 = Bounds.OuterHeight / 2.0 - 1.0;
			if ((double)y > Bounds.absY + num2 + 1.0)
			{
				UpdateValue(0f - num);
			}
			else
			{
				UpdateValue(num);
			}
			api.Gui.PlaySound("tick");
		}
		else if (DisableButtonFocus)
		{
			focusable = true;
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		buttonHighlightTexture.Dispose();
	}
}
