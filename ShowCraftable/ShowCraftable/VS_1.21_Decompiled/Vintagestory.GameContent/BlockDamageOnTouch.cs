using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockDamageOnTouch : Block
{
	protected float sprintIntoDamage = 1f;

	protected float fallIntoDamageMul = 30f;

	protected HashSet<AssetLocation> immuneCreatures = new HashSet<AssetLocation>();

	protected EnumDamageType damageType = EnumDamageType.PiercingAttack;

	protected int damageTier;

	protected double collisionSpeedThreshold = 0.3;

	protected double onEntityInsideDamageProbability = 0.2;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		sprintIntoDamage = Attributes["sprintIntoDamage"].AsFloat(1f);
		fallIntoDamageMul = Attributes["fallIntoDamageMul"].AsFloat(15f);
		immuneCreatures = new HashSet<AssetLocation>(Attributes["immuneCreatures"].AsObject(Array.Empty<AssetLocation>(), Code.Domain));
		damageType = Enum.Parse<EnumDamageType>(Attributes["damageType"].AsString("PiercingAttack"));
		damageTier = Attributes["damageTier"].AsInt();
		collisionSpeedThreshold = Attributes["collisionSpeedThreshold"].AsFloat(0.3f);
		onEntityInsideDamageProbability = Attributes["onEntityInsideDamageProbability"].AsFloat(0.2f);
	}

	public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
	{
		if (world.Side == EnumAppSide.Server && entity is EntityAgent && (entity as EntityAgent).ServerControls.Sprint && entity.ServerPos.Motion.LengthSq() > 0.001)
		{
			if (immuneCreatures.Contains(entity.Code))
			{
				return;
			}
			if (world.Rand.NextDouble() < onEntityInsideDamageProbability)
			{
				entity.ReceiveDamage(new DamageSource
				{
					Source = EnumDamageSource.Block,
					SourceBlock = this,
					Type = EnumDamageType.PiercingAttack,
					SourcePos = pos.ToVec3d()
				}, sprintIntoDamage);
				entity.ServerPos.Motion.Set(0.0, 0.0, 0.0);
			}
		}
		base.OnEntityInside(world, entity, pos);
	}

	public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		if (world.Side == EnumAppSide.Server && isImpact && 0.0 - collideSpeed.Y >= collisionSpeedThreshold && !immuneCreatures.Contains(entity.Code))
		{
			entity.ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Block,
				SourceBlock = this,
				Type = damageType,
				DamageTier = damageTier,
				SourcePos = pos.ToVec3d()
			}, (float)Math.Abs(collideSpeed.Y * (double)fallIntoDamageMul));
		}
	}
}
