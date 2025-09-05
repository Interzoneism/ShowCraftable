using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class AiTaskFlyCircleIfEntity : AiTaskFlyCircle
{
	protected float seekingRangeVer = 25f;

	protected float seekingRangeHor = 25f;

	public AiTaskFlyCircleIfEntity(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		seekingRangeHor = taskConfig["seekingRangeHor"].AsFloat(25f);
		seekingRangeVer = taskConfig["seekingRangeVer"].AsFloat(25f);
	}

	public override bool ShouldExecute()
	{
		CenterPos = SpawnPos;
		if (CenterPos == null)
		{
			return false;
		}
		long elapsedMilliseconds = entity.World.ElapsedMilliseconds;
		if (cooldownUntilMs > elapsedMilliseconds)
		{
			return false;
		}
		cooldownUntilMs = entity.World.ElapsedMilliseconds + 1000;
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		targetEntity = getEntity();
		if (targetEntity != null)
		{
			return base.ShouldExecute();
		}
		return false;
	}

	public override bool ContinueExecute(float dt)
	{
		if (base.ContinueExecute(dt))
		{
			return isNear();
		}
		return false;
	}

	private bool isNear()
	{
		if (targetEntity.ServerPos.SquareHorDistanceTo(CenterPos) <= (double)(seekingRangeHor * seekingRangeHor))
		{
			return targetEntity.ServerPos.Dimension == entity.ServerPos.Dimension;
		}
		return false;
	}

	public Entity getEntity()
	{
		if (CenterPos == null)
		{
			return null;
		}
		return entity.World.GetNearestEntity(CenterPos, seekingRangeHor, seekingRangeVer, (Entity e) => e is EntityPlayer entityPlayer && targetablePlayerMode(entityPlayer.Player));
	}
}
