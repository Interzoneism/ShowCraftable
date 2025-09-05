using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class WorldGenStructuresConfigBase
{
	[JsonProperty]
	public Dictionary<string, Dictionary<AssetLocation, AssetLocation>> RocktypeRemapGroups;

	[JsonProperty]
	public Dictionary<string, int> SchematicYOffsets;

	public Dictionary<string, Dictionary<int, Dictionary<int, int>>> resolvedRocktypeRemapGroups;

	public void ResolveRemaps(ICoreServerAPI api, RockStrataConfig rockstrata)
	{
		if (RocktypeRemapGroups == null)
		{
			return;
		}
		resolvedRocktypeRemapGroups = new Dictionary<string, Dictionary<int, Dictionary<int, int>>>();
		foreach (KeyValuePair<string, Dictionary<AssetLocation, AssetLocation>> rocktypeRemapGroup in RocktypeRemapGroups)
		{
			resolvedRocktypeRemapGroups[rocktypeRemapGroup.Key] = ResolveRockTypeRemaps(rocktypeRemapGroup.Value, rockstrata, api);
		}
	}

	public static Dictionary<int, Dictionary<int, int>> ResolveRockTypeRemaps(Dictionary<AssetLocation, AssetLocation> rockTypeRemaps, RockStrataConfig rockstrata, ICoreAPI api)
	{
		Dictionary<int, Dictionary<int, int>> dictionary = new Dictionary<int, Dictionary<int, int>>();
		foreach (KeyValuePair<AssetLocation, AssetLocation> rockTypeRemap in rockTypeRemaps)
		{
			RockStratum[] variants;
			Block[] array;
			if (rockTypeRemap.Key.Path.Contains("*"))
			{
				array = api.World.SearchBlocks(rockTypeRemap.Key);
				foreach (Block block in array)
				{
					Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
					variants = rockstrata.Variants;
					foreach (RockStratum rockStratum in variants)
					{
						Block block2 = api.World.GetBlock(rockStratum.BlockCode);
						AssetLocation blockCode = block.CodeWithVariant("rock", block2.LastCodePart());
						Block block3 = api.World.GetBlock(blockCode);
						if (block3 != null)
						{
							dictionary2[block2.Id] = block3.Id;
						}
					}
					dictionary[block.Id] = dictionary2;
				}
				continue;
			}
			Dictionary<int, int> dictionary3 = new Dictionary<int, int>();
			variants = rockstrata.Variants;
			foreach (RockStratum rockStratum2 in variants)
			{
				Block block4 = api.World.GetBlock(rockStratum2.BlockCode);
				AssetLocation assetLocation = rockTypeRemap.Value.Clone();
				assetLocation.Path = assetLocation.Path.Replace("{rock}", block4.LastCodePart());
				Block block5 = api.World.GetBlock(assetLocation);
				if (block5 != null)
				{
					dictionary3[block4.Id] = block5.Id;
					Block block6 = api.World.GetBlock(new AssetLocation("ore-quartz-" + block4.LastCodePart()));
					if (block6 != null)
					{
						dictionary3[block6.Id] = block5.Id;
					}
				}
			}
			array = api.World.SearchBlocks(rockTypeRemap.Key);
			foreach (Block block7 in array)
			{
				dictionary[block7.Id] = dictionary3;
			}
		}
		return dictionary;
	}
}
