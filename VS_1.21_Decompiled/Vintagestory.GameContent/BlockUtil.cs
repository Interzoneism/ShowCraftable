using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public static class BlockUtil
{
	public static ItemStack[] GetKnifeStacks(ICoreAPI api)
	{
		return ObjectCacheUtil.GetOrCreate(api, "knifeStacks", () => (from item in api.World.Items
			where item.Tool == EnumTool.Knife
			select new ItemStack(item)).ToArray());
	}
}
