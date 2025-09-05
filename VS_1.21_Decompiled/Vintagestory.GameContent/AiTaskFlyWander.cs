using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskFlyWander : AiTaskBase
{
	private bool stayNearSpawn;

	private float radius;

	private float height;

	private float minDistance;

	protected double desiredYPos;

	protected float moveSpeed = 0.04f;

	public Vec3d SpawnPos;

	public Vec3d targetPos;

	private float targetTolerangeRange;

	public AiTaskFlyWander(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		stayNearSpawn = taskConfig["stayNearSpawn"].AsBool();
		radius = taskConfig["radius"].AsFloat(10f);
		height = taskConfig["height"].AsFloat(5f);
		minDistance = taskConfig["minDistance"].AsFloat(10f);
		moveSpeed = taskConfig["moveSpeed"].AsFloat(0.04f);
		targetTolerangeRange = taskConfig["targetTolerangeRange"].AsFloat(15f);
	}

	public override void OnEntityLoaded()
	{
		loadOrCreateSpawnPos();
	}

	public override void OnEntitySpawn()
	{
		loadOrCreateSpawnPos();
	}

	public void loadOrCreateSpawnPos()
	{
		if (entity.WatchedAttributes.HasAttribute("spawnPosX"))
		{
			SpawnPos = new Vec3d(entity.WatchedAttributes.GetDouble("spawnPosX"), entity.WatchedAttributes.GetDouble("spawnPosY"), entity.WatchedAttributes.GetDouble("spawnPosZ"));
			return;
		}
		SpawnPos = entity.ServerPos.XYZ;
		entity.WatchedAttributes.SetDouble("spawnPosX", SpawnPos.X);
		entity.WatchedAttributes.SetDouble("spawnPosY", SpawnPos.Y);
		entity.WatchedAttributes.SetDouble("spawnPosZ", SpawnPos.Z);
	}

	public override bool ShouldExecute()
	{
		if (cooldownUntilMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		return true;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		Vec3d vec3d = (stayNearSpawn ? SpawnPos : entity.ServerPos.XYZ);
		double x = 0.0;
		double z = 0.0;
		for (int i = 0; i < 10; i++)
		{
			float num = (float)world.Rand.NextDouble() * ((float)Math.PI * 2f);
			x = (double)radius * Math.Sin(num);
			z = (double)radius * Math.Cos(num);
			if (vec3d.AddCopy(x, 0.0, z).HorizontalSquareDistanceTo(entity.ServerPos.XYZ) > minDistance)
			{
				break;
			}
		}
		targetPos = vec3d.AddCopy(x, 0.0, z);
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
		double num = GameMath.Clamp(desiredYPos - entity.ServerPos.Y, -0.2, 0.2);
		double num2 = targetPos.X - entity.ServerPos.X;
		double num3 = targetPos.Z - entity.ServerPos.Z;
		float end = (float)Math.Atan2(num2, num3);
		entity.ServerPos.Yaw += GameMath.AngleRadDistance(entity.ServerPos.Yaw, end) * dt;
		double z = Math.Cos(entity.ServerPos.Yaw);
		double x = Math.Sin(entity.ServerPos.Yaw);
		entity.Controls.WalkVector.Set(x, num, z);
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
		if (entity.Alive)
		{
			return num2 * num2 + num3 * num3 > (double)(targetTolerangeRange * targetTolerangeRange);
		}
		return false;
	}

	protected void ReadjustFlyHeight()
	{
		int num = entity.World.BlockAccessor.GetTerrainMapheightAt(entity.SidedPos.AsBlockPos);
		int num2 = 10;
		while (num2-- > 0 && entity.World.BlockAccessor.GetBlockRaw((int)entity.ServerPos.X, num, (int)entity.ServerPos.Z, 2).IsLiquid())
		{
			num++;
		}
		desiredYPos = (float)num + height;
	}
}
