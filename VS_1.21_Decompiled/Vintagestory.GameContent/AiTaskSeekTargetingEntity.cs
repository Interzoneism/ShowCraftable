using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class AiTaskSeekTargetingEntity : AiTaskSeekEntity
{
	private Entity guardedEntity;

	private Entity lastattackingEntity;

	private long lastattackingEntityFoundMs;

	public AiTaskSeekTargetingEntity(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		searchWaitMs = 1000;
	}

	public override bool ShouldExecute()
	{
		if (entity.World.Rand.NextDouble() < 0.1)
		{
			string text = entity.WatchedAttributes.GetString("guardedPlayerUid");
			if (text != null)
			{
				guardedEntity = entity.World.PlayerByUid(text)?.Entity;
			}
			else
			{
				long entityId = entity.WatchedAttributes.GetLong("guardedEntityId", 0L);
				guardedEntity = entity.World.GetEntityById(entityId);
			}
		}
		if (guardedEntity == null)
		{
			return false;
		}
		if (entity.WatchedAttributes.GetBool("commandSit"))
		{
			return false;
		}
		if (entity.World.ElapsedMilliseconds - lastattackingEntityFoundMs > 30000)
		{
			lastattackingEntity = null;
		}
		return base.ShouldExecute();
	}

	public override void StartExecute()
	{
		base.StartExecute();
		lastattackingEntityFoundMs = entity.World.ElapsedMilliseconds;
		lastattackingEntity = targetEntity;
	}

	public override bool IsTargetableEntity(Entity e, float range, bool ignoreEntityCode = false)
	{
		if (!base.IsTargetableEntity(e, range, ignoreEntityCode))
		{
			return false;
		}
		IAiTask[] array = e.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager.ActiveTasksBySlot;
		if (e != lastattackingEntity || !e.Alive)
		{
			return array?.FirstOrDefault((IAiTask task) => task is AiTaskBaseTargetable aiTaskBaseTargetable && aiTaskBaseTargetable.TargetEntity == guardedEntity && aiTaskBaseTargetable.AggressiveTargeting) != null;
		}
		return true;
	}
}
