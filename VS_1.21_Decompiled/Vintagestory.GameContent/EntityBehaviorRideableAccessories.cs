using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorRideableAccessories : EntityBehaviorAttachable
{
	protected bool hasBridle;

	public EntityBehaviorRideableAccessories(Entity entity)
		: base(entity)
	{
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		EntityBehaviorRideable behavior = entity.GetBehavior<EntityBehaviorRideable>();
		if (behavior != null)
		{
			behavior.CanRide += EntityBehaviorDressable_CanRide;
			behavior.CanTurn += Bh_CanTurn;
		}
	}

	private bool Bh_CanTurn(IMountableSeat seat, out string errorMessage)
	{
		hasBridle = false;
		foreach (ItemSlot item in Inventory)
		{
			if (!item.Empty && item.Itemstack.Collectible.Attributes != null && item.Itemstack.Collectible.Attributes.IsTrue("isBridle"))
			{
				hasBridle = true;
				break;
			}
		}
		errorMessage = (hasBridle ? null : "nobridle");
		return hasBridle;
	}

	private bool EntityBehaviorDressable_CanRide(IMountableSeat seat, out string errorMessage)
	{
		hasBridle = false;
		foreach (ItemSlot item in Inventory)
		{
			if (!item.Empty && item.Itemstack.Collectible.Attributes != null && item.Itemstack.Collectible.Attributes.IsTrue("isSaddle"))
			{
				hasBridle = true;
				break;
			}
		}
		errorMessage = (hasBridle ? null : "nosaddle");
		return hasBridle;
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
		base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
	}
}
