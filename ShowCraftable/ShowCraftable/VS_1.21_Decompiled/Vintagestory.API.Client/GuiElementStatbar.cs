using System;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementStatbar : GuiElementTextBase
{
	private float minValue;

	private float maxValue = 100f;

	private float value = 32f;

	private float? futureValue;

	private float? previousValue;

	private float valueChangeVelocity;

	private float lineInterval = 10f;

	private float prevValueDisplayRemainingSec;

	private float prevValueSize;

	private float? nowRenderingPreviousValue;

	private double[] color;

	private bool rightToLeft;

	private LoadedTexture baseTexture;

	private LoadedTexture barTexture;

	private LoadedTexture flashTexture;

	private LoadedTexture valueTexture;

	private LoadedTexture previousValueTexture;

	private int valueHeight;

	public bool ShouldFlash;

	public float FlashTime;

	public bool ShowValueOnHover = true;

	private bool valuesSet;

	private bool hideable;

	public StatbarValueDelegate onGetStatbarValue;

	public CairoFont valueFont = CairoFont.WhiteSmallText().WithStroke(ColorUtil.BlackArgbDouble, 0.75);

	public static double DefaultHeight = 8.0;

	private long visibleSinceMs;

	private Func<long> getMs;

	public bool HideWhenFull { get; set; }

	public bool PrevValueBeingDisplayed => prevValueDisplayRemainingSec > 0f;

	public float PreviousValueDisplayTime { get; set; } = 2f;

	public GuiElementStatbar(ICoreClientAPI capi, ElementBounds bounds, double[] color, bool rightToLeft, bool hideable)
		: base(capi, "", CairoFont.WhiteDetailText(), bounds)
	{
		barTexture = new LoadedTexture(capi);
		flashTexture = new LoadedTexture(capi);
		valueTexture = new LoadedTexture(capi);
		previousValueTexture = new LoadedTexture(capi);
		if (hideable)
		{
			baseTexture = new LoadedTexture(capi);
		}
		this.hideable = hideable;
		this.color = color;
		this.rightToLeft = rightToLeft;
		onGetStatbarValue = () => (float)Math.Round(value, 1) + " / " + (int)maxValue;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		Bounds.CalcWorldBounds();
		if (hideable)
		{
			surface = new ImageSurface((Format)0, Bounds.OuterWidthInt + 1, Bounds.OuterHeightInt + 1);
			ctx = new Context((Surface)(object)surface);
			GuiElement.RoundRectangle(ctx, 0.0, 0.0, Bounds.InnerWidth, Bounds.InnerHeight, 1.0);
			ctx.SetSourceRGBA(0.15, 0.15, 0.15, 1.0);
			ctx.Fill();
			EmbossRoundRectangleElement(ctx, 0.0, 0.0, Bounds.InnerWidth, Bounds.InnerHeight, inverse: false, 3, 1);
		}
		else
		{
			ctx.Operator = (Operator)2;
			GuiElement.RoundRectangle(ctx, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, 1.0);
			ctx.SetSourceRGBA(0.15, 0.15, 0.15, 1.0);
			ctx.Fill();
			EmbossRoundRectangleElement(ctx, Bounds, inverse: false, 3, 1);
		}
		if (valuesSet)
		{
			recomposeOverlays();
		}
		if (hideable)
		{
			generateTexture(surface, ref baseTexture);
			((Surface)surface).Dispose();
			ctx.Dispose();
		}
	}

	private void recomposeOverlays()
	{
		TyronThreadPool.QueueTask(delegate
		{
			ComposeValueOverlay();
			ComposeFlashOverlay();
		});
		if (ShowValueOnHover)
		{
			api.Gui.TextTexture.GenOrUpdateTextTexture(onGetStatbarValue(), valueFont, ref valueTexture, new TextBackground
			{
				FillColor = GuiStyle.DialogStrongBgColor,
				Padding = 5,
				BorderWidth = 2.0
			});
		}
	}

	private void ComposeValueOverlay()
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Expected O, but got Unknown
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Expected O, but got Unknown
		Bounds.CalcWorldBounds();
		double num = (double)value / (double)(maxValue - minValue);
		valueHeight = (int)Bounds.OuterHeight + 1;
		ImageSurface surface = new ImageSurface((Format)0, Bounds.OuterWidthInt + 1, valueHeight);
		Context ctx = new Context((Surface)(object)surface);
		double num2 = 0.0;
		if (num > 0.01)
		{
			double num3 = Bounds.OuterWidth * num;
			double x = (rightToLeft ? (Bounds.OuterWidth - num3) : 0.0);
			num2 = (rightToLeft ? (Bounds.OuterWidth - num3) : num3);
			GuiElement.RoundRectangle(ctx, x, 0.0, num3, Bounds.OuterHeight, 1.0);
			ctx.SetSourceRGB(color[0], color[1], color[2]);
			ctx.FillPreserve();
			ctx.SetSourceRGB(color[0] * 0.4, color[1] * 0.4, color[2] * 0.4);
			ctx.LineWidth = GuiElement.scaled(2.0);
			ctx.StrokePreserve();
			SurfaceTransformBlur.BlurFull(surface, 2.0);
			num3 = Bounds.InnerWidth * num;
			x = (rightToLeft ? (Bounds.InnerWidth - num3) : 0.0);
			EmbossRoundRectangleElement(ctx, x, 0.0, num3, Bounds.InnerHeight, inverse: false, 2, 1);
		}
		ImageSurface surfacePrev = null;
		Context ctxPrev = null;
		if (previousValue.HasValue)
		{
			float num4 = previousValue.Value;
			if (num4 > value && (float)(getMs() - visibleSinceMs) < PreviousValueDisplayTime * 1000f)
			{
				double num5 = num4 / (maxValue - minValue);
				if (num5 > 0.01)
				{
					surfacePrev = new ImageSurface((Format)0, Bounds.OuterWidthInt + 1, valueHeight);
					ctxPrev = new Context((Surface)(object)surfacePrev);
					double num6 = Bounds.OuterWidth * num5;
					double x2 = (rightToLeft ? (Bounds.OuterWidth - num6) : 0.0);
					GuiElement.RoundRectangle(ctxPrev, x2, 0.0, num6, Bounds.OuterHeight, 1.0);
					ctxPrev.SetSourceRGB(color[0], color[1], color[2]);
					ctxPrev.FillPreserve();
					ctxPrev.SetSourceRGB(color[0] * 0.4, color[1] * 0.4, color[2] * 0.4);
					ctxPrev.LineWidth = GuiElement.scaled(2.0);
					ctxPrev.StrokePreserve();
					SurfaceTransformBlur.BlurFull(surfacePrev, 2.0);
					num6 = Bounds.InnerWidth * num5;
					x2 = (rightToLeft ? (Bounds.InnerWidth - num6) : 0.0);
					EmbossRoundRectangleElement(ctxPrev, x2, 0.0, num6, Bounds.InnerHeight, inverse: false, 2, 1);
				}
			}
		}
		if (futureValue.HasValue)
		{
			double num7 = (futureValue.Value - value) / (maxValue - minValue);
			if (num7 > 0.01)
			{
				double x3 = num2;
				double num8 = Bounds.OuterWidth * num7;
				if (rightToLeft)
				{
					x3 = num2 - num8;
				}
				GuiElement.RoundRectangle(ctx, x3, 0.0, num8, Bounds.OuterHeight, 1.0);
				ctx.SetSourceRGBA(0.0, 1.0, 0.0, 0.35);
				ctx.FillPreserve();
				ctx.SetSourceRGBA(0.0, 0.5, 0.0, 0.35);
				ctx.LineWidth = GuiElement.scaled(2.0);
				ctx.StrokePreserve();
				SurfaceTransformBlur.BlurFull(surface, 2.0);
				num8 = Bounds.InnerWidth * num;
				x3 = (rightToLeft ? (Bounds.InnerWidth - num8) : 0.0);
				EmbossRoundRectangleElement(ctx, x3, 0.0, num8, Bounds.InnerHeight, inverse: false, 2, 1);
			}
		}
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
		ctx.LineWidth = GuiElement.scaled(2.2);
		int num9 = Math.Min(50, (int)((maxValue - minValue) / lineInterval));
		for (int i = 1; i < num9; i++)
		{
			ctx.NewPath();
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
			double num10 = Bounds.InnerWidth * (double)i / (double)num9;
			ctx.MoveTo(num10, 0.0);
			ctx.LineTo(num10, Math.Max(3.0, Bounds.InnerHeight - 1.0));
			ctx.ClosePath();
			ctx.Stroke();
		}
		api.Event.EnqueueMainThreadTask(delegate
		{
			generateTexture(surface, ref barTexture);
			if (surfacePrev != null)
			{
				generateTexture(surfacePrev, ref previousValueTexture);
				((Surface)surfacePrev).Dispose();
				ctxPrev.Dispose();
				prevValueSize = 1f;
			}
			ctx.Dispose();
			((Surface)surface).Dispose();
		}, "recompstatbar");
	}

	private void ComposeFlashOverlay()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		valueHeight = (int)Bounds.OuterHeight + 1;
		ImageSurface surface = new ImageSurface((Format)0, Bounds.OuterWidthInt + 28, Bounds.OuterHeightInt + 28);
		Context ctx = new Context((Surface)(object)surface);
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		ctx.Paint();
		GuiElement.RoundRectangle(ctx, 12.0, 12.0, Bounds.OuterWidthInt + 4, Bounds.OuterHeightInt + 4, 1.0);
		ctx.SetSourceRGB(color[0], color[1], color[2]);
		ctx.FillPreserve();
		SurfaceTransformBlur.BlurFull(surface, 3.0);
		ctx.Fill();
		SurfaceTransformBlur.BlurFull(surface, 2.0);
		GuiElement.RoundRectangle(ctx, 15.0, 15.0, Bounds.OuterWidthInt - 2, Bounds.OuterHeightInt - 2, 1.0);
		ctx.Operator = (Operator)0;
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		ctx.Fill();
		api.Event.EnqueueMainThreadTask(delegate
		{
			generateTexture(surface, ref flashTexture);
			ctx.Dispose();
			((Surface)surface).Dispose();
		}, "recompstatbar");
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		double renderX = Bounds.renderX;
		double renderY = Bounds.renderY;
		if (value == maxValue && HideWhenFull)
		{
			return;
		}
		if (hideable)
		{
			api.Render.RenderTexture(baseTexture.TextureId, renderX, renderY, Bounds.OuterWidthInt + 1, Bounds.OuterHeightInt + 1);
		}
		float num = 0f;
		if (ShouldFlash)
		{
			FlashTime += 6f * deltaTime;
			num = GameMath.Sin(FlashTime);
			if (num < 0f)
			{
				ShouldFlash = false;
				FlashTime = 0f;
			}
			if (FlashTime < (float)Math.PI / 2f)
			{
				num = Math.Min(1f, num * 3f);
			}
		}
		if (num > 0f)
		{
			api.Render.RenderTexture(flashTexture.TextureId, renderX - 14.0, renderY - 14.0, Bounds.OuterWidthInt + 28, Bounds.OuterHeightInt + 28, 50f, new Vec4f(1.5f, 1f, 1f, num));
		}
		if (previousValue.HasValue && previousValueTexture.TextureId > 0 && (double)prevValueSize > 0.01)
		{
			if ((float)(getMs() - visibleSinceMs) > PreviousValueDisplayTime * 1000f)
			{
				prevValueSize = Math.Max(0f, prevValueSize - deltaTime);
			}
			api.Render.RenderTexture(previousValueTexture.TextureId, renderX, renderY, (float)Bounds.OuterWidthInt * prevValueSize, valueHeight, 50f, new Vec4f(1f, 1f, 1f, 0.35f));
		}
		if (barTexture.TextureId > 0)
		{
			api.Render.RenderTexture(barTexture.TextureId, renderX, renderY, Bounds.OuterWidthInt + 1, valueHeight);
		}
		if (ShowValueOnHover && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			double posX = api.Input.MouseX + 16;
			double posY = api.Input.MouseY + valueTexture.Height - 4;
			api.Render.RenderTexture(valueTexture.TextureId, posX, posY, valueTexture.Width, valueTexture.Height, 2000f);
		}
	}

	public void SetLineInterval(float value)
	{
		lineInterval = value;
	}

	public void SetValue(float value)
	{
		this.value = value;
		valuesSet = true;
		recomposeOverlays();
	}

	public float GetValue()
	{
		return value;
	}

	public void SetValues(float value, float min, float max)
	{
		valuesSet = true;
		this.value = value;
		minValue = min;
		maxValue = max;
		recomposeOverlays();
	}

	public void SetMinMax(float min, float max)
	{
		minValue = min;
		maxValue = max;
		recomposeOverlays();
	}

	public override void Dispose()
	{
		base.Dispose();
		baseTexture?.Dispose();
		barTexture.Dispose();
		previousValueTexture.Dispose();
		flashTexture.Dispose();
		valueTexture.Dispose();
	}

	public void SetFutureValues(float? futureValue, float velocity)
	{
		this.futureValue = futureValue;
		valueChangeVelocity = velocity;
	}

	public void SetPrevValue(float? previousValue, long visibleSinceMs, Func<long> getMs)
	{
		this.previousValue = previousValue;
		this.visibleSinceMs = visibleSinceMs;
		this.getMs = getMs;
	}
}
