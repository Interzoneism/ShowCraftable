using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class QuadMeshUtilExt
{
	private static int[] quadVertices = new int[12]
	{
		-1, -1, 0, 1, -1, 0, 1, 1, 0, -1,
		1, 0
	};

	private static int[] quadTextureCoords = new int[8] { 0, 0, 1, 0, 1, 1, 0, 1 };

	private static int[] quadVertexIndices = new int[6] { 0, 1, 2, 0, 2, 3 };

	public static MeshData GetQuadModelData()
	{
		MeshData meshData = new MeshData(4, 6, withNormals: false, withUv: true, withRgba: false, withFlags: false);
		for (int i = 0; i < 4; i++)
		{
			meshData.AddVertex(quadVertices[i * 3], quadVertices[i * 3 + 1], quadVertices[i * 3 + 2], quadTextureCoords[i * 2], quadTextureCoords[i * 2 + 1]);
		}
		for (int j = 0; j < 6; j++)
		{
			meshData.AddIndex(quadVertexIndices[j]);
		}
		return meshData;
	}

	public static MeshData GetCustomQuadModelData(float x, float y, float z, float dw, float dh, byte r, byte g, byte b, byte a, int textureId = 0)
	{
		MeshData meshData = new MeshData(4, 6, withNormals: false, withUv: true, withRgba: true, withFlags: false);
		for (int i = 0; i < 4; i++)
		{
			meshData.AddVertex(x + ((quadVertices[i * 3] > 0) ? dw : 0f), y + ((quadVertices[i * 3 + 1] > 0) ? dh : 0f), z, quadTextureCoords[i * 2], quadTextureCoords[i * 2 + 1], new byte[4] { r, g, b, a });
		}
		meshData.AddTextureId(textureId);
		for (int j = 0; j < 6; j++)
		{
			meshData.AddIndex(quadVertexIndices[j]);
		}
		return meshData;
	}

	public static MeshData GetCustomQuadModelDataHorizontal(float x, float y, float z, float dw, float dl, byte r, byte g, byte b, byte a)
	{
		MeshData meshData = new MeshData(4, 6, withNormals: false, withUv: true, withRgba: true, withFlags: false);
		for (int i = 0; i < 4; i++)
		{
			meshData.AddVertex(x + ((quadVertices[i * 3] > 0) ? dw : 0f), y + 0f, z + ((quadVertices[i * 3 + 2] > 0) ? dl : 0f), quadTextureCoords[i * 2], quadTextureCoords[i * 2 + 1], new byte[4] { r, g, b, a });
		}
		for (int j = 0; j < 6; j++)
		{
			meshData.AddIndex(quadVertexIndices[j]);
		}
		return meshData;
	}

	public static MeshData GetCustomQuadModelData(float u, float v, float uWidth, float vHeight, float x, float y, float dw, float dh, byte r, byte g, byte b, byte a)
	{
		MeshData meshData = new MeshData(4, 6, withNormals: false, withUv: true, withRgba: true, withFlags: false);
		for (int i = 0; i < 4; i++)
		{
			meshData.AddVertex(x + ((quadVertices[i * 3] > 0) ? dw : 0f), y + ((quadVertices[i * 3 + 1] > 0) ? dh : 0f), 0f, quadTextureCoords[i * 2], quadTextureCoords[i * 2 + 1], new byte[4] { r, g, b, a });
		}
		for (int j = 0; j < 6; j++)
		{
			meshData.AddIndex(quadVertexIndices[j]);
		}
		meshData.Uv[0] = u;
		meshData.Uv[1] = v;
		meshData.Uv[2] = u + uWidth;
		meshData.Uv[3] = v;
		meshData.Uv[4] = u + uWidth;
		meshData.Uv[5] = v + vHeight;
		meshData.Uv[6] = u;
		meshData.Uv[7] = v + vHeight;
		return meshData;
	}
}
