using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods.NoObf;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class BlockPatchConfig
{
	[JsonProperty]
	public NatFloat ChanceMultiplier;

	[JsonProperty]
	public BlockPatch[] Patches;

	public BlockPatch[] PatchesNonTree;

	internal void ResolveBlockIds(ICoreServerAPI api, RockStrataConfig rockstrata, LCGRandom rnd)
	{
		List<BlockPatch> list = new List<BlockPatch>();
		for (int i = 0; i < Patches.Length; i++)
		{
			BlockPatch blockPatch = Patches[i];
			if (blockPatch.Placement != EnumBlockPatchPlacement.OnTrees && blockPatch.Placement != EnumBlockPatchPlacement.UnderTrees)
			{
				list.Add(blockPatch);
			}
			blockPatch.Init(api, rockstrata, rnd, i);
		}
		PatchesNonTree = list.ToArray();
	}

	public bool IsPatchSuitableAt(BlockPatch patch, Block onBlock, int mapSizeY, int climate, int y, float forestRel, float shrubRel)
	{
		if ((patch.Placement == EnumBlockPatchPlacement.NearWater || patch.Placement == EnumBlockPatchPlacement.UnderWater) && onBlock.LiquidCode != "water")
		{
			return false;
		}
		if ((patch.Placement == EnumBlockPatchPlacement.NearSeaWater || patch.Placement == EnumBlockPatchPlacement.UnderSeaWater) && onBlock.LiquidCode != "saltwater")
		{
			return false;
		}
		if (forestRel < patch.MinForest || forestRel > patch.MaxForest || shrubRel < patch.MinShrub || forestRel > patch.MaxShrub)
		{
			return false;
		}
		int rainFall = Climate.GetRainFall((climate >> 8) & 0xFF, y);
		float num = (float)rainFall / 255f;
		if (num < patch.MinRain || num > patch.MaxRain)
		{
			return false;
		}
		int scaledAdjustedTemperature = Climate.GetScaledAdjustedTemperature((climate >> 16) & 0xFF, y - TerraGenConfig.seaLevel);
		if (scaledAdjustedTemperature < patch.MinTemp || scaledAdjustedTemperature > patch.MaxTemp)
		{
			return false;
		}
		float num2 = ((float)y - (float)TerraGenConfig.seaLevel) / ((float)mapSizeY - (float)TerraGenConfig.seaLevel);
		if (num2 < patch.MinY || num2 > patch.MaxY)
		{
			return false;
		}
		float num3 = (float)Climate.GetFertility(rainFall, scaledAdjustedTemperature, num2) / 255f;
		if (num3 >= patch.MinFertility)
		{
			return num3 <= patch.MaxFertility;
		}
		return false;
	}

	public bool IsPatchSuitableUnderTree(BlockPatch patch, int mapSizeY, ClimateCondition climate, int y)
	{
		float rainfall = climate.Rainfall;
		if (rainfall < patch.MinRain || rainfall > patch.MaxRain)
		{
			return false;
		}
		float temperature = climate.Temperature;
		if (temperature < (float)patch.MinTemp || temperature > (float)patch.MaxTemp)
		{
			return false;
		}
		float num = ((float)y - (float)TerraGenConfig.seaLevel) / ((float)mapSizeY - (float)TerraGenConfig.seaLevel);
		if (num < patch.MinY || num > patch.MaxY)
		{
			return false;
		}
		float fertility = climate.Fertility;
		if (fertility >= patch.MinFertility)
		{
			return fertility <= patch.MaxFertility;
		}
		return false;
	}
}
