using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogCreateAuction : GuiDialog
{
	private int lastPrice = 1;

	private ModSystemAuction auctionSys;

	private EntityAgent owningEntity;

	private InventoryGeneric auctionSlotInv;

	protected string gearIcon = "<itemstack type='item' code='gear-rusty' rsize='1.75' offy='2'>";

	private ElementBounds dialogBounds;

	public override double InputOrder => 0.1;

	public override double DrawOrder => 1.0;

	public override bool UnregisterOnClose => true;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogCreateAuction(ICoreClientAPI capi, EntityAgent owningEntity, InventoryGeneric auctionSlotInv)
		: base(capi)
	{
		this.owningEntity = owningEntity;
		this.auctionSlotInv = auctionSlotInv;
		auctionSys = capi.ModLoader.GetModSystem<ModSystemAuction>();
		Init();
	}

	public void Init()
	{
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 30.0, 50.0, 50.0);
		ElementBounds elementBounds2 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds2.BothSizing = ElementSizing.FitToChildren;
		dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		elementBounds2.verticalSizing = ElementSizing.FitToChildren;
		elementBounds2.horizontalSizing = ElementSizing.Fixed;
		elementBounds2.fixedWidth = 300.0;
		ElementBounds elementBounds3 = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(10.0, 1.0);
		ElementBounds elementBounds4 = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(10.0, 1.0);
		ElementBounds elementBounds5 = ElementBounds.Fixed(0.0, 0.0, 250.0, 25.0).FixedUnder(elementBounds, 20.0);
		ElementBounds elementBounds6 = ElementBounds.Fixed(0.0, 0.0, 100.0, 30.0).FixedUnder(elementBounds5);
		ElementBounds elementBounds7 = ElementBounds.Fixed(0.0, 0.0, 250.0, 25.0).FixedUnder(elementBounds6, 20.0);
		ElementBounds elementBounds8 = ElementBounds.Fixed(0.0, 0.0, 150.0, 25.0).FixedUnder(elementBounds7);
		ElementBounds elementBounds9 = ElementBounds.Fixed(0.0, 0.0, 300.0, 25.0).FixedUnder(elementBounds8, 20.0);
		ElementBounds elementBounds10 = ElementBounds.Fixed(0.0, 0.0, 300.0, 25.0).FixedUnder(elementBounds9);
		int[] array = new int[5] { 1, 2, 3, 4, 5 };
		string[] array2 = new string[5];
		string[] array3 = new string[5];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] *= auctionSys.DurationWeeksMul;
			array2[i] = array[i].ToString() ?? "";
			array3[i] = ((array[i] == 1) ? Lang.Get("{0} week", array[i]) : Lang.Get("{0} weeks", array[i]));
		}
		Composers["tradercreateauction"] = capi.Gui.CreateCompo("tradercreateauction-" + owningEntity.EntityId, dialogBounds).AddShadedDialogBG(elementBounds2).AddDialogTitleBar(Lang.Get("Create Auction"), OnCreateAuctionClose)
			.BeginChildElements(elementBounds2)
			.AddItemSlotGrid(auctionSlotInv, delegate(object p)
			{
				capi.Network.SendPacketClient(p);
			}, 1, null, elementBounds, "traderSellingSlots")
			.AddStaticText(Lang.Get("Price in rusty gears"), CairoFont.WhiteSmallText(), elementBounds5)
			.AddNumberInput(elementBounds6, onPriceChanged, CairoFont.WhiteSmallText(), "price")
			.AddStaticText(Lang.Get("Duration"), CairoFont.WhiteSmallText(), elementBounds7)
			.AddDropDown(array2, array3, 0, onDurationChanged, elementBounds8, CairoFont.WhiteSmallText(), "duration")
			.AddRichtext(Lang.Get("Deposit: {0}", 1) + " " + gearIcon, CairoFont.WhiteSmallText(), elementBounds9, "depositText")
			.AddRichtext(Lang.Get("Trader cut on sale (10%): {0}", 1) + " " + gearIcon, CairoFont.WhiteSmallText(), elementBounds10, "cutText")
			.AddSmallButton(Lang.Get("Cancel"), OnCancelAuctionClose, elementBounds3.FixedUnder(elementBounds10, 20.0).WithFixedPadding(8.0, 5.0))
			.AddSmallButton(Lang.Get("Create Auction"), OnCreateAuctionConfirm, elementBounds4.FixedUnder(elementBounds10, 20.0).WithFixedPadding(8.0, 5.0), EnumButtonStyle.Normal, "buysellButton")
			.EndChildElements()
			.Compose();
		Composers["tradercreateauction"].GetNumberInput("price").SetValue(lastPrice);
	}

	private void onPriceChanged(string text)
	{
		float num = (float)Composers["tradercreateauction"].GetNumberInput("price").GetText().ToInt(1) * auctionSys.SalesCutRate + auctionSys.debtClient;
		Composers["tradercreateauction"].GetRichtext("cutText").SetNewText(Lang.Get("Trader cut on sale (10%): {0}", (int)num) + " " + gearIcon, CairoFont.WhiteSmallText());
	}

	private void onDurationChanged(string code, bool selected)
	{
		int num = code.ToInt(1) / auctionSys.DurationWeeksMul;
		Composers["tradercreateauction"].GetRichtext("depositText").SetNewText(Lang.Get("Deposit: {0}", num) + " " + gearIcon, CairoFont.WhiteSmallText());
	}

	private bool OnCancelAuctionClose()
	{
		TryClose();
		return true;
	}

	private bool OnCreateAuctionConfirm()
	{
		GuiComposer composer = Composers["tradercreateauction"];
		int playerAssets = InventoryTrader.GetPlayerAssets(capi.World.Player.Entity);
		int num = composer.GetDropDown("duration").SelectedValue.ToInt(1);
		int num2 = (int)composer.GetNumberInput("price").GetValue();
		if (num2 < 1)
		{
			capi.TriggerIngameError(this, "atleast1gear", Lang.Get("Must sell item for at least 1 gear"));
			return true;
		}
		if (playerAssets < auctionSys.GetDepositCost(auctionSlotInv[0]) * num / auctionSys.DurationWeeksMul)
		{
			capi.TriggerIngameError(this, "notenoughgears", Lang.Get("Not enough gears to pay the deposit"));
			return true;
		}
		auctionSys.PlaceAuctionClient(owningEntity, num2, num);
		OnCreateAuctionClose();
		lastPrice = num2;
		auctionSlotInv[0].Itemstack = null;
		capi.Gui.PlaySound(new AssetLocation("effect/receptionbell.ogg"));
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

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
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
