using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class ModInfo : IComparable<ModInfo>
{
	private class DependenciesConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(IEnumerable<ModDependency>).IsAssignableFrom(objectType);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return (from prop in JObject.Load(reader).Properties()
				select new ModDependency(prop.Name, (string)prop.Value)).ToList().AsReadOnly();
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			foreach (ModDependency item in (IEnumerable<ModDependency>)value)
			{
				writer.WritePropertyName(item.ModID);
				writer.WriteValue(item.Version);
			}
			writer.WriteEndObject();
		}
	}

	private class ReadOnlyListConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			if (objectType.IsGenericType)
			{
				return objectType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);
			}
			return false;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			Type elementType = objectType.GetGenericArguments()[0];
			IEnumerable<object> enumerable = ((IEnumerable<JToken>)JArray.Load(reader)).Select((JToken e) => e.ToObject(elementType));
			Type type = typeof(List<>).MakeGenericType(elementType);
			IList list = (IList)Activator.CreateInstance(type);
			foreach (object item in enumerable)
			{
				list.Add(item);
			}
			return type.GetMethod("AsReadOnly").Invoke(list, Array.Empty<object>());
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			Type type = value.GetType().GetGenericArguments()[0];
			writer.WriteStartArray();
			foreach (object item in (IEnumerable)value)
			{
				serializer.Serialize(writer, item, type);
			}
			writer.WriteEndArray();
		}
	}

	private IReadOnlyList<string> _authors = Array.Empty<string>();

	[JsonRequired]
	public EnumModType Type;

	[JsonProperty]
	public int TextureSize = 32;

	[JsonRequired]
	public string Name;

	[JsonProperty]
	public string Version = "";

	[JsonProperty]
	public string NetworkVersion;

	[JsonProperty]
	public string IconPath;

	[JsonProperty]
	public string Description = "";

	[JsonProperty]
	public string Website = "";

	public bool CoreMod;

	[JsonProperty]
	public string ModID { get; set; }

	[JsonProperty]
	public IReadOnlyList<string> Authors
	{
		get
		{
			return _authors;
		}
		set
		{
			IEnumerable<string> source = value ?? Enumerable.Empty<string>();
			_authors = source.ToList().AsReadOnly();
		}
	}

	[JsonProperty]
	public IReadOnlyList<string> Contributors { get; set; } = new List<string>().AsReadOnly();

	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public EnumAppSide Side { get; set; } = EnumAppSide.Universal;

	[JsonProperty]
	public bool RequiredOnClient { get; set; } = true;

	[JsonProperty]
	public bool RequiredOnServer { get; set; } = true;

	[JsonProperty]
	[JsonConverter(typeof(DependenciesConverter))]
	public IReadOnlyList<ModDependency> Dependencies { get; set; } = new List<ModDependency>().AsReadOnly();

	public ModInfo()
	{
	}

	public ModInfo(EnumModType type, string name, string modID, string version, string description, IEnumerable<string> authors, IEnumerable<string> contributors, string website, EnumAppSide side, bool requiredOnClient, bool requiredOnServer, IEnumerable<ModDependency> dependencies)
	{
		Type = type;
		Name = name ?? throw new ArgumentNullException("name");
		ModID = modID ?? throw new ArgumentNullException("modID");
		Version = version ?? "";
		Description = description ?? "";
		Authors = ReadOnlyCopy<string>(authors);
		Contributors = ReadOnlyCopy<string>(contributors);
		Website = website ?? "";
		Side = side;
		RequiredOnClient = requiredOnClient;
		RequiredOnServer = requiredOnServer;
		Dependencies = ReadOnlyCopy<ModDependency>(dependencies);
		static IReadOnlyList<T> ReadOnlyCopy<T>(IEnumerable<T> elements)
		{
			return (elements ?? Enumerable.Empty<T>()).ToList().AsReadOnly();
		}
	}

	[OnDeserialized]
	private void OnDeserialized(StreamingContext ctx)
	{
		ModID = ModID ?? ToModID(Name);
	}

	public void Init()
	{
		if (NetworkVersion == null)
		{
			NetworkVersion = Version;
		}
	}

	public static string ToModID(string name)
	{
		if (name == null)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder(name.Length);
		for (int i = 0; i < name.Length; i++)
		{
			char c = name[i];
			bool num = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
			bool flag = c >= '0' && c <= '9';
			if (num || flag)
			{
				stringBuilder.Append(char.ToLower(c));
			}
			if (flag && i == 0)
			{
				throw new ArgumentException("Can't convert '" + name + "' to a mod ID automatically, because it starts with a number, which is illegal", "name");
			}
		}
		return stringBuilder.ToString();
	}

	public static bool IsValidModID(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return false;
		}
		for (int i = 0; i < str.Length; i++)
		{
			char c = str[i];
			bool num = c >= 'a' && c <= 'z';
			bool flag = c >= '0' && c <= '9';
			if (!num && (!flag || i == 0))
			{
				return false;
			}
		}
		return true;
	}

	public int CompareTo(ModInfo other)
	{
		int num = ModID.CompareOrdinal(other.ModID);
		if (num != 0)
		{
			return num;
		}
		if (GameVersion.IsNewerVersionThan(Version, other.Version))
		{
			return -1;
		}
		if (GameVersion.IsLowerVersionThan(Version, other.Version))
		{
			return 1;
		}
		return 0;
	}
}
