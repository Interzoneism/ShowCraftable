using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskEidolonMeleeAttack : AiTaskMeleeAttack
{
	public AiTaskEidolonMeleeAttack(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		attackRange = 4.25f;
		turnToTarget = false;
	}

	protected override void attackTarget()
	{
		Vec3d xYZ = entity.Pos.XYZ;
		partitionUtil.WalkEntities(xYZ, 6.0, delegate(Entity e)
		{
			if (e.EntityId == entity.EntityId || !e.IsInteractable)
			{
				return true;
			}
			if (!e.Alive)
			{
				return true;
			}
			if (!hasDirectContact(e, minDist, minVerDist))
			{
				return true;
			}
			e.ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Entity,
				SourceEntity = entity,
				Type = damageType,
				DamageTier = damageTier,
				KnockbackStrength = knockbackStrength / 2f
			}, damage * GlobalConstants.CreatureDamageModifier);
			return true;
		}, EnumEntitySearchType.Creatures);
	}

	protected override bool hasDirectContact(Entity targetEntity, float minDist, float minVerDist)
	{
		Cuboidd cuboidd = targetEntity.SelectionBox.ToDouble().Translate(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
		tmpPos.Set(entity.ServerPos).Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		double num = cuboidd.ShortestDistanceFrom(tmpPos);
		double num2 = Math.Abs(cuboidd.ShortestVerticalDistanceFrom(tmpPos.Y));
		if (num >= (double)minDist || num2 >= (double)minVerDist)
		{
			return false;
		}
		return true;
	}
}
