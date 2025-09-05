using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockSeaweed : BlockWaterPlant
{
	protected Block[] blocks;

	private PlantAirParticles splashParticleProps = new PlantAirParticles();

	public override string RemapToLiquidsLayer => "water-still-7";

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		blocks = new Block[2]
		{
			api.World.BlockAccessor.GetBlock(CodeWithParts("section")),
			api.World.BlockAccessor.GetBlock(CodeWithParts("top"))
		};
	}

	public override bool CanPlantStay(IBlockAccessor blockAccessor, BlockPos pos)
	{
		Block blockBelow = blockAccessor.GetBlockBelow(pos, 1, 1);
		if (blockBelow.Fertility <= 0)
		{
			if (blockBelow is BlockSeaweed)
			{
				return blockBelow.Variant["part"] == "section";
			}
			return false;
		}
		return true;
	}

	public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		if (api.World.Rand.NextDouble() < 0.0025)
		{
			splashParticleProps.BasePos.Set((float)pos.X + 0.33f, pos.Y, (float)pos.Z + 0.33f);
			splashParticleProps.AddPos.Set(0.33000001311302185, 1.0, 0.33000001311302185);
			manager.Spawn(splashParticleProps);
		}
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
	{
		isWindAffected = false;
		return true;
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		IBlockAccessor blockAccessor = api.World.BlockAccessor;
		int num = ((blockAccessor.GetBlockBelow(pos, 1, 1) is BlockSeaweed) ? 1 : 0) + ((blockAccessor.GetBlockBelow(pos, 2, 1) is BlockSeaweed) ? 1 : 0) + ((blockAccessor.GetBlockBelow(pos, 3, 1) is BlockSeaweed) ? 1 : 0) + ((blockAccessor.GetBlockBelow(pos, 4, 1) is BlockSeaweed) ? 1 : 0);
		float[] xyz = sourceMesh.xyz;
		int[] flags = sourceMesh.Flags;
		int flagsCount = sourceMesh.FlagsCount;
		for (int i = 0; i < flagsCount; i++)
		{
			float num2 = xyz[i * 3 + 1];
			VertexFlags.ReplaceWindData(ref flags[i], num + ((num2 > 0f) ? 1 : 0));
		}
	}

	public override bool TryPlaceBlockForWorldGenUnderwater(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, int minWaterDepth, int maxWaterDepth, BlockPatchAttributes attributes = null)
	{
		NatFloat heightNatFloat = attributes?.Height ?? NatFloat.createGauss(3f, 3f);
		BlockPos blockPos = pos.DownCopy();
		for (int i = 1; i < maxWaterDepth; i++)
		{
			blockPos.Down();
			Block block = blockAccessor.GetBlock(blockPos);
			if (block is BlockWaterPlant)
			{
				return false;
			}
			if (block.Fertility > 0)
			{
				PlaceSeaweed(blockAccessor, blockPos, i, worldGenRand, heightNatFloat);
				return true;
			}
			if (!block.IsLiquid())
			{
				return false;
			}
		}
		return false;
	}

	internal void PlaceSeaweed(IBlockAccessor blockAccessor, BlockPos pos, int depth, IRandom random, NatFloat heightNatFloat)
	{
		int num = Math.Min(depth, (int)heightNatFloat.nextFloat(1f, random));
		while (num-- > 1)
		{
			pos.Up();
			blockAccessor.SetBlock(blocks[0].BlockId, pos);
		}
		pos.Up();
		if (blocks[1] == null)
		{
			blockAccessor.SetBlock(blocks[0].BlockId, pos);
		}
		else
		{
			blockAccessor.SetBlock(blocks[1].BlockId, pos);
		}
	}
}
