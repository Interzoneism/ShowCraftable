using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

internal class NoiseLandforms : NoiseBase
{
	public static LandformsWorldProperty landforms;

	public float scale;

	public NoiseLandforms(long seed, ICoreServerAPI api, float scale)
		: base(seed)
	{
		LoadLandforms(api);
		this.scale = scale;
	}

	public static void LoadLandforms(ICoreServerAPI api)
	{
		landforms = api.Assets.Get("worldgen/landforms.json").ToObject<LandformsWorldProperty>();
		int num = 0;
		for (int i = 0; i < landforms.Variants.Length; i++)
		{
			LandformVariant landformVariant = landforms.Variants[i];
			landformVariant.index = i;
			landformVariant.Init(api.WorldManager, i);
			if (landformVariant.Mutations != null)
			{
				num += landformVariant.Mutations.Length;
			}
		}
		landforms.LandFormsByIndex = new LandformVariant[num + landforms.Variants.Length];
		for (int j = 0; j < landforms.Variants.Length; j++)
		{
			landforms.LandFormsByIndex[j] = landforms.Variants[j];
		}
		int num2 = landforms.Variants.Length;
		for (int k = 0; k < landforms.Variants.Length; k++)
		{
			LandformVariant landformVariant2 = landforms.Variants[k];
			if (landformVariant2.Mutations == null)
			{
				continue;
			}
			for (int l = 0; l < landformVariant2.Mutations.Length; l++)
			{
				LandformVariant landformVariant3 = landformVariant2.Mutations[l];
				if (landformVariant3.TerrainOctaves == null)
				{
					landformVariant3.TerrainOctaves = landformVariant2.TerrainOctaves;
				}
				if (landformVariant3.TerrainOctaveThresholds == null)
				{
					landformVariant3.TerrainOctaveThresholds = landformVariant2.TerrainOctaveThresholds;
				}
				if (landformVariant3.TerrainYKeyPositions == null)
				{
					landformVariant3.TerrainYKeyPositions = landformVariant2.TerrainYKeyPositions;
				}
				if (landformVariant3.TerrainYKeyThresholds == null)
				{
					landformVariant3.TerrainYKeyThresholds = landformVariant2.TerrainYKeyThresholds;
				}
				landforms.LandFormsByIndex[num2] = landformVariant3;
				landformVariant3.Init(api.WorldManager, num2);
				num2++;
			}
		}
	}

	public int GetLandformIndexAt(int unscaledXpos, int unscaledZpos, int temp, int rain)
	{
		float num = (float)unscaledXpos / scale;
		float num2 = (float)unscaledZpos / scale;
		int xpos = (int)num;
		int zpos = (int)num2;
		int parentLandformIndexAt = GetParentLandformIndexAt(xpos, zpos, temp, rain);
		LandformVariant[] mutations = landforms.Variants[parentLandformIndexAt].Mutations;
		if (mutations != null && mutations.Length != 0)
		{
			InitPositionSeed(unscaledXpos / 2, unscaledZpos / 2);
			float num3 = (float)NextInt(101) / 100f;
			for (int i = 0; i < mutations.Length; i++)
			{
				LandformVariant landformVariant = mutations[i];
				if (landformVariant.UseClimateMap)
				{
					int num4 = rain - GameMath.Clamp(rain, landformVariant.MinRain, landformVariant.MaxRain);
					double num5 = (float)temp - GameMath.Clamp(temp, landformVariant.MinTemp, landformVariant.MaxTemp);
					if (num4 != 0 || num5 != 0.0)
					{
						continue;
					}
				}
				num3 -= mutations[i].Chance;
				if (num3 <= 0f)
				{
					return mutations[i].index;
				}
			}
		}
		return parentLandformIndexAt;
	}

	public int GetParentLandformIndexAt(int xpos, int zpos, int temp, int rain)
	{
		InitPositionSeed(xpos, zpos);
		double num = 0.0;
		int i;
		for (i = 0; i < landforms.Variants.Length; i++)
		{
			double num2 = landforms.Variants[i].Weight;
			if (landforms.Variants[i].UseClimateMap)
			{
				int num3 = rain - GameMath.Clamp(rain, landforms.Variants[i].MinRain, landforms.Variants[i].MaxRain);
				double num4 = (float)temp - GameMath.Clamp(temp, landforms.Variants[i].MinTemp, landforms.Variants[i].MaxTemp);
				if (num3 != 0 || num4 != 0.0)
				{
					num2 = 0.0;
				}
			}
			landforms.Variants[i].WeightTmp = num2;
			num += num2;
		}
		double num5 = num * (double)NextInt(10000) / 10000.0;
		for (i = 0; i < landforms.Variants.Length; i++)
		{
			num5 -= landforms.Variants[i].WeightTmp;
			if (num5 <= 0.0)
			{
				return landforms.Variants[i].index;
			}
		}
		return landforms.Variants[i].index;
	}
}
