using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public class EntityBehaviorPassivePhysics : PhysicsBehaviorBase, IPhysicsTickable, IRemotePhysics
{
	private readonly Vec3d prevPos = new Vec3d();

	private double motionBeforeY;

	private bool feetInLiquidBefore;

	private bool onGroundBefore;

	private bool swimmingBefore;

	private bool collidedBefore;

	protected Vec3d newPos = new Vec3d();

	private double waterDragValue = GlobalConstants.WaterDrag;

	private double airDragValue = GlobalConstants.AirDragAlways;

	private double groundDragValue = 0.699999988079071;

	private double gravityPerSecond = GlobalConstants.GravityPerSecond;

	public Action<float> OnPhysicsTickCallback;

	public Entity Entity => entity;

	public bool Ticking { get; set; } = true;

	public EntityBehaviorPassivePhysics(Entity entity)
		: base(entity)
	{
	}

	public void SetState(EntityPos pos)
	{
		prevPos.Set(pos);
		motionBeforeY = pos.Motion.Y;
		Entity entity = base.entity;
		onGroundBefore = entity.OnGround;
		feetInLiquidBefore = entity.FeetInLiquid;
		swimmingBefore = entity.Swimming;
		collidedBefore = entity.Collided;
	}

	public virtual void SetProperties(JsonObject attributes)
	{
		waterDragValue = 1.0 - (1.0 - waterDragValue) * attributes["waterDragFactor"].AsDouble(1.0);
		JsonObject jsonObject = attributes["airDragFactor"];
		double num = (jsonObject.Exists ? jsonObject.AsDouble(1.0) : attributes["airDragFallingFactor"].AsDouble(1.0));
		airDragValue = 1.0 - (1.0 - airDragValue) * num;
		if (entity.WatchedAttributes.HasAttribute("airDragFactor"))
		{
			airDragValue = 1f - (1f - GlobalConstants.AirDragAlways) * (float)entity.WatchedAttributes.GetDouble("airDragFactor");
		}
		groundDragValue = 0.3 * attributes["groundDragFactor"].AsDouble(1.0);
		gravityPerSecond *= attributes["gravityFactor"].AsDouble(1.0);
		if (entity.WatchedAttributes.HasAttribute("gravityFactor"))
		{
			gravityPerSecond = GlobalConstants.GravityPerSecond * (float)entity.WatchedAttributes.GetDouble("gravityFactor");
		}
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		Init();
		SetProperties(attributes);
		if (entity.Api is ICoreServerAPI coreServerAPI)
		{
			coreServerAPI.Server.AddPhysicsTickable(this);
			return;
		}
		EnumHandling handled = EnumHandling.Handled;
		OnReceivedServerPos(isTeleport: true, ref handled);
	}

	public override void OnReceivedServerPos(bool isTeleport, ref EnumHandling handled)
	{
	}

	public void OnReceivedClientPos(int version)
	{
		if (version > previousVersion)
		{
			previousVersion = version;
			HandleRemotePhysics(1f / 15f, isTeleport: true);
		}
		else
		{
			HandleRemotePhysics(1f / 15f, isTeleport: false);
		}
	}

	public void HandleRemotePhysics(float dt, bool isTeleport)
	{
		if (nPos == null)
		{
			nPos = new Vec3d();
			nPos.Set(entity.ServerPos);
		}
		float num = dt * 60f;
		EntityPos entityPos = lPos;
		entityPos.SetFrom(nPos);
		nPos.Set(entity.ServerPos);
		Vec3d motion = entityPos.Motion;
		if (isTeleport)
		{
			entityPos.SetFrom(nPos);
		}
		motion.X = (nPos.X - entityPos.X) / (double)num;
		motion.Y = (nPos.Y - entityPos.Y) / (double)num;
		motion.Z = (nPos.Z - entityPos.Z) / (double)num;
		if (motion.Length() > 20.0)
		{
			motion.Set(0.0, 0.0, 0.0);
		}
		entity.Pos.Motion.Set(motion);
		entity.ServerPos.Motion.Set(motion);
		PhysicsBehaviorBase.collisionTester.NewTick(entityPos);
		entity.Pos.SetFrom(entity.ServerPos);
		SetState(entityPos);
		RemoteMotionAndCollision(entityPos, num);
		ApplyTests(entityPos);
	}

	public void RemoteMotionAndCollision(EntityPos pos, float dtFactor)
	{
		double num = gravityPerSecond / 60.0 * (double)dtFactor + Math.Max(0.0, -0.014999999664723873 * pos.Motion.Y * (double)dtFactor);
		pos.Motion.Y -= num;
		PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, pos, dtFactor, ref newPos, 0f, CollisionYExtra);
		bool flag = pos.Motion.Y < 0.0;
		entity.OnGround = entity.CollidedVertically && flag;
		pos.Motion.Y += num;
		pos.SetPos(nPos);
	}

	public void MotionAndCollision(EntityPos pos, float dt)
	{
		float num = 60f * dt;
		Entity entity = base.entity;
		Vec3d motion = pos.Motion;
		IBlockAccessor blockAccessor = entity.World.BlockAccessor;
		int dimension = pos.Dimension;
		if (onGroundBefore)
		{
			if (motion.HorLength() < 1E-05)
			{
				motion.X = 0.0;
				motion.Z = 0.0;
			}
			else if (!feetInLiquidBefore)
			{
				Block blockRaw = blockAccessor.GetBlockRaw((int)pos.X, (int)(pos.InternalY - 0.05000000074505806), (int)pos.Z, 1);
				double num2 = 1.0 - groundDragValue * (double)blockRaw.DragMultiplier;
				motion.X *= num2;
				motion.Z *= num2;
			}
		}
		Block block = null;
		if (feetInLiquidBefore || swimmingBefore)
		{
			motion.Scale(Math.Pow(waterDragValue, dt * 33f));
			block = blockAccessor.GetBlockRaw((int)pos.X, (int)pos.InternalY, (int)pos.Z, 2);
			if (feetInLiquidBefore)
			{
				Vec3d pushVector = block.PushVector;
				if (pushVector != null)
				{
					float num3 = 300f / GameMath.Clamp(entity.MaterialDensity, 750f, 2500f) * num;
					motion.Add(pushVector.X * (double)num3, pushVector.Y * (double)num3, pushVector.Z * (double)num3);
				}
			}
		}
		else
		{
			motion.Scale((float)Math.Pow(airDragValue, dt * 33f));
		}
		if (entity.ApplyGravity)
		{
			double num4 = gravityPerSecond / 60.0 * (double)num + Math.Max(0.0, -0.014999999664723873 * motion.Y * (double)num);
			if (entity.Swimming)
			{
				float num5 = GameMath.Clamp(1f - entity.MaterialDensity / (float)block.MaterialDensity, -1f, 1f);
				Block blockRaw2 = blockAccessor.GetBlockRaw((int)pos.X, (int)(pos.InternalY + 1.0), (int)pos.Z, 2);
				float num6 = GameMath.Clamp((float)(int)pos.Y + (float)block.LiquidLevel / 8f + (blockRaw2.IsLiquid() ? 1.125f : 0f) - (float)pos.Y - (entity.SelectionBox.Y2 - (float)entity.SwimmingOffsetY), 0f, 1f);
				double num7 = GameMath.Clamp(60f * num5 * num6, -1.5f, 1.5f) - 1f;
				double num8 = GameMath.Clamp(100.0 * Math.Abs(motion.Y * (double)num) - 0.019999999552965164, 1.0, 1.25);
				motion.Y += num4 * num7;
				motion.Y /= num8;
			}
			else
			{
				motion.Y -= num4;
			}
		}
		double num9 = motion.X * (double)num + pos.X;
		double num10 = motion.Y * (double)num + pos.Y;
		double num11 = motion.Z * (double)num + pos.Z;
		applyCollision(pos, num);
		Vec3d vec3d = newPos;
		if (blockAccessor.IsNotTraversable((int)num9, (int)pos.Y, (int)pos.Z, dimension))
		{
			vec3d.X = pos.X;
		}
		if (blockAccessor.IsNotTraversable((int)pos.X, (int)num10, (int)pos.Z, dimension))
		{
			vec3d.Y = pos.Y;
		}
		if (blockAccessor.IsNotTraversable((int)pos.X, (int)pos.Y, (int)num11, dimension))
		{
			vec3d.Z = pos.Z;
		}
		pos.SetPos(vec3d);
		if ((num9 < vec3d.X && motion.X < 0.0) || (num9 > vec3d.X && motion.X > 0.0))
		{
			motion.X = 0.0;
		}
		if ((num10 < vec3d.Y && motion.Y < 0.0) || (num10 > vec3d.Y && motion.Y > 0.0))
		{
			motion.Y = 0.0;
		}
		if ((num11 < vec3d.Z && motion.Z < 0.0) || (num11 > vec3d.Z && motion.Z > 0.0))
		{
			motion.Z = 0.0;
		}
	}

	protected virtual void applyCollision(EntityPos pos, float dtFactor)
	{
		PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, pos, dtFactor, ref newPos, 0f, CollisionYExtra);
	}

	public void ApplyTests(EntityPos pos)
	{
		Entity entity = base.entity;
		IBlockAccessor blockAccessor = entity.World.BlockAccessor;
		bool flag = pos.Motion.Y <= 0.0;
		entity.OnGround = entity.CollidedVertically && flag;
		Block blockRaw = blockAccessor.GetBlockRaw((int)pos.X, (int)pos.InternalY, (int)pos.Z, 2);
		entity.FeetInLiquid = blockRaw.MatterState == EnumMatterState.Liquid;
		entity.InLava = blockRaw.LiquidCode == "lava";
		if (entity.FeetInLiquid)
		{
			Block blockRaw2 = blockAccessor.GetBlockRaw((int)pos.X, (int)(pos.InternalY + 1.0), (int)pos.Z, 2);
			float num = (float)(int)pos.Y + (float)blockRaw.LiquidLevel / 8f + (blockRaw2.IsLiquid() ? 1.125f : 0f) - (float)pos.Y - (entity.SelectionBox.Y2 - (float)entity.SwimmingOffsetY);
			entity.Swimming = num > 0f;
			if (!feetInLiquidBefore && !(entity is EntityAgent { MountedOn: not null }) && !entity.IsFirstTick())
			{
				entity.OnCollideWithLiquid();
			}
		}
		else
		{
			entity.Swimming = false;
			if (swimmingBefore || feetInLiquidBefore)
			{
				entity.OnExitedLiquid();
			}
		}
		if (!collidedBefore && entity.Collided)
		{
			entity.OnCollided();
		}
		if (entity.OnGround)
		{
			if (!onGroundBefore)
			{
				entity.OnFallToGround(motionBeforeY);
			}
			entity.PositionBeforeFalling.Set(newPos);
		}
		if (GlobalConstants.OutsideWorld(pos.X, pos.Y, pos.Z, entity.World.BlockAccessor))
		{
			entity.DespawnReason = new EntityDespawnData
			{
				Reason = EnumDespawnReason.Death,
				DamageSourceForDeath = new DamageSource
				{
					Source = EnumDamageSource.Fall
				}
			};
			return;
		}
		Cuboidd entityBox = PhysicsBehaviorBase.collisionTester.entityBox;
		int num2 = (int)entityBox.X2;
		int num3 = (int)entityBox.Y2;
		int num4 = (int)entityBox.Z2;
		int num5 = (int)entityBox.Z1;
		BlockPos tmpPos = PhysicsBehaviorBase.collisionTester.tmpPos;
		tmpPos.dimension = entity.Pos.Dimension;
		for (int i = (int)entityBox.Y1; i <= num3; i++)
		{
			for (int j = (int)entityBox.X1; j <= num2; j++)
			{
				for (int k = num5; k <= num4; k++)
				{
					tmpPos.Set(j, i, k);
					blockAccessor.GetBlock(tmpPos).OnEntityInside(entity.World, entity, tmpPos);
				}
			}
		}
		OnPhysicsTickCallback?.Invoke(0f);
		entity.PhysicsUpdateWatcher?.Invoke(0f, prevPos);
	}

	public void OnPhysicsTick(float dt)
	{
		Entity entity = base.entity;
		if (entity.State != EnumEntityState.Active || !Ticking)
		{
			return;
		}
		IMountable mountable = mountableSupplier;
		if (mountable == null || !mountable.IsBeingControlled() || entity.World.Side != EnumAppSide.Server)
		{
			EntityPos sidedPos = entity.SidedPos;
			PhysicsBehaviorBase.collisionTester.AssignToEntity(this, sidedPos.Dimension);
			int num = ((!(sidedPos.Motion.Length() > 0.1)) ? 1 : 10);
			float dt2 = dt / (float)num;
			for (int i = 0; i < num; i++)
			{
				SetState(sidedPos);
				MotionAndCollision(sidedPos, dt2);
				ApplyTests(sidedPos);
			}
			entity.Pos.SetFrom(sidedPos);
		}
	}

	public void AfterPhysicsTick(float dt)
	{
		entity.AfterPhysicsTick?.Invoke();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		if (sapi != null)
		{
			sapi.Server.RemovePhysicsTickable(this);
		}
	}

	public override string PropertyName()
	{
		return "entitypassivephysics";
	}
}
