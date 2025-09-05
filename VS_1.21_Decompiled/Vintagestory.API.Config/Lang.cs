using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Config;

public static class Lang
{
	public static Dictionary<string, ITranslationService> AvailableLanguages { get; } = new Dictionary<string, ITranslationService>();

	public static string CurrentLocale { get; private set; }

	public static string DefaultLocale { get; set; } = "en";

	public static void Load(ILogger logger, IAssetManager assetManager, string language = "en")
	{
		CurrentLocale = language;
		JsonObject[] array = JsonObject.FromJson(File.ReadAllText(Path.Combine(GamePaths.AssetsPath, "game", "lang", "languages.json"))).AsArray();
		foreach (JsonObject jsonObject in array)
		{
			string text = jsonObject["code"].AsString();
			EnumLinebreakBehavior lbBehavior = (EnumLinebreakBehavior)Enum.Parse(typeof(EnumLinebreakBehavior), jsonObject["linebreakBehavior"].AsString("AfterWord"));
			LoadLanguage(logger, assetManager, text, text != CurrentLocale, lbBehavior);
		}
		if (!AvailableLanguages.ContainsKey(language))
		{
			logger.Error("Language '{0}' not found. Will default to english.", language);
			CurrentLocale = "en";
		}
	}

	public static void ChangeLanguage(string languageCode)
	{
		CurrentLocale = languageCode;
	}

	public static void LoadLanguage(ILogger logger, IAssetManager assetManager, string languageCode = "en", bool lazyLoad = false, EnumLinebreakBehavior lbBehavior = EnumLinebreakBehavior.AfterWord)
	{
		if (AvailableLanguages.ContainsKey(languageCode))
		{
			AvailableLanguages[languageCode].UseAssetManager(assetManager);
			AvailableLanguages[languageCode].Load(lazyLoad);
		}
		else
		{
			TranslationService translationService = new TranslationService(languageCode, logger, assetManager, lbBehavior);
			translationService.Load(lazyLoad);
			AvailableLanguages.Add(languageCode, translationService);
		}
	}

	public static void PreLoad(ILogger logger, string assetsPath, string defaultLanguage = "en")
	{
		CurrentLocale = defaultLanguage;
		JsonObject[] array = JsonObject.FromJson(File.ReadAllText(Path.Combine(GamePaths.AssetsPath, "game", "lang", "languages.json"))).AsArray();
		bool flag = false;
		JsonObject[] array2 = array;
		foreach (JsonObject jsonObject in array2)
		{
			string text = jsonObject["code"].AsString();
			EnumLinebreakBehavior lbBehavior = (EnumLinebreakBehavior)Enum.Parse(typeof(EnumLinebreakBehavior), jsonObject["linebreakBehavior"].AsString("AfterWord"));
			TranslationService translationService = new TranslationService(text, logger, null, lbBehavior);
			bool lazyLoad = text != defaultLanguage;
			translationService.PreLoad(assetsPath, lazyLoad);
			AvailableLanguages[text] = translationService;
			if (text == defaultLanguage)
			{
				flag = true;
			}
		}
		if (defaultLanguage != "en" && !flag)
		{
			logger.Error("Language '{0}' not found. Will default to english.", defaultLanguage);
			AvailableLanguages["en"].PreLoad(assetsPath);
			CurrentLocale = "en";
		}
	}

	public static void PreLoadModWorldConfig(string modPath, string modDomain, string defaultLanguage = "en")
	{
		JsonObject[] array = JsonObject.FromJson(File.ReadAllText(Path.Combine(GamePaths.AssetsPath, "game", "lang", "languages.json"))).AsArray();
		foreach (JsonObject jsonObject in array)
		{
			string text = jsonObject["code"].AsString();
			_ = (EnumLinebreakBehavior)Enum.Parse(typeof(EnumLinebreakBehavior), jsonObject["linebreakBehavior"].AsString("AfterWord"));
			bool lazyLoad = text != defaultLanguage;
			AvailableLanguages[text].PreLoadModWorldConfig(modPath, modDomain, lazyLoad);
		}
	}

	public static string GetIfExists(string key, params object[] args)
	{
		if (!HasTranslation(key))
		{
			return AvailableLanguages[DefaultLocale].GetIfExists(key, args);
		}
		return AvailableLanguages[CurrentLocale].GetIfExists(key, args);
	}

	public static string GetL(string langcode, string key, params object[] args)
	{
		if (!AvailableLanguages.TryGetValue(langcode, out var value) || !value.HasTranslation(key, findWildcarded: false))
		{
			return AvailableLanguages[DefaultLocale].Get(key, args);
		}
		return value.Get(key, args);
	}

	public static string GetMatchingL(string langcode, string key, params object[] args)
	{
		if (!AvailableLanguages.TryGetValue(langcode, out var value) || !value.HasTranslation(key))
		{
			return AvailableLanguages[DefaultLocale].GetMatching(key, args);
		}
		return value.GetMatching(key, args);
	}

	public static string Get(string key, params object[] args)
	{
		if (!HasTranslation(key, findWildcarded: false))
		{
			return AvailableLanguages[DefaultLocale].Get(key, args);
		}
		return AvailableLanguages[CurrentLocale].Get(key, args);
	}

	public static string GetWithFallback(string key, string fallbackKey, params object[] args)
	{
		if (!HasTranslation(key, findWildcarded: false))
		{
			if (!HasTranslation(fallbackKey, findWildcarded: false))
			{
				return AvailableLanguages[DefaultLocale].Get(key, args);
			}
			return AvailableLanguages[CurrentLocale].Get(fallbackKey, args);
		}
		return AvailableLanguages[CurrentLocale].Get(key, args);
	}

	public static string GetUnformatted(string key)
	{
		if (!HasTranslation(key))
		{
			return AvailableLanguages[DefaultLocale].GetUnformatted(key);
		}
		return AvailableLanguages[CurrentLocale].GetUnformatted(key);
	}

	public static string GetMatching(string key, params object[] args)
	{
		if (!HasTranslation(key))
		{
			return AvailableLanguages[DefaultLocale].GetMatching(key, args);
		}
		return AvailableLanguages[CurrentLocale].GetMatching(key, args);
	}

	public static string GetMatchingIfExists(string key, params object[] args)
	{
		if (!HasTranslation(key, findWildcarded: true, logErrors: false))
		{
			return AvailableLanguages[DefaultLocale].GetMatchingIfExists(key, args);
		}
		return AvailableLanguages[CurrentLocale].GetMatchingIfExists(key, args);
	}

	public static IDictionary<string, string> GetAllEntries()
	{
		Dictionary<string, string> source = AvailableLanguages[DefaultLocale].GetAllEntries().ToDictionary((KeyValuePair<string, string> p) => p.Key, (KeyValuePair<string, string> p) => p.Value);
		Dictionary<string, string> currentEntries = AvailableLanguages[CurrentLocale].GetAllEntries().ToDictionary((KeyValuePair<string, string> p) => p.Key, (KeyValuePair<string, string> p) => p.Value);
		foreach (KeyValuePair<string, string> item in source.Where((KeyValuePair<string, string> entry) => !currentEntries.ContainsKey(entry.Key)))
		{
			currentEntries.Add(item.Key, item.Value);
		}
		return currentEntries;
	}

	public static bool HasTranslation(string key, bool findWildcarded = true, bool logErrors = true)
	{
		return AvailableLanguages[CurrentLocale].HasTranslation(key, findWildcarded, logErrors);
	}

	public static void InitialiseSearch()
	{
		AvailableLanguages[CurrentLocale].InitialiseSearch();
	}

	public static string GetNamePlaceHolder(AssetLocation code)
	{
		string text = "";
		string[] array = code.Path.Split('-');
		for (int i = 0; i < array.Length; i++)
		{
			if (!(array[i] == "north") && !(array[i] == "east") && !(array[i] == "west") && !(array[i] == "south") && !(array[i] == "up") && !(array[i] == "down"))
			{
				if (i > 0)
				{
					text += " ";
				}
				if (i > 0 && i == array.Length - 1)
				{
					text += "(";
				}
				text = ((i != 0) ? (text + array[i]) : (text + array[i].First().ToString().ToUpper() + array[i].Substring(1)));
				if (i > 0 && i == array.Length - 1)
				{
					text += ")";
				}
			}
		}
		return text;
	}

	public static bool UsesNonLatinCharacters(string lang)
	{
		if (!(lang == "ar") && !lang.StartsWithOrdinal("zh-"))
		{
			switch (lang)
			{
			default:
				return lang == "ru";
			case "ja":
			case "ko":
			case "th":
			case "uk":
				break;
			}
		}
		return true;
	}
}
