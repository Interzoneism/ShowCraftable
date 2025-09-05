using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorFloatUpWhenStuck : EntityBehavior
{
	private bool onlyWhenDead;

	private int counter;

	private bool stuckInBlock;

	private float pushVelocityMul = 1f;

	private Vec3d tmpPos = new Vec3d();

	public EntityBehaviorFloatUpWhenStuck(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		onlyWhenDead = attributes["onlyWhenDead"].AsBool();
		pushVelocityMul = attributes["pushVelocityMul"].AsFloat(1f);
		counter = (int)entity.EntityId / 10 % 10;
	}

	public override void OnTesselated()
	{
		base.OnTesselated();
		ensureCenterAPExists();
	}

	private void ensureCenterAPExists()
	{
		if (entity.AnimManager != null && entity.World.Side == EnumAppSide.Client && entity.AnimManager.Animator?.GetAttachmentPointPose("Center") == null)
		{
			HashSet<AssetLocation> orCreate = ObjectCacheUtil.GetOrCreate(entity.Api, "missingCenterApEntityCodes", () => new HashSet<AssetLocation>());
			if (!orCreate.Contains(entity.Code))
			{
				orCreate.Add(entity.Code);
				entity.World.Logger.Warning(string.Concat("Entity ", entity.Code, " with shape ", entity.Properties.Client.Shape?.ToString(), " seems to be missing attachment point center but also has the FloatUpWhenStuck behavior - it might not work correctly with the center point lacking"));
			}
		}
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.World.ElapsedMilliseconds < 2000 || entity.World.Side == EnumAppSide.Client || (counter++ <= 10 && (!stuckInBlock || counter <= 1)))
		{
			return;
		}
		counter = 0;
		if ((onlyWhenDead && entity.Alive) || entity.Properties.CanClimbAnywhere)
		{
			return;
		}
		stuckInBlock = false;
		entity.Properties.Habitat = EnumHabitat.Land;
		if (!entity.Swimming)
		{
			tmpPos.Set(entity.SidedPos.X, entity.SidedPos.Y, entity.SidedPos.Z);
			Cuboidd collidingCollisionBox = entity.World.CollisionTester.GetCollidingCollisionBox(entity.World.BlockAccessor, entity.CollisionBox.Clone().ShrinkBy(0.01f), tmpPos, alsoCheckTouch: false);
			if (collidingCollisionBox != null)
			{
				PushoutOfCollisionbox(deltaTime, collidingCollisionBox);
				stuckInBlock = true;
			}
		}
	}

	private void PushoutOfCollisionbox(float dt, Cuboidd collBox)
	{
		double x = entity.SidedPos.X;
		double y = entity.SidedPos.Y;
		double z = entity.SidedPos.Z;
		IBlockAccessor blockAccessor = entity.World.BlockAccessor;
		Vec3i vec3i = null;
		double num = 99.0;
		for (int i = 0; i < Cardinal.ALL.Length; i++)
		{
			if (num <= 0.25)
			{
				break;
			}
			Cardinal cardinal = Cardinal.ALL[i];
			for (int j = 1; j <= 4; j++)
			{
				float num2 = (float)j / 4f;
				if (!entity.World.CollisionTester.IsColliding(blockAccessor, entity.CollisionBox, tmpPos.Set(x + (double)((float)cardinal.Normali.X * num2), y, z + (double)((float)cardinal.Normali.Z * num2)), alsoCheckTouch: false) && (double)num2 < num)
				{
					num = num2 + (cardinal.IsDiagnoal ? 0.1f : 0f);
					vec3i = cardinal.Normali;
					break;
				}
			}
		}
		if (vec3i == null)
		{
			vec3i = BlockFacing.UP.Normali;
		}
		dt = Math.Min(dt, 0.1f);
		float num3 = ((float)entity.World.Rand.NextDouble() - 0.5f) / 600f;
		float num4 = ((float)entity.World.Rand.NextDouble() - 0.5f) / 600f;
		entity.SidedPos.X += (float)vec3i.X * dt * 0.4f;
		entity.SidedPos.Y += (float)vec3i.Y * dt * 0.4f;
		entity.SidedPos.Z += (float)vec3i.Z * dt * 0.4f;
		entity.SidedPos.Motion.X = pushVelocityMul * (float)vec3i.X * dt + num3;
		entity.SidedPos.Motion.Y = pushVelocityMul * (float)vec3i.Y * dt * 2f;
		entity.SidedPos.Motion.Z = pushVelocityMul * (float)vec3i.Z * dt + num4;
		entity.Properties.Habitat = EnumHabitat.Air;
	}

	public override string PropertyName()
	{
		return "floatupwhenstuck";
	}
}
