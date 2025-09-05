using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods.NoObf;

public class LandformVariant : WorldPropertyVariant
{
	[JsonIgnore]
	public int index;

	[JsonIgnore]
	public float[] TerrainYThresholds;

	[JsonIgnore]
	public int ColorInt;

	[JsonIgnore]
	public double WeightTmp;

	[JsonProperty]
	public string HexColor;

	[JsonProperty]
	public double Weight;

	[JsonProperty]
	public bool UseClimateMap;

	[JsonProperty]
	public float MinTemp = -50f;

	[JsonProperty]
	public float MaxTemp = 50f;

	[JsonProperty]
	public int MinRain;

	[JsonProperty]
	public int MaxRain = 255;

	[JsonProperty]
	public bool UseWindMap;

	[JsonProperty]
	public int MinWindStrength;

	[JsonProperty]
	public int MaxWindStrength;

	[JsonProperty]
	public double[] TerrainOctaves;

	[JsonProperty]
	public double[] TerrainOctaveThresholds = new double[11];

	[JsonProperty]
	public float[] TerrainYKeyPositions;

	[JsonProperty]
	public float[] TerrainYKeyThresholds;

	[JsonProperty]
	public LandformVariant[] Mutations = Array.Empty<LandformVariant>();

	[JsonProperty]
	public float Chance;

	private static Random rnd = new Random();

	public void Init(IWorldManagerAPI api, int index)
	{
		this.index = index;
		expandOctaves(api);
		LerpThresholds(api.MapSizeY);
		ColorInt = rnd.Next(int.MaxValue) | -16777216;
	}

	protected void expandOctaves(IWorldManagerAPI api)
	{
		int terrainOctaveCount = TerraGenConfig.GetTerrainOctaveCount(api.MapSizeY);
		int num = terrainOctaveCount - TerrainOctaves.Length;
		if (num > 0)
		{
			double[] array = new double[num].Fill(TerrainOctaves[TerrainOctaves.Length - 1]);
			double num2 = 0.0;
			for (int i = 0; i < array.Length; i++)
			{
				double num3 = Math.Pow(0.8, i + 1);
				array[i] *= num3;
				num2 += num3;
			}
			double num4 = 0.0;
			for (int j = 0; j < TerrainOctaves.Length; j++)
			{
				num4 += TerrainOctaves[j];
			}
			for (int k = 0; k < TerrainOctaves.Length; k++)
			{
				TerrainOctaves[k] *= (num4 + num2) / num4;
			}
			TerrainOctaves = TerrainOctaves.Append(array);
		}
		int num5 = terrainOctaveCount - TerrainOctaveThresholds.Length;
		if (num5 > 0)
		{
			TerrainOctaveThresholds = TerrainOctaveThresholds.Append(new double[num5].Fill(TerrainOctaveThresholds[TerrainOctaveThresholds.Length - 1]));
		}
	}

	private void LerpThresholds(int mapSizeY)
	{
		TerrainYThresholds = new float[mapSizeY];
		float v = 1f;
		float num = 0f;
		int num2 = -1;
		for (int i = 0; i < mapSizeY; i++)
		{
			if (num2 + 1 >= TerrainYKeyThresholds.Length)
			{
				TerrainYThresholds[i] = 1f;
				continue;
			}
			if ((float)i >= TerrainYKeyPositions[num2 + 1] * (float)mapSizeY)
			{
				v = TerrainYKeyThresholds[num2 + 1];
				num = TerrainYKeyPositions[num2 + 1] * (float)mapSizeY;
				num2++;
			}
			float v2 = 0f;
			float num3 = mapSizeY;
			if (num2 + 1 < TerrainYKeyThresholds.Length)
			{
				v2 = TerrainYKeyThresholds[num2 + 1];
				num3 = TerrainYKeyPositions[num2 + 1] * (float)mapSizeY;
			}
			float num4 = num3 - num;
			float t = ((float)i - num) / num4;
			if (num4 == 0f)
			{
				string text = "";
				for (int j = 0; j < TerrainYKeyPositions.Length; j++)
				{
					if (j > 0)
					{
						text += ", ";
					}
					text += TerrainYKeyPositions[j] * (float)mapSizeY;
				}
				throw new Exception(string.Concat("Illegal TerrainYKeyPositions in landforms.js, Landform ", Code, ", key positions must be more than 0 blocks apart. Translated key positions for this maps world height: ", text));
			}
			TerrainYThresholds[i] = 1f - GameMath.Lerp(v, v2, t);
		}
	}

	public float[] AddTerrainNoiseThresholds(float[] thresholds, float weight)
	{
		for (int i = 0; i < thresholds.Length; i++)
		{
			thresholds[i] += weight * TerrainYThresholds[i];
		}
		return thresholds;
	}
}
