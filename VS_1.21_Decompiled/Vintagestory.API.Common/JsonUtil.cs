using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Vintagestory.API.Common;

public static class JsonUtil
{
	public static void Populate<T>(this JToken value, T target) where T : class
	{
		JsonReader val = value.CreateReader();
		try
		{
			JsonSerializer.CreateDefault().Populate(val, (object)target);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public static T FromBytes<T>(byte[] data)
	{
		using MemoryStream stream = new MemoryStream(data);
		using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
		return JsonConvert.DeserializeObject<T>(streamReader.ReadToEnd());
	}

	public static T FromString<T>(string data)
	{
		return JsonConvert.DeserializeObject<T>(data);
	}

	public static byte[] ToBytes<T>(T obj)
	{
		return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject((object)obj));
	}

	public static string ToString<T>(T obj)
	{
		return JsonConvert.SerializeObject((object)obj);
	}

	public static string ToPrettyString<T>(T obj)
	{
		return JsonConvert.SerializeObject((object)obj, (Formatting)1);
	}

	public static void PopulateObject(object toPopulate, string text, string domain, JsonSerializerSettings settings = null)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		if (domain != "game")
		{
			if (settings == null)
			{
				settings = new JsonSerializerSettings();
			}
			settings.Converters.Add((JsonConverter)(object)new AssetLocationJsonParser(domain));
		}
		JsonConvert.PopulateObject(text, toPopulate, settings);
	}

	public static JsonSerializer CreateSerializerForDomain(string domain, JsonSerializerSettings settings = null)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		if (domain != "game")
		{
			if (settings == null)
			{
				settings = new JsonSerializerSettings();
			}
			settings.Converters.Add((JsonConverter)(object)new AssetLocationJsonParser(domain));
		}
		return JsonSerializer.CreateDefault(settings);
	}

	public static void PopulateObject(object toPopulate, JToken token, JsonSerializer js)
	{
		JsonReader val = token.CreateReader();
		try
		{
			js.Populate(val, toPopulate);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public static T ToObject<T>(string text, string domain, JsonSerializerSettings settings = null)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		if (domain != "game")
		{
			if (settings == null)
			{
				settings = new JsonSerializerSettings();
			}
			settings.Converters.Add((JsonConverter)(object)new AssetLocationJsonParser(domain));
		}
		return JsonConvert.DeserializeObject<T>(text, settings);
	}

	public static T ToObject<T>(this JToken token, string domain, JsonSerializerSettings settings = null)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		if (domain != "game")
		{
			if (settings == null)
			{
				settings = new JsonSerializerSettings();
			}
			settings.Converters.Add((JsonConverter)(object)new AssetLocationJsonParser(domain));
		}
		return token.ToObject<T>(JsonSerializer.Create(settings));
	}
}
