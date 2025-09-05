using System;

namespace Vintagestory.API.Datastructures;

public class SortableQueue<T> where T : IComparable<T>
{
	public int Count;

	public int maxSize = 27;

	protected int tail;

	protected int head;

	protected T[] array;

	public SortableQueue()
	{
		array = new T[maxSize];
	}

	public void Clear()
	{
		Count = 0;
		head = 0;
		tail = 0;
		Array.Clear(array, 0, maxSize);
	}

	public void Enqueue(T v)
	{
		if (Count == maxSize)
		{
			expandArray();
		}
		array[tail++ % maxSize] = v;
		Count++;
	}

	public void EnqueueOrMerge(T v)
	{
		if (head % maxSize > tail % maxSize)
		{
			for (int i = head % maxSize; i < maxSize; i++)
			{
				if (((IMergeable<T>)(object)array[i]).MergeIfEqual(v))
				{
					return;
				}
			}
			for (int j = 0; j < tail % maxSize; j++)
			{
				if (((IMergeable<T>)(object)array[j]).MergeIfEqual(v))
				{
					return;
				}
			}
		}
		else
		{
			for (int k = head % maxSize; k < tail % maxSize; k++)
			{
				if (((IMergeable<T>)(object)array[k]).MergeIfEqual(v))
				{
					return;
				}
			}
		}
		Enqueue(v);
	}

	public T Dequeue()
	{
		Count--;
		int num = head++ % maxSize;
		T result = array[num];
		array[num] = default(T);
		return result;
	}

	public void Sort()
	{
		if (head % maxSize > tail % maxSize)
		{
			T[] dest = new T[maxSize];
			int num = maxSize - head % maxSize;
			ArrayCopy(array, head % maxSize, dest, 0, num);
			ArrayCopy(array, 0, dest, num, tail % maxSize);
			Array.Clear(array, 0, maxSize);
			array = dest;
			head = 0;
			tail = tail % maxSize + num;
		}
		int num2 = head % maxSize;
		Array.Sort(array, num2, tail % maxSize - num2);
	}

	public void RunForEach(Action<T> action)
	{
		if (head % maxSize > tail % maxSize)
		{
			for (int i = head % maxSize; i < maxSize; i++)
			{
				action(array[i]);
			}
			for (int j = 0; j < tail % maxSize; j++)
			{
				action(array[j]);
			}
		}
		else
		{
			for (int k = head % maxSize; k < tail % maxSize; k++)
			{
				action(array[k]);
			}
		}
	}

	public T DequeueLIFO()
	{
		Count--;
		T result = array[tail-- % maxSize];
		if (tail < 0)
		{
			tail += maxSize;
		}
		return result;
	}

	private void expandArray()
	{
		head %= maxSize;
		int num = maxSize;
		maxSize = maxSize * 2 + 1;
		T[] dest = new T[maxSize];
		if (head == 0)
		{
			ArrayCopy(array, 0, dest, 0, num);
		}
		else
		{
			num -= head;
			ArrayCopy(array, head, dest, 0, num);
			ArrayCopy(array, 0, dest, num, head);
		}
		array = dest;
		head = 0;
		tail = Count;
	}

	private void ArrayCopy(T[] src, int srcOffset, T[] dest, int destOffset, int len)
	{
		if (len > 128)
		{
			Array.Copy(src, srcOffset, dest, destOffset, len);
			return;
		}
		int num = srcOffset;
		int num2 = destOffset;
		int num3 = len / 4 * 4 + srcOffset;
		while (num < num3)
		{
			dest[num2] = src[num];
			dest[num2 + 1] = src[num + 1];
			dest[num2 + 2] = src[num + 2];
			dest[num2 + 3] = src[num + 3];
			num += 4;
			num2 += 4;
		}
		len += srcOffset;
		while (num < len)
		{
			dest[num2++] = src[num++];
		}
	}
}
