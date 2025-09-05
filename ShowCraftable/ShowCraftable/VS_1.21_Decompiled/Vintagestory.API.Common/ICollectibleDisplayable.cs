using Vintagestory.API.Client;

namespace Vintagestory.API.Common;

public interface ICollectibleDisplayable
{
	MeshData GetMeshDataForDisplay(ItemSlot inSlot, string displayType);

	void NowOnDisplay(BlockEntity byBlockEntity, ItemSlot inSlot);
}
