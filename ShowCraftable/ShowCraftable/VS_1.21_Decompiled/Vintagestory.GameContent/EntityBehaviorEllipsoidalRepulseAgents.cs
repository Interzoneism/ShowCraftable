using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorEllipsoidalRepulseAgents : EntityBehaviorRepulseAgents, ICustomRepulseBehavior
{
	protected Vec3d offset;

	protected Vec3d radius;

	public EntityBehaviorEllipsoidalRepulseAgents(Entity entity)
		: base(entity)
	{
		entity.customRepulseBehavior = true;
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		offset = attributes["offset"].AsObject(new Vec3d());
		radius = attributes["radius"].AsObject<Vec3d>();
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		touchdist = Math.Max(radius.X, radius.Z);
		entity.BHRepulseAgents = this;
		entity.AfterPhysicsTick = base.AfterPhysicsTick;
	}

	public override void UpdateColSelBoxes()
	{
		touchdist = Math.Max(radius.X, radius.Z);
	}

	public override float GetTouchDistance(ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		return (float)Math.Max(radius.X, radius.Z) + 0.5f;
	}

	public bool Repulse(Entity e, Vec3d pushVector)
	{
		if (!(e.BHRepulseAgents is EntityBehaviorRepulseAgents entityBehaviorRepulseAgents))
		{
			return true;
		}
		Entity entity = base.entity;
		Vec3d vec3d = radius;
		double num = entityBehaviorRepulseAgents.ownPosRepulseY;
		if (num > ownPosRepulseY + vec3d.Y || ownPosRepulseY > num + (double)e.SelectionBox.Height)
		{
			return true;
		}
		double num2 = ownPosRepulseX - entityBehaviorRepulseAgents.ownPosRepulseX;
		double num3 = ownPosRepulseZ - entityBehaviorRepulseAgents.ownPosRepulseZ;
		float yaw = entity.ServerPos.Yaw;
		double num4 = RelDistanceToEllipsoid(num2, num3, vec3d.X, vec3d.Z, yaw);
		if (num4 >= 1.0)
		{
			return true;
		}
		double num5 = -1.0 * (1.0 - num4);
		double num6 = num2 * num5;
		double num7 = 0.0;
		double num8 = num3 * num5;
		float num9 = entity.SelectionBox.Length * entity.SelectionBox.Height;
		float num10 = GameMath.Clamp(e.SelectionBox.Length * e.SelectionBox.Height / num9, 0f, 1f) / 1.5f;
		if (e.OnGround)
		{
			num10 *= 10f;
		}
		pushVector.Add(num6 * (double)num10, num7 * (double)num10 * 0.75, num8 * (double)num10);
		return true;
	}

	public double RelDistanceToEllipsoid(double x, double z, double wdt, double len, double yaw)
	{
		double num = x * Math.Cos(yaw) - z * Math.Sin(yaw);
		double num2 = x * Math.Sin(yaw) + z * Math.Cos(yaw);
		double num3 = num + offset.X;
		num2 += offset.Z;
		return num3 * num3 / (wdt * wdt) + num2 * num2 / (len * len);
	}
}
