using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class LineMeshUtil
{
	public static MeshData GetRectangle(int color = 0)
	{
		MeshData meshData = new MeshData();
		meshData.SetMode(EnumDrawMode.Lines);
		meshData.xyz = new float[12];
		meshData.Rgba = new byte[16];
		meshData.Indices = new int[8];
		AddLineLoop(meshData, new Vec3f(-1f, -1f, 0f), new Vec3f(-1f, 1f, 0f), new Vec3f(1f, 1f, 0f), new Vec3f(1f, -1f, 0f), color);
		return meshData;
	}

	public static MeshData GetCube(int color = 0)
	{
		MeshData meshData = new MeshData();
		meshData.SetMode(EnumDrawMode.Lines);
		meshData.xyz = new float[72];
		meshData.Rgba = new byte[96];
		meshData.Indices = new int[48];
		AddLineLoop(meshData, new Vec3f(-1f, -1f, -1f), new Vec3f(-1f, 1f, -1f), new Vec3f(1f, 1f, -1f), new Vec3f(1f, -1f, -1f), color);
		AddLineLoop(meshData, new Vec3f(-1f, -1f, -1f), new Vec3f(1f, -1f, -1f), new Vec3f(1f, -1f, 1f), new Vec3f(-1f, -1f, 1f), color);
		AddLineLoop(meshData, new Vec3f(-1f, -1f, -1f), new Vec3f(-1f, -1f, 1f), new Vec3f(-1f, 1f, 1f), new Vec3f(-1f, 1f, -1f), color);
		AddLineLoop(meshData, new Vec3f(-1f, -1f, 1f), new Vec3f(1f, -1f, 1f), new Vec3f(1f, 1f, 1f), new Vec3f(-1f, 1f, 1f), color);
		AddLineLoop(meshData, new Vec3f(-1f, 1f, -1f), new Vec3f(-1f, 1f, 1f), new Vec3f(1f, 1f, 1f), new Vec3f(1f, 1f, -1f), color);
		AddLineLoop(meshData, new Vec3f(1f, -1f, -1f), new Vec3f(1f, 1f, -1f), new Vec3f(1f, 1f, 1f), new Vec3f(1f, -1f, 1f), color);
		return meshData;
	}

	public static void AddLine2D(MeshData m, float x1, float y1, float x2, float y2, int color)
	{
		int verticesCount = m.GetVerticesCount();
		AddVertex(m, x1, y1, 50f, color);
		AddVertex(m, x2, y2, 50f, color);
		m.Indices[m.IndicesCount++] = verticesCount;
		m.Indices[m.IndicesCount++] = verticesCount + 1;
	}

	public static void AddLineLoop(MeshData m, Vec3f p0, Vec3f p1, Vec3f p2, Vec3f p3, int color)
	{
		int verticesCount = m.GetVerticesCount();
		AddVertex(m, p0.X, p0.Y, p0.Z, color);
		AddVertex(m, p1.X, p1.Y, p1.Z, color);
		AddVertex(m, p2.X, p2.Y, p2.Z, color);
		AddVertex(m, p3.X, p3.Y, p3.Z, color);
		m.Indices[m.IndicesCount++] = verticesCount;
		m.Indices[m.IndicesCount++] = verticesCount + 1;
		m.Indices[m.IndicesCount++] = verticesCount + 1;
		m.Indices[m.IndicesCount++] = verticesCount + 2;
		m.Indices[m.IndicesCount++] = verticesCount + 2;
		m.Indices[m.IndicesCount++] = verticesCount + 3;
		m.Indices[m.IndicesCount++] = verticesCount + 3;
		m.Indices[m.IndicesCount++] = verticesCount;
	}

	public static void AddVertex(MeshData model, float x, float y, float z, int color)
	{
		model.xyz[model.XyzCount] = x;
		model.xyz[model.XyzCount + 1] = y;
		model.xyz[model.XyzCount + 2] = z;
		model.Rgba[model.RgbaCount] = ColorUtil.ColorR(color);
		model.Rgba[model.RgbaCount + 1] = ColorUtil.ColorG(color);
		model.Rgba[model.RgbaCount + 2] = ColorUtil.ColorB(color);
		model.Rgba[model.RgbaCount + 3] = ColorUtil.ColorA(color);
		model.VerticesCount++;
	}
}
