using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockWithLeavesMotion : Block
{
	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		if (VertexFlags.WindMode == EnumWindBitMode.NoWind)
		{
			return;
		}
		int num = 0;
		int i = 0;
		bool flag = api.World.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight) >= 14;
		if (flag)
		{
			for (int j = 0; j < 6; j++)
			{
				BlockFacing facing = BlockFacing.ALLFACES[j];
				Block block = world.BlockAccessor.GetBlock(pos.AddCopy(facing));
				if (block.BlockMaterial != EnumBlockMaterial.Leaves && block.SideSolid[BlockFacing.ALLFACES[j].Opposite.Index])
				{
					num |= 1 << j;
				}
			}
			for (i = 1; i < 8; i++)
			{
				Block blockBelow = api.World.BlockAccessor.GetBlockBelow(pos, i);
				if (blockBelow.VertexFlags.WindMode == EnumWindBitMode.NoWind && blockBelow.SideSolid[BlockFacing.UP.Index])
				{
					break;
				}
			}
		}
		decalMesh.ToggleWindModeSetWindData(num, flag, i);
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (VertexFlags.WindMode == EnumWindBitMode.NoWind)
		{
			return;
		}
		bool flag = ((lightRgbsByCorner[24] >> 24) & 0xFF) >= 159;
		int i = 1;
		int num = 0;
		if (flag)
		{
			for (int j = 0; j < 6; j++)
			{
				Block block = chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[j]];
				if (block.BlockMaterial != EnumBlockMaterial.Leaves && block.SideSolid[TileSideEnum.GetOpposite(j)])
				{
					num |= 1 << j;
				}
			}
			int num2 = TileSideEnum.MoveIndex[5];
			int num3 = extIndex3d + num2;
			for (; i < 8; i++)
			{
				Block block2 = ((num3 < 0) ? api.World.BlockAccessor.GetBlockBelow(pos, i) : chunkExtBlocks[num3]);
				if (block2.VertexFlags.WindMode == EnumWindBitMode.NoWind && block2.SideSolid[4])
				{
					break;
				}
				num3 += num2;
			}
		}
		sourceMesh.ToggleWindModeSetWindData(num, flag, i);
	}
}
