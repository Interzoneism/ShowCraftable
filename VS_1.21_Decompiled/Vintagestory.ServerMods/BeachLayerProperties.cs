using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class BeachLayerProperties
{
	[JsonProperty]
	public float Strength;

	[JsonProperty]
	public AssetLocation BlockCode;

	public Dictionary<int, int> BlockIdMapping;

	public int BlockId;

	public void ResolveBlockIds(ICoreServerAPI api, RockStrataConfig rockstrata)
	{
		if (BlockCode != null && BlockCode.Path.Length > 0)
		{
			if (BlockCode.Path.Contains("{rocktype}"))
			{
				BlockIdMapping = new Dictionary<int, int>();
				for (int i = 0; i < rockstrata.Variants.Length; i++)
				{
					if (!rockstrata.Variants[i].IsDeposit)
					{
						string newValue = rockstrata.Variants[i].BlockCode.Path.Split('-')[1];
						Block block = api.World.GetBlock(rockstrata.Variants[i].BlockCode);
						Block block2 = api.World.GetBlock(BlockCode.CopyWithPath(BlockCode.Path.Replace("{rocktype}", newValue)));
						if (block != null && block2 != null)
						{
							BlockIdMapping[block.BlockId] = block2.BlockId;
						}
					}
				}
			}
			else
			{
				BlockId = api.WorldManager.GetBlockId(BlockCode);
			}
		}
		else
		{
			BlockCode = null;
		}
	}
}
