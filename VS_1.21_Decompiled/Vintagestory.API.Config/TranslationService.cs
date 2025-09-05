using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.API.Config;

public class TranslationService : ITranslationService
{
	internal Dictionary<string, string> entryCache = new Dictionary<string, string>();

	private Dictionary<string, KeyValuePair<Regex, string>> regexCache = new Dictionary<string, KeyValuePair<Regex, string>>();

	private Dictionary<string, string> wildcardCache = new Dictionary<string, string>();

	private HashSet<string> notFound = new HashSet<string>();

	private IAssetManager assetManager;

	private readonly ILogger logger;

	internal bool loaded;

	private string preLoadAssetsPath;

	private Dictionary<string, string> preLoadModPaths = new Dictionary<string, string>();

	private bool modWorldConfig;

	public EnumLinebreakBehavior LineBreakBehavior { get; set; }

	public string LanguageCode { get; }

	public TranslationService(string languageCode, ILogger logger, IAssetManager assetManager = null, EnumLinebreakBehavior lbBehavior = EnumLinebreakBehavior.AfterWord)
	{
		LanguageCode = languageCode;
		this.logger = logger;
		this.assetManager = assetManager;
		LineBreakBehavior = lbBehavior;
	}

	public void Load(bool lazyLoad = false)
	{
		preLoadAssetsPath = null;
		if (lazyLoad)
		{
			return;
		}
		loaded = true;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		Dictionary<string, KeyValuePair<Regex, string>> dictionary2 = new Dictionary<string, KeyValuePair<Regex, string>>();
		Dictionary<string, string> dictionary3 = new Dictionary<string, string>();
		foreach (IAsset item in assetManager.Origins.SelectMany((IAssetOrigin p) => from a in p.GetAssets(AssetCategory.lang)
			where a.Name.Equals(LanguageCode + ".json") || a.Name.Equals("worldconfig-" + LanguageCode + ".json")
			select a))
		{
			try
			{
				string text = item.ToText();
				LoadEntries(dictionary, dictionary2, dictionary3, JsonConvert.DeserializeObject<Dictionary<string, string>>(text), item.Location.Domain);
			}
			catch (Exception e)
			{
				logger.Error("Failed to load language file: " + item.Name);
				logger.Error(e);
			}
		}
		entryCache = dictionary;
		regexCache = dictionary2;
		wildcardCache = dictionary3;
	}

	public void PreLoad(string assetsPath, bool lazyLoad = false)
	{
		preLoadAssetsPath = assetsPath;
		if (lazyLoad)
		{
			return;
		}
		loaded = true;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		Dictionary<string, KeyValuePair<Regex, string>> dictionary2 = new Dictionary<string, KeyValuePair<Regex, string>>();
		Dictionary<string, string> dictionary3 = new Dictionary<string, string>();
		foreach (FileInfo item in new DirectoryInfo(Path.Combine(assetsPath, "game", "lang")).EnumerateFiles(LanguageCode + ".json", SearchOption.AllDirectories))
		{
			try
			{
				string text = File.ReadAllText(item.FullName);
				LoadEntries(dictionary, dictionary2, dictionary3, JsonConvert.DeserializeObject<Dictionary<string, string>>(text));
			}
			catch (Exception e)
			{
				logger.Error("Failed to load language file: " + item.Name);
				logger.Error(e);
			}
		}
		entryCache = dictionary;
		regexCache = dictionary2;
		wildcardCache = dictionary3;
	}

	public void PreLoadModWorldConfig(string modPath = null, string modDomain = null, bool lazyLoad = false)
	{
		modWorldConfig = true;
		if (modPath != null && modDomain != null && !preLoadModPaths.ContainsKey(modDomain))
		{
			preLoadModPaths.Add(modDomain, modPath);
		}
		if (lazyLoad || modPath == null || modDomain == null)
		{
			return;
		}
		Dictionary<string, string> entryCache = new Dictionary<string, string>();
		Dictionary<string, KeyValuePair<Regex, string>> regexCache = new Dictionary<string, KeyValuePair<Regex, string>>();
		Dictionary<string, string> wildcardCache = new Dictionary<string, string>();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (modPath == null && modDomain == null)
		{
			dictionary = preLoadModPaths;
		}
		else
		{
			dictionary.Add(modDomain, modPath);
		}
		dictionary.Foreach(delegate(KeyValuePair<string, string> mod)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(mod.Value, "assets", "game", "lang"));
			try
			{
				foreach (FileInfo item in directoryInfo.EnumerateFiles("worldconfig-" + LanguageCode + ".json", SearchOption.AllDirectories))
				{
					try
					{
						string text = File.ReadAllText(item.FullName);
						LoadEntries(entryCache, regexCache, wildcardCache, JsonConvert.DeserializeObject<Dictionary<string, string>>(text));
					}
					catch (Exception e)
					{
						logger.Error("Failed to load language file: " + item.Name);
						logger.Error(e);
					}
				}
			}
			catch
			{
				logger.Error("Failed to find language folder: " + directoryInfo.FullName);
			}
		});
		this.entryCache.AddRange(entryCache);
		this.regexCache.AddRange(regexCache);
		this.wildcardCache.AddRange(wildcardCache);
	}

	protected void EnsureLoaded()
	{
		if (loaded)
		{
			return;
		}
		if (preLoadAssetsPath != null)
		{
			PreLoad(preLoadAssetsPath);
			if (modWorldConfig)
			{
				PreLoadModWorldConfig();
			}
		}
		else
		{
			Load();
		}
	}

	public void Invalidate()
	{
		loaded = false;
	}

	protected string Format(string value, params object[] args)
	{
		if (value.ContainsFast("{p"))
		{
			return PluralFormat(value, args);
		}
		return TryFormat(value, args);
	}

	private string TryFormat(string value, params object[] args)
	{
		string result;
		try
		{
			result = string.Format(value, args);
		}
		catch (Exception e)
		{
			logger.Error(e);
			result = value;
			if (logger != null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("Translation string format exception thrown for: \"");
				foreach (char c in value)
				{
					stringBuilder.Append(c);
					if (c == '{' || c == '}')
					{
						stringBuilder.Append(c);
					}
				}
				stringBuilder.Append("\"\n   Args were: ");
				for (int j = 0; j < args.Length; j++)
				{
					if (j > 0)
					{
						stringBuilder.Append(", ");
					}
					stringBuilder.Append(args[j].ToString());
				}
				try
				{
					logger.Warning(stringBuilder.ToString());
				}
				catch (Exception e2)
				{
					logger.Error("Exception thrown when trying to print exception message for an incorrect translation entry. Exception: ");
					logger.Error(e2);
				}
			}
		}
		return result;
	}

	private string PluralFormat(string value, object[] args)
	{
		int num = value.IndexOfOrdinal("{p");
		if (value.Length < num + 5)
		{
			return TryFormat(value, args);
		}
		int num2 = num + 4;
		int num3 = value.IndexOf('}', num2);
		char c = value[num + 2];
		if (c < '0' || c > '9')
		{
			return TryFormat(value, args);
		}
		if (num3 < 0)
		{
			return TryFormat(value, args);
		}
		int num4 = c - 48;
		if ((c = value[num + 3]) != ':')
		{
			if (value[num + 4] != ':' || c < '0' || c > '9')
			{
				return TryFormat(value, args);
			}
			num4 = num4 * 10 + c - 48;
			num2++;
		}
		if (num4 >= args.Length)
		{
			throw new IndexOutOfRangeException("Index out of range: Plural format {p#:...} referenced an argument " + num4 + " but only " + args.Length + " arguments were available in the code");
		}
		float n = 0f;
		try
		{
			n = float.Parse(args[num4].ToString());
		}
		catch (Exception)
		{
		}
		string value2 = value.Substring(0, num);
		string input = value.Substring(num2, num3 - num2);
		string value3 = value.Substring(num3 + 1);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(TryFormat(value2, args));
		stringBuilder.Append(BuildPluralFormat(input, n));
		stringBuilder.Append(Format(value3, args));
		return stringBuilder.ToString();
	}

	internal static string BuildPluralFormat(string input, float n)
	{
		string[] array = input.Split('|');
		int round = 3;
		if (array.Length >= 2)
		{
			if (!TryGuessRounding(array[1], out round))
			{
				TryGuessRounding(array[^1], out round);
			}
			if (round < 0 || round > 15)
			{
				round = 3;
			}
		}
		int num = (int)Math.Ceiling(Math.Round(n, round));
		if (num < 0 || num >= array.Length)
		{
			num = array.Length - 1;
		}
		if (array.Length >= 2)
		{
			if (n == 1f)
			{
				num = 1;
			}
			else if (n < 1f)
			{
				num = 0;
			}
		}
		return WithNumberFormatting(array[num], n);
	}

	private static bool TryGuessRounding(string entry, out int round)
	{
		string partB;
		string numberFormattingFrom = GetNumberFormattingFrom(entry, out partB);
		if (numberFormattingFrom.IndexOf('.') > 0)
		{
			round = numberFormattingFrom.Length - numberFormattingFrom.IndexOf('.') - 1;
			return true;
		}
		if (numberFormattingFrom.Length > 0)
		{
			round = 0;
			return true;
		}
		round = 3;
		return false;
	}

	internal static string GetNumberFormattingFrom(string rawResult, out string partB)
	{
		int startIndexOfNumberFormat = GetStartIndexOfNumberFormat(rawResult);
		if (startIndexOfNumberFormat >= 0)
		{
			int num = startIndexOfNumberFormat;
			while (++num < rawResult.Length)
			{
				char c = rawResult[num];
				if (c != '#' && c != '.' && c != '0' && c != ',')
				{
					break;
				}
			}
			partB = rawResult.Substring(num);
			return rawResult.Substring(startIndexOfNumberFormat, num - startIndexOfNumberFormat);
		}
		partB = rawResult;
		return "";
	}

	private static int GetStartIndexOfNumberFormat(string rawResult)
	{
		int num = rawResult.IndexOf('#');
		int num2 = rawResult.IndexOf('0');
		int result = -1;
		if (num >= 0 && num2 >= 0)
		{
			result = Math.Min(num, num2);
		}
		else if (num >= 0)
		{
			result = num;
		}
		else if (num2 >= 0)
		{
			result = num2;
		}
		return result;
	}

	internal static string WithNumberFormatting(string rawResult, float n)
	{
		int startIndexOfNumberFormat = GetStartIndexOfNumberFormat(rawResult);
		if (startIndexOfNumberFormat < 0)
		{
			return rawResult;
		}
		string text = rawResult.Substring(0, startIndexOfNumberFormat);
		string partB;
		string numberFormattingFrom = GetNumberFormattingFrom(rawResult, out partB);
		string text2;
		try
		{
			text2 = ((numberFormattingFrom.Length != 1 || n != 0f) ? n.ToString(numberFormattingFrom, GlobalConstants.DefaultCultureInfo) : "0");
		}
		catch (Exception)
		{
			text2 = n.ToString(GlobalConstants.DefaultCultureInfo);
		}
		return text + text2 + partB;
	}

	public string GetIfExists(string key, params object[] args)
	{
		EnsureLoaded();
		if (!entryCache.TryGetValue(KeyWithDomain(key), out var value))
		{
			return null;
		}
		return Format(value, args);
	}

	public string Get(string key, params object[] args)
	{
		return Format(GetUnformatted(key), args);
	}

	public IDictionary<string, string> GetAllEntries()
	{
		EnsureLoaded();
		return entryCache;
	}

	public string GetUnformatted(string key)
	{
		EnsureLoaded();
		if (!entryCache.TryGetValue(KeyWithDomain(key), out var value))
		{
			return key;
		}
		return value;
	}

	public string GetMatching(string key, params object[] args)
	{
		EnsureLoaded();
		string matchingIfExists = GetMatchingIfExists(KeyWithDomain(key), args);
		if (!string.IsNullOrEmpty(matchingIfExists))
		{
			return matchingIfExists;
		}
		return Format(key, args);
	}

	public bool HasTranslation(string key, bool findWildcarded = true)
	{
		return HasTranslation(key, findWildcarded, logErrors: true);
	}

	public bool HasTranslation(string key, bool findWildcarded, bool logErrors)
	{
		EnsureLoaded();
		string validKey = KeyWithDomain(key);
		if (entryCache.ContainsKey(validKey))
		{
			return true;
		}
		if (findWildcarded)
		{
			if (!key.Contains(":"))
			{
				key = "game:" + key;
			}
			bool flag = wildcardCache.Any((KeyValuePair<string, string> pair) => key.StartsWithFast(pair.Key));
			if (!flag)
			{
				flag = regexCache.Values.Any((KeyValuePair<Regex, string> pair) => pair.Key.IsMatch(validKey));
			}
			if (!flag && logErrors && !key.Contains("desc-") && notFound.Add(key))
			{
				logger.VerboseDebug("Lang key not found: " + key.Replace("{", "{{").Replace("}", "}}"));
			}
			return flag;
		}
		return false;
	}

	public void UseAssetManager(IAssetManager assetManager)
	{
		this.assetManager = assetManager;
	}

	public string GetMatchingIfExists(string key, params object[] args)
	{
		EnsureLoaded();
		string validKey = KeyWithDomain(key);
		if (entryCache.TryGetValue(validKey, out var value))
		{
			return Format(value, args);
		}
		using (IEnumerator<KeyValuePair<string, string>> enumerator = wildcardCache.Where((KeyValuePair<string, string> pair) => validKey.StartsWithFast(pair.Key)).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return Format(enumerator.Current.Value, args);
			}
		}
		return (from pair in regexCache.Values
			where pair.Key.IsMatch(validKey)
			select Format(pair.Value, args)).FirstOrDefault();
	}

	private void LoadEntries(Dictionary<string, string> entryCache, Dictionary<string, KeyValuePair<Regex, string>> regexCache, Dictionary<string, string> wildcardCache, Dictionary<string, string> entries, string domain = "game")
	{
		foreach (KeyValuePair<string, string> entry in entries)
		{
			LoadEntry(entryCache, regexCache, wildcardCache, entry, domain);
		}
	}

	private void LoadEntry(Dictionary<string, string> entryCache, Dictionary<string, KeyValuePair<Regex, string>> regexCache, Dictionary<string, string> wildcardCache, KeyValuePair<string, string> entry, string domain = "game")
	{
		string text = KeyWithDomain(entry.Key, domain);
		switch (text.CountChars('*'))
		{
		case 0:
			entryCache[text] = entry.Value;
			return;
		case 1:
			if (text.EndsWith('*'))
			{
				wildcardCache[text.TrimEnd('*')] = entry.Value;
				return;
			}
			break;
		}
		Regex key = new Regex("^" + text.Replace("*", "(.*)") + "$", RegexOptions.Compiled);
		regexCache[text] = new KeyValuePair<Regex, string>(key, entry.Value);
	}

	private static string KeyWithDomain(string key, string domain = "game")
	{
		if (key.Contains(':'))
		{
			return key;
		}
		return new StringBuilder(domain).Append(':').Append(key).ToString();
	}

	public void InitialiseSearch()
	{
		regexCache.Values.Any((KeyValuePair<Regex, string> pair) => pair.Key.IsMatch("nonsense_value_and_fairly_longgg"));
	}
}
