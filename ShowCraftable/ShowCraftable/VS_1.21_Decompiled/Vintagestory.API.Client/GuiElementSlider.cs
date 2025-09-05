using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementSlider : GuiElementControl
{
	private int minValue;

	private int maxValue = 100;

	private int step = 1;

	private string unit = "";

	private int currentValue;

	private int alarmValue;

	private HashSet<int> skipValues = new HashSet<int>();

	private List<int> allowValues = new List<int>();

	private bool mouseDownOnSlider;

	private bool mouseOnSlider;

	private bool triggerOnMouseUp;

	private bool didChangeValue;

	private LoadedTexture handleTexture;

	private LoadedTexture hoverTextTexture;

	private LoadedTexture restingTextTexture;

	private LoadedTexture sliderFillTexture;

	private LoadedTexture alarmValueTexture;

	private GuiElementStaticText textElem;

	private GuiElementStaticText textElemResting;

	private Rectangled alarmTextureRect;

	private ActionConsumable<int> onNewSliderValue;

	public SliderTooltipDelegate OnSliderTooltip;

	public SliderTooltipDelegate OnSliderRestingText;

	internal const int unscaledHeight = 20;

	internal const int unscaledPadding = 4;

	private int unscaledHandleWidth = 15;

	private int unscaledHandleHeight = 35;

	private int unscaledHoverTextHeight = 50;

	private double handleWidth;

	private double handleHeight;

	private double padding;

	public bool TooltipExceedClipBounds { get; set; }

	public bool ShowTextWhenResting { get; set; }

	public override bool Enabled
	{
		get
		{
			return base.Enabled;
		}
		set
		{
			enabled = value;
			ComposeHandleElement();
			if (alarmValue > minValue && alarmValue < maxValue)
			{
				MakeAlarmValueTexture();
			}
			ComposeHoverTextElement();
			ComposeRestingTextElement();
			ComposeFillTexture();
		}
	}

	public override bool Focusable => enabled;

	public GuiElementSlider(ICoreClientAPI capi, ActionConsumable<int> onNewSliderValue, ElementBounds bounds)
		: base(capi, bounds)
	{
		handleTexture = new LoadedTexture(capi);
		hoverTextTexture = new LoadedTexture(capi);
		restingTextTexture = new LoadedTexture(capi);
		sliderFillTexture = new LoadedTexture(capi);
		alarmValueTexture = new LoadedTexture(capi);
		this.onNewSliderValue = onNewSliderValue;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		handleWidth = GuiElement.scaled(unscaledHandleWidth) * Scale;
		handleHeight = GuiElement.scaled(unscaledHandleHeight) * Scale;
		padding = GuiElement.scaled(4.0) * Scale;
		Bounds.CalcWorldBounds();
		ctxStatic.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
		GuiElement.RoundRectangle(ctxStatic, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, 1.0);
		ctxStatic.Fill();
		EmbossRoundRectangleElement(ctxStatic, Bounds, inverse: true, 1, 1);
		_ = Bounds.InnerWidth;
		_ = padding;
		_ = Bounds.InnerHeight;
		_ = padding;
		ComposeHandleElement();
		ComposeFillTexture();
		ComposeHoverTextElement();
		ComposeRestingTextElement();
	}

	internal void ComposeHandleElement()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, (int)handleWidth + 4, (int)handleHeight + 4);
		Context val2 = genContext(val);
		val2.SetSourceRGBA(1.0, 1.0, 1.0, 0.0);
		val2.Paint();
		GuiElement.RoundRectangle(val2, 2.0, 2.0, handleWidth, handleHeight, 1.0);
		if (!enabled)
		{
			val2.SetSourceRGB(43.0 / 255.0, 11.0 / 85.0, 8.0 / 85.0);
			val2.FillPreserve();
		}
		GuiElement.fillWithPattern(api, val2, GuiElement.woodTextureName, nearestScalingFiler: false, preserve: true, enabled ? 255 : 159, 0.5f);
		val2.SetSourceRGB(43.0 / 255.0, 11.0 / 85.0, 8.0 / 85.0);
		val2.LineWidth = 2.0;
		val2.Stroke();
		generateTexture(val, ref handleTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	internal void ComposeHoverTextElement()
	{
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected O, but got Unknown
		ElementBounds elementBounds = new ElementBounds().WithFixedPadding(7.0).WithParent(ElementBounds.Empty);
		string text = currentValue + unit;
		if (OnSliderTooltip != null)
		{
			text = OnSliderTooltip(currentValue);
		}
		textElem = new GuiElementStaticText(api, text, EnumTextOrientation.Center, elementBounds, CairoFont.WhiteMediumText().WithFontSize((float)GuiStyle.SubNormalFontSize));
		textElem.Font.UnscaledFontsize = GuiStyle.SmallishFontSize;
		textElem.AutoBoxSize();
		textElem.Bounds.CalcWorldBounds();
		ImageSurface val = new ImageSurface((Format)0, (int)elementBounds.OuterWidth, (int)elementBounds.OuterHeight);
		Context val2 = genContext(val);
		val2.SetSourceRGBA(1.0, 1.0, 1.0, 0.0);
		val2.Paint();
		val2.SetSourceRGBA(GuiStyle.DialogStrongBgColor);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, elementBounds.OuterWidth, elementBounds.OuterHeight, GuiStyle.ElementBGRadius);
		val2.FillPreserve();
		double[] dialogStrongBgColor = GuiStyle.DialogStrongBgColor;
		val2.SetSourceRGBA(dialogStrongBgColor[0] / 2.0, dialogStrongBgColor[1] / 2.0, dialogStrongBgColor[2] / 2.0, dialogStrongBgColor[3]);
		val2.Stroke();
		textElem.ComposeElements(val2, val);
		generateTexture(val, ref hoverTextTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	internal void ComposeRestingTextElement()
	{
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Expected O, but got Unknown
		ElementBounds elementBounds = new ElementBounds().WithFixedPadding(7.0).WithParent(ElementBounds.Empty);
		double num = (Bounds.InnerWidth - 2.0 * padding - handleWidth / 2.0) * (1.0 * (double)currentValue - (double)minValue) / (double)(maxValue - minValue);
		string text = currentValue + unit;
		if (OnSliderRestingText != null)
		{
			text = OnSliderRestingText(currentValue);
		}
		else if (OnSliderTooltip != null)
		{
			text = OnSliderTooltip(currentValue);
		}
		textElemResting = new GuiElementStaticText(api, text, EnumTextOrientation.Center, elementBounds, CairoFont.WhiteSmallText());
		textElemResting.AutoBoxSize();
		textElemResting.Bounds.CalcWorldBounds();
		ElementBounds bounds = textElemResting.Bounds;
		double num2 = (int)(GuiElement.scaled(30.0) * Scale);
		FontExtents fontExtents = textElemResting.Font.GetFontExtents();
		bounds.fixedY = (num2 - ((FontExtents)(ref fontExtents)).Height) / 2.0 / (double)RuntimeEnv.GUIScale;
		if (!enabled)
		{
			textElemResting.Font.Color[3] = 0.35;
		}
		if (num - 10.0 >= textElemResting.Bounds.InnerWidth)
		{
			textElemResting.Font.Color = new double[4]
			{
				0.0,
				0.0,
				0.0,
				enabled ? 1.0 : 0.5
			};
		}
		ImageSurface val = new ImageSurface((Format)0, (int)elementBounds.OuterWidth, (int)elementBounds.OuterHeight);
		Context val2 = genContext(val);
		textElemResting.ComposeElements(val2, val);
		generateTexture(val, ref restingTextTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	internal void ComposeFillTexture()
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		double num = (Bounds.InnerWidth - 2.0 * padding - handleWidth / 2.0) * (1.0 * (double)currentValue - (double)minValue) / (double)(maxValue - minValue);
		double num2 = Bounds.InnerHeight - 2.0 * padding;
		ImageSurface val = new ImageSurface((Format)0, (int)(num + 5.0), (int)num2);
		Context val2 = genContext(val);
		SurfacePattern pattern = GuiElement.getPattern(api, GuiElement.waterTextureName, doCache: true, enabled ? 255 : 127, 0.5f);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, val.Width, val.Height, 1.0);
		if (enabled)
		{
			val2.SetSourceRGBA(0.0, 0.0, 0.0, 1.0);
		}
		else
		{
			val2.SetSourceRGBA(0.15, 0.15, 0.0, 0.65);
		}
		val2.FillPreserve();
		val2.SetSource((Pattern)(object)pattern);
		val2.Fill();
		generateTexture(val, ref sliderFillTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if ((float)(alarmValue - minValue) / (float)(maxValue - minValue) > 0f && alarmValueTexture.TextureId > 0)
		{
			_ = (float)alarmValue / (float)maxValue;
			api.Render.RenderTexture(alarmValueTexture.TextureId, Bounds.renderX + alarmTextureRect.X, Bounds.renderY + alarmTextureRect.Y, alarmTextureRect.Width, alarmTextureRect.Height);
		}
		double num = (Bounds.InnerWidth - 2.0 * padding - handleWidth / 2.0) * (1.0 * (double)currentValue - (double)minValue) / (double)(maxValue - minValue);
		double num2 = Bounds.InnerHeight - 2.0 * padding;
		double num3 = (handleHeight - Bounds.OuterHeight + padding) / 2.0;
		api.Render.Render2DTexturePremultipliedAlpha(sliderFillTexture.TextureId, Bounds.renderX + padding, Bounds.renderY + padding, (int)(num + 5.0), (int)num2);
		api.Render.Render2DTexturePremultipliedAlpha(handleTexture.TextureId, Bounds.renderX + num, Bounds.renderY - num3, (int)handleWidth + 4, (int)handleHeight + 4);
		if (mouseDownOnSlider || mouseOnSlider)
		{
			if (TooltipExceedClipBounds)
			{
				api.Render.PopScissor();
			}
			ElementBounds bounds = textElem.Bounds;
			api.Render.Render2DTexturePremultipliedAlpha(hoverTextTexture.TextureId, (int)(Bounds.renderX + padding + num - bounds.OuterWidth / 2.0 + handleWidth / 2.0), (int)(Bounds.renderY - GuiElement.scaled(20.0) - bounds.OuterHeight), bounds.OuterWidthInt, bounds.OuterHeightInt, 300f);
			if (TooltipExceedClipBounds)
			{
				api.Render.PushScissor(InsideClipBounds);
			}
		}
		if (ShowTextWhenResting)
		{
			api.Render.PushScissor(Bounds, stacking: true);
			ElementBounds bounds2 = textElemResting.Bounds;
			double posX = ((num - 10.0 < bounds2.InnerWidth) ? (Bounds.renderX + padding + num - bounds2.OuterWidth / 2.0 + handleWidth / 2.0 + (double)(restingTextTexture.Width / 2) + 10.0) : ((double)(int)Bounds.renderX));
			api.Render.Render2DTexturePremultipliedAlpha(restingTextTexture.TextureId, posX, Bounds.renderY + (num2 - bounds2.OuterHeight - padding / 2.0) / 2.0, bounds2.OuterWidthInt, bounds2.OuterHeightInt, 300f);
			api.Render.PopScissor();
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
		obj.SetSourceRGBA(1.0, 0.0, 1.0, enabled ? 0.4 : 0.25);
		GuiElement.RoundRectangle(obj, 0.0, 0.0, alarmTextureRect.Width, alarmTextureRect.Height, GuiStyle.ElementBGRadius);
		obj.Fill();
		generateTexture(val, ref alarmValueTexture.TextureId);
		obj.Dispose();
		((Surface)val).Dispose();
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (enabled && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			args.Handled = updateValue(api.Input.MouseX);
			mouseDownOnSlider = true;
		}
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		mouseDownOnSlider = false;
		if (enabled)
		{
			if (onNewSliderValue != null && didChangeValue && triggerOnMouseUp)
			{
				onNewSliderValue(currentValue);
			}
			didChangeValue = false;
		}
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		mouseOnSlider = Bounds.PointInside(api.Input.MouseX, api.Input.MouseY);
		if (enabled && mouseDownOnSlider)
		{
			args.Handled = updateValue(api.Input.MouseX);
		}
	}

	public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
	{
		if (enabled && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			args.SetHandled();
			int num = Math.Sign(args.deltaPrecise);
			if ((currentValue > minValue || num >= 0) && (currentValue < maxValue || num <= 0))
			{
				currentValue = allowValues[allowValues.IndexOf(currentValue) + num];
				ComposeHoverTextElement();
				ComposeRestingTextElement();
				ComposeFillTexture();
				onNewSliderValue?.Invoke(currentValue);
			}
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (!base.HasFocus)
		{
			return;
		}
		int num = 0;
		if (args.KeyCode == 47)
		{
			if (currentValue <= allowValues.First())
			{
				return;
			}
			num = -1;
		}
		else if (args.KeyCode == 48)
		{
			if (currentValue >= allowValues.Last())
			{
				return;
			}
			num = 1;
		}
		if (num != 0)
		{
			currentValue = allowValues[allowValues.IndexOf(currentValue) + num];
			ComposeHoverTextElement();
			ComposeRestingTextElement();
			ComposeFillTexture();
			onNewSliderValue?.Invoke(currentValue);
		}
	}

	internal void TriggerOnlyOnMouseUp(bool trigger = true)
	{
		triggerOnMouseUp = trigger;
	}

	private bool updateValue(int mouseX)
	{
		double num = Bounds.InnerWidth - 2.0 * padding - handleWidth / 2.0;
		double num2 = GameMath.Clamp((double)mouseX - Bounds.renderX - padding, 0.0, num);
		double value = (double)minValue + (double)(maxValue - minValue) * num2 / num;
		int num3 = ((allowValues.Count == 0) ? currentValue : allowValues.OrderBy((int item) => Math.Abs(value - (double)item)).First());
		if (num3 == currentValue)
		{
			return true;
		}
		didChangeValue = true;
		currentValue = num3;
		ComposeHoverTextElement();
		ComposeRestingTextElement();
		ComposeFillTexture();
		if (onNewSliderValue != null && !triggerOnMouseUp)
		{
			return onNewSliderValue(currentValue);
		}
		return true;
	}

	public void SetAlarmValue(int value)
	{
		alarmValue = value;
		MakeAlarmValueTexture();
	}

	public void SetSkipValues(HashSet<int> skipValues)
	{
		this.skipValues = skipValues;
		allowValues.Clear();
		for (int i = minValue; i <= maxValue; i += step)
		{
			if (!skipValues.Contains(i))
			{
				allowValues.Add(i);
			}
		}
	}

	public void ClearSkipValues()
	{
		skipValues.Clear();
		allowValues.Clear();
		for (int i = minValue; i <= maxValue; i += step)
		{
			allowValues.Add(i);
		}
	}

	public void AddSkipValue(int skipValue)
	{
		skipValues.Add(skipValue);
		allowValues.Remove(skipValue);
	}

	public void RemoveSkipValue(int skipValue)
	{
		skipValues.Remove(skipValue);
		allowValues.Clear();
		for (int i = minValue; i <= maxValue; i += step)
		{
			if (!skipValues.Contains(i))
			{
				allowValues.Add(i);
			}
		}
	}

	public void SetValues(int currentValue, int minValue, int maxValue, int step, string unit = "")
	{
		this.currentValue = currentValue;
		this.minValue = minValue;
		this.maxValue = maxValue;
		this.step = step;
		this.unit = unit;
		allowValues.Clear();
		for (int i = minValue; i <= maxValue; i += step)
		{
			if (!skipValues.Contains(i))
			{
				allowValues.Add(i);
			}
		}
		ComposeHoverTextElement();
		ComposeRestingTextElement();
		ComposeFillTexture();
	}

	public void SetValue(int currentValue)
	{
		this.currentValue = currentValue;
	}

	public int GetValue()
	{
		return currentValue;
	}

	public override void Dispose()
	{
		base.Dispose();
		handleTexture.Dispose();
		hoverTextTexture.Dispose();
		restingTextTexture.Dispose();
		sliderFillTexture.Dispose();
		alarmValueTexture.Dispose();
	}
}
