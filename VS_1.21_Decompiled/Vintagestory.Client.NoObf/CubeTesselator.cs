using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class CubeTesselator : IBlockTesselator
{
	private float blockHeight = 1f;

	public CubeTesselator(float blockHeight)
	{
		this.blockHeight = blockHeight;
	}

	public void Tesselate(TCTCache vars)
	{
		float num = blockHeight;
		int num2 = 0;
		TextureAtlasPosition[] textureAtlasPositionsByTextureSubId = vars.textureAtlasPositionsByTextureSubId;
		int[] fastBlockTextureSubidsByFace = vars.fastBlockTextureSubidsByFace;
		bool hasAlternates = vars.block.HasAlternates;
		bool hasTiles = vars.block.HasTiles;
		int value = vars.ColorMapData.Value;
		int drawFaceFlags = vars.drawFaceFlags;
		int extIndex3d = vars.extIndex3d;
		int vertexFlags = vars.VertexFlags;
		FastVec3f[][] blockFaceVertices = vars.blockFaceVertices;
		BakedCompositeTexture[][] array = null;
		int k = 0;
		if (hasAlternates || hasTiles)
		{
			array = vars.block.FastTextureVariants;
			k = GameMath.MurmurHash3(vars.posX, vars.posY, vars.posZ);
		}
		MeshData[] poolForPass = vars.tct.GetPoolForPass(vars.RenderPass, 1);
		int[] moveIndex = TileSideEnum.MoveIndex;
		for (int i = 0; i < moveIndex.Length; i++)
		{
			if ((drawFaceFlags & (1 << i)) == 0)
			{
				continue;
			}
			vars.CalcBlockFaceLight(i, extIndex3d + moveIndex[i]);
			int num3;
			if (hasTiles)
			{
				BakedCompositeTexture[] array2 = array[i];
				if (array2 != null)
				{
					int tiledTexturesSelector = BakedCompositeTexture.GetTiledTexturesSelector(array2, i, vars.posX, vars.posY, vars.posZ);
					num3 = array2[GameMath.Mod(tiledTexturesSelector, array2.Length)].TextureSubId;
					goto IL_0142;
				}
			}
			if (hasAlternates)
			{
				BakedCompositeTexture[] array3 = array[i];
				if (array3 != null)
				{
					num3 = array3[GameMath.Mod(k, array3.Length)].TextureSubId;
					goto IL_0142;
				}
			}
			num3 = fastBlockTextureSubidsByFace[i];
			goto IL_0142;
			IL_0142:
			DrawBlockFace(vars, i, blockFaceVertices[i], textureAtlasPositionsByTextureSubId[num3], vertexFlags | BlockFacing.ALLFACES[i].NormalPackedFlags, value, poolForPass, num);
			num2 += 4;
		}
	}

	public static void DrawBlockFace(TCTCache vars, int tileSide, FastVec3f[] quadOffsets, TextureAtlasPosition texPos, int flags, int colorMapDataValue, MeshData[] meshPools, float blockHeight = 1f)
	{
		float num = ((tileSide <= 3) ? blockHeight : 1f);
		MeshData obj = meshPools[texPos.atlasNumber];
		int verticesCount = obj.VerticesCount;
		int[] currentLightRGBByCorner = vars.CurrentLightRGBByCorner;
		float y = texPos.y2;
		float v = y + (texPos.y1 - y) * num;
		float finalX = vars.finalX;
		float finalY = vars.finalY;
		float finalZ = vars.finalZ;
		FastVec3f fastVec3f = quadOffsets[7];
		obj.AddVertexWithFlags(finalX + fastVec3f.X, finalY + fastVec3f.Y * blockHeight, finalZ + fastVec3f.Z, texPos.x2, y, currentLightRGBByCorner[3], flags);
		fastVec3f = quadOffsets[5];
		obj.AddVertexWithFlags(finalX + fastVec3f.X, finalY + fastVec3f.Y * blockHeight, finalZ + fastVec3f.Z, texPos.x2, v, currentLightRGBByCorner[1], flags);
		fastVec3f = quadOffsets[4];
		obj.AddVertexWithFlags(finalX + fastVec3f.X, finalY + fastVec3f.Y * blockHeight, finalZ + fastVec3f.Z, texPos.x1, v, currentLightRGBByCorner[0], flags);
		fastVec3f = quadOffsets[6];
		obj.AddVertexWithFlags(finalX + fastVec3f.X, finalY + fastVec3f.Y * blockHeight, finalZ + fastVec3f.Z, texPos.x1, y, currentLightRGBByCorner[2], flags);
		obj.CustomInts.Add4(colorMapDataValue);
		obj.AddQuadIndices(verticesCount);
		vars.UpdateChunkMinMax(finalX, finalY, finalZ);
		vars.UpdateChunkMinMax(finalX + 1f, finalY + blockHeight, finalZ + 1f);
	}
}
