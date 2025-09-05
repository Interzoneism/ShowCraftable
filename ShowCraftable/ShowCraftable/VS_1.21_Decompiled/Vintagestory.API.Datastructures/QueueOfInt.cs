using System;

namespace Vintagestory.API.Datastructures;

public class QueueOfInt
{
	public int Count;

	public int maxSize = 27;

	protected int tail;

	protected int head;

	protected int[] array;

	public QueueOfInt()
	{
		array = new int[maxSize];
	}

	public void Clear()
	{
		Count = 0;
		head = 0;
		tail = 0;
	}

	public void Enqueue(int a, int b, int c, int d)
	{
		Enqueue(a + 128 + (b + 128 << 8) + (c + 128 << 16) + (d << 24));
	}

	public void Enqueue(int v)
	{
		if (Count == maxSize)
		{
			expandArray();
		}
		array[tail++ % maxSize] = v;
		Count++;
	}

	public void EnqueueIfLarger(int a, int b, int c, int d)
	{
		EnqueueIfLarger(a + 128 + (b + 128 << 8) + (c + 128 << 16) + (d << 24));
	}

	public void EnqueueIfLarger(int v)
	{
		int num = head % maxSize;
		int num2 = tail % maxSize;
		int num3 = v & 0xFFFFFF;
		if (num2 >= num)
		{
			for (int i = num; i < num2; i++)
			{
				if ((array[i] & 0xFFFFFF) == num3)
				{
					ReplaceIfLarger(i, v);
					return;
				}
			}
		}
		else
		{
			for (int j = 0; j < num2; j++)
			{
				if ((array[j] & 0xFFFFFF) == num3)
				{
					ReplaceIfLarger(j, v);
					return;
				}
			}
			for (int k = num; k < array.Length; k++)
			{
				if ((array[k] & 0xFFFFFF) == num3)
				{
					ReplaceIfLarger(k, v);
					return;
				}
			}
		}
		Enqueue(v);
	}

	private void ReplaceIfLarger(int i, int v)
	{
		if ((array[i] & 0x1F000000) < (v & 0x1F000000))
		{
			array[i] = v;
		}
	}

	public int Dequeue()
	{
		Count--;
		return array[head++ % maxSize];
	}

	public int DequeueLIFO()
	{
		Count--;
		int result = array[tail-- % maxSize];
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
		int[] dest = new int[maxSize];
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

	private void ArrayCopy(int[] src, int srcOffset, int[] dest, int destOffset, int len)
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
