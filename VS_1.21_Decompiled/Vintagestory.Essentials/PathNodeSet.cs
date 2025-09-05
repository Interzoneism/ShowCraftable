using System.Collections;
using System.Collections.Generic;

namespace Vintagestory.Essentials;

public class PathNodeSet : IEnumerable<PathNode>, IEnumerable
{
	private int arraySize = 16;

	private PathNode[][] buckets = new PathNode[4][];

	private int[] bucketCount = new int[4];

	private int size;

	public int Count => size;

	public void Clear()
	{
		for (int i = 0; i < 4; i++)
		{
			bucketCount[i] = 0;
		}
		size = 0;
	}

	public PathNodeSet()
	{
		for (int i = 0; i < 4; i++)
		{
			buckets[i] = new PathNode[arraySize];
		}
	}

	public bool Add(PathNode value)
	{
		int num = value.Z % 2 * 2 + value.X % 2;
		num = (num + 4) % 4;
		PathNode[] array = buckets[num];
		int num2 = bucketCount[num];
		int num3 = num2;
		while (--num3 >= 0)
		{
			if (value.Equals(array[num3]))
			{
				return false;
			}
		}
		if (num2 >= arraySize)
		{
			ExpandArrays();
			array = buckets[num];
		}
		float fCost = value.fCost;
		num3 = num2 - 1;
		while (num3 >= 0 && (array[num3].fCost < fCost || (array[num3].fCost == fCost && array[num3].hCost < value.hCost)))
		{
			num3--;
		}
		num3++;
		int num4 = num2;
		while (num4 > num3)
		{
			array[num4] = array[--num4];
		}
		array[num3] = value;
		num2++;
		bucketCount[num] = num2;
		size++;
		return true;
	}

	public PathNode RemoveNearest()
	{
		if (size == 0)
		{
			return null;
		}
		PathNode pathNode = null;
		int num = 0;
		for (int i = 0; i < 4; i++)
		{
			int num2 = bucketCount[i] - 1;
			if (num2 >= 0)
			{
				PathNode pathNode2 = buckets[i][num2];
				if ((object)pathNode == null || pathNode2.fCost < pathNode.fCost || (pathNode2.fCost == pathNode.fCost && pathNode2.hCost < pathNode.hCost))
				{
					pathNode = pathNode2;
					num = i;
				}
			}
		}
		bucketCount[num]--;
		size--;
		return pathNode;
	}

	public void Remove(PathNode value)
	{
		int num = value.Z % 2 * 2 + value.X % 2;
		num = (num + 4) % 4;
		PathNode[] array = buckets[num];
		int num2 = bucketCount[num];
		int num3 = num2;
		while (--num3 >= 0)
		{
			if (value.Equals(array[num3]))
			{
				num2 = --bucketCount[num];
				while (num3 < num2)
				{
					array[num3] = array[++num3];
				}
				size--;
				break;
			}
		}
	}

	public PathNode TryFindValue(PathNode value)
	{
		int num = value.Z % 2 * 2 + value.X % 2;
		num = (num + 4) % 4;
		PathNode[] array = buckets[num];
		int num2 = bucketCount[num];
		while (--num2 >= 0)
		{
			if (value.Equals(array[num2]))
			{
				return array[num2];
			}
		}
		return null;
	}

	private void ExpandArrays()
	{
		int num = arraySize * 3 / 2;
		for (int i = 0; i < 4; i++)
		{
			PathNode[] array = new PathNode[num];
			int num2 = bucketCount[i];
			PathNode[] array2 = buckets[i];
			for (int j = 0; j < num2; j++)
			{
				array[j] = array2[j];
			}
			buckets[i] = array;
		}
		arraySize = num;
	}

	public IEnumerator<PathNode> GetEnumerator()
	{
		for (int bucket = 0; bucket < 4; bucket++)
		{
			for (int i = 0; i < bucketCount[bucket]; i++)
			{
				yield return buckets[bucket][i];
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
