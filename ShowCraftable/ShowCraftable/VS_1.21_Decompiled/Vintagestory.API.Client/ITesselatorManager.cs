using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public interface ITesselatorManager
{
	MeshData GetDefaultBlockMesh(Block block);

	MultiTextureMeshRef GetDefaultBlockMeshRef(Block block);

	MultiTextureMeshRef GetDefaultItemMeshRef(Item block);

	Shape GetCachedShape(AssetLocation location);

	MeshData CreateMesh(string typeForLogging, CompositeShape cshape, TextureSourceBuilder texgen, ITexPositionSource texSource = null);

	void ThreadDispose();
}
