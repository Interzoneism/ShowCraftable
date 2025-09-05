using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IBlockEntityContainer
{
	IInventory Inventory { get; }

	string InventoryClassName { get; }

	void DropContents(Vec3d atPos);
}
