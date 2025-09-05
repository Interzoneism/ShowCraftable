using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class ModSystemStopRaiseShieldAnim : ModSystem
{
	private ICoreClientAPI capi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.AfterActiveSlotChanged += Event_AfterActiveSlotChanged;
	}

	public override void AssetsFinalize(ICoreAPI api)
	{
		capi.World.Player.InventoryManager.GetHotbarInventory().SlotModified += ModSystemStopRaiseShieldAnim_SlotModified;
	}

	private void ModSystemStopRaiseShieldAnim_SlotModified(int slotid)
	{
		maybeStopRaiseShield();
	}

	private void Event_AfterActiveSlotChanged(ActiveSlotChangeEventArgs obj)
	{
		maybeStopRaiseShield();
	}

	private void maybeStopRaiseShield()
	{
		EntityPlayer entity = capi.World.Player.Entity;
		if (!(entity.RightHandItemSlot.Itemstack?.Item is ItemShield) && entity.AnimManager.IsAnimationActive("raiseshield-right"))
		{
			entity.AnimManager.StopAnimation("raiseshield-right");
		}
	}
}
