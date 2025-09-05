using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Util;

public static class DictExtensions
{
	public static void AddRange<K, V>(this IDictionary<K, V> dict, IDictionary<K, V> elems)
	{
		foreach (KeyValuePair<K, V> elem in elems)
		{
			dict[elem.Key] = elem.Value;
		}
	}

	public static V Get<K, V>(this IDictionary<K, V> dict, K key, V defaultValue = default(V))
	{
		if (dict.TryGetValue(key, out var value))
		{
			return value;
		}
		return defaultValue;
	}

	public static void Remove<K, V>(this CachingConcurrentDictionary<K, V> dict, K key)
	{
		dict.TryRemove(key, out var _);
	}

	public static void Remove<K, V>(this ConcurrentDictionary<K, V> dict, K key)
	{
		dict.TryRemove(key, out var _);
	}

	public static void RemoveAll<K, V>(this IDictionary<K, V> dict, Func<K, V, bool> predicate)
	{
		foreach (K item in from key in dict.Keys.ToArray()
			where predicate(key, dict[key])
			select key)
		{
			dict.Remove(item);
		}
	}

	public static void RemoveAllByKey<K, V>(this IDictionary<K, V> dict, Func<K, bool> predicate)
	{
		foreach (K item in from key in dict.Keys.ToArray()
			where predicate(key)
			select key)
		{
			dict.Remove(item);
		}
	}
}
