using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskWander : AiTaskBase
{
	public Vec3d MainTarget;

	private bool done;

	private float moveSpeed = 0.03f;

	private float wanderChance = 0.02f;

	private float maxHeight = 7f;

	private float? preferredLightLevel;

	private float targetDistance = 0.12f;

	private NatFloat wanderRangeHorizontal = NatFloat.createStrongerInvexp(3f, 40f);

	private NatFloat wanderRangeVertical = NatFloat.createStrongerInvexp(3f, 10f);

	public bool StayCloseToSpawn;

	public Vec3d SpawnPosition;

	public double MaxDistanceToSpawn;

	private long lastTimeInRangeMs;

	private int failedWanders;

	private bool needsToTele;

	private float tryStartAnimAgain = 0.1f;

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

	public AiTaskWander(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		float num = 3f;
		float num2 = 30f;
		if (taskConfig["maxDistanceToSpawn"].Exists)
		{
			StayCloseToSpawn = true;
			MaxDistanceToSpawn = taskConfig["maxDistanceToSpawn"].AsDouble(10.0);
			SpawnPosition = new Vec3d(entity.Attributes.GetDouble("spawnX"), entity.Attributes.GetDouble("spawnY"), entity.Attributes.GetDouble("spawnZ"));
			BlockPos blockPos = entity.WatchedAttributes.GetBlockPos("importOffset");
			if (blockPos != null)
			{
				SpawnPosition.Add(blockPos);
			}
		}
		targetDistance = taskConfig["targetDistance"].AsFloat(0.12f);
		moveSpeed = taskConfig["movespeed"].AsFloat(0.03f);
		wanderChance = taskConfig["wanderChance"].AsFloat(0.02f);
		num = taskConfig["wanderRangeMin"].AsFloat(3f);
		num2 = taskConfig["wanderRangeMax"].AsFloat(30f);
		wanderRangeHorizontal = NatFloat.createInvexp(num, num2);
		maxHeight = taskConfig["maxHeight"].AsFloat(7f);
		preferredLightLevel = taskConfig["preferredLightLevel"].AsFloat(-99f);
		if (preferredLightLevel < 0f)
		{
			preferredLightLevel = null;
		}
	}

	public override void OnEntityLoaded()
	{
		if (StayCloseToSpawn && (SpawnPosition == null || (SpawnPosition.X == 0.0 && SpawnPosition.Z == 0.0) || !entity.Attributes.HasAttribute("spawnX")))
		{
			OnEntitySpawn();
		}
	}

	public override void OnEntitySpawn()
	{
		entity.Attributes.SetDouble("spawnX", entity.ServerPos.X);
		entity.Attributes.SetDouble("spawnY", entity.ServerPos.Y);
		entity.Attributes.SetDouble("spawnZ", entity.ServerPos.Z);
		SpawnPosition = entity.ServerPos.XYZ;
	}

	public Vec3d loadNextWanderTarget()
	{
		EnumHabitat habitat = entity.Properties.Habitat;
		int num = 9;
		Vec4d vec4d = null;
		Vec4d vec4d2 = new Vec4d();
		BlockPos blockPos = new BlockPos(entity.Pos.Dimension);
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
			if (StayCloseToSpawn)
			{
				double num6 = (double)vec4d2.SquareDistanceTo(SpawnPosition) / (MaxDistanceToSpawn * MaxDistanceToSpawn);
				vec4d2.W = 1.0 - num6;
			}
			switch (habitat)
			{
			case EnumHabitat.Air:
			{
				int rainMapHeightAt = world.BlockAccessor.GetRainMapHeightAt((int)vec4d2.X, (int)vec4d2.Z);
				vec4d2.Y = Math.Min(vec4d2.Y, (float)rainMapHeightAt + maxHeight);
				if (entity.World.BlockAccessor.GetBlockRaw((int)vec4d2.X, (int)vec4d2.Y, (int)vec4d2.Z, 2).IsLiquid())
				{
					vec4d2.W = 0.0;
				}
				break;
			}
			case EnumHabitat.Land:
			{
				vec4d2.Y = moveDownToFloor((int)vec4d2.X, vec4d2.Y, (int)vec4d2.Z);
				if (vec4d2.Y < 0.0)
				{
					vec4d2.W = 0.0;
					break;
				}
				if (entity.World.BlockAccessor.GetBlockRaw((int)vec4d2.X, (int)vec4d2.Y, (int)vec4d2.Z, 2).IsLiquid())
				{
					vec4d2.W /= 2.0;
				}
				bool stop = false;
				bool willFall = false;
				float yaw = (float)Math.Atan2(num3, num5) + (float)Math.PI / 2f;
				Vec3d vec3d = vec4d2.XYZ.Ahead(1.0, 0f, yaw);
				Vec3d vec3d2 = entity.ServerPos.XYZ.Ahead(1.0, 0f, yaw);
				int prevY = (int)vec3d2.Y;
				GameMath.BresenHamPlotLine2d((int)vec3d2.X, (int)vec3d2.Z, (int)vec3d.X, (int)vec3d.Z, delegate(int x, int z)
				{
					if (!stop)
					{
						double num8 = moveDownToFloor(x, prevY, z);
						if (num8 < 0.0 || (double)prevY - num8 > 4.0)
						{
							willFall = true;
							stop = true;
						}
						if (num8 - (double)prevY > 2.0)
						{
							stop = true;
						}
						prevY = (int)num8;
					}
				});
				if (willFall)
				{
					vec4d2.W = 0.0;
				}
				break;
			}
			case EnumHabitat.Sea:
				if (!entity.World.BlockAccessor.GetBlockRaw((int)vec4d2.X, (int)vec4d2.Y, (int)vec4d2.Z, 2).IsLiquid())
				{
					vec4d2.W = 0.0;
				}
				break;
			case EnumHabitat.Underwater:
				if (!entity.World.BlockAccessor.GetBlockRaw((int)vec4d2.X, (int)vec4d2.Y, (int)vec4d2.Z, 2).IsLiquid())
				{
					vec4d2.W = 0.0;
				}
				else
				{
					vec4d2.W = 1.0 / (Math.Abs(num4) + 1.0);
				}
				break;
			}
			if (vec4d2.W > 0.0)
			{
				for (int num7 = 0; num7 < BlockFacing.HORIZONTALS.Length; num7++)
				{
					BlockFacing blockFacing = BlockFacing.HORIZONTALS[num7];
					if (entity.World.BlockAccessor.IsSideSolid((int)vec4d2.X + blockFacing.Normali.X, (int)vec4d2.Y, (int)vec4d2.Z + blockFacing.Normali.Z, blockFacing.Opposite))
					{
						vec4d2.W *= 0.5;
					}
				}
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

	private double moveDownToFloor(int x, double y, int z)
	{
		int num = 5;
		while (num-- > 0)
		{
			if (world.BlockAccessor.IsSideSolid(x, (int)y, z, BlockFacing.UP))
			{
				return y + 1.0;
			}
			y -= 1.0;
		}
		return -1.0;
	}

	public override bool ShouldExecute()
	{
		if (base.rand.NextDouble() > (double)((failedWanders > 0) ? (1f - wanderChance * 4f * (float)failedWanders) : wanderChance))
		{
			failedWanders = 0;
			return false;
		}
		needsToTele = false;
		if (StayCloseToSpawn)
		{
			double num = entity.ServerPos.XYZ.SquareDistanceTo(SpawnPosition);
			long elapsedMilliseconds = entity.World.ElapsedMilliseconds;
			if (num > MaxDistanceToSpawn * MaxDistanceToSpawn)
			{
				if (elapsedMilliseconds - lastTimeInRangeMs > 120000 && entity.World.GetNearestEntity(entity.ServerPos.XYZ, 15f, 15f, (Entity e) => e is EntityPlayer) == null)
				{
					needsToTele = true;
				}
				MainTarget = SpawnPosition.Clone();
				return true;
			}
			lastTimeInRangeMs = elapsedMilliseconds;
		}
		MainTarget = loadNextWanderTarget();
		return MainTarget != null;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		if (needsToTele && StayCloseToSpawn)
		{
			entity.TeleportTo(SpawnPosition);
			done = true;
		}
		else
		{
			done = false;
			pathTraverser.WalkTowards(MainTarget, moveSpeed, targetDistance, OnGoalReached, OnStuck);
			tryStartAnimAgain = 0.1f;
		}
	}

	public override bool ContinueExecute(float dt)
	{
		base.ContinueExecute(dt);
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		if (animMeta != null && tryStartAnimAgain > 0f && (tryStartAnimAgain -= dt) <= 0f)
		{
			entity.AnimManager.StartAnimation(animMeta);
		}
		if (entity.Controls.IsClimbing && entity.Properties.CanClimbAnywhere && entity.ClimbingIntoFace != null)
		{
			BlockFacing climbingIntoFace = entity.ClimbingIntoFace;
			if (Math.Sign(climbingIntoFace.Normali.X) == Math.Sign(pathTraverser.CurrentTarget.X - entity.ServerPos.X))
			{
				pathTraverser.CurrentTarget.X = entity.ServerPos.X;
			}
			if (Math.Sign(climbingIntoFace.Normali.Y) == Math.Sign(pathTraverser.CurrentTarget.Y - entity.ServerPos.Y))
			{
				pathTraverser.CurrentTarget.Y = entity.ServerPos.Y;
			}
			if (Math.Sign(climbingIntoFace.Normali.Z) == Math.Sign(pathTraverser.CurrentTarget.Z - entity.ServerPos.Z))
			{
				pathTraverser.CurrentTarget.Z = entity.ServerPos.Z;
			}
		}
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
		failedWanders++;
	}

	private void OnGoalReached()
	{
		done = true;
		failedWanders = 0;
	}
}
