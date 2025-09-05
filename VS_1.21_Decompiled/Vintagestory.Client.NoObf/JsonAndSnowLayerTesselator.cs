using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class JsonAndSnowLayerTesselator : IBlockTesselator
{
	private IBlockTesselator json;

	private BlockPos tmpPos = new BlockPos();

	public JsonAndSnowLayerTesselator()
	{
		json = new JsonTesselator();
	}

	public void Tesselate(TCTCache vars)
	{
		if (vars.tct.currentChunkBlocksExt[vars.extIndex3d + TileSideEnum.MoveIndex[5]].AllowSnowCoverage(vars.tct.game, tmpPos.Set(vars.posX, vars.posY - 1, vars.posZ)))
		{
			float finalX = vars.finalX;
			float finalZ = vars.finalZ;
			vars.finalX = vars.lx;
			vars.finalZ = vars.lz;
			int num = vars.VertexFlags & 0x1FFFFFF & -1793;
			MeshData[] poolForPass = vars.tct.GetPoolForPass(EnumChunkRenderPass.Opaque, 1);
			TextureAtlasPosition texPos = vars.textureAtlasPositionsByTextureSubId[vars.fastBlockTextureSubidsByFace[6]];
			for (int i = 0; i < 6; i++)
			{
				if ((vars.drawFaceFlags & TileSideEnum.ToFlags(i)) != 0)
				{
					vars.CalcBlockFaceLight(i, vars.extIndex3d + TileSideEnum.MoveIndex[i]);
					CubeTesselator.DrawBlockFace(vars, i, vars.blockFaceVertices[i], texPos, num | BlockFacing.ALLFACES[i].NormalPackedFlags, 0, poolForPass, 0.125f);
				}
			}
			vars.finalX = finalX;
			vars.finalZ = finalZ;
		}
		json.Tesselate(vars);
	}
}
