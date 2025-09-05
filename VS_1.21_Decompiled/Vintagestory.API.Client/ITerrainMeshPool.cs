namespace Vintagestory.API.Client;

public interface ITerrainMeshPool
{
	void AddMeshData(MeshData data, int lodLevel = 1);

	void AddMeshData(MeshData data, float[] tfMatrix, int lodLevel = 1);

	void AddMeshData(MeshData data, ColorMapData colorMapData, int lodLevel = 1);
}
