using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskButterflyWander : AiTaskBase
{
	protected float moveSpeed = 0.03f;

	protected float maxHeight = 7f;

	protected float? preferredLightLevel;

	protected float wanderDuration;

	protected float desiredYaw;

	protected float desiredflyHeightAboveGround;

	protected double desiredYPos;

	protected float minTurnAnglePerSec;

	protected float maxTurnAnglePerSec;

	protected float curTurnRadPerSec;

	public AiTaskButterflyWander(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		moveSpeed = taskConfig["movespeed"].AsFloat(0.03f);
		maxHeight = taskConfig["maxHeight"].AsFloat(7f);
		preferredLightLevel = taskConfig["preferredLightLevel"].AsFloat(-99f);
		if (preferredLightLevel < 0f)
		{
			preferredLightLevel = null;
		}
		if (entity?.Properties?.Server?.Attributes != null)
		{
			minTurnAnglePerSec = (entity.Properties.Server?.Attributes.GetTreeAttribute("pathfinder").GetFloat("minTurnAnglePerSec", 250f)).Value;
			maxTurnAnglePerSec = (entity.Properties.Server?.Attributes.GetTreeAttribute("pathfinder").GetFloat("maxTurnAnglePerSec", 450f)).Value;
		}
		else
		{
			minTurnAnglePerSec = 250f;
			maxTurnAnglePerSec = 450f;
		}
	}

	public override bool ShouldExecute()
	{
		return true;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		wanderDuration = 0.5f + (float)entity.World.Rand.NextDouble() * (float)entity.World.Rand.NextDouble() * 1f;
		desiredYaw = (float)((double)entity.ServerPos.Yaw + 12.566370964050293 * (entity.World.Rand.NextDouble() - 0.5));
		desiredflyHeightAboveGround = 1f + 4f * (float)entity.World.Rand.NextDouble() + 4f * (float)(entity.World.Rand.NextDouble() * entity.World.Rand.NextDouble());
		ReadjustFlyHeight();
		entity.Controls.Forward = true;
		curTurnRadPerSec = minTurnAnglePerSec + (float)entity.World.Rand.NextDouble() * (maxTurnAnglePerSec - minTurnAnglePerSec);
		curTurnRadPerSec *= 0.87266463f * moveSpeed;
	}

	public override bool ContinueExecute(float dt)
	{
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		if (entity.OnGround || entity.World.Rand.NextDouble() < 0.03)
		{
			ReadjustFlyHeight();
		}
		wanderDuration -= dt;
		double num = GameMath.Clamp(desiredYPos - entity.ServerPos.Y, -1.0, 1.0);
		float val = GameMath.AngleRadDistance(desiredYaw, entity.ServerPos.Yaw);
		if (!entity.FeetInLiquid)
		{
			entity.ServerPos.Yaw += GameMath.Clamp(val, (0f - curTurnRadPerSec) * dt * ((num < 0.0) ? 0.25f : 1f), curTurnRadPerSec * dt * ((num < 0.0) ? 0.25f : 1f));
			entity.ServerPos.Yaw = entity.ServerPos.Yaw % ((float)Math.PI * 2f);
		}
		else if (entity.World.Rand.NextDouble() < 0.001)
		{
			entity.ServerPos.Motion.Y = 0.019999999552965164;
		}
		double z = Math.Cos(entity.ServerPos.Yaw);
		double x = Math.Sin(entity.ServerPos.Yaw);
		entity.Controls.WalkVector.Set(x, num, z);
		entity.Controls.WalkVector.Mul(moveSpeed);
		if (num < 0.0)
		{
			entity.Controls.WalkVector.Mul(0.75);
		}
		if (entity.Swimming)
		{
			entity.Controls.WalkVector.Y = 2f * moveSpeed;
			entity.Controls.FlyVector.Y = 2f * moveSpeed;
		}
		if (entity.CollidedHorizontally)
		{
			wanderDuration -= 10f * dt;
		}
		return wanderDuration > 0f;
	}

	protected void ReadjustFlyHeight()
	{
		int num = entity.World.BlockAccessor.GetTerrainMapheightAt(entity.SidedPos.AsBlockPos);
		int num2 = 10;
		while (num2-- > 0 && entity.World.BlockAccessor.GetBlockRaw((int)entity.ServerPos.X, num, (int)entity.ServerPos.Z, 2).IsLiquid())
		{
			num++;
		}
		desiredYPos = (float)num + desiredflyHeightAboveGround;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
	}
}
