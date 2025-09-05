using System;
using System.Collections;
using System.Collections.Generic;

namespace Vintagestory.API.Datastructures;

public class FastSmallDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
{
	private TKey[] keys;

	private TValue[] values;

	private int count;

	public ICollection<TKey> Keys
	{
		get
		{
			TKey[] array = new TKey[count];
			Array.Copy(keys, array, count);
			return array;
		}
	}

	public ICollection<TValue> Values
	{
		get
		{
			TValue[] array = new TValue[count];
			Array.Copy(values, array, count);
			return array;
		}
	}

	int ICollection<KeyValuePair<TKey, TValue>>.Count => count;

	public bool IsReadOnly => false;

	public int Count => count;

	public TValue this[TKey key]
	{
		get
		{
			for (int i = 0; i < keys.Length && i < count; i++)
			{
				if (key.Equals(keys[i]))
				{
					return values[i];
				}
			}
			throw new KeyNotFoundException("The key " + key.ToString() + " was not found");
		}
		set
		{
			for (int i = 0; i < count; i++)
			{
				if (key.Equals(keys[i]))
				{
					values[i] = value;
					return;
				}
			}
			if (count == keys.Length)
			{
				ExpandArrays();
			}
			keys[count] = key;
			values[count++] = value;
		}
	}

	public FastSmallDictionary(int size)
	{
		keys = ((size == 0) ? Array.Empty<TKey>() : new TKey[size]);
		values = ((size == 0) ? Array.Empty<TValue>() : new TValue[size]);
	}

	public FastSmallDictionary(TKey key, TValue value)
		: this(1)
	{
		keys[0] = key;
		values[0] = value;
		count = 1;
	}

	public FastSmallDictionary(IDictionary<TKey, TValue> dict)
		: this(dict.Count)
	{
		foreach (KeyValuePair<TKey, TValue> item in dict)
		{
			Add(item);
		}
	}

	public FastSmallDictionary<TKey, TValue> Clone()
	{
		FastSmallDictionary<TKey, TValue> fastSmallDictionary = new FastSmallDictionary<TKey, TValue>(count);
		fastSmallDictionary.keys = new TKey[count];
		fastSmallDictionary.values = new TValue[count];
		fastSmallDictionary.count = count;
		Array.Copy(keys, fastSmallDictionary.keys, count);
		Array.Copy(values, fastSmallDictionary.values, count);
		return fastSmallDictionary;
	}

	public TKey GetFirstKey()
	{
		return keys[0];
	}

	public TValue TryGetValue(string key)
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			if (key.Equals(keys[i]))
			{
				return values[i];
			}
		}
		return default(TValue);
	}

	private void ExpandArrays()
	{
		int num = keys.Length + 3;
		TKey[] array = new TKey[num];
		TValue[] array2 = new TValue[num];
		for (int i = 0; i < keys.Length; i++)
		{
			array[i] = keys[i];
			array2[i] = values[i];
		}
		values = array2;
		keys = array;
	}

	public bool ContainsKey(TKey key)
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			if (key.Equals(keys[i]))
			{
				return true;
			}
		}
		return false;
	}

	public void Add(TKey key, TValue value)
	{
		if (count == keys.Length)
		{
			ExpandArrays();
		}
		keys[count] = key;
		values[count++] = value;
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			if (key.Equals(keys[i]))
			{
				value = values[i];
				return true;
			}
		}
		value = default(TValue);
		return false;
	}

	public void Clear()
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			keys[i] = default(TKey);
			values[i] = default(TValue);
		}
		count = 0;
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		for (int i = 0; i < count; i++)
		{
			yield return new KeyValuePair<TKey, TValue>(keys[i], values[i]);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Add(KeyValuePair<TKey, TValue> item)
	{
		Add(item.Key, item.Value);
	}

	internal void AddIfNotPresent(TKey key, TValue value)
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			if (key.Equals(keys[i]))
			{
				return;
			}
		}
		Add(key, value);
	}

	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			if (item.Key.Equals(keys[i]))
			{
				TValue val = values[i];
				if (item.Value == null)
				{
					return val == null;
				}
				return item.Value.Equals(val);
			}
		}
		return false;
	}

	public bool Remove(TKey key)
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			if (key.Equals(keys[i]))
			{
				removeEntry(i);
				return true;
			}
		}
		return false;
	}

	public bool Remove(KeyValuePair<TKey, TValue> item)
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			if (!item.Key.Equals(keys[i]))
			{
				continue;
			}
			TValue val = values[i];
			if (item.Value == null)
			{
				if (val == null)
				{
					removeEntry(i);
					return true;
				}
				return false;
			}
			if (item.Value.Equals(val))
			{
				removeEntry(i);
				return true;
			}
			return false;
		}
		return false;
	}

	private void removeEntry(int index)
	{
		for (int i = index + 1; i < keys.Length && i < count; i++)
		{
			keys[i - 1] = keys[i];
			values[i - 1] = values[i];
		}
		count--;
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		KeyValuePair<TKey, TValue>[] array2 = new KeyValuePair<TKey, TValue>[count];
		for (int i = 0; i < count; i++)
		{
			array2[i] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
		}
		Array.Copy(array2, 0, array, arrayIndex, count);
	}
}
