using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods.NoObf;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class TreeGenBlocks
{
	[JsonProperty]
	public AssetLocation logBlockCode;

	[JsonProperty]
	public AssetLocation otherLogBlockCode;

	[JsonProperty]
	public double otherLogChance = 0.01;

	[JsonProperty]
	public AssetLocation leavesBlockCode;

	[JsonProperty]
	public AssetLocation leavesBranchyBlockCode;

	[JsonProperty]
	public AssetLocation vinesBlockCode;

	[JsonProperty]
	public AssetLocation mossDecorCode;

	[JsonProperty]
	public AssetLocation vinesEndBlockCode;

	[JsonProperty]
	public string trunkSegmentBase;

	[JsonProperty]
	public string[] trunkSegmentVariants;

	[JsonProperty]
	public int leavesLevels;

	public Block mossDecorBlock;

	public Block vinesBlock;

	public Block vinesEndBlock;

	public int logBlockId;

	public int otherLogBlockId;

	public int leavesBlockId;

	public int leavesBranchyBlockId;

	public int leavesBranchyDeadBlockId;

	public int[] trunkSegmentBlockIds;

	private float leafLevelFactor = 5f;

	private int[][] leavesByLevel = new int[2][];

	public HashSet<int> blockIds = new HashSet<int>();

	public void ResolveBlockNames(ICoreServerAPI api, string treeName)
	{
		int num = api.WorldManager.GetBlockId(logBlockCode);
		if (num == -1)
		{
			api.Server.LogWarning("Tree gen tree " + treeName + ": No block found with the blockcode " + logBlockCode);
			num = 0;
		}
		logBlockId = num;
		if (otherLogBlockCode != null)
		{
			int num2 = api.WorldManager.GetBlockId(otherLogBlockCode);
			if (num2 == -1)
			{
				api.Server.LogWarning("Tree gen tree " + treeName + ": No block found with the blockcode " + otherLogBlockCode);
				num2 = 0;
			}
			otherLogBlockId = num2;
		}
		int num3 = api.WorldManager.GetBlockId(leavesBlockCode);
		if (num3 == -1)
		{
			api.Server.LogWarning("Tree gen tree " + treeName + ": No block found with the blockcode " + leavesBlockCode);
			num3 = 0;
		}
		leavesBlockId = num3;
		int num4 = api.WorldManager.GetBlockId(leavesBranchyBlockCode);
		if (num4 == -1)
		{
			api.Server.LogWarning("Tree gen tree " + treeName + ": No block found with the blockcode " + leavesBranchyBlockCode);
			num4 = 0;
		}
		leavesBranchyBlockId = num4;
		if (vinesBlockCode != null)
		{
			int blockId = api.WorldManager.GetBlockId(vinesBlockCode);
			if (blockId == -1)
			{
				api.Server.LogWarning("Tree gen tree " + treeName + ": No block found with the blockcode " + vinesBlockCode);
			}
			else
			{
				vinesBlock = api.World.Blocks[blockId];
			}
		}
		if (mossDecorCode != null)
		{
			mossDecorBlock = api.World.GetBlock(mossDecorCode);
			if (mossDecorBlock == null)
			{
				api.Server.LogWarning("Tree gen tree " + treeName + ": No decor block found with the blockcode " + mossDecorCode);
			}
		}
		if (vinesEndBlockCode != null)
		{
			int blockId2 = api.WorldManager.GetBlockId(vinesEndBlockCode);
			if (blockId2 == -1)
			{
				api.Server.LogWarning("Tree gen tree " + treeName + ": No block found with the blockcode " + vinesEndBlockCode);
			}
			else
			{
				vinesEndBlock = api.World.Blocks[blockId2];
			}
		}
		if (trunkSegmentVariants != null && trunkSegmentVariants.Length != 0 && trunkSegmentBase != null)
		{
			trunkSegmentBlockIds = new int[trunkSegmentVariants.Length];
			for (int i = 0; i < trunkSegmentVariants.Length; i++)
			{
				string domainAndPath = trunkSegmentBase + trunkSegmentVariants[i] + "-ud";
				trunkSegmentBlockIds[i] = api.WorldManager.GetBlockId(new AssetLocation(domainAndPath));
				blockIds.Add(trunkSegmentBlockIds[i]);
			}
		}
		if (leavesLevels == 0)
		{
			int num5 = 0;
			if (leavesBlockCode.SecondCodePart() == "grown" && leavesBranchyBlockCode.Path != "log-grown-baldcypress-ud")
			{
				int[] array = new int[7];
				int[] array2 = new int[7];
				for (int j = 1; j < 8; j++)
				{
					int blockId3 = api.WorldManager.GetBlockId(new AssetLocation(leavesBlockCode.Domain, leavesBlockCode.FirstCodePart() + "-grown" + j + "-" + leavesBlockCode.CodePartsAfterSecond()));
					int blockId4 = api.WorldManager.GetBlockId(new AssetLocation(leavesBranchyBlockCode.Domain, leavesBranchyBlockCode.FirstCodePart() + "-grown" + j + "-" + leavesBranchyBlockCode.CodePartsAfterSecond()));
					if (blockId3 == 0 || blockId4 == 0)
					{
						break;
					}
					num5++;
					array[j - 1] = blockId3;
					array2[j - 1] = blockId4;
				}
				leavesByLevel[0] = new int[num5];
				leavesByLevel[1] = new int[num5];
				for (int k = 0; k < num5; k++)
				{
					leavesByLevel[0][k] = array[k];
					leavesByLevel[1][k] = array2[k];
					blockIds.Add(array[k]);
					blockIds.Add(array2[k]);
				}
			}
			if (num5 == 0)
			{
				leavesByLevel[0] = new int[1];
				leavesByLevel[1] = new int[1];
				leavesByLevel[0][0] = num3;
				leavesByLevel[1][0] = num4;
				blockIds.Add(num3);
				blockIds.Add(num4);
			}
		}
		else
		{
			leavesByLevel = new int[leavesLevels][];
			Block block = api.World.Blocks[num3];
			for (int l = 0; l < leavesLevels; l++)
			{
				leavesByLevel[l] = new int[1] { api.WorldManager.GetBlockId(block.CodeWithParts((l + 1).ToString())) };
				blockIds.Add(leavesByLevel[l][0]);
			}
			leafLevelFactor = ((float)leavesLevels - 0.5f) / 0.3f;
		}
		blockIds.Add(num);
		if (otherLogBlockId != 0)
		{
			blockIds.Add(otherLogBlockId);
		}
	}

	public int GetLeaves(float width, int treeSubType)
	{
		int[] array = leavesByLevel[Math.Min(leavesByLevel.Length - 1, (int)(width * leafLevelFactor + 0.5f))];
		return array[treeSubType % array.Length];
	}
}
