using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class CloudMeshUtil : CubeMeshUtil
{
	public static MeshData GetCubeModelDataForClouds(float scaleH, float scaleV, Vec3f translate)
	{
		MeshData meshData = new MeshData();
		meshData.xyz = new float[72];
		meshData.Normals = new int[24];
		for (int i = 0; i < 24; i++)
		{
			meshData.xyz[3 * i] = (float)CubeMeshUtil.CubeVertices[3 * i] * scaleH + translate.X;
			meshData.xyz[3 * i + 1] = (float)CubeMeshUtil.CubeVertices[3 * i + 1] * scaleV + translate.Y;
			meshData.xyz[3 * i + 2] = (float)CubeMeshUtil.CubeVertices[3 * i + 2] * scaleH + translate.Z;
		}
		meshData.SetVerticesCount(24);
		meshData.SetIndices(CubeMeshUtil.CubeVertexIndices);
		meshData.SetIndicesCount(36);
		return meshData;
	}

	public static void AddIndicesWithSides(MeshData model, int offset, byte sideFlags)
	{
		int num = 12 + 6 * (sideFlags & 1) + 6 * ((sideFlags >> 1) & 1) + 6 * ((sideFlags >> 2) & 1) + 6 * ((sideFlags >> 3) & 1);
		if (model.IndicesCount + num >= model.IndicesMax)
		{
			model.GrowIndexBuffer(model.IndicesCount + num);
		}
		model.Indices[model.IndicesCount++] = offset + 16;
		model.Indices[model.IndicesCount++] = offset + 17;
		model.Indices[model.IndicesCount++] = offset + 18;
		model.Indices[model.IndicesCount++] = offset + 16;
		model.Indices[model.IndicesCount++] = offset + 18;
		model.Indices[model.IndicesCount++] = offset + 19;
		model.Indices[model.IndicesCount++] = offset + 20;
		model.Indices[model.IndicesCount++] = offset + 21;
		model.Indices[model.IndicesCount++] = offset + 22;
		model.Indices[model.IndicesCount++] = offset + 20;
		model.Indices[model.IndicesCount++] = offset + 22;
		model.Indices[model.IndicesCount++] = offset + 23;
		if ((sideFlags & BlockFacing.NORTH.Flag) > 0)
		{
			model.Indices[model.IndicesCount++] = offset;
			model.Indices[model.IndicesCount++] = offset + 1;
			model.Indices[model.IndicesCount++] = offset + 2;
			model.Indices[model.IndicesCount++] = offset;
			model.Indices[model.IndicesCount++] = offset + 2;
			model.Indices[model.IndicesCount++] = offset + 3;
		}
		if ((sideFlags & BlockFacing.EAST.Flag) > 0)
		{
			model.Indices[model.IndicesCount++] = offset + 4;
			model.Indices[model.IndicesCount++] = offset + 5;
			model.Indices[model.IndicesCount++] = offset + 6;
			model.Indices[model.IndicesCount++] = offset + 4;
			model.Indices[model.IndicesCount++] = offset + 6;
			model.Indices[model.IndicesCount++] = offset + 7;
		}
		if ((sideFlags & BlockFacing.SOUTH.Flag) > 0)
		{
			model.Indices[model.IndicesCount++] = offset + 8;
			model.Indices[model.IndicesCount++] = offset + 9;
			model.Indices[model.IndicesCount++] = offset + 10;
			model.Indices[model.IndicesCount++] = offset + 8;
			model.Indices[model.IndicesCount++] = offset + 10;
			model.Indices[model.IndicesCount++] = offset + 11;
		}
		if ((sideFlags & BlockFacing.WEST.Flag) > 0)
		{
			model.Indices[model.IndicesCount++] = offset + 12;
			model.Indices[model.IndicesCount++] = offset + 13;
			model.Indices[model.IndicesCount++] = offset + 14;
			model.Indices[model.IndicesCount++] = offset + 12;
			model.Indices[model.IndicesCount++] = offset + 14;
			model.Indices[model.IndicesCount++] = offset + 15;
		}
	}

	public static int[] GetIndicesWithSides(int offset, byte faceFlags)
	{
		int[] array = new int[12 + 6 * (faceFlags & 1) + 6 * ((faceFlags >> 1) & 1) + 6 * ((faceFlags >> 2) & 1) + 6 * ((faceFlags >> 3) & 1)];
		int num = 0;
		array[num++] = offset + 16;
		array[num++] = offset + 17;
		array[num++] = offset + 18;
		array[num++] = offset + 16;
		array[num++] = offset + 18;
		array[num++] = offset + 19;
		array[num++] = offset + 20;
		array[num++] = offset + 21;
		array[num++] = offset + 22;
		array[num++] = offset + 20;
		array[num++] = offset + 22;
		array[num++] = offset + 23;
		if ((faceFlags & BlockFacing.NORTH.Flag) > 0)
		{
			array[num++] = offset;
			array[num++] = offset + 1;
			array[num++] = offset + 2;
			array[num++] = offset;
			array[num++] = offset + 2;
			array[num++] = offset + 3;
		}
		if ((faceFlags & BlockFacing.EAST.Flag) > 0)
		{
			array[num++] = offset + 4;
			array[num++] = offset + 5;
			array[num++] = offset + 6;
			array[num++] = offset + 4;
			array[num++] = offset + 6;
			array[num++] = offset + 7;
		}
		if ((faceFlags & BlockFacing.SOUTH.Flag) > 0)
		{
			array[num++] = offset + 8;
			array[num++] = offset + 9;
			array[num++] = offset + 10;
			array[num++] = offset + 8;
			array[num++] = offset + 10;
			array[num++] = offset + 11;
		}
		if ((faceFlags & BlockFacing.WEST.Flag) > 0)
		{
			array[num++] = offset + 12;
			array[num++] = offset + 13;
			array[num++] = offset + 14;
			array[num++] = offset + 12;
			array[num++] = offset + 14;
			array[num++] = offset + 15;
		}
		return array;
	}
}
