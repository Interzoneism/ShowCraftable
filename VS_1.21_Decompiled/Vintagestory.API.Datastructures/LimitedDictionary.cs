using System.Collections.Generic;

namespace Vintagestory.API.Datastructures;

public class LimitedDictionary<TKey, TValue>
{
	private Dictionary<TKey, TValue> dictionary;

	private Queue<TKey> keys;

	private int capacity;

	public TValue this[TKey key]
	{
		get
		{
			dictionary.TryGetValue(key, out var value);
			return value;
		}
		set
		{
			if (!dictionary.ContainsKey(key))
			{
				if (dictionary.Count == capacity)
				{
					TKey key2 = keys.Dequeue();
					dictionary.Remove(key2);
				}
				keys.Enqueue(key);
			}
			dictionary[key] = value;
		}
	}

	public int Count => keys.Count;

	public LimitedDictionary(int maxCapacity)
	{
		keys = new Queue<TKey>(maxCapacity);
		capacity = maxCapacity;
		dictionary = new Dictionary<TKey, TValue>(maxCapacity);
	}

	public void Add(TKey key, TValue value)
	{
		if (dictionary.Count == capacity)
		{
			TKey key2 = keys.Dequeue();
			dictionary.Remove(key2);
		}
		dictionary.Add(key, value);
		keys.Enqueue(key);
	}
}
