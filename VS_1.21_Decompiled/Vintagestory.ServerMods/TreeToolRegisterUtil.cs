using Vintagestory.API.Common;
using Vintagestory.ServerMods.WorldEdit;

namespace Vintagestory.ServerMods;

public static class TreeToolRegisterUtil
{
	public static void Register(ModSystem mod)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((WorldEdit)mod).RegisterTool("TreeGen", typeof(TreeGenTool));
	}
}
