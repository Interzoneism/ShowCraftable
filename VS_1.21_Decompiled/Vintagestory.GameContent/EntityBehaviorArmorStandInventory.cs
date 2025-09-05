using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class EntityBehaviorArmorStandInventory : EntityBehaviorSeraphInventory
{
	public override string InventoryClassName => "inventory";

	public override string PropertyName()
	{
		return "armorstandinventory";
	}

	public EntityBehaviorArmorStandInventory(Entity entity)
		: base(entity)
	{
	}

	protected override void Inventory_SlotModified(int slotid)
	{
		base.Inventory_SlotModified(slotid);
		storeInv();
	}
}
