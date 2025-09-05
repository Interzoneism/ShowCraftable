using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class CrossAndSnowlayerTesselator : IBlockTesselator
{
	private float blockheight;

	public CrossAndSnowlayerTesselator(float blockheight)
	{
		this.blockheight = blockheight;
	}

	public void Tesselate(TCTCache vars)
	{
		bool flag = false;
		int num = vars.extIndex3d + TileSideEnum.MoveIndex[5];
		flag = vars.tct.currentChunkFluidBlocksExt[num].SideSolid[BlockFacing.UP.Index];
		if (!flag)
		{
			flag = vars.tct.currentChunkBlocksExt[num].SideSolid[BlockFacing.UP.Index];
		}
		if (flag)
		{
			float finalX = vars.finalX;
			float finalZ = vars.finalZ;
			vars.finalX = vars.lx;
			vars.finalZ = vars.lz;
			int num2 = vars.VertexFlags & 0x1FFFFFF & -1793;
			MeshData[] poolForPass = vars.tct.GetPoolForPass(EnumChunkRenderPass.Opaque, 1);
			for (int i = 0; i < 6; i++)
			{
				if ((vars.drawFaceFlags & TileSideEnum.ToFlags(i)) != 0)
				{
					vars.CalcBlockFaceLight(i, vars.extIndex3d + TileSideEnum.MoveIndex[i]);
					CubeTesselator.DrawBlockFace(vars, i, vars.blockFaceVertices[i], vars.textureAtlasPositionsByTextureSubId[vars.fastBlockTextureSubidsByFace[6]], num2 | BlockFacing.ALLFACES[i].NormalPackedFlags, 0, poolForPass, blockheight);
				}
			}
			vars.finalX = finalX;
			vars.finalZ = finalZ;
		}
		vars.drawFaceFlags = 3;
		CrossTesselator.DrawCross(vars, 1.41f);
	}
}
