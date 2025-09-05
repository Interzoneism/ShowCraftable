using Vintagestory.API.Common;

namespace Vintagestory.API.Util;

public static class ObjectCacheUtil
{
	public static T TryGet<T>(ICoreAPI api, string key)
	{
		if (api.ObjectCache.TryGetValue(key, out var value))
		{
			return (T)value;
		}
		return default(T);
	}

	public static T GetOrCreate<T>(ICoreAPI api, string key, CreateCachableObjectDelegate<T> onRequireCreate)
	{
		if (!api.ObjectCache.TryGetValue(key, out var value) || value == null)
		{
			T val = onRequireCreate();
			api.ObjectCache[key] = val;
			return val;
		}
		return (T)value;
	}

	public static bool Delete(ICoreAPI api, string key)
	{
		if (key == null)
		{
			return false;
		}
		return api.ObjectCache.Remove(key);
	}
}
