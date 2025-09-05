using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace Vintagestory.Common;

public class SettingsClass<T> : ISettingsClass<T>
{
	public Dictionary<string, T> values;

	public T defaultValue;

	public List<SettingsChangedWatcher<T>> Watchers = new List<SettingsChangedWatcher<T>>();

	public bool Dirty;

	public bool ShouldTriggerWatchers = true;

	public T this[string key]
	{
		get
		{
			if (!values.TryGetValue(key, out var value))
			{
				return defaultValue;
			}
			return value;
		}
		set
		{
			Set(key, value, ShouldTriggerWatchers);
		}
	}

	public SettingsClass()
	{
		StringComparer ordinalIgnoreCase = StringComparer.OrdinalIgnoreCase;
		values = new Dictionary<string, T>(ordinalIgnoreCase);
	}

	public bool Exists(string key)
	{
		return values.ContainsKey(key);
	}

	public T Get(string key, T defaultValue = default(T))
	{
		if (values.TryGetValue(key, out var value))
		{
			return value;
		}
		return defaultValue;
	}

	public void Set(string key, T value, bool shouldTriggerWatchers)
	{
		if (!values.ContainsKey(key) || !EqualityComparer<T>.Default.Equals(values[key], value))
		{
			values[key] = value;
			if (shouldTriggerWatchers)
			{
				TriggerWatcher(key);
			}
			Dirty = true;
		}
	}

	public void TriggerWatcher(string key)
	{
		string text = key.ToLowerInvariant();
		T newValue = values[key];
		foreach (SettingsChangedWatcher<T> watcher in Watchers)
		{
			if (watcher.key == text)
			{
				watcher.handler(newValue);
			}
		}
	}

	public void AddWatcher(string key, OnSettingsChanged<T> handler)
	{
		if (handler == null)
		{
			throw new ArgumentNullException("handler cannot be null!");
		}
		Watchers.Add(new SettingsChangedWatcher<T>
		{
			key = key.ToLowerInvariant(),
			handler = handler
		});
	}

	public bool RemoveWatcher(string key, OnSettingsChanged<T> handler)
	{
		for (int i = 0; i < Watchers.Count; i++)
		{
			SettingsChangedWatcher<T> settingsChangedWatcher = Watchers[i];
			if (settingsChangedWatcher.key == key.ToLowerInvariant() && settingsChangedWatcher.handler == handler)
			{
				Watchers.RemoveAt(i);
				return true;
			}
		}
		return false;
	}
}
