using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityTradingHumanoid : EntityDressedHumanoid
{
	public const int PlayerStoppedInteracting = 1212;

	public InventoryTrader Inventory;

	public TradeProperties TradeProps;

	public List<EntityPlayer> interactingWithPlayer = new List<EntityPlayer>();

	protected GuiDialog dlg;

	protected int tickCount;

	protected double doubleRefreshIntervalDays = 7.0;

	private bool wasImported;

	protected EntityBehaviorConversable ConversableBh => GetBehavior<EntityBehaviorConversable>();

	public virtual EntityTalkUtil TalkUtil { get; }

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		EntityBehaviorConversable behavior = GetBehavior<EntityBehaviorConversable>();
		if (behavior != null)
		{
			behavior.OnControllerCreated = (Action<DialogueController>)Delegate.Combine(behavior.OnControllerCreated, (Action<DialogueController>)delegate(DialogueController controller)
			{
				controller.DialogTriggers += Dialog_DialogTriggers;
			});
		}
		if (Inventory == null)
		{
			Inventory = new InventoryTrader("traderInv", EntityId.ToString() ?? "", api);
		}
		if (api.Side == EnumAppSide.Server)
		{
			string text = base.Properties.Attributes?["tradePropsFile"].AsString();
			AssetLocation assetLocation = null;
			try
			{
				if (text != null)
				{
					assetLocation = ((text == null) ? null : AssetLocation.Create(text, Code.Domain));
					TradeProps = api.Assets.Get(assetLocation.WithPathAppendixOnce(".json")).ToObject<TradeProperties>();
				}
				else
				{
					TradeProps = base.Properties.Attributes["tradeProps"]?.AsObject<TradeProperties>();
				}
			}
			catch (Exception e)
			{
				api.World.Logger.Error("Failed deserializing TradeProperties for trader {0}, exception logged to verbose debug", properties.Code);
				api.World.Logger.Error(e);
				api.World.Logger.VerboseDebug("Failed deserializing TradeProperties:");
				api.World.Logger.VerboseDebug("=================");
				api.World.Logger.VerboseDebug("Tradeprops json:");
				if (assetLocation != null)
				{
					api.World.Logger.VerboseDebug("File path {0}:", assetLocation);
				}
				api.World.Logger.VerboseDebug("{0}", base.Properties.Server.Attributes["tradeProps"].ToJsonToken());
			}
		}
		try
		{
			Inventory.LateInitialize("traderInv-" + EntityId, api, this);
		}
		catch (Exception e2)
		{
			api.World.Logger.Error("Failed initializing trader inventory. Will recreate. Exception logged to verbose debug");
			api.World.Logger.Error(e2);
			api.World.Logger.VerboseDebug("Failed initializing trader inventory. Will recreate.");
			WatchedAttributes.RemoveAttribute("traderInventory");
			Inventory = new InventoryTrader("traderInv", EntityId.ToString() ?? "", api);
			Inventory.LateInitialize("traderInv-" + EntityId, api, this);
			RefreshBuyingSellingInventory();
		}
	}

	public override void OnEntitySpawn()
	{
		base.OnEntitySpawn();
		if (World.Api.Side == EnumAppSide.Server)
		{
			setupTaskBlocker();
			reloadTradingList();
		}
	}

	private void reloadTradingList()
	{
		if (TradeProps != null)
		{
			RefreshBuyingSellingInventory();
			WatchedAttributes.SetDouble("lastRefreshTotalDays", World.Calendar.TotalDays - World.Rand.NextDouble() * 6.0);
			Inventory.MoneySlot.Itemstack = null;
			Inventory.GiveToTrader((int)TradeProps.Money.nextFloat(1f, World.Rand));
		}
	}

	public override void DidImportOrExport(BlockPos startPos)
	{
		base.DidImportOrExport(startPos);
		wasImported = true;
	}

	public override void OnEntityLoaded()
	{
		base.OnEntityLoaded();
		if (Api.Side == EnumAppSide.Server)
		{
			setupTaskBlocker();
			if (wasImported)
			{
				reloadTradingList();
			}
		}
	}

	protected void setupTaskBlocker()
	{
		EntityBehaviorTaskAI behavior = GetBehavior<EntityBehaviorTaskAI>();
		if (behavior != null)
		{
			behavior.TaskManager.OnShouldExecuteTask += (IAiTask task) => interactingWithPlayer.Count == 0;
		}
		EntityBehaviorActivityDriven behavior2 = GetBehavior<EntityBehaviorActivityDriven>();
		if (behavior2 != null)
		{
			behavior2.OnShouldRunActivitySystem += () => (interactingWithPlayer.Count > 0) ? EnumInteruptionType.TradeRequested : EnumInteruptionType.None;
		}
	}

	protected void RefreshBuyingSellingInventory(float refreshChance = 1.1f)
	{
		if (TradeProps == null)
		{
			return;
		}
		TradeProps.Buying.List.Shuffle(World.Rand);
		int num = Math.Min(TradeProps.Buying.List.Length, TradeProps.Buying.MaxItems);
		TradeProps.Selling.List.Shuffle(World.Rand);
		int num2 = Math.Min(TradeProps.Selling.List.Length, TradeProps.Selling.MaxItems);
		Stack<TradeItem> stack = new Stack<TradeItem>();
		Stack<TradeItem> stack2 = new Stack<TradeItem>();
		ItemSlotTrade[] sellingSlots = Inventory.SellingSlots;
		ItemSlotTrade[] buyingSlots = Inventory.BuyingSlots;
		string[] ignoredAttributes = GlobalConstants.IgnoredStackAttributes.Append("condition");
		for (int i = 0; i < TradeProps.Selling.List.Length; i++)
		{
			if (stack2.Count >= num2)
			{
				break;
			}
			TradeItem item = TradeProps.Selling.List[i];
			if (item.Resolve(World, "tradeItem resolver") && !sellingSlots.Any((ItemSlotTrade slot) => slot?.Itemstack != null && slot.TradeItem.Stock > 0 && (item.ResolvedItemstack?.Equals(World, slot.Itemstack, ignoredAttributes) ?? false)))
			{
				stack2.Push(item);
			}
		}
		for (int num3 = 0; num3 < TradeProps.Buying.List.Length; num3++)
		{
			if (stack.Count >= num)
			{
				break;
			}
			TradeItem item2 = TradeProps.Buying.List[num3];
			if (item2.Resolve(World, "tradeItem resolver") && !buyingSlots.Any((ItemSlotTrade slot) => slot?.Itemstack != null && slot.TradeItem.Stock > 0 && (item2.ResolvedItemstack?.Equals(World, slot.Itemstack, ignoredAttributes) ?? false)))
			{
				stack.Push(item2);
			}
		}
		replaceTradeItems(stack, buyingSlots, num, refreshChance, EnumTradeDirection.Buy);
		replaceTradeItems(stack2, sellingSlots, num2, refreshChance, EnumTradeDirection.Sell);
		ITreeAttribute orCreateTradeStore = GetOrCreateTradeStore();
		Inventory.ToTreeAttributes(orCreateTradeStore);
		WatchedAttributes.MarkAllDirty();
	}

	protected void replaceTradeItems(Stack<TradeItem> newItems, ItemSlotTrade[] slots, int quantity, float refreshChance, EnumTradeDirection tradeDir)
	{
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < quantity; i++)
		{
			if (World.Rand.NextDouble() > (double)refreshChance)
			{
				continue;
			}
			if (newItems.Count == 0)
			{
				break;
			}
			TradeItem newTradeItem = newItems.Pop();
			if (newTradeItem.ResolvedItemstack.Collectible is ITradeableCollectible tradeableCollectible && !tradeableCollectible.ShouldTrade(this, newTradeItem, tradeDir))
			{
				i--;
				continue;
			}
			int num = slots.IndexOf((ItemSlotTrade bslot) => bslot.Itemstack != null && bslot.TradeItem.Stock == 0 && (newTradeItem?.ResolvedItemstack.Equals(World, bslot.Itemstack, GlobalConstants.IgnoredStackAttributes) ?? false));
			ItemSlotTrade itemSlotTrade;
			if (num != -1)
			{
				itemSlotTrade = slots[num];
				hashSet.Add(num);
			}
			else
			{
				for (; hashSet.Contains(i); i++)
				{
				}
				if (i >= slots.Length)
				{
					break;
				}
				itemSlotTrade = slots[i];
				hashSet.Add(i);
			}
			ResolvedTradeItem resolvedTradeItem = newTradeItem.Resolve(World);
			if (resolvedTradeItem.Stock > 0)
			{
				itemSlotTrade.SetTradeItem(resolvedTradeItem);
				itemSlotTrade.MarkDirty();
			}
		}
	}

	protected virtual int Dialog_DialogTriggers(EntityAgent triggeringEntity, string value, JsonObject data)
	{
		if (value == "opentrade")
		{
			if (!Alive || !(triggeringEntity.Pos.SquareDistanceTo(Pos) <= 7f))
			{
				if (World.Side == EnumAppSide.Server)
				{
					IServerPlayer player = (triggeringEntity as EntityPlayer).Player as IServerPlayer;
					(Api as ICoreServerAPI).Network.SendEntityPacket(player, EntityId, 1212);
				}
				return 0;
			}
			ConversableBh.Dialog?.TryClose();
			TryOpenTradeDialog(triggeringEntity);
			interactingWithPlayer.Add(triggeringEntity as EntityPlayer);
		}
		return -1;
	}

	private void TryOpenTradeDialog(EntityAgent forEntity)
	{
		if (World.Side != EnumAppSide.Client)
		{
			return;
		}
		EntityPlayer entityPlayer = forEntity as EntityPlayer;
		IPlayer player = World.PlayerByUid(entityPlayer.PlayerUID);
		ICoreClientAPI coreClientAPI = (ICoreClientAPI)Api;
		GuiDialog guiDialog = dlg;
		if (guiDialog == null || !guiDialog.IsOpened())
		{
			if (coreClientAPI.Gui.OpenedGuis.FirstOrDefault((GuiDialog dlg) => dlg is GuiDialogTrader && dlg.IsOpened()) == null)
			{
				coreClientAPI.Network.SendEntityPacket(EntityId, 1001);
				player.InventoryManager.OpenInventory(Inventory);
				dlg = new GuiDialogTrader(Inventory, this, World.Api as ICoreClientAPI);
				dlg.TryOpen();
			}
			else
			{
				coreClientAPI.TriggerIngameError(this, "onlyonedialog", Lang.Get("Can only trade with one trader at a time"));
			}
		}
		else
		{
			coreClientAPI.World.Player.InventoryManager.CloseInventoryAndSync(Inventory);
		}
	}

	public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data)
	{
		base.OnReceivedClientPacket(player, packetid, data);
		if (packetid < 1000)
		{
			Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
			return;
		}
		if (packetid == 1000 && Inventory.TryBuySell(player) == EnumTransactionResult.Success)
		{
			(Api as ICoreServerAPI).WorldManager.GetChunk(ServerPos.AsBlockPos)?.MarkModified();
			AnimManager.StopAnimation("idle");
			AnimManager.StartAnimation(new AnimationMetaData
			{
				Animation = "nod",
				Code = "nod",
				Weight = 10f,
				EaseOutSpeed = 10000f,
				EaseInSpeed = 10000f
			});
			TreeAttribute treeAttribute = new TreeAttribute();
			Inventory.ToTreeAttributes(treeAttribute);
			(Api as ICoreServerAPI).Network.BroadcastEntityPacket(EntityId, 1234, treeAttribute.ToBytes());
		}
		if (packetid == 1001)
		{
			player.InventoryManager.OpenInventory(Inventory);
		}
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		base.OnReceivedServerPacket(packetid, data);
		if (packetid == 1212)
		{
			dlg?.TryClose();
			interactingWithPlayer.Remove((Api as ICoreClientAPI).World.Player.Entity);
			(Api as ICoreClientAPI).World.Player.InventoryManager.CloseInventoryAndSync(Inventory);
		}
		if (packetid == 1234)
		{
			TreeAttribute treeAttribute = new TreeAttribute();
			treeAttribute.FromBytes(data);
			Inventory.FromTreeAttributes(treeAttribute);
		}
	}

	public double NextRefreshTotalDays()
	{
		double num = WatchedAttributes.GetDouble("lastRefreshTotalDays", World.Calendar.TotalDays - 10.0);
		return doubleRefreshIntervalDays - (World.Calendar.TotalDays - num);
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (World.Side == EnumAppSide.Server && TradeProps != null && tickCount++ > 200)
		{
			double num = WatchedAttributes.GetDouble("lastRefreshTotalDays", World.Calendar.TotalDays - 10.0);
			int num2 = 10;
			while (World.Calendar.TotalDays - num > doubleRefreshIntervalDays && interactingWithPlayer.Count == 0 && num2-- > 0)
			{
				int traderAssets = Inventory.GetTraderAssets();
				double num3 = 0.07 + World.Rand.NextDouble() * 0.21;
				float num4 = TradeProps.Money.nextFloat(1f, World.Rand);
				int units = (int)Math.Max(-3.0, Math.Min(num4, (double)traderAssets + num3 * (double)(int)num4) - (double)traderAssets);
				Inventory.GiveToTrader(units);
				RefreshBuyingSellingInventory(0.5f);
				num += doubleRefreshIntervalDays;
				WatchedAttributes.SetDouble("lastRefreshTotalDays", num);
				tickCount = 1;
			}
			if (num2 <= 0)
			{
				WatchedAttributes.SetDouble("lastRefreshTotalDays", World.Calendar.TotalDays + 1.0 + World.Rand.NextDouble() * 5.0);
			}
		}
		if (interactingWithPlayer.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < interactingWithPlayer.Count; i++)
		{
			EntityPlayer entityPlayer = interactingWithPlayer[i];
			if (!Alive || entityPlayer.Pos.SquareDistanceTo(Pos) > 5f)
			{
				interactingWithPlayer.Remove(entityPlayer);
				Inventory.Close(entityPlayer.Player);
				i--;
			}
		}
		if (Api is ICoreClientAPI coreClientAPI && !interactingWithPlayer.Contains(coreClientAPI.World.Player.Entity))
		{
			dlg?.TryClose();
		}
	}

	public override void FromBytes(BinaryReader reader, bool forClient)
	{
		base.FromBytes(reader, forClient);
		if (Inventory == null)
		{
			Inventory = new InventoryTrader("traderInv", EntityId.ToString() ?? "", null);
		}
		Inventory.FromTreeAttributes(GetOrCreateTradeStore());
	}

	public override void ToBytes(BinaryWriter writer, bool forClient)
	{
		Inventory.ToTreeAttributes(GetOrCreateTradeStore());
		base.ToBytes(writer, forClient);
	}

	private ITreeAttribute GetOrCreateTradeStore()
	{
		if (!WatchedAttributes.HasAttribute("traderInventory"))
		{
			ITreeAttribute treeAttribute = new TreeAttribute();
			Inventory.ToTreeAttributes(treeAttribute);
			WatchedAttributes["traderInventory"] = treeAttribute;
		}
		return WatchedAttributes["traderInventory"] as ITreeAttribute;
	}
}
