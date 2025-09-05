using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockLava : BlockForFluidsLayer, IBlockFlowing
{
	private class FireLocation
	{
		public readonly BlockPos firePos;

		public readonly BlockFacing facing;

		public FireLocation(BlockPos firePos, BlockFacing facing)
		{
			this.firePos = firePos;
			this.facing = facing;
		}
	}

	private readonly int temperature = 1200;

	private readonly int tempLossPerMeter = 100;

	private Block blockFire;

	private AdvancedParticleProperties[] fireParticles;

	public string Flow { get; set; }

	public Vec3i FlowNormali { get; set; }

	public bool IsLava => true;

	public int Height { get; set; }

	public BlockLava()
	{
		if (Attributes != null)
		{
			temperature = Attributes["temperature"].AsInt(1200);
			tempLossPerMeter = Attributes["tempLossPerMeter"].AsInt(100);
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		string text = Variant["flow"];
		Flow = ((text != null) ? string.Intern(text) : null);
		FlowNormali = ((Flow == null) ? null : Cardinal.FromInitial(Flow)?.Normali);
		Height = Variant["height"]?.ToInt() ?? 7;
		if (blockFire == null)
		{
			blockFire = api.World.GetBlock(new AssetLocation("fire"));
			fireParticles = new AdvancedParticleProperties[blockFire.ParticleProperties.Length];
			for (int i = 0; i < fireParticles.Length; i++)
			{
				fireParticles[i] = blockFire.ParticleProperties[i].Clone();
			}
			fireParticles[2].HsvaColor[2].avg += 60f;
			fireParticles[2].LifeLength.avg += 3f;
		}
	}

	public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
	{
		base.OnServerGameTick(world, pos, extra);
		FireLocation fireLocation = (FireLocation)extra;
		world.BlockAccessor.SetBlock(blockFire.BlockId, fireLocation.firePos);
		world.BlockAccessor.GetBlockEntity(fireLocation.firePos)?.GetBehavior<BEBehaviorBurning>()?.OnFirePlaced(fireLocation.facing, null);
	}

	private FireLocation FindFireLocation(IWorldAccessor world, BlockPos lavaPos)
	{
		Random rand = world.Rand;
		int num = 20;
		if (world.BlockAccessor.GetBlockAbove(lavaPos).Id == 0)
		{
			num = 40;
		}
		BlockPos blockPos = new BlockPos(lavaPos.dimension);
		for (int i = 0; i < num; i++)
		{
			blockPos.Set(lavaPos);
			blockPos.Add(rand.Next(7) - 3, rand.Next(4), rand.Next(7) - 3);
			if (world.BlockAccessor.GetBlock(blockPos).Id == 0)
			{
				BlockFacing blockFacing = IsNextToCombustibleBlock(world, lavaPos, blockPos);
				if (blockFacing != null)
				{
					return new FireLocation(blockPos, blockFacing);
				}
			}
		}
		return null;
	}

	private BlockFacing IsNextToCombustibleBlock(IWorldAccessor world, BlockPos lavaPos, BlockPos airBlockPos)
	{
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing in aLLFACES)
		{
			BlockPos pos = airBlockPos.AddCopy(blockFacing);
			Block block = world.BlockAccessor.GetBlock(pos);
			if (block.CombustibleProps != null && block.CombustibleProps.BurnTemperature <= GetTemperatureAtLocation(lavaPos, airBlockPos))
			{
				return blockFacing;
			}
		}
		return null;
	}

	private int GetTemperatureAtLocation(BlockPos lavaPos, BlockPos airBlockPos)
	{
		int num = lavaPos.ManhattenDistance(airBlockPos);
		return temperature - num * tempLossPerMeter;
	}

	public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
	{
		if (LiquidLevel == 7)
		{
			FireLocation fireLocation = FindFireLocation(world, pos);
			if (fireLocation != null)
			{
				extra = fireLocation;
				return true;
			}
		}
		extra = null;
		return false;
	}

	public override float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
	{
		return 99999f;
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
	{
		isWindAffected = false;
		pos.Up();
		Block block = world.BlockAccessor.GetBlock(pos);
		pos.Down();
		if (!block.IsLiquid())
		{
			if (block.CollisionBoxes != null)
			{
				return block.CollisionBoxes.Length == 0;
			}
			return true;
		}
		return false;
	}

	public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		if (GameMath.MurmurHash3Mod(pos.X, pos.Y, pos.Z, 100) < 2)
		{
			for (int i = 0; i < fireParticles.Length; i++)
			{
				AdvancedParticleProperties advancedParticleProperties = fireParticles[i];
				advancedParticleProperties.Quantity.avg = (float)i * 0.3f;
				advancedParticleProperties.WindAffectednesAtPos = windAffectednessAtPos;
				advancedParticleProperties.basePos.X = (float)pos.X + TopMiddlePos.X;
				advancedParticleProperties.basePos.Y = (float)pos.InternalY + TopMiddlePos.Y;
				advancedParticleProperties.basePos.Z = (float)pos.Z + TopMiddlePos.Z;
				manager.Spawn(advancedParticleProperties);
			}
		}
		base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
	}
}
