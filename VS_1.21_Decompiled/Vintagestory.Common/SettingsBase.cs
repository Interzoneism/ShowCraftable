using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Vintagestory.API.Client;

namespace Vintagestory.Common;

[JsonObject(/*Could not decode attribute arguments.*/)]
public abstract class SettingsBase : SettingsBaseNoObf, ISettings
{
	protected bool isnewfile;

	public abstract string FileName { get; }

	public abstract string TempFileName { get; }

	public abstract string BkpFileName { get; }

	public bool IsDirty
	{
		get
		{
			if (!BoolSettings.Dirty && !StringSettings.Dirty && !StringListSettings.Dirty && !FloatSettings.Dirty && !IntSettings.Dirty)
			{
				return OtherDirty;
			}
			return true;
		}
	}

	public void AddWatcher<T>(string key, OnSettingsChanged<T> handler)
	{
		if (typeof(T) == typeof(bool))
		{
			BoolSettings.AddWatcher(key, handler as OnSettingsChanged<bool>);
		}
		if (typeof(T) == typeof(string))
		{
			StringSettings.AddWatcher(key, handler as OnSettingsChanged<string>);
		}
		if (typeof(T) == typeof(int))
		{
			IntSettings.AddWatcher(key, handler as OnSettingsChanged<int>);
		}
		if (typeof(T) == typeof(float))
		{
			FloatSettings.AddWatcher(key, handler as OnSettingsChanged<float>);
		}
		if (typeof(T) == typeof(List<string>))
		{
			StringListSettings.AddWatcher(key, handler as OnSettingsChanged<List<string>>);
		}
	}

	public virtual void ClearWatchers()
	{
		StringListSettings.Watchers.Clear();
		BoolSettings.Watchers.Clear();
		IntSettings.Watchers.Clear();
		FloatSettings.Watchers.Clear();
		StringSettings.Watchers.Clear();
	}

	public string GetStringSetting(string key, string defaultValue = null)
	{
		string value = defaultValue;
		base.stringSettings.TryGetValue(key.ToLowerInvariant(), out value);
		return value;
	}

	public List<string> GetStringListSetting(string key, List<string> defaultValue = null)
	{
		List<string> value = defaultValue;
		base.stringListSettings.TryGetValue(key.ToLowerInvariant(), out value);
		return value;
	}

	public int GetIntSetting(string key)
	{
		base.intSettings.TryGetValue(key.ToLowerInvariant(), out var value);
		return value;
	}

	public float GetFloatSetting(string key)
	{
		base.floatSettings.TryGetValue(key.ToLowerInvariant(), out var value);
		return value;
	}

	public bool GetBoolSetting(string key)
	{
		base.boolSettings.TryGetValue(key.ToLowerInvariant(), out var value);
		return value;
	}

	public bool HasSetting(string name)
	{
		name = name.ToLowerInvariant();
		if (!base.stringSettings.ContainsKey(name) && !base.intSettings.ContainsKey(name) && !base.floatSettings.ContainsKey(name))
		{
			return base.boolSettings.ContainsKey(name);
		}
		return true;
	}

	public Type GetSettingType(string name)
	{
		name = name.ToLowerInvariant();
		if (base.stringSettings.ContainsKey(name))
		{
			return typeof(string);
		}
		if (base.intSettings.ContainsKey(name))
		{
			return typeof(int);
		}
		if (base.floatSettings.ContainsKey(name))
		{
			return typeof(float);
		}
		if (base.boolSettings.ContainsKey(name))
		{
			return typeof(bool);
		}
		return null;
	}

	internal object GetSetting(string name)
	{
		name = name.ToLowerInvariant();
		if (base.stringSettings.ContainsKey(name))
		{
			return GetStringSetting(name);
		}
		if (base.intSettings.ContainsKey(name))
		{
			return GetIntSetting(name);
		}
		if (base.floatSettings.ContainsKey(name))
		{
			return GetFloatSetting(name);
		}
		if (base.boolSettings.ContainsKey(name))
		{
			return GetBoolSetting(name);
		}
		return null;
	}

	protected SettingsBase()
	{
		StringComparer ordinalIgnoreCase = StringComparer.OrdinalIgnoreCase;
		base.stringSettings = new Dictionary<string, string>(ordinalIgnoreCase);
		base.intSettings = new Dictionary<string, int>(ordinalIgnoreCase);
		base.boolSettings = new Dictionary<string, bool>(ordinalIgnoreCase);
		base.floatSettings = new Dictionary<string, float>(ordinalIgnoreCase);
		base.stringListSettings = new Dictionary<string, List<string>>(ordinalIgnoreCase);
	}

	[OnDeserializing]
	internal void OnDeserializingMethod(StreamingContext context)
	{
		LoadDefaultValues();
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		StringComparer ordinalIgnoreCase = StringComparer.OrdinalIgnoreCase;
		if (base.stringSettings == null)
		{
			base.stringSettings = new Dictionary<string, string>(ordinalIgnoreCase);
		}
		if (base.intSettings == null)
		{
			base.intSettings = new Dictionary<string, int>(ordinalIgnoreCase);
		}
		if (base.boolSettings == null)
		{
			base.boolSettings = new Dictionary<string, bool>(ordinalIgnoreCase);
		}
		if (base.floatSettings == null)
		{
			base.floatSettings = new Dictionary<string, float>(ordinalIgnoreCase);
		}
		if (base.stringListSettings == null)
		{
			base.stringListSettings = new Dictionary<string, List<string>>(ordinalIgnoreCase);
		}
		DidDeserialize();
	}

	internal virtual void DidDeserialize()
	{
	}

	public virtual void Load()
	{
		LoadDefaultValues();
		if (!File.Exists(FileName) && File.Exists(BkpFileName))
		{
			File.Move(BkpFileName, FileName);
		}
		if (File.Exists(FileName))
		{
			try
			{
				string text;
				using (TextReader textReader = new StreamReader(FileName))
				{
					text = textReader.ReadToEnd();
					textReader.Close();
				}
				JsonConvert.PopulateObject(text, (object)this);
				return;
			}
			catch (Exception)
			{
				isnewfile = true;
				return;
			}
		}
		OtherDirty = true;
		isnewfile = true;
	}

	public virtual bool Save(bool force = false)
	{
		if (!IsDirty && !force)
		{
			return true;
		}
		try
		{
			using (TextWriter textWriter = new StreamWriter(TempFileName))
			{
				textWriter.Write(JsonConvert.SerializeObject((object)this, (Formatting)1));
				textWriter.Close();
			}
			if (!File.Exists(FileName))
			{
				File.Move(TempFileName, FileName);
			}
			else
			{
				File.Replace(TempFileName, FileName, BkpFileName);
			}
		}
		catch (IOException)
		{
			return false;
		}
		OtherDirty = false;
		BoolSettings.Dirty = false;
		StringSettings.Dirty = false;
		StringListSettings.Dirty = false;
		FloatSettings.Dirty = false;
		IntSettings.Dirty = false;
		return true;
	}

	public abstract void LoadDefaultValues();
}
