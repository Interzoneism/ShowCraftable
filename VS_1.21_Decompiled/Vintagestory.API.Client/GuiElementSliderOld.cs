using System;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementSliderOld : GuiElement
{
	private int minValue;

	private int maxValue = 100;

	private int step = 1;

	private string unit = "";

	private int currentValue;

	private int alarmValue;

	private bool mouseDownOnSlider;

	private bool triggerOnMouseUp;

	private bool didChangeValue;

	private int handleTextureId;

	private int hoverTextTextureId;

	private GuiElementStaticText textElem;

	private int alarmValueTextureId;

	private Rectangled alarmTextureRect;

	private ActionConsumable<int> onNewSliderValue;

	internal const int unscaledHeight = 20;

	internal const int unscaledPadding = 6;

	private int unscaledHandleWidth = 15;

	private int unscaledHandleHeight = 40;

	private int unscaledHoverTextHeight = 50;

	private double handleWidth;

	private double handleHeight;

	private double hoverTextWidth;

	private double hoverTextHeight;

	private double padding;

	public GuiElementSliderOld(ICoreClientAPI capi, ActionConsumable<int> onNewSliderValue, ElementBounds bounds)
		: base(capi, bounds)
	{
		this.onNewSliderValue = onNewSliderValue;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Expected O, but got Unknown
		handleWidth = GuiElement.scaled(unscaledHandleWidth);
		handleHeight = GuiElement.scaled(unscaledHandleHeight);
		hoverTextWidth = GuiElement.scaled(unscaledHoverTextHeight);
		hoverTextHeight = GuiElement.scaled(unscaledHoverTextHeight);
		padding = GuiElement.scaled(6.0);
		Bounds.CalcWorldBounds();
		GuiElement.RoundRectangle(ctxStatic, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, GuiStyle.ElementBGRadius);
		GuiElement.fillWithPattern(api, ctxStatic, GuiElement.woodTextureName);
		EmbossRoundRectangleElement(ctxStatic, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight);
		double num = Bounds.InnerWidth - 2.0 * padding;
		double height = Bounds.InnerHeight - 2.0 * padding;
		ctxStatic.SetSourceRGBA(0.0, 0.0, 0.0, 0.6);
		GuiElement.RoundRectangle(ctxStatic, Bounds.drawX + padding, Bounds.drawY + padding, num, height, GuiStyle.ElementBGRadius);
		ctxStatic.Fill();
		EmbossRoundRectangleElement(ctxStatic, Bounds.drawX + padding, Bounds.drawY + padding, num, height, inverse: true);
		if (alarmValue > 0 && alarmValue < maxValue)
		{
			float num2 = (float)alarmValue / (float)maxValue;
			alarmTextureRect = new Rectangled
			{
				X = padding + (Bounds.InnerWidth - 2.0 * padding) * (double)num2,
				Y = padding,
				Width = (Bounds.InnerWidth - 2.0 * padding) * (double)(1f - num2),
				Height = Bounds.InnerHeight - 2.0 * padding
			};
			ctxStatic.SetSourceRGBA(0.62, 0.0, 0.0, 0.4);
			GuiElement.RoundRectangle(ctxStatic, Bounds.drawX + padding + num * (double)num2, Bounds.drawY + padding, num * (double)(1f - num2), height, GuiStyle.ElementBGRadius);
			ctxStatic.Fill();
		}
		ImageSurface val = new ImageSurface((Format)0, (int)handleWidth + 5, (int)handleHeight + 5);
		Context val2 = genContext(val);
		val2.SetSourceRGBA(1.0, 1.0, 1.0, 0.0);
		val2.Paint();
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, handleWidth, handleHeight, GuiStyle.ElementBGRadius);
		val2.Fill();
		SurfaceTransformBlur.BlurFull(val, 3.0);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, handleWidth, handleHeight, GuiStyle.ElementBGRadius);
		GuiElement.fillWithPattern(api, val2, GuiElement.woodTextureName);
		EmbossRoundRectangleElement(val2, 0.0, 0.0, handleWidth, handleHeight);
		generateTexture(val, ref handleTextureId);
		val2.Dispose();
		((Surface)val).Dispose();
		ComposeHoverTextElement();
	}

	internal void ComposeHoverTextElement()
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected O, but got Unknown
		ElementBounds elementBounds = new ElementBounds().WithFixedPadding(7.0).WithParent(ElementBounds.Empty);
		textElem = new GuiElementStaticText(api, currentValue + unit, EnumTextOrientation.Center, elementBounds, CairoFont.WhiteMediumText());
		textElem.Font.UnscaledFontsize = GuiStyle.SmallishFontSize;
		textElem.AutoBoxSize();
		textElem.Bounds.CalcWorldBounds();
		ImageSurface val = new ImageSurface((Format)0, (int)elementBounds.OuterWidth, (int)elementBounds.OuterHeight);
		Context val2 = genContext(val);
		val2.SetSourceRGBA(1.0, 1.0, 1.0, 0.0);
		val2.Paint();
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.3);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, elementBounds.OuterWidth, elementBounds.OuterHeight, GuiStyle.ElementBGRadius);
		val2.Fill();
		textElem.ComposeElements(val2, val);
		generateTexture(val, ref hoverTextTextureId);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if ((float)(alarmValue - minValue) / (float)(maxValue - minValue) > 0f)
		{
			_ = (float)alarmValue / (float)maxValue;
			api.Render.RenderTexture(alarmValueTextureId, Bounds.renderX + alarmTextureRect.X, Bounds.renderY + alarmTextureRect.Y, alarmTextureRect.Width, alarmTextureRect.Height);
		}
		double num = (Bounds.InnerWidth - 2.0 * padding - handleWidth / 2.0) * (1.0 * (double)currentValue - (double)minValue) / (double)(maxValue - minValue);
		double num2 = (handleHeight - Bounds.InnerHeight) / 2.0;
		api.Render.RenderTexture(handleTextureId, Bounds.renderX + padding + num, Bounds.renderY - num2, (int)handleWidth + 5, (int)handleHeight + 5);
		if (mouseDownOnSlider || Bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			ElementBounds bounds = textElem.Bounds;
			api.Render.RenderTexture(hoverTextTextureId, Bounds.renderX + padding + num - bounds.OuterWidth / 2.0 + handleWidth / 2.0, Bounds.renderY - GuiElement.scaled(20.0) - bounds.OuterHeight, bounds.OuterWidth, bounds.OuterHeight);
		}
	}

	private void MakeAlarmValueTexture()
	{
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Expected O, but got Unknown
		float num = (float)(alarmValue - minValue) / (float)(maxValue - minValue);
		alarmTextureRect = new Rectangled
		{
			X = padding + (Bounds.InnerWidth - 2.0 * padding) * (double)num,
			Y = padding,
			Width = (Bounds.InnerWidth - 2.0 * padding) * (double)(1f - num),
			Height = Bounds.InnerHeight - 2.0 * padding
		};
		ImageSurface val = new ImageSurface((Format)0, (int)alarmTextureRect.Width, (int)alarmTextureRect.Height);
		Context obj = genContext(val);
		obj.SetSourceRGBA(1.0, 0.0, 1.0, 0.4);
		GuiElement.RoundRectangle(obj, 0.0, 0.0, alarmTextureRect.Width, alarmTextureRect.Height, GuiStyle.ElementBGRadius);
		obj.Fill();
		generateTexture(val, ref alarmValueTextureId);
		obj.Dispose();
		((Surface)val).Dispose();
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (Bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			args.Handled = updateValue(api.Input.MouseX);
			mouseDownOnSlider = true;
		}
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		mouseDownOnSlider = false;
		if (onNewSliderValue != null && didChangeValue && triggerOnMouseUp)
		{
			onNewSliderValue(currentValue);
		}
		didChangeValue = false;
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		if (mouseDownOnSlider)
		{
			args.Handled = updateValue(api.Input.MouseX);
		}
	}

	internal void triggerOnlyOnMouseUp(bool trigger = true)
	{
		triggerOnMouseUp = trigger;
	}

	private bool updateValue(int mouseX)
	{
		double num = Bounds.InnerWidth - 2.0 * padding - handleWidth / 2.0;
		double num2 = GameMath.Clamp((double)mouseX - Bounds.renderX - padding, 0.0, num);
		double num3 = (double)minValue + (double)(maxValue - minValue) * num2 / num;
		int num4 = Math.Max(minValue, Math.Min(maxValue, step * (int)Math.Round(1.0 * num3 / (double)step)));
		if (num4 != currentValue)
		{
			didChangeValue = true;
		}
		currentValue = num4;
		ComposeHoverTextElement();
		if (onNewSliderValue != null && !triggerOnMouseUp)
		{
			return onNewSliderValue(currentValue);
		}
		return false;
	}

	public void SetAlarmValue(int value)
	{
		alarmValue = value;
		MakeAlarmValueTexture();
	}

	public void setValues(int currentValue, int minValue, int maxValue, int step, string unit = "")
	{
		this.currentValue = currentValue;
		this.minValue = minValue;
		this.maxValue = maxValue;
		this.step = step;
		this.unit = unit;
		ComposeHoverTextElement();
	}

	public int GetValue()
	{
		return currentValue;
	}
}
