using System;
using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementCompactScrollbar : GuiElementScrollbar
{
	public static int scrollbarPadding = 2;

	public override bool Focusable => enabled;

	public GuiElementCompactScrollbar(ICoreClientAPI capi, Action<float> onNewScrollbarValue, ElementBounds bounds)
		: base(capi, onNewScrollbarValue, bounds)
	{
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		ctxStatic.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
		GuiElement.RoundRectangle(ctxStatic, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, 1.0);
		ctxStatic.Fill();
		EmbossRoundRectangleElement(ctxStatic, Bounds, inverse: true, 2, 1);
		RecomposeHandle();
	}

	public override void RecomposeHandle()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		Bounds.CalcWorldBounds();
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.InnerWidth - 1, (int)currentHandleHeight + 1);
		Context val2 = genContext(val);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, Bounds.InnerWidth - 1.0, currentHandleHeight, 2.0);
		val2.SetSourceRGBA(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2], GuiStyle.DialogDefaultBgColor[3]);
		val2.Fill();
		EmbossRoundRectangleElement(val2, 0.0, 0.0, Bounds.InnerWidth - 1.0, currentHandleHeight, inverse: false, 2, 2);
		generateTexture(val, ref handleTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.Render2DTexturePremultipliedAlpha(handleTexture.TextureId, (float)(Bounds.renderX + Bounds.absPaddingX + 1.0), (float)(Bounds.renderY + Bounds.absPaddingY + (double)currentHandlePosition), (float)Bounds.InnerWidth - 1f, currentHandleHeight + 1f, 200f + zOffset);
	}
}
