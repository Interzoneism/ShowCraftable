using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class InWorldContainer
{
	protected RoomRegistry roomReg;

	protected Room room;

	protected ICoreAPI Api;

	protected float temperatureCached = -1000f;

	protected PositionProviderDelegate positionProvider;

	protected Action onRequireSyncToClient;

	public InventorySupplierDelegate inventorySupplier;

	private string treeAttrKey;

	private InventoryBase prevInventory;

	private bool didInit;

	public Room Room => room;

	public InventoryBase Inventory => inventorySupplier();

	public InWorldContainer(InventorySupplierDelegate inventorySupplier, string treeAttrKey)
	{
		this.inventorySupplier = inventorySupplier;
		this.treeAttrKey = treeAttrKey;
	}

	public void Init(ICoreAPI Api, PositionProviderDelegate positionProvider, Action onRequireSyncToClient)
	{
		this.Api = Api;
		this.positionProvider = positionProvider;
		this.onRequireSyncToClient = onRequireSyncToClient;
		roomReg = Api.ModLoader.GetModSystem<RoomRegistry>();
		LateInit();
	}

	public void Reset()
	{
		didInit = false;
	}

	public void LateInit()
	{
		if (Inventory == null || didInit)
		{
			return;
		}
		if (prevInventory != null && Inventory != prevInventory)
		{
			prevInventory.OnAcquireTransitionSpeed -= Inventory_OnAcquireTransitionSpeed;
			if (Api.Side == EnumAppSide.Client)
			{
				prevInventory.OnInventoryOpened -= Inventory_OnInventoryOpenedClient;
			}
		}
		didInit = true;
		Inventory.ResolveBlocksOrItems();
		Inventory.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
		if (Api.Side == EnumAppSide.Client)
		{
			Inventory.OnInventoryOpened += Inventory_OnInventoryOpenedClient;
		}
		else
		{
			Inventory.SlotModified += Inventory_SlotModified;
		}
		prevInventory = Inventory;
	}

	private void Inventory_SlotModified(int obj)
	{
		if (!(Inventory is InventoryBasePlayer))
		{
			Api.World.BlockAccessor.GetChunkAtBlockPos(positionProvider())?.MarkModified();
		}
	}

	private void Inventory_OnInventoryOpenedClient(IPlayer player)
	{
		OnTick(1f);
	}

	public virtual void OnTick(float dt)
	{
		if (Api.Side == EnumAppSide.Client)
		{
			return;
		}
		temperatureCached = -1000f;
		if (!HasTransitionables())
		{
			return;
		}
		room = roomReg.GetRoomForPosition(positionProvider());
		if (room.AnyChunkUnloaded != 0)
		{
			return;
		}
		foreach (ItemSlot item in Inventory)
		{
			if (item.Itemstack != null)
			{
				AssetLocation code = item.Itemstack.Collectible.Code;
				item.Itemstack.Collectible.UpdateAndGetTransitionStates(Api.World, item);
				if (item.Itemstack?.Collectible.Code != code)
				{
					onRequireSyncToClient();
				}
			}
		}
		temperatureCached = -1000f;
	}

	protected virtual bool HasTransitionables()
	{
		foreach (ItemSlot item in Inventory)
		{
			ItemStack itemstack = item.Itemstack;
			if (itemstack != null && itemstack.Collectible.RequiresTransitionableTicking(Api.World, itemstack))
			{
				return true;
			}
		}
		return false;
	}

	protected virtual float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul)
	{
		float num = ((Api != null && transType == EnumTransitionType.Perish) ? GetPerishRate() : 1f);
		if (transType == EnumTransitionType.Dry || transType == EnumTransitionType.Melt)
		{
			num = 0.25f;
		}
		return baseMul * num;
	}

	public virtual float GetPerishRate()
	{
		BlockPos blockPos = positionProvider().Copy();
		blockPos.Y = Api.World.SeaLevel;
		float temperature = temperatureCached;
		if (temperature < -999f)
		{
			temperature = Api.World.BlockAccessor.GetClimateAt(blockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays).Temperature;
			if (Api.Side == EnumAppSide.Server)
			{
				temperatureCached = temperature;
			}
		}
		if (room == null)
		{
			room = roomReg.GetRoomForPosition(positionProvider());
		}
		float num = 0f;
		float num2 = (float)room.SkylightCount / (float)Math.Max(1, room.SkylightCount + room.NonSkylightCount);
		if (room.IsSmallRoom)
		{
			num = 1f;
			num -= 0.4f * num2;
			num -= 0.5f * GameMath.Clamp((float)room.NonCoolingWallCount / (float)Math.Max(1, room.CoolingWallCount), 0f, 1f);
		}
		int lightLevel = Api.World.BlockAccessor.GetLightLevel(positionProvider(), EnumLightLevelType.OnlySunLight);
		float num3 = 0.1f;
		num3 = (room.IsSmallRoom ? (num3 + (0.3f * num + 1.75f * num2)) : ((!((float)room.ExitCount <= 0.1f * (float)(room.CoolingWallCount + room.NonCoolingWallCount))) ? (num3 + 0.5f * num2) : (num3 + 1.25f * num2)));
		num3 = GameMath.Clamp(num3, 0f, 1.5f);
		float num4 = temperature + (float)GameMath.Clamp(lightLevel - 11, 0, 10) * num3;
		float v = 5f;
		float val = GameMath.Lerp(num4, v, num);
		val = Math.Min(val, num4);
		return Math.Max(0.1f, Math.Min(2.4f, (float)Math.Pow(3.0, (double)(val / 19f) - 1.2) - 0.1f));
	}

	public void ReloadRoom()
	{
		room = roomReg.GetRoomForPosition(positionProvider());
	}

	public void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		foreach (ItemSlot item in Inventory)
		{
			item.Itemstack?.Collectible.OnStoreCollectibleMappings(Api.World, item, blockIdMapping, itemIdMapping);
		}
	}

	public void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		foreach (ItemSlot item in Inventory)
		{
			if (item.Itemstack != null)
			{
				if (!item.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
				{
					item.Itemstack = null;
				}
				else
				{
					item.Itemstack.Collectible.OnLoadCollectibleMappings(worldForResolve, item, oldBlockIdMapping, oldItemIdMapping, resolveImports);
				}
				if (item.Itemstack?.Collectible is IResolvableCollectible resolvableCollectible)
				{
					resolvableCollectible.Resolve(item, worldForResolve, resolveImports);
				}
			}
		}
	}

	public void ToTreeAttributes(ITreeAttribute tree)
	{
		if (Inventory != null)
		{
			ITreeAttribute treeAttribute = new TreeAttribute();
			Inventory.ToTreeAttributes(treeAttribute);
			tree[treeAttrKey] = treeAttribute;
		}
	}

	public void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		Inventory.FromTreeAttributes(tree.GetTreeAttribute(treeAttrKey));
	}
}
