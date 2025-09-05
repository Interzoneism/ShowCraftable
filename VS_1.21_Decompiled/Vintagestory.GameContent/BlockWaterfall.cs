using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockWaterfall : BlockForFluidsLayer
{
	private float particleQuantity = 0.2f;

	private bool isBoiling;

	public static int ReplacableThreshold = 5000;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side == EnumAppSide.Client)
		{
			(api as ICoreClientAPI).Settings.Int.AddWatcher("particleLevel", OnParticleLevelChanged);
			OnParticleLevelChanged(0);
		}
		isBoiling = HasBehavior<BlockBehaviorSteaming>();
	}

	private void OnParticleLevelChanged(int newValue)
	{
		particleQuantity = 0.2f * (float)(api as ICoreClientAPI).Settings.Int["particleLevel"] / 100f;
	}

	public override float GetAmbientSoundStrength(IWorldAccessor world, BlockPos pos)
	{
		for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
		{
			BlockFacing face = BlockFacing.HORIZONTALS[i];
			if (world.BlockAccessor.GetBlockOnSide(pos, face).Replaceable >= 6000 && !world.BlockAccessor.GetBlockOnSide(pos, face, 2).IsLiquid())
			{
				return 1f;
			}
		}
		return 0f;
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
	{
		isWindAffected = true;
		if (pos.Y >= 2)
		{
			return world.BlockAccessor.GetBlockBelow(pos).Replaceable >= ReplacableThreshold;
		}
		return false;
	}

	public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		if (ParticleProperties == null || ParticleProperties.Length == 0)
		{
			return;
		}
		for (int i = 0; i < 4; i++)
		{
			if (!(api.World.Rand.NextDouble() > (double)particleQuantity))
			{
				BlockFacing blockFacing = BlockFacing.HORIZONTALS[i];
				if (!manager.BlockAccess.GetBlockOnSide(pos, blockFacing).SideSolid[blockFacing.Opposite.Index] && manager.BlockAccess.GetBlockOnSide(pos, blockFacing, 2).BlockId == 0)
				{
					AdvancedParticleProperties advancedParticleProperties = ParticleProperties[i];
					advancedParticleProperties.basePos.X = (float)pos.X + TopMiddlePos.X;
					advancedParticleProperties.basePos.Y = pos.InternalY;
					advancedParticleProperties.basePos.Z = (float)pos.Z + TopMiddlePos.Z;
					advancedParticleProperties.WindAffectednes = windAffectednessAtPos * 0.25f;
					advancedParticleProperties.HsvaColor[3].avg = 180f * Math.Min(1f, secondsTicking / 7f);
					advancedParticleProperties.Quantity.avg = 1f;
					advancedParticleProperties.Velocity[1].avg = -0.4f;
					advancedParticleProperties.Velocity[0].avg = GlobalConstants.CurrentWindSpeedClient.X * windAffectednessAtPos;
					advancedParticleProperties.Velocity[2].avg = GlobalConstants.CurrentWindSpeedClient.Z * windAffectednessAtPos;
					advancedParticleProperties.Size.avg = 0.05f;
					advancedParticleProperties.Size.var = 0f;
					advancedParticleProperties.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 0.8f);
					manager.Spawn(advancedParticleProperties);
				}
			}
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
