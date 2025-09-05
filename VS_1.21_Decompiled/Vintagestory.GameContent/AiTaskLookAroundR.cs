using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskLookAroundR : AiTaskBaseR
{
	private AiTaskLookAroundConfig Config => GetConfig<AiTaskLookAroundConfig>();

	public AiTaskLookAroundR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskLookAroundConfig>(entity, taskConfig, aiConfig);
	}

	public override bool ShouldExecute()
	{
		return PreconditionsSatisficed();
	}

	public override void StartExecute()
	{
		base.StartExecute();
		entity.ServerPos.Yaw = (float)GameMath.Clamp(entity.World.Rand.NextDouble() * 6.2831854820251465, entity.ServerPos.Yaw - (float)Math.PI / 4f * GlobalConstants.OverallSpeedMultiplier * Config.TurnAngleFactor, entity.ServerPos.Yaw + (float)Math.PI / 4f * GlobalConstants.OverallSpeedMultiplier * Config.TurnAngleFactor);
	}
}
