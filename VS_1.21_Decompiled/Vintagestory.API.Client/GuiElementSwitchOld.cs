using System;
using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementSwitchOld : GuiElementTextBase
{
	private Action<bool> handler;

	internal const double unscaledWidth = 60.0;

	internal const double unscaledHandleWidth = 30.0;

	internal const double unscaledHeight = 30.0;

	internal const double unscaledPadding = 3.0;

	private int offHandleTextureId;

	private int onHandleTextureId;

	public bool On;

	public GuiElementSwitchOld(ICoreClientAPI capi, Action<bool> OnToggled, ElementBounds bounds)
		: base(capi, "", null, bounds)
	{
		Font = CairoFont.WhiteSmallText().WithFontSize((float)GuiStyle.SubNormalFontSize);
		handler = OnToggled;
		bounds.fixedWidth = 60.0;
		bounds.fixedHeight = 30.0;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		ctxStatic.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
		GuiElement.RoundRectangle(ctxStatic, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, GuiStyle.ElementBGRadius);
		ctxStatic.Fill();
		EmbossRoundRectangleElement(ctxStatic, Bounds, inverse: true);
		createHandle("0", ref offHandleTextureId);
		createHandle("1", ref onHandleTextureId);
	}

	private void createHandle(string text, ref int textureId)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		double num = GuiElement.scaled(30.0);
		double num2 = GuiElement.scaled(30.0) - 2.0 * GuiElement.scaled(3.0);
		ImageSurface val = new ImageSurface((Format)0, (int)Math.Ceiling(num), (int)Math.Ceiling(num2));
		Context val2 = genContext(val);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, num, num2, 1.0);
		GuiElement.fillWithPattern(api, val2, GuiElement.stoneTextureName);
		EmbossRoundRectangleElement(val2, 0.0, 0.0, num, num2, inverse: false, 2, 1);
		Font.SetupContext(val2);
		generateTexture(val, ref textureId);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		double num = GuiElement.scaled(30.0);
		double height = GuiElement.scaled(30.0) - 2.0 * GuiElement.scaled(3.0);
		double num2 = GuiElement.scaled(3.0);
		api.Render.RenderTexture(On ? onHandleTextureId : offHandleTextureId, Bounds.renderX + (On ? (GuiElement.scaled(60.0) - num - 2.0 * num2) : 0.0) + num2, Bounds.renderY + num2, num, height);
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		On = !On;
		handler(On);
		api.Gui.PlaySound("toggleswitch");
	}
}
