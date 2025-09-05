namespace Vintagestory.API.Client;

public interface ISettingsClass<T>
{
	T this[string key] { get; set; }

	T Get(string key, T defaultValue = default(T));

	void Set(string key, T value, bool shouldTriggerWatchers);

	bool Exists(string key);

	void AddWatcher(string key, OnSettingsChanged<T> OnValueChanged);

	bool RemoveWatcher(string key, OnSettingsChanged<T> handler);
}
