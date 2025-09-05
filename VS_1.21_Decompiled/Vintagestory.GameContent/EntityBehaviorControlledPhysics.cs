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

public class EntityBehaviorControlledPhysics : PhysicsBehaviorBase, IPhysicsTickable, IRemotePhysics
{
	protected const double collisionboxReductionForInsideBlocksCheck = 0.009;

	protected bool smoothStepping;

	protected readonly List<PModule> physicsModules = new List<PModule>();

	protected readonly List<PModule> customModules = new List<PModule>();

	protected Vec3d newPos = new Vec3d();

	protected readonly Vec3d prevPos = new Vec3d();

	protected readonly BlockPos tmpPos = new BlockPos();

	protected readonly Cuboidd entityBox = new Cuboidd();

	protected readonly List<FastVec3i> traversed = new List<FastVec3i>(4);

	protected readonly List<Block> traversedBlocks = new List<Block>(4);

	protected readonly IComparer<FastVec3i> fastVec3IComparer = new FastVec3iComparer();

	protected readonly Vec3d moveDelta = new Vec3d();

	protected double prevYMotion;

	protected bool onGroundBefore;

	protected bool feetInLiquidBefore;

	protected bool swimmingBefore;

	protected float knockBackCounter;

	protected Cuboidf sneakTestCollisionbox = new Cuboidf();

	protected readonly Cuboidd steppingCollisionBox = new Cuboidd();

	protected readonly Vec3d steppingTestVec = new Vec3d();

	protected readonly Vec3d steppingTestMotion = new Vec3d();

	public Matrixf tmpModelMat = new Matrixf();

	public float StepHeight = 0.6f;

	public float stepUpSpeed = 0.07f;

	public float climbUpSpeed = 0.07f;

	public float climbDownSpeed = 0.035f;

	public Entity Entity => entity;

	public bool Ticking { get; set; }

	public override bool ThreadSafe => true;

	public void SetState(EntityPos pos, float dt)
	{
		float dtFac = dt * 60f;
		Entity entity = base.entity;
		prevPos.Set(pos);
		prevYMotion = pos.Motion.Y;
		onGroundBefore = entity.OnGround;
		feetInLiquidBefore = entity.FeetInLiquid;
		swimmingBefore = entity.Swimming;
		traversed.Clear();
		traversedBlocks.Clear();
		if (entity.AdjustCollisionBoxToAnimation)
		{
			AdjustCollisionBoxToAnimation(dtFac);
		}
	}

	public EntityBehaviorControlledPhysics(Entity entity)
		: base(entity)
	{
	}

	public virtual void SetModules()
	{
		physicsModules.Add(new PModuleWind());
		physicsModules.Add(new PModuleOnGround());
		physicsModules.Add(new PModuleInLiquid());
		physicsModules.Add(new PModuleInAir());
		physicsModules.Add(new PModuleGravity());
		physicsModules.Add(new PModuleMotionDrag());
		physicsModules.Add(new PModuleKnockback());
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		Init();
		SetProperties(properties, attributes);
		if (entity.Api is ICoreServerAPI coreServerAPI)
		{
			coreServerAPI.Server.AddPhysicsTickable(this);
		}
		entity.PhysicsUpdateWatcher?.Invoke(0f, entity.SidedPos.XYZ);
		if (entity.Api.Side != EnumAppSide.Client)
		{
			return;
		}
		EnumHandling handled = EnumHandling.Handled;
		OnReceivedServerPos(isTeleport: true, ref handled);
		entity.Attributes.RegisterModifiedListener("dmgkb", delegate
		{
			if (entity.Attributes.GetInt("dmgkb") == 1)
			{
				knockBackCounter = 2f;
			}
		});
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.World is IServerWorldAccessor)
		{
			if (mountableSupplier?.Controller is EntityPlayer { Alive: not false })
			{
				callOnEntityInside();
				entity.AfterPhysicsTick?.Invoke();
			}
		}
		else
		{
			entity.AfterPhysicsTick?.Invoke();
		}
	}

	public void SetProperties(EntityProperties properties, JsonObject attributes)
	{
		StepHeight = attributes["stepHeight"].AsFloat(0.6f);
		stepUpSpeed = attributes["stepUpSpeed"].AsFloat(0.07f);
		climbUpSpeed = attributes["climbUpSpeed"].AsFloat(0.07f);
		climbDownSpeed = attributes["climbDownSpeed"].AsFloat(0.035f);
		sneakTestCollisionbox = entity.CollisionBox.Clone().OmniNotDownGrowBy(-0.1f);
		sneakTestCollisionbox.Y2 /= 2f;
		SetModules();
		JsonObject config = properties?.Attributes?["physics"];
		for (int i = 0; i < physicsModules.Count; i++)
		{
			physicsModules[i].Initialize(config, entity);
		}
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
		lPos.SetFrom(nPos);
		nPos.Set(entity.ServerPos);
		if (isTeleport)
		{
			lPos.SetFrom(nPos);
		}
		lPos.Dimension = entity.Pos.Dimension;
		tmpPos.dimension = lPos.Dimension;
		lPos.Motion.X = (nPos.X - lPos.X) / (double)num;
		lPos.Motion.Y = (nPos.Y - lPos.Y) / (double)num;
		lPos.Motion.Z = (nPos.Z - lPos.Z) / (double)num;
		if (lPos.Motion.Length() > 20.0)
		{
			lPos.Motion.Set(0.0, 0.0, 0.0);
		}
		entity.Pos.Motion.Set(lPos.Motion);
		entity.ServerPos.Motion.Set(lPos.Motion);
		PhysicsBehaviorBase.collisionTester.NewTick(lPos);
		EntityAgent entityAgent = entity as EntityAgent;
		if (entityAgent?.MountedOn != null)
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
		}
		else
		{
			entity.Pos.SetFrom(entity.ServerPos);
			SetState(lPos, dt);
			RemoteMotionAndCollision(lPos, num);
			ApplyTests(lPos, ((EntityAgent)entity).Controls, dt, remote: true);
			if (knockBackCounter > 0f)
			{
				knockBackCounter -= dt;
				return;
			}
			knockBackCounter = 0f;
			entity.Attributes.SetInt("dmgkb", 0);
		}
	}

	public void RemoteMotionAndCollision(EntityPos pos, float dtFactor)
	{
		double num = (double)(1f / 60f * dtFactor) + Math.Max(0.0, -0.014999999664723873 * pos.Motion.Y * (double)dtFactor);
		pos.Motion.Y -= num;
		PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, pos, dtFactor, ref newPos, 0f, 0f);
		bool flag = lPos.Motion.Y < 0.0;
		entity.OnGround = entity.CollidedVertically && flag;
		pos.Motion.Y += num;
		pos.SetPos(nPos);
	}

	public void MotionAndCollision(EntityPos pos, EntityControls controls, float dt)
	{
		Entity entity = base.entity;
		foreach (PModule physicsModule in physicsModules)
		{
			if (physicsModule.Applicable(entity, pos, controls))
			{
				physicsModule.DoApply(dt, entity, pos, controls);
			}
		}
		foreach (PModule customModule in customModules)
		{
			if (customModule.Applicable(entity, pos, controls))
			{
				customModule.DoApply(dt, entity, pos, controls);
			}
		}
	}

	public void ApplyTests(EntityPos pos, EntityControls controls, float dt, bool remote)
	{
		Entity entity = base.entity;
		EntityProperties properties = entity.Properties;
		IBlockAccessor blockAccessor = entity.World.BlockAccessor;
		float num = dt * 60f;
		BlockPos blockPos = tmpPos;
		Cuboidd cuboidd = entityBox;
		Vec3d motion = pos.Motion;
		Vec3d newPosition = newPos;
		controls.IsClimbing = false;
		entity.ClimbingOnFace = null;
		entity.ClimbingIntoFace = null;
		if (properties.CanClimb)
		{
			bool flag = properties.CanClimbAnywhere && entity.Alive;
			int layer = ((!flag) ? 1 : 0);
			int num2 = (int)Math.Ceiling(entity.CollisionBox.Y2);
			cuboidd.SetAndTranslate(entity.CollisionBox, pos.X, pos.Y, pos.Z);
			blockPos.Set((int)pos.X, 0, (int)pos.Z);
			for (int i = 0; i < num2; i++)
			{
				blockPos.Y = (int)pos.Y + i;
				Block block = blockAccessor.GetBlock(blockPos, layer);
				if (!block.IsClimbable(blockPos) && !flag)
				{
					continue;
				}
				Cuboidf[] collisionBoxes = block.GetCollisionBoxes(blockAccessor, blockPos);
				if (collisionBoxes == null)
				{
					continue;
				}
				for (int j = 0; j < collisionBoxes.Length; j++)
				{
					double num3 = cuboidd.ShortestDistanceFrom(collisionBoxes[j], blockPos);
					controls.IsClimbing |= num3 < (double)properties.ClimbTouchDistance;
					if (controls.IsClimbing)
					{
						entity.ClimbingOnFace = null;
						break;
					}
				}
			}
			if (flag && controls.WalkVector.LengthSq() > 1E-05)
			{
				BlockFacing blockFacing = BlockFacing.FromVector(controls.WalkVector.X, controls.WalkVector.Y, controls.WalkVector.Z);
				if (blockFacing != null)
				{
					blockPos.Set((int)pos.X + blockFacing.Normali.X, (int)pos.Y + blockFacing.Normali.Y, (int)pos.Z + blockFacing.Normali.Z);
					Cuboidf[] collisionBoxes2 = blockAccessor.GetBlock(blockPos, 0).GetCollisionBoxes(blockAccessor, blockPos);
					entity.ClimbingIntoFace = ((collisionBoxes2 != null && collisionBoxes2.Length != 0) ? blockFacing : null);
				}
			}
			if (!controls.IsClimbing)
			{
				float climbTouchDistance = properties.ClimbTouchDistance;
				int num4 = (int)pos.Y;
				for (int k = 0; k < 4; k++)
				{
					blockPos.IterateHorizontalOffsets(k);
					Cuboidf[] collisionBoxes3;
					int num5;
					for (int l = 0; l < num2; l++)
					{
						blockPos.Y = num4 + l;
						Block block2 = blockAccessor.GetBlock(blockPos, layer);
						if (!block2.IsClimbable(blockPos) && !flag)
						{
							continue;
						}
						collisionBoxes3 = block2.GetCollisionBoxes(blockAccessor, blockPos);
						if (collisionBoxes3 == null)
						{
							continue;
						}
						num5 = 0;
						while (num5 < collisionBoxes3.Length)
						{
							if (!(cuboidd.ShortestDistanceFrom(collisionBoxes3[num5], blockPos) < (double)climbTouchDistance))
							{
								num5++;
								continue;
							}
							goto IL_02a1;
						}
					}
					continue;
					IL_02a1:
					controls.IsClimbing = true;
					entity.ClimbingOnFace = BlockFacing.HORIZONTALS[k];
					entity.ClimbingOnCollBox = collisionBoxes3[num5];
					break;
				}
			}
		}
		if (!remote)
		{
			if (controls.IsClimbing && controls.WalkVector.Y == 0.0)
			{
				motion.Y = (controls.Sneak ? Math.Max(0f - climbUpSpeed, motion.Y - (double)climbUpSpeed) : motion.Y);
				if (controls.Jump)
				{
					motion.Y = climbDownSpeed * dt * 60f;
				}
			}
			double num6 = motion.X * (double)num + pos.X;
			double num7 = motion.Y * (double)num + pos.Y;
			double num8 = motion.Z * (double)num + pos.Z;
			moveDelta.Set(motion.X * (double)num, prevYMotion * (double)num, motion.Z * (double)num);
			PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, pos, num, ref newPosition, 0f, CollisionYExtra);
			if (!properties.CanClimbAnywhere)
			{
				controls.IsStepping = HandleSteppingOnBlocks(pos, moveDelta, num, controls);
			}
			HandleSneaking(pos, controls, dt);
			int num9 = (int)pos.X;
			int num10 = (int)pos.Y;
			int num11 = (int)pos.Z;
			if (entity.CollidedHorizontally && !controls.IsClimbing && !controls.IsStepping && properties.Habitat != EnumHabitat.Underwater)
			{
				if (blockAccessor.GetBlockRaw(num9, (int)(pos.InternalY + 0.5), num11, 2).LiquidLevel >= 7 || blockAccessor.GetBlockRaw(num9, (int)pos.InternalY, num11, 2).LiquidLevel >= 7 || blockAccessor.GetBlockRaw(num9, (int)(pos.InternalY - 0.05), num11, 2).LiquidLevel >= 7)
				{
					motion.Y += 0.2 * (double)dt;
					controls.IsStepping = true;
				}
				else
				{
					double num12 = Math.Abs(motion.X);
					double num13 = Math.Abs(motion.Z);
					if (num12 > num13)
					{
						if (num13 < 0.001)
						{
							motion.Z += ((motion.Z < 0.0) ? (-0.0025) : 0.0025);
						}
					}
					else if (num12 < 0.001)
					{
						motion.X += ((motion.X < 0.0) ? (-0.0025) : 0.0025);
					}
				}
			}
			float num14 = entity.CollisionBox.Width / 2f;
			if (blockAccessor.IsNotTraversable((int)(num6 + (double)(num14 * (float)Math.Sign(motion.X))), num10, num11, pos.Dimension))
			{
				newPosition.X = pos.X;
			}
			if (blockAccessor.IsNotTraversable(num9, (int)num7, num11, pos.Dimension))
			{
				newPosition.Y = pos.Y;
			}
			if (blockAccessor.IsNotTraversable(num9, num10, (int)(num8 + (double)(num14 * (float)Math.Sign(motion.Z))), pos.Dimension))
			{
				newPosition.Z = pos.Z;
			}
			pos.SetPos(newPosition);
			if ((num6 < newPosition.X && motion.X < 0.0) || (num6 > newPosition.X && motion.X > 0.0))
			{
				motion.X = 0.0;
			}
			if ((num7 < newPosition.Y && motion.Y < 0.0) || (num7 > newPosition.Y && motion.Y > 0.0))
			{
				motion.Y = 0.0;
			}
			if ((num8 < newPosition.Z && motion.Z < 0.0) || (num8 > newPosition.Z && motion.Z > 0.0))
			{
				motion.Z = 0.0;
			}
		}
		bool flag2 = prevYMotion <= 0.0;
		entity.OnGround = entity.CollidedVertically && flag2;
		float num15 = entity.CollisionBox.X2 - entity.OriginCollisionBox.X2;
		float num16 = entity.CollisionBox.Z2 - entity.OriginCollisionBox.Z2;
		int x = (int)(pos.X + (double)num15);
		int num17 = (int)pos.InternalY;
		int z = (int)(pos.Z + (double)num16);
		int num18 = (int)(pos.InternalY + entity.SwimmingOffsetY);
		Block blockRaw = blockAccessor.GetBlockRaw(x, num17, z, 2);
		Block block3 = ((num18 == num17) ? blockRaw : blockAccessor.GetBlockRaw(x, num18, z, 2));
		entity.Swimming = block3.IsLiquid();
		entity.OnGround = (entity.CollidedVertically && flag2 && !controls.IsClimbing) || controls.IsStepping;
		if (blockRaw.IsLiquid())
		{
			Block blockRaw2 = blockAccessor.GetBlockRaw(x, num17 + 1, z, 2);
			entity.FeetInLiquid = (double)((float)(blockRaw.LiquidLevel + ((blockRaw2.LiquidLevel > 0) ? 1 : 0)) / 8f) >= pos.Y - (double)(int)pos.Y;
			entity.InLava = blockRaw.LiquidCode == "lava";
			if (!feetInLiquidBefore && entity.FeetInLiquid && (!entity.IsFirstTick() || prevPos.LengthSq() != 0.0))
			{
				entity.OnCollideWithLiquid();
			}
		}
		else
		{
			entity.FeetInLiquid = false;
			entity.InLava = false;
		}
		if (!onGroundBefore && entity.OnGround)
		{
			entity.OnFallToGround(prevYMotion);
		}
		if ((swimmingBefore || feetInLiquidBefore) && !entity.Swimming && !entity.FeetInLiquid)
		{
			entity.OnExitedLiquid();
		}
		if (!flag2 || entity.OnGround || controls.IsClimbing)
		{
			entity.PositionBeforeFalling.Set(pos);
		}
		Cuboidd cuboidd2 = PhysicsBehaviorBase.collisionTester.entityBox;
		int num19 = (int)(cuboidd2.X2 - 0.009);
		int num20 = (int)(cuboidd2.Y2 - 0.009);
		int num21 = (int)(cuboidd2.Z2 - 0.009);
		int num22 = (int)(cuboidd2.X1 + 0.009);
		int num23 = (int)(cuboidd2.Z1 + 0.009);
		for (int m = (int)(cuboidd2.Y1 + 0.009); m <= num20; m++)
		{
			for (int n = num22; n <= num19; n++)
			{
				for (int num24 = num23; num24 <= num21; num24++)
				{
					blockPos.Set(n, m, num24);
					Block block4 = blockAccessor.GetBlock(blockPos);
					if (block4.Id != 0)
					{
						FastVec3i item = new FastVec3i(n, m, num24);
						int num25 = traversed.BinarySearch(item, fastVec3IComparer);
						if (num25 < 0)
						{
							num25 = ~num25;
						}
						traversed.Insert(num25, item);
						traversedBlocks.Insert(num25, block4);
					}
				}
			}
		}
		entity.PhysicsUpdateWatcher?.Invoke(0f, prevPos);
	}

	public virtual void OnPhysicsTick(float dt)
	{
		Entity entity = base.entity;
		if (entity.State != EnumEntityState.Active)
		{
			return;
		}
		EntityPos sidedPos = entity.SidedPos;
		PhysicsBehaviorBase.collisionTester.AssignToEntity(this, sidedPos.Dimension);
		EntityControls controls = ((EntityAgent)entity).Controls;
		EntityAgent entityAgent = entity as EntityAgent;
		if (entityAgent?.MountedOn != null)
		{
			AdjustMountedPositionFor(entityAgent);
			return;
		}
		SetState(sidedPos, dt);
		MotionAndCollision(sidedPos, controls, dt);
		ApplyTests(sidedPos, controls, dt, remote: false);
		if (entity.World.Side == EnumAppSide.Server)
		{
			entity.Pos.SetFrom(entity.ServerPos);
		}
		IMountable mountable = mountableSupplier;
		if (mountable == null)
		{
			return;
		}
		IMountableSeat[] seats = mountable.Seats;
		for (int i = 0; i < seats.Length; i++)
		{
			if (seats[i]?.Passenger is EntityAgent { MountedOn: not null } entityAgent2)
			{
				AdjustMountedPositionFor(entityAgent2);
			}
		}
	}

	private void AdjustMountedPositionFor(EntityAgent entity)
	{
		entity.Swimming = false;
		entity.OnGround = false;
		EntityPos sidedPos = entity.SidedPos;
		if (!(entity is EntityPlayer))
		{
			sidedPos.SetFrom(entity.MountedOn.SeatPosition);
		}
		else
		{
			sidedPos.SetPos(entity.MountedOn.SeatPosition);
		}
		sidedPos.Motion.X = 0.0;
		sidedPos.Motion.Y = 0.0;
		sidedPos.Motion.Z = 0.0;
	}

	public virtual void AfterPhysicsTick(float dt)
	{
		Entity entity = base.entity;
		if (entity.State == EnumEntityState.Active && (mountableSupplier == null || capi != null || !mountableSupplier.IsBeingControlled()))
		{
			BlockPos blockPos = tmpPos;
			List<Block> list = traversedBlocks;
			List<FastVec3i> list2 = traversed;
			for (int i = 0; i < list.Count; i++)
			{
				blockPos.Set(list2[i]);
				list[i].OnEntityInside(entity.World, entity, blockPos);
			}
			entity.AfterPhysicsTick?.Invoke();
		}
	}

	public void HandleSneaking(EntityPos pos, EntityControls controls, float dt)
	{
		if (!controls.Sneak || !entity.OnGround || pos.Motion.Y > 0.0)
		{
			return;
		}
		Vec3d vec3d = new Vec3d();
		vec3d.Set(pos.X, pos.InternalY - (double)(GlobalConstants.GravityPerSecond * dt), pos.Z);
		if (!PhysicsBehaviorBase.collisionTester.IsColliding(entity.World.BlockAccessor, sneakTestCollisionbox, vec3d))
		{
			return;
		}
		tmpPos.Set((int)pos.X, (int)pos.Y - 1, (int)pos.Z);
		Block block = entity.World.BlockAccessor.GetBlock(tmpPos);
		vec3d.Set(newPos.X, newPos.Y - (double)(GlobalConstants.GravityPerSecond * dt) + (double)pos.DimensionYAdjustment, pos.Z);
		if (!PhysicsBehaviorBase.collisionTester.IsColliding(entity.World.BlockAccessor, sneakTestCollisionbox, vec3d))
		{
			if (block.IsClimbable(tmpPos))
			{
				newPos.X += (pos.X - newPos.X) / 10.0;
			}
			else
			{
				newPos.X = pos.X;
			}
		}
		vec3d.Set(pos.X, newPos.Y - (double)(GlobalConstants.GravityPerSecond * dt) + (double)pos.DimensionYAdjustment, newPos.Z);
		if (!PhysicsBehaviorBase.collisionTester.IsColliding(entity.World.BlockAccessor, sneakTestCollisionbox, vec3d))
		{
			if (block.IsClimbable(tmpPos))
			{
				newPos.Z += (pos.Z - newPos.Z) / 10.0;
			}
			else
			{
				newPos.Z = pos.Z;
			}
		}
	}

	protected virtual bool HandleSteppingOnBlocks(EntityPos pos, Vec3d moveDelta, float dtFac, EntityControls controls)
	{
		if (controls.WalkVector.X == 0.0 && controls.WalkVector.Z == 0.0)
		{
			return false;
		}
		if ((!entity.OnGround && !entity.Swimming) || entity.Properties.Habitat == EnumHabitat.Underwater)
		{
			return false;
		}
		steppingCollisionBox.SetAndTranslate(entity.CollisionBox, pos.X, pos.Y, pos.Z);
		steppingCollisionBox.Y2 = Math.Max(steppingCollisionBox.Y1 + (double)StepHeight, steppingCollisionBox.Y2);
		Vec3d walkVector = controls.WalkVector;
		Cuboidd cuboidd = FindSteppableCollisionBox(steppingCollisionBox, moveDelta.Y, walkVector);
		if (cuboidd != null)
		{
			Vec3d vec3d = steppingTestMotion;
			vec3d.Set(moveDelta.X, moveDelta.Y, moveDelta.Z);
			if (TryStep(pos, vec3d, dtFac, cuboidd, steppingCollisionBox))
			{
				return true;
			}
			Vec3d vec3d2 = steppingTestVec;
			vec3d.Z = 0.0;
			if (TryStep(pos, vec3d, dtFac, FindSteppableCollisionBox(steppingCollisionBox, moveDelta.Y, vec3d2.Set(walkVector.X, walkVector.Y, 0.0)), steppingCollisionBox))
			{
				return true;
			}
			vec3d.Set(0.0, moveDelta.Y, moveDelta.Z);
			if (TryStep(pos, vec3d, dtFac, FindSteppableCollisionBox(steppingCollisionBox, moveDelta.Y, vec3d2.Set(0.0, walkVector.Y, walkVector.Z)), steppingCollisionBox))
			{
				return true;
			}
			return false;
		}
		return false;
	}

	public bool TryStep(EntityPos pos, Vec3d moveDelta, float dtFac, Cuboidd steppableBox, Cuboidd entityCollisionBox)
	{
		if (steppableBox == null)
		{
			return false;
		}
		double y = steppableBox.Y2 - entityCollisionBox.Y1 + 0.03;
		Vec3d pos2 = newPos.OffsetCopy(moveDelta.X, y, moveDelta.Z);
		if (!PhysicsBehaviorBase.collisionTester.IsColliding(entity.World.BlockAccessor, entity.CollisionBox, pos2, alsoCheckTouch: false))
		{
			pos.Y += stepUpSpeed * dtFac;
			PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, pos, dtFac, ref newPos);
			return true;
		}
		return false;
	}

	public static bool GetCollidingCollisionBox(IBlockAccessor blockAccessor, Cuboidf entityBoxRel, Vec3d pos, out CachedCuboidList blocks, BlockPos tmpPos, bool alsoCheckTouch = true)
	{
		blocks = new CachedCuboidList();
		Vec3d vec3d = new Vec3d();
		Cuboidd cuboidd = entityBoxRel.ToDouble().Translate(pos);
		int num = (int)((double)entityBoxRel.MinX + pos.X);
		int num2 = (int)((double)entityBoxRel.MinY + pos.Y - 1.0);
		int num3 = (int)((double)entityBoxRel.MinZ + pos.Z);
		int num4 = (int)Math.Ceiling((double)entityBoxRel.MaxX + pos.X);
		int num5 = (int)Math.Ceiling((double)entityBoxRel.MaxY + pos.Y);
		int num6 = (int)Math.Ceiling((double)entityBoxRel.MaxZ + pos.Z);
		for (int i = num2; i <= num5; i++)
		{
			for (int j = num; j <= num4; j++)
			{
				for (int k = num3; k <= num6; k++)
				{
					tmpPos.Set(j, i, k);
					Block block = blockAccessor.GetBlock(tmpPos);
					vec3d.Set(j, i, k);
					Cuboidf[] collisionBoxes = block.GetCollisionBoxes(blockAccessor, tmpPos);
					if (collisionBoxes == null)
					{
						continue;
					}
					foreach (Cuboidf cuboidf in collisionBoxes)
					{
						if (cuboidf != null && (alsoCheckTouch ? cuboidd.IntersectsOrTouches(cuboidf, vec3d) : cuboidd.Intersects(cuboidf, vec3d)))
						{
							blocks.Add(cuboidf, j, tmpPos.InternalY, k, block);
						}
					}
				}
			}
		}
		return blocks.Count > 0;
	}

	public Cuboidd FindSteppableCollisionBox(Cuboidd entityCollisionBox, double motionY, Vec3d walkVector)
	{
		Cuboidd cuboidd = null;
		CachedCuboidListFaster collisionBoxList = PhysicsBehaviorBase.collisionTester.CollisionBoxList;
		int count = collisionBoxList.Count;
		BlockPos blockPos = new BlockPos(entity.ServerPos.Dimension);
		for (int i = 0; i < count; i++)
		{
			Block block = collisionBoxList.blocks[i];
			if (block.CollisionBoxes != null && !block.CanStep && entity.CollisionBox.Height < 5f * block.CollisionBoxes[0].Height)
			{
				continue;
			}
			blockPos.Set(collisionBoxList.positions[i]);
			if (!block.SideIsSolid(blockPos, 4))
			{
				blockPos.Down();
				Block mostSolidBlock = entity.World.BlockAccessor.GetMostSolidBlock(blockPos);
				blockPos.Up();
				if (mostSolidBlock.CollisionBoxes != null && !mostSolidBlock.CanStep && entity.CollisionBox.Height < 5f * mostSolidBlock.CollisionBoxes[0].Height)
				{
					continue;
				}
			}
			Cuboidd cuboidd2 = collisionBoxList.cuboids[i];
			EnumIntersect enumIntersect = CollisionTester.AabbIntersect(cuboidd2, entityCollisionBox, walkVector);
			if (enumIntersect != EnumIntersect.NoIntersect)
			{
				if ((enumIntersect == EnumIntersect.Stuck && !block.AllowStepWhenStuck) || (enumIntersect == EnumIntersect.IntersectY && motionY > 0.0))
				{
					return null;
				}
				double num = cuboidd2.Y2 - entityCollisionBox.Y1;
				if (!(num <= 0.0) && num <= (double)StepHeight && (cuboidd == null || cuboidd.Y2 < cuboidd2.Y2))
				{
					cuboidd = cuboidd2;
				}
			}
		}
		return cuboidd;
	}

	public List<Cuboidd> FindSteppableCollisionboxSmooth(Cuboidd entityCollisionBox, Cuboidd entitySensorBox, double motionY, Vec3d walkVector)
	{
		List<Cuboidd> list = new List<Cuboidd>();
		GetCollidingCollisionBox(entity.World.BlockAccessor, entitySensorBox.ToFloat(), new Vec3d(), out var blocks, tmpPos);
		for (int i = 0; i < blocks.Count; i++)
		{
			Cuboidd cuboidd = blocks.cuboids[i];
			Block block = blocks.blocks[i];
			if (!block.CanStep && block.CollisionBoxes != null && entity.CollisionBox.Height < 5f * block.CollisionBoxes[0].Height)
			{
				continue;
			}
			BlockPos blockPos = blocks.positions[i];
			if (!block.SideIsSolid(blockPos, 4))
			{
				blockPos.Down();
				Block mostSolidBlock = entity.World.BlockAccessor.GetMostSolidBlock(blockPos);
				blockPos.Up();
				if (!mostSolidBlock.CanStep && mostSolidBlock.CollisionBoxes != null && entity.CollisionBox.Height < 5f * mostSolidBlock.CollisionBoxes[0].Height)
				{
					continue;
				}
			}
			EnumIntersect enumIntersect = CollisionTester.AabbIntersect(cuboidd, entityCollisionBox, walkVector);
			if ((enumIntersect == EnumIntersect.Stuck && !block.AllowStepWhenStuck) || (enumIntersect == EnumIntersect.IntersectY && motionY > 0.0))
			{
				return null;
			}
			double num = cuboidd.Y2 - entityCollisionBox.Y1;
			if (!(num <= (entity.CollidedVertically ? 0.0 : (-0.05))) && num <= (double)StepHeight)
			{
				list.Add(cuboidd);
			}
		}
		return list;
	}

	public void AdjustCollisionBoxToAnimation(float dtFac)
	{
		AttachmentPointAndPose attachmentPointAndPose = entity.AnimManager.Animator?.GetAttachmentPointPose("Center");
		if (attachmentPointAndPose != null)
		{
			float[] vec = new float[4] { 0f, 0f, 0f, 1f };
			AttachmentPoint attachPoint = attachmentPointAndPose.AttachPoint;
			CompositeShape shape = entity.Properties.Client.Shape;
			float num = shape?.rotateX ?? 0f;
			float num2 = shape?.rotateY ?? 0f;
			float num3 = shape?.rotateZ ?? 0f;
			float[] array = Mat4f.Create();
			Mat4f.Identity(array);
			Mat4f.Translate(array, array, 0f, entity.CollisionBox.Y2 / 2f, 0f);
			double[] array2 = Quaterniond.Create();
			Quaterniond.RotateX(array2, array2, entity.SidedPos.Pitch + num * ((float)Math.PI / 180f));
			Quaterniond.RotateY(array2, array2, entity.SidedPos.Yaw + (num2 + 90f) * ((float)Math.PI / 180f));
			Quaterniond.RotateZ(array2, array2, entity.SidedPos.Roll + num3 * ((float)Math.PI / 180f));
			float[] array3 = new float[array2.Length];
			for (int i = 0; i < array2.Length; i++)
			{
				array3[i] = (float)array2[i];
			}
			Mat4f.Mul(array, array, Mat4f.FromQuat(Mat4f.Create(), array3));
			float size = entity.Properties.Client.Size;
			Mat4f.Translate(array, array, 0f, (0f - entity.CollisionBox.Y2) / 2f, 0f);
			Mat4f.Scale(array, array, new float[3] { size, size, size });
			Mat4f.Translate(array, array, -0.5f, 0f, -0.5f);
			tmpModelMat.Set(array).Mul(attachmentPointAndPose.AnimModelMatrix).Translate(attachPoint.PosX / 16.0, attachPoint.PosY / 16.0, attachPoint.PosZ / 16.0);
			EntityPos sidedPos = entity.SidedPos;
			float[] array4 = Mat4f.MulWithVec4(tmpModelMat.Values, vec);
			float num4 = array4[0] - (entity.CollisionBox.X1 - entity.OriginCollisionBox.X1);
			float num5 = array4[2] - (entity.CollisionBox.Z1 - entity.OriginCollisionBox.Z1);
			if ((double)Math.Abs(num4) > 1E-05 || (double)Math.Abs(num5) > 1E-05)
			{
				EntityPos entityPos = sidedPos.Copy();
				entityPos.Motion.X = num4;
				entityPos.Motion.Z = num5;
				moveDelta.Set(entityPos.Motion.X, entityPos.Motion.Y, entityPos.Motion.Z);
				PhysicsBehaviorBase.collisionTester.ApplyTerrainCollision(entity, entityPos, dtFac, ref newPos);
				double x = (newPos.X - sidedPos.X) / (double)dtFac - (double)num4;
				double z = (newPos.Z - sidedPos.Z) / (double)dtFac - (double)num5;
				sidedPos.Motion.X = x;
				sidedPos.Motion.Z = z;
				entity.CollisionBox.Set(entity.OriginCollisionBox);
				entity.CollisionBox.Translate(array4[0], 0f, array4[2]);
				entity.SelectionBox.Set(entity.OriginSelectionBox);
				entity.SelectionBox.Translate(array4[0], 0f, array4[2]);
			}
		}
	}

	protected void callOnEntityInside()
	{
		EntityPos serverPos = entity.ServerPos;
		IWorldAccessor world = entity.World;
		Cuboidd cuboidd = PhysicsBehaviorBase.collisionTester.entityBox;
		cuboidd.SetAndTranslate(entity.CollisionBox, serverPos.X, serverPos.Y, serverPos.Z);
		cuboidd.RemoveRoundingErrors();
		BlockPos minPos = new BlockPos((int)cuboidd.X1, (int)cuboidd.Y1, (int)cuboidd.Z1, serverPos.Dimension);
		BlockPos maxPos = new BlockPos((int)cuboidd.X2, (int)cuboidd.Y2, (int)cuboidd.Z2, serverPos.Dimension);
		world.BlockAccessor.WalkBlocks(minPos, maxPos, delegate(Block block, int x, int y, int z)
		{
			if (block.Id != 0)
			{
				minPos.Set(x, y, z);
				block.OnEntityInside(world, entity, minPos);
			}
		});
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
		return "entitycontrolledphysics";
	}
}
