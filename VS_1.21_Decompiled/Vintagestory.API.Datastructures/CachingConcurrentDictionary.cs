using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Vintagestory.API.Datastructures;

public class CachingConcurrentDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
{
	private ICollection<TValue> valuesCached;

	public new ICollection<TValue> Values => valuesCached ?? (valuesCached = base.Values);

	public new TValue this[TKey key]
	{
		get
		{
			if (!TryGetValue(key, out var value))
			{
				throw new KeyNotFoundException();
			}
			return value;
		}
		set
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			TryAdd(key, value);
		}
	}

	public new bool TryAdd(TKey key, TValue value)
	{
		bool num = base.TryAdd(key, value);
		if (num)
		{
			valuesCached = null;
		}
		return num;
	}

	public new bool TryRemove(TKey key, out TValue value)
	{
		bool num = base.TryRemove(key, out value);
		if (num)
		{
			valuesCached = null;
		}
		return num;
	}
}
