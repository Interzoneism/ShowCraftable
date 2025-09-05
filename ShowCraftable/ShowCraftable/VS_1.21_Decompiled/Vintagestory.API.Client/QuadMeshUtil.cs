namespace Vintagestory.API.Client;

public class QuadMeshUtil
{
	private static int[] quadVertices = new int[12]
	{
		-1, -1, 0, 1, -1, 0, 1, 1, 0, -1,
		1, 0
	};

	private static int[] quadTextureCoords = new int[8] { 0, 0, 1, 0, 1, 1, 0, 1 };

	private static int[] quadVertexIndices = new int[6] { 0, 1, 2, 0, 2, 3 };

	public static MeshData GetQuad()
	{
		MeshData meshData = new MeshData();
		float[] array = new float[12];
		for (int i = 0; i < 12; i++)
		{
			array[i] = quadVertices[i];
		}
		meshData.SetXyz(array);
		float[] array2 = new float[8];
		for (int j = 0; j < array2.Length; j++)
		{
			array2[j] = quadTextureCoords[j];
		}
		meshData.SetUv(array2);
		meshData.SetVerticesCount(4);
		meshData.SetIndices(quadVertexIndices);
		meshData.SetIndicesCount(6);
		return meshData;
	}

	public static MeshData GetCustomQuadModelData(float x, float y, float z, float dw, float dh)
	{
		MeshData meshData = new MeshData(4, 6, withNormals: false, withUv: true, withRgba: false, withFlags: false);
		for (int i = 0; i < 4; i++)
		{
			meshData.AddVertex(x + ((quadVertices[i * 3] > 0) ? dw : 0f), y + ((quadVertices[i * 3 + 1] > 0) ? dh : 0f), z, quadTextureCoords[i * 2], quadTextureCoords[i * 2 + 1]);
		}
		for (int j = 0; j < 6; j++)
		{
			meshData.AddIndex(quadVertexIndices[j]);
		}
		return meshData;
	}

	public static MeshData GetCustomQuad(float x, float y, float z, float width, float height, byte r, byte g, byte b, byte a)
	{
		MeshData meshData = new MeshData();
		meshData.SetXyz(new float[12]
		{
			x,
			y,
			z,
			x + width,
			y,
			z,
			x + width,
			y + height,
			z,
			x,
			y + height,
			z
		});
		float[] array = new float[8];
		for (int i = 0; i < array.Length; i += 2)
		{
			array[i] = (float)quadTextureCoords[i] * width;
			array[i + 1] = (float)quadTextureCoords[i + 1] * height;
		}
		meshData.SetUv(array);
		byte[] array2 = new byte[16];
		for (int j = 0; j < 4; j++)
		{
			array2[j * 4] = r;
			array2[j * 4 + 1] = g;
			array2[j * 4 + 2] = b;
			array2[j * 4 + 3] = a;
		}
		meshData.SetRgba(array2);
		meshData.SetVerticesCount(4);
		meshData.SetIndices(quadVertexIndices);
		meshData.SetIndicesCount(6);
		return meshData;
	}

	public static MeshData GetCustomQuadHorizontal(float x, float y, float z, float width, float length, byte r, byte g, byte b, byte a)
	{
		MeshData meshData = new MeshData();
		meshData.SetXyz(new float[12]
		{
			x,
			y,
			z,
			x + width,
			y,
			z,
			x + width,
			y,
			z + length,
			x,
			y,
			z + length
		});
		float[] array = new float[8];
		for (int i = 0; i < array.Length; i += 2)
		{
			array[i] = (float)quadTextureCoords[i] * width;
			array[i + 1] = (float)quadTextureCoords[i + 1] * length;
		}
		meshData.SetUv(array);
		byte[] array2 = new byte[16];
		for (int j = 0; j < 4; j++)
		{
			array2[j * 4] = r;
			array2[j * 4 + 1] = g;
			array2[j * 4 + 2] = b;
			array2[j * 4 + 3] = a;
		}
		meshData.SetRgba(array2);
		meshData.SetVerticesCount(4);
		meshData.SetIndices(quadVertexIndices);
		meshData.SetIndicesCount(6);
		return meshData;
	}

	public static MeshData GetCustomQuadModelData(float u, float v, float u2, float v2, float dx, float dy, float dw, float dh, byte r, byte g, byte b, byte a)
	{
		MeshData meshData = new MeshData();
		meshData.SetXyz(new float[12]
		{
			dx,
			dy,
			0f,
			dx + dw,
			dy,
			0f,
			dx + dw,
			dy + dh,
			0f,
			dx,
			dy + dh,
			0f
		});
		meshData.SetUv(new float[8] { u, v, u2, v, u2, v2, u, v2 });
		byte[] array = new byte[16];
		for (int i = 0; i < 4; i++)
		{
			array[i * 4] = r;
			array[i * 4 + 1] = g;
			array[i * 4 + 2] = b;
			array[i * 4 + 3] = a;
		}
		meshData.SetRgba(array);
		meshData.SetVerticesCount(4);
		meshData.SetIndices(quadVertexIndices);
		meshData.SetIndicesCount(6);
		return meshData;
	}
}
