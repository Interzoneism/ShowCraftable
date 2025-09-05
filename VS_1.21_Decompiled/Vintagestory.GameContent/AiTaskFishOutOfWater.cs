using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskFishOutOfWater : AiTaskBase
{
	internal Vec3d targetPos = new Vec3d();

	protected float seekingRange = 2f;

	public JsonObject taskConfig;

	private float moveSpeed = 0.03f;

	private float searchWaterAccum;

	private float outofWaterAccum;

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

	public AiTaskFishOutOfWater(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		this.taskConfig = taskConfig;
		moveSpeed = taskConfig["movespeed"].AsFloat(0.03f);
	}

	public override bool ShouldExecute()
	{
		if (!entity.OnGround || entity.Swimming)
		{
			return false;
		}
		return true;
	}

	private Vec3d nearbyWaterOrRandomTarget()
	{
		int num = 9;
		Vec4d vec4d = null;
		Vec4d vec4d2 = new Vec4d();
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
				vec4d2.W = 1.0 / Math.Sqrt((num3 - 1.0) * (num3 - 1.0) + (num5 - 1.0) * (num5 - 1.0) + 1.0);
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

	public override void StartExecute()
	{
		base.StartExecute();
		searchWaterAccum = 0f;
		outofWaterAccum = 0f;
		targetPos = nearbyWaterOrRandomTarget();
		if (targetPos != null)
		{
			pathTraverser.WalkTowards(targetPos, moveSpeed, 0.12f, OnGoalReached, OnStuck);
		}
	}

	private void OnStuck()
	{
	}

	private void OnGoalReached()
	{
	}

	public override bool ContinueExecute(float dt)
	{
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		if (entity.Swimming)
		{
			return false;
		}
		outofWaterAccum += dt;
		if (outofWaterAccum > 30f)
		{
			entity.Die(EnumDespawnReason.Death, new DamageSource
			{
				Type = EnumDamageType.Suffocation
			});
			return false;
		}
		if (targetPos == null)
		{
			searchWaterAccum += dt;
			if (searchWaterAccum >= 2f)
			{
				targetPos = nearbyWaterOrRandomTarget();
				if (targetPos != null)
				{
					pathTraverser.WalkTowards(targetPos, moveSpeed, 0.12f, OnGoalReached, OnStuck);
				}
				searchWaterAccum = 0f;
			}
		}
		if (targetPos != null && world.Rand.NextDouble() < 0.2)
		{
			pathTraverser.CurrentTarget.X = targetPos.X;
			pathTraverser.CurrentTarget.Y = targetPos.Y;
			pathTraverser.CurrentTarget.Z = targetPos.Z;
		}
		return true;
	}

	public override void FinishExecute(bool cancelled)
	{
		pathTraverser.Stop();
		base.FinishExecute(cancelled);
	}
}
