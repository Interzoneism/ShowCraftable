using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface IHandBookPageCodeProvider
{
	string HandbookPageCodeForStack(IWorldAccessor world, ItemStack stack);
}
