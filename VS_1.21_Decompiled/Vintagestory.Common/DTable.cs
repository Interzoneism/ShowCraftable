using System;
using System.Collections.Generic;
using System.Threading;

namespace Vintagestory.Common;

public class DTable<TKey, TValue>
{
	public readonly TKey[] keys;

	public readonly TValue[] values;

	public volatile int count;

	private volatile int countBleedingEdge;

	public DTable(int capacity)
	{
		keys = new TKey[capacity];
		values = new TValue[capacity];
		count = 0;
	}

	public DTable(TKey key, TValue value)
	{
		int num = 4;
		keys = new TKey[num];
		values = new TValue[num];
		int num2 = 0;
		keys[num2] = key;
		values[num2] = value;
		count = num2 + 1;
		countBleedingEdge = num2 + 1;
	}

	public DTable(DTable<TKey, TValue> old, TKey key, TValue value)
	{
		int num = old.values.Length;
		num = ((num <= 5) ? 8 : ((num * 3 / 2 + 1) / 2 * 2));
		keys = new TKey[num];
		values = new TValue[num];
		int num2 = old.count;
		CopyArray(old.keys, keys, 0, 0, num2);
		CopyArray(old.values, values, 0, 0, num2);
		keys[num2] = key;
		values[num2] = value;
		count = num2 + 1;
		countBleedingEdge = num2 + 1;
	}

	public DTable(DTable<TKey, TValue> old, int toRemove)
	{
		int num = old.values.Length;
		keys = new TKey[num];
		values = new TValue[num];
		int num2 = old.count;
		if (toRemove >= num2)
		{
			toRemove = num2;
			CopyArray(old.keys, keys, 0, 0, toRemove);
			CopyArray(old.values, values, 0, 0, toRemove);
			count = num2;
			countBleedingEdge = num2;
		}
		else
		{
			CopyArray(old.keys, keys, 0, 0, toRemove);
			CopyArray(old.keys, keys, toRemove + 1, toRemove, num2);
			CopyArray(old.values, values, 0, 0, toRemove);
			CopyArray(old.values, values, toRemove + 1, toRemove, num2);
			count = num2 - 1;
			countBleedingEdge = num2 - 1;
		}
	}

	private void CopyArray<T>(T[] source, T[] dest, int sourceStart, int destStart, int sourceEnd)
	{
		if (sourceEnd - sourceStart < 32)
		{
			int num = destStart - sourceStart;
			for (int i = sourceStart; i < source.Length && i < sourceEnd; i++)
			{
				dest[i + num] = source[i];
			}
		}
		else
		{
			Array.Copy(source, sourceStart, dest, destStart, sourceEnd - sourceStart);
		}
	}

	internal TValue GetValue(TKey key)
	{
		TKey[] array = keys;
		for (int i = 0; i < array.Length && i < count; i++)
		{
			if (key.Equals(array[i]))
			{
				return values[i];
			}
		}
		throw new KeyNotFoundException("The key " + key.ToString() + " was not found");
	}

	internal TValue TryGetValue(TKey key)
	{
		TKey[] array = keys;
		for (int i = 0; i < array.Length && i < count; i++)
		{
			if (key.Equals(array[i]))
			{
				return values[i];
			}
		}
		return default(TValue);
	}

	internal bool TryGetValue(TKey key, out TValue value)
	{
		TKey[] array = keys;
		for (int i = 0; i < array.Length && i < count; i++)
		{
			if (key.Equals(array[i]))
			{
				value = values[i];
				return true;
			}
		}
		value = default(TValue);
		return false;
	}

	internal bool ContainsKey(TKey key)
	{
		TKey[] array = keys;
		for (int i = 0; i < array.Length && i < count; i++)
		{
			if (key.Equals(array[i]))
			{
				return true;
			}
		}
		return false;
	}

	internal int IndexOf(TKey key)
	{
		TKey[] array = keys;
		for (int i = 0; i < array.Length && i < count; i++)
		{
			if (key.Equals(array[i]))
			{
				return i;
			}
		}
		return -1;
	}

	internal int IndexOf(TKey key, TValue value)
	{
		TKey[] array = keys;
		for (int i = 0; i < array.Length && i < count; i++)
		{
			if (key.Equals(array[i]))
			{
				if (value.Equals(values[i]))
				{
					return i;
				}
				return -1;
			}
		}
		return -1;
	}

	internal bool ReplaceIfKeyExists(TKey key, TValue newValue)
	{
		TKey[] array = keys;
		for (int i = 0; i < array.Length && i < count; i++)
		{
			if (key.Equals(array[i]))
			{
				values[i] = newValue;
				return true;
			}
		}
		return false;
	}

	internal ICollection<TKey> KeysCopy()
	{
		TKey[] array = new TKey[count];
		if (array.Length < 32)
		{
			TKey[] array2 = keys;
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = array2[i];
			}
		}
		else
		{
			Array.Copy(keys, array, array.Length);
		}
		return array;
	}

	internal ICollection<TValue> ValuesCopy()
	{
		TValue[] array = new TValue[count];
		if (array.Length < 32)
		{
			TValue[] array2 = values;
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = array2[i];
			}
		}
		else
		{
			Array.Copy(values, array, array.Length);
		}
		return array;
	}

	internal bool Add(TKey key, TValue value)
	{
		int i;
		for (i = countBleedingEdge; Interlocked.CompareExchange(ref countBleedingEdge, i + 1, i) != i; i++)
		{
		}
		if (i >= values.Length)
		{
			return false;
		}
		keys[i] = key;
		values[i] = value;
		Interlocked.Increment(ref count);
		return true;
	}

	internal void DuplicateKeyCheck(TKey key)
	{
		TKey[] array = keys;
		bool flag = false;
		for (int i = 0; i < array.Length && i < count; i++)
		{
			ref TKey reference = ref key;
			TKey val = default(TKey);
			if (val == null)
			{
				val = reference;
				reference = ref val;
			}
			object obj = array[i];
			if (reference.Equals(obj))
			{
				if (flag)
				{
					val = key;
					throw new InvalidOperationException("ConcurrentSmallDictionary was written to with the same key '" + val?.ToString() + "' in two different threads, we can't handle that!");
				}
				flag = true;
			}
		}
	}

	internal void CopyTo(KeyValuePair<TKey, TValue>[] dest, int destIndex)
	{
		int num = count;
		if (num > dest.Length - destIndex)
		{
			num = dest.Length - destIndex;
		}
		for (int i = 0; i < keys.Length && i < num; i++)
		{
			dest[i + destIndex] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
		}
	}
}
