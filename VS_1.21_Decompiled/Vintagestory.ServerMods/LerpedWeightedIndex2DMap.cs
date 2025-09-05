using System;
using System.Collections.Generic;
using System.Linq;

namespace Vintagestory.ServerMods;

public class LerpedWeightedIndex2DMap
{
	public int sizeX;

	public int topleftPadding;

	public int botRightPadding;

	private WeightedIndex[][] groups;

	public WeightedIndex[] this[float x, float z]
	{
		get
		{
			int num = (int)Math.Floor(x - 0.5f);
			int num2 = num + 1;
			int num3 = (int)Math.Floor(z - 0.5f);
			int num4 = num3 + 1;
			float lerp = x - ((float)num + 0.5f);
			float lerp2 = z - ((float)num3 + 0.5f);
			WeightedIndex[] left = Lerp(groups[(num3 + topleftPadding) * sizeX + num + topleftPadding], groups[(num3 + topleftPadding) * sizeX + num2 + topleftPadding], lerp);
			WeightedIndex[] right = Lerp(groups[(num4 + topleftPadding) * sizeX + num + topleftPadding], groups[(num4 + topleftPadding) * sizeX + num2 + topleftPadding], lerp);
			return LerpSorted(left, right, lerp2);
		}
	}

	public LerpedWeightedIndex2DMap(int[] discreteValues2d, int sizeX)
	{
		this.sizeX = sizeX;
		groups = new WeightedIndex[discreteValues2d.Length][];
		for (int i = 0; i < discreteValues2d.Length; i++)
		{
			groups[i] = new WeightedIndex[1]
			{
				new WeightedIndex
				{
					Index = discreteValues2d[i],
					Weight = 1f
				}
			};
		}
	}

	public LerpedWeightedIndex2DMap(int[] rawScalarValues, int sizeX, int boxBlurRadius, int dataTopLeftPadding, int dataBotRightPadding)
	{
		this.sizeX = sizeX;
		topleftPadding = dataTopLeftPadding;
		botRightPadding = dataBotRightPadding;
		groups = new WeightedIndex[rawScalarValues.Length][];
		Dictionary<int, float> dictionary = new Dictionary<int, float>();
		for (int i = 0; i < sizeX; i++)
		{
			for (int j = 0; j < sizeX; j++)
			{
				int num = Math.Max(0, i - boxBlurRadius);
				int num2 = Math.Max(0, j - boxBlurRadius);
				int num3 = Math.Min(sizeX - 1, i + boxBlurRadius);
				int num4 = Math.Min(sizeX - 1, j + boxBlurRadius);
				dictionary.Clear();
				float num5 = 1f / (float)((num3 - num + 1) * (num4 - num2 + 1));
				for (int k = num; k <= num3; k++)
				{
					for (int l = num2; l <= num4; l++)
					{
						int key = rawScalarValues[l * sizeX + k];
						if (dictionary.TryGetValue(key, out var value))
						{
							dictionary[key] = num5 + value;
						}
						else
						{
							dictionary[key] = num5;
						}
					}
				}
				groups[j * sizeX + i] = new WeightedIndex[dictionary.Count];
				int num6 = 0;
				foreach (KeyValuePair<int, float> item in dictionary)
				{
					groups[j * sizeX + i][num6++] = new WeightedIndex
					{
						Index = item.Key,
						Weight = item.Value
					};
				}
			}
		}
	}

	public float[] WeightsAt(float x, float z, float[] output)
	{
		for (int i = 0; i < output.Length; i++)
		{
			output[i] = 0f;
		}
		int num = (int)Math.Floor(x - 0.5f) + topleftPadding;
		int num2 = num + 1;
		int num3 = (int)Math.Floor(z - 0.5f) + topleftPadding;
		int num4 = num3 + 1;
		float lerp = x - ((float)(num - topleftPadding) + 0.5f);
		float num5 = z - ((float)(num3 - topleftPadding) + 0.5f);
		HalfBiLerp(groups[num3 * sizeX + num], groups[num3 * sizeX + num2], lerp, output, 1f - num5);
		HalfBiLerp(groups[num4 * sizeX + num], groups[num4 * sizeX + num2], lerp, output, num5);
		return output;
	}

	private WeightedIndex[] Lerp(WeightedIndex[] left, WeightedIndex[] right, float lerp)
	{
		Dictionary<int, WeightedIndex> dictionary = new Dictionary<int, WeightedIndex>();
		for (int i = 0; i < left.Length; i++)
		{
			int index = left[i].Index;
			dictionary.TryGetValue(index, out var value);
			dictionary[index] = new WeightedIndex(index, value.Weight + (1f - lerp) * left[i].Weight);
		}
		for (int j = 0; j < right.Length; j++)
		{
			int index2 = right[j].Index;
			dictionary.TryGetValue(index2, out var value2);
			dictionary[index2] = new WeightedIndex(index2, value2.Weight + lerp * right[j].Weight);
		}
		return dictionary.Values.ToArray();
	}

	private WeightedIndex[] LerpSorted(WeightedIndex[] left, WeightedIndex[] right, float lerp)
	{
		SortedDictionary<int, WeightedIndex> sortedDictionary = new SortedDictionary<int, WeightedIndex>();
		for (int i = 0; i < left.Length; i++)
		{
			int index = left[i].Index;
			sortedDictionary.TryGetValue(index, out var value);
			sortedDictionary[index] = new WeightedIndex
			{
				Index = index,
				Weight = value.Weight + (1f - lerp) * left[i].Weight
			};
		}
		for (int j = 0; j < right.Length; j++)
		{
			int index2 = right[j].Index;
			sortedDictionary.TryGetValue(index2, out var value2);
			sortedDictionary[index2] = new WeightedIndex
			{
				Index = index2,
				Weight = value2.Weight + lerp * right[j].Weight
			};
		}
		return sortedDictionary.Values.ToArray();
	}

	public void Split(WeightedIndex[] weightedIndices, out int[] indices, out float[] weights)
	{
		indices = new int[weightedIndices.Length];
		weights = new float[weightedIndices.Length];
		for (int i = 0; i < weightedIndices.Length; i++)
		{
			indices[i] = weightedIndices[i].Index;
			weights[i] = weightedIndices[i].Weight;
		}
	}

	private void HalfBiLerp(WeightedIndex[] left, WeightedIndex[] right, float lerp, float[] output, float overallweight)
	{
		for (int i = 0; i < left.Length; i++)
		{
			int index = left[i].Index;
			output[index] += (1f - lerp) * left[i].Weight * overallweight;
		}
		for (int j = 0; j < right.Length; j++)
		{
			int index2 = right[j].Index;
			output[index2] += lerp * right[j].Weight * overallweight;
		}
	}
}
