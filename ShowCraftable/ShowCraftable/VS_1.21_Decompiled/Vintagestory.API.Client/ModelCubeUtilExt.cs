using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class ModelCubeUtilExt : CubeMeshUtil
{
	public enum EnumShadeMode
	{
		Off,
		On,
		Gradient
	}

	private static int[] gradientNormalMixedFlags;

	static ModelCubeUtilExt()
	{
		gradientNormalMixedFlags = new int[6];
		for (int i = 0; i < 6; i++)
		{
			Vec3f vec3f = new Vec3f(0f, 1f, 0f).Mul(0.33f) + BlockFacing.ALLFACES[i].Normalf.Clone().Mul(0.66f);
			vec3f.Normalize();
			gradientNormalMixedFlags[i] = VertexFlags.PackNormal(vec3f);
		}
	}

	public static void AddFace(MeshData modeldata, BlockFacing face, Vec3f centerXyz, Vec3f sizeXyz, Vec2f originUv, Vec2f sizeUv, int textureId, int color, EnumShadeMode shade, int[] vertexFlags, float brightness = 1f, int uvRotation = 0, byte climateColorMapId = 0, byte seasonColorMapId = 0, short renderPass = -1)
	{
		int num = face.Index * 12;
		int num2 = face.Index * 8;
		int verticesCount = modeldata.VerticesCount;
		int color2 = ColorUtil.ColorMultiply3(color, brightness);
		if (shade == EnumShadeMode.Gradient)
		{
			float num3 = sizeXyz.Y / 2f;
			int normalPackedFlags = BlockFacing.UP.NormalPackedFlags;
			int num4 = gradientNormalMixedFlags[face.Index];
			for (int i = 0; i < 4; i++)
			{
				float x = centerXyz.X + sizeXyz.X * (float)CubeMeshUtil.CubeVertices[num++] / 2f;
				float num5 = centerXyz.Y + sizeXyz.Y * (float)CubeMeshUtil.CubeVertices[num++] / 2f;
				int num6 = 2 * ((uvRotation + i) % 4) + num2;
				modeldata.AddWithFlagsVertex(x, num5, centerXyz.Z + sizeXyz.Z * (float)CubeMeshUtil.CubeVertices[num++] / 2f, originUv.X + sizeUv.X * (float)CubeMeshUtil.CubeUvCoords[num6], originUv.Y + sizeUv.Y * (float)CubeMeshUtil.CubeUvCoords[num6 + 1], color2, vertexFlags[i] | ((num5 > num3) ? normalPackedFlags : num4));
			}
		}
		else
		{
			for (int j = 0; j < 4; j++)
			{
				int num7 = 2 * ((uvRotation + j) % 4) + num2;
				modeldata.AddWithFlagsVertex(centerXyz.X + sizeXyz.X * (float)CubeMeshUtil.CubeVertices[num++] / 2f, centerXyz.Y + sizeXyz.Y * (float)CubeMeshUtil.CubeVertices[num++] / 2f, centerXyz.Z + sizeXyz.Z * (float)CubeMeshUtil.CubeVertices[num++] / 2f, originUv.X + sizeUv.X * (float)CubeMeshUtil.CubeUvCoords[num7], originUv.Y + sizeUv.Y * (float)CubeMeshUtil.CubeUvCoords[num7 + 1], color2, vertexFlags[j]);
			}
		}
		modeldata.AddIndex(verticesCount);
		modeldata.AddIndex(verticesCount + 1);
		modeldata.AddIndex(verticesCount + 2);
		modeldata.AddIndex(verticesCount);
		modeldata.AddIndex(verticesCount + 2);
		modeldata.AddIndex(verticesCount + 3);
		if (modeldata.XyzFacesCount >= modeldata.XyzFaces.Length)
		{
			Array.Resize(ref modeldata.XyzFaces, modeldata.XyzFaces.Length + 32);
		}
		if (modeldata.TextureIndicesCount >= modeldata.TextureIndices.Length)
		{
			Array.Resize(ref modeldata.TextureIndices, modeldata.TextureIndices.Length + 32);
		}
		modeldata.TextureIndices[modeldata.TextureIndicesCount++] = modeldata.getTextureIndex(textureId);
		modeldata.XyzFaces[modeldata.XyzFacesCount++] = (byte)((shade != EnumShadeMode.Off) ? face.MeshDataIndex : 0);
		if (modeldata.ClimateColorMapIds != null)
		{
			if (modeldata.ColorMapIdsCount >= modeldata.ClimateColorMapIds.Length)
			{
				Array.Resize(ref modeldata.ClimateColorMapIds, modeldata.ClimateColorMapIds.Length + 32);
				Array.Resize(ref modeldata.SeasonColorMapIds, modeldata.SeasonColorMapIds.Length + 32);
			}
			modeldata.ClimateColorMapIds[modeldata.ColorMapIdsCount] = climateColorMapId;
			modeldata.SeasonColorMapIds[modeldata.ColorMapIdsCount++] = seasonColorMapId;
		}
		if (modeldata.RenderPassesAndExtraBits != null)
		{
			if (modeldata.RenderPassCount >= modeldata.RenderPassesAndExtraBits.Length)
			{
				Array.Resize(ref modeldata.RenderPassesAndExtraBits, modeldata.RenderPassesAndExtraBits.Length + 32);
			}
			modeldata.RenderPassesAndExtraBits[modeldata.RenderPassCount++] = renderPass;
		}
	}

	public static void AddFaceSkipTex(MeshData modeldata, BlockFacing face, Vec3f centerXyz, Vec3f sizeXyz, int color, float brightness = 1f)
	{
		int num = face.Index * 12;
		int verticesCount = modeldata.VerticesCount;
		for (int i = 0; i < 4; i++)
		{
			float[] array = new float[3]
			{
				centerXyz.X + sizeXyz.X * (float)CubeMeshUtil.CubeVertices[num++] / 2f,
				centerXyz.Y + sizeXyz.Y * (float)CubeMeshUtil.CubeVertices[num++] / 2f,
				centerXyz.Z + sizeXyz.Z * (float)CubeMeshUtil.CubeVertices[num++] / 2f
			};
			modeldata.AddVertexSkipTex(array[0], array[1], array[2], ColorUtil.ColorMultiply3(color, brightness));
		}
		modeldata.AddIndex(verticesCount);
		modeldata.AddIndex(verticesCount + 1);
		modeldata.AddIndex(verticesCount + 2);
		modeldata.AddIndex(verticesCount);
		modeldata.AddIndex(verticesCount + 2);
		modeldata.AddIndex(verticesCount + 3);
	}
}
