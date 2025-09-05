using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class AiTaskFollowLeadHolderR : AiTaskBaseTargetableR
{
	protected ClothManager clothManager;

	protected EntityBehaviorRopeTieable? ropeTieableBehavior;

	protected long lastGoalReachedMs;

	protected ClothSystem? clothSystem;

	private AiTaskFollowLeadHolderConfig Config => GetConfig<AiTaskFollowLeadHolderConfig>();

	public AiTaskFollowLeadHolderR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		clothManager = entity.World.Api.ModLoader.GetModSystem<ClothManager>();
		ropeTieableBehavior = entity.GetBehavior<EntityBehaviorRopeTieable>();
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskFollowLeadHolderConfig>(entity, taskConfig, aiConfig);
	}

	public override void AfterInitialize()
	{
		base.AfterInitialize();
		ropeTieableBehavior = entity.GetBehavior<EntityBehaviorRopeTieable>();
	}

	public override bool ShouldExecute()
	{
		if (!PreconditionsSatisficed())
		{
			return false;
		}
		if (GetOwnGeneration() < Config.MinGeneration)
		{
			return false;
		}
		if (entity.World.ElapsedMilliseconds - lastGoalReachedMs < Config.GoalReachedCooldownMs)
		{
			return false;
		}
		int[] array = ropeTieableBehavior?.ClothIds?.value;
		if (array == null)
		{
			return false;
		}
		int[] array2 = array;
		foreach (int clothid in array2)
		{
			clothSystem = clothManager.GetClothSystem(clothid);
			if (clothSystem == null)
			{
				continue;
			}
			ClothPoint pinnedToPoint = GetPinnedToPoint(entity);
			if (pinnedToPoint != null)
			{
				targetEntity = pinnedToPoint.PinnedToEntity;
				if (targetEntity.ServerPos.DistanceTo(entity.ServerPos) < (double)Config.MaxDistanceToTarget)
				{
					return false;
				}
				if (!IsTargetableEntity(targetEntity, Config.SeekingRange))
				{
					return false;
				}
				return true;
			}
		}
		return false;
	}

	public override void StartExecute()
	{
		if (targetEntity != null)
		{
			pathTraverser.WalkTowards(targetEntity.ServerPos.XYZ, Config.MoveSpeed, MinDistanceToTarget(Config.ExtraMinDistanceToTarget), OnGoalReached, OnStuck, Config.AiCreatureType);
			base.StartExecute();
		}
	}

	public override bool ContinueExecute(float dt)
	{
		if (targetEntity == null || clothSystem == null)
		{
			return false;
		}
		if (!ContinueExecute(dt))
		{
			return false;
		}
		float num = MinDistanceToTarget();
		double num2 = targetEntity.ServerPos.DistanceTo(entity.ServerPos.XYZ);
		if (num2 > (double)Config.MaxDistanceToTarget)
		{
			pathTraverser.WalkTowards(targetEntity.ServerPos.XYZ, Config.MoveSpeed, num, OnGoalReached, OnStuck, Config.AiCreatureType);
		}
		if (num2 < (double)num)
		{
			return false;
		}
		if (GetPinnedToPoint(entity) == null)
		{
			return false;
		}
		return true;
	}

	protected virtual void OnGoalReached()
	{
		lastGoalReachedMs = entity.World.ElapsedMilliseconds;
	}

	protected virtual void OnStuck()
	{
	}

	protected virtual ClothPoint? GetPinnedToPoint(EntityAgent entity)
	{
		if (clothSystem == null)
		{
			return null;
		}
		if (clothSystem.FirstPoint.PinnedToEntity != null && clothSystem.FirstPoint.PinnedToEntity.EntityId != entity.EntityId)
		{
			return clothSystem.FirstPoint;
		}
		if (clothSystem.LastPoint.PinnedToEntity != null && clothSystem.LastPoint.PinnedToEntity.EntityId != entity.EntityId)
		{
			return clothSystem.LastPoint;
		}
		return null;
	}
}
