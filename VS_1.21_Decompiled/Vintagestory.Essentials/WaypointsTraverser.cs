using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Vintagestory.Essentials;

public class WaypointsTraverser : PathTraverserBase
{
	private float minTurnAnglePerSec;

	private float maxTurnAnglePerSec;

	private Vec3f targetVec = new Vec3f();

	private List<Vec3d> waypoints;

	private List<Vec3d> newWaypoints;

	private PathfinderTask asyncSearchObject;

	private int waypointToReachIndex;

	private long lastWaypointIncTotalMs;

	private Vec3d desiredTarget;

	private PathfindSystem psys;

	private PathfindingAsync asyncPathfinder;

	protected EnumAICreatureType creatureType;

	public bool PathFindDebug;

	private Action OnNoPath;

	public Action OnFoundPath;

	private Action OnGoalReached_New;

	private Action OnStuck_New;

	private float movingSpeed_New;

	private float targetDistance_New;

	private Vec3d prevPos = new Vec3d(0.0, -2000.0, 0.0);

	private Vec3d prevPrevPos = new Vec3d(0.0, -1000.0, 0.0);

	private float prevPosAccum;

	private float sqDistToTarget;

	private float distCheckAccum;

	private float lastDistToTarget;

	public override Vec3d CurrentTarget => waypoints[waypoints.Count - 1];

	public override bool Ready
	{
		get
		{
			if (waypoints != null)
			{
				return asyncSearchObject == null;
			}
			return false;
		}
	}

	public WaypointsTraverser(EntityAgent entity, EnumAICreatureType creatureType = EnumAICreatureType.Default)
		: base(entity)
	{
		if (entity?.Properties.Server?.Attributes?.GetTreeAttribute("pathfinder") != null)
		{
			minTurnAnglePerSec = (float)entity.Properties.Server.Attributes.GetTreeAttribute("pathfinder").GetDecimal("minTurnAnglePerSec", 250.0);
			maxTurnAnglePerSec = (float)entity.Properties.Server.Attributes.GetTreeAttribute("pathfinder").GetDecimal("maxTurnAnglePerSec", 450.0);
		}
		else
		{
			minTurnAnglePerSec = 250f;
			maxTurnAnglePerSec = 450f;
		}
		psys = entity.World.Api.ModLoader.GetModSystem<PathfindSystem>();
		asyncPathfinder = entity.World.Api.ModLoader.GetModSystem<PathfindingAsync>();
		this.creatureType = creatureType;
	}

	public void FollowRoute(List<Vec3d> swoopPath, float movingSpeed, float targetDistance, Action OnGoalReached, Action OnStuck)
	{
		waypoints = swoopPath;
		base.WalkTowards(desiredTarget, movingSpeed, targetDistance, OnGoalReached, OnStuck);
	}

	public override bool NavigateTo(Vec3d target, float movingSpeed, float targetDistance, Action OnGoalReached, Action OnStuck, Action onNoPath = null, bool giveUpWhenNoPath = false, int searchDepth = 999, int mhdistanceTolerance = 0, EnumAICreatureType? creatureType = null)
	{
		desiredTarget = target;
		OnNoPath = onNoPath;
		OnStuck_New = OnStuck;
		OnGoalReached_New = OnGoalReached;
		movingSpeed_New = movingSpeed;
		targetDistance_New = targetDistance;
		if (creatureType.HasValue)
		{
			this.creatureType = creatureType.Value;
		}
		BlockPos asBlockPos = entity.ServerPos.AsBlockPos;
		if (entity.World.BlockAccessor.IsNotTraversable(asBlockPos))
		{
			HandleNoPath();
			return false;
		}
		FindPath(asBlockPos, target.AsBlockPos, searchDepth, mhdistanceTolerance);
		return AfterFoundPath();
	}

	public override bool NavigateTo_Async(Vec3d target, float movingSpeed, float targetDistance, Action OnGoalReached, Action OnStuck, Action onNoPath = null, int searchDepth = 999, int mhdistanceTolerance = 0, EnumAICreatureType? creatureType = null)
	{
		if (asyncSearchObject != null)
		{
			return false;
		}
		desiredTarget = target;
		if (creatureType.HasValue)
		{
			this.creatureType = creatureType.Value;
		}
		OnNoPath = onNoPath;
		OnGoalReached_New = OnGoalReached;
		OnStuck_New = OnStuck;
		movingSpeed_New = movingSpeed;
		targetDistance_New = targetDistance;
		BlockPos asBlockPos = entity.ServerPos.AsBlockPos;
		if (entity.World.BlockAccessor.IsNotTraversable(asBlockPos))
		{
			HandleNoPath();
			return false;
		}
		FindPath_Async(asBlockPos, target.AsBlockPos, searchDepth, mhdistanceTolerance);
		return true;
	}

	private void FindPath(BlockPos startBlockPos, BlockPos targetBlockPos, int searchDepth, int mhdistanceTolerance = 0)
	{
		waypointToReachIndex = 0;
		float stepHeight = entity.GetBehavior<EntityBehaviorControlledPhysics>()?.StepHeight ?? 0.6f;
		int maxFallHeight = (entity.Properties.FallDamage ? (Math.Min(8, (int)Math.Round(3.51 / Math.Max(0.01, entity.Properties.FallDamageMultiplier))) - (int)(movingSpeed * 30f)) : 8);
		newWaypoints = psys.FindPathAsWaypoints(startBlockPos, targetBlockPos, maxFallHeight, stepHeight, entity.CollisionBox, searchDepth, mhdistanceTolerance, creatureType);
	}

	public PathfinderTask PreparePathfinderTask(BlockPos startBlockPos, BlockPos targetBlockPos, int searchDepth = 999, int mhdistanceTolerance = 0, EnumAICreatureType? creatureType = null)
	{
		float stepHeight = entity.GetBehavior<EntityBehaviorControlledPhysics>()?.StepHeight ?? 0.6f;
		int num;
		if (entity.Properties.FallDamage)
		{
			JsonObject attributes = entity.Properties.Attributes;
			if (attributes == null || !attributes["reckless"].AsBool())
			{
				num = 4 - (int)(movingSpeed * 30f);
				goto IL_0071;
			}
		}
		num = 12;
		goto IL_0071;
		IL_0071:
		int maxFallHeight = num;
		return new PathfinderTask(startBlockPos, targetBlockPos, maxFallHeight, stepHeight, entity.CollisionBox, searchDepth, mhdistanceTolerance, creatureType ?? this.creatureType);
	}

	private void FindPath_Async(BlockPos startBlockPos, BlockPos targetBlockPos, int searchDepth, int mhdistanceTolerance = 0)
	{
		waypointToReachIndex = 0;
		asyncSearchObject = PreparePathfinderTask(startBlockPos, targetBlockPos, searchDepth, mhdistanceTolerance, creatureType);
		asyncPathfinder.EnqueuePathfinderTask(asyncSearchObject);
	}

	public bool AfterFoundPath()
	{
		if (asyncSearchObject != null)
		{
			newWaypoints = asyncSearchObject.waypoints;
			asyncSearchObject = null;
		}
		if (newWaypoints == null)
		{
			HandleNoPath();
			return false;
		}
		waypoints = newWaypoints;
		if (PathFindDebug)
		{
			List<BlockPos> list = new List<BlockPos>();
			List<int> list2 = new List<int>();
			int num = 0;
			foreach (Vec3d waypoint in waypoints)
			{
				list.Add(waypoint.AsBlockPos);
				list2.Add(ColorUtil.ColorFromRgba(128, 128, Math.Min(255, 128 + num * 8), 150));
				num++;
			}
			list.Add(desiredTarget.AsBlockPos);
			list2.Add(ColorUtil.ColorFromRgba(128, 0, 255, 255));
			IPlayer player = entity.World.AllOnlinePlayers[0];
			entity.World.HighlightBlocks(player, 2, list, list2);
		}
		waypoints.Add(desiredTarget);
		base.WalkTowards(desiredTarget, movingSpeed_New, targetDistance_New, OnGoalReached_New, OnStuck_New);
		OnFoundPath?.Invoke();
		return true;
	}

	public void HandleNoPath()
	{
		waypoints = new List<Vec3d>();
		if (PathFindDebug)
		{
			List<BlockPos> list = new List<BlockPos>();
			List<int> list2 = new List<int>();
			int num = 0;
			foreach (PathNode item in entity.World.Api.ModLoader.GetModSystem<PathfindSystem>().astar.closedSet)
			{
				list.Add(item);
				list2.Add(ColorUtil.ColorFromRgba(Math.Min(255, num * 4), 0, 0, 150));
				num++;
			}
			IPlayer player = entity.World.AllOnlinePlayers[0];
			entity.World.HighlightBlocks(player, 2, list, list2);
		}
		waypoints.Add(desiredTarget);
		base.WalkTowards(desiredTarget, movingSpeed_New, targetDistance_New, OnGoalReached_New, OnStuck_New);
		if (OnNoPath != null)
		{
			Active = false;
			OnNoPath();
		}
	}

	public override bool WalkTowards(Vec3d target, float movingSpeed, float targetDistance, Action OnGoalReached, Action OnStuck, EnumAICreatureType creatureType = EnumAICreatureType.Default)
	{
		waypoints = new List<Vec3d>();
		waypoints.Add(target);
		return base.WalkTowards(target, movingSpeed, targetDistance, OnGoalReached, OnStuck, creatureType);
	}

	protected override bool BeginGo()
	{
		entity.Controls.Forward = true;
		entity.ServerControls.Forward = true;
		curTurnRadPerSec = minTurnAnglePerSec + (float)entity.World.Rand.NextDouble() * (maxTurnAnglePerSec - minTurnAnglePerSec);
		curTurnRadPerSec *= 0.87266463f;
		stuckCounter = 0;
		waypointToReachIndex = 0;
		lastWaypointIncTotalMs = entity.World.ElapsedMilliseconds;
		distCheckAccum = 0f;
		prevPosAccum = 0f;
		return true;
	}

	public override void OnGameTick(float dt)
	{
		if (asyncSearchObject != null)
		{
			if (!asyncSearchObject.Finished)
			{
				return;
			}
			AfterFoundPath();
		}
		if (!Active)
		{
			return;
		}
		bool nearHorizontally = false;
		int num = 0;
		bool flag = IsNearTarget(num++, ref nearHorizontally) || IsNearTarget(num++, ref nearHorizontally) || IsNearTarget(num++, ref nearHorizontally);
		if (flag)
		{
			waypointToReachIndex += num;
			lastWaypointIncTotalMs = entity.World.ElapsedMilliseconds;
		}
		target = waypoints[Math.Min(waypoints.Count - 1, waypointToReachIndex)];
		_ = waypointToReachIndex;
		_ = waypoints.Count;
		if (waypointToReachIndex >= waypoints.Count)
		{
			Stop();
			OnGoalReached?.Invoke();
			return;
		}
		bool flag2 = nearHorizontally && !flag && entity.Properties.Habitat == EnumHabitat.Land;
		bool flag3 = (entity.CollidedVertically && entity.Controls.IsClimbing) || (entity.CollidedHorizontally && entity.ServerPos.Motion.Y <= 0.0) || flag2 || (entity.CollidedHorizontally && waypoints.Count > 1 && waypointToReachIndex < waypoints.Count && entity.World.ElapsedMilliseconds - lastWaypointIncTotalMs > 2000);
		double num2 = prevPrevPos.SquareDistanceTo(prevPos);
		flag3 |= num2 < 0.0001 && entity.World.Rand.NextDouble() < GameMath.Clamp(1.0 - num2 * 1.2, 0.1, 0.9);
		prevPosAccum += dt;
		if ((double)prevPosAccum > 0.2)
		{
			prevPosAccum = 0f;
			prevPrevPos.Set(prevPos);
			prevPos.Set(entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z);
		}
		distCheckAccum += dt;
		if (distCheckAccum > 2f)
		{
			distCheckAccum = 0f;
			if ((double)Math.Abs(sqDistToTarget - lastDistToTarget) < 0.1)
			{
				flag3 = true;
				stuckCounter += 30;
			}
			else if (!flag3)
			{
				stuckCounter = 0;
			}
			lastDistToTarget = sqDistToTarget;
		}
		if (flag3)
		{
			stuckCounter++;
		}
		if (GlobalConstants.OverallSpeedMultiplier > 0f && (float)stuckCounter > 60f / GlobalConstants.OverallSpeedMultiplier)
		{
			Stop();
			OnStuck?.Invoke();
			return;
		}
		EntityControls entityControls = ((entity.MountedOn == null) ? entity.Controls : entity.MountedOn.Controls);
		if (entityControls == null)
		{
			return;
		}
		targetVec.Set((float)(target.X - entity.ServerPos.X), (float)(target.Y - entity.ServerPos.InternalY), (float)(target.Z - entity.ServerPos.Z));
		targetVec.Normalize();
		float end = 0f;
		if ((double)sqDistToTarget >= 0.01)
		{
			end = (float)Math.Atan2(targetVec.X, targetVec.Z);
		}
		float num3 = movingSpeed;
		if (sqDistToTarget < 1f)
		{
			num3 = Math.Max(0.005f, movingSpeed * Math.Max(sqDistToTarget, 0.2f));
		}
		float num4 = GameMath.AngleRadDistance(entity.ServerPos.Yaw, end);
		float num5 = curTurnRadPerSec * dt * GlobalConstants.OverallSpeedMultiplier * movingSpeed;
		entity.ServerPos.Yaw += GameMath.Clamp(num4, 0f - num5, num5);
		entity.ServerPos.Yaw = entity.ServerPos.Yaw % ((float)Math.PI * 2f);
		double z = Math.Cos(entity.ServerPos.Yaw);
		double x = Math.Sin(entity.ServerPos.Yaw);
		entityControls.WalkVector.Set(x, GameMath.Clamp(targetVec.Y, -1f, 1f), z);
		entityControls.WalkVector.Mul(num3 * GlobalConstants.OverallSpeedMultiplier / Math.Max(1f, Math.Abs(num4) * 3f));
		if (entity.Properties.RotateModelOnClimb && entity.Controls.IsClimbing && entity.ClimbingIntoFace != null && entity.Alive)
		{
			BlockFacing climbingIntoFace = entity.ClimbingIntoFace;
			if (Math.Sign(climbingIntoFace.Normali.X) == Math.Sign(entityControls.WalkVector.X))
			{
				entityControls.WalkVector.X = 0.0;
			}
			if (Math.Sign(climbingIntoFace.Normali.Y) == Math.Sign(entityControls.WalkVector.Y))
			{
				entityControls.WalkVector.Y = 0.0 - entityControls.WalkVector.Y;
			}
			if (Math.Sign(climbingIntoFace.Normali.Z) == Math.Sign(entityControls.WalkVector.Z))
			{
				entityControls.WalkVector.Z = 0.0;
			}
		}
		if (entity.Properties.Habitat == EnumHabitat.Underwater)
		{
			entityControls.FlyVector.Set(entityControls.WalkVector);
			Vec3d xYZ = entity.Pos.XYZ;
			Block blockRaw = entity.World.BlockAccessor.GetBlockRaw((int)xYZ.X, (int)xYZ.Y, (int)xYZ.Z, 2);
			Block blockRaw2 = entity.World.BlockAccessor.GetBlockRaw((int)xYZ.X, (int)(xYZ.Y + 1.0), (int)xYZ.Z, 2);
			float num6 = GameMath.Clamp((float)(int)xYZ.Y + (float)blockRaw.LiquidLevel / 8f + (blockRaw2.IsLiquid() ? 1.125f : 0f) - (float)xYZ.Y - (float)entity.SwimmingOffsetY, 0f, 1f);
			num6 = 1f - Math.Min(1f, num6 + 0.5f);
			if (num6 > 0f)
			{
				entityControls.FlyVector.Y = GameMath.Clamp(entityControls.FlyVector.Y, -0.03999999910593033, -0.019999999552965164) * (double)(1f - num6);
				return;
			}
			float num7 = movingSpeed * GlobalConstants.OverallSpeedMultiplier / (float)Math.Sqrt(targetVec.X * targetVec.X + targetVec.Z * targetVec.Z);
			entityControls.FlyVector.Y = targetVec.Y * num7;
		}
		else if (entity.Swimming)
		{
			entityControls.FlyVector.Set(entityControls.WalkVector);
			Vec3d xYZ2 = entity.Pos.XYZ;
			Block blockRaw3 = entity.World.BlockAccessor.GetBlockRaw((int)xYZ2.X, (int)xYZ2.Y, (int)xYZ2.Z, 2);
			Block blockRaw4 = entity.World.BlockAccessor.GetBlockRaw((int)xYZ2.X, (int)(xYZ2.Y + 1.0), (int)xYZ2.Z, 2);
			float num8 = GameMath.Clamp((float)(int)xYZ2.Y + (float)blockRaw3.LiquidLevel / 8f + (blockRaw4.IsLiquid() ? 1.125f : 0f) - (float)xYZ2.Y - (float)entity.SwimmingOffsetY, 0f, 1f);
			num8 = Math.Min(1f, num8 + 0.5f);
			entityControls.FlyVector.Y = GameMath.Clamp(entityControls.FlyVector.Y, 0.019999999552965164, 0.03999999910593033) * (double)num8;
			if (entity.CollidedHorizontally)
			{
				entityControls.FlyVector.Y = 0.05000000074505806;
			}
		}
	}

	private bool IsNearTarget(int waypointOffset, ref bool nearHorizontally)
	{
		if (waypoints.Count - 1 < waypointToReachIndex + waypointOffset)
		{
			return false;
		}
		int index = Math.Min(waypoints.Count - 1, waypointToReachIndex + waypointOffset);
		Vec3d vec3d = waypoints[index];
		double internalY = entity.ServerPos.InternalY;
		sqDistToTarget = vec3d.HorizontalSquareDistanceTo(entity.ServerPos.X, entity.ServerPos.Z);
		double num = (vec3d.Y - internalY) * (vec3d.Y - internalY);
		bool flag = internalY > vec3d.Y;
		sqDistToTarget += (float)Math.Max(0.0, num - (flag ? 1.0 : 0.5));
		if (!nearHorizontally)
		{
			double num2 = vec3d.HorizontalSquareDistanceTo(entity.ServerPos.X, entity.ServerPos.Z);
			nearHorizontally = num2 < (double)(TargetDistance * TargetDistance);
		}
		return sqDistToTarget < TargetDistance * TargetDistance;
	}

	public override void Stop()
	{
		Active = false;
		entity.Controls.Forward = false;
		entity.ServerControls.Forward = false;
		entity.Controls.WalkVector.Set(0.0, 0.0, 0.0);
		stuckCounter = 0;
		distCheckAccum = 0f;
		prevPosAccum = 0f;
		asyncSearchObject = null;
	}

	public override void Retarget()
	{
		Active = true;
		distCheckAccum = 0f;
		prevPosAccum = 0f;
		waypointToReachIndex = waypoints.Count - 1;
	}
}
