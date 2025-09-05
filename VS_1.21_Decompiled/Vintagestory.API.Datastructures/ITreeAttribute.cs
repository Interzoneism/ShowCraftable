using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Datastructures;

public interface ITreeAttribute : IAttribute, IEnumerable<KeyValuePair<string, IAttribute>>, IEnumerable
{
	IAttribute this[string key] { get; set; }

	int Count { get; }

	IAttribute[] Values { get; }

	bool HasAttribute(string key);

	bool TryGetAttribute(string key, out IAttribute value);

	void RemoveAttribute(string key);

	void SetBool(string key, bool value);

	void SetInt(string key, int value);

	void SetLong(string key, long value);

	void SetDouble(string key, double value);

	void SetFloat(string key, float value);

	void SetString(string key, string value);

	void SetBytes(string key, byte[] value);

	void SetItemstack(string key, ItemStack itemstack);

	bool? TryGetBool(string key);

	bool GetBool(string key, bool defaultValue = false);

	int? TryGetInt(string key);

	int GetInt(string key, int defaultValue = 0);

	int GetAsInt(string key, int defaultValue = 0);

	bool GetAsBool(string key, bool defaultValue = false);

	double GetDecimal(string key, double defaultValue = 0.0);

	string GetAsString(string key, string defaultValue = null);

	long? TryGetLong(string key);

	long GetLong(string key, long defaultValue = 0L);

	float? TryGetFloat(string key);

	float GetFloat(string key, float defaultValue = 0f);

	double? TryGetDouble(string key);

	double GetDouble(string key, double defaultValue = 0.0);

	string GetString(string key, string defaultValue = null);

	byte[] GetBytes(string key, byte[] defaultValue = null);

	ItemStack GetItemstack(string key, ItemStack defaultValue = null);

	ITreeAttribute GetTreeAttribute(string key);

	ITreeAttribute GetOrAddTreeAttribute(string key);

	new ITreeAttribute Clone();

	void MergeTree(ITreeAttribute tree);

	OrderedDictionary<string, IAttribute> SortedCopy(bool recursive = false);

	bool Equals(IWorldAccessor worldForResolve, IAttribute attr, params string[] ignoreSubTrees);

	bool IsSubSetOf(IWorldAccessor worldForResolve, IAttribute other);

	int GetHashCode(string[] ignoredAttributes);
}
