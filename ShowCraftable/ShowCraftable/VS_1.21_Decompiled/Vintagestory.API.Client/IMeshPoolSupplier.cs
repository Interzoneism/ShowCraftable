namespace Vintagestory.API.Client;

public interface IMeshPoolSupplier
{
	MeshData GetMeshPoolForPass(int textureid, EnumChunkRenderPass forRenderPass, int lodLevel);
}
