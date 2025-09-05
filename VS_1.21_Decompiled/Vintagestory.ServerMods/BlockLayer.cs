using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class BlockLayer
{
	[JsonProperty]
	public string Name;

	[JsonProperty]
	public string ID;

	[JsonProperty]
	public AssetLocation BlockCode;

	[JsonProperty]
	public BlockLayerCodeByMin[] BlockCodeByMin;

	[JsonProperty]
	public int MinTemp = -30;

	[JsonProperty]
	public int MaxTemp = 40;

	[JsonProperty]
	public float MinRain;

	[JsonProperty]
	public float MaxRain = 1f;

	[JsonProperty]
	public float MinFertility;

	[JsonProperty]
	public float MaxFertility = 1f;

	[JsonProperty]
	public float MinY;

	[JsonProperty]
	public float MaxY = 1f;

	[JsonProperty]
	public int Thickness = 1;

	[JsonProperty]
	public double[] NoiseAmplitudes;

	[JsonProperty]
	public double[] NoiseFrequencies;

	[JsonProperty]
	public double NoiseThreshold = 0.5;

	private NormalizedSimplexNoise noiseGen;

	public int BlockId;

	public Dictionary<int, int> BlockIdMapping;

	public void Init(ICoreServerAPI api, RockStrataConfig rockstrata, Random rnd)
	{
		ResolveBlockIds(api, rockstrata);
		if (NoiseAmplitudes != null)
		{
			noiseGen = new NormalizedSimplexNoise(NoiseAmplitudes, NoiseFrequencies, rnd.Next());
		}
	}

	public bool NoiseOk(BlockPos pos)
	{
		if (noiseGen != null)
		{
			return noiseGen.Noise((double)pos.X / 10.0, (double)pos.Y / 10.0, (double)pos.Z / 10.0) > NoiseThreshold;
		}
		return true;
	}

	private void ResolveBlockIds(ICoreServerAPI api, RockStrataConfig rockstrata)
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
		if (BlockCodeByMin == null)
		{
			return;
		}
		for (int j = 0; j < BlockCodeByMin.Length; j++)
		{
			AssetLocation blockCode = BlockCodeByMin[j].BlockCode;
			if (blockCode.Path.Contains("{rocktype}"))
			{
				BlockCodeByMin[j].BlockIdMapping = new Dictionary<int, int>();
				for (int k = 0; k < rockstrata.Variants.Length; k++)
				{
					string newValue2 = rockstrata.Variants[k].BlockCode.Path.Split('-')[1];
					Block block3 = api.World.GetBlock(rockstrata.Variants[k].BlockCode);
					Block block4 = api.World.GetBlock(blockCode.CopyWithPath(blockCode.Path.Replace("{rocktype}", newValue2)));
					if (block3 != null && block4 != null)
					{
						BlockCodeByMin[j].BlockIdMapping[block3.BlockId] = block4.BlockId;
					}
				}
			}
			else
			{
				BlockCodeByMin[j].BlockId = api.WorldManager.GetBlockId(blockCode);
			}
		}
	}

	public int GetBlockId(double posRand, float temp, float rainRel, float fertilityRel, int firstBlockId, BlockPos pos, int mapheight)
	{
		if (noiseGen != null && noiseGen.Noise((double)pos.X / 20.0, (double)pos.Y / 20.0, (double)pos.Z / 20.0) < NoiseThreshold)
		{
			return 0;
		}
		if (BlockCode != null)
		{
			int value = BlockId;
			if (BlockIdMapping != null)
			{
				BlockIdMapping.TryGetValue(firstBlockId, out value);
			}
			return value;
		}
		float num = (float)pos.Y / (float)mapheight;
		for (int i = 0; i < BlockCodeByMin.Length; i++)
		{
			BlockLayerCodeByMin blockLayerCodeByMin = BlockCodeByMin[i];
			float num2 = Math.Abs(temp - GameMath.Max(temp, blockLayerCodeByMin.MinTemp));
			float num3 = Math.Abs(rainRel - GameMath.Max(rainRel, blockLayerCodeByMin.MinRain));
			float num4 = Math.Abs(fertilityRel - GameMath.Clamp(fertilityRel, blockLayerCodeByMin.MinFertility, blockLayerCodeByMin.MaxFertility));
			float num5 = Math.Abs(num - GameMath.Clamp(num, blockLayerCodeByMin.MinY, blockLayerCodeByMin.MaxY)) * 10f;
			if ((double)(num2 + num3 + num4 + num5) <= posRand)
			{
				int value2 = blockLayerCodeByMin.BlockId;
				if (blockLayerCodeByMin.BlockIdMapping != null)
				{
					blockLayerCodeByMin.BlockIdMapping.TryGetValue(firstBlockId, out value2);
				}
				return value2;
			}
		}
		return 0;
	}

	public float CalcTrfDistance(float temperature, float rainRel, float fertilityRel)
	{
		float num = Math.Abs(temperature - GameMath.Clamp(temperature, MinTemp, MaxTemp));
		float num2 = Math.Abs(rainRel - GameMath.Clamp(rainRel, MinRain, MaxRain)) * 10f;
		float num3 = Math.Abs(fertilityRel - GameMath.Clamp(fertilityRel, MinFertility, MaxFertility)) * 10f;
		return num + num2 + num3;
	}

	public float CalcYDistance(int posY, int mapheight)
	{
		float num = (float)posY / (float)mapheight;
		return Math.Abs(num - GameMath.Clamp(num, MinY, MaxY)) * 10f;
	}
}
