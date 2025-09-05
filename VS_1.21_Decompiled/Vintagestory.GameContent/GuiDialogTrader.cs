using System.Collections.Generic;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogTrader : GuiDialog
{
	private InventoryTrader traderInventory;

	private EntityAgent owningEntity;

	private double prevPlrAbsFixedX;

	private double prevPlrAbsFixedY;

	private double prevTdrAbsFixedX;

	private double prevTdrAbsFixedY;

	private double notifyPlayerMoneyTextSeconds;

	private double notifyTraderMoneyTextSeconds;

	private int rows = 4;

	private int cols = 4;

	private int curTab;

	private ModSystemAuction auctionSys;

	private InventoryGeneric auctionSlotInv;

	private GuiElementCellList<Auction> listElem;

	private List<Auction> auctions;

	private ElementBounds clipBounds;

	private AuctionCellEntry selectedElem;

	public override string ToggleKeyCombinationCode => null;

	private bool auctionHouseEnabled => capi.World.Config.GetBool("auctionHouse", defaultValue: true);

	public override bool PrefersUngrabbedMouse => false;

	public override float ZSize => 300f;

	public GuiDialogTrader(InventoryTrader traderInventory, EntityAgent owningEntity, ICoreClientAPI capi, int rows = 4, int cols = 4)
		: base(capi)
	{
		auctionSys = capi.ModLoader.GetModSystem<ModSystemAuction>();
		auctionSys.OnCellUpdateClient = delegate
		{
			listElem?.ReloadCells(auctions);
			updateScrollbarBounds();
		};
		auctionSys.curTraderClient = owningEntity as EntityTradingHumanoid;
		this.traderInventory = traderInventory;
		this.owningEntity = owningEntity;
		this.rows = rows;
		this.cols = cols;
		if (!auctionSys.createAuctionSlotByPlayer.TryGetValue(capi.World.Player.PlayerUID, out auctionSlotInv))
		{
			auctionSys.createAuctionSlotByPlayer[capi.World.Player.PlayerUID] = (auctionSlotInv = new InventoryGeneric(1, "auctionslot-" + capi.World.Player.PlayerUID, capi));
			auctionSlotInv.OnGetSuitability = (ItemSlot s, ItemSlot t, bool isMerge) => -1f;
		}
		capi.Network.SendPacketClient(capi.World.Player.InventoryManager.OpenInventory(auctionSlotInv));
		Compose();
	}

	public void Compose()
	{
		//IL_0ae0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ae5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0afe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b03: Unknown result type (might be due to invalid IL or missing references)
		GuiTab[] tabs = new GuiTab[3]
		{
			new GuiTab
			{
				Name = Lang.Get("Local goods"),
				DataInt = 0
			},
			new GuiTab
			{
				Name = Lang.Get("Auction house"),
				DataInt = 1
			},
			new GuiTab
			{
				Name = Lang.Get("Your Auctions"),
				DataInt = 2
			}
		};
		ElementBounds bounds = ElementBounds.Fixed(0.0, -24.0, 500.0, 25.0);
		CairoFont cairoFont = CairoFont.WhiteDetailText();
		if (!auctionHouseEnabled)
		{
			tabs = new GuiTab[1]
			{
				new GuiTab
				{
					Name = Lang.Get("Local goods"),
					DataInt = 0
				}
			};
		}
		ElementBounds elementBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds bounds2 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		ElementBounds elementBounds2 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds elementBounds3 = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		string displayName = owningEntity.GetBehavior<EntityBehaviorNameTag>().DisplayName;
		string text = Lang.Get("tradingwindow-" + owningEntity.Code.Path, displayName);
		if (curTab > 0)
		{
			text = Lang.Get("tradertabtitle-" + curTab);
		}
		base.SingleComposer = capi.Gui.CreateCompo("traderdialog-" + owningEntity.EntityId, bounds2).AddShadedDialogBG(elementBounds).AddDialogTitleBar(text, OnTitleBarClose)
			.AddHorizontalTabs(tabs, bounds, OnTabClicked, cairoFont, cairoFont.Clone().WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
			.BeginChildElements(elementBounds);
		base.SingleComposer.GetHorizontalTabs("tabs").activeElement = curTab;
		if (curTab == 0)
		{
			double unscaledSlotPadding = GuiElementItemSlotGridBase.unscaledSlotPadding;
			ElementBounds elementBounds4 = ElementStdBounds.SlotGrid(EnumDialogArea.None, unscaledSlotPadding, 70.0 + unscaledSlotPadding, cols, rows).FixedGrow(2.0 * unscaledSlotPadding, 2.0 * unscaledSlotPadding);
			ElementBounds elementBounds5 = ElementStdBounds.SlotGrid(EnumDialogArea.None, unscaledSlotPadding + elementBounds4.fixedWidth + 20.0, 70.0 + unscaledSlotPadding, cols, rows).FixedGrow(2.0 * unscaledSlotPadding, 2.0 * unscaledSlotPadding);
			ElementBounds bounds3 = ElementStdBounds.SlotGrid(EnumDialogArea.None, unscaledSlotPadding + elementBounds4.fixedWidth + 20.0, 15.0 + unscaledSlotPadding, cols, 1).FixedGrow(2.0 * unscaledSlotPadding, 2.0 * unscaledSlotPadding).FixedUnder(elementBounds5, 5.0);
			ElementBounds elementBounds6 = ElementStdBounds.SlotGrid(EnumDialogArea.None, unscaledSlotPadding, 15.0 + unscaledSlotPadding, cols, 1).FixedGrow(2.0 * unscaledSlotPadding, 2.0 * unscaledSlotPadding).FixedUnder(elementBounds4, 5.0);
			ElementBounds elementBounds7 = ElementBounds.Fixed(unscaledSlotPadding, 85.0 + 2.0 * unscaledSlotPadding + elementBounds4.fixedHeight + elementBounds6.fixedHeight, 200.0, 25.0);
			ElementBounds elementBounds8 = ElementBounds.Fixed(elementBounds4.fixedWidth + unscaledSlotPadding + 20.0, 85.0 + 2.0 * unscaledSlotPadding + elementBounds4.fixedHeight + elementBounds6.fixedHeight, 200.0, 25.0);
			ElementBounds elementBounds9 = elementBounds8.FlatCopy().WithFixedOffset(0.0, elementBounds8.fixedHeight);
			ElementBounds elementBounds10 = elementBounds7.FlatCopy().WithFixedOffset(0.0, elementBounds7.fixedHeight);
			CairoFont cairoFont2 = CairoFont.WhiteDetailText();
			cairoFont2.Color[3] *= 0.7;
			base.SingleComposer.AddStaticText(Lang.Get("trader-newgoodsdelivery", (owningEntity as EntityTradingHumanoid).NextRefreshTotalDays()), cairoFont2, ElementBounds.Fixed(unscaledSlotPadding, 20.0 + unscaledSlotPadding, 430.0, 25.0)).AddStaticText(Lang.Get("You can Buy"), CairoFont.WhiteDetailText(), ElementBounds.Fixed(unscaledSlotPadding, 50.0 + unscaledSlotPadding, 200.0, 25.0)).AddStaticText(Lang.Get("You can Sell"), CairoFont.WhiteDetailText(), ElementBounds.Fixed(elementBounds4.fixedWidth + unscaledSlotPadding + 20.0, 50.0 + unscaledSlotPadding, 200.0, 25.0))
				.AddItemSlotGrid(traderInventory, DoSendPacket, cols, new int[rows * cols].Fill((int i) => i), elementBounds4, "traderSellingSlots")
				.AddItemSlotGrid(traderInventory, DoSendPacket, cols, new int[cols].Fill((int i) => rows * cols + i), elementBounds6, "playerBuyingSlots")
				.AddItemSlotGrid(traderInventory, DoSendPacket, cols, new int[rows * cols].Fill((int i) => rows * cols + cols + i), elementBounds5, "traderBuyingSlots")
				.AddItemSlotGrid(traderInventory, DoSendPacket, cols, new int[cols].Fill((int i) => rows * cols + cols + rows * cols + i), bounds3, "playerSellingSlots")
				.AddStaticText(Lang.Get("trader-yourselection"), CairoFont.WhiteDetailText(), ElementBounds.Fixed(unscaledSlotPadding, 70.0 + 2.0 * unscaledSlotPadding + elementBounds4.fixedHeight, 150.0, 25.0))
				.AddStaticText(Lang.Get("trader-youroffer"), CairoFont.WhiteDetailText(), ElementBounds.Fixed(elementBounds4.fixedWidth + unscaledSlotPadding + 20.0, 70.0 + 2.0 * unscaledSlotPadding + elementBounds4.fixedHeight, 150.0, 25.0))
				.AddDynamicText("", CairoFont.WhiteDetailText(), elementBounds7, "costText")
				.AddDynamicText("", CairoFont.WhiteDetailText(), elementBounds10, "playerMoneyText")
				.AddDynamicText("", CairoFont.WhiteDetailText(), elementBounds8, "gainText")
				.AddDynamicText("", CairoFont.WhiteDetailText(), elementBounds9, "traderMoneyText")
				.AddSmallButton(Lang.Get("Goodbye!"), OnByeClicked, elementBounds2.FixedUnder(elementBounds10, 20.0))
				.AddSmallButton(Lang.Get("Buy / Sell"), OnBuySellClicked, elementBounds3.FixedUnder(elementBounds9, 20.0), EnumButtonStyle.Normal, "buysellButton")
				.EndChildElements()
				.Compose();
			base.SingleComposer.GetButton("buysellButton").Enabled = false;
			CalcAndUpdateAssetsDisplay();
			return;
		}
		double fixedHeight = 377.0;
		ElementBounds elementBounds11 = ElementBounds.Fixed(0.0, 25.0, 700.0, fixedHeight);
		clipBounds = elementBounds11.ForkBoundingParent();
		ElementBounds elementBounds12 = elementBounds11.FlatCopy().FixedGrow(3.0).WithFixedOffset(0.0, 0.0);
		ElementBounds bounds4 = elementBounds12.CopyOffsetedSibling(3.0 + elementBounds11.fixedWidth + 7.0).WithFixedWidth(20.0);
		if (curTab == 1)
		{
			auctions = auctionSys.activeAuctions;
			base.SingleComposer.BeginClip(clipBounds).AddInset(elementBounds12, 3).AddCellList(elementBounds11, createCell, auctionSys.activeAuctions, "stacklist")
				.EndClip()
				.AddVerticalScrollbar(OnNewScrollbarValue, bounds4, "scrollbar")
				.AddSmallButton(Lang.Get("Goodbye!"), OnByeClicked, elementBounds2.FixedUnder(clipBounds, 20.0))
				.AddSmallButton(Lang.Get("Buy"), OnBuyAuctionClicked, elementBounds3.FixedUnder(clipBounds, 20.0), EnumButtonStyle.Normal, "buyauction");
		}
		if (curTab == 2)
		{
			auctions = auctionSys.ownAuctions;
			ElementBounds elementBounds13 = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
			string text2 = Lang.Get("Place Auction");
			string text3 = Lang.Get("Cancel Auction");
			TextExtents textExtents = CairoFont.ButtonText().GetTextExtents(text2);
			double num = ((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale;
			textExtents = CairoFont.ButtonText().GetTextExtents(text3);
			_ = ((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale;
			base.SingleComposer.BeginClip(clipBounds).AddInset(elementBounds12, 3).AddCellList(elementBounds11, createCell, auctionSys.ownAuctions, "stacklist")
				.EndClip()
				.AddVerticalScrollbar(OnNewScrollbarValue, bounds4, "scrollbar")
				.AddSmallButton(Lang.Get("Goodbye!"), OnByeClicked, elementBounds2.FixedUnder(clipBounds, 20.0))
				.AddSmallButton(Lang.Get("Place Auction"), OnCreateAuction, elementBounds3.FixedUnder(clipBounds, 20.0), EnumButtonStyle.Normal, "placeAuction")
				.AddSmallButton(text3, OnCancelAuction, elementBounds13.FlatCopy().FixedUnder(clipBounds, 20.0).WithFixedAlignmentOffset(0.0 - num, 0.0), EnumButtonStyle.Normal, "cancelAuction")
				.AddSmallButton(Lang.Get("Collect Funds"), OnCollectFunds, elementBounds13.FlatCopy().FixedUnder(clipBounds, 20.0).WithFixedAlignmentOffset(0.0 - num, 0.0), EnumButtonStyle.Normal, "collectFunds")
				.AddSmallButton(Lang.Get("Retrieve Items"), OnRetrieveItems, elementBounds13.FixedUnder(clipBounds, 20.0).WithFixedAlignmentOffset(0.0 - num, 0.0), EnumButtonStyle.Normal, "retrieveItems");
		}
		if (curTab == 1 || curTab == 2)
		{
			selectedElem = null;
			listElem = base.SingleComposer.GetCellList<Auction>("stacklist");
			listElem.BeforeCalcBounds();
			listElem.UnscaledCellVerPadding = 0;
			listElem.unscaledCellSpacing = 5;
			base.SingleComposer.EndChildElements().Compose();
			updateScrollbarBounds();
			didClickAuctionElem(-1);
		}
	}

	private void updateScrollbarBounds()
	{
		if (listElem != null)
		{
			base.SingleComposer.GetScrollbar("scrollbar")?.Bounds.CalcWorldBounds();
			base.SingleComposer.GetScrollbar("scrollbar")?.SetHeights((float)clipBounds.fixedHeight, (float)listElem.Bounds.fixedHeight);
		}
	}

	private void OnNewScrollbarValue(float value)
	{
		listElem = base.SingleComposer.GetCellList<Auction>("stacklist");
		listElem.Bounds.fixedY = 0f - value;
		listElem.Bounds.CalcWorldBounds();
	}

	private bool OnCancelAuction()
	{
		if (selectedElem?.auction == null)
		{
			return false;
		}
		auctionSys.RetrieveAuctionClient(owningEntity, selectedElem.auction.AuctionId);
		return true;
	}

	private bool OnBuyAuctionClicked()
	{
		if (selectedElem?.auction == null)
		{
			return false;
		}
		object obj = capi.OpenedGuis.FirstOrDefault((object d) => d is GuiDialogConfirmPurchase);
		if (obj != null)
		{
			(obj as GuiDialog).Focus();
			return true;
		}
		new GuiDialogConfirmPurchase(capi, capi.World.Player.Entity, owningEntity, selectedElem.auction).TryOpen();
		return true;
	}

	private bool OnCollectFunds()
	{
		if (selectedElem?.auction == null)
		{
			return false;
		}
		auctionSys.RetrieveAuctionClient(owningEntity, selectedElem.auction.AuctionId);
		return true;
	}

	private bool OnRetrieveItems()
	{
		if (selectedElem?.auction == null)
		{
			return false;
		}
		auctionSys.RetrieveAuctionClient(owningEntity, selectedElem.auction.AuctionId);
		return true;
	}

	private IGuiElementCell createCell(Auction auction, ElementBounds bounds)
	{
		bounds.fixedPaddingY = 0.0;
		return new AuctionCellEntry(capi, auctionSlotInv, bounds, auction, didClickAuctionElem);
	}

	private void didClickAuctionElem(int index)
	{
		if (selectedElem != null)
		{
			selectedElem.Selected = false;
		}
		if (index >= 0)
		{
			selectedElem = base.SingleComposer.GetCellList<Auction>("stacklist").elementCells[index] as AuctionCellEntry;
			selectedElem.Selected = true;
		}
		if (curTab == 2)
		{
			Auction auction = selectedElem?.auction;
			bool flag = (auction != null && auction.State == EnumAuctionState.Sold) || (auction != null && auction.State == EnumAuctionState.SoldRetrieved);
			base.SingleComposer.GetButton("cancelAuction").Visible = auction != null && auction.State == EnumAuctionState.Active;
			base.SingleComposer.GetButton("retrieveItems").Visible = (auction != null && auction.State == EnumAuctionState.Expired) || (flag && auction.SellerUid != capi.World.Player.PlayerUID);
			base.SingleComposer.GetButton("collectFunds").Visible = flag && auction.SellerUid == capi.World.Player.PlayerUID;
		}
	}

	private bool OnCreateAuction()
	{
		object obj = capi.OpenedGuis.FirstOrDefault((object d) => d is GuiDialogCreateAuction);
		if (obj != null)
		{
			(obj as GuiDialog).Focus();
			return true;
		}
		new GuiDialogCreateAuction(capi, owningEntity, auctionSlotInv).TryOpen();
		return true;
	}

	private void OnTabClicked(int tab)
	{
		curTab = tab;
		Compose();
	}

	private void CalcAndUpdateAssetsDisplay()
	{
		int playerAssets = InventoryTrader.GetPlayerAssets(capi.World.Player.Entity);
		base.SingleComposer.GetDynamicText("playerMoneyText")?.SetNewText(Lang.Get("You have {0} Gears", playerAssets));
		int traderAssets = traderInventory.GetTraderAssets();
		base.SingleComposer.GetDynamicText("traderMoneyText")?.SetNewText(Lang.Get("{0} has {1} Gears", owningEntity.GetBehavior<EntityBehaviorNameTag>().DisplayName, traderAssets));
	}

	private void TraderInventory_SlotModified(int slotid)
	{
		int totalCost = traderInventory.GetTotalCost();
		int totalGain = traderInventory.GetTotalGain();
		base.SingleComposer.GetDynamicText("costText")?.SetNewText((totalCost > 0) ? Lang.Get("Total Cost: {0} Gears", totalCost) : "");
		base.SingleComposer.GetDynamicText("gainText")?.SetNewText((totalGain > 0) ? Lang.Get("Total Gain: {0} Gears", totalGain) : "");
		if (base.SingleComposer.GetButton("buysellButton") != null)
		{
			base.SingleComposer.GetButton("buysellButton").Enabled = totalCost > 0 || totalGain > 0;
			CalcAndUpdateAssetsDisplay();
		}
	}

	private bool OnBuySellClicked()
	{
		EnumTransactionResult num = traderInventory.TryBuySell(capi.World.Player);
		if (num == EnumTransactionResult.Success)
		{
			capi.Gui.PlaySound(new AssetLocation("sounds/effect/cashregister"), randomizePitch: false, 0.25f);
			(owningEntity as EntityTradingHumanoid).TalkUtil?.Talk(EnumTalkType.Purchase);
		}
		if (num == EnumTransactionResult.PlayerNotEnoughAssets)
		{
			(owningEntity as EntityTradingHumanoid).TalkUtil?.Talk(EnumTalkType.Complain);
			if (notifyPlayerMoneyTextSeconds <= 0.0)
			{
				prevPlrAbsFixedX = base.SingleComposer.GetDynamicText("playerMoneyText").Bounds.absFixedX;
				prevPlrAbsFixedY = base.SingleComposer.GetDynamicText("playerMoneyText").Bounds.absFixedY;
			}
			notifyPlayerMoneyTextSeconds = 1.5;
		}
		if (num == EnumTransactionResult.TraderNotEnoughAssets)
		{
			(owningEntity as EntityTradingHumanoid).TalkUtil?.Talk(EnumTalkType.Complain);
			if (notifyTraderMoneyTextSeconds <= 0.0)
			{
				prevTdrAbsFixedX = base.SingleComposer.GetDynamicText("traderMoneyText").Bounds.absFixedX;
				prevTdrAbsFixedY = base.SingleComposer.GetDynamicText("traderMoneyText").Bounds.absFixedY;
			}
			notifyTraderMoneyTextSeconds = 1.5;
		}
		if (num == EnumTransactionResult.TraderNotEnoughSupplyOrDemand)
		{
			(owningEntity as EntityTradingHumanoid).TalkUtil?.Talk(EnumTalkType.Complain);
		}
		capi.Network.SendEntityPacket(owningEntity.EntityId, 1000);
		TraderInventory_SlotModified(0);
		CalcAndUpdateAssetsDisplay();
		return true;
	}

	private bool OnByeClicked()
	{
		TryClose();
		return true;
	}

	private void DoSendPacket(object p)
	{
		capi.Network.SendEntityPacket(owningEntity.EntityId, p);
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		traderInventory.SlotModified += TraderInventory_SlotModified;
		auctionSys.DidEnterAuctionHouse();
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		traderInventory.SlotModified -= TraderInventory_SlotModified;
		(owningEntity as EntityTradingHumanoid).TalkUtil?.Talk(EnumTalkType.Goodbye);
		capi.World.Player.InventoryManager.CloseInventoryAndSync(traderInventory);
		base.SingleComposer.GetSlotGrid("traderSellingSlots")?.OnGuiClosed(capi);
		base.SingleComposer.GetSlotGrid("playerBuyingSlots")?.OnGuiClosed(capi);
		base.SingleComposer.GetSlotGrid("traderBuyingSlots")?.OnGuiClosed(capi);
		base.SingleComposer.GetSlotGrid("playerSellingSlots")?.OnGuiClosed(capi);
		auctionSlotInv[0].Itemstack = null;
		capi.World.Player.InventoryManager.CloseInventoryAndSync(auctionSlotInv);
		auctionSys.DidLeaveAuctionHouse();
	}

	public override void OnBeforeRenderFrame3D(float deltaTime)
	{
		base.OnBeforeRenderFrame3D(deltaTime);
		if (notifyPlayerMoneyTextSeconds > 0.0)
		{
			notifyPlayerMoneyTextSeconds -= deltaTime;
			GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("playerMoneyText");
			if (dynamicText != null)
			{
				if (notifyPlayerMoneyTextSeconds <= 0.0)
				{
					dynamicText.Bounds.absFixedX = prevPlrAbsFixedX;
					dynamicText.Bounds.absFixedY = prevPlrAbsFixedY;
				}
				else
				{
					dynamicText.Bounds.absFixedX = prevPlrAbsFixedX + notifyPlayerMoneyTextSeconds * (capi.World.Rand.NextDouble() * 4.0 - 2.0);
					dynamicText.Bounds.absFixedY = prevPlrAbsFixedY + notifyPlayerMoneyTextSeconds * (capi.World.Rand.NextDouble() * 4.0 - 2.0);
				}
			}
		}
		if (!(notifyTraderMoneyTextSeconds > 0.0))
		{
			return;
		}
		notifyTraderMoneyTextSeconds -= deltaTime;
		GuiElementDynamicText dynamicText2 = base.SingleComposer.GetDynamicText("traderMoneyText");
		if (dynamicText2 != null)
		{
			if (notifyTraderMoneyTextSeconds <= 0.0)
			{
				dynamicText2.Bounds.absFixedX = prevPlrAbsFixedX;
				dynamicText2.Bounds.absFixedY = prevPlrAbsFixedY;
			}
			else
			{
				dynamicText2.Bounds.absFixedX = prevTdrAbsFixedX + notifyTraderMoneyTextSeconds * (capi.World.Rand.NextDouble() * 4.0 - 2.0);
				dynamicText2.Bounds.absFixedY = prevTdrAbsFixedY + notifyTraderMoneyTextSeconds * (capi.World.Rand.NextDouble() * 4.0 - 2.0);
			}
		}
	}
}
