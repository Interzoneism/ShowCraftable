using System;
using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementSwitch : GuiElementControl
{
	private Action<bool> handler;

	private LoadedTexture onTexture;

	public bool On;

	internal double unscaledPadding;

	internal double unscaledSize;

	public override bool Focusable => enabled;

	public GuiElementSwitch(ICoreClientAPI capi, Action<bool> OnToggled, ElementBounds bounds, double size = 30.0, double padding = 4.0)
		: base(capi, bounds)
	{
		onTexture = new LoadedTexture(capi);
		bounds.fixedWidth = size;
		bounds.fixedHeight = size;
		unscaledPadding = padding;
		unscaledSize = size;
		handler = OnToggled;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		ctxStatic.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
		GuiElement.RoundRectangle(ctxStatic, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, 1.0);
		ctxStatic.Fill();
		EmbossRoundRectangleElement(ctxStatic, Bounds, inverse: true, 1, 1);
		genOnTexture();
	}

	private void genOnTexture()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		double num = GuiElement.scaled(unscaledSize - 2.0 * unscaledPadding);
		ImageSurface val = new ImageSurface((Format)0, (int)num, (int)num);
		Context val2 = genContext(val);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, num, num, 1.0);
		if (enabled)
		{
			val2.SetSourceRGBA(0.0, 0.0, 0.0, 1.0);
		}
		else
		{
			val2.SetSourceRGBA(0.15, 0.15, 0.0, 0.65);
		}
		val2.FillPreserve();
		GuiElement.fillWithPattern(api, val2, GuiElement.waterTextureName, nearestScalingFiler: false, preserve: true, enabled ? 255 : 127, 0.5f);
		generateTexture(val, ref onTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (On)
		{
			double num = GuiElement.scaled(unscaledPadding);
			api.Render.Render2DLoadedTexture(onTexture, (int)(Bounds.renderX + num), (int)(Bounds.renderY + num));
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		if (enabled)
		{
			On = !On;
			handler?.Invoke(On);
			api.Gui.PlaySound("toggleswitch");
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (base.HasFocus && (args.KeyCode == 49 || args.KeyCode == 51))
		{
			args.Handled = true;
			On = !On;
			handler?.Invoke(On);
			api.Gui.PlaySound("toggleswitch");
		}
	}

	public void SetValue(bool on)
	{
		On = on;
	}

	public override void Dispose()
	{
		base.Dispose();
		onTexture.Dispose();
	}
}
