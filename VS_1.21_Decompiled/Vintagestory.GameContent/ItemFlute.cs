using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class ItemFlute : Item
{
	protected string GroupCode = "mountableanimal";

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		long elapsedMilliseconds = api.World.ElapsedMilliseconds;
		long num = slot.Itemstack.Attributes.GetLong("lastPlayerMs", -99999L);
		if (num > elapsedMilliseconds)
		{
			num = elapsedMilliseconds - 4001;
		}
		if (elapsedMilliseconds - num > 4000)
		{
			slot.Itemstack.Attributes.SetLong("lastPlayerMs", elapsedMilliseconds);
			api.World.PlaySoundAt(new AssetLocation("sounds/instrument/elkcall"), byEntity, (byEntity as EntityPlayer)?.Player, 0.75f, 32f, 0.5f);
			if (api.Side == EnumAppSide.Server)
			{
				callElk(byEntity);
			}
			handling = EnumHandHandling.PreventDefault;
		}
	}

	private void callElk(EntityAgent byEntity)
	{
		IPlayer player = (byEntity as EntityPlayer).Player;
		if (!api.ModLoader.GetModSystem<ModSystemEntityOwnership>().OwnerShipsByPlayerUid.TryGetValue(player.PlayerUID, out var value) || value == null || !value.TryGetValue(GroupCode, out var value2))
		{
			return;
		}
		Entity entityById = api.World.GetEntityById(value2.EntityId);
		if (entityById == null)
		{
			return;
		}
		EntityBehaviorMortallyWoundable behavior = entityById.GetBehavior<EntityBehaviorMortallyWoundable>();
		if ((behavior != null && behavior.HealthState == EnumEntityHealthState.MortallyWounded) || (behavior != null && behavior.HealthState == EnumEntityHealthState.Dead))
		{
			return;
		}
		AiTaskManager taskManager = entityById.GetBehavior<EntityBehaviorTaskAI>().TaskManager;
		AiTaskComeToOwner aiTaskComeToOwner = taskManager.AllTasks.FirstOrDefault((IAiTask t) => t is AiTaskComeToOwner) as AiTaskComeToOwner;
		if (entityById.ServerPos.DistanceTo(byEntity.ServerPos) > (double)aiTaskComeToOwner.TeleportMaxRange)
		{
			return;
		}
		IMountable mountable = entityById?.GetInterface<IMountable>();
		if (mountable != null)
		{
			if (mountable.IsMountedBy(player.Entity))
			{
				return;
			}
			if (mountable.AnyMounted())
			{
				entityById.GetBehavior<EntityBehaviorRideable>()?.UnmnountPassengers();
			}
		}
		entityById.AlwaysActive = true;
		entityById.State = EnumEntityState.Active;
		aiTaskComeToOwner.allowTeleportCount = 1;
		taskManager.StopTasks();
		taskManager.ExecuteTask(aiTaskComeToOwner, 0);
	}
}
