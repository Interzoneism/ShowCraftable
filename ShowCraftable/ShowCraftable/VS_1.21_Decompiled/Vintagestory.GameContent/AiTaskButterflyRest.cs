using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class AiTaskButterflyRest : AiTaskBase
{
	public Vec3d MainTarget;

	private int taskState;

	private float moveSpeed = 0.03f;

	private float targetDistance = 0.07f;

	private double searchFrequency = 0.05000000074505806;

	private double restUntilTotalHours;

	private BlockPos tmpPos = new BlockPos();

	private EnumRestReason reason;

	private WeatherSystemServer wsys;

	public AiTaskButterflyRest(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		(entity.Api as ICoreServerAPI).Event.DidBreakBlock += Event_DidBreakBlock;
		wsys = entity.Api.ModLoader.GetModSystem<WeatherSystemServer>();
		targetDistance = taskConfig["targetDistance"].AsFloat(0.07f);
		moveSpeed = taskConfig["movespeed"].AsFloat(0.03f);
		searchFrequency = taskConfig["searchFrequency"].AsFloat(0.07f);
		cooldownUntilTotalHours = entity.World.Calendar.TotalHours + mincooldownHours + entity.World.Rand.NextDouble() * (maxcooldownHours - mincooldownHours);
	}

	public override void OnEntityDespawn(EntityDespawnData reason)
	{
		(entity.Api as ICoreServerAPI).Event.DidBreakBlock -= Event_DidBreakBlock;
	}

	private void Event_DidBreakBlock(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel)
	{
		if (tmpPos != null && blockSel.Position.Equals(tmpPos))
		{
			taskState = 4;
		}
	}

	public override bool ShouldExecute()
	{
		if (base.rand.NextDouble() > searchFrequency)
		{
			return false;
		}
		reason = EnumRestReason.NoReason;
		if (cooldownUntilTotalHours < entity.World.Calendar.TotalHours)
		{
			reason = EnumRestReason.TakingABreak;
		}
		else if ((double)entity.World.Calendar.GetDayLightStrength(entity.Pos.X, entity.Pos.Z) < 0.6)
		{
			reason = EnumRestReason.Night;
		}
		else
		{
			WeatherSystemServer weatherSystemServer = wsys;
			if ((weatherSystemServer != null && weatherSystemServer.WeatherDataSlowAccess.GetWindSpeed(entity.ServerPos.XYZ) > 0.75) || (double?)wsys?.GetPrecipitation(entity.ServerPos.XYZ) > 0.1)
			{
				reason = EnumRestReason.Wind;
			}
		}
		if (reason == EnumRestReason.NoReason)
		{
			return false;
		}
		double num = base.rand.NextDouble() * 4.0 - 2.0;
		double num2 = base.rand.NextDouble() * 4.0 - 2.0;
		for (int num3 = 1; num3 >= 0; num3--)
		{
			tmpPos.Set((int)(entity.ServerPos.X + num), 0, (int)(entity.ServerPos.Z + num2));
			tmpPos.Y = entity.World.BlockAccessor.GetTerrainMapheightAt(tmpPos) + num3;
			if (entity.World.BlockAccessor.GetBlock(tmpPos, 2).BlockId == 0)
			{
				Block block = entity.World.BlockAccessor.GetBlock(tmpPos);
				VertexFlags vertexFlags = block.VertexFlags;
				int num4;
				if (vertexFlags == null || vertexFlags.WindMode != EnumWindBitMode.WeakWind)
				{
					VertexFlags vertexFlags2 = block.VertexFlags;
					num4 = ((vertexFlags2 != null && vertexFlags2.WindMode == EnumWindBitMode.TallBend) ? 1 : 0);
				}
				else
				{
					num4 = 1;
				}
				bool flag = (byte)num4 != 0;
				JsonObject attributes = block.Attributes;
				if (attributes != null && attributes.IsTrue("butterflyFeed"))
				{
					double num5 = block.Attributes["sitHeight"].AsDouble(block.TopMiddlePos.Y);
					entity.WatchedAttributes.SetDouble("windWaveIntensity", (block.VertexFlags.WindMode == EnumWindBitMode.NoWind) ? 0.0 : (flag ? (num5 / 2.0) : num5));
					MainTarget = tmpPos.ToVec3d().Add(block.TopMiddlePos.X, num5, block.TopMiddlePos.Z);
					return true;
				}
				if (block.SideSolid[BlockFacing.UP.Index])
				{
					block = entity.World.BlockAccessor.GetBlock(tmpPos.UpCopy());
					if (!block.IsLiquid())
					{
						double num6 = block.TopMiddlePos.Y;
						SyncedTreeAttribute watchedAttributes = entity.WatchedAttributes;
						VertexFlags vertexFlags3 = block.VertexFlags;
						watchedAttributes.SetDouble("windWaveIntensity", (vertexFlags3 != null && vertexFlags3.WindMode == EnumWindBitMode.NoWind) ? 0.0 : (flag ? (num6 / 2.0) : num6));
						MainTarget = tmpPos.ToVec3d().Add(block.TopMiddlePos.X, num6 - 1.0, block.TopMiddlePos.Z);
						return true;
					}
				}
			}
		}
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		taskState = 0;
		pathTraverser.WalkTowards(MainTarget, moveSpeed, targetDistance, OnGoalReached, OnStuck);
	}

	public override bool ContinueExecute(float dt)
	{
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		if (taskState == 1)
		{
			entity.ServerPos.Motion.Set(0.0, 0.0, 0.0);
			entity.AnimManager.StartAnimation("rest");
			taskState = 2;
			double num = 0.5 + entity.World.Rand.NextDouble() * 3.0;
			restUntilTotalHours = entity.World.Calendar.TotalHours + num;
		}
		if (entity.World.Rand.NextDouble() > 0.05)
		{
			return true;
		}
		if (!entity.World.BlockAccessor.GetBlock(entity.Pos.AsBlockPos.Down()).SideSolid[BlockFacing.UP.Index])
		{
			return false;
		}
		if (entity.World.BlockAccessor.GetBlock(entity.Pos.AsBlockPos).IsLiquid())
		{
			return false;
		}
		switch (reason)
		{
		case EnumRestReason.Night:
			return (double)entity.World.Calendar.GetDayLightStrength(entity.Pos.X, entity.Pos.Z) < 0.8;
		case EnumRestReason.TakingABreak:
			if (taskState != 0)
			{
				return entity.World.Calendar.TotalHours < restUntilTotalHours;
			}
			return true;
		case EnumRestReason.Wind:
		{
			WeatherSystemServer weatherSystemServer = wsys;
			if (weatherSystemServer == null || !(weatherSystemServer.WeatherDataSlowAccess.GetWindSpeed(entity.ServerPos.XYZ) > 0.2))
			{
				return (double?)wsys?.GetPrecipitation(entity.ServerPos.XYZ) > 0.05;
			}
			return true;
		}
		default:
			return false;
		}
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		if (cancelled)
		{
			pathTraverser.Stop();
		}
		entity.StopAnimation("rest");
	}

	private void OnStuck()
	{
		taskState = 3;
	}

	private void OnGoalReached()
	{
		taskState = 1;
	}
}
