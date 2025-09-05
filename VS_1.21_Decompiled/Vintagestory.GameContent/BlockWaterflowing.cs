using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockWaterflowing : BlockForFluidsLayer
{
	private float particleQuantity = 0.2f;

	private bool isBoiling;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side == EnumAppSide.Client)
		{
			(api as ICoreClientAPI).Settings.Int.AddWatcher("particleLevel", OnParticelLevelChanged);
			OnParticelLevelChanged(0);
		}
		ParticleProperties[0].SwimOnLiquid = true;
		isBoiling = HasBehavior<BlockBehaviorSteaming>();
	}

	private void OnParticelLevelChanged(int newValue)
	{
		particleQuantity = 0.4f * (float)(api as ICoreClientAPI).Settings.Int["particleLevel"] / 100f;
	}

	public override float GetAmbientSoundStrength(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockAbove(pos).Replaceable >= 6000 && !world.BlockAccessor.GetBlockAbove(pos, 1, 2).IsLiquid())
		{
			return 1f;
		}
		return 0f;
	}

	public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			blockBehaviors[i].OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
		}
		if (!(api.World.Rand.NextDouble() > (double)particleQuantity))
		{
			AdvancedParticleProperties advancedParticleProperties = ParticleProperties[0];
			advancedParticleProperties.basePos.X = pos.X;
			advancedParticleProperties.basePos.Y = pos.InternalY;
			advancedParticleProperties.basePos.Z = pos.Z;
			advancedParticleProperties.Velocity[0].avg = (float)base.PushVector.X * 500f;
			advancedParticleProperties.Velocity[1].avg = (float)base.PushVector.Y * 1000f;
			advancedParticleProperties.Velocity[2].avg = (float)base.PushVector.Z * 500f;
			advancedParticleProperties.GravityEffect.avg = 0.5f;
			advancedParticleProperties.HsvaColor[3].avg = 180f * Math.Min(1f, secondsTicking / 7f);
			advancedParticleProperties.Quantity.avg = 1f;
			advancedParticleProperties.PosOffset[1].avg = 0.125f;
			advancedParticleProperties.PosOffset[1].var = (float)LiquidLevel / 8f * 0.75f;
			advancedParticleProperties.SwimOnLiquid = true;
			advancedParticleProperties.Size.avg = 0.05f;
			advancedParticleProperties.Size.var = 0f;
			advancedParticleProperties.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 0.8f);
			manager.Spawn(advancedParticleProperties);
		}
	}

	public override float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
	{
		if (creatureType == EnumAICreatureType.SeaCreature && !isBoiling)
		{
			return 0f;
		}
		if (!isBoiling || creatureType == EnumAICreatureType.HeatProofCreature)
		{
			return 5f;
		}
		return 99999f;
	}
}
