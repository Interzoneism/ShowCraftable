using System;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;

namespace Vintagestory.API.Util;

public static class WildcardUtil
{
	public static AssetLocation WildCardReplace(this AssetLocation code, AssetLocation search, AssetLocation replace)
	{
		if (search == code)
		{
			return search;
		}
		if (code == null || (search.Domain != "*" && search.Domain != code.Domain))
		{
			return null;
		}
		string text = Regex.Escape(search.Path).Replace("\\*", "(.*)");
		Match match = Regex.Match(code.Path, "^" + text + "$");
		if (!match.Success)
		{
			return null;
		}
		string text2 = replace.Path;
		for (int i = 1; i < match.Groups.Count; i++)
		{
			CaptureCollection captures = match.Groups[i].Captures;
			for (int j = 0; j < captures.Count; j++)
			{
				Capture capture = captures[j];
				int startIndex = text2.IndexOf('*');
				text2 = text2.Remove(startIndex, 1).Insert(startIndex, capture.Value);
			}
		}
		return new AssetLocation(code.Domain, text2);
	}

	public static bool Match(string needle, string haystack)
	{
		return fastMatch(needle, haystack);
	}

	public static bool Match(string[] needles, string haystack)
	{
		for (int i = 0; i < needles.Length; i++)
		{
			if (fastMatch(needles[i], haystack))
			{
				return true;
			}
		}
		return false;
	}

	public static bool Match(AssetLocation needle, AssetLocation haystack)
	{
		if (needle.Domain != "*" && needle.Domain != haystack.Domain)
		{
			return false;
		}
		return fastMatch(needle.Path, haystack.Path);
	}

	public static bool Match(AssetLocation wildCard, AssetLocation inCode, string[] allowedVariants)
	{
		if (wildCard.Equals(inCode))
		{
			return true;
		}
		int num;
		if (inCode == null || (wildCard.Domain != "*" && !wildCard.Domain.Equals(inCode.Domain)) || ((num = wildCard.Path.IndexOf('*')) == -1 && wildCard.Path.IndexOf('(') == -1))
		{
			return false;
		}
		if (num == wildCard.Path.Length - 1)
		{
			if (!StringUtil.FastStartsWith(inCode.Path, wildCard.Path, num))
			{
				return false;
			}
		}
		else
		{
			if (!StringUtil.FastStartsWith(inCode.Path, wildCard.Path, num))
			{
				return false;
			}
			string text = Regex.Escape(wildCard.Path).Replace("\\*", "(.*)");
			if (!Regex.IsMatch(inCode.Path, "^" + text + "$", RegexOptions.None))
			{
				return false;
			}
		}
		if (allowedVariants != null && !MatchesVariants(wildCard, inCode, allowedVariants))
		{
			return false;
		}
		return true;
	}

	public static bool MatchesVariants(AssetLocation wildCard, AssetLocation inCode, string[] allowedVariants)
	{
		int num = wildCard.Path.IndexOf('*');
		int num2 = wildCard.Path.Length - num - 1;
		if (inCode.Path.Length <= num)
		{
			return false;
		}
		string text = inCode.Path.Substring(num);
		if (text.Length - num2 <= 0)
		{
			return false;
		}
		string value = text.Substring(0, text.Length - num2);
		return allowedVariants.Contains(value);
	}

	public static string GetWildcardValue(AssetLocation wildCard, AssetLocation inCode)
	{
		if (inCode == null || (wildCard.Domain != "*" && !wildCard.Domain.Equals(inCode.Domain)))
		{
			return null;
		}
		if (!wildCard.Path.Contains('*'))
		{
			return null;
		}
		string text = Regex.Escape(wildCard.Path).Replace("\\*", "(.*)");
		Match match = Regex.Match(inCode.Path, "^" + text + "$", RegexOptions.None);
		if (!match.Success)
		{
			return null;
		}
		return match.Groups[1].Captures[0].Value;
	}

	private static bool fastMatch(string needle, string haystack)
	{
		if (haystack == null)
		{
			throw new ArgumentNullException("Text cannot be null");
		}
		if (needle.Length == 0)
		{
			return false;
		}
		if (needle[0] == '@')
		{
			return Regex.IsMatch(haystack, "^" + needle.Substring(1) + "$", RegexOptions.None);
		}
		int length = needle.Length;
		for (int i = 0; i < length; i++)
		{
			char c = needle[i];
			if (c == '*')
			{
				int num = length - 1 - i;
				if (num == 0)
				{
					return true;
				}
				int num2 = needle.IndexOf('*', i + 1);
				if (num2 >= 0)
				{
					if (needle.IndexOf('*', num2 + 1) >= 0)
					{
						needle = Regex.Escape(needle).Replace("\\*", ".*");
						return Regex.IsMatch(haystack, "^" + needle + "$", RegexOptions.IgnoreCase);
					}
					if (haystack.Length < needle.Length - 2)
					{
						return false;
					}
					int num3 = length - (num2 + 1);
					if (!EndsWith(haystack, needle, num3))
					{
						return false;
					}
					string value = needle.Substring(i + 1, num2 - (i + 1)).ToLowerInvariant();
					if (i == 0 && num3 == 0)
					{
						return haystack.ToLowerInvariant().Contains(value);
					}
					return haystack.Substring(i, haystack.Length - i - num3).ToLowerInvariant().Contains(value);
				}
				if (haystack.Length >= needle.Length - 1)
				{
					return EndsWith(haystack, needle, num);
				}
				return false;
			}
			if (haystack.Length <= i)
			{
				return false;
			}
			char c2 = haystack[i];
			if (c != c2 && char.ToLowerInvariant(c) != char.ToLowerInvariant(c2))
			{
				return false;
			}
		}
		return needle.Length == haystack.Length;
	}

	private static bool EndsWith(string haystack, string needle, int endCharsCount)
	{
		int num = haystack.Length - 1;
		int num2 = needle.Length - 1;
		for (int i = 0; i < endCharsCount; i++)
		{
			char c = haystack[num - i];
			char c2 = needle[num2 - i];
			if (c2 != c && char.ToLowerInvariant(c2) != char.ToLowerInvariant(c))
			{
				return false;
			}
		}
		return true;
	}

	internal static bool fastExactMatch(string needle, string haystack)
	{
		if (haystack.Length != needle.Length)
		{
			return false;
		}
		for (int num = needle.Length - 1; num >= 0; num--)
		{
			char c = needle[num];
			char c2 = haystack[num];
			if (c != c2 && char.ToLowerInvariant(c) != char.ToLowerInvariant(c2))
			{
				return false;
			}
		}
		return true;
	}

	internal static bool fastMatch(string needle, string haystack, string needleAsRegex)
	{
		int length = needleAsRegex.Length;
		if (length > 0 && needleAsRegex[0] == '^')
		{
			return Regex.IsMatch(haystack, needleAsRegex, RegexOptions.IgnoreCase);
		}
		if (haystack.Length < needle.Length - 1)
		{
			return false;
		}
		if (length != 0 && !EndsWith(haystack, needle, length))
		{
			return false;
		}
		int num = needle.Length - length - 1;
		for (int i = 0; i < num; i++)
		{
			char c = needle[i];
			char c2 = haystack[i];
			if (c != c2 && char.ToLowerInvariant(c) != char.ToLowerInvariant(c2))
			{
				return false;
			}
		}
		return true;
	}

	internal static string Prepare(string needle)
	{
		if (needle[0] == '@')
		{
			return "^" + needle.Substring(1) + "$";
		}
		int num = needle.IndexOf('*');
		if (num == -1)
		{
			return null;
		}
		if (needle[0] != '^' && needle.IndexOf('*', num + 1) < 0)
		{
			return needle.Substring(num + 1);
		}
		needle = Regex.Escape(needle).Replace("\\*", ".*");
		return "^" + needle + "$";
	}
}
