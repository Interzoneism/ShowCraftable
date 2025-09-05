using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskTurretMode : AiTaskBaseTargetable
{
	private long lastSearchTotalMs;

	protected int searchWaitMs = 2000;

	private float minTurnAnglePerSec;

	private float maxTurnAnglePerSec;

	private float curTurnRadPerSec;

	private float projectileDamage;

	private int projectileDamageTier;

	private AssetLocation projectileCode;

	private float maxTurnAngleRad;

	private float maxOffAngleThrowRad;

	private float spawnAngleRad;

	private bool immobile;

	private float sensingRange;

	private float firingRangeMin;

	private float firingRangeMax;

	private float abortRange;

	private EnumTurretState currentState;

	private float currentStateTime;

	private bool executing;

	private EntityProjectile prevProjectile;

	private double overshootAdjustment;

	private bool inFiringRange
	{
		get
		{
			double num = targetEntity.ServerPos.DistanceTo(entity.ServerPos);
			if (num >= (double)firingRangeMin)
			{
				return num <= (double)firingRangeMax;
			}
			return false;
		}
	}

	private bool inSensingRange => targetEntity.ServerPos.DistanceTo(entity.ServerPos) <= (double)sensingRange;

	private bool inAbortRange => targetEntity.ServerPos.DistanceTo(entity.ServerPos) <= (double)abortRange;

	public AiTaskTurretMode(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		projectileDamage = taskConfig["projectileDamage"].AsFloat(1f);
		projectileDamageTier = taskConfig["projectileDamageTier"].AsInt(1);
		sensingRange = taskConfig["sensingRange"].AsFloat(30f);
		firingRangeMin = taskConfig["firingRangeMin"].AsFloat(14f);
		firingRangeMax = taskConfig["firingRangeMax"].AsFloat(26f);
		abortRange = taskConfig["abortRange"].AsFloat(14f);
		projectileCode = AssetLocation.Create(taskConfig["projectileCode"].AsString("thrownstone-{rock}"), entity.Code.Domain);
		immobile = taskConfig["immobile"].AsBool();
		maxTurnAngleRad = taskConfig["maxTurnAngleDeg"].AsFloat(360f) * ((float)Math.PI / 180f);
		maxOffAngleThrowRad = taskConfig["maxOffAngleThrowDeg"].AsFloat() * ((float)Math.PI / 180f);
		spawnAngleRad = entity.Attributes.GetFloat("spawnAngleRad");
	}

	public override void AfterInitialize()
	{
		base.AfterInitialize();
		entity.AnimManager.OnAnimationStopped += AnimManager_OnAnimationStopped;
	}

	private void AnimManager_OnAnimationStopped(string anim)
	{
		if (executing && targetEntity != null)
		{
			updateState();
		}
	}

	public override bool ShouldExecute()
	{
		if (base.rand.NextDouble() > 0.10000000149011612 && (WhenInEmotionState == null || !IsInEmotionState(WhenInEmotionState)))
		{
			return false;
		}
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		if (lastSearchTotalMs + searchWaitMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		if (WhenInEmotionState == null && base.rand.NextDouble() > 0.5)
		{
			return false;
		}
		if (cooldownUntilMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		lastSearchTotalMs = entity.World.ElapsedMilliseconds;
		float range = sensingRange;
		targetEntity = partitionUtil.GetNearestEntity(entity.ServerPos.XYZ, range, (Entity e) => IsTargetableEntity(e, range) && hasDirectContact(e, range, range / 2f) && aimableDirection(e), EnumEntitySearchType.Creatures);
		if (targetEntity != null)
		{
			return !inAbortRange;
		}
		return false;
	}

	private bool aimableDirection(Entity e)
	{
		if (!immobile)
		{
			return true;
		}
		float aimYaw = getAimYaw(e);
		if (aimYaw > spawnAngleRad - maxTurnAngleRad - maxOffAngleThrowRad)
		{
			return aimYaw < spawnAngleRad + maxTurnAngleRad + maxOffAngleThrowRad;
		}
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		ITreeAttribute treeAttribute = entity?.Properties.Server?.Attributes?.GetTreeAttribute("pathfinder");
		if (treeAttribute != null)
		{
			minTurnAnglePerSec = treeAttribute.GetFloat("minTurnAnglePerSec", 250f);
			maxTurnAnglePerSec = treeAttribute.GetFloat("maxTurnAnglePerSec", 450f);
		}
		else
		{
			minTurnAnglePerSec = 250f;
			maxTurnAnglePerSec = 450f;
		}
		curTurnRadPerSec = minTurnAnglePerSec + (float)entity.World.Rand.NextDouble() * (maxTurnAnglePerSec - minTurnAnglePerSec);
		curTurnRadPerSec *= (float)Math.PI / 180f;
		currentState = EnumTurretState.Idle;
		currentStateTime = 0f;
		executing = true;
	}

	private void updateState()
	{
		switch (currentState)
		{
		case EnumTurretState.Idle:
			if (inFiringRange)
			{
				entity.StartAnimation("load");
				currentState = EnumTurretState.TurretMode;
				currentStateTime = 0f;
			}
			else if (inSensingRange)
			{
				entity.StartAnimation("turret");
				currentState = EnumTurretState.TurretMode;
				currentStateTime = 0f;
			}
			break;
		case EnumTurretState.TurretMode:
			if (isAnimDone("turret"))
			{
				if (inAbortRange)
				{
					abort();
				}
				else if (inFiringRange)
				{
					currentState = EnumTurretState.TurretModeLoad;
					entity.StopAnimation("turret");
					entity.StartAnimation("load-fromturretpose");
					entity.World.PlaySoundAt("sounds/creature/bowtorn/draw", entity, null, randomizePitch: false);
					currentStateTime = 0f;
				}
				else if (currentStateTime > 5f)
				{
					currentState = EnumTurretState.Stop;
					entity.StopAnimation("turret");
				}
			}
			break;
		case EnumTurretState.TurretModeLoad:
			if (isAnimDone("load"))
			{
				entity.StartAnimation("hold");
				currentState = EnumTurretState.TurretModeHold;
				currentStateTime = 0f;
			}
			break;
		case EnumTurretState.TurretModeHold:
			if (inFiringRange || inAbortRange)
			{
				if ((double)currentStateTime > 1.25)
				{
					fireProjectile();
					currentState = EnumTurretState.TurretModeFired;
					entity.StopAnimation("hold");
					entity.StartAnimation("fire");
				}
			}
			else if (currentStateTime > 2f)
			{
				currentState = EnumTurretState.TurretModeUnload;
				entity.StopAnimation("hold");
				entity.StartAnimation("unload");
			}
			break;
		case EnumTurretState.TurretModeUnload:
			if (isAnimDone("unload"))
			{
				currentState = EnumTurretState.Stop;
			}
			break;
		case EnumTurretState.TurretModeFired:
		{
			float num = sensingRange;
			if (inAbortRange || !targetEntity.Alive || !targetablePlayerMode((targetEntity as EntityPlayer)?.Player) || !hasDirectContact(targetEntity, num, num / 2f))
			{
				abort();
			}
			else if (inSensingRange)
			{
				currentState = EnumTurretState.TurretModeReload;
				entity.StartAnimation("reload");
				entity.World.PlaySoundAt("sounds/creature/bowtorn/reload", entity, null, randomizePitch: false);
			}
			break;
		}
		case EnumTurretState.TurretModeReload:
			if (isAnimDone("reload"))
			{
				if (inAbortRange)
				{
					abort();
					break;
				}
				entity.World.PlaySoundAt("sounds/creature/bowtorn/draw", entity, null, randomizePitch: false);
				currentState = EnumTurretState.TurretModeLoad;
			}
			break;
		}
	}

	private void abort()
	{
		currentState = EnumTurretState.Stop;
		entity.StopAnimation("hold");
		entity.StopAnimation("turret");
		AiTaskManager taskManager = entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager;
		taskManager.GetTask<AiTaskStayInRange>().targetEntity = targetEntity;
		taskManager.ExecuteTask<AiTaskStayInRange>();
	}

	private bool isAnimDone(string anim)
	{
		RunningAnimation animationState = entity.AnimManager.GetAnimationState(anim);
		if (animationState.Running)
		{
			return (double)animationState.AnimProgress >= 0.95;
		}
		return true;
	}

	private void fireProjectile()
	{
		AssetLocation assetLocation = projectileCode.Clone();
		if (projectileCode.Path.Contains('{'))
		{
			string newValue = "granite";
			IMapChunk mapChunkAtBlockPos = entity.World.BlockAccessor.GetMapChunkAtBlockPos(entity.Pos.AsBlockPos);
			if (mapChunkAtBlockPos != null)
			{
				int num = (int)entity.Pos.Z % 32;
				int num2 = (int)entity.Pos.X % 32;
				newValue = entity.World.Blocks[mapChunkAtBlockPos.TopRockIdMap[num * 32 + num2]].Variant["rock"] ?? "granite";
			}
			assetLocation.Path = assetLocation.Path.Replace("{rock}", newValue);
		}
		EntityProperties entityType = entity.World.GetEntityType(assetLocation);
		if (entityType == null)
		{
			throw new Exception("No such projectile exists - " + assetLocation);
		}
		EntityProjectile entityProjectile = entity.World.ClassRegistry.CreateEntity(entityType) as EntityProjectile;
		entityProjectile.FiredBy = entity;
		entityProjectile.Damage = projectileDamage;
		entityProjectile.DamageTier = projectileDamageTier;
		entityProjectile.ProjectileStack = new ItemStack(entity.World.GetItem(new AssetLocation("stone-granite")));
		entityProjectile.NonCollectible = true;
		Vec3d vec3d = entity.ServerPos.XYZ.Add(0.0, entity.LocalEyePos.Y, 0.0);
		Vec3d vec3d2 = targetEntity.ServerPos.XYZ.Add(0.0, targetEntity.LocalEyePos.Y, 0.0) + targetEntity.ServerPos.Motion * 8f;
		double num3 = vec3d.DistanceTo(vec3d2);
		double num4 = prevProjectile?.ServerPos.Motion.Length() ?? 0.0;
		if (prevProjectile != null && !prevProjectile.EntityHit && num4 < 0.01)
		{
			float num5 = vec3d.DistanceTo(prevProjectile.ServerPos.XYZ);
			if (num3 > (double)num5)
			{
				overshootAdjustment = (0.0 - ((double)num5 - num3)) / 4.0;
			}
			else
			{
				overshootAdjustment = (num3 - (double)num5) / 4.0;
			}
		}
		num3 += overshootAdjustment;
		double num6 = Math.Pow(num3, 0.2);
		Vec3d vec3d3 = (vec3d2 - vec3d).Normalize() * GameMath.Clamp(num6 - 1.0, 0.10000000149011612, 1.0);
		vec3d3.Y += (num3 - 10.0) / 200.0;
		entityProjectile.ServerPos.SetPosWithDimension(entity.ServerPos.XYZ.Add(0.0, entity.LocalEyePos.Y, 0.0));
		entityProjectile.ServerPos.Motion.Set(vec3d3);
		entityProjectile.SetInitialRotation();
		entityProjectile.Pos.SetFrom(entityProjectile.ServerPos);
		entityProjectile.World = entity.World;
		entity.World.SpawnEntity(entityProjectile);
		if (prevProjectile == null || num4 < 0.01)
		{
			prevProjectile = entityProjectile;
		}
		entity.World.PlaySoundAt("sounds/creature/bowtorn/release", entity, null, randomizePitch: false);
	}

	public override bool ContinueExecute(float dt)
	{
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		currentStateTime += dt;
		updateState();
		float aimYaw = getAimYaw(targetEntity);
		aimYaw = GameMath.Clamp(aimYaw, spawnAngleRad - maxTurnAngleRad, spawnAngleRad + maxTurnAngleRad);
		float val = GameMath.AngleRadDistance(entity.ServerPos.Yaw, aimYaw);
		entity.ServerPos.Yaw += GameMath.Clamp(val, (0f - curTurnRadPerSec) * dt, curTurnRadPerSec * dt);
		entity.ServerPos.Yaw = entity.ServerPos.Yaw % ((float)Math.PI * 2f);
		return currentState != EnumTurretState.Stop;
	}

	private float getAimYaw(Entity targetEntity)
	{
		Vec3f vec3f = new Vec3f();
		vec3f.Set((float)(targetEntity.ServerPos.X - entity.ServerPos.X), (float)(targetEntity.ServerPos.Y - entity.ServerPos.Y), (float)(targetEntity.ServerPos.Z - entity.ServerPos.Z));
		return (float)Math.Atan2(vec3f.X, vec3f.Z);
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		entity.StopAnimation("turret");
		entity.StopAnimation("hold");
		executing = false;
		prevProjectile = null;
	}
}
