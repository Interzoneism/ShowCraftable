using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AuctionCellEntry : GuiElement, IGuiElementCell, IDisposable
{
	public DummySlot dummySlot;

	private ElementBounds scissorBounds;

	public Auction auction;

	public LoadedTexture hoverTexture;

	private float unscaledIconSize = 35f;

	private float iconSize;

	private double unScaledCellHeight = 35.0;

	private GuiElementRichtext stackNameTextElem;

	private GuiElementRichtext priceTextElem;

	private GuiElementRichtext expireTextElem;

	private GuiElementRichtext sellerTextElem;

	private bool composed;

	public bool Selected;

	private Action<int> onClick;

	private float accum1Sec;

	private string prevExpireText;

	public bool Visible => true;

	ElementBounds IGuiElementCell.Bounds => Bounds;

	public AuctionCellEntry(ICoreClientAPI capi, InventoryBase inventoryAuction, ElementBounds bounds, Auction auction, Action<int> onClick)
		: base(capi, bounds)
	{
		iconSize = (float)GuiElement.scaled(unscaledIconSize);
		dummySlot = new DummySlot(auction.ItemStack, inventoryAuction);
		this.onClick = onClick;
		this.auction = auction;
		CairoFont cairoFont = CairoFont.WhiteDetailText();
		double fixedY = (unScaledCellHeight - cairoFont.UnscaledFontsize) / 2.0;
		scissorBounds = ElementBounds.FixedSize(unscaledIconSize, unscaledIconSize).WithParent(Bounds);
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, fixedY, 270.0, 25.0).WithParent(Bounds).FixedRightOf(scissorBounds, 10.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, fixedY, 75.0, 25.0).WithParent(Bounds).FixedRightOf(elementBounds, 10.0);
		ElementBounds elementBounds3 = ElementBounds.Fixed(0.0, 0.0, 160.0, 25.0).WithParent(Bounds).FixedRightOf(elementBounds2, 10.0);
		ElementBounds bounds2 = ElementBounds.Fixed(0.0, fixedY, 110.0, 25.0).WithParent(Bounds).FixedRightOf(elementBounds3, 10.0);
		stackNameTextElem = new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, dummySlot.Itemstack.GetName(), cairoFont), elementBounds);
		double unscaledFontsize = cairoFont.UnscaledFontsize;
		ItemStack singleCurrencyStack = capi.ModLoader.GetModSystem<ModSystemAuction>().SingleCurrencyStack;
		RichTextComponentBase[] components = new RichTextComponentBase[2]
		{
			new RichTextComponent(capi, auction.Price.ToString() ?? "", cairoFont)
			{
				PaddingRight = 10.0,
				VerticalAlign = EnumVerticalAlign.Top
			},
			new ItemstackTextComponent(capi, singleCurrencyStack, unscaledFontsize * 2.5, 0.0, EnumFloat.Inline)
			{
				VerticalAlign = EnumVerticalAlign.Top,
				offX = 0.0 - GuiElement.scaled(unscaledFontsize * 0.5),
				offY = 0.0 - GuiElement.scaled(unscaledFontsize * 0.75)
			}
		};
		priceTextElem = new GuiElementRichtext(capi, components, elementBounds2);
		expireTextElem = new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, prevExpireText = auction.GetExpireText(capi), cairoFont.Clone().WithFontSize(14f)), elementBounds3);
		expireTextElem.BeforeCalcBounds();
		elementBounds3.fixedY = 5.0 + (25.0 - expireTextElem.TotalHeight / (double)RuntimeEnv.GUIScale) / 2.0;
		sellerTextElem = new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, auction.SellerName, cairoFont.Clone().WithOrientation(EnumTextOrientation.Right)), bounds2);
		hoverTexture = new LoadedTexture(capi);
	}

	public void Recompose()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		composed = true;
		stackNameTextElem.Compose();
		priceTextElem.Compose();
		expireTextElem.Compose();
		sellerTextElem.Compose();
		ImageSurface val = new ImageSurface((Format)0, 2, 2);
		Context obj = genContext(val);
		obj.NewPath();
		obj.LineTo(0.0, 0.0);
		obj.LineTo(2.0, 0.0);
		obj.LineTo(2.0, 2.0);
		obj.LineTo(0.0, 2.0);
		obj.ClosePath();
		obj.SetSourceRGBA(0.0, 0.0, 0.0, 0.15);
		obj.Fill();
		generateTexture(val, ref hoverTexture);
		obj.Dispose();
		((Surface)val).Dispose();
	}

	public void OnRenderInteractiveElements(ICoreClientAPI api, float deltaTime)
	{
		if (!composed)
		{
			Recompose();
		}
		accum1Sec += deltaTime;
		if (accum1Sec > 1f)
		{
			string expireText = auction.GetExpireText(api);
			if (expireText != prevExpireText)
			{
				expireTextElem.Components = VtmlUtil.Richtextify(api, expireText, CairoFont.WhiteDetailText().WithFontSize(14f));
				expireTextElem.RecomposeText();
				prevExpireText = expireText;
			}
		}
		if (scissorBounds.InnerWidth <= 0.0 || scissorBounds.InnerHeight <= 0.0)
		{
			return;
		}
		api.Render.PushScissor(scissorBounds, stacking: true);
		api.Render.RenderItemstackToGui(dummySlot, scissorBounds.renderX + (double)(iconSize / 2f), scissorBounds.renderY + (double)(iconSize / 2f), 100.0, iconSize * 0.55f, -1);
		api.Render.PopScissor();
		api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, scissorBounds.renderX, scissorBounds.renderY, scissorBounds.OuterWidth, scissorBounds.OuterHeight);
		stackNameTextElem.RenderInteractiveElements(deltaTime);
		priceTextElem.RenderInteractiveElements(deltaTime);
		expireTextElem.RenderInteractiveElements(deltaTime);
		MouseOverCursor = expireTextElem.MouseOverCursor;
		sellerTextElem.RenderInteractiveElements(deltaTime);
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		Vec2d vec2d = Bounds.PositionInside(mouseX, mouseY);
		if (Selected || (vec2d != null && IsPositionInside(api.Input.MouseX, api.Input.MouseY)))
		{
			api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds.absX, Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			if (Selected)
			{
				api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds.absX, Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			}
		}
	}

	public void OnMouseMoveOnElement(MouseEvent args, int elementIndex)
	{
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		if (scissorBounds.PositionInside(mouseX, mouseY) != null)
		{
			api.Input.TriggerOnMouseEnterSlot(dummySlot);
		}
		else
		{
			api.Input.TriggerOnMouseLeaveSlot(dummySlot);
		}
		args.Handled = true;
	}

	public void OnMouseDownOnElement(MouseEvent args, int elementIndex)
	{
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		if (expireTextElem.Bounds.PointInside(mouseX, mouseY))
		{
			expireTextElem.OnMouseDownOnElement(api, args);
		}
	}

	public void UpdateCellHeight()
	{
		Bounds.CalcWorldBounds();
		scissorBounds.CalcWorldBounds();
		stackNameTextElem.BeforeCalcBounds();
		priceTextElem.BeforeCalcBounds();
		expireTextElem.BeforeCalcBounds();
		sellerTextElem.BeforeCalcBounds();
		Bounds.fixedHeight = unScaledCellHeight;
	}

	public void OnMouseUpOnElement(MouseEvent args, int elementIndex)
	{
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		if (expireTextElem.Bounds.PointInside(mouseX, mouseY))
		{
			expireTextElem.OnMouseUp(api, args);
		}
		if (!args.Handled)
		{
			onClick?.Invoke(elementIndex);
		}
	}

	public override void Dispose()
	{
		stackNameTextElem.Dispose();
		priceTextElem.Dispose();
		expireTextElem.Dispose();
	}
}
