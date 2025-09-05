using System;
using Cairo;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementScrollbar : GuiElementControl
{
	public static int DefaultScrollbarWidth = 20;

	public static int DeafultScrollbarPadding = 2;

	protected Action<float> onNewScrollbarValue;

	public bool mouseDownOnScrollbarHandle;

	public int mouseDownStartY;

	protected float visibleHeight;

	protected float totalHeight;

	protected float currentHandlePosition;

	protected float currentHandleHeight;

	public float zOffset;

	protected LoadedTexture handleTexture;

	public override bool Focusable => enabled;

	public float ScrollConversionFactor
	{
		get
		{
			if (Bounds.InnerHeight - (double)currentHandleHeight <= 0.0)
			{
				return 1f;
			}
			float num = (float)(Bounds.InnerHeight - (double)currentHandleHeight);
			return (totalHeight - visibleHeight) / num;
		}
	}

	public float CurrentYPosition
	{
		get
		{
			return currentHandlePosition * ScrollConversionFactor;
		}
		set
		{
			currentHandlePosition = value / ScrollConversionFactor;
		}
	}

	public GuiElementScrollbar(ICoreClientAPI capi, Action<float> onNewScrollbarValue, ElementBounds bounds)
		: base(capi, bounds)
	{
		handleTexture = new LoadedTexture(capi);
		this.onNewScrollbarValue = onNewScrollbarValue;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		ctxStatic.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
		ElementRoundRectangle(ctxStatic, Bounds);
		ctxStatic.Fill();
		EmbossRoundRectangleElement(ctxStatic, Bounds, inverse: true);
		RecomposeHandle();
	}

	public virtual void RecomposeHandle()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		Bounds.CalcWorldBounds();
		int num = (int)Bounds.InnerWidth;
		int num2 = (int)currentHandleHeight;
		ImageSurface val = new ImageSurface((Format)0, num, num2);
		Context val2 = genContext(val);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, num, num2, 1.0);
		val2.SetSourceRGBA(GuiStyle.DialogHighlightColor);
		val2.FillPreserve();
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
		val2.Fill();
		EmbossRoundRectangleElement(val2, 0.0, 0.0, num, num2, inverse: false, 2, 1);
		generateTexture(val, ref handleTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.Render2DTexturePremultipliedAlpha(handleTexture.TextureId, (int)(Bounds.renderX + Bounds.absPaddingX), (int)(Bounds.renderY + Bounds.absPaddingY + (double)currentHandlePosition), (int)Bounds.InnerWidth, (int)currentHandleHeight, 200f + zOffset);
	}

	public void SetHeights(float visibleHeight, float totalHeight)
	{
		this.visibleHeight = visibleHeight;
		SetNewTotalHeight(totalHeight);
	}

	public void SetNewTotalHeight(float totalHeight)
	{
		this.totalHeight = totalHeight;
		float num = GameMath.Clamp(visibleHeight / totalHeight, 0f, 1f);
		currentHandleHeight = Math.Max(10f, (float)((double)num * Bounds.InnerHeight));
		currentHandlePosition = (float)Math.Min(currentHandlePosition, Bounds.InnerHeight - (double)currentHandleHeight);
		TriggerChanged();
		RecomposeHandle();
	}

	public void SetScrollbarPosition(int pos)
	{
		currentHandlePosition = pos;
		onNewScrollbarValue(0f);
	}

	public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
	{
		if (!(Bounds.InnerHeight <= (double)currentHandleHeight + 0.001))
		{
			float num = currentHandlePosition - (float)GuiElement.scaled(102.0) * args.deltaPrecise / ScrollConversionFactor;
			double max = Bounds.InnerHeight - (double)currentHandleHeight;
			currentHandlePosition = (float)GameMath.Clamp(num, 0.0, max);
			TriggerChanged();
			args.SetHandled();
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (!(Bounds.InnerHeight <= (double)currentHandleHeight + 0.001) && Bounds.PointInside(args.X, args.Y))
		{
			mouseDownOnScrollbarHandle = true;
			mouseDownStartY = GameMath.Max(0, args.Y - (int)Bounds.renderY, 0);
			if ((float)mouseDownStartY > currentHandleHeight)
			{
				mouseDownStartY = (int)currentHandleHeight / 2;
			}
			UpdateHandlePositionAbs(args.Y - (int)Bounds.renderY - mouseDownStartY);
			args.Handled = true;
		}
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		mouseDownOnScrollbarHandle = false;
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		if (mouseDownOnScrollbarHandle)
		{
			UpdateHandlePositionAbs(args.Y - (int)Bounds.renderY - mouseDownStartY);
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (hasFocus && (args.KeyCode == 46 || args.KeyCode == 45))
		{
			float num = ((args.KeyCode == 46) ? (-0.5f) : 0.5f);
			float num2 = currentHandlePosition - (float)GuiElement.scaled(102.0) * num / ScrollConversionFactor;
			double max = Bounds.InnerHeight - (double)currentHandleHeight;
			currentHandlePosition = (float)GameMath.Clamp(num2, 0.0, max);
			TriggerChanged();
		}
	}

	private void UpdateHandlePositionAbs(float y)
	{
		double max = Bounds.InnerHeight - (double)currentHandleHeight;
		currentHandlePosition = (float)GameMath.Clamp(y, 0.0, max);
		TriggerChanged();
	}

	public void TriggerChanged()
	{
		onNewScrollbarValue(CurrentYPosition);
	}

	public void ScrollToBottom()
	{
		float num = 1f;
		if (totalHeight < visibleHeight)
		{
			currentHandlePosition = 0f;
			num = 0f;
		}
		else
		{
			currentHandlePosition = (float)(Bounds.InnerHeight - (double)currentHandleHeight);
		}
		float obj = num * (totalHeight - visibleHeight);
		onNewScrollbarValue(obj);
	}

	public void EnsureVisible(double posX, double posY)
	{
		double num = CurrentYPosition;
		double num2 = (double)CurrentYPosition + Bounds.InnerHeight;
		if (posY < num)
		{
			float num3 = (float)(num - posY) / ScrollConversionFactor;
			currentHandlePosition = Math.Max(0f, currentHandlePosition - num3);
			TriggerChanged();
		}
		else if (posY > num2)
		{
			float num4 = (float)(posY - num2) / ScrollConversionFactor;
			currentHandlePosition = (float)Math.Min(Bounds.InnerHeight - (double)currentHandleHeight, currentHandlePosition + num4);
			TriggerChanged();
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		handleTexture.Dispose();
	}
}
