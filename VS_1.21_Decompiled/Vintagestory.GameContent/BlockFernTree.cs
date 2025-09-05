using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockFernTree : Block, ITreeGenerator
{
	public Block? trunk;

	public Block? trunkTopYoung;

	public Block? trunkTopMedium;

	public Block? trunkTopOld;

	public Block? foliage;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (Variant["part"] == "trunk")
		{
			(api as ICoreServerAPI)?.RegisterTreeGenerator(Code, this);
		}
		IBlockAccessor blockAccessor = api.World.BlockAccessor;
		if (trunk == null)
		{
			trunk = blockAccessor.GetBlock(CodeWithVariant("part", "trunk"));
		}
		if (trunkTopYoung == null)
		{
			trunkTopYoung = blockAccessor.GetBlock(CodeWithVariant("part", "trunk-top-young"));
		}
		if (trunkTopMedium == null)
		{
			trunkTopMedium = blockAccessor.GetBlock(CodeWithVariant("part", "trunk-top-medium"));
		}
		if (trunkTopOld == null)
		{
			trunkTopOld = blockAccessor.GetBlock(CodeWithVariant("part", "trunk-top-old"));
		}
		if (foliage == null)
		{
			foliage = blockAccessor.GetBlock(CodeWithVariant("part", "foliage"));
		}
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		base.OnDecalTesselation(world, decalMesh, pos);
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		base.OnJsonTesselation(ref sourceMesh, ref lightRgbsByCorner, pos, chunkExtBlocks, extIndex3d);
		if (Variant["part"] == "foliage")
		{
			for (int i = 0; i < sourceMesh.FlagsCount; i++)
			{
				sourceMesh.Flags[i] = (sourceMesh.Flags[i] & -33546241) | BlockFacing.UP.NormalPackedFlags;
			}
		}
	}

	public string? Type()
	{
		return Variant["type"];
	}

	public void GrowTree(IBlockAccessor blockAccessor, BlockPos pos, TreeGenParams treeGenParams, IRandom rand)
	{
		float value = ((treeGenParams.otherBlockChance == 0f) ? (1f + (float)rand.NextDouble() * 2.5f) : (1.5f + (float)rand.NextDouble() * 4f));
		int num = GameMath.RoundRandom(rand, value);
		while (num-- > 0)
		{
			GrowOneFern(blockAccessor, pos.UpCopy(), treeGenParams.size, treeGenParams.vinesGrowthChance, rand);
			pos.X += rand.NextInt(8) - 4;
			pos.Z += rand.NextInt(8) - 4;
			bool flag = false;
			for (int num2 = 2; num2 >= -2; num2--)
			{
				if (blockAccessor.GetBlockAbove(pos, num2).Fertility > 0 && !blockAccessor.GetBlockAbove(pos, num2 + 1, 2).IsLiquid())
				{
					pos.Y += num2;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				break;
			}
		}
	}

	private void GrowOneFern(IBlockAccessor blockAccessor, BlockPos upos, float sizeModifier, float vineGrowthChance, IRandom rand)
	{
		int num = GameMath.Clamp((int)(sizeModifier * (float)(2 + rand.NextInt(6))), 2, 6);
		Block block = ((num > 2) ? trunkTopOld : ((num != 1) ? trunkTopMedium : trunkTopYoung));
		if (num == 1)
		{
			block = trunkTopYoung;
		}
		for (int i = 0; i < num; i++)
		{
			Block block2 = ((i == num - 2) ? block : ((i == num - 1) ? foliage : trunk));
			if (block2 != null && !blockAccessor.GetBlockAbove(upos, i).IsReplacableBy(block2))
			{
				return;
			}
		}
		for (int j = 0; j < num; j++)
		{
			Block block3 = ((j == num - 2) ? block : ((j == num - 1) ? foliage : trunk));
			if (block3 != null)
			{
				blockAccessor.SetBlock(block3.BlockId, upos);
			}
			upos.Up();
		}
	}
}
