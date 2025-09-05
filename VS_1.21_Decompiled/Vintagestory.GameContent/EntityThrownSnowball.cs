using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityThrownSnowball : Entity, IProjectile
{
	protected bool beforeCollided;

	protected bool stuck;

	protected long msLaunch;

	protected Vec3d motionBeforeCollide = new Vec3d();

	protected CollisionTester collTester = new CollisionTester();

	public Entity FiredBy;

	public float Damage;

	public int DamageTier;

	public ItemStack ProjectileStack;

	public bool NonCollectible;

	public float collidedAccum;

	public float VerticalImpactBreakChance = 1f;

	public float HorizontalImpactBreakChance = 1f;

	public float ImpactParticleSize = 1.5f;

	public int ImpactParticleCount = 10;

	public override bool IsInteractable => false;

	Entity IProjectile.FiredBy
	{
		get
		{
			return FiredBy;
		}
		set
		{
			FiredBy = value;
		}
	}

	float IProjectile.Damage
	{
		get
		{
			return Damage;
		}
		set
		{
			Damage = value;
		}
	}

	int IProjectile.DamageTier
	{
		get
		{
			return DamageTier;
		}
		set
		{
			DamageTier = value;
		}
	}

	EnumDamageType IProjectile.DamageType { get; set; }

	bool IProjectile.IgnoreInvFrames { get; set; }

	ItemStack IProjectile.ProjectileStack
	{
		get
		{
			return ProjectileStack;
		}
		set
		{
			ProjectileStack = value;
		}
	}

	ItemStack IProjectile.WeaponStack { get; set; }

	float IProjectile.DropOnImpactChance { get; set; }

	bool IProjectile.DamageStackOnImpact { get; set; }

	bool IProjectile.NonCollectible
	{
		get
		{
			return NonCollectible;
		}
		set
		{
			NonCollectible = value;
		}
	}

	bool IProjectile.EntityHit { get; }

	float IProjectile.Weight
	{
		get
		{
			return base.Properties.Weight;
		}
		set
		{
			base.Properties.Weight = value;
		}
	}

	bool IProjectile.Stuck
	{
		get
		{
			return stuck;
		}
		set
		{
			stuck = value;
		}
	}

	void IProjectile.PreInitialize()
	{
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		msLaunch = World.ElapsedMilliseconds;
		if (ProjectileStack?.Collectible != null)
		{
			ProjectileStack.ResolveBlockOrItem(World);
		}
		GetBehavior<EntityBehaviorPassivePhysics>().CollisionYExtra = 0f;
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (ShouldDespawn)
		{
			return;
		}
		EntityPos sidedPos = base.SidedPos;
		stuck = base.Collided;
		if (stuck)
		{
			sidedPos.Pitch = 0f;
			sidedPos.Roll = 0f;
			sidedPos.Yaw = (float)Math.PI / 2f;
			collidedAccum += dt;
			if (NonCollectible && collidedAccum > 1f)
			{
				Die();
			}
		}
		else
		{
			sidedPos.Pitch = (float)World.ElapsedMilliseconds / 300f % ((float)Math.PI * 2f);
			sidedPos.Roll = 0f;
			sidedPos.Yaw = (float)World.ElapsedMilliseconds / 400f % ((float)Math.PI * 2f);
		}
		if (World is IServerWorldAccessor)
		{
			Entity nearestEntity = World.GetNearestEntity(ServerPos.XYZ, 5f, 5f, (Entity e) => e.EntityId != EntityId && (FiredBy == null || e.EntityId != FiredBy.EntityId || World.ElapsedMilliseconds - msLaunch >= 500) && e.IsInteractable && e.SelectionBox.ToDouble().Translate(e.ServerPos.X, e.ServerPos.Y, e.ServerPos.Z).ShortestDistanceFrom(ServerPos.X, ServerPos.Y, ServerPos.Z) < 0.5);
			if (nearestEntity != null)
			{
				nearestEntity.ReceiveDamage(new DamageSource
				{
					Source = ((FiredBy is EntityPlayer) ? EnumDamageSource.Player : EnumDamageSource.Entity),
					SourceEntity = this,
					CauseEntity = FiredBy,
					Type = (((double)Damage > 0.01) ? EnumDamageType.BluntAttack : EnumDamageType.Frost),
					DamageTier = DamageTier,
					YDirKnockbackDiv = 3f
				}, Damage);
				World.PlaySoundAt(new AssetLocation("sounds/block/snow"), this, null, randomizePitch: false);
				World.SpawnCubeParticles(base.SidedPos.XYZ.AddCopy(base.SidedPos.Motion.X, base.SidedPos.Motion.Y, base.SidedPos.Motion.Z), ProjectileStack, 0.2f, 12, 1.2f);
				Die();
				return;
			}
		}
		beforeCollided = false;
		motionBeforeCollide.Set(sidedPos.Motion.X, sidedPos.Motion.Y, sidedPos.Motion.Z);
	}

	public override void OnCollided()
	{
		EntityPos sidedPos = base.SidedPos;
		if (!beforeCollided && World is IServerWorldAccessor)
		{
			float num = GameMath.Clamp((float)motionBeforeCollide.Length() * 4f, 0f, 1f);
			if (CollidedHorizontally)
			{
				float num2 = ((sidedPos.Motion.X != 0.0) ? 1 : (-1));
				float num3 = ((sidedPos.Motion.Z != 0.0) ? 1 : (-1));
				sidedPos.Motion.X = (double)num2 * motionBeforeCollide.X * 0.4000000059604645;
				sidedPos.Motion.Z = (double)num3 * motionBeforeCollide.Z * 0.4000000059604645;
				if (num > 0.1f && World.Rand.NextDouble() > (double)(1f - HorizontalImpactBreakChance))
				{
					World.SpawnCubeParticles(base.SidedPos.XYZ.OffsetCopy(0.0, 0.2, 0.0), ProjectileStack, 0.5f, ImpactParticleCount, ImpactParticleSize, null, new Vec3f(num2 * (float)motionBeforeCollide.X * 8f, 0f, num3 * (float)motionBeforeCollide.Z * 8f));
					Die();
				}
			}
			if (CollidedVertically && motionBeforeCollide.Y <= 0.0)
			{
				sidedPos.Motion.Y = GameMath.Clamp(motionBeforeCollide.Y * -0.30000001192092896, -0.10000000149011612, 0.10000000149011612);
				if (num > 0.1f && World.Rand.NextDouble() > (double)(1f - VerticalImpactBreakChance))
				{
					World.SpawnCubeParticles(base.SidedPos.XYZ.OffsetCopy(0.0, 0.2, 0.0), ProjectileStack, 0.5f, ImpactParticleCount, ImpactParticleSize, null, new Vec3f((float)motionBeforeCollide.X * 8f, (float)(0.0 - motionBeforeCollide.Y) * 6f, (float)motionBeforeCollide.Z * 8f));
					Die();
				}
			}
			World.PlaySoundAt(new AssetLocation("sounds/block/snow"), this, null, randomizePitch: false, 32f, num);
			World.SpawnCubeParticles(base.SidedPos.XYZ.OffsetCopy(0.0, 0.25, 0.0), ProjectileStack, 0.5f, ImpactParticleCount, ImpactParticleSize, null, new Vec3f((float)motionBeforeCollide.X * 8f, (float)(0.0 - motionBeforeCollide.Y) * 6f, (float)motionBeforeCollide.Z * 8f));
			WatchedAttributes.MarkAllDirty();
		}
		beforeCollided = true;
	}

	public override bool CanCollect(Entity byEntity)
	{
		return false;
	}

	public override void OnCollideWithLiquid()
	{
		base.OnCollideWithLiquid();
	}

	public override void ToBytes(BinaryWriter writer, bool forClient)
	{
		base.ToBytes(writer, forClient);
		writer.Write(beforeCollided);
		ProjectileStack.ToBytes(writer);
	}

	public override void FromBytes(BinaryReader reader, bool fromServer)
	{
		base.FromBytes(reader, fromServer);
		beforeCollided = reader.ReadBoolean();
		ProjectileStack = ((World == null) ? new ItemStack(reader) : new ItemStack(reader, World));
	}
}
