using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods.NoObf;

public class TreeGenConfig
{
	[JsonProperty]
	public int yOffset;

	[JsonProperty]
	public float sizeMultiplier;

	[JsonProperty]
	public NatFloat sizeVar = NatFloat.Zero;

	[JsonProperty]
	public float heightMultiplier;

	[JsonProperty]
	public TreeGenTrunk[] trunks;

	[JsonProperty]
	public TreeGenBranch[] branches;

	[JsonProperty]
	public TreeGenBlocks treeBlocks;

	public EnumTreeType Treetype;

	internal void Init(AssetLocation location, ILogger logger)
	{
		if (trunks == null)
		{
			trunks = Array.Empty<TreeGenTrunk>();
		}
		if (branches == null)
		{
			branches = Array.Empty<TreeGenBranch>();
		}
		for (int i = 1; i < trunks.Length; i++)
		{
			if (trunks[i].inherit != null)
			{
				Inheritance inherit = trunks[i].inherit;
				if (inherit.from >= i || inherit.from < 0)
				{
					logger.Warning(string.Concat("Inheritance value out of bounds in trunk element ", i.ToString(), " in ", location, ". Skipping."));
				}
				else
				{
					trunks[i].InheritFrom(trunks[inherit.from], inherit.skip);
				}
			}
		}
		for (int j = 1; j < branches.Length; j++)
		{
			if (branches[j].inherit != null)
			{
				Inheritance inherit2 = branches[j].inherit;
				if (inherit2.from >= j || inherit2.from < 0)
				{
					logger.Warning(string.Concat("Inheritance value out of bounds in branch element ", j.ToString(), " in ", location, ". Skipping."));
				}
				else
				{
					branches[j].InheritFrom(branches[inherit2.from], inherit2.skip);
				}
			}
		}
	}
}
