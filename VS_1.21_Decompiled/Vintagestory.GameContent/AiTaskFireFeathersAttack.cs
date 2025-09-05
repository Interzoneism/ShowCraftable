using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class AiTaskFireFeathersAttack : AiTaskFlyCircle
{
	public bool Enabled = true;

	protected float seekingRangeVer = 25f;

	protected float seekingRangeHor = 25f;

	protected int fireAfterMs;

	protected int durationMs;

	protected ProjectileConfig[] projectileConfigs;

	protected float accum;

	protected bool projectilesFired;

	protected float minVerticalDistance = 5f;

	protected float minHorizontalDistance = 10f;

	protected long globalAttackCooldownMs = 3000L;

	public AiTaskFireFeathersAttack(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		fireAfterMs = taskConfig["fireAfterMs"].AsInt(1000);
		durationMs = taskConfig["durationMs"].AsInt(1000);
		seekingRangeHor = taskConfig["seekingRangeHor"].AsFloat(25f);
		seekingRangeVer = taskConfig["seekingRangeVer"].AsFloat(25f);
		projectileConfigs = taskConfig["projectileConfigs"].AsObject<ProjectileConfig[]>(null, entity.Code.Domain);
		minVerticalDistance = taskConfig["minVerticalDistance"].AsFloat(5f);
		minHorizontalDistance = taskConfig["minHorizontalDistance"].AsFloat(10f);
		globalAttackCooldownMs = taskConfig["globalAttackCooldownMs"].AsInt(3000);
		ProjectileConfig[] array = projectileConfigs;
		foreach (ProjectileConfig projectileConfig in array)
		{
			projectileConfig.EntityType = entity.World.GetEntityType(projectileConfig.Code);
			if (projectileConfig.EntityType == null)
			{
				throw new Exception("No such projectile exists - " + projectileConfig.Code);
			}
			projectileConfig.CollectibleStack?.Resolve(entity.World, $"Projectile stack of {entity.Code}");
		}
	}

	public override bool ShouldExecute()
	{
		if (!Enabled)
		{
			return false;
		}
		CenterPos = SpawnPos;
		long elapsedMilliseconds = entity.World.ElapsedMilliseconds;
		if (cooldownUntilMs > elapsedMilliseconds)
		{
			return false;
		}
		cooldownUntilMs = entity.World.ElapsedMilliseconds + 1500;
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		if (!checkGlobalAttackCooldown())
		{
			return false;
		}
		if (entity.World.ElapsedMilliseconds - attackedByEntityMs > 30000)
		{
			attackedByEntity = null;
		}
		if (retaliateAttacks && attackedByEntity != null && attackedByEntity.Alive && attackedByEntity.IsInteractable && IsTargetableEntity(attackedByEntity, 15f, ignoreEntityCode: true))
		{
			targetEntity = attackedByEntity;
		}
		else
		{
			targetEntity = entity.World.GetNearestEntity(CenterPos, seekingRangeHor, seekingRangeVer, (Entity e) => IsTargetableEntity(e, seekingRangeHor) && hasDirectContact(e, seekingRangeHor, seekingRangeVer));
		}
		if (targetEntity != null && entity.ServerPos.Y - targetEntity.ServerPos.Y > (double)minVerticalDistance)
		{
			return entity.ServerPos.HorDistanceTo(targetEntity.ServerPos) > (double)minHorizontalDistance;
		}
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		accum = 0f;
		projectilesFired = false;
	}

	public override bool ContinueExecute(float dt)
	{
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		followTarget();
		accum += dt;
		if (accum * 1000f > (float)fireAfterMs)
		{
			if (!projectilesFired)
			{
				ProjectileConfig[] array = projectileConfigs;
				foreach (ProjectileConfig projectileConfig in array)
				{
					projectileConfig.LeftToFire = GameMath.RoundRandom(entity.World.Rand, projectileConfig.Quantity.nextFloat());
				}
				world.PlaySoundAt("sounds/creature/erel/fire", entity, null, randomizePitch: false, 100f);
			}
			fireProjectiles();
			projectilesFired = true;
		}
		if (base.ContinueExecute(dt))
		{
			return accum * 1000f < (float)durationMs;
		}
		return false;
	}

	public override void FinishExecute(bool cancelled)
	{
		(entity as EntityErel).LastAttackTime = entity.World.ElapsedMilliseconds;
		base.FinishExecute(cancelled);
	}

	protected void followTarget()
	{
		Vec3d vec3d = targetEntity.Pos.XYZ - entity.ServerPos.XYZ;
		vec3d.Normalize();
		entity.ServerPos.Motion.Length();
		_ = 0.01;
		entity.ServerPos.Yaw = (float)Math.Atan2(vec3d.X, vec3d.Z);
	}

	protected bool checkGlobalAttackCooldown()
	{
		long lastAttackTime = (entity as EntityErel).LastAttackTime;
		return entity.World.ElapsedMilliseconds - lastAttackTime > globalAttackCooldownMs;
	}

	protected void fireProjectiles()
	{
		IWorldAccessor worldAccessor = entity.World;
		Random random = worldAccessor.Rand;
		projectileConfigs = projectileConfigs.Shuffle(random);
		ProjectileConfig[] array = projectileConfigs;
		foreach (ProjectileConfig projectileConfig in array)
		{
			if (projectileConfig.LeftToFire > 0)
			{
				projectileConfig.LeftToFire--;
				EntityProjectile entityProjectile = worldAccessor.ClassRegistry.CreateEntity(projectileConfig.EntityType) as EntityProjectile;
				entityProjectile.FiredBy = entity;
				entityProjectile.DamageType = projectileConfig.DamageType;
				entityProjectile.Damage = projectileConfig.Damage;
				entityProjectile.DamageTier = projectileConfig.DamageTier;
				entityProjectile.ProjectileStack = projectileConfig.CollectibleStack?.ResolvedItemstack?.Clone() ?? new ItemStack(worldAccessor.GetItem(new AssetLocation("stone-granite")));
				entityProjectile.NonCollectible = projectileConfig.CollectibleStack?.ResolvedItemstack == null;
				entityProjectile.World = worldAccessor;
				Vec3d vec3d = entity.ServerPos.XYZ.Add(random.NextDouble() * 6.0 - 3.0, random.NextDouble() * 5.0, random.NextDouble() * 6.0 - 3.0);
				Vec3d vec3d2 = targetEntity.ServerPos.XYZ.Add(0.0, targetEntity.LocalEyePos.Y, 0.0) + targetEntity.ServerPos.Motion * 8f;
				double num = vec3d.DistanceTo(vec3d2);
				double num2 = Math.Pow(num, 0.2);
				Vec3d vec3d3 = (vec3d2 - vec3d).Normalize() * GameMath.Clamp(num2 - 1.0, 0.10000000149011612, 1.0);
				vec3d3.Y += (num - 10.0) / 150.0;
				vec3d3.X *= 1.0 + (random.NextDouble() - 0.5) / 3.0;
				vec3d3.Y *= 1.0 + (random.NextDouble() - 0.5) / 5.0;
				vec3d3.Z *= 1.0 + (random.NextDouble() - 0.5) / 3.0;
				entityProjectile.ServerPos.SetPosWithDimension(vec3d);
				entityProjectile.Pos.SetFrom(vec3d);
				entityProjectile.ServerPos.Motion.Set(vec3d3);
				entityProjectile.SetInitialRotation();
				worldAccessor.SpawnEntity(entityProjectile);
				break;
			}
		}
	}
}
