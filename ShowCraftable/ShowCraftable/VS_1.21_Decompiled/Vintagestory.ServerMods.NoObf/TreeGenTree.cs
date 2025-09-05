using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods.NoObf;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class TreeGenTree
{
	public ushort vinesBlockId;

	public ushort logBlockId;

	public ushort leavesBlockId;

	public ushort leavesBranchyBlockId;

	[JsonProperty]
	public EnumTreeGenMode mode;

	[JsonProperty]
	private AssetLocation logBlockCode;

	[JsonProperty]
	private AssetLocation leavesBlockCode;

	[JsonProperty]
	private AssetLocation leavesBranchyBlockCode;

	public void ResolveBlockNames(ICoreServerAPI api)
	{
		int num = api.WorldManager.GetBlockId(logBlockCode);
		if (num == -1)
		{
			api.Server.LogWarning("Tree gen tree: No block found with the blockcode " + logBlockCode);
			num = 0;
		}
		logBlockId = (ushort)num;
		int num2 = api.WorldManager.GetBlockId(leavesBlockCode);
		if (num2 == -1)
		{
			api.Server.LogWarning("Tree gen tree: No block found with the blockcode " + leavesBlockCode);
			num2 = 0;
		}
		leavesBlockId = (ushort)num2;
		int num3 = api.WorldManager.GetBlockId(leavesBranchyBlockCode);
		if (num3 == -1)
		{
			api.Server.LogWarning("Tree gen tree: No block found with the blockcode " + leavesBranchyBlockCode);
			num3 = 0;
		}
		leavesBranchyBlockId = (ushort)num3;
	}
}
