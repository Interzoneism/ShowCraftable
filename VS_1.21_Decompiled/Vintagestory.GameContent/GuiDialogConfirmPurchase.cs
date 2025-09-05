using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogConfirmPurchase : GuiDialog
{
	private ModSystemAuction auctionSys;

	private EntityAgent buyerEntity;

	private EntityAgent traderEntity;

	private Auction auction;

	private ElementBounds dialogBounds;

	public override double InputOrder => 0.1;

	public override double DrawOrder => 1.0;

	public override bool UnregisterOnClose => true;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogConfirmPurchase(ICoreClientAPI capi, EntityAgent buyerEntity, EntityAgent auctioneerEntity, Auction auction)
		: base(capi)
	{
		this.buyerEntity = buyerEntity;
		traderEntity = auctioneerEntity;
		this.auction = auction;
		auctionSys = capi.ModLoader.GetModSystem<ModSystemAuction>();
		Init();
	}

	public void Init()
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 30.0, 400.0, 80.0);
		ElementBounds elementBounds2 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds2.BothSizing = ElementSizing.FitToChildren;
		dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		elementBounds2.verticalSizing = ElementSizing.FitToChildren;
		elementBounds2.horizontalSizing = ElementSizing.Fixed;
		elementBounds2.fixedWidth = 300.0;
		int num = auctionSys.DeliveryCostsByDistance(traderEntity.Pos.XYZ, auction.SrcAuctioneerEntityPos);
		RichTextComponentBase[] array = new RichTextComponentBase[2]
		{
			new ItemstackTextComponent(capi, auction.ItemStack, 60.0, 10.0),
			new RichTextComponent(capi, auction.ItemStack.GetName() + "\r\n", CairoFont.WhiteSmallText())
		};
		array = array.Append(VtmlUtil.Richtextify(capi, auction.ItemStack.GetDescription(capi.World, new DummySlot(auction.ItemStack)), CairoFont.WhiteDetailText()));
		CairoFont cairoFont = CairoFont.WhiteDetailText();
		double unscaledFontsize = cairoFont.UnscaledFontsize;
		ItemStack singleCurrencyStack = auctionSys.SingleCurrencyStack;
		RichTextComponentBase[] components = new RichTextComponentBase[2]
		{
			new RichTextComponent(capi, Lang.Get("Delivery: {0}", num), cairoFont)
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
		RichTextComponentBase[] components2 = new RichTextComponentBase[2]
		{
			new RichTextComponent(capi, Lang.Get("Total Cost: {0}", auction.Price + num), cairoFont)
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
		Composers["confirmauctionpurchase"] = capi.Gui.CreateCompo("tradercreateauction-" + buyerEntity.EntityId, dialogBounds).AddShadedDialogBG(elementBounds2).AddDialogTitleBar(Lang.Get("Purchase this item?"), OnCreateAuctionClose)
			.BeginChildElements(elementBounds2)
			.AddRichtext(array, elementBounds, "itemstack");
		Composers["confirmauctionpurchase"].GetRichtext("itemstack").BeforeCalcBounds();
		double num2 = Math.Max(110.0, elementBounds.fixedHeight + 20.0);
		ElementBounds elementBounds3 = ElementBounds.Fixed(0.0, num2, 35.0, 25.0);
		ElementBounds elementBounds4 = ElementBounds.Fixed(0.0, num2 + 3.0, 250.0, 25.0).FixedRightOf(elementBounds3);
		ElementBounds elementBounds5 = ElementBounds.Fixed(0.0, 0.0, 200.0, 30.0).FixedUnder(elementBounds4, 20.0);
		ElementBounds elementBounds6 = ElementBounds.Fixed(0.0, 0.0, 150.0, 30.0).FixedUnder(elementBounds5);
		ElementBounds bounds = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0).FixedUnder(elementBounds6, 15.0);
		ElementBounds bounds2 = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0).FixedUnder(elementBounds6, 15.0);
		Composers["confirmauctionpurchase"].AddSwitch(onDeliveryModeChanged, elementBounds3, "delivery", 25.0).AddStaticText(Lang.Get("Deliver to current trader"), CairoFont.WhiteSmallText(), elementBounds4).AddRichtext(components, elementBounds5, "deliveryCost")
			.AddRichtext(components2, elementBounds6, "totalCost")
			.AddSmallButton(Lang.Get("Cancel"), OnCancel, bounds)
			.AddSmallButton(Lang.Get("Purchase"), OnPurchase, bounds2, EnumButtonStyle.Normal, "buysellButton")
			.EndChildElements()
			.Compose();
		Composers["confirmauctionpurchase"].GetSwitch("delivery").On = true;
	}

	private void onDeliveryModeChanged(bool on)
	{
		int num = auctionSys.DeliveryCostsByDistance(traderEntity.Pos.XYZ, auction.SrcAuctioneerEntityPos);
		GuiElementRichtext richtext = Composers["confirmauctionpurchase"].GetRichtext("totalCost");
		(richtext.Components[0] as RichTextComponent).DisplayText = Lang.Get("Total Cost: {0}", auction.Price + (on ? num : 0));
		richtext.RecomposeText();
	}

	private bool OnCancel()
	{
		TryClose();
		return true;
	}

	private bool OnPurchase()
	{
		auctionSys.BuyAuctionClient(traderEntity, auction.AuctionId, Composers["confirmauctionpurchase"].GetSwitch("delivery").On);
		TryClose();
		return true;
	}

	public override bool CaptureAllInputs()
	{
		return IsOpened();
	}

	private void OnCreateAuctionClose()
	{
		TryClose();
	}

	public override void OnMouseMove(MouseEvent args)
	{
		base.OnMouseMove(args);
		args.Handled = dialogBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY);
	}

	public override void OnKeyDown(KeyEvent args)
	{
		base.OnKeyDown(args);
		if (focused && args.KeyCode == 50)
		{
			TryClose();
		}
		args.Handled = dialogBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY);
	}

	public override void OnKeyUp(KeyEvent args)
	{
		base.OnKeyUp(args);
		args.Handled = dialogBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY);
	}

	public override void OnMouseDown(MouseEvent args)
	{
		base.OnMouseDown(args);
		args.Handled = dialogBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY);
	}

	public override void OnMouseUp(MouseEvent args)
	{
		base.OnMouseUp(args);
		args.Handled = dialogBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY);
	}
}
