using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemAuction : ModSystem
{
	protected ICoreAPI api;

	protected ICoreServerAPI sapi;

	protected ICoreClientAPI capi;

	protected AuctionsData auctionsData = new AuctionsData();

	public Dictionary<string, InventoryGeneric> createAuctionSlotByPlayer = new Dictionary<string, InventoryGeneric>();

	public Action OnCellUpdateClient;

	public EntityTradingHumanoid curTraderClient;

	public float debtClient;

	public float DeliveryPriceMul = 1f;

	public int DurationWeeksMul = 6;

	public float SalesCutRate = 0.1f;

	public ItemStack SingleCurrencyStack;

	public List<Auction> activeAuctions = new List<Auction>();

	public List<Auction> ownAuctions = new List<Auction>();

	protected OrderedDictionary<long, Auction> auctions => auctionsData.auctions;

	protected IServerNetworkChannel serverCh => sapi.Network.GetChannel("auctionHouse");

	protected IClientNetworkChannel clientCh => capi.Network.GetChannel("auctionHouse");

	private bool auctionHouseEnabled => sapi.World.Config.GetBool("auctionHouse", defaultValue: true);

	public int DeliveryCostsByDistance(Vec3d src, Vec3d dst)
	{
		return DeliveryCostsByDistance(src.DistanceTo(dst));
	}

	public int DeliveryCostsByDistance(double distance)
	{
		return (int)Math.Ceiling(3.5 * Math.Log((distance - 200.0) / 10000.0 + 1.0) * (double)DeliveryPriceMul);
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		this.api = api;
		api.Network.RegisterChannel("auctionHouse").RegisterMessageType<AuctionActionPacket>().RegisterMessageType<AuctionlistPacket>()
			.RegisterMessageType<AuctionActionResponsePacket>()
			.RegisterMessageType<DebtPacket>();
	}

	public void loadPricingConfig()
	{
		DeliveryPriceMul = api.World.Config.GetFloat("auctionHouseDeliveryPriceMul", 1f);
		DurationWeeksMul = api.World.Config.GetInt("auctionHouseDurationWeeksMul", 3);
		SalesCutRate = api.World.Config.GetFloat("auctionHouseSalesCutRate", 0.1f);
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		capi = api;
		clientCh.SetMessageHandler<AuctionlistPacket>(onAuctionList).SetMessageHandler<AuctionActionResponsePacket>(onAuctionActionResponse).SetMessageHandler<DebtPacket>(onDebtPkt);
		api.Event.BlockTexturesLoaded += Event_BlockTexturesLoaded;
	}

	private void onDebtPkt(DebtPacket pkt)
	{
		debtClient = pkt.TraderDebt;
	}

	private void Event_BlockTexturesLoaded()
	{
		Item item = capi.World.GetItem(new AssetLocation("gear-rusty"));
		if (item != null)
		{
			SingleCurrencyStack = new ItemStack(item);
		}
		loadPricingConfig();
	}

	private void onAuctionActionResponse(AuctionActionResponsePacket pkt)
	{
		if (pkt.ErrorCode != null)
		{
			capi.TriggerIngameError(this, pkt.ErrorCode, Lang.Get("auctionerror-" + pkt.ErrorCode));
			curTraderClient?.TalkUtil.Talk(EnumTalkType.Complain);
			return;
		}
		if (pkt.Action == EnumAuctionAction.PurchaseAuction || (pkt.Action == EnumAuctionAction.RetrieveAuction && pkt.MoneyReceived))
		{
			capi.Gui.PlaySound(new AssetLocation("sounds/effect/cashregister"), randomizePitch: false, 0.25f);
		}
		curTraderClient?.TalkUtil.Talk(EnumTalkType.Purchase);
	}

	private void onAuctionList(AuctionlistPacket pkt)
	{
		debtClient = pkt.TraderDebt;
		if (pkt.IsFullUpdate)
		{
			activeAuctions.Clear();
			ownAuctions.Clear();
			auctions.Clear();
		}
		if (pkt.NewAuctions != null)
		{
			Auction[] newAuctions = pkt.NewAuctions;
			foreach (Auction auction in newAuctions)
			{
				auctions[auction.AuctionId] = auction;
				auction.ItemStack.ResolveBlockOrItem(capi.World);
				if (auction.State == EnumAuctionState.Active || (auction.State == EnumAuctionState.Sold && auction.RetrievableTotalHours - capi.World.Calendar.TotalHours > 0.0))
				{
					insertOrUpdate(activeAuctions, auction);
				}
				else
				{
					remove(activeAuctions, auction);
				}
				if (auction.SellerUid == capi.World.Player.PlayerUID || (auction.State == EnumAuctionState.Sold && auction.BuyerUid == capi.World.Player.PlayerUID))
				{
					insertOrUpdate(ownAuctions, auction);
				}
				else
				{
					remove(ownAuctions, auction);
				}
			}
		}
		if (pkt.RemovedAuctions != null)
		{
			long[] removedAuctions = pkt.RemovedAuctions;
			foreach (long num in removedAuctions)
			{
				auctions.Remove(num);
				RemoveFromList(num, activeAuctions);
				RemoveFromList(num, ownAuctions);
			}
		}
		activeAuctions.Sort();
		ownAuctions.Sort();
		OnCellUpdateClient?.Invoke();
	}

	private void remove(List<Auction> auctions, Auction auction)
	{
		for (int i = 0; i < auctions.Count; i++)
		{
			if (auctions[i].AuctionId == auction.AuctionId)
			{
				auctions.RemoveAt(i);
				break;
			}
		}
	}

	private void insertOrUpdate(List<Auction> auctions, Auction auction)
	{
		bool flag = false;
		int num = 0;
		while (!flag && num < auctions.Count)
		{
			if (auctions[num].AuctionId == auction.AuctionId)
			{
				auctions[num] = auction;
				return;
			}
			num++;
		}
		auctions.Add(auction);
	}

	private void RemoveFromList(long auctionId, List<Auction> auctions)
	{
		for (int i = 0; i < auctions.Count; i++)
		{
			if (auctions[i].AuctionId == auctionId)
			{
				auctions.RemoveAt(i);
				i--;
			}
		}
	}

	public void DidEnterAuctionHouse()
	{
		clientCh.SendPacket(new AuctionActionPacket
		{
			Action = EnumAuctionAction.EnterAuctionHouse
		});
	}

	public void DidLeaveAuctionHouse()
	{
		clientCh.SendPacket(new AuctionActionPacket
		{
			Action = EnumAuctionAction.LeaveAuctionHouse
		});
	}

	public void PlaceAuctionClient(Entity traderEntity, int price, int durationWeeks = 1)
	{
		clientCh.SendPacket(new AuctionActionPacket
		{
			Action = EnumAuctionAction.PlaceAuction,
			AtAuctioneerEntityId = traderEntity.EntityId,
			Price = price,
			DurationWeeks = durationWeeks
		});
	}

	public void BuyAuctionClient(Entity traderEntity, long auctionId, bool withDelivery)
	{
		clientCh.SendPacket(new AuctionActionPacket
		{
			Action = EnumAuctionAction.PurchaseAuction,
			AtAuctioneerEntityId = traderEntity.EntityId,
			AuctionId = auctionId,
			WithDelivery = withDelivery
		});
	}

	public void RetrieveAuctionClient(Entity traderEntity, long auctionId)
	{
		clientCh.SendPacket(new AuctionActionPacket
		{
			Action = EnumAuctionAction.RetrieveAuction,
			AtAuctioneerEntityId = traderEntity.EntityId,
			AuctionId = auctionId
		});
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		api.Network.GetChannel("auctionHouse").SetMessageHandler<AuctionActionPacket>(onAuctionAction);
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.PlayerDisconnect += Event_PlayerDisconnect;
		api.Event.PlayerJoin += Event_PlayerJoin;
		api.Event.RegisterGameTickListener(TickAuctions, 5000);
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		foreach (Auction value in auctionsData.auctions.Values)
		{
			if (value.BuyerUid == byPlayer.PlayerUID)
			{
				if (value.RetrievableTotalHours <= sapi.World.Calendar.TotalHours)
				{
					num3++;
				}
				else
				{
					num4++;
				}
			}
			if (value.SellerUid == byPlayer.PlayerUID)
			{
				if (value.State == EnumAuctionState.Sold || value.State == EnumAuctionState.SoldRetrieved)
				{
					num5++;
				}
				if (value.State == EnumAuctionState.Expired)
				{
					num++;
				}
				if (value.State == EnumAuctionState.Active)
				{
					num2++;
				}
			}
		}
		if (num + num2 + num3 + num4 + num5 > 0)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(Lang.Get("Auction House: You have") + " ");
			if (num2 > 0)
			{
				stringBuilder.AppendLine(Lang.Get("{0} active auctions", num2));
			}
			if (num5 > 0)
			{
				stringBuilder.AppendLine(Lang.Get("{0} sold auctions", num5));
			}
			if (num > 0)
			{
				stringBuilder.AppendLine(Lang.Get("{0} expired auctions", num));
			}
			if (num4 > 0)
			{
				stringBuilder.AppendLine(Lang.Get("{0} purchased auctions en-route", num3));
			}
			if (num3 > 0)
			{
				stringBuilder.AppendLine(Lang.Get("{0} purchased auctions ready for pick-up", num3));
			}
			byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, stringBuilder.ToString(), EnumChatType.Notification);
		}
	}

	private void Event_PlayerDisconnect(IServerPlayer byPlayer)
	{
		if (createAuctionSlotByPlayer.TryGetValue(byPlayer.PlayerUID, out var _))
		{
			byPlayer.InventoryManager.CloseInventoryAndSync(createAuctionSlotByPlayer[byPlayer.PlayerUID]);
			createAuctionSlotByPlayer.Remove(byPlayer.PlayerUID);
		}
	}

	private void onAuctionAction(IServerPlayer fromPlayer, AuctionActionPacket pkt)
	{
		if (!auctionHouseEnabled)
		{
			return;
		}
		switch (pkt.Action)
		{
		case EnumAuctionAction.EnterAuctionHouse:
			if (!createAuctionSlotByPlayer.ContainsKey(fromPlayer.PlayerUID))
			{
				InventoryGeneric inventoryGeneric = (createAuctionSlotByPlayer[fromPlayer.PlayerUID] = new InventoryGeneric(1, "auctionslot-" + fromPlayer.PlayerUID, sapi));
				InventoryGeneric ainv = inventoryGeneric;
				ainv.OnGetSuitability = (ItemSlot s, ItemSlot t, bool isMerge) => -1f;
				ainv.OnInventoryClosed += delegate(IPlayer plr)
				{
					ainv.DropAll(plr.Entity.Pos.XYZ);
				};
			}
			fromPlayer.InventoryManager.OpenInventory(createAuctionSlotByPlayer[fromPlayer.PlayerUID]);
			sendAuctions(auctions.Values, null, isFullUpdate: true, fromPlayer);
			break;
		case EnumAuctionAction.LeaveAuctionHouse:
			Event_PlayerDisconnect(fromPlayer);
			break;
		case EnumAuctionAction.PurchaseAuction:
		{
			Entity entityById2 = sapi.World.GetEntityById(pkt.AtAuctioneerEntityId);
			PurchaseAuction(pkt.AuctionId, fromPlayer.Entity, entityById2, pkt.WithDelivery, out var failureCode3);
			serverCh.SendPacket(new AuctionActionResponsePacket
			{
				Action = pkt.Action,
				AuctionId = pkt.AuctionId,
				ErrorCode = failureCode3
			}, fromPlayer);
			break;
		}
		case EnumAuctionAction.RetrieveAuction:
		{
			string failureCode2;
			ItemStack itemStack = RetrieveAuction(pkt.AuctionId, pkt.AtAuctioneerEntityId, fromPlayer.Entity, out failureCode2);
			if (itemStack != null)
			{
				if (!fromPlayer.InventoryManager.TryGiveItemstack(itemStack, slotNotifyEffect: true))
				{
					sapi.World.SpawnItemEntity(itemStack, fromPlayer.Entity.Pos.XYZ);
				}
				sapi.World.Logger.Audit("{0} Got 1x{1} from Auction at {2}.", fromPlayer.PlayerName, itemStack.Collectible.Code, fromPlayer.Entity.Pos);
			}
			serverCh.SendPacket(new AuctionActionResponsePacket
			{
				Action = pkt.Action,
				AuctionId = pkt.AuctionId,
				ErrorCode = failureCode2,
				MoneyReceived = (itemStack != null && itemStack.Collectible.Attributes?["currency"].Exists == true)
			}, fromPlayer);
			break;
		}
		case EnumAuctionAction.PlaceAuction:
		{
			if (!createAuctionSlotByPlayer.TryGetValue(fromPlayer.PlayerUID, out var value))
			{
				break;
			}
			if (value.Empty)
			{
				serverCh.SendPacket(new AuctionActionResponsePacket
				{
					Action = pkt.Action,
					AuctionId = pkt.AuctionId,
					ErrorCode = "emptyauctionslot"
				}, fromPlayer);
				break;
			}
			pkt.DurationWeeks = Math.Max(1, pkt.DurationWeeks);
			Entity entityById = sapi.World.GetEntityById(pkt.AtAuctioneerEntityId);
			PlaceAuction(value[0], value[0].StackSize, pkt.Price, pkt.DurationWeeks * 7 * 24, pkt.DurationWeeks / DurationWeeksMul, fromPlayer.Entity, entityById, out var failureCode);
			if (failureCode != null)
			{
				value.DropAll(fromPlayer.Entity.Pos.XYZ);
			}
			auctionsData.DebtToTraderByPlayer.TryGetValue(fromPlayer.PlayerUID, out var value2);
			serverCh.SendPacket(new AuctionActionResponsePacket
			{
				Action = pkt.Action,
				AuctionId = pkt.AuctionId,
				ErrorCode = failureCode
			}, fromPlayer);
			serverCh.SendPacket(new DebtPacket
			{
				TraderDebt = value2
			}, fromPlayer);
			break;
		}
		}
	}

	public List<Auction> GetActiveAuctions()
	{
		return auctions.Values.Where((Auction ac) => ac.State == EnumAuctionState.Active || ac.State == EnumAuctionState.Sold).ToList();
	}

	public List<Auction> GetAuctionsFrom(IPlayer player)
	{
		List<Auction> list = new List<Auction>();
		foreach (Auction value in auctions.Values)
		{
			if (value.SellerName == player.PlayerUID)
			{
				list.Add(value);
			}
		}
		return list;
	}

	private void TickAuctions(float dt)
	{
		double totalHours = sapi.World.Calendar.TotalHours;
		Auction[] array = auctions.Values.ToArray();
		List<Auction> list = new List<Auction>();
		Auction[] array2 = array;
		foreach (Auction auction in array2)
		{
			if (auction.State == EnumAuctionState.Active && auction.ExpireTotalHours < totalHours)
			{
				auction.State = EnumAuctionState.Expired;
				list.Add(auction);
			}
		}
		sendAuctions(list, null);
	}

	public virtual int GetDepositCost(ItemSlot forItem)
	{
		return 1;
	}

	public void PlaceAuction(ItemSlot slot, int quantity, int price, double durationHours, int depositCost, EntityAgent sellerEntity, Entity auctioneerEntity, out string failureCode)
	{
		if (slot.StackSize < quantity)
		{
			failureCode = "notenoughitems";
			return;
		}
		if (GetAuctionsFrom((sellerEntity as EntityPlayer).Player).Count > 30)
		{
			failureCode = "toomanyauctions";
			return;
		}
		if (InventoryTrader.GetPlayerAssets(sellerEntity) < GetDepositCost(slot) * depositCost)
		{
			failureCode = "notenoughgears";
			return;
		}
		if (price < 1)
		{
			failureCode = "atleast1gear";
			return;
		}
		failureCode = null;
		InventoryTrader.DeductFromEntity(sapi, sellerEntity, depositCost);
		(auctioneerEntity as EntityTradingHumanoid).Inventory?.GiveToTrader(depositCost);
		long num = ++auctionsData.nextAuctionId;
		string text = sellerEntity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName;
		if (text == null)
		{
			text = sellerEntity.Properties.Code.ToShortString();
		}
		string key = (sellerEntity as EntityPlayer)?.PlayerUID ?? "";
		auctionsData.DebtToTraderByPlayer.TryGetValue(key, out var value);
		float num2 = (float)price * SalesCutRate + value;
		auctionsData.DebtToTraderByPlayer[key] = num2 - (float)(int)num2;
		Auction auction = new Auction
		{
			AuctionId = num,
			ExpireTotalHours = sapi.World.Calendar.TotalHours + durationHours,
			ItemStack = slot.TakeOut(quantity),
			PostedTotalHours = sapi.World.Calendar.TotalHours,
			Price = price,
			TraderCut = (int)num2,
			SellerName = text,
			SellerUid = (sellerEntity as EntityPlayer)?.PlayerUID,
			SellerEntityId = sellerEntity.EntityId,
			SrcAuctioneerEntityPos = auctioneerEntity.Pos.XYZ,
			SrcAuctioneerEntityId = auctioneerEntity.EntityId
		};
		auctions.Add(num, auction);
		slot.MarkDirty();
		sendAuctions(new Auction[1] { auction }, null);
	}

	public void PurchaseAuction(long auctionId, EntityAgent buyerEntity, Entity auctioneerEntity, bool withDelivery, out string failureCode)
	{
		if (auctions.TryGetValue(auctionId, out var value))
		{
			if ((buyerEntity as EntityPlayer)?.PlayerUID == value.SellerUid)
			{
				failureCode = "ownauction";
				return;
			}
			if (value.BuyerName != null)
			{
				failureCode = "alreadypurchased";
				return;
			}
			int playerAssets = InventoryTrader.GetPlayerAssets(buyerEntity);
			int num = (withDelivery ? DeliveryCostsByDistance(auctioneerEntity.Pos.XYZ, value.SrcAuctioneerEntityPos) : 0);
			int num2 = value.Price + num;
			if (playerAssets < num2)
			{
				failureCode = "notenoughgears";
				return;
			}
			InventoryTrader.DeductFromEntity(sapi, buyerEntity, num2);
			(auctioneerEntity as EntityTradingHumanoid).Inventory?.GiveToTrader((int)((float)value.Price * SalesCutRate + (float)num));
			string text = buyerEntity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName;
			if (text == null)
			{
				text = buyerEntity.Properties.Code.ToShortString();
			}
			value.BuyerName = text;
			value.WithDelivery = withDelivery;
			value.BuyerUid = (buyerEntity as EntityPlayer)?.PlayerUID;
			value.RetrievableTotalHours = sapi.World.Calendar.TotalHours + 1.0 + (double)(3 * num);
			value.DstAuctioneerEntityId = (withDelivery ? auctioneerEntity.EntityId : value.SrcAuctioneerEntityId);
			value.DstAuctioneerEntityPos = (withDelivery ? auctioneerEntity.Pos.XYZ : value.SrcAuctioneerEntityPos);
			value.State = EnumAuctionState.Sold;
			sendAuctions(new Auction[1] { value }, null);
			failureCode = null;
		}
		else
		{
			failureCode = "nosuchauction";
		}
	}

	public void DeleteActiveAuction(long auctionId)
	{
		auctions.Remove(auctionId);
		sendAuctions(null, new long[1] { auctionId });
	}

	public ItemStack RetrieveAuction(long auctionId, long atAuctioneerEntityId, EntityPlayer reqEntity, out string failureCode)
	{
		if (!auctions.TryGetValue(auctionId, out var value))
		{
			failureCode = "nosuchauction";
			return null;
		}
		if (reqEntity.PlayerUID == value.BuyerUid)
		{
			if (value.RetrievableTotalHours > sapi.World.Calendar.TotalHours)
			{
				failureCode = "notyetretrievable";
				return null;
			}
			if (value.State == EnumAuctionState.SoldRetrieved)
			{
				failureCode = "alreadyretrieved";
				return null;
			}
			if (value.State == EnumAuctionState.Expired || value.State == EnumAuctionState.Active)
			{
				sapi.Logger.Notification("Auction was bought by {0}, but is in state {1}? O.o Setting it to sold state.");
				value.State = EnumAuctionState.Sold;
				value.RetrievableTotalHours = sapi.World.Calendar.TotalHours + 6.0;
				failureCode = null;
				sendAuctions(new Auction[1] { value }, null);
				return null;
			}
			if (!value.WithDelivery && value.SrcAuctioneerEntityId != atAuctioneerEntityId && value.SrcAuctioneerEntityPos.DistanceTo(reqEntity.Pos.XYZ) > 100f)
			{
				failureCode = "wrongtrader";
				return null;
			}
			value.State = EnumAuctionState.SoldRetrieved;
			if (value.MoneyCollected)
			{
				auctions.Remove(auctionId);
				sendAuctions(null, new long[1] { auctionId });
			}
			else
			{
				sendAuctions(new Auction[1] { value }, null);
			}
			failureCode = null;
			return value.ItemStack.Clone();
		}
		if (reqEntity.PlayerUID == value.SellerUid)
		{
			if (value.State == EnumAuctionState.Active)
			{
				value.State = EnumAuctionState.Expired;
				value.RetrievableTotalHours = sapi.World.Calendar.TotalHours + 6.0;
				failureCode = null;
				sendAuctions(new Auction[1] { value }, null);
				return null;
			}
			if (value.RetrievableTotalHours > sapi.World.Calendar.TotalHours)
			{
				failureCode = "notyetretrievable";
				return null;
			}
			if (value.State == EnumAuctionState.Expired)
			{
				auctions.Remove(auctionId);
				sendAuctions(null, new long[1] { auctionId });
				failureCode = null;
				return value.ItemStack;
			}
			if (value.State == EnumAuctionState.Sold || value.State == EnumAuctionState.SoldRetrieved)
			{
				if (value.MoneyCollected)
				{
					failureCode = "moneyalreadycollected";
					return null;
				}
				if (value.State == EnumAuctionState.SoldRetrieved)
				{
					auctions.Remove(auctionId);
					sendAuctions(null, new long[1] { auctionId });
				}
				else
				{
					sendAuctions(new Auction[1] { value }, null);
				}
				failureCode = null;
				value.MoneyCollected = true;
				ItemStack itemStack = SingleCurrencyStack.Clone();
				itemStack.StackSize = value.Price - value.TraderCut;
				return itemStack;
			}
			failureCode = "codingerror";
			return null;
		}
		failureCode = "notyouritem";
		return null;
	}

	private void sendAuctions(IEnumerable<Auction> newauctions, long[] removedauctions, bool isFullUpdate = false, IServerPlayer toPlayer = null)
	{
		Auction[] array = newauctions?.ToArray();
		if ((array == null || array.Length == 0) && (removedauctions == null || removedauctions.Length == 0) && !isFullUpdate)
		{
			return;
		}
		float value = 0f;
		if (toPlayer != null)
		{
			auctionsData.DebtToTraderByPlayer.TryGetValue(toPlayer.PlayerUID, out value);
		}
		AuctionlistPacket message = new AuctionlistPacket
		{
			NewAuctions = array,
			RemovedAuctions = removedauctions,
			IsFullUpdate = isFullUpdate,
			TraderDebt = value
		};
		if (toPlayer != null)
		{
			sapi.Network.GetChannel("auctionHouse").SendPacket(message, toPlayer);
			return;
		}
		foreach (string key in createAuctionSlotByPlayer.Keys)
		{
			if (sapi.World.PlayerByUid(key) is IServerPlayer serverPlayer)
			{
				sapi.Network.GetChannel("auctionHouse").SendPacket(message, serverPlayer);
			}
		}
	}

	private void Event_GameWorldSave()
	{
		sapi.WorldManager.SaveGame.StoreData("auctionsData", SerializerUtil.Serialize(auctionsData));
	}

	private void Event_SaveGameLoaded()
	{
		Item item = sapi.World.GetItem(new AssetLocation("gear-rusty"));
		if (item == null)
		{
			return;
		}
		SingleCurrencyStack = new ItemStack(item);
		byte[] data = sapi.WorldManager.SaveGame.GetData("auctionsData");
		if (data != null)
		{
			auctionsData = SerializerUtil.Deserialize<AuctionsData>(data);
			foreach (Auction value in auctionsData.auctions.Values)
			{
				value.ItemStack?.ResolveBlockOrItem(sapi.World);
			}
		}
		loadPricingConfig();
	}
}
