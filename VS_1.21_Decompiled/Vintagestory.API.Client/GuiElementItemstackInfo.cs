using System;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class GuiElementItemstackInfo : GuiElementTextBase
{
	public bool Dirty;

	public bool Render = true;

	public GuiElementRichtext titleElement;

	public GuiElementRichtext descriptionElement;

	public LoadedTexture texture;

	public static double ItemStackSize = GuiElementPassiveItemSlot.unscaledItemSize * 2.5;

	public static int MarginTop = 24;

	public static int BoxWidth = 415;

	public static int MinBoxHeight = 80;

	private static double[] backTint = GuiStyle.DialogStrongBgColor;

	public ItemSlot curSlot;

	private ItemStack curStack;

	private CairoFont titleFont;

	private double maxWidth;

	private InfoTextDelegate OnRequireInfoText;

	private ElementBounds scissorBounds;

	public Action onRenderStack;

	public string[] RecompCheckIgnoredStackAttributes;

	public GuiElementItemstackInfo(ICoreClientAPI capi, ElementBounds bounds, InfoTextDelegate OnRequireInfoText)
		: base(capi, "", CairoFont.WhiteSmallText(), bounds)
	{
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		this.OnRequireInfoText = OnRequireInfoText;
		texture = new LoadedTexture(capi);
		ElementBounds elementBounds = bounds.CopyOnlySize();
		ElementBounds elementBounds2 = elementBounds.CopyOffsetedSibling(ItemStackSize + 50.0, MarginTop, 0.0 - ItemStackSize - 50.0);
		elementBounds2.WithParent(bounds);
		elementBounds.WithParent(bounds);
		descriptionElement = new GuiElementRichtext(capi, Array.Empty<RichTextComponentBase>(), elementBounds2);
		descriptionElement.zPos = 1001f;
		titleFont = Font.Clone();
		titleFont.FontWeight = (FontWeight)1;
		titleElement = new GuiElementRichtext(capi, Array.Empty<RichTextComponentBase>(), elementBounds);
		titleElement.zPos = 1001f;
		maxWidth = bounds.fixedWidth;
		onRenderStack = delegate
		{
			double num = (int)GuiElement.scaled(30.0 + ItemStackSize / 2.0);
			api.Render.RenderItemstackToGui(curSlot, (double)(int)Bounds.renderX + num, (double)(int)Bounds.renderY + num + (double)(int)GuiElement.scaled(MarginTop), 1000.0 + GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize) * 2.0, (float)GuiElement.scaled(ItemStackSize), -1, shading: true, rotate: true, showStackSize: false);
		};
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
	}

	private void RecalcBounds()
	{
		descriptionElement.BeforeCalcBounds();
		titleElement.BeforeCalcBounds();
		double val = Math.Max(titleElement.MaxLineWidth / (double)RuntimeEnv.GUIScale, descriptionElement.MaxLineWidth / (double)RuntimeEnv.GUIScale + 10.0 + 40.0 + GuiElementPassiveItemSlot.unscaledItemSize * 3.0);
		val = Math.Min(val, maxWidth);
		double fixedWidth = val - ItemStackSize - 50.0;
		Bounds.fixedWidth = val;
		descriptionElement.Bounds.fixedWidth = fixedWidth;
		titleElement.Bounds.fixedWidth = val;
		descriptionElement.Bounds.CalcWorldBounds();
		double num = Math.Max(descriptionElement.Bounds.fixedHeight, 25.0 + GuiElementPassiveItemSlot.unscaledItemSize * 3.0);
		titleElement.Bounds.fixedHeight = num;
		descriptionElement.Bounds.fixedHeight = num;
		Bounds.fixedHeight = 25.0 + num;
	}

	public void AsyncRecompose()
	{
		if (curSlot?.Itemstack == null)
		{
			return;
		}
		Dirty = true;
		string stackName = curSlot.GetStackName();
		string text = OnRequireInfoText(curSlot);
		text.TrimEnd();
		titleElement.Bounds.fixedWidth = maxWidth - 10.0;
		descriptionElement.Bounds.fixedWidth = maxWidth - 40.0 - GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize) * 3.0 - 10.0;
		descriptionElement.Bounds.CalcWorldBounds();
		titleElement.Bounds.CalcWorldBounds();
		titleElement.SetNewTextWithoutRecompose(stackName, titleFont, null, recalcBounds: true);
		descriptionElement.SetNewTextWithoutRecompose(text, Font, null, recalcBounds: true);
		RecalcBounds();
		Bounds.CalcWorldBounds();
		ElementBounds textBounds = Bounds.CopyOnlySize();
		textBounds.CalcWorldBounds();
		TyronThreadPool.QueueTask(delegate
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Expected O, but got Unknown
			//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0203: Expected O, but got Unknown
			ImageSurface surface = new ImageSurface((Format)0, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
			Context ctx = genContext(surface);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
			ctx.Paint();
			ctx.SetSourceRGBA(backTint[0], backTint[1], backTint[2], backTint[3]);
			GuiElement.RoundRectangle(ctx, textBounds.bgDrawX, textBounds.bgDrawY, textBounds.OuterWidthInt, textBounds.OuterHeightInt, GuiStyle.DialogBGRadius);
			ctx.FillPreserve();
			ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor[0] * 1.4, GuiStyle.DialogStrongBgColor[1] * 1.4, GuiStyle.DialogStrongBgColor[2] * 1.4, 1.0);
			ctx.LineWidth = 5.25;
			ctx.StrokePreserve();
			SurfaceTransformBlur.BlurFull(surface, 8.2);
			ctx.SetSourceRGBA(backTint[0] / 2.0, backTint[1] / 2.0, backTint[2] / 2.0, backTint[3]);
			ctx.Stroke();
			int num = (int)(GuiElement.scaled(ItemStackSize) + GuiElement.scaled(40.0));
			int num2 = (int)(GuiElement.scaled(ItemStackSize) + GuiElement.scaled(40.0));
			ImageSurface val = new ImageSurface((Format)0, num, num2);
			Context val2 = genContext(val);
			val2.SetSourceRGBA(GuiStyle.DialogSlotBackColor);
			GuiElement.RoundRectangle(val2, 0.0, 0.0, num, num2, 0.0);
			val2.FillPreserve();
			val2.SetSourceRGBA(GuiStyle.DialogSlotFrontColor);
			val2.LineWidth = 5.0;
			val2.Stroke();
			SurfaceTransformBlur.BlurFull(val, 7.0);
			SurfaceTransformBlur.BlurFull(val, 7.0);
			SurfaceTransformBlur.BlurFull(val, 7.0);
			EmbossRoundRectangleElement(val2, 0.0, 0.0, num, num2, inverse: true);
			ctx.SetSourceSurface((Surface)(object)val, (int)textBounds.drawX, (int)(textBounds.drawY + GuiElement.scaled(MarginTop)));
			ctx.Rectangle(textBounds.drawX, textBounds.drawY + GuiElement.scaled(MarginTop), (double)num, (double)num2);
			ctx.Fill();
			val2.Dispose();
			((Surface)val).Dispose();
			api.Event.EnqueueMainThreadTask(delegate
			{
				titleElement.Compose();
				descriptionElement.Compose();
				generateTexture(surface, ref texture);
				ctx.Dispose();
				((Surface)surface).Dispose();
				double num3 = (int)(30.0 + ItemStackSize / 2.0);
				scissorBounds = ElementBounds.Fixed(4.0 + num3 - ItemStackSize, 2.0 + num3 + (double)MarginTop - ItemStackSize, ItemStackSize + 38.0, ItemStackSize + 38.0).WithParent(Bounds);
				scissorBounds.CalcWorldBounds();
				Dirty = false;
			}, "genstackinfotexture");
		});
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (curSlot?.Itemstack != null && !Dirty && Render)
		{
			api.Render.Render2DTexturePremultipliedAlpha(texture.TextureId, Bounds, 1000f);
			titleElement.RenderInteractiveElements(deltaTime);
			descriptionElement.RenderInteractiveElements(deltaTime);
			api.Render.PushScissor(scissorBounds);
			onRenderStack();
			api.Render.PopScissor();
		}
	}

	public ItemSlot GetSlot()
	{
		return curSlot;
	}

	public bool SetSourceSlot(ItemSlot nowSlot, bool forceRecompose = false)
	{
		bool num = forceRecompose || curStack == null != (nowSlot?.Itemstack == null) || (nowSlot?.Itemstack != null && !nowSlot.Itemstack.Equals(api.World, curStack, RecompCheckIgnoredStackAttributes));
		if (nowSlot?.Itemstack == null)
		{
			curSlot = null;
		}
		if (num)
		{
			curSlot = nowSlot;
			curStack = nowSlot?.Itemstack?.Clone();
			if (nowSlot?.Itemstack == null)
			{
				Bounds.fixedHeight = 0.0;
			}
			AsyncRecompose();
		}
		return num;
	}

	public override void Dispose()
	{
		base.Dispose();
		texture.Dispose();
		descriptionElement?.Dispose();
		titleElement?.Dispose();
	}
}
