using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface IShelvable
{
	EnumShelvableLayout? GetShelvableType(ItemStack stack)
	{
		return EnumShelvableLayout.Quadrants;
	}

	ModelTransform? GetOnShelfTransform(ItemStack stack)
	{
		return null;
	}
}
