using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.API.Datastructures;

public class JsonObject : IReadOnlyCollection<JsonObject>, IEnumerable<JsonObject>, IEnumerable
{
	private JToken token;

	public JsonObject this[string key]
	{
		get
		{
			if (token == null || !(token is JObject))
			{
				return new JsonObject(null);
			}
			JToken obj = token;
			JToken val = default(JToken);
			((JObject)((obj is JObject) ? obj : null)).TryGetValue(key, StringComparison.OrdinalIgnoreCase, ref val);
			return new JsonObject(val);
		}
	}

	public bool Exists => token != null;

	public virtual JToken Token
	{
		get
		{
			return token;
		}
		set
		{
			token = value;
		}
	}

	public int Count
	{
		get
		{
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			if (token == null)
			{
				throw new InvalidOperationException("Cannot count a null token");
			}
			JToken obj = token;
			JObject val = (JObject)(object)((obj is JObject) ? obj : null);
			if (val != null)
			{
				return ((JContainer)val).Count;
			}
			JToken obj2 = token;
			JArray val2 = (JArray)(object)((obj2 is JArray) ? obj2 : null);
			if (val2 != null)
			{
				return ((JContainer)val2).Count;
			}
			throw new InvalidOperationException("Can iterate only over a JObject or JArray, this token is of type " + ((object)token.Type/*cast due to .constrained prefix*/).ToString());
		}
	}

	public static JsonObject FromJson(string jsonCode)
	{
		return new JsonObject(JToken.Parse(jsonCode));
	}

	public JsonObject(JToken token)
	{
		this.token = token;
	}

	public JsonObject(JsonObject original, bool unused)
	{
		token = original.token;
	}

	public bool KeyExists(string key)
	{
		return token[(object)key] != null;
	}

	public T AsObject<T>(T defaultValue = default(T))
	{
		JsonSerializerSettings val = null;
		if (token != null)
		{
			return JsonConvert.DeserializeObject<T>(((object)token).ToString(), val);
		}
		return defaultValue;
	}

	public T AsObject<T>(T defaultValue, string domain)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		JsonSerializerSettings val = null;
		if (domain != "game")
		{
			val = new JsonSerializerSettings();
			val.Converters.Add((JsonConverter)(object)new AssetLocationJsonParser(domain));
		}
		if (token != null)
		{
			return JsonConvert.DeserializeObject<T>(((object)token).ToString(), val);
		}
		return defaultValue;
	}

	public T AsObject<T>(JsonSerializerSettings settings, T defaultValue, string domain = "game")
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
		if (token != null)
		{
			return JsonConvert.DeserializeObject<T>(((object)token).ToString(), settings);
		}
		return defaultValue;
	}

	public JsonObject[] AsArray()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		if (!(token is JArray))
		{
			return null;
		}
		JArray val = (JArray)token;
		JsonObject[] array = new JsonObject[((JContainer)val).Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new JsonObject(val[i]);
		}
		return array;
	}

	public string AsString(string defaultValue = null)
	{
		return GetValue(defaultValue);
	}

	[Obsolete("Use AsArray<string>() instead")]
	public string[] AsStringArray(string[] defaultValue = null, string defaultDomain = null)
	{
		return AsArray(defaultValue, defaultDomain);
	}

	[Obsolete("Use AsArray<float>() instead")]
	public float[] AsFloatArray(float[] defaultValue = null)
	{
		return AsArray(defaultValue);
	}

	public T[] AsArray<T>(T[] defaultValue = null, string defaultDomain = null)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		if (!(token is JArray))
		{
			return defaultValue;
		}
		JArray val = (JArray)token;
		T[] array = new T[((JContainer)val).Count];
		for (int i = 0; i < array.Length; i++)
		{
			JToken val2 = val[i];
			if (val2 is JValue || val2 is JObject)
			{
				array[i] = val2.ToObject<T>(defaultDomain);
				continue;
			}
			return defaultValue;
		}
		return array;
	}

	public bool AsBool(bool defaultValue = false)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (!(token is JValue))
		{
			return defaultValue;
		}
		object value = ((JValue)token).Value;
		if (value is bool)
		{
			return (bool)value;
		}
		if (value is string)
		{
			if (!bool.TryParse(value?.ToString() ?? "", out var result))
			{
				return defaultValue;
			}
			return result;
		}
		return defaultValue;
	}

	public int AsInt(int defaultValue = 0)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (!(token is JValue))
		{
			return defaultValue;
		}
		object value = ((JValue)token).Value;
		if (value is long)
		{
			return (int)(long)value;
		}
		if (value is int)
		{
			return (int)value;
		}
		if (value is float)
		{
			return (int)(float)value;
		}
		if (value is double)
		{
			return (int)(double)value;
		}
		if (value is string)
		{
			if (!int.TryParse(value?.ToString() ?? "", out var result))
			{
				return defaultValue;
			}
			return result;
		}
		return defaultValue;
	}

	public float AsFloat(float defaultValue = 0f)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (!(token is JValue))
		{
			return defaultValue;
		}
		object value = ((JValue)token).Value;
		if (value is int)
		{
			return (int)value;
		}
		if (value is float)
		{
			return (float)value;
		}
		if (value is long)
		{
			return (long)value;
		}
		if (value is double)
		{
			return (float)(double)value;
		}
		if (value is string)
		{
			if (!float.TryParse(value?.ToString() ?? "", NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
			{
				return defaultValue;
			}
			return result;
		}
		return defaultValue;
	}

	public double AsDouble(double defaultValue = 0.0)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (!(token is JValue))
		{
			return defaultValue;
		}
		object value = ((JValue)token).Value;
		if (value is int)
		{
			return (int)value;
		}
		if (value is long)
		{
			return (long)value;
		}
		if (value is double)
		{
			return (double)value;
		}
		if (value is string)
		{
			if (!double.TryParse(value?.ToString() ?? "", out var result))
			{
				return defaultValue;
			}
			return result;
		}
		return defaultValue;
	}

	private T GetValue<T>(T defaultValue = default(T))
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (!(token is JValue))
		{
			return defaultValue;
		}
		if (!(((JValue)token).Value is T))
		{
			return defaultValue;
		}
		return token.ToObject<T>();
	}

	public override string ToString()
	{
		return (((object)token)?.ToString()).DeDuplicate();
	}

	public bool IsArray()
	{
		return token is JArray;
	}

	public IAttribute ToAttribute()
	{
		return ToAttribute(token);
	}

	public virtual void FillPlaceHolder(string key, string value)
	{
		FillPlaceHolder(token, key, value);
	}

	internal static void FillPlaceHolder(JToken token, string key, string value)
	{
		JValue val = (JValue)(object)((token is JValue) ? token : null);
		if (val != null && val.Value is string)
		{
			val.Value = (val.Value as string).Replace("{" + key + "}", value);
		}
		JArray val2 = (JArray)(object)((token is JArray) ? token : null);
		if (val2 != null)
		{
			foreach (JToken item in val2)
			{
				FillPlaceHolder(item, key, value);
			}
		}
		JObject val3 = (JObject)(object)((token is JObject) ? token : null);
		if (val3 == null)
		{
			return;
		}
		foreach (KeyValuePair<string, JToken> item2 in val3)
		{
			FillPlaceHolder(item2.Value, key, value);
		}
	}

	private static IAttribute ToAttribute(JToken token)
	{
		JValue val = (JValue)(object)((token is JValue) ? token : null);
		if (val != null)
		{
			if (val.Value is int)
			{
				return new IntAttribute((int)val.Value);
			}
			if (val.Value is long)
			{
				return new LongAttribute((long)val.Value);
			}
			if (val.Value is float)
			{
				return new FloatAttribute((float)val.Value);
			}
			if (val.Value is double)
			{
				return new DoubleAttribute((double)val.Value);
			}
			if (val.Value is bool)
			{
				return new BoolAttribute((bool)val.Value);
			}
			if (val.Value is string)
			{
				return new StringAttribute((string)val.Value);
			}
		}
		JObject val2 = (JObject)(object)((token is JObject) ? token : null);
		if (val2 != null)
		{
			TreeAttribute treeAttribute = new TreeAttribute();
			{
				foreach (KeyValuePair<string, JToken> item in val2)
				{
					treeAttribute[item.Key] = ToAttribute(item.Value);
				}
				return treeAttribute;
			}
		}
		JArray val3 = (JArray)(object)((token is JArray) ? token : null);
		if (val3 != null)
		{
			if (!((JToken)val3).HasValues)
			{
				return new TreeArrayAttribute(Array.Empty<TreeAttribute>());
			}
			JToken obj = val3[0];
			JValue val4 = (JValue)(object)((obj is JValue) ? obj : null);
			if (val4 != null)
			{
				if (val4.Value is int)
				{
					return new IntArrayAttribute(ToPrimitiveArray<int>(val3));
				}
				if (val4.Value is long)
				{
					return new LongArrayAttribute(ToPrimitiveArray<long>(val3));
				}
				if (val4.Value is float)
				{
					return new FloatArrayAttribute(ToPrimitiveArray<float>(val3));
				}
				if (val4.Value is double)
				{
					return new DoubleArrayAttribute(ToPrimitiveArray<double>(val3));
				}
				if (val4.Value is bool)
				{
					return new BoolArrayAttribute(ToPrimitiveArray<bool>(val3));
				}
				if (val4.Value is string)
				{
					return new StringArrayAttribute(ToPrimitiveArray<string>(val3));
				}
				return null;
			}
			TreeAttribute[] array = new TreeAttribute[((JContainer)val3).Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (TreeAttribute)ToAttribute(val3[i]);
			}
			return new TreeArrayAttribute(array);
		}
		return null;
	}

	public static T[] ToPrimitiveArray<T>(JArray array)
	{
		T[] array2 = new T[((JContainer)array).Count];
		for (int i = 0; i < array2.Length; i++)
		{
			_ = array[i];
			array2[i] = array[i].ToObject<T>();
		}
		return array2;
	}

	public JsonObject Clone()
	{
		return new JsonObject(token.DeepClone());
	}

	public bool IsTrue(string attrName)
	{
		if (token == null || !(token is JObject))
		{
			return false;
		}
		JToken obj = token[(object)attrName];
		JValue val = (JValue)(object)((obj is JValue) ? obj : null);
		if (val != null)
		{
			object value = val.Value;
			if (value is bool)
			{
				return (bool)value;
			}
			if (val.Value is string value2)
			{
				bool.TryParse(value2, out var result);
				return result;
			}
		}
		return false;
	}

	public IEnumerator<JsonObject> GetEnumerator()
	{
		if (token == null)
		{
			throw new InvalidOperationException("Cannot iterate over a null token");
		}
		JToken obj = token;
		JObject val = (JObject)(object)((obj is JObject) ? obj : null);
		if (val != null)
		{
			foreach (KeyValuePair<string, JToken> item in val)
			{
				yield return new JsonObject(JToken.op_Implicit(item.Key));
			}
			yield break;
		}
		JToken val2 = token;
		JArray jarr = (JArray)(object)((val2 is JArray) ? val2 : null);
		if (jarr != null)
		{
			for (int i = 0; i < Count; i++)
			{
				yield return new JsonObject(jarr[i]);
			}
			yield break;
		}
		throw new InvalidOperationException("Can iterate only over a JObject or JArray, this token is of type " + ((object)token.Type/*cast due to .constrained prefix*/).ToString());
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
