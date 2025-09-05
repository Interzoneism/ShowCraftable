using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityBehaviorPlayerPhysics : EntityBehaviorControlledPhysics, IRenderer, IDisposable, IRemotePhysics
{
	private IPlayer player;

	private IServerPlayer serverPlayer;

	private EntityPlayer entityPlayer;

	private const float interval = 1f / 60f;

	private float accum;

	private int currentTick;

	private int prevDimension;

	public const float ClippingToleranceOnDimensionChange = 0.0625f;

	public double RenderOrder => 1.0;

	public int RenderRange => 9999;

	public EntityBehaviorPlayerPhysics(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		entityPlayer = entity as EntityPlayer;
		Init();
		SetProperties(properties, attributes);
		if (entity.Api.Side == EnumAppSide.Client)
		{
			smoothStepping = true;
			capi.Event.RegisterRenderer(this, EnumRenderStage.Before, "playerphysics");
		}
		else
		{
			EnumHandling handled = EnumHandling.Handled;
			OnReceivedServerPos(isTeleport: true, ref handled);
		}
		entity.PhysicsUpdateWatcher?.Invoke(0f, entity.SidedPos.XYZ);
	}

	public override void SetModules()
	{
		physicsModules.Add(new PModuleWind());
		physicsModules.Add(new PModuleOnGround());
		physicsModules.Add(new PModulePlayerInLiquid(entityPlayer));
		physicsModules.Add(new PModulePlayerInAir());
		physicsModules.Add(new PModuleGravity());
		physicsModules.Add(new PModuleMotionDrag());
		physicsModules.Add(new PModuleKnockback());
	}

	public override void OnReceivedServerPos(bool isTeleport, ref EnumHandling handled)
	{
	}

	public new void OnReceivedClientPos(int version)
	{
		if (serverPlayer == null)
		{
			serverPlayer = entityPlayer.Player as IServerPlayer;
		}
		entity.ServerPos.SetFrom(entity.Pos);
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

	public new void HandleRemotePhysics(float dt, bool isTeleport)
	{
		if (player == null)
		{
			player = entityPlayer.Player;
		}
		if (player == null)
		{
			return;
		}
		Entity entity = base.entity;
		if (nPos == null)
		{
			nPos = new Vec3d();
			nPos.Set(entity.ServerPos);
		}
		EntityPos entityPos = lPos;
		float num = dt * 60f;
		entityPos.SetFrom(nPos);
		nPos.Set(entity.ServerPos);
		entityPos.Dimension = entity.Pos.Dimension;
		if (isTeleport)
		{
			entityPos.SetFrom(nPos);
		}
		entityPos.Motion.X = (nPos.X - entityPos.X) / (double)num;
		entityPos.Motion.Y = (nPos.Y - entityPos.Y) / (double)num;
		entityPos.Motion.Z = (nPos.Z - entityPos.Z) / (double)num;
		if (entityPos.Motion.Length() > 20.0)
		{
			entityPos.Motion.Set(0.0, 0.0, 0.0);
		}
		entity.Pos.Motion.Set(entityPos.Motion);
		entity.ServerPos.Motion.Set(entityPos.Motion);
		PhysicsBehaviorBase.collisionTester.NewTick(entityPos);
		EntityAgent entityAgent = entity as EntityAgent;
		if (entityAgent.MountedOn != null)
		{
			entity.Swimming = false;
			entity.OnGround = false;
			if (capi != null)
			{
				entity.Pos.SetPos(entityAgent.MountedOn.SeatPosition);
			}
			entity.ServerPos.Motion.X = 0.0;
			entity.ServerPos.Motion.Y = 0.0;
			entity.ServerPos.Motion.Z = 0.0;
			if (sapi != null)
			{
				PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, entityPos, num, ref newPos, 0f, 0f);
			}
			return;
		}
		entity.Pos.SetFrom(entity.ServerPos);
		SetState(entityPos, dt);
		EntityControls controls = entityAgent.Controls;
		if (!controls.NoClip)
		{
			if (sapi != null)
			{
				PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, entityPos, num, ref newPos, 0f, 0f);
			}
			RemoteMotionAndCollision(entityPos, num);
			ApplyTests(entityPos, entityAgent.Controls, dt, remote: true);
		}
		else
		{
			EntityPos serverPos = entity.ServerPos;
			serverPos.X += serverPos.Motion.X * (double)dt * 60.0;
			serverPos.Y += serverPos.Motion.Y * (double)dt * 60.0;
			serverPos.Z += serverPos.Motion.Z * (double)dt * 60.0;
			entity.Swimming = false;
			entity.FeetInLiquid = false;
			entity.OnGround = false;
			controls.Gliding = false;
		}
	}

	public override void OnPhysicsTick(float dt)
	{
		SimPhysics(dt, entity.SidedPos);
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.World is IServerWorldAccessor)
		{
			callOnEntityInside();
			entity.AfterPhysicsTick?.Invoke();
		}
	}

	public void SimPhysics(float dt, EntityPos pos)
	{
		Entity entity = base.entity;
		if (entity.State != EnumEntityState.Active)
		{
			return;
		}
		if (player == null)
		{
			player = entityPlayer.Player;
		}
		if (player == null)
		{
			return;
		}
		EntityAgent entityAgent = entity as EntityAgent;
		EntityControls controls = entityAgent.Controls;
		prevPos.Set(pos);
		tmpPos.dimension = pos.Dimension;
		SetState(pos, dt);
		SetPlayerControls(pos, controls, dt);
		if (entityAgent.MountedOn != null)
		{
			entity.Swimming = false;
			entity.OnGround = false;
			pos.SetPos(entityAgent.MountedOn.SeatPosition);
			pos.Motion.X = 0.0;
			pos.Motion.Y = 0.0;
			pos.Motion.Z = 0.0;
			return;
		}
		MotionAndCollision(pos, controls, dt);
		if (!controls.NoClip)
		{
			PhysicsBehaviorBase.collisionTester.NewTick(pos);
			if (prevDimension != pos.Dimension)
			{
				prevDimension = pos.Dimension;
				PhysicsBehaviorBase.collisionTester.PushOutFromBlocks(entity.World.BlockAccessor, entity, pos.XYZ, 0.075f);
			}
			ApplyTests(pos, controls, dt, remote: false);
			if (controls.Gliding)
			{
				if (entity.Collided || entity.FeetInLiquid || !entity.Alive || player.WorldData.FreeMove || controls.IsClimbing)
				{
					controls.GlideSpeed = 0.0;
					controls.Gliding = false;
					controls.IsFlying = false;
					entityPlayer.WalkPitch = 0f;
				}
			}
			else
			{
				controls.GlideSpeed = 0.0;
			}
		}
		else
		{
			pos.X += pos.Motion.X * (double)dt * 60.0;
			pos.Y += pos.Motion.Y * (double)dt * 60.0;
			pos.Z += pos.Motion.Z * (double)dt * 60.0;
			entity.Swimming = false;
			entity.FeetInLiquid = false;
			entity.OnGround = false;
			controls.Gliding = false;
			prevDimension = pos.Dimension;
		}
	}

	public void SetPlayerControls(EntityPos pos, EntityControls controls, float dt)
	{
		IClientWorldAccessor clientWorldAccessor = entity.World as IClientWorldAccessor;
		controls.IsFlying = player.WorldData.FreeMove || (clientWorldAccessor != null && clientWorldAccessor.Player.ClientId != player.ClientId && !controls.IsClimbing);
		controls.NoClip = player.WorldData.NoClip;
		controls.MovespeedMultiplier = player.WorldData.MoveSpeedMultiplier;
		if (controls.Gliding && !controls.IsClimbing)
		{
			controls.IsFlying = true;
		}
		if ((controls.TriesToMove || controls.Gliding) && player is IClientPlayer clientPlayer)
		{
			float yaw = pos.Yaw;
			pos.Yaw = (entity.Api as ICoreClientAPI).Input.MouseYaw;
			if (entity.Swimming || controls.Gliding)
			{
				float pitch = pos.Pitch;
				pos.Pitch = clientPlayer.CameraPitch;
				controls.CalcMovementVectors(pos, dt);
				pos.Yaw = yaw;
				pos.Pitch = pitch;
			}
			else
			{
				controls.CalcMovementVectors(pos, dt);
				pos.Yaw = yaw;
			}
			float end = (float)Math.Atan2(controls.WalkVector.X, controls.WalkVector.Z);
			float val = GameMath.AngleRadDistance(entityPlayer.WalkYaw, end);
			entityPlayer.WalkYaw += GameMath.Clamp(val, -6f * dt * GlobalConstants.OverallSpeedMultiplier, 6f * dt * GlobalConstants.OverallSpeedMultiplier);
			entityPlayer.WalkYaw = GameMath.Mod(entityPlayer.WalkYaw, (float)Math.PI * 2f);
			if (entity.Swimming || controls.Gliding)
			{
				float end2 = 0f - (float)Math.Sin(pos.Pitch);
				float val2 = GameMath.AngleRadDistance(entityPlayer.WalkPitch, end2);
				entityPlayer.WalkPitch += GameMath.Clamp(val2, -2f * dt * GlobalConstants.OverallSpeedMultiplier, 2f * dt * GlobalConstants.OverallSpeedMultiplier);
				entityPlayer.WalkPitch = GameMath.Mod(entityPlayer.WalkPitch, (float)Math.PI * 2f);
			}
			else
			{
				entityPlayer.WalkPitch = 0f;
			}
			return;
		}
		if (!entity.Swimming && !controls.Gliding)
		{
			entityPlayer.WalkPitch = 0f;
		}
		else if (entity.OnGround && entityPlayer.WalkPitch != 0f)
		{
			entityPlayer.WalkPitch = GameMath.Mod(entityPlayer.WalkPitch, (float)Math.PI * 2f);
			if (entityPlayer.WalkPitch < 0.01f || entityPlayer.WalkPitch > 3.1315928f)
			{
				entityPlayer.WalkPitch = 0f;
			}
			else
			{
				entityPlayer.WalkPitch -= GameMath.Clamp(entityPlayer.WalkPitch, 0f, 1.2f * dt * GlobalConstants.OverallSpeedMultiplier);
				if (entityPlayer.WalkPitch < 0f)
				{
					entityPlayer.WalkPitch = 0f;
				}
			}
		}
		float yaw2 = pos.Yaw;
		controls.CalcMovementVectors(pos, dt);
		pos.Yaw = yaw2;
	}

	public void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		if (capi.IsGamePaused)
		{
			return;
		}
		if (capi.World.Player.Entity != base.entity)
		{
			smoothStepping = false;
			capi.Event.UnregisterRenderer(this, EnumRenderStage.Before);
			return;
		}
		accum += dt;
		if ((double)accum > 0.5)
		{
			accum = 0f;
		}
		Entity entity = entityPlayer.MountedOn?.Entity;
		IPhysicsTickable physicsTickable = null;
		if (entityPlayer.MountedOn?.MountSupplier.Controller == entityPlayer)
		{
			physicsTickable = entity?.SidedProperties.Behaviors.Find((EntityBehavior b) => b is IPhysicsTickable) as IPhysicsTickable;
		}
		while (accum >= 1f / 60f)
		{
			OnPhysicsTick(1f / 60f);
			physicsTickable?.OnPhysicsTick(1f / 60f);
			accum -= 1f / 60f;
			currentTick++;
			if (currentTick % 4 == 0 && entityPlayer.EntityId != 0L && entityPlayer.Alive)
			{
				capi.Network.SendPlayerPositionPacket();
				if (physicsTickable != null)
				{
					capi.Network.SendPlayerMountPositionPacket(entity);
				}
			}
			AfterPhysicsTick(1f / 60f);
			physicsTickable?.AfterPhysicsTick(1f / 60f);
		}
		base.entity.PhysicsUpdateWatcher?.Invoke(accum, prevPos);
		entity?.PhysicsUpdateWatcher?.Invoke(accum, prevPos);
	}

	protected override bool HandleSteppingOnBlocks(EntityPos pos, Vec3d moveDelta, float dtFac, EntityControls controls)
	{
		if (!controls.TriesToMove || (!entity.OnGround && !entity.Swimming) || entity.Properties.Habitat == EnumHabitat.Underwater)
		{
			return false;
		}
		Cuboidd cuboidd = entity.CollisionBox.ToDouble();
		double num = 0.75 + (controls.Sprint ? 0.25 : (controls.Sneak ? 0.05 : 0.2));
		Vec2d vec2d = new Vec2d((cuboidd.X1 + cuboidd.X2) / 2.0, (cuboidd.Z1 + cuboidd.Z2) / 2.0);
		double y = Math.Max(cuboidd.Y1 + (double)StepHeight, cuboidd.Y2);
		cuboidd.Translate(pos.X, pos.Y, pos.Z);
		Vec3d vec3d = controls.WalkVector.Clone();
		Vec3d vec3d2 = vec3d.Clone().Normalize();
		double val = vec3d2.X * num;
		double val2 = vec3d2.Z * num;
		Cuboidd cuboidd2 = new Cuboidd
		{
			X1 = Math.Min(0.0, val),
			X2 = Math.Max(0.0, val),
			Z1 = Math.Min(0.0, val2),
			Z2 = Math.Max(0.0, val2),
			Y1 = (double)entity.CollisionBox.Y1 + 0.01 - ((!entity.CollidedVertically && !controls.Jump) ? 0.05 : 0.0),
			Y2 = y
		};
		cuboidd2.Translate(vec2d.X, 0.0, vec2d.Y);
		cuboidd2.Translate(pos.X, pos.Y, pos.Z);
		Vec3d vec3d3 = new Vec3d();
		Vec2d vec2d2 = new Vec2d();
		List<Cuboidd> list = FindSteppableCollisionboxSmooth(cuboidd, cuboidd2, moveDelta.Y, vec3d);
		if (list != null && list.Count > 0)
		{
			if (TryStepSmooth(controls, pos, vec2d2.Set(vec3d.X, vec3d.Z), dtFac, list, cuboidd))
			{
				return true;
			}
			Cuboidd cuboidd3 = cuboidd2.Clone();
			if (cuboidd3.Z1 == pos.Z + vec2d.Y)
			{
				cuboidd3.Z2 = cuboidd3.Z1;
			}
			else
			{
				cuboidd3.Z1 = cuboidd3.Z2;
			}
			if (TryStepSmooth(controls, pos, vec2d2.Set(vec3d.X, 0.0), dtFac, FindSteppableCollisionboxSmooth(cuboidd, cuboidd3, moveDelta.Y, vec3d3.Set(vec3d.X, vec3d.Y, 0.0)), cuboidd))
			{
				return true;
			}
			Cuboidd cuboidd4 = cuboidd2.Clone();
			if (cuboidd4.X1 == pos.X + vec2d.X)
			{
				cuboidd4.X2 = cuboidd4.X1;
			}
			else
			{
				cuboidd4.X1 = cuboidd4.X2;
			}
			if (TryStepSmooth(controls, pos, vec2d2.Set(0.0, vec3d.Z), dtFac, FindSteppableCollisionboxSmooth(cuboidd, cuboidd4, moveDelta.Y, vec3d3.Set(0.0, vec3d.Y, vec3d.Z)), cuboidd))
			{
				return true;
			}
		}
		return false;
	}

	public bool TryStepSmooth(EntityControls controls, EntityPos pos, Vec2d walkVec, float dtFac, List<Cuboidd> steppableBoxes, Cuboidd entityCollisionBox)
	{
		if (steppableBoxes == null || steppableBoxes.Count == 0)
		{
			return false;
		}
		double num = 0.03;
		Vec2d vec2d = new Vec2d(walkVec.Y, 0.0 - walkVec.X).Normalize();
		double num2 = Math.Abs(vec2d.X * 0.3) + 0.001;
		double num3 = 0.0 - num2;
		double num4 = Math.Abs(vec2d.Y * 0.3) + 0.001;
		double num5 = 0.0 - num4;
		Cuboidf entityBoxRel = new Cuboidf((float)num3, entity.CollisionBox.Y1, (float)num5, (float)num2, entity.CollisionBox.Y2, (float)num4);
		double num6 = pos.Y;
		bool flag = false;
		foreach (Cuboidd steppableBox in steppableBoxes)
		{
			double num7 = steppableBox.Y2 - entityCollisionBox.Y1 + num;
			Vec3d pos2 = new Vec3d(GameMath.Clamp(newPos.X, steppableBox.MinX, steppableBox.MaxX), newPos.Y + num7 + (double)pos.DimensionYAdjustment, GameMath.Clamp(newPos.Z, steppableBox.MinZ, steppableBox.MaxZ));
			if (!PhysicsBehaviorBase.collisionTester.IsColliding(entity.World.BlockAccessor, entityBoxRel, pos2, alsoCheckTouch: false))
			{
				double num8 = (controls.Sprint ? 0.1 : (controls.Sneak ? 0.025 : 0.05));
				num6 = (steppableBox.IntersectsOrTouches(entityCollisionBox) ? Math.Max(num6, pos.Y + num8 * (double)dtFac) : Math.Max(num6, Math.Min(pos.Y + num8 * (double)dtFac, steppableBox.Y2 - (double)entity.CollisionBox.Y1 + num)));
				flag = true;
			}
		}
		if (flag)
		{
			pos.Y = num6;
			PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, pos, dtFac, ref newPos);
		}
		return flag;
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		capi?.Event.UnregisterRenderer(this, EnumRenderStage.Before);
	}

	public void Dispose()
	{
	}
}
