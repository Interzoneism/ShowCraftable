using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

public class CubeMeshUtil
{
	public static float[] CloudSideShadings = new float[4] { 1f, 0.6f, 0.6f, 0.45f };

	public static float[] DefaultBlockSideShadings = new float[4] { 1f, 0.75f, 0.6f, 0.45f };

	public static float[] DefaultBlockSideShadingsByFacing = new float[6]
	{
		DefaultBlockSideShadings[2],
		DefaultBlockSideShadings[1],
		DefaultBlockSideShadings[2],
		DefaultBlockSideShadings[1],
		DefaultBlockSideShadings[0],
		DefaultBlockSideShadings[3]
	};

	public static int[] CubeVertices = new int[72]
	{
		-1, -1, -1, -1, 1, -1, 1, 1, -1, 1,
		-1, -1, 1, -1, -1, 1, 1, -1, 1, 1,
		1, 1, -1, 1, 1, -1, 1, 1, 1, 1,
		-1, 1, 1, -1, -1, 1, -1, -1, 1, -1,
		1, 1, -1, 1, -1, -1, -1, -1, 1, 1,
		1, 1, 1, -1, -1, 1, -1, -1, 1, 1,
		-1, -1, 1, -1, -1, -1, 1, -1, -1, 1,
		-1, 1
	};

	public static byte[] CubeFaceIndices = new byte[6]
	{
		BlockFacing.NORTH.MeshDataIndex,
		BlockFacing.EAST.MeshDataIndex,
		BlockFacing.SOUTH.MeshDataIndex,
		BlockFacing.WEST.MeshDataIndex,
		BlockFacing.UP.MeshDataIndex,
		BlockFacing.DOWN.MeshDataIndex
	};

	public static int[] CubeUvCoords = new int[48]
	{
		1, 0, 1, 1, 0, 1, 0, 0, 1, 0,
		1, 1, 0, 1, 0, 0, 1, 0, 1, 1,
		0, 1, 0, 0, 1, 0, 1, 1, 0, 1,
		0, 0, 1, 0, 1, 1, 0, 1, 0, 0,
		1, 0, 1, 1, 0, 1, 0, 0
	};

	public static int[] CubeVertexIndices = new int[36]
	{
		0, 1, 2, 0, 2, 3, 4, 5, 6, 4,
		6, 7, 8, 9, 10, 8, 10, 11, 12, 13,
		14, 12, 14, 15, 16, 17, 18, 16, 18, 19,
		20, 21, 22, 20, 22, 23
	};

	public static int[] BaseCubeVertexIndices = new int[6] { 0, 1, 2, 0, 2, 3 };

	public static MeshData GetCube()
	{
		MeshData meshData = new MeshData();
		float[] array = new float[72];
		for (int i = 0; i < 72; i++)
		{
			array[i] = CubeVertices[i];
		}
		meshData.SetXyz(array);
		float[] array2 = new float[48];
		for (int j = 0; j < 48; j++)
		{
			array2[j] = CubeUvCoords[j];
		}
		byte[] rgba = new byte[96];
		meshData.SetRgba(rgba);
		meshData.SetUv(array2);
		meshData.TextureIndices = new byte[6];
		meshData.SetVerticesCount(24);
		meshData.SetIndices(CubeVertexIndices);
		meshData.SetIndicesCount(36);
		meshData.Flags = new int[24];
		for (int k = 0; k < 24; k += 4)
		{
			BlockFacing blockFacing = BlockFacing.ALLFACES[k / 6];
			meshData.Flags[k] = blockFacing.NormalPackedFlags;
			meshData.Flags[k + 1] = meshData.Flags[k];
			meshData.Flags[k + 2] = meshData.Flags[k];
			meshData.Flags[k + 3] = meshData.Flags[k];
		}
		meshData.VerticesMax = meshData.VerticesCount;
		return meshData;
	}

	public static byte[] GetShadedCubeRGBA(int baseColor, float[] blockSideShadings, bool smoothShadedSides)
	{
		int num = ColorUtil.ColorMultiply3(baseColor, blockSideShadings[0]);
		int num2 = ColorUtil.ColorMultiply3(baseColor, blockSideShadings[1]);
		int num3 = ColorUtil.ColorMultiply3(baseColor, blockSideShadings[2]);
		int num4 = ColorUtil.ColorMultiply3(baseColor, blockSideShadings[3]);
		return GetShadedCubeRGBA(new int[6] { num2, num3, num3, num2, num, num4 }, smoothShadedSides);
	}

	public unsafe static byte[] GetShadedCubeRGBA(int[] colorSides, bool smoothShadedSides)
	{
		byte[] array = new byte[96];
		fixed (byte* ptr = array)
		{
			int* ptr2 = (int*)ptr;
			for (int i = 0; i < 6; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					ptr2[(i * 4 + j) * 4 / 4] = colorSides[i];
				}
			}
			if (smoothShadedSides)
			{
				*ptr2 = colorSides[3];
				ptr2[1] = colorSides[3];
				ptr2[4] = colorSides[3];
				ptr2[7] = colorSides[3];
				ptr2[16] = colorSides[3];
				ptr2[19] = colorSides[3];
				ptr2[20] = colorSides[3];
				ptr2[21] = colorSides[3];
			}
		}
		return array;
	}

	public static MeshData GetCubeOnlyScaleXyz(float scaleH, float scaleV, Vec3f translate)
	{
		MeshData cube = GetCube();
		for (int i = 0; i < cube.GetVerticesCount(); i++)
		{
			cube.xyz[3 * i] *= scaleH;
			cube.xyz[3 * i + 1] *= scaleV;
			cube.xyz[3 * i + 2] *= scaleH;
			cube.xyz[3 * i] += translate.X;
			cube.xyz[3 * i + 1] += translate.Y;
			cube.xyz[3 * i + 2] += translate.Z;
		}
		return cube;
	}

	public static MeshData GetCube(float scaleH, float scaleV, Vec3f translate)
	{
		MeshData cube = GetCube();
		for (int i = 0; i < cube.GetVerticesCount(); i++)
		{
			cube.xyz[3 * i] *= scaleH;
			cube.xyz[3 * i + 1] *= scaleV;
			cube.xyz[3 * i + 2] *= scaleH;
			cube.xyz[3 * i] += translate.X;
			cube.xyz[3 * i + 1] += translate.Y;
			cube.xyz[3 * i + 2] += translate.Z;
			cube.Uv[2 * i] *= 2f * scaleH;
			cube.Uv[2 * i + 1] *= ((i >= 16) ? (2f * scaleH) : (2f * scaleV));
		}
		cube.Rgba.Fill(byte.MaxValue);
		return cube;
	}

	public static MeshData GetCube(float scaleX, float scaleY, float scaleZ, Vec3f translate)
	{
		MeshData cube = GetCube();
		cube.Rgba.Fill(byte.MaxValue);
		return ScaleCubeMesh(cube, scaleX, scaleY, scaleZ, translate);
	}

	public static MeshData ScaleCubeMesh(MeshData modelData, float scaleX, float scaleY, float scaleZ, Vec3f translate)
	{
		float[] array = new float[3] { scaleZ, scaleX, scaleX };
		float[] array2 = new float[3] { scaleY, scaleZ, scaleY };
		float[] array3 = new float[3] { translate.Z, translate.X, translate.X };
		float[] array4 = new float[3] { translate.Y, translate.Z, translate.Y };
		int verticesCount = modelData.GetVerticesCount();
		for (int i = 0; i < verticesCount; i++)
		{
			modelData.xyz[3 * i] *= scaleX;
			modelData.xyz[3 * i + 1] *= scaleY;
			modelData.xyz[3 * i + 2] *= scaleZ;
			modelData.xyz[3 * i] += scaleX + translate.X;
			modelData.xyz[3 * i + 1] += scaleY + translate.Y;
			modelData.xyz[3 * i + 2] += scaleZ + translate.Z;
			BlockFacing obj = BlockFacing.ALLFACES[i / 4];
			int axis = (int)obj.Axis;
			switch (obj.Index)
			{
			case 0:
				modelData.Uv[2 * i] = modelData.Uv[2 * i] * 2f * array[axis] + (1f - 2f * array[axis]) - array3[axis];
				modelData.Uv[2 * i + 1] = (1f - modelData.Uv[2 * i + 1]) * 2f * array2[axis] + (1f - 2f * array2[axis]) - array4[axis];
				break;
			case 1:
				modelData.Uv[2 * i] = modelData.Uv[2 * i] * 2f * array[axis] + (1f - 2f * array[axis]) - array3[axis];
				modelData.Uv[2 * i + 1] = (1f - modelData.Uv[2 * i + 1]) * 2f * array2[axis] + (1f - 2f * array2[axis]) - array4[axis];
				break;
			case 2:
				modelData.Uv[2 * i] = modelData.Uv[2 * i] * 2f * array[axis] + array3[axis];
				modelData.Uv[2 * i + 1] = (1f - modelData.Uv[2 * i + 1]) * 2f * array2[axis] + (1f - 2f * array2[axis]) - array4[axis];
				break;
			case 3:
				modelData.Uv[2 * i] = modelData.Uv[2 * i] * 2f * array[axis] + array3[axis];
				modelData.Uv[2 * i + 1] = (1f - modelData.Uv[2 * i + 1]) * 2f * array2[axis] + (1f - 2f * array2[axis]) - array4[axis];
				break;
			case 4:
				modelData.Uv[2 * i] = (1f - modelData.Uv[2 * i]) * 2f * array[axis] + (1f - 2f * array[axis]) - array3[axis];
				modelData.Uv[2 * i + 1] = modelData.Uv[2 * i + 1] * 2f * array2[axis] + (1f - 2f * array2[axis]) - array4[axis];
				break;
			case 5:
				modelData.Uv[2 * i] = modelData.Uv[2 * i] * 2f * array[axis] + (1f - 2f * array[axis]) - array3[axis];
				modelData.Uv[2 * i + 1] = (1f - modelData.Uv[2 * i + 1]) * 2f * array2[axis] + array4[axis];
				break;
			}
		}
		return modelData;
	}

	public static MeshData GetCubeFace(BlockFacing face)
	{
		int index = face.Index;
		MeshData meshData = new MeshData();
		float[] array = new float[12];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = CubeVertices[i + 12 * index];
		}
		meshData.SetXyz(array);
		float[] array2 = new float[8];
		for (int j = 0; j < array2.Length; j++)
		{
			array2[j] = CubeUvCoords[j + 8 * index];
		}
		meshData.SetUv(array2);
		byte[] array3 = new byte[16];
		for (int k = 0; k < array3.Length; k++)
		{
			array3[k] = byte.MaxValue;
		}
		meshData.SetRgba(array3);
		meshData.SetVerticesCount(4);
		int[] array4 = new int[6];
		for (int l = 0; l < array4.Length; l++)
		{
			array4[l] = CubeVertexIndices[l];
		}
		meshData.SetIndices(array4);
		meshData.SetIndicesCount(6);
		return meshData;
	}

	public static MeshData GetCubeFace(BlockFacing face, float scaleH, float scaleV, Vec3f translate)
	{
		MeshData cubeFace = GetCubeFace(face);
		for (int i = 0; i < cubeFace.GetVerticesCount(); i++)
		{
			cubeFace.xyz[3 * i] *= scaleH;
			cubeFace.xyz[3 * i + 1] *= scaleV;
			cubeFace.xyz[3 * i + 2] *= scaleH;
			cubeFace.xyz[3 * i] += translate.X;
			cubeFace.xyz[3 * i + 1] += translate.Y;
			cubeFace.xyz[3 * i + 2] += translate.Z;
			cubeFace.Uv[2 * i] *= 2f * scaleH;
			cubeFace.Uv[2 * i + 1] *= ((i >= 16) ? (2f * scaleH) : (2f * scaleV));
		}
		cubeFace.Rgba.Fill(byte.MaxValue);
		return cubeFace;
	}

	public static void SetXyzFacesAndPacketNormals(MeshData mesh)
	{
		mesh.AddXyzFace(BlockFacing.NORTH.MeshDataIndex);
		mesh.AddXyzFace(BlockFacing.EAST.MeshDataIndex);
		mesh.AddXyzFace(BlockFacing.SOUTH.MeshDataIndex);
		mesh.AddXyzFace(BlockFacing.WEST.MeshDataIndex);
		mesh.AddXyzFace(BlockFacing.UP.MeshDataIndex);
		mesh.AddXyzFace(BlockFacing.DOWN.MeshDataIndex);
		for (int i = 0; i < 6; i++)
		{
			mesh.Flags[i * 4] = (mesh.Flags[i * 4 + 1] = (mesh.Flags[i * 4 + 2] = (mesh.Flags[i * 4 + 3] = VertexFlags.PackNormal(BlockFacing.ALLFACES[i].Normali))));
		}
	}
}
