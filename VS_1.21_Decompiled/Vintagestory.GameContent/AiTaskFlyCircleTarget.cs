using System;
using VSEssentialsMod.Entity.AI.Task;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskFlyCircleTarget : AiTaskFlyCircle
{
	protected float seekingRangeVer = 25f;

	protected float seekingRangeHor = 25f;

	protected TimeSpan cooldownTime = TimeSpan.FromMilliseconds(1000.0);

	protected TimeSpan targetRetentionTime = TimeSpan.FromSeconds(30.0);

	protected int CurrentDimension => entity.Pos.Dimension;

	protected int TargetDimension => targetEntity?.Pos.Dimension ?? CurrentDimension;

	protected int OtherDimension
	{
		get
		{
			if (CurrentDimension != 0)
			{
				return 0;
			}
			return 2;
		}
	}

	public AiTaskFlyCircleTarget(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		seekingRangeHor = taskConfig["seekingRangeHor"].AsFloat(25f);
		seekingRangeVer = taskConfig["seekingRangeVer"].AsFloat(25f);
		cooldownTime = TimeSpan.FromMilliseconds(taskConfig["cooldownMs"].AsInt(1000));
		targetRetentionTime = TimeSpan.FromSeconds(taskConfig["targetRetentionTimeSec"].AsInt(30));
	}

	public override bool ShouldExecute()
	{
		long elapsedMilliseconds = entity.World.ElapsedMilliseconds;
		if (cooldownUntilMs > elapsedMilliseconds)
		{
			return false;
		}
		cooldownUntilMs = entity.World.ElapsedMilliseconds + (long)cooldownTime.TotalMilliseconds;
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		Vec3d vec3d = entity.ServerPos.XYZ.Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		if (entity.World.ElapsedMilliseconds - attackedByEntityMs > (long)targetRetentionTime.TotalMilliseconds)
		{
			attackedByEntity = null;
		}
		if (retaliateAttacks && attackedByEntity != null && attackedByEntity.Alive && attackedByEntity.IsInteractable && IsTargetableEntity(attackedByEntity, 15f, ignoreEntityCode: true))
		{
			targetEntity = attackedByEntity;
		}
		else
		{
			targetEntity = entity.World.GetNearestEntity(vec3d, seekingRangeHor, seekingRangeVer, (Entity e) => IsTargetableEntity(e, seekingRangeHor) && hasDirectContact(e, seekingRangeHor, seekingRangeVer));
			if (targetEntity == null)
			{
				vec3d.Y += dimensionOffset(CurrentDimension, OtherDimension);
				targetEntity = entity.World.GetNearestEntity(vec3d, seekingRangeHor, seekingRangeVer, (Entity e) => IsTargetableEntity(e, seekingRangeHor) && hasDirectContact(e, seekingRangeHor, seekingRangeVer));
			}
		}
		if (targetEntity != null)
		{
			return base.ShouldExecute();
		}
		return false;
	}

	public override void StartExecute()
	{
		timeSwitchToNormalWorld();
		base.StartExecute();
		CenterPos = targetEntity.ServerPos.XYZ;
		CenterPos.Y += dimensionOffset(TargetDimension, CurrentDimension);
	}

	public override bool ContinueExecute(float dt)
	{
		CenterPos = targetEntity.ServerPos.XYZ;
		CenterPos.Y += dimensionOffset(TargetDimension, CurrentDimension);
		return base.ContinueExecute(dt);
	}

	public override bool CanSensePlayer(EntityPlayer eplr, double range)
	{
		if (!friendlyTarget && AggressiveTargeting)
		{
			if (creatureHostility == EnumCreatureHostility.NeverHostile)
			{
				return false;
			}
			if (creatureHostility == EnumCreatureHostility.Passive && (bhEmo == null || (!IsInEmotionState("aggressiveondamage") && !IsInEmotionState("aggressivearoundentities"))))
			{
				return false;
			}
		}
		float num = eplr.Stats.GetBlended("animalSeekingRange");
		IPlayer player = eplr.Player;
		if (eplr.Controls.Sneak && eplr.OnGround)
		{
			num *= 0.6f;
		}
		EntityPos entityPos = eplr.Pos.Copy();
		entityPos.Dimension = CurrentDimension;
		if ((num == 1f || entity.ServerPos.DistanceTo(entityPos) < range * (double)num) && targetablePlayerMode(player))
		{
			return true;
		}
		return false;
	}

	protected double dimensionOffset(int fromDimension, int toDimension)
	{
		return (toDimension - fromDimension) * 32768;
	}

	protected override bool hasDirectContact(Entity targetEntity, float minDist, float minVerDist)
	{
		EntityPos entityPos = targetEntity.Pos.Copy();
		entityPos.Dimension = entity.Pos.Dimension;
		Cuboidd cuboidd = targetEntity.SelectionBox.ToDouble().Translate(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
		tmpPos.Set(entity.ServerPos).Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		double num = cuboidd.ShortestDistanceFrom(tmpPos);
		double num2 = Math.Abs(cuboidd.ShortestVerticalDistanceFrom(tmpPos.Y));
		if (num >= (double)minDist || num2 >= (double)minVerDist)
		{
			return false;
		}
		rayTraceFrom.Set(entity.ServerPos);
		rayTraceFrom.Y += 1.0 / 32.0;
		rayTraceTo.Set(entityPos);
		rayTraceTo.Y += 1.0 / 32.0;
		bool flag = false;
		entity.World.RayTraceForSelection(this, rayTraceFrom, rayTraceTo, ref blockSel, ref entitySel);
		flag = blockSel == null;
		if (!flag)
		{
			rayTraceFrom.Y += entity.SelectionBox.Y2 * 7f / 16f;
			rayTraceTo.Y += targetEntity.SelectionBox.Y2 * 7f / 16f;
			entity.World.RayTraceForSelection(this, rayTraceFrom, rayTraceTo, ref blockSel, ref entitySel);
			flag = blockSel == null;
		}
		if (!flag)
		{
			rayTraceFrom.Y += entity.SelectionBox.Y2 * 7f / 16f;
			rayTraceTo.Y += targetEntity.SelectionBox.Y2 * 7f / 16f;
			entity.World.RayTraceForSelection(this, rayTraceFrom, rayTraceTo, ref blockSel, ref entitySel);
			flag = blockSel == null;
		}
		if (!flag)
		{
			return false;
		}
		return true;
	}

	protected void timeSwitchToNormalWorld()
	{
		Timeswitch modSystem = entity.Api.ModLoader.GetModSystem<Timeswitch>();
		if (entity.ServerPos.Dimension != 0)
		{
			(entity as EntityErel)?.ChangeDimension(0);
			modSystem?.ChangeEntityDimensionOnClient(entity, 0);
		}
	}
}
