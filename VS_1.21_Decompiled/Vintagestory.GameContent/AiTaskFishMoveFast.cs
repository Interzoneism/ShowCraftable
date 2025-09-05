using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskFishMoveFast : AiTaskBase
{
	public Vec3d MainTarget;

	private bool done;

	private float moveSpeed = 0.06f;

	private float wanderChance = 0.04f;

	private float? preferredLightLevel;

	private float targetDistance = 0.12f;

	private NatFloat wanderRangeHorizontal = NatFloat.createStrongerInvexp(3f, 40f);

	private NatFloat wanderRangeVertical = NatFloat.createStrongerInvexp(3f, 10f);

	public float WanderRangeMul
	{
		get
		{
			return entity.Attributes.GetFloat("wanderRangeMul", 1f);
		}
		set
		{
			entity.Attributes.SetFloat("wanderRangeMul", value);
		}
	}

	public int FailedConsecutivePathfinds
	{
		get
		{
			return entity.Attributes.GetInt("failedConsecutivePathfinds");
		}
		set
		{
			entity.Attributes.SetInt("failedConsecutivePathfinds", value);
		}
	}

	public AiTaskFishMoveFast(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		float num = 3f;
		float num2 = 30f;
		targetDistance = taskConfig["targetDistance"].AsFloat(0.12f);
		moveSpeed = taskConfig["movespeed"].AsFloat(0.03f);
		wanderChance = taskConfig["wanderChance"].AsFloat(0.015f);
		num = taskConfig["wanderRangeMin"].AsFloat(3f);
		num2 = taskConfig["wanderRangeMax"].AsFloat(30f);
		wanderRangeHorizontal = NatFloat.createStrongerInvexp(num, num2);
		preferredLightLevel = taskConfig["preferredLightLevel"].AsFloat(-99f);
		if (preferredLightLevel < 0f)
		{
			preferredLightLevel = null;
		}
	}

	public Vec3d loadNextWanderTarget()
	{
		int num = 9;
		Vec4d vec4d = null;
		Vec4d vec4d2 = new Vec4d();
		BlockPos blockPos = new BlockPos(entity.ServerPos.Dimension);
		if (FailedConsecutivePathfinds > 10)
		{
			WanderRangeMul = Math.Max(0.1f, WanderRangeMul * 0.9f);
		}
		else
		{
			WanderRangeMul = Math.Min(1f, WanderRangeMul * 1.1f);
			if (base.rand.NextDouble() < 0.05)
			{
				WanderRangeMul = Math.Min(1f, WanderRangeMul * 1.5f);
			}
		}
		float num2 = WanderRangeMul;
		if (base.rand.NextDouble() < 0.05)
		{
			num2 *= 3f;
		}
		while (num-- > 0)
		{
			double num3 = wanderRangeHorizontal.nextFloat() * (float)(base.rand.Next(2) * 2 - 1) * num2;
			double num4 = wanderRangeVertical.nextFloat() * (float)(base.rand.Next(2) * 2 - 1) * num2;
			double num5 = wanderRangeHorizontal.nextFloat() * (float)(base.rand.Next(2) * 2 - 1) * num2;
			vec4d2.X = entity.ServerPos.X + num3;
			vec4d2.Y = entity.ServerPos.InternalY + num4;
			vec4d2.Z = entity.ServerPos.Z + num5;
			vec4d2.W = 1.0;
			if (!entity.World.BlockAccessor.GetBlockRaw((int)vec4d2.X, (int)vec4d2.Y, (int)vec4d2.Z, 2).IsLiquid())
			{
				vec4d2.W = 0.0;
			}
			else
			{
				vec4d2.W = 1.0 / (Math.Abs(num4) + 1.0);
			}
			if (preferredLightLevel.HasValue && vec4d2.W != 0.0)
			{
				blockPos.Set((int)vec4d2.X, (int)vec4d2.Y, (int)vec4d2.Z);
				int val = Math.Abs((int)preferredLightLevel.Value - entity.World.BlockAccessor.GetLightLevel(blockPos, EnumLightLevelType.MaxLight));
				vec4d2.W /= Math.Max(1, val);
			}
			if (vec4d == null || vec4d2.W > vec4d.W)
			{
				vec4d = new Vec4d(vec4d2.X, vec4d2.Y, vec4d2.Z, vec4d2.W);
				if (vec4d2.W >= 1.0)
				{
					break;
				}
			}
		}
		if (vec4d.W > 0.0)
		{
			FailedConsecutivePathfinds = Math.Max(FailedConsecutivePathfinds - 3, 0);
			return vec4d.XYZ;
		}
		FailedConsecutivePathfinds++;
		return null;
	}

	public override bool ShouldExecute()
	{
		if (!entity.Swimming)
		{
			return false;
		}
		if (base.rand.NextDouble() > (double)wanderChance && !entity.CollidedHorizontally && !entity.CollidedVertically)
		{
			return false;
		}
		MainTarget = loadNextWanderTarget();
		return MainTarget != null;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		done = false;
		pathTraverser.WalkTowards(MainTarget, moveSpeed, targetDistance, OnGoalReached, OnStuck);
	}

	public override bool ContinueExecute(float dt)
	{
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		base.ContinueExecute(dt);
		if ((double)MainTarget.HorizontalSquareDistanceTo(entity.ServerPos.X, entity.ServerPos.Z) < 0.5)
		{
			pathTraverser.Stop();
			return false;
		}
		return !done;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		if (cancelled)
		{
			pathTraverser.Stop();
		}
	}

	private void OnStuck()
	{
		done = true;
	}

	private void OnGoalReached()
	{
		done = true;
	}
}
