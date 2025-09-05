using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskWanderR : AiTaskBaseR
{
	protected int failedWanders;

	protected long lastTimeInRangeMs;

	protected bool spawnPositionSet;

	protected Vec3d mainTarget = new Vec3d();

	protected Vec3d spawnPosition = new Vec3d();

	protected bool needsToTeleport;

	private readonly Vec4d bestTarget = new Vec4d();

	private readonly Vec4d currentTarget = new Vec4d();

	private readonly BlockPos blockPosBuffer = new BlockPos(0);

	private AiTaskWanderConfig Config => GetConfig<AiTaskWanderConfig>();

	protected float WanderRangeMul
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

	protected int FailedConsecutivePathfinds
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

	public AiTaskWanderR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskWanderConfig>(entity, taskConfig, aiConfig);
		spawnPosition = new Vec3d(entity.Attributes.GetDouble("spawnX"), entity.Attributes.GetDouble("spawnY"), entity.Attributes.GetDouble("spawnZ"));
	}

	public override bool ShouldExecute()
	{
		if (!PreconditionsSatisficed())
		{
			return false;
		}
		failedWanders = 0;
		needsToTeleport = false;
		if (!Config.StayCloseToSpawn)
		{
			return LoadNextWanderTarget();
		}
		long elapsedMilliseconds = entity.World.ElapsedMilliseconds;
		if ((double)entity.ServerPos.XYZ.SquareDistanceTo(spawnPosition.X, spawnPosition.Y, spawnPosition.Z) <= Config.MaxDistanceToSpawn * Config.MaxDistanceToSpawn)
		{
			lastTimeInRangeMs = elapsedMilliseconds;
			return false;
		}
		if (elapsedMilliseconds - lastTimeInRangeMs > Config.TeleportToSpawnTimeout && entity.World.GetNearestEntity(entity.ServerPos.XYZ, Config.NoPlayersRange, Config.NoPlayersRange, (Entity target) => target is EntityPlayer) == null)
		{
			needsToTeleport = true;
		}
		mainTarget = spawnPosition;
		return true;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		if (needsToTeleport)
		{
			entity.TeleportTo((int)spawnPosition.X, (int)spawnPosition.Y, (int)spawnPosition.Z);
			stopTask = true;
		}
		else
		{
			pathTraverser.WalkTowards(mainTarget, Config.MoveSpeed, Config.MinDistanceToTarget, OnGoalReached, OnStuck);
		}
	}

	public override bool ContinueExecute(float dt)
	{
		if (!base.ContinueExecute(dt))
		{
			return false;
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
		if ((double)mainTarget.HorizontalSquareDistanceTo(entity.ServerPos.X, entity.ServerPos.Z) < 0.5)
		{
			pathTraverser.Stop();
			return false;
		}
		return true;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		if (cancelled)
		{
			pathTraverser.Stop();
		}
	}

	public override void OnEntityLoaded()
	{
		if (!entity.Attributes.HasAttribute("spawnX"))
		{
			OnEntitySpawn();
		}
	}

	public override void OnEntitySpawn()
	{
		entity.Attributes.SetDouble("spawnX", entity.ServerPos.X);
		entity.Attributes.SetDouble("spawnY", entity.ServerPos.Y);
		entity.Attributes.SetDouble("spawnZ", entity.ServerPos.Z);
		spawnPosition.Set(entity.ServerPos.XYZ);
	}

	protected override bool CheckExecutionChance()
	{
		return base.Rand.NextDouble() <= (double)((failedWanders > 0) ? (1f - Config.ExecutionChance * 4f * (float)failedWanders) : Config.ExecutionChance);
	}

	protected virtual bool LoadNextWanderTarget()
	{
		EnumHabitat habitat = entity.Properties.Habitat;
		bool flag = false;
		if (FailedConsecutivePathfinds > 10)
		{
			WanderRangeMul = Math.Max(0.1f, WanderRangeMul * 0.9f);
		}
		else
		{
			WanderRangeMul = Math.Min(1f, WanderRangeMul * 1.1f);
			if (Config.DoRandomWanderRangeChanges && base.Rand.NextDouble() < 0.05)
			{
				WanderRangeMul = Math.Min(1f, WanderRangeMul * 1.5f);
			}
		}
		float num = WanderRangeMul;
		if (Config.DoRandomWanderRangeChanges && base.Rand.NextDouble() < 0.05)
		{
			num *= 3f;
		}
		for (int i = 0; i < Config.MaxBlocksChecked; i++)
		{
			double num2 = Config.WanderRangeHorizontal.nextFloat() * (float)(base.Rand.Next(2) * 2 - 1) * num;
			double num3 = Config.WanderRangeVertical.nextFloat() * (float)(base.Rand.Next(2) * 2 - 1) * num;
			double num4 = Config.WanderRangeHorizontal.nextFloat() * (float)(base.Rand.Next(2) * 2 - 1) * num;
			currentTarget.X = entity.ServerPos.X + num2;
			currentTarget.Y = entity.ServerPos.InternalY + num3;
			currentTarget.Z = entity.ServerPos.Z + num4;
			currentTarget.W = 1.0;
			if (Config.StayCloseToSpawn)
			{
				double num5 = (double)currentTarget.SquareDistanceTo(spawnPosition) / (Config.MaxDistanceToSpawn * Config.MaxDistanceToSpawn);
				currentTarget.W = 1.0 - num5;
			}
			switch (habitat)
			{
			case EnumHabitat.Air:
			{
				int rainMapHeightAt = world.BlockAccessor.GetRainMapHeightAt((int)currentTarget.X, (int)currentTarget.Z);
				currentTarget.Y = Math.Min(currentTarget.Y, (float)rainMapHeightAt + Config.MaxHeight);
				if (entity.World.BlockAccessor.GetBlockRaw((int)currentTarget.X, (int)currentTarget.Y, (int)currentTarget.Z, 2).IsLiquid())
				{
					currentTarget.W = 0.0;
				}
				break;
			}
			case EnumHabitat.Land:
			{
				currentTarget.Y = MoveDownToFloor((int)currentTarget.X, currentTarget.Y, (int)currentTarget.Z);
				if (currentTarget.Y < 0.0)
				{
					currentTarget.W = 0.0;
					break;
				}
				if (entity.World.BlockAccessor.GetBlockRaw((int)currentTarget.X, (int)currentTarget.Y, (int)currentTarget.Z, 2).IsLiquid())
				{
					currentTarget.W /= 2.0;
				}
				bool stop = false;
				bool willFall = false;
				float yaw = (float)Math.Atan2(num2, num4) + (float)Math.PI / 2f;
				Vec3d vec3d = new Vec3d(currentTarget.X, currentTarget.Y, currentTarget.Z).Ahead(1.0, 0f, yaw);
				Vec3d vec3d2 = new Vec3d(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z).Ahead(1.0, 0f, yaw);
				int prevY = (int)vec3d2.Y;
				GameMath.BresenHamPlotLine2d((int)vec3d2.X, (int)vec3d2.Z, (int)vec3d.X, (int)vec3d.Z, delegate(int x, int z)
				{
					if (!stop)
					{
						double num7 = MoveDownToFloor(x, prevY, z);
						if (num7 < 0.0 || (double)prevY - num7 > 4.0)
						{
							willFall = true;
							stop = true;
						}
						if (num7 - (double)prevY > 2.0)
						{
							stop = true;
						}
						prevY = (int)num7;
					}
				});
				if (willFall)
				{
					currentTarget.W = 0.0;
				}
				break;
			}
			case EnumHabitat.Sea:
				if (!entity.World.BlockAccessor.GetBlockRaw((int)currentTarget.X, (int)currentTarget.Y, (int)currentTarget.Z, 2).IsLiquid())
				{
					currentTarget.W = 0.0;
				}
				break;
			case EnumHabitat.Underwater:
				if (!entity.World.BlockAccessor.GetBlockRaw((int)currentTarget.X, (int)currentTarget.Y, (int)currentTarget.Z, 2).IsLiquid())
				{
					currentTarget.W = 0.0;
				}
				else
				{
					currentTarget.W = 1.0 / (Math.Abs(num3) + 1.0);
				}
				break;
			}
			if (currentTarget.W > 0.0)
			{
				for (int num6 = 0; num6 < BlockFacing.HORIZONTALS.Length; num6++)
				{
					if (entity.World.BlockAccessor.IsSideSolid((int)currentTarget.X + BlockFacing.HORIZONTALS[num6].Normali.X, (int)currentTarget.Y, (int)currentTarget.Z + BlockFacing.HORIZONTALS[num6].Normali.Z, BlockFacing.HORIZONTALS[num6].Opposite))
					{
						currentTarget.W *= 0.5;
					}
				}
			}
			if (!Config.IgnoreLightLevel && currentTarget.W != 0.0)
			{
				blockPosBuffer.Set((int)currentTarget.X, (int)currentTarget.Y, (int)currentTarget.Z);
				int val = Math.Abs(Config.PreferredLightLevel - entity.World.BlockAccessor.GetLightLevel(blockPosBuffer, Config.PreferredLightLevelType));
				currentTarget.W /= Math.Max(1, val);
			}
			if (!flag || currentTarget.W > bestTarget.W)
			{
				flag = true;
				bestTarget.Set(currentTarget.X, currentTarget.Y, currentTarget.Z, currentTarget.W);
				if (currentTarget.W >= 1.0)
				{
					break;
				}
			}
		}
		if (bestTarget.W > 0.0)
		{
			FailedConsecutivePathfinds = Math.Max(FailedConsecutivePathfinds - 3, 0);
			mainTarget.Set(bestTarget.X, bestTarget.Y, bestTarget.Z);
			return true;
		}
		FailedConsecutivePathfinds++;
		return false;
	}

	protected virtual double MoveDownToFloor(int x, double y, int z)
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

	protected virtual void OnStuck()
	{
		stopTask = true;
		failedWanders++;
	}

	protected virtual void OnGoalReached()
	{
		stopTask = true;
		failedWanders = 0;
	}
}
