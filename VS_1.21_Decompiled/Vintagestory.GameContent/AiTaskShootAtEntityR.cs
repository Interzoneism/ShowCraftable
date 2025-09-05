using System;
using OpenTK.Mathematics;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskShootAtEntityR : AiTaskBaseTargetableR
{
	protected float minTurnAnglePerSec;

	protected float maxTurnAnglePerSec;

	protected float currentTurnRadPerSec;

	protected bool alreadyThrown;

	protected long previousTargetId;

	protected float currentYawDispersion;

	protected float currentPitchDispersion;

	protected const string defaultRockType = "granite";

	protected readonly NatFloat randomFloat;

	private AiTaskShootAtEntityConfig Config => GetConfig<AiTaskShootAtEntityConfig>();

	public AiTaskShootAtEntityR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskShootAtEntityConfig>(entity, taskConfig, aiConfig);
		randomFloat = new NatFloat(0f, 1f, Config.DispersionDistribution);
	}

	public override bool ShouldExecute()
	{
		if (!PreconditionsSatisficed() && (!Config.RetaliateUnconditionally || !base.RecentlyAttacked))
		{
			return false;
		}
		if (!CheckAndResetSearchCooldown())
		{
			return false;
		}
		return SearchForTarget();
	}

	public override void StartExecute()
	{
		if (targetEntity != null)
		{
			base.StartExecute();
			ITreeAttribute treeAttribute = entity.Properties.Server?.Attributes?.GetTreeAttribute("pathfinder");
			if (treeAttribute != null)
			{
				minTurnAnglePerSec = treeAttribute.GetFloat("minTurnAnglePerSec", Config.DefaultMinTurnAngleDegPerSec);
				maxTurnAnglePerSec = treeAttribute.GetFloat("maxTurnAnglePerSec", Config.DefaultMaxTurnAngleDegPerSec);
			}
			else
			{
				minTurnAnglePerSec = Config.DefaultMinTurnAngleDegPerSec;
				maxTurnAnglePerSec = Config.DefaultMaxTurnAngleDegPerSec;
			}
			currentTurnRadPerSec = minTurnAnglePerSec + (float)base.Rand.NextDouble() * (maxTurnAnglePerSec - minTurnAnglePerSec);
			currentTurnRadPerSec *= (float)Math.PI / 180f;
			alreadyThrown = false;
		}
	}

	public override bool ContinueExecute(float dt)
	{
		if (!base.ContinueExecute(dt))
		{
			return false;
		}
		if (targetEntity == null)
		{
			return false;
		}
		AdjustYaw(dt);
		if (entity.World.ElapsedMilliseconds - executionStartTimeMs > Config.ThrowAtMs && !alreadyThrown)
		{
			SetOrAdjustDispersion();
			ShootProjectile();
			alreadyThrown = true;
		}
		return true;
	}

	protected override bool IsTargetableEntity(Entity target, float range)
	{
		if (!base.IsTargetableEntity(target, range))
		{
			return false;
		}
		if (!HasDirectContact(target, range, range * Config.VerticalRangeFactor))
		{
			return false;
		}
		if (!CanAimAt(target))
		{
			return false;
		}
		return true;
	}

	protected virtual float GetAimYaw(Entity targetEntity)
	{
		FastVec3f fastVec3f = default(FastVec3f);
		fastVec3f.Set((float)(targetEntity.ServerPos.X - entity.ServerPos.X), (float)(targetEntity.ServerPos.Y - entity.ServerPos.Y), (float)(targetEntity.ServerPos.Z - entity.ServerPos.Z));
		return MathF.Atan2(fastVec3f.X, fastVec3f.Z);
	}

	protected virtual bool CanAimAt(Entity target)
	{
		if (!Config.Immobile)
		{
			return true;
		}
		float aimYaw = GetAimYaw(target);
		if (aimYaw > Config.SpawnAngleRad - Config.MaxTurnAngleRad - Config.MaxThrowingAngleRad)
		{
			return aimYaw < Config.SpawnAngleRad + Config.MaxTurnAngleRad + Config.MaxThrowingAngleRad;
		}
		return false;
	}

	protected virtual AssetLocation ReplaceRockType(AssetLocation code)
	{
		if (!Config.ReplaceRockVariant)
		{
			return code;
		}
		AssetLocation assetLocation = code.Clone();
		string newValue = "granite";
		IMapChunk mapChunkAtBlockPos = entity.World.BlockAccessor.GetMapChunkAtBlockPos(entity.Pos.AsBlockPos);
		if (mapChunkAtBlockPos != null)
		{
			int num = (int)entity.Pos.Z % 32;
			int num2 = (int)entity.Pos.X % 32;
			newValue = entity.World.Blocks[mapChunkAtBlockPos.TopRockIdMap[num * 32 + num2]].Variant["rock"] ?? "granite";
		}
		assetLocation.Path = assetLocation.Path.Replace("{rock}", newValue);
		return assetLocation;
	}

	protected virtual void SetOrAdjustDispersion()
	{
		if (targetEntity != null)
		{
			if (targetEntity.EntityId == previousTargetId)
			{
				currentYawDispersion = MathF.Max(Config.YawDispersionDeg, currentYawDispersion - Config.DispersionReductionSpeedDeg);
				currentPitchDispersion = MathF.Max(Config.PitchDispersionDeg, currentPitchDispersion - Config.DispersionReductionSpeedDeg);
			}
			else
			{
				currentYawDispersion = MathF.Max(Config.MaxYawDispersionDeg, Config.YawDispersionDeg);
				currentPitchDispersion = MathF.Max(Config.MaxPitchDispersionDeg, Config.PitchDispersionDeg);
				previousTargetId = targetEntity.EntityId;
			}
		}
	}

	protected virtual void AdjustYaw(float dt)
	{
		if (targetEntity != null)
		{
			float aimYaw = GetAimYaw(targetEntity);
			aimYaw = GameMath.Clamp(aimYaw, Config.SpawnAngleRad - Config.MaxTurnAngleRad, Config.SpawnAngleRad + Config.MaxTurnAngleRad);
			float val = GameMath.AngleRadDistance(entity.ServerPos.Yaw, aimYaw);
			entity.ServerPos.Yaw += GameMath.Clamp(val, (0f - currentTurnRadPerSec) * dt, currentTurnRadPerSec * dt);
			entity.ServerPos.Yaw %= (float)Math.PI * 2f;
		}
	}

	protected virtual void ShootProjectile()
	{
		if (targetEntity != null)
		{
			CreateProjectile(out Entity projectileEntity, out IProjectile projectile, out Item projectileItem);
			projectile.FiredBy = entity;
			projectile.Damage = Config.ProjectileDamage;
			projectile.DamageTier = Config.ProjectileDamageTier;
			projectile.DamageType = Config.ProjectileDamageType;
			projectile.IgnoreInvFrames = Config.IgnoreInvFrames;
			projectile.ProjectileStack = new ItemStack(projectileItem);
			projectile.NonCollectible = Config.NonCollectible;
			projectile.DropOnImpactChance = Config.DropOnImpactChance;
			projectile.DamageStackOnImpact = Config.DamageStackOnImpact;
			SetProjectilePositionAndVelocity(projectileEntity, projectile, Config.ProjectileGravityFactor, Config.ProjectileSpeed);
			entity.World.SpawnPriorityEntity(projectileEntity);
			if (Config.ShootSound != null)
			{
				entity.World.PlaySoundAt(Config.ShootSound, entity, null, Config.RandomizePitch, Config.SoundRange, Config.SoundVolume);
			}
		}
	}

	protected virtual void CreateProjectile(out Entity projectileEntity, out IProjectile projectile, out Item projectileItem)
	{
		AssetLocation assetLocation = ReplaceRockType(Config.ProjectileCode);
		AssetLocation assetLocation2 = ReplaceRockType(Config.ProjectileItem);
		EntityProperties entityType = base.entity.World.GetEntityType(assetLocation);
		if (entityType == null)
		{
			throw new ArgumentException($"Error while running '{Config.Code}' AI task for entity '{base.entity.Code}': projectile entity with code '{assetLocation}' does not exist");
		}
		Entity entity = base.entity.World.ClassRegistry.CreateEntity(entityType);
		if (entity == null)
		{
			throw new ArgumentException($"Error while running '{Config.Code}' AI task for entity '{base.entity.Code}': unable to create entity with code '{assetLocation}'.");
		}
		if (!(entity is IProjectile projectile2))
		{
			throw new ArgumentException($"Error while running '{Config.Code}' AI task for entity '{base.entity.Code}': projectile entity '{assetLocation}' should have 'IProjectile' interface.");
		}
		projectile = projectile2;
		Item item = base.entity.World.GetItem(assetLocation2);
		if (item == null)
		{
			throw new ArgumentException($"Error while running '{Config.Code}' AI task for entity '{base.entity.Code}': projectile item '{assetLocation2}' does not exist.");
		}
		projectileItem = item;
		projectileEntity = entity;
	}

	protected virtual void SetProjectilePositionAndVelocity(Entity projectileEntity, IProjectile projectile, double gravityFactor, double speed)
	{
		if (targetEntity == null)
		{
			return;
		}
		Vec3d vec3d = entity.ServerPos.XYZ.Add(0.0, entity.LocalEyePos.Y, 0.0);
		Vec3d vec3d2 = targetEntity.ServerPos.XYZ.Add(0.0, targetEntity.LocalEyePos.Y, 0.0);
		float num = 1f / 60f;
		speed *= (double)num;
		double acceleration = gravityFactor * (double)GlobalConstants.GravityPerSecond * (double)num;
		double num2 = (vec3d2 - vec3d).Length() / speed;
		vec3d2 += targetEntity.ServerPos.Motion * num2;
		FastVec3d start = new FastVec3d(vec3d.X, vec3d.Y, vec3d.Z);
		FastVec3d target = new FastVec3d(vec3d2.X, vec3d2.Y, vec3d2.Z);
		FastVec3d velocity = new FastVec3d(0.0, 0.0, 0.0);
		bool flag = false;
		for (int i = 0; i < 30; i++)
		{
			flag = SolveBallisticArc(out velocity, start, target, speed, acceleration);
			if (flag)
			{
				break;
			}
			speed *= 1.100000023841858;
		}
		if (!flag)
		{
			FallBackVelocity(out velocity, start, target);
		}
		velocity = ApplyDispersionToVelocity(velocity, currentYawDispersion, currentPitchDispersion);
		projectileEntity.ServerPos.SetPosWithDimension(entity.ServerPos.BehindCopy(0.21).XYZ.Add(0.0, entity.LocalEyePos.Y, 0.0));
		projectileEntity.ServerPos.Motion.Set(velocity.X, velocity.Y, velocity.Z);
		projectileEntity.Pos.SetFrom(projectileEntity.ServerPos);
		projectileEntity.World = entity.World;
		projectile.PreInitialize();
	}

	protected virtual bool SolveBallisticArc(out FastVec3d velocity, FastVec3d start, FastVec3d target, double speed, double acceleration)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		Vector3d start2 = default(Vector3d);
		((Vector3d)(ref start2))._002Ector(start.X, start.Y, start.Z);
		Vector3d target2 = default(Vector3d);
		((Vector3d)(ref target2))._002Ector(target.X, target.Y, target.Z);
		Vector3d velocity2;
		bool result = SolveBallisticArc(out velocity2, start2, target2, speed, acceleration);
		velocity = new FastVec3d(velocity2.X, velocity2.Y, velocity2.Z);
		return result;
	}

	protected virtual bool SolveBallisticArc(out Vector3d velocity, Vector3d start, Vector3d target, double speed, double acceleration)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		velocity = Vector3d.Zero;
		Vector3d val = target - start;
		Vector2d val2 = default(Vector2d);
		((Vector2d)(ref val2))._002Ector(val.X, val.Z);
		double length = ((Vector2d)(ref val2)).Length;
		double y = val.Y;
		double num = speed * speed;
		double num2 = num * num - acceleration * (acceleration * length * length + 2.0 * y * num);
		if (num2 < 0.0)
		{
			return false;
		}
		double num3 = Math.Sqrt(num2);
		double num4 = Math.Atan2(num - num3, acceleration * length);
		double num5 = speed * Math.Sin(num4);
		double num6 = speed * Math.Cos(num4);
		Vector2d val3 = Vector2d.Normalize(val2);
		double num7 = val3.X * num6;
		double num8 = val3.Y * num6;
		velocity = new Vector3d(num7, num5, num8);
		return true;
	}

	protected virtual void FallBackVelocity(out FastVec3d velocity, FastVec3d start, FastVec3d target)
	{
		if (targetEntity == null)
		{
			velocity = new FastVec3d(0.0, 0.0, 0.0);
			return;
		}
		Vec3d vec3d = new Vec3d(start.X, start.Y, start.Z);
		Vec3d vec3d2 = new Vec3d(target.X, target.Y, target.Z);
		double num = Math.Pow(vec3d.SquareDistanceTo(vec3d2), 0.1);
		Vec3d vec3d3 = (vec3d2 - vec3d).Normalize() * GameMath.Clamp(num - 1.0, 0.10000000149011612, 1.0);
		velocity = new FastVec3d(vec3d3.X, vec3d3.Y, vec3d3.Z);
	}

	protected virtual FastVec3d ApplyDispersionToVelocity(FastVec3d velocity, float yawDispersionDeg, float pitchDispersionDeg)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		Vector3d direction = Vector3d.Normalize(new Vector3d(velocity.X, velocity.Y, velocity.Z));
		Vector2 dispersionDeg = default(Vector2);
		((Vector2)(ref dispersionDeg))._002Ector(yawDispersionDeg, pitchDispersionDeg);
		Vector3d directionWithDispersion = GetDirectionWithDispersion(direction, dispersionDeg);
		double num = velocity.Length();
		return new FastVec3d(directionWithDispersion.X * num, directionWithDispersion.Y * num, directionWithDispersion.Z * num);
	}

	protected virtual Vector3d GetDirectionWithDispersion(Vector3d direction, Vector2 dispersionDeg)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		float num = randomFloat.nextFloat() * dispersionDeg.Y * ((float)Math.PI / 180f);
		float num2 = randomFloat.nextFloat() * dispersionDeg.X * ((float)Math.PI / 180f);
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector(0f, 0f, 1f);
		Vector3d val2 = Vector3.op_Implicit(val) - direction;
		if (!(((Vector3d)(ref val2)).Length < 1000000000.0))
		{
			val2 = Vector3.op_Implicit(val) + direction;
			if (!(((Vector3d)(ref val2)).Length < 1000000000.0))
			{
				goto IL_00a3;
			}
		}
		((Vector3)(ref val))._002Ector(0f, 1f, 0f);
		goto IL_00a3;
		IL_00a3:
		Vector3d val3 = Vector3d.Normalize(direction);
		Vector3d val4 = Vector3d.Normalize(Vector3d.Cross(val3, Vector3.op_Implicit(val)));
		Vector3d val5 = Vector3d.Normalize(Vector3d.Cross(val4, val3));
		Vector3d val6 = val4 * Math.Tan(num2);
		Vector3d val7 = val5 * Math.Tan(num);
		return Vector3d.Normalize(val3 + val6 + val7);
	}
}
