using System;
using System.Collections;
using System.Collections.Generic;

namespace Vintagestory.Common;

internal class IndexedFifoQueue<T> : IEnumerable<T>, IEnumerable where T : ILongIndex
{
	private readonly Dictionary<long, T> elementsByIndex;

	private readonly T[] elements;

	private int start;

	private int end;

	private bool isfull;

	public int Count
	{
		get
		{
			if (end >= start && (!isfull || end != start))
			{
				return end - start;
			}
			return elements.Length - start + end;
		}
	}

	public int Capacity => elements.Length;

	public IndexedFifoQueue(int capacity)
	{
		elements = new T[capacity];
		elementsByIndex = new Dictionary<long, T>(capacity);
	}

	public bool IsFull()
	{
		return isfull;
	}

	public T GetByIndex(long index)
	{
		elementsByIndex.TryGetValue(index, out var value);
		return value;
	}

	public T GetAtPosition(int position)
	{
		return elements[(start + position) % elements.Length];
	}

	public void Enqueue(T elem)
	{
		if (Count >= elements.Length - 1)
		{
			throw new Exception("Indexed Fifo Queue overflow");
		}
		elements[end] = elem;
		elementsByIndex[elem.Index] = elem;
		end++;
		if (end >= elements.Length)
		{
			end = 0;
		}
		isfull = start == end;
	}

	public T Dequeue()
	{
		T result = elements[start];
		elements[start] = default(T);
		elementsByIndex.Remove(result.Index);
		if (start != end)
		{
			start++;
		}
		if (start >= elements.Length)
		{
			start = 0;
		}
		isfull = false;
		return result;
	}

	internal void Requeue()
	{
		T val = Dequeue();
		if (val != null)
		{
			Enqueue(val);
		}
	}

	public T Peek()
	{
		return elements[start];
	}

	public bool Remove(long index)
	{
		bool flag = false;
		elementsByIndex.Remove(index);
		for (int i = 0; i < Count; i++)
		{
			int num = (i + start) % elements.Length;
			if (elements[num].Index == index)
			{
				flag = true;
			}
			if (flag)
			{
				int num2 = (num + 1) % elements.Length;
				if (elements[num2] == null)
				{
					break;
				}
				elements[num] = elements[num2];
			}
		}
		if (flag)
		{
			end--;
			if (end < 0)
			{
				end += elements.Length;
			}
			elements[end] = default(T);
			isfull = false;
		}
		return flag;
	}

	public IEnumerator<T> GetEnumerator()
	{
		for (int i = 0; i < Count; i++)
		{
			int num = i + start;
			if (num >= elements.Length)
			{
				num -= elements.Length;
			}
			yield return elements[num];
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Clear()
	{
		for (int i = 0; i < elements.Length; i++)
		{
			elements[i] = default(T);
		}
		elementsByIndex.Clear();
		start = 0;
		end = 0;
		isfull = false;
	}
}
