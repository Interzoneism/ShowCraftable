using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class AiTaskFollowLeadHolder : AiTaskStayCloseToEntity
{
	private ClothManager cm;

	private int minGeneration;

	private long goalReachedEllapsedMs;

	private ClothSystem cs;

	public AiTaskFollowLeadHolder(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		minGeneration = taskConfig["minGeneration"].AsInt();
		cm = entity.World.Api.ModLoader.GetModSystem<ClothManager>();
	}

	public override bool ShouldExecute()
	{
		minSeekSeconds = 99f;
		if (entity.WatchedAttributes.GetInt("generation") < minGeneration)
		{
			return false;
		}
		if (entity.World.ElapsedMilliseconds - goalReachedEllapsedMs < 1000)
		{
			return false;
		}
		int[] array = entity.GetBehavior<EntityBehaviorRopeTieable>()?.ClothIds?.value;
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				cs = cm.GetClothSystem(array[i]);
				if (cs == null)
				{
					continue;
				}
				ClothPoint pinnedToPoint = getPinnedToPoint(cs, entity);
				if (pinnedToPoint != null)
				{
					targetEntity = pinnedToPoint.PinnedToEntity;
					if (targetEntity.ServerPos.DistanceTo(entity.ServerPos) < 2.0)
					{
						return false;
					}
					return true;
				}
			}
		}
		return false;
	}

	public override void StartExecute()
	{
		float xSize = targetEntity.SelectionBox.XSize;
		pathTraverser.WalkTowards(targetEntity.ServerPos.XYZ, moveSpeed, xSize + 1f, OnGoalReached, base.OnStuck);
		targetOffset.Set(0.0, 0.0, 0.0);
		stuck = false;
		base.StartExecute();
	}

	public override bool ContinueExecute(float dt)
	{
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		float xSize = targetEntity.SelectionBox.XSize;
		double num = targetEntity.ServerPos.DistanceTo(entity.ServerPos.XYZ);
		if (num > 2.0)
		{
			initialTargetPos = targetEntity.ServerPos.XYZ;
			pathTraverser.WalkTowards(targetEntity.ServerPos.XYZ, moveSpeed, xSize + 1f, OnGoalReached, base.OnStuck);
		}
		if (num < (double)(xSize + 1f))
		{
			return false;
		}
		if (getPinnedToPoint(cs, entity) == null)
		{
			return false;
		}
		return true;
	}

	protected override void OnGoalReached()
	{
		goalReachedEllapsedMs = entity.World.ElapsedMilliseconds;
		base.OnGoalReached();
	}

	private ClothPoint getPinnedToPoint(ClothSystem cs, EntityAgent entity)
	{
		if (cs.FirstPoint.PinnedToEntity != null && cs.FirstPoint.PinnedToEntity.EntityId != entity.EntityId)
		{
			return cs.FirstPoint;
		}
		if (cs.LastPoint.PinnedToEntity != null && cs.LastPoint.PinnedToEntity.EntityId != entity.EntityId)
		{
			return cs.LastPoint;
		}
		return null;
	}
}
