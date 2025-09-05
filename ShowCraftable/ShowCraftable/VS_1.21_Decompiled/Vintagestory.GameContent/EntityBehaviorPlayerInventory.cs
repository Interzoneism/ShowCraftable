using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorPlayerInventory : EntityBehaviorTexturedClothing
{
	private bool slotModifiedRegistered;

	private float accum;

	private IPlayer Player => (entity as EntityPlayer).Player;

	public override InventoryBase Inventory => Player?.InventoryManager.GetOwnInventory("character") as InventoryBase;

	public override string InventoryClassName => "gear";

	public override string PropertyName()
	{
		return "playerinventory";
	}

	public EntityBehaviorPlayerInventory(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		IInventory inventory = Player?.InventoryManager.GetOwnInventory("backpack");
		if (inventory != null)
		{
			inventory.SlotModified -= base.Inventory_SlotModifiedBackpack;
		}
	}

	protected override void loadInv()
	{
	}

	public override void storeInv()
	{
	}

	public override void OnGameTick(float deltaTime)
	{
		if (!slotModifiedRegistered)
		{
			slotModifiedRegistered = true;
			IInventory inventory = Player?.InventoryManager.GetOwnInventory("backpack");
			if (inventory != null)
			{
				inventory.SlotModified += base.Inventory_SlotModifiedBackpack;
			}
		}
		base.OnGameTick(deltaTime);
		accum += deltaTime;
		if (accum > 1f)
		{
			entity.Attributes.SetBool("hasProtectiveEyeGear", Inventory != null && Inventory.FirstOrDefault((ItemSlot slot) => !slot.Empty && (slot.Itemstack.Collectible.Attributes?.IsTrue("eyeprotective") ?? false)) != null);
		}
	}

	public override void OnTesselation(ref Shape entityShape, string shapePathForLogging, ref bool shapeIsCloned, ref string[] willDeleteElements)
	{
		IInventory inventory = Player?.InventoryManager.GetOwnInventory("backpack");
		Dictionary<string, ItemSlot> dictionary = new Dictionary<string, ItemSlot>();
		int num = 0;
		while (inventory != null && num < 4)
		{
			ItemSlot itemSlot = inventory[num];
			if (!itemSlot.Empty)
			{
				dictionary[itemSlot.Itemstack.ItemAttributes?["attachableToEntity"]?["categoryCode"]?.AsString() ?? (itemSlot.Itemstack.Class.ToString() + itemSlot.Itemstack.Collectible.Id)] = itemSlot;
			}
			num++;
		}
		foreach (KeyValuePair<string, ItemSlot> item in dictionary)
		{
			entityShape = addGearToShape(entityShape, item.Value, "default", shapePathForLogging, ref shapeIsCloned, ref willDeleteElements);
		}
		base.OnTesselation(ref entityShape, shapePathForLogging, ref shapeIsCloned, ref willDeleteElements);
	}

	public override void OnEntityDeath(DamageSource damageSourceForDeath)
	{
		Api.Event.EnqueueMainThreadTask(delegate
		{
			EntityServerProperties server = entity.Properties.Server;
			if (server == null || server.Attributes?.GetBool("keepContents") != true)
			{
				Player.InventoryManager.OnDeath();
			}
			EntityServerProperties server2 = entity.Properties.Server;
			if (server2 != null && server2.Attributes?.GetBool("dropArmorOnDeath") == true)
			{
				foreach (ItemSlot item in Inventory)
				{
					if (!item.Empty)
					{
						JsonObject itemAttributes = item.Itemstack.ItemAttributes;
						if (itemAttributes != null && itemAttributes["protectionModifiers"].Exists)
						{
							Api.World.SpawnItemEntity(item.Itemstack, entity.ServerPos.XYZ);
							item.Itemstack = null;
							item.MarkDirty();
						}
					}
				}
			}
		}, "dropinventoryondeath");
	}
}
