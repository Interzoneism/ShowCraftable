using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class InventoryTrader : InventoryBase
{
	private EntityTradingHumanoid traderEntity;

	private ItemSlot[] slots;

	private string[] ignoredAttrs = GlobalConstants.IgnoredStackAttributes.Append("backpack", "condition");

	public ItemSlotTrade[] SellingSlots
	{
		get
		{
			ItemSlotTrade[] array = new ItemSlotTrade[16];
			for (int i = 0; i < 16; i++)
			{
				array[i] = slots[i] as ItemSlotTrade;
			}
			return array;
		}
	}

	public ItemSlotTrade[] BuyingSlots
	{
		get
		{
			ItemSlotTrade[] array = new ItemSlotTrade[16];
			for (int i = 0; i < 16; i++)
			{
				array[i] = slots[20 + i] as ItemSlotTrade;
			}
			return array;
		}
	}

	public int BuyingCartTotalCost => 0;

	public ItemSlot MoneySlot => slots[40];

	public override int Count => 41;

	public override ItemSlot this[int slotId]
	{
		get
		{
			if (slotId < 0 || slotId >= Count)
			{
				return null;
			}
			return slots[slotId];
		}
		set
		{
			if (slotId < 0 || slotId >= Count)
			{
				throw new ArgumentOutOfRangeException("slotId");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			slots[slotId] = value;
		}
	}

	public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
	{
		return 0f;
	}

	public InventoryTrader(string inventoryID, ICoreAPI api)
		: base(inventoryID, api)
	{
		slots = GenEmptySlots(Count);
	}

	public InventoryTrader(string className, string instanceID, ICoreAPI api)
		: base(className, instanceID, api)
	{
		slots = GenEmptySlots(Count);
	}

	internal void LateInitialize(string id, ICoreAPI api, EntityTradingHumanoid traderEntity)
	{
		base.LateInitialize(id, api);
		this.traderEntity = traderEntity;
	}

	public override object ActivateSlot(int slotId, ItemSlot mouseSlot, ref ItemStackMoveOperation op)
	{
		if (slotId <= 15)
		{
			AddToBuyingCart(slots[slotId] as ItemSlotTrade);
			return InvNetworkUtil.GetActivateSlotPacket(slotId, op);
		}
		if (slotId <= 19)
		{
			ItemSlotTrade itemSlotTrade = slots[slotId] as ItemSlotTrade;
			if (op.MouseButton == EnumMouseButton.Right)
			{
				if (itemSlotTrade.TradeItem?.Stack != null)
				{
					itemSlotTrade.TakeOut(itemSlotTrade.TradeItem.Stack.StackSize);
					itemSlotTrade.MarkDirty();
				}
			}
			else
			{
				itemSlotTrade.Itemstack = null;
				itemSlotTrade.MarkDirty();
			}
			return InvNetworkUtil.GetActivateSlotPacket(slotId, op);
		}
		if (slotId <= 34)
		{
			return InvNetworkUtil.GetActivateSlotPacket(slotId, op);
		}
		if (slotId <= 39)
		{
			return base.ActivateSlot(slotId, mouseSlot, ref op);
		}
		return InvNetworkUtil.GetActivateSlotPacket(slotId, op);
	}

	private void AddToBuyingCart(ItemSlotTrade sellingSlot)
	{
		if (sellingSlot.Empty)
		{
			return;
		}
		for (int i = 0; i < 4; i++)
		{
			ItemSlotTrade itemSlotTrade = slots[16 + i] as ItemSlotTrade;
			if (!itemSlotTrade.Empty && itemSlotTrade.Itemstack.Equals(Api.World, sellingSlot.Itemstack) && itemSlotTrade.Itemstack.StackSize + sellingSlot.TradeItem.Stack.StackSize <= itemSlotTrade.Itemstack.Collectible.MaxStackSize)
			{
				itemSlotTrade.Itemstack.StackSize += sellingSlot.TradeItem.Stack.StackSize;
				itemSlotTrade.MarkDirty();
				return;
			}
		}
		for (int j = 0; j < 4; j++)
		{
			ItemSlotTrade itemSlotTrade2 = slots[16 + j] as ItemSlotTrade;
			if (itemSlotTrade2.Empty)
			{
				itemSlotTrade2.Itemstack = sellingSlot.TradeItem.Stack.Clone();
				itemSlotTrade2.Itemstack.ResolveBlockOrItem(Api.World);
				itemSlotTrade2.TradeItem = sellingSlot.TradeItem;
				itemSlotTrade2.MarkDirty();
				break;
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree)
	{
		slots = SlotsFromTreeAttributes(tree, slots);
		ITreeAttribute treeAttribute = tree.GetTreeAttribute("tradeItems");
		if (treeAttribute == null)
		{
			return;
		}
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i] is ItemSlotTrade && !slots[i].Empty)
			{
				(slots[i] as ItemSlotTrade).TradeItem = new ResolvedTradeItem(treeAttribute.GetTreeAttribute(i.ToString() ?? ""));
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		SlotsToTreeAttributes(slots, tree);
		TreeAttribute treeAttribute = new TreeAttribute();
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i].Itemstack != null && slots[i] is ItemSlotTrade)
			{
				TreeAttribute treeAttribute2 = new TreeAttribute();
				(slots[i] as ItemSlotTrade).TradeItem?.ToTreeAttributes(treeAttribute2);
				treeAttribute[i.ToString() ?? ""] = treeAttribute2;
			}
		}
		tree["tradeItems"] = treeAttribute;
	}

	internal EnumTransactionResult TryBuySell(IPlayer buyingPlayer)
	{
		if (!HasPlayerEnoughAssets(buyingPlayer))
		{
			return EnumTransactionResult.PlayerNotEnoughAssets;
		}
		if (!HasTraderEnoughAssets())
		{
			return EnumTransactionResult.TraderNotEnoughAssets;
		}
		if (!HasTraderEnoughStock(buyingPlayer))
		{
			return EnumTransactionResult.TraderNotEnoughSupplyOrDemand;
		}
		if (!HasTraderEnoughDemand(buyingPlayer))
		{
			return EnumTransactionResult.TraderNotEnoughSupplyOrDemand;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			for (int i = 0; i < 4; i++)
			{
				GetBuyingCartSlot(i).Itemstack = null;
			}
			return EnumTransactionResult.Success;
		}
		for (int j = 0; j < 4; j++)
		{
			ItemSlotTrade buyingCartSlot = GetBuyingCartSlot(j);
			if (buyingCartSlot.Itemstack?.Collectible is ITradeableCollectible tradeableCollectible)
			{
				EnumTransactionResult enumTransactionResult = tradeableCollectible.OnTryTrade(traderEntity, buyingCartSlot, EnumTradeDirection.Buy);
				if (enumTransactionResult != EnumTransactionResult.Success)
				{
					return enumTransactionResult;
				}
			}
		}
		for (int k = 0; k < 4; k++)
		{
			ItemSlot sellingCartSlot = GetSellingCartSlot(k);
			if (sellingCartSlot.Itemstack?.Collectible is ITradeableCollectible tradeableCollectible2)
			{
				EnumTransactionResult enumTransactionResult2 = tradeableCollectible2.OnTryTrade(traderEntity, sellingCartSlot, EnumTradeDirection.Sell);
				if (enumTransactionResult2 != EnumTransactionResult.Success)
				{
					return enumTransactionResult2;
				}
			}
		}
		if (!HandleMoneyTransaction(buyingPlayer))
		{
			return EnumTransactionResult.Failure;
		}
		for (int l = 0; l < 4; l++)
		{
			ItemSlotTrade buyingCartSlot2 = GetBuyingCartSlot(l);
			if (buyingCartSlot2.Itemstack != null)
			{
				GiveOrDrop(buyingPlayer.Entity, buyingCartSlot2.Itemstack);
				buyingCartSlot2.TradeItem.Stock -= buyingCartSlot2.Itemstack.StackSize / buyingCartSlot2.TradeItem.Stack.StackSize;
				buyingCartSlot2.Itemstack = null;
				buyingCartSlot2.MarkDirty();
			}
		}
		for (int m = 0; m < 4; m++)
		{
			ItemSlot sellingCartSlot2 = GetSellingCartSlot(m);
			if (sellingCartSlot2.Itemstack == null)
			{
				continue;
			}
			ResolvedTradeItem tradeItem = GetBuyingConditionsSlot(sellingCartSlot2.Itemstack).TradeItem;
			if (tradeItem != null)
			{
				int num = sellingCartSlot2.Itemstack.StackSize / tradeItem.Stack.StackSize;
				tradeItem.Stock -= num;
				ItemStack itemStack = sellingCartSlot2.TakeOut(num * tradeItem.Stack.StackSize);
				if (itemStack.Collectible is ITradeableCollectible tradeableCollectible3)
				{
					tradeableCollectible3.OnDidTrade(traderEntity, itemStack, EnumTradeDirection.Buy);
				}
				sellingCartSlot2.MarkDirty();
			}
		}
		return EnumTransactionResult.Success;
	}

	public bool HasTraderEnoughStock(IPlayer player)
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		for (int i = 0; i < 4; i++)
		{
			ItemSlotTrade buyingCartSlot = GetBuyingCartSlot(i);
			if (buyingCartSlot.Itemstack != null)
			{
				ItemSlotTrade sellingConditionsSlot = GetSellingConditionsSlot(buyingCartSlot.Itemstack);
				int slotId = GetSlotId(sellingConditionsSlot);
				if (!dictionary.TryGetValue(slotId, out var value))
				{
					value = buyingCartSlot.TradeItem.Stock;
				}
				dictionary[slotId] = value - buyingCartSlot.Itemstack.StackSize / buyingCartSlot.TradeItem.Stack.StackSize;
				if (dictionary[slotId] < 0)
				{
					player.InventoryManager.NotifySlot(player, buyingCartSlot);
					player.InventoryManager.NotifySlot(player, sellingConditionsSlot);
					return false;
				}
			}
		}
		return true;
	}

	public bool HasTraderEnoughDemand(IPlayer player)
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		for (int i = 0; i < 4; i++)
		{
			ItemSlot sellingCartSlot = GetSellingCartSlot(i);
			if (sellingCartSlot.Itemstack != null)
			{
				ItemSlotTrade buyingConditionsSlot = GetBuyingConditionsSlot(sellingCartSlot.Itemstack);
				ResolvedTradeItem resolvedTradeItem = buyingConditionsSlot?.TradeItem;
				if (resolvedTradeItem == null)
				{
					player.InventoryManager.NotifySlot(player, sellingCartSlot);
					return false;
				}
				int slotId = GetSlotId(buyingConditionsSlot);
				if (!dictionary.TryGetValue(slotId, out var value))
				{
					value = resolvedTradeItem.Stock;
				}
				dictionary[slotId] = value - sellingCartSlot.Itemstack.StackSize / resolvedTradeItem.Stack.StackSize;
				if (dictionary[slotId] < 0)
				{
					player.InventoryManager.NotifySlot(player, buyingConditionsSlot);
					player.InventoryManager.NotifySlot(player, sellingCartSlot);
					return false;
				}
			}
		}
		return true;
	}

	public bool IsTraderInterestedIn(ItemStack stack)
	{
		ItemSlotTrade buyingConditionsSlot = GetBuyingConditionsSlot(stack);
		ResolvedTradeItem resolvedTradeItem = buyingConditionsSlot?.TradeItem;
		if (resolvedTradeItem == null)
		{
			return false;
		}
		if (resolvedTradeItem.Stock == 0)
		{
			PerformNotifySlot(GetSlotId(buyingConditionsSlot));
		}
		return resolvedTradeItem.Stock > 0;
	}

	public bool HasPlayerEnoughAssets(IPlayer buyingPlayer)
	{
		int playerAssets = GetPlayerAssets(buyingPlayer.Entity);
		int totalCost = GetTotalCost();
		int totalGain = GetTotalGain();
		if (playerAssets - totalCost + totalGain < 0)
		{
			return false;
		}
		return true;
	}

	public bool HasTraderEnoughAssets()
	{
		int traderAssets = GetTraderAssets();
		int totalCost = GetTotalCost();
		int totalGain = GetTotalGain();
		if (traderAssets + totalCost - totalGain < 0)
		{
			return false;
		}
		return true;
	}

	private bool HandleMoneyTransaction(IPlayer buyingPlayer)
	{
		int playerAssets = GetPlayerAssets(buyingPlayer.Entity);
		int traderAssets = GetTraderAssets();
		int totalCost = GetTotalCost();
		int totalGain = GetTotalGain();
		if (playerAssets - totalCost + totalGain < 0)
		{
			return false;
		}
		if (traderAssets + totalCost - totalGain < 0)
		{
			return false;
		}
		int num = totalCost - totalGain;
		if (num > 0)
		{
			DeductFromEntity(Api, buyingPlayer.Entity, num);
			GiveToTrader(num);
		}
		else
		{
			GiveOrDrop(buyingPlayer.Entity, new ItemStack(Api.World.GetItem(new AssetLocation("gear-rusty"))), -num, null);
			DeductFromTrader(-num);
		}
		return true;
	}

	public void GiveToTrader(int units)
	{
		if (MoneySlot.Empty)
		{
			MoneySlot.Itemstack = new ItemStack(Api.World.GetItem(new AssetLocation("gear-rusty")), units);
		}
		else
		{
			MoneySlot.Itemstack.StackSize += units;
		}
		MoneySlot.MarkDirty();
	}

	public void DeductFromTrader(int units)
	{
		MoneySlot.Itemstack.StackSize -= units;
		if (MoneySlot.StackSize <= 0)
		{
			MoneySlot.Itemstack = null;
		}
		MoneySlot.MarkDirty();
	}

	public static void DeductFromEntity(ICoreAPI api, EntityAgent eagent, int totalUnitsToDeduct)
	{
		SortedDictionary<int, List<ItemSlot>> moneys = new SortedDictionary<int, List<ItemSlot>>();
		eagent.WalkInventory(delegate(ItemSlot invslot)
		{
			if (invslot is ItemSlotCreative)
			{
				return true;
			}
			if (invslot.Itemstack == null || invslot.Itemstack.Collectible.Attributes == null)
			{
				return true;
			}
			int num3 = CurrencyValuePerItem(invslot);
			if (num3 != 0)
			{
				if (!moneys.TryGetValue(num3, out var value))
				{
					value = new List<ItemSlot>();
				}
				value.Add(invslot);
				moneys[num3] = value;
			}
			return true;
		});
		foreach (KeyValuePair<int, List<ItemSlot>> item in moneys.Reverse())
		{
			int key = item.Key;
			foreach (ItemSlot item2 in item.Value)
			{
				int num = Math.Min(key * item2.StackSize, totalUnitsToDeduct);
				num = num / key * key;
				item2.Itemstack.StackSize -= num / key;
				if (item2.StackSize <= 0)
				{
					item2.Itemstack = null;
				}
				item2.MarkDirty();
				totalUnitsToDeduct -= num;
			}
			if (totalUnitsToDeduct <= 0)
			{
				break;
			}
		}
		if (totalUnitsToDeduct > 0)
		{
			foreach (KeyValuePair<int, List<ItemSlot>> item3 in moneys)
			{
				int key2 = item3.Key;
				foreach (ItemSlot item4 in item3.Value)
				{
					int num2 = Math.Max(key2, Math.Min(key2 * item4.StackSize, totalUnitsToDeduct));
					num2 = num2 / key2 * key2;
					item4.Itemstack.StackSize -= num2 / key2;
					if (item4.StackSize <= 0)
					{
						item4.Itemstack = null;
					}
					item4.MarkDirty();
					totalUnitsToDeduct -= num2;
				}
				if (totalUnitsToDeduct <= 0)
				{
					break;
				}
			}
		}
		if (totalUnitsToDeduct < 0)
		{
			GiveOrDrop(eagent, new ItemStack(api.World.GetItem(new AssetLocation("gear-rusty"))), -totalUnitsToDeduct, null);
		}
	}

	public void GiveOrDrop(EntityAgent eagent, ItemStack stack)
	{
		if (stack != null)
		{
			GiveOrDrop(eagent, stack, stack.StackSize, traderEntity);
		}
	}

	public static void GiveOrDrop(EntityAgent eagent, ItemStack stack, int quantity, EntityTradingHumanoid entityTrader)
	{
		if (stack == null)
		{
			return;
		}
		while (quantity > 0)
		{
			int num = Math.Min(quantity, stack.Collectible.MaxStackSize);
			if (num <= 0)
			{
				break;
			}
			ItemStack itemStack = stack.Clone();
			itemStack.StackSize = num;
			if (entityTrader != null && itemStack.Collectible is ITradeableCollectible tradeableCollectible)
			{
				tradeableCollectible.OnDidTrade(entityTrader, itemStack, EnumTradeDirection.Sell);
			}
			if (!eagent.TryGiveItemStack(itemStack))
			{
				eagent.World.SpawnItemEntity(itemStack, eagent.Pos.XYZ);
			}
			quantity -= num;
		}
	}

	public static int GetPlayerAssets(EntityAgent eagent)
	{
		int totalAssets = 0;
		eagent.WalkInventory(delegate(ItemSlot invslot)
		{
			if (invslot is ItemSlotCreative || !(invslot.Inventory is InventoryBasePlayer))
			{
				return true;
			}
			totalAssets += CurrencyValuePerItem(invslot) * invslot.StackSize;
			return true;
		});
		return totalAssets;
	}

	public int GetTraderAssets()
	{
		int num = 0;
		if (MoneySlot.Empty)
		{
			return 0;
		}
		return num + CurrencyValuePerItem(MoneySlot) * MoneySlot.StackSize;
	}

	private static int CurrencyValuePerItem(ItemSlot slot)
	{
		JsonObject jsonObject = slot.Itemstack?.Collectible?.Attributes?["currency"];
		if (jsonObject != null && jsonObject.Exists)
		{
			JsonObject jsonObject2 = jsonObject["value"];
			if (!jsonObject2.Exists)
			{
				return 0;
			}
			return jsonObject2.AsInt();
		}
		return 0;
	}

	public int GetTotalCost()
	{
		int num = 0;
		for (int i = 0; i < 4; i++)
		{
			ItemSlotTrade buyingCartSlot = GetBuyingCartSlot(i);
			ResolvedTradeItem tradeItem = buyingCartSlot.TradeItem;
			if (tradeItem != null)
			{
				int num2 = buyingCartSlot.StackSize / tradeItem.Stack.StackSize;
				num += tradeItem.Price * num2;
			}
		}
		return num;
	}

	public int GetTotalGain()
	{
		int num = 0;
		for (int i = 0; i < 4; i++)
		{
			ItemSlotSurvival sellingCartSlot = GetSellingCartSlot(i);
			if (sellingCartSlot.Itemstack != null)
			{
				ResolvedTradeItem resolvedTradeItem = GetBuyingConditionsSlot(sellingCartSlot.Itemstack)?.TradeItem;
				if (resolvedTradeItem != null)
				{
					int num2 = sellingCartSlot.StackSize / resolvedTradeItem.Stack.StackSize;
					num += resolvedTradeItem.Price * num2;
				}
			}
		}
		return num;
	}

	protected override ItemSlot NewSlot(int slotId)
	{
		if (slotId < 36)
		{
			return new ItemSlotTrade(this, slotId > 19 && slotId <= 35);
		}
		return new ItemSlotBuying(this);
	}

	public ItemSlotTrade GetSellingSlot(int index)
	{
		return slots[index] as ItemSlotTrade;
	}

	public ItemSlotTrade GetBuyingSlot(int index)
	{
		return slots[20 + index] as ItemSlotTrade;
	}

	public ItemSlotTrade GetBuyingCartSlot(int index)
	{
		return slots[16 + index] as ItemSlotTrade;
	}

	public ItemSlotSurvival GetSellingCartSlot(int index)
	{
		return slots[36 + index] as ItemSlotSurvival;
	}

	public ItemSlotTrade GetBuyingConditionsSlot(ItemStack forStack)
	{
		for (int i = 0; i < 16; i++)
		{
			ItemSlotTrade buyingSlot = GetBuyingSlot(i);
			if (buyingSlot.Itemstack != null)
			{
				string[] ignoreAttributeSubTrees = ((buyingSlot.TradeItem.AttributesToIgnore == null) ? ignoredAttrs : ignoredAttrs.Append<string>(buyingSlot.TradeItem.AttributesToIgnore.Split(',')));
				if ((buyingSlot.Itemstack.Equals(Api.World, forStack, ignoreAttributeSubTrees) || buyingSlot.Itemstack.Satisfies(forStack)) && forStack.Collectible.IsReasonablyFresh(traderEntity.World, forStack))
				{
					return buyingSlot;
				}
			}
		}
		return null;
	}

	public ItemSlotTrade GetSellingConditionsSlot(ItemStack forStack)
	{
		for (int i = 0; i < 16; i++)
		{
			ItemSlotTrade sellingSlot = GetSellingSlot(i);
			if (sellingSlot.Itemstack != null && sellingSlot.Itemstack.Equals(Api.World, forStack, GlobalConstants.IgnoredStackAttributes))
			{
				return sellingSlot;
			}
		}
		return null;
	}

	public override object Close(IPlayer player)
	{
		object result = base.Close(player);
		for (int i = 0; i < 4; i++)
		{
			slots[i + 16].Itemstack = null;
			Api.World.SpawnItemEntity(slots[i + 36].Itemstack, traderEntity.ServerPos.XYZ);
			slots[i + 36].Itemstack = null;
		}
		return result;
	}

	public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
	{
		WeightedSlot weightedSlot = new WeightedSlot();
		if (PutLocked || sourceSlot.Inventory == this)
		{
			return weightedSlot;
		}
		if (!IsTraderInterestedIn(sourceSlot.Itemstack))
		{
			return weightedSlot;
		}
		if (CurrencyValuePerItem(sourceSlot) != 0)
		{
			return weightedSlot;
		}
		for (int i = 0; i < 4; i++)
		{
			ItemSlot sellingCartSlot = GetSellingCartSlot(i);
			if ((skipSlots == null || !skipSlots.Contains(sellingCartSlot)) && sellingCartSlot.CanTakeFrom(sourceSlot))
			{
				float suitability = GetSuitability(sourceSlot, sellingCartSlot, sellingCartSlot.Itemstack != null);
				if (weightedSlot.slot == null || weightedSlot.weight < suitability)
				{
					weightedSlot.slot = sellingCartSlot;
					weightedSlot.weight = suitability;
				}
			}
		}
		return weightedSlot;
	}
}
