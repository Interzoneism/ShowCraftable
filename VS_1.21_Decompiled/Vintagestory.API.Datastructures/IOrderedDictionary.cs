using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Vintagestory.API.Datastructures;

public interface IOrderedDictionary<TKey, TValue> : IOrderedDictionary, ICollection, IEnumerable, IDictionary, IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>
{
	new int Add(TKey key, TValue value);

	void Insert(int index, TKey key, TValue value);

	TValue GetValueAtIndex(int index);

	void SetAtIndex(int index, TValue value);
}
