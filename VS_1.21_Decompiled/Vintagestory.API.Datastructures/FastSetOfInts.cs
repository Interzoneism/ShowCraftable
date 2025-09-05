using System.Collections;
using System.Collections.Generic;

namespace Vintagestory.API.Datastructures;

public class FastSetOfInts : IEnumerable<int>, IEnumerable
{
	private int size;

	private int[] set;

	public int Count => size;

	public void Clear()
	{
		size = 0;
	}

	public FastSetOfInts()
	{
		set = new int[27];
	}

	public bool Add(int a, int b, int c, int d)
	{
		return Add(a + 128 + (b + 128 << 8) + (c + 128 << 16) + (d << 24));
	}

	public bool Add(int value)
	{
		int num = size;
		while (--num >= 0)
		{
			if (set[num] == value)
			{
				return false;
			}
		}
		if (size >= set.Length)
		{
			expandArray();
		}
		set[size++] = value;
		return true;
	}

	public void RemoveIfMatches(int a, int b, int c, int d)
	{
		int num = size;
		int num2 = a + 128 + (b + 128 << 8) + (c + 128 << 16) + (d << 24);
		while (--num >= 0)
		{
			if (set[num] == num2)
			{
				RemoveAt(num);
				break;
			}
		}
	}

	private void RemoveAt(int i)
	{
		for (int j = i + 1; j < size; j++)
		{
			set[j - 1] = set[j];
		}
		size--;
	}

	private void expandArray()
	{
		int[] array = new int[set.Length * 3 / 2 + 1];
		for (int i = 0; i < set.Length; i++)
		{
			array[i] = set[i];
		}
		set = array;
	}

	public IEnumerator<int> GetEnumerator()
	{
		for (int i = 0; i < size; i++)
		{
			yield return set[i];
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
