using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Util;

public static class ListExtensions
{
	public static void Shuffle<T>(this List<T> array, Random rand)
	{
		int num = array.Count;
		while (num > 1)
		{
			int index = rand.Next(num);
			num--;
			T value = array[num];
			array[num] = array[index];
			array[index] = value;
		}
	}

	public static void Shuffle<T>(this List<T> array, IRandom rand)
	{
		int num = array.Count;
		while (num > 1)
		{
			int index = rand.NextInt(num);
			num--;
			T value = array[num];
			array[num] = array[index];
			array[index] = value;
		}
	}
}
