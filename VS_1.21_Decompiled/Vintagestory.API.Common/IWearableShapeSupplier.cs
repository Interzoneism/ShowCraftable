using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Common;

public interface IWearableShapeSupplier
{
	Shape GetShape(ItemStack stack, Entity forEntity, string texturePrefixCode);
}
