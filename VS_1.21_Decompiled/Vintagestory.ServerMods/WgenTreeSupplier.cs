using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class WgenTreeSupplier
{
	private ICoreServerAPI api;

	internal TreeGenProperties treeGenProps;

	internal TreeGeneratorsUtil treeGenerators;

	private float worldheight;

	private Dictionary<TreeVariant, float> distances = new Dictionary<TreeVariant, float>();

	public WgenTreeSupplier(ICoreServerAPI api)
	{
		treeGenerators = new TreeGeneratorsUtil(api);
		this.api = api;
	}

	internal void LoadTrees()
	{
		treeGenProps = api.Assets.Get("worldgen/treengenproperties.json").ToObject<TreeGenProperties>();
		treeGenProps.descVineMinTempRel = (float)Climate.DescaleTemperature(treeGenProps.vinesMinTemp) / 255f;
		treeGenerators.LoadTreeGenerators();
		worldheight = api.WorldManager.MapSizeY;
	}

	public TreeGenInstance GetRandomTreeGenForClimate(LCGRandom rnd, int climate, int forest, int y, bool isUnderwater)
	{
		return GetRandomGenForClimate(rnd, treeGenProps.TreeGens, climate, forest, y, isUnderwater);
	}

	public TreeGenInstance GetRandomShrubGenForClimate(LCGRandom rnd, int climate, int forest, int y)
	{
		return GetRandomGenForClimate(rnd, treeGenProps.ShrubGens, climate, forest, y, isUnderwater: false);
	}

	public TreeGenInstance GetRandomGenForClimate(LCGRandom rnd, TreeVariant[] gens, int climate, int forest, int y, bool isUnderwater)
	{
		int rainFall = Climate.GetRainFall((climate >> 8) & 0xFF, y);
		int scaledAdjustedTemperature = Climate.GetScaledAdjustedTemperature((climate >> 16) & 0xFF, y - TerraGenConfig.seaLevel);
		float posYRel = ((float)y - (float)TerraGenConfig.seaLevel) / ((float)api.WorldManager.MapSizeY - (float)TerraGenConfig.seaLevel);
		int fertility = Climate.GetFertility(rainFall, scaledAdjustedTemperature, posYRel);
		float num = 0f;
		distances.Clear();
		foreach (TreeVariant treeVariant in gens)
		{
			if ((!isUnderwater || treeVariant.Habitat != EnumTreeHabitat.Land) && (isUnderwater || treeVariant.Habitat != EnumTreeHabitat.Water))
			{
				float num2 = (float)Math.Abs(fertility - treeVariant.FertMid) / treeVariant.FertRange;
				float num3 = (float)Math.Abs(rainFall - treeVariant.RainMid) / treeVariant.RainRange;
				float num4 = (float)Math.Abs(scaledAdjustedTemperature - treeVariant.TempMid) / treeVariant.TempRange;
				float num5 = (float)Math.Abs(forest - treeVariant.ForestMid) / treeVariant.ForestRange;
				float num6 = Math.Abs((float)y / worldheight - treeVariant.HeightMid) / treeVariant.HeightRange;
				double num7 = Math.Max(0f, 1.2f * num2 * num2 - 1f) + Math.Max(0f, 1.2f * num3 * num3 - 1f) + Math.Max(0f, 1.2f * num4 * num4 - 1f) + Math.Max(0f, 1.2f * num5 * num5 - 1f) + Math.Max(0f, 1.2f * num6 * num6 - 1f);
				if (!(rnd.NextDouble() < num7))
				{
					float num8 = GameMath.Clamp(1f - (num2 + num3 + num4 + num5 + num6) / 5f, 0f, 1f) * treeVariant.Weight / 100f;
					distances.Add(treeVariant, num8);
					num += num8;
				}
			}
		}
		distances = distances.Shuffle(rnd);
		double num9 = rnd.NextDouble() * (double)num;
		foreach (KeyValuePair<TreeVariant, float> distance in distances)
		{
			num9 -= (double)distance.Value;
			if (num9 <= 0.001)
			{
				float num10 = GameMath.Clamp(0.7f - distance.Value, 0f, 0.7f) * 1f / 0.7f * distance.Key.SuitabilitySizeBonus;
				float size = distance.Key.MinSize + (float)rnd.NextDouble() * (distance.Key.MaxSize - distance.Key.MinSize) + num10;
				float num11 = Climate.DescaleTemperature(scaledAdjustedTemperature);
				float num12 = Math.Max(0f, ((float)rainFall / 255f - treeGenProps.vinesMinRain) / (1f - treeGenProps.vinesMinRain));
				float num13 = Math.Max(0f, (num11 / 255f - treeGenProps.descVineMinTempRel) / (1f - treeGenProps.descVineMinTempRel));
				float num14 = (float)rainFall / 255f;
				float num15 = num11 / 255f;
				float vinesGrowthChance = 1.5f * num12 * num13 + 0.5f * num12 * GameMath.Clamp((num13 + 0.33f) / 1.33f, 0f, 1f);
				float mossGrowthChance = GameMath.Clamp((float)(2.25 * (double)num14 - 0.5 + Math.Sqrt(num15) * 3.0 * Math.Max(-0.5, 0.5 - (double)num15)), 0f, 1f);
				ITreeGenerator generator = treeGenerators.GetGenerator(distance.Key.Generator);
				if (generator == null)
				{
					api.World.Logger.Error("treengenproperties.json references tree generator {0}, but no such generator exists!", distance.Key.Generator);
					return null;
				}
				return new TreeGenInstance
				{
					treeGen = generator,
					size = size,
					vinesGrowthChance = vinesGrowthChance,
					mossGrowthChance = mossGrowthChance
				};
			}
		}
		return null;
	}
}
