using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Vintagestory.Common;

public class ConcurrentSmallDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
{
	private DTable<TKey, TValue> contents;

	public ICollection<TKey> Keys
	{
		get
		{
			DTable<TKey, TValue> dTable = contents;
			if (dTable != null)
			{
				return dTable.KeysCopy();
			}
			return Array.Empty<TKey>();
		}
	}

	public ICollection<TValue> Values
	{
		get
		{
			DTable<TKey, TValue> dTable = contents;
			if (dTable != null)
			{
				return dTable.ValuesCopy();
			}
			return Array.Empty<TValue>();
		}
	}

	public bool IsReadOnly => false;

	int ICollection<KeyValuePair<TKey, TValue>>.Count
	{
		get
		{
			DTable<TKey, TValue> dTable = contents;
			if (dTable != null)
			{
				return dTable.count;
			}
			return 0;
		}
	}

	public int Count
	{
		get
		{
			DTable<TKey, TValue> dTable = contents;
			if (dTable != null)
			{
				return dTable.count;
			}
			return 0;
		}
	}

	public TValue this[TKey key]
	{
		get
		{
			return contents.GetValue(key);
		}
		set
		{
			Add(key, value);
		}
	}

	public ConcurrentSmallDictionary(int capacity)
	{
		if (capacity == 0)
		{
			contents = null;
		}
		else
		{
			contents = new DTable<TKey, TValue>(capacity);
		}
	}

	public ConcurrentSmallDictionary()
		: this(4)
	{
	}

	public bool IsEmpty()
	{
		DTable<TKey, TValue> dTable = contents;
		if (dTable != null)
		{
			return dTable.count == 0;
		}
		return true;
	}

	public void Add(TKey key, TValue value)
	{
		DTable<TKey, TValue> dTable = contents;
		if (dTable == null)
		{
			if (Interlocked.CompareExchange(ref contents, new DTable<TKey, TValue>(key, value), dTable) != dTable)
			{
				Add(key, value);
			}
		}
		else if (!dTable.ReplaceIfKeyExists(key, value))
		{
			if (!dTable.Add(key, value) && Interlocked.CompareExchange(ref contents, new DTable<TKey, TValue>(dTable, key, value), dTable) != dTable)
			{
				Add(key, value);
			}
			contents.DuplicateKeyCheck(key);
		}
	}

	public TValue TryGetValue(TKey key)
	{
		DTable<TKey, TValue> dTable = contents;
		if (dTable != null)
		{
			return dTable.TryGetValue(key);
		}
		return default(TValue);
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		DTable<TKey, TValue> dTable = contents;
		if (dTable == null)
		{
			value = default(TValue);
			return false;
		}
		return dTable.TryGetValue(key, out value);
	}

	public bool ContainsKey(TKey key)
	{
		return contents?.ContainsKey(key) ?? false;
	}

	public void Add(KeyValuePair<TKey, TValue> item)
	{
		Add(item.Key, item.Value);
	}

	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		if (!contents.TryGetValue(item.Key, out var value))
		{
			return false;
		}
		return item.Value.Equals(value);
	}

	public bool Remove(TKey key)
	{
		DTable<TKey, TValue> dTable = contents;
		if (dTable == null)
		{
			return false;
		}
		int num = dTable.IndexOf(key);
		if (num < 0)
		{
			return false;
		}
		if (Interlocked.CompareExchange(ref contents, new DTable<TKey, TValue>(dTable, num), dTable) != dTable)
		{
			return Remove(key);
		}
		return true;
	}

	public bool Remove(KeyValuePair<TKey, TValue> item)
	{
		DTable<TKey, TValue> dTable = contents;
		if (dTable == null)
		{
			return false;
		}
		int num = dTable.IndexOf(item.Key, item.Value);
		if (num < 0)
		{
			return false;
		}
		if (Interlocked.CompareExchange(ref contents, new DTable<TKey, TValue>(dTable, num), dTable) != dTable)
		{
			return Remove(item);
		}
		return true;
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		contents.CopyTo(array, arrayIndex);
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		DTable<TKey, TValue> contents = this.contents;
		if (contents != null)
		{
			int end_snapshot = contents.count;
			for (int pos = 0; pos < end_snapshot; pos++)
			{
				yield return new KeyValuePair<TKey, TValue>(contents.keys[pos], contents.values[pos]);
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Clear()
	{
		contents = null;
	}
}
