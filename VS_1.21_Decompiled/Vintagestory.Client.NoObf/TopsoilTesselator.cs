using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class TopsoilTesselator : IBlockTesselator
{
	public void Tesselate(TCTCache vars)
	{
		int textureSubIdSecond = vars.fastBlockTextureSubidsByFace[6];
		int drawFaceFlags = vars.drawFaceFlags;
		int vertexFlags = vars.VertexFlags;
		int colorMapDataValue = vars.ColorMapData.Value;
		uint num = 0u;
		BakedCompositeTexture[][] array = null;
		bool hasAlternates;
		if (hasAlternates = vars.block.HasAlternates)
		{
			array = vars.block.FastTextureVariants;
			num = GameMath.oaatHashU(vars.posX, vars.posY, vars.posZ);
		}
		int num2 = 0;
		Block block = vars.tct.currentChunkBlocksExt[vars.extIndex3d + 1156];
		if ((block.BlockMaterial == EnumBlockMaterial.Snow || block.snowLevel > 0f) && vars.block.Textures.TryGetValue("snowed", out var value))
		{
			textureSubIdSecond = value.Baked.TextureSubId;
			colorMapDataValue = 0;
		}
		int num3 = GameMath.MurmurHash3Mod(vars.posX, vars.posY, vars.posZ, 4);
		MeshData[] poolForPass = vars.tct.GetPoolForPass(vars.RenderPass, 1);
		for (int i = 0; i < 6; i++)
		{
			if ((drawFaceFlags & TileSideEnum.ToFlags(i)) == 0)
			{
				continue;
			}
			vars.CalcBlockFaceLight(i, vars.extIndex3d + TileSideEnum.MoveIndex[i]);
			int textureSubId;
			if (hasAlternates)
			{
				BakedCompositeTexture[] array2 = array[i];
				if (array2 != null)
				{
					textureSubId = array2[num % array2.Length].TextureSubId;
					goto IL_0149;
				}
			}
			textureSubId = vars.fastBlockTextureSubidsByFace[i];
			goto IL_0149;
			IL_0149:
			int rotIndex = ((i == 4) ? num3 : 0);
			DrawBlockFaceTopSoil(vars, vertexFlags | BlockFacing.ALLFACES[i].NormalPackedFlags, vars.blockFaceVertices[i], colorMapDataValue, textureSubId, textureSubIdSecond, poolForPass, rotIndex);
			num2 += 4;
		}
	}

	private void DrawBlockFaceTopSoil(TCTCache vars, int flags, FastVec3f[] quadOffsets, int colorMapDataValue, int textureSubId, int textureSubIdSecond, MeshData[] meshPools, int rotIndex)
	{
		TextureAtlasPosition textureAtlasPosition = vars.textureAtlasPositionsByTextureSubId[textureSubId];
		TextureAtlasPosition textureAtlasPosition2 = vars.textureAtlasPositionsByTextureSubId[textureSubIdSecond];
		MeshData meshData = meshPools[textureAtlasPosition.atlasNumber];
		int verticesCount = meshData.VerticesCount;
		float num = vars.lx;
		float num2 = vars.ly;
		float num3 = vars.lz;
		FastVec3f fastVec3f = quadOffsets[7];
		meshData.AddVertexWithFlags(num + fastVec3f.X, num2 + fastVec3f.Y, num3 + fastVec3f.Z, textureAtlasPosition.x2, textureAtlasPosition.y2, vars.CurrentLightRGBByCorner[3], flags);
		fastVec3f = quadOffsets[5];
		meshData.AddVertexWithFlags(num + fastVec3f.X, num2 + fastVec3f.Y, num3 + fastVec3f.Z, textureAtlasPosition.x2, textureAtlasPosition.y1, vars.CurrentLightRGBByCorner[1], flags);
		fastVec3f = quadOffsets[4];
		meshData.AddVertexWithFlags(num + fastVec3f.X, num2 + fastVec3f.Y, num3 + fastVec3f.Z, textureAtlasPosition.x1, textureAtlasPosition.y1, vars.CurrentLightRGBByCorner[0], flags);
		fastVec3f = quadOffsets[6];
		meshData.AddVertexWithFlags(num + fastVec3f.X, num2 + fastVec3f.Y, num3 + fastVec3f.Z, textureAtlasPosition.x1, textureAtlasPosition.y2, vars.CurrentLightRGBByCorner[2], flags);
		float x = textureAtlasPosition2.x1;
		float u = textureAtlasPosition2.x1 + (textureAtlasPosition2.x2 - textureAtlasPosition2.x1) / 2f;
		float y = textureAtlasPosition2.y1;
		float y2 = textureAtlasPosition2.y2;
		switch (rotIndex)
		{
		case 0:
			meshData.CustomShorts.AddPackedUV(u, y2);
			meshData.CustomShorts.AddPackedUV(u, y);
			meshData.CustomShorts.AddPackedUV(x, y);
			meshData.CustomShorts.AddPackedUV(x, y2);
			break;
		case 1:
			meshData.CustomShorts.AddPackedUV(x, y2);
			meshData.CustomShorts.AddPackedUV(x, y);
			meshData.CustomShorts.AddPackedUV(u, y);
			meshData.CustomShorts.AddPackedUV(u, y2);
			break;
		case 2:
			meshData.CustomShorts.AddPackedUV(u, y);
			meshData.CustomShorts.AddPackedUV(u, y2);
			meshData.CustomShorts.AddPackedUV(x, y2);
			meshData.CustomShorts.AddPackedUV(x, y);
			break;
		case 3:
			meshData.CustomShorts.AddPackedUV(x, y);
			meshData.CustomShorts.AddPackedUV(x, y2);
			meshData.CustomShorts.AddPackedUV(u, y2);
			meshData.CustomShorts.AddPackedUV(u, y);
			break;
		}
		meshData.CustomInts.Add4(colorMapDataValue);
		meshData.AddQuadIndices(verticesCount);
		vars.UpdateChunkMinMax(num, num2, num3);
		vars.UpdateChunkMinMax(num + 1f, num2 + 1f, num3 + 1f);
	}
}
