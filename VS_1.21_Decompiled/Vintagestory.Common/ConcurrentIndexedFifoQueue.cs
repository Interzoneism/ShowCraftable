using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

internal class ConcurrentIndexedFifoQueue<T> : IEnumerable<T>, IEnumerable where T : ILongIndex
{
	internal readonly ConcurrentDictionary<long, T> elementsByIndex;

	private readonly T[] elements;

	private readonly int length;

	private volatile uint start;

	private volatile uint end;

	private volatile uint startBleedingEdge;

	private volatile uint endBleedingEdge;

	public int Count => (int)(end - start);

	public int Capacity => elements.Length;

	public ConcurrentIndexedFifoQueue(int capacity, int stages)
	{
		capacity = Math.Min(ArrayConvert.GetRoundedUpSize(capacity), 65536);
		elements = new T[capacity];
		length = capacity;
		elementsByIndex = new ConcurrentDictionary<long, T>(stages, capacity);
	}

	public bool IsFull()
	{
		return (int)(end - start) >= length;
	}

	public bool IsEmpty()
	{
		return start == end;
	}

	public T GetByIndex(long index)
	{
		elementsByIndex.TryGetValue(index, out var value);
		return value;
	}

	public void Enqueue(T elem)
	{
		elementsByIndex[elem.Index] = elem;
		EnqueueWithoutAddingToIndex(elem);
	}

	internal void EnqueueWithoutAddingToIndex(T elem)
	{
		uint num = endBleedingEdge;
		if ((int)(num - start) > length - 1)
		{
			throw new Exception("Indexed Fifo Queue overflow. Try increasing servermagicnumbers RequestChunkColumnsQueueSize?");
		}
		uint num2;
		while ((num2 = Interlocked.CompareExchange(ref endBleedingEdge, num + 1, num)) != num)
		{
			num = num2;
			if ((int)(num2 - start) > length - 1)
			{
				throw new Exception("Indexed Fifo Queue overflow. Try increasing servermagicnumbers RequestChunkColumnsQueueSize?");
			}
		}
		elements[(ushort)num % length] = elem;
		Interlocked.Increment(ref end);
		if (!elementsByIndex.ContainsKey(elem.Index))
		{
			throw new Exception("In queue but missed from index!");
		}
	}

	public T DequeueWithoutRemovingFromIndex()
	{
		uint num = startBleedingEdge;
		if ((int)(num - end) >= 0)
		{
			return default(T);
		}
		uint num2;
		while ((num2 = Interlocked.CompareExchange(ref startBleedingEdge, num + 1, num)) != num)
		{
			num = num2;
			if ((int)(num2 - end) >= 0)
			{
				return default(T);
			}
		}
		T result = elements[(ushort)num % length];
		Interlocked.Increment(ref start);
		elements[(ushort)num % length] = default(T);
		return result;
	}

	public T Dequeue()
	{
		T val = DequeueWithoutRemovingFromIndex();
		if (val != null)
		{
			elementsByIndex.Remove(val.Index);
		}
		return val;
	}

	internal void Requeue()
	{
		if (IsFull())
		{
			Interlocked.Increment(ref endBleedingEdge);
			Interlocked.Increment(ref end);
			Interlocked.Increment(ref startBleedingEdge);
			Interlocked.Increment(ref start);
		}
		else
		{
			T val = DequeueWithoutRemovingFromIndex();
			if (val != null)
			{
				EnqueueWithoutAddingToIndex(val);
			}
		}
	}

	public T Peek()
	{
		return elements[(ushort)startBleedingEdge % length];
	}

	public T PeekAtPosition(int position)
	{
		return elements[(ushort)((int)startBleedingEdge + position) % length];
	}

	public bool Remove(long index)
	{
		if (elementsByIndex.TryRemove(index, out var value))
		{
			value.FlagToDispose();
			return true;
		}
		return false;
	}

	public IEnumerator<T> GetEnumerator()
	{
		uint end_snapshot = end;
		for (uint pos = startBleedingEdge; pos != end_snapshot; pos++)
		{
			if ((int)(startBleedingEdge - pos) > 0)
			{
				pos = startBleedingEdge;
				if ((int)(pos - end_snapshot) >= 0)
				{
					break;
				}
			}
			yield return elements[(ushort)pos % length];
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public ICollection<T> Snapshot()
	{
		return elementsByIndex.Values;
	}

	public void Clear()
	{
		elementsByIndex.Clear();
		start = 0u;
		startBleedingEdge = 0u;
		end = 0u;
		endBleedingEdge = 0u;
		for (int i = 0; i < elements.Length; i++)
		{
			elements[i] = default(T);
		}
	}
}
