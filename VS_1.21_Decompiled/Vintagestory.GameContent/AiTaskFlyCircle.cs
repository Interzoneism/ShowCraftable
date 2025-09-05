using System;
using System.Numerics;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskFlyCircle : AiTaskTargetableAt
{
	protected bool stayNearSpawn;

	protected float minRadius;

	protected float maxRadius;

	protected float height;

	protected double desiredYPos;

	protected float moveSpeed = 0.04f;

	protected float direction = 1f;

	protected float directionChangeCoolDown = 60f;

	protected double desiredRadius;

	public AiTaskFlyCircle(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		stayNearSpawn = taskConfig["stayNearSpawn"].AsBool();
		minRadius = taskConfig["minRadius"].AsFloat(10f);
		maxRadius = taskConfig["maxRadius"].AsFloat(20f);
		height = taskConfig["height"].AsFloat(5f);
		moveSpeed = taskConfig["moveSpeed"].AsFloat(0.04f);
		direction = ((taskConfig["direction"].AsString("left") == "left") ? 1 : (-1));
	}

	public override bool ShouldExecute()
	{
		return true;
	}

	public override void StartExecute()
	{
		desiredRadius = minRadius + (float)world.Rand.NextDouble() * (maxRadius - minRadius);
		if (stayNearSpawn)
		{
			CenterPos = SpawnPos;
		}
		else
		{
			float num = (float)world.Rand.NextDouble() * ((float)Math.PI * 2f);
			double x = desiredRadius * Math.Sin(num);
			double z = desiredRadius * Math.Cos(num);
			CenterPos = entity.ServerPos.XYZ.Add(x, 0.0, z);
		}
		base.StartExecute();
	}

	public override bool ContinueExecute(float dt)
	{
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		if ((int)CenterPos.Y / 32768 != entity.Pos.Dimension)
		{
			return false;
		}
		if (entity.OnGround || entity.World.Rand.NextDouble() < 0.03)
		{
			UpdateFlyHeight();
		}
		double num = GameMath.Clamp(desiredYPos - entity.ServerPos.Y, -0.33, 0.33);
		double num2 = entity.ServerPos.X - CenterPos.X;
		double num3 = entity.ServerPos.Z - CenterPos.Z;
		double num4 = Math.Sqrt(num2 * num2 + num3 * num3);
		double num5 = desiredRadius - num4;
		if (Math.Abs(num2) + Math.Abs(num3) <= double.Epsilon)
		{
			return entity.Alive;
		}
		Vector3 vector = Vector3.Normalize(new Vector3((float)num2, 0f, (float)num3)) * (float)desiredRadius;
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(vector, new Vector3(0f, 0f - direction, 0f)) + vector * (float)num5 * dt * 100f);
		float end = (float)Math.Atan2(0f - vector2.Z, vector2.X) + (float)Math.PI / 2f + 0.1f * direction;
		entity.ServerPos.Yaw += GameMath.AngleRadDistance(entity.ServerPos.Yaw, end) * dt;
		entity.Controls.WalkVector.Set(vector2.X, num, vector2.Z);
		entity.Controls.WalkVector.Mul(moveSpeed);
		if (num < 0.0)
		{
			entity.Controls.WalkVector.Mul(0.5);
		}
		if (entity.Swimming)
		{
			entity.Controls.WalkVector.Y = 2f * moveSpeed;
			entity.Controls.FlyVector.Y = 2f * moveSpeed;
		}
		double num6 = entity.ServerPos.Motion.Length();
		if (num6 > 0.01)
		{
			entity.ServerPos.Roll = (float)Math.Asin(GameMath.Clamp((0.0 - entity.ServerPos.Motion.Y) / num6, -1.0, 1.0));
		}
		directionChangeCoolDown = Math.Max(0f, directionChangeCoolDown - dt);
		if (entity.CollidedHorizontally && directionChangeCoolDown <= 0f)
		{
			directionChangeCoolDown = 2f;
			direction *= -1f;
		}
		return entity.Alive;
	}

	protected void UpdateFlyHeight()
	{
		int num = entity.World.BlockAccessor.GetTerrainMapheightAt(entity.SidedPos.AsBlockPos);
		int num2 = 10;
		int num3 = 32768 * entity.SidedPos.Dimension;
		while (num2-- > 0 && entity.World.BlockAccessor.GetBlockRaw((int)entity.ServerPos.X, num + num3, (int)entity.ServerPos.Z, 2).IsLiquid())
		{
			num++;
		}
		desiredYPos = (float)num + height;
	}
}
