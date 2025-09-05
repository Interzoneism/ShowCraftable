using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskLookAtEntityR : AiTaskBaseTargetableR
{
	protected float minTurnAnglePerSec;

	protected float maxTurnAnglePerSec;

	protected float currentTurnRadPerSec;

	protected const float yawChangeRateToStop = 0.01f;

	private AiTaskLookAtEntityConfig Config => GetConfig<AiTaskLookAtEntityConfig>();

	public AiTaskLookAtEntityR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskLookAtEntityConfig>(entity, taskConfig, aiConfig);
	}

	public override bool ShouldExecute()
	{
		if (!PreconditionsSatisficed())
		{
			return false;
		}
		return SearchForTarget();
	}

	public override void StartExecute()
	{
		base.StartExecute();
		ITreeAttribute treeAttribute = entity?.Properties.Server?.Attributes?.GetTreeAttribute("pathfinder");
		if (treeAttribute != null)
		{
			minTurnAnglePerSec = treeAttribute.GetFloat("minTurnAnglePerSec", Config.DefaultMinTurnAngleDegPerSec);
			maxTurnAnglePerSec = treeAttribute.GetFloat("maxTurnAnglePerSec", Config.DefaultMaxTurnAngleDegPerSec);
		}
		else
		{
			minTurnAnglePerSec = Config.DefaultMinTurnAngleDegPerSec;
			maxTurnAnglePerSec = Config.DefaultMaxTurnAngleDegPerSec;
		}
		currentTurnRadPerSec = minTurnAnglePerSec + (float)base.Rand.NextDouble() * (maxTurnAnglePerSec - minTurnAnglePerSec);
		currentTurnRadPerSec *= (float)Math.PI / 180f;
	}

	public override bool ContinueExecute(float dt)
	{
		if (!base.ContinueExecute(dt))
		{
			return false;
		}
		if (targetEntity == null)
		{
			return false;
		}
		FastVec3f fastVec3f = default(FastVec3f);
		fastVec3f.Set((float)(targetEntity.ServerPos.X - entity.ServerPos.X), (float)(targetEntity.ServerPos.Y - entity.ServerPos.Y), (float)(targetEntity.ServerPos.Z - entity.ServerPos.Z));
		float num = (float)Math.Atan2(fastVec3f.X, fastVec3f.Z);
		if (Config.MaxTurnAngleRad < (float)Math.PI)
		{
			num = GameMath.Clamp(num, Config.SpawnAngleRad - Config.MaxTurnAngleRad, Config.SpawnAngleRad + Config.MaxTurnAngleRad);
		}
		float num2 = GameMath.AngleRadDistance(entity.ServerPos.Yaw, num);
		entity.ServerPos.Yaw += GameMath.Clamp(num2, (0f - currentTurnRadPerSec) * dt * GlobalConstants.OverallSpeedMultiplier, currentTurnRadPerSec * dt * GlobalConstants.OverallSpeedMultiplier);
		entity.ServerPos.Yaw %= (float)Math.PI * 2f;
		return Math.Abs(num2) > 0.01f;
	}
}
