using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Datastructures;

public class RingArray<T> : IEnumerable<T>, IEnumerable
{
	private T[] elements;

	private int cursor;

	public T this[int index]
	{
		get
		{
			return elements[index];
		}
		set
		{
			elements[index] = value;
		}
	}

	public int EndPosition
	{
		get
		{
			return cursor;
		}
		set
		{
			cursor = value;
		}
	}

	public T[] Values => elements;

	public int Length => elements.Length;

	public RingArray(int capacity)
	{
		elements = new T[capacity];
	}

	public RingArray(int capacity, T[] initialvalues)
	{
		elements = new T[capacity];
		if (initialvalues != null)
		{
			for (int i = 0; i < initialvalues.Length; i++)
			{
				Add(initialvalues[i]);
			}
		}
	}

	public void Add(T elem)
	{
		elements[cursor] = elem;
		cursor = (cursor + 1) % elements.Length;
	}

	public IEnumerator<T> GetEnumerator()
	{
		for (int i = 0; i < elements.Length; i++)
		{
			yield return elements[(cursor + i) % elements.Length];
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
		cursor = 0;
	}

	public void ResizeTo(int size)
	{
		T[] array = new T[size];
		for (int i = 0; i < elements.Length; i++)
		{
			array[size - 1] = elements[GameMath.Mod(EndPosition - i, elements.Length)];
			size--;
			if (size <= 0)
			{
				break;
			}
		}
		elements = array;
	}
}
