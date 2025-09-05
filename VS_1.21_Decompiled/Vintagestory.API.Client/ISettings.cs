using System.Collections.Generic;

namespace Vintagestory.API.Client;

public interface ISettings
{
	ISettingsClass<bool> Bool { get; }

	ISettingsClass<int> Int { get; }

	ISettingsClass<float> Float { get; }

	ISettingsClass<string> String { get; }

	ISettingsClass<List<string>> Strings { get; }

	void AddWatcher<T>(string key, OnSettingsChanged<T> OnValueChanged);
}
