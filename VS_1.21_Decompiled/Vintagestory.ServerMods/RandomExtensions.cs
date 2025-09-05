using System;

namespace Vintagestory.ServerMods;

internal static class RandomExtensions
{
	public static void Shuffle<T>(this Random rng, T[] array)
	{
		int num = array.Length;
		while (num > 1)
		{
			int num2 = rng.Next(num--);
			T val = array[num];
			array[num] = array[num2];
			array[num2] = val;
		}
	}
}
