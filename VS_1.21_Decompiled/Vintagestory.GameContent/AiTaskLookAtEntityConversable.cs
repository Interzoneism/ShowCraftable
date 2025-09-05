using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public sealed class AiTaskLookAtEntityConversable : AiTaskBaseR
{
	private float minTurnAnglePerSec;

	private float maxTurnAnglePerSec;

	private float curTurnRadPerSec;

	private readonly Entity target;

	public AiTaskLookAtEntityConversable(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		world.Logger.Error("This AI task 'AiTaskLookAtEntityConversable' can only be created from code.");
		throw new InvalidOperationException("This AI task can only be created from code.");
	}

	public AiTaskLookAtEntityConversable(EntityAgent entity, Entity target)
		: base(entity)
	{
		this.target = target;
	}

	public override bool ShouldExecute()
	{
		return false;
	}

	public override void StartExecute()
	{
		if (entity.Properties.Server?.Attributes != null)
		{
			minTurnAnglePerSec = entity.Properties.Server.Attributes.GetTreeAttribute("pathfinder").GetFloat("minTurnAnglePerSec", 250f);
			maxTurnAnglePerSec = entity.Properties.Server.Attributes.GetTreeAttribute("pathfinder").GetFloat("maxTurnAnglePerSec", 450f);
		}
		else
		{
			minTurnAnglePerSec = 250f;
			maxTurnAnglePerSec = 450f;
		}
		curTurnRadPerSec = minTurnAnglePerSec + (float)base.Rand.NextDouble() * (maxTurnAnglePerSec - minTurnAnglePerSec);
		curTurnRadPerSec *= (float)Math.PI / 180f;
	}

	public override bool ContinueExecute(float dt)
	{
		FastVec3f fastVec3f = default(FastVec3f);
		fastVec3f.Set((float)(target.ServerPos.X - entity.ServerPos.X), (float)(target.ServerPos.Y - entity.ServerPos.Y), (float)(target.ServerPos.Z - entity.ServerPos.Z));
		float end = (float)Math.Atan2(fastVec3f.X, fastVec3f.Z);
		float num = GameMath.AngleRadDistance(entity.ServerPos.Yaw, end);
		entity.ServerPos.Yaw += GameMath.Clamp(num, (0f - curTurnRadPerSec) * dt, curTurnRadPerSec * dt);
		entity.ServerPos.Yaw %= (float)Math.PI * 2f;
		return (double)Math.Abs(num) > 0.01;
	}
}
