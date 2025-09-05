using System.Collections;
using System.Collections.Generic;

namespace Vintagestory.API.Datastructures;

public class UniqueQueue<T> : IEnumerable<T>, IEnumerable
{
	private HashSet<T> hashSet;

	private Queue<T> queue;

	public int Count => hashSet.Count;

	public UniqueQueue()
	{
		hashSet = new HashSet<T>();
		queue = new Queue<T>();
	}

	public void Clear()
	{
		hashSet.Clear();
		queue.Clear();
	}

	public bool Contains(T item)
	{
		return hashSet.Contains(item);
	}

	public void Enqueue(T item)
	{
		if (hashSet.Add(item))
		{
			queue.Enqueue(item);
		}
	}

	public T Dequeue()
	{
		T val = queue.Dequeue();
		hashSet.Remove(val);
		return val;
	}

	public T Peek()
	{
		return queue.Peek();
	}

	public IEnumerator<T> GetEnumerator()
	{
		return queue.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return queue.GetEnumerator();
	}
}
