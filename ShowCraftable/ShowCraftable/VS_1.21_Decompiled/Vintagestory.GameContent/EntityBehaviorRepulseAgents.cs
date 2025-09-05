using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorRepulseAgents : EntityBehavior
{
	protected Vec3d pushVector = new Vec3d();

	protected EntityPartitioning partitionUtil;

	protected bool movable = true;

	protected bool ignorePlayers;

	protected double touchdist;

	private IClientWorldAccessor cworld;

	public double ownPosRepulseX;

	public double ownPosRepulseY;

	public double ownPosRepulseZ;

	public float mySize;

	protected int dimension;

	public override bool ThreadSafe => true;

	public EntityBehaviorRepulseAgents(Entity entity)
		: base(entity)
	{
		entity.hasRepulseBehavior = true;
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		movable = attributes["movable"].AsBool(defaultValue: true);
		partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
		ignorePlayers = entity is EntityPlayer && entity.World.Config.GetAsBool("player2PlayerCollisions", defaultValue: true);
		cworld = entity.World as IClientWorldAccessor;
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		touchdist = entity.touchDistance;
		entity.BHRepulseAgents = this;
		entity.AfterPhysicsTick = AfterPhysicsTick;
	}

	public override void UpdateColSelBoxes()
	{
		touchdist = entity.touchDistance;
	}

	public void AfterPhysicsTick()
	{
		Entity obj = entity;
		EntityPos sidedPos = obj.SidedPos;
		Cuboidf collisionBox = obj.CollisionBox;
		Cuboidf originCollisionBox = obj.OriginCollisionBox;
		ownPosRepulseX = sidedPos.X + (double)(collisionBox.X2 - originCollisionBox.X2);
		ownPosRepulseY = sidedPos.Y + (double)(collisionBox.Y2 - originCollisionBox.Y2);
		ownPosRepulseZ = sidedPos.Z + (double)(collisionBox.Z2 - originCollisionBox.Z2);
	}

	public override void OnGameTick(float deltaTime)
	{
		Entity entity = base.entity;
		if (entity.State == EnumEntityState.Inactive || !entity.IsInteractable || !movable)
		{
			return;
		}
		EntityAgent entityAgent = entity as EntityAgent;
		if (entityAgent?.MountedOn != null || entity.World.ElapsedMilliseconds < 2000)
		{
			return;
		}
		Vec3d vec3d = pushVector;
		vec3d.Set(0.0, 0.0, 0.0);
		mySize = entity.SelectionBox.Length * entity.SelectionBox.Height * (float)((entityAgent == null || !entityAgent.Controls.Sneak) ? 1 : 2);
		dimension = entity.ServerPos.Dimension;
		if (cworld != null && entity != cworld.Player.Entity)
		{
			WalkEntity(cworld.Player.Entity);
		}
		else
		{
			partitionUtil.WalkEntities(ownPosRepulseX, ownPosRepulseY, ownPosRepulseZ, touchdist + partitionUtil.LargestTouchDistance + 0.1, WalkEntity, IsInRangePartition, EnumEntitySearchType.Creatures);
		}
		if (vec3d.X != 0.0 || vec3d.Z != 0.0 || vec3d.Y != 0.0)
		{
			vec3d.X = GameMath.Clamp(vec3d.X, -3.0, 3.0) / 30.0;
			vec3d.Y = GameMath.Clamp(vec3d.Y, -3.0, 0.5) / 30.0;
			vec3d.Z = GameMath.Clamp(vec3d.Z, -3.0, 3.0) / 30.0;
			if (cworld != null && entity == cworld.Player.Entity)
			{
				entity.SidedPos.Motion.Add(vec3d);
			}
			else
			{
				entity.ServerPos.Motion.Add(vec3d);
			}
		}
	}

	private bool WalkEntity(Entity e)
	{
		Entity entity = base.entity;
		if (e == entity || !(e.BHRepulseAgents is EntityBehaviorRepulseAgents entityBehaviorRepulseAgents) || !e.IsInteractable || (ignorePlayers && e is EntityPlayer))
		{
			return true;
		}
		if (e is EntityAgent entityAgent && entityAgent.MountedOn?.Entity == entity)
		{
			return true;
		}
		if (e.ServerPos.Dimension != dimension)
		{
			return true;
		}
		if (entityBehaviorRepulseAgents is ICustomRepulseBehavior customRepulseBehavior)
		{
			return customRepulseBehavior.Repulse(entity, pushVector);
		}
		double num = ownPosRepulseX - entityBehaviorRepulseAgents.ownPosRepulseX;
		double num2 = ownPosRepulseY - entityBehaviorRepulseAgents.ownPosRepulseY;
		double num3 = ownPosRepulseZ - entityBehaviorRepulseAgents.ownPosRepulseZ;
		double num4 = num * num + num2 * num2 + num3 * num3;
		double num5 = entity.touchDistanceSq + e.touchDistanceSq;
		if (num4 >= num5)
		{
			return true;
		}
		double num6 = (1.0 - num4 / num5) / (double)Math.Max(0.001f, GameMath.Sqrt(num4));
		double num7 = num * num6;
		double num8 = num2 * num6;
		double num9 = num3 * num6;
		float num10 = GameMath.Clamp(e.SelectionBox.Length * e.SelectionBox.Height / mySize, 0f, 1f);
		if (entity.OnGround)
		{
			num10 *= 3f;
		}
		pushVector.Add(num7 * (double)num10, num8 * (double)num10 * 0.75, num9 * (double)num10);
		return true;
	}

	public override string PropertyName()
	{
		return "repulseagents";
	}

	private bool IsInRangePartition(Entity e, double posX, double posY, double posZ, double radiusSq)
	{
		return true;
	}
}
