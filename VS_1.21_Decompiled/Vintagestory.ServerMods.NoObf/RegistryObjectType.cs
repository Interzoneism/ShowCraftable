using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods.NoObf;

[DocumentAsJson]
[JsonObject(/*Could not decode attribute arguments.*/)]
public abstract class RegistryObjectType
{
	internal volatile int parseStarted;

	[DocumentAsJson]
	public bool Enabled = true;

	public JObject jsonObject;

	[DocumentAsJson]
	public AssetLocation Code;

	[DocumentAsJson]
	public RegistryObjectVariantGroup[] VariantGroups;

	public OrderedDictionary<string, string> Variant = new OrderedDictionary<string, string>();

	[JsonProperty]
	public WorldInteraction[] Interactions;

	[JsonProperty]
	public AssetLocation[] SkipVariants;

	[JsonProperty]
	public AssetLocation[] AllowedVariants;

	public HashSet<AssetLocation> AllowedVariantsQuickLookup = new HashSet<AssetLocation>();

	[JsonProperty]
	public string Class;

	[JsonProperty]
	public string[] Tags = Array.Empty<string>();

	public bool WildCardMatch(AssetLocation[] wildcards)
	{
		foreach (AssetLocation wildCard in wildcards)
		{
			if (WildCardMatch(wildCard))
			{
				return true;
			}
		}
		return false;
	}

	public bool WildCardMatch(AssetLocation wildCard)
	{
		if (wildCard == Code)
		{
			return true;
		}
		if (Code == null || wildCard.Domain != Code.Domain)
		{
			return false;
		}
		string text = Regex.Escape(wildCard.Path).Replace("\\*", "(.*)");
		return Regex.IsMatch(Code.Path, "^" + text + "$", RegexOptions.IgnoreCase);
	}

	public static bool WildCardMatches(string blockCode, List<string> wildCards, out string matchingWildcard)
	{
		foreach (string wildCard in wildCards)
		{
			if (WildcardUtil.Match(wildCard, blockCode))
			{
				matchingWildcard = wildCard;
				return true;
			}
		}
		matchingWildcard = null;
		return false;
	}

	public static bool WildCardMatch(AssetLocation wildCard, AssetLocation blockCode)
	{
		if (wildCard == blockCode)
		{
			return true;
		}
		string text = Regex.Escape(wildCard.Path).Replace("\\*", "(.*)");
		return Regex.IsMatch(blockCode.Path, "^" + text + "$", RegexOptions.IgnoreCase);
	}

	public static bool WildCardMatches(AssetLocation blockCode, List<AssetLocation> wildCards, out AssetLocation matchingWildcard)
	{
		foreach (AssetLocation wildCard in wildCards)
		{
			if (WildCardMatch(wildCard, blockCode))
			{
				matchingWildcard = wildCard;
				return true;
			}
		}
		matchingWildcard = null;
		return false;
	}

	internal virtual void CreateBasetype(ICoreAPI api, string filepathForLogging, string entryDomain, JObject entityTypeObject)
	{
		loadInherits(api, ref entityTypeObject, entryDomain, filepathForLogging);
		AssetLocation code;
		try
		{
			code = entityTypeObject.GetValue("code", StringComparison.InvariantCultureIgnoreCase).ToObject<AssetLocation>(entryDomain);
		}
		catch (Exception innerException)
		{
			throw new Exception("Asset has no valid code property. Will ignore. Exception thrown:-", innerException);
		}
		Code = code;
		JToken val = default(JToken);
		if (entityTypeObject.TryGetValue("variantgroups", StringComparison.InvariantCultureIgnoreCase, ref val))
		{
			VariantGroups = val.ToObject<RegistryObjectVariantGroup[]>();
			entityTypeObject.Remove(val.Path);
		}
		if (entityTypeObject.TryGetValue("skipVariants", StringComparison.InvariantCultureIgnoreCase, ref val))
		{
			SkipVariants = val.ToObject<AssetLocation[]>(entryDomain);
			entityTypeObject.Remove(val.Path);
		}
		if (entityTypeObject.TryGetValue("allowedVariants", StringComparison.InvariantCultureIgnoreCase, ref val))
		{
			AllowedVariants = val.ToObject<AssetLocation[]>(entryDomain);
			entityTypeObject.Remove(val.Path);
		}
		if (entityTypeObject.TryGetValue("enabled", StringComparison.InvariantCultureIgnoreCase, ref val))
		{
			Enabled = val.ToObject<bool>();
			entityTypeObject.Remove(val.Path);
		}
		else
		{
			Enabled = true;
		}
		jsonObject = entityTypeObject;
	}

	private void loadInherits(ICoreAPI api, ref JObject entityTypeObject, string entryDomain, string parentFileNameForLogging)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		JToken token = default(JToken);
		if (!entityTypeObject.TryGetValue("inheritFrom", StringComparison.InvariantCultureIgnoreCase, ref token))
		{
			return;
		}
		AssetLocation assetLocation = token.ToObject<AssetLocation>(entryDomain).WithPathAppendixOnce(".json");
		IAsset asset = api.Assets.TryGet(assetLocation);
		if (asset != null)
		{
			try
			{
				JObject entityTypeObject2 = JObject.Parse(asset.ToText());
				loadInherits(api, ref entityTypeObject2, entryDomain, assetLocation.ToShortString());
				((JContainer)entityTypeObject2).Merge((object)entityTypeObject, new JsonMergeSettings
				{
					MergeArrayHandling = (MergeArrayHandling)2,
					PropertyNameComparison = StringComparison.InvariantCultureIgnoreCase
				});
				entityTypeObject = entityTypeObject2;
				entityTypeObject.Remove("inheritFrom");
				return;
			}
			catch (Exception ex)
			{
				api.Logger.Error(Lang.Get("File {0} wants to inherit from {1}, but this is not valid json. Exception: {2}.", parentFileNameForLogging, assetLocation, ex));
				return;
			}
		}
		api.Logger.Error(Lang.Get("File {0} wants to inherit from {1}, but this file does not exist. Will ignore.", parentFileNameForLogging, assetLocation));
	}

	internal virtual RegistryObjectType CreateAndPopulate(ICoreServerAPI api, AssetLocation fullcode, JObject jobject, JsonSerializer deserializer, OrderedDictionary<string, string> variant)
	{
		return this;
	}

	protected T CreateResolvedType<T>(ICoreServerAPI api, AssetLocation fullcode, JObject jobject, JsonSerializer deserializer, OrderedDictionary<string, string> variant) where T : RegistryObjectType, new()
	{
		T val = new T
		{
			Code = Code,
			VariantGroups = VariantGroups,
			Enabled = Enabled,
			jsonObject = jobject,
			Variant = variant
		};
		try
		{
			solveByType((JToken)(object)jobject, fullcode.Path, variant);
		}
		catch (Exception e)
		{
			api.Server.Logger.Error("Exception thrown while trying to resolve *byType properties of type {0}, variant {1}. Will ignore most of the attributes. Exception thrown:", Code, fullcode);
			api.Server.Logger.Error(e);
		}
		try
		{
			JsonUtil.PopulateObject(val, (JToken)(object)jobject, deserializer);
		}
		catch (Exception e2)
		{
			api.Server.Logger.Error("Exception thrown while trying to parse json data of the type with code {0}, variant {1}. Will ignore most of the attributes. Exception:", Code, fullcode);
			api.Server.Logger.Error(e2);
		}
		val.Code = fullcode;
		val.jsonObject = null;
		return val;
	}

	protected static void solveByType(JToken json, string codePath, OrderedDictionary<string, string> searchReplace)
	{
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Invalid comparison between Unknown and I4
		JObject val = (JObject)(object)((json is JObject) ? json : null);
		if (val != null)
		{
			List<string> list = null;
			Dictionary<string, JToken> dictionary = null;
			foreach (KeyValuePair<string, JToken> item in val)
			{
				if (!item.Key.EndsWith("byType", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				string key = item.Key.Substring(0, item.Key.Length - "byType".Length);
				JToken value = item.Value;
				foreach (KeyValuePair<string, JToken> item2 in (JObject)(((value is JObject) ? value : null) ?? throw new FormatException("Invalid value at key: " + item.Key)))
				{
					if (WildcardUtil.Match(item2.Key, codePath))
					{
						JToken value2 = item2.Value;
						if (dictionary == null)
						{
							dictionary = new Dictionary<string, JToken>();
						}
						dictionary.Add(key, value2);
						break;
					}
				}
				if (list == null)
				{
					list = new List<string>();
				}
				list.Add(item.Key);
			}
			if (list != null)
			{
				foreach (string item3 in list)
				{
					val.Remove(item3);
				}
				if (dictionary != null)
				{
					foreach (KeyValuePair<string, JToken> item4 in dictionary)
					{
						JToken obj = val[item4.Key];
						JObject val2 = (JObject)(object)((obj is JObject) ? obj : null);
						if (val2 != null)
						{
							((JContainer)val2).Merge((object)item4.Value);
						}
						else
						{
							val[item4.Key] = item4.Value;
						}
					}
				}
			}
			{
				foreach (KeyValuePair<string, JToken> item5 in val)
				{
					solveByType(item5.Value, codePath, searchReplace);
				}
				return;
			}
		}
		if ((int)json.Type == 8)
		{
			string text = (string)((JValue)((json is JValue) ? json : null)).Value;
			if (text.Contains("{"))
			{
				((JValue)((json is JValue) ? json : null)).Value = RegistryObject.FillPlaceHolder(text, searchReplace);
			}
			return;
		}
		JArray val3 = (JArray)(object)((json is JArray) ? json : null);
		if (val3 == null)
		{
			return;
		}
		foreach (JToken item6 in val3)
		{
			solveByType(item6, codePath, searchReplace);
		}
	}
}
