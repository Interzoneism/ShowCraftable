using System.Collections;
using System.Collections.Generic;

namespace Vintagestory.API.Datastructures;

public class FastLargeSetOfLongs : IEnumerable<long>, IEnumerable
{
	private int count;

	private readonly long[][] buckets;

	private readonly int[] sizes;

	private readonly int mask;

	public int Count => count;

	public void Clear()
	{
		count = 0;
		for (int i = 0; i < sizes.Length; i++)
		{
			sizes[i] = 0;
		}
	}

	public FastLargeSetOfLongs(int numbuckets)
	{
		int num = numbuckets - 1;
		int num2 = num | (num >> 1);
		int num3 = num2 | (num2 >> 2);
		int num4 = num3 | (num3 >> 4);
		int num5 = num4 | (num4 >> 8);
		numbuckets = (num5 | (num5 >> 16)) + 1;
		sizes = new int[numbuckets];
		buckets = new long[numbuckets][];
		for (int i = 0; i < buckets.Length; i++)
		{
			buckets[i] = new long[7];
		}
		mask = numbuckets - 1;
	}

	public bool Add(long value)
	{
		int num = (int)value & mask;
		long[] array = buckets[num];
		int num2 = sizes[num];
		int num3 = num2;
		while (--num3 >= 0)
		{
			if (array[num3] == value)
			{
				return false;
			}
		}
		if (num2 >= array.Length)
		{
			array = expandArray(num);
		}
		array[num2++] = value;
		sizes[num] = num2;
		count++;
		return true;
	}

	private long[] expandArray(int j)
	{
		long[] array = buckets[j];
		long[] array2 = new long[array.Length * 3 / 2 + 1];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = array[i];
		}
		buckets[j] = array2;
		return array2;
	}

	public IEnumerator<long> GetEnumerator()
	{
		for (int j = 0; j < buckets.Length; j++)
		{
			int size = sizes[j];
			long[] set = buckets[j];
			for (int i = 0; i < size; i++)
			{
				yield return set[i];
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
