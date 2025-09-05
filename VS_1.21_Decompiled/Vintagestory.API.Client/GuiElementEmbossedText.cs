using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementEmbossedText : GuiElementTextBase
{
	public static int Padding = 4;

	private LoadedTexture texture;

	public GuiElementEmbossedText(ICoreClientAPI capi, string text, CairoFont font, ElementBounds bounds)
		: base(capi, text, font, bounds)
	{
		texture = new LoadedTexture(capi);
		enabled = true;
	}

	public bool IsEnabled()
	{
		return enabled;
	}

	public void SetEnabled(bool enabled)
	{
		base.enabled = enabled;
		Compose();
	}

	public override void ComposeTextElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		Compose();
	}

	internal void AutoBoxSize()
	{
		Font.AutoBoxSize(text, Bounds);
		Bounds.FixedGrow(2 * Padding);
	}

	private void Compose()
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Expected O, but got Unknown
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		Bounds.CalcWorldBounds();
		double num = GuiElement.scaled(1.5) / 2.0;
		double num2 = (0.0 - GuiElement.scaled(1.5)) / 2.0;
		double num3 = GuiElement.scaled(Padding);
		if (!enabled)
		{
			num /= 2.0;
			num2 /= 2.0;
		}
		ImageSurface val = new ImageSurface((Format)0, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
		Context val2 = new Context((Surface)(object)val);
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		val2.Paint();
		val2.Operator = (Operator)1;
		val2.Antialias = (Antialias)6;
		Font.Color = new double[4] { 0.0, 0.0, 0.0, 0.4 };
		Font.SetupContext(val2);
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.95);
		val2.MoveTo(num3 + num, num3 + num);
		DrawTextLineAt(val2, text, 0.0, 0.0);
		val2.SetSourceRGBA(255.0, 255.0, 255.0, enabled ? 0.95 : 0.5);
		val2.MoveTo(num3 + num2, num3 + num2);
		DrawTextLineAt(val2, text, 0.0, 0.0);
		SurfaceTransformBlur.BlurPartial(val, 3.0, 6);
		val2.Operator = (Operator)1;
		if (enabled)
		{
			Font.Color = new double[4] { 0.0, 0.0, 0.0, 0.5 };
		}
		else
		{
			Font.Color = new double[4] { 0.5, 0.5, 0.5, 0.75 };
		}
		Font.SetupContext(val2);
		val2.MoveTo(num3, num3);
		DrawTextLineAt(val2, text, 0.0, 0.0);
		generateTexture(val, ref texture);
		((Surface)val).Dispose();
		val2.Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.Render2DTexturePremultipliedAlpha(texture.TextureId, Bounds);
	}

	public override void Dispose()
	{
		base.Dispose();
		texture.Dispose();
	}
}
