using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using Vintagestory.API.Config;

namespace Vintagestory.API.Util;

public static class StringUtil
{
	public unsafe static int GetNonRandomizedHashCode(this string str)
	{
		fixed (char* ptr = str)
		{
			uint num = 352654597u;
			uint num2 = num;
			uint* ptr2 = (uint*)ptr;
			int num3 = str.Length;
			while (num3 > 2)
			{
				num3 -= 4;
				num = (BitOperations.RotateLeft(num, 5) + num) ^ *ptr2;
				num2 = (BitOperations.RotateLeft(num2, 5) + num2) ^ ptr2[1];
				ptr2 += 2;
			}
			if (num3 > 0)
			{
				num2 = (BitOperations.RotateLeft(num2, 5) + num2) ^ *ptr2;
			}
			return (int)(num + num2 * 1566083941);
		}
	}

	public static int IndexOfOrdinal(this string a, string b)
	{
		return a.IndexOf(b, StringComparison.Ordinal);
	}

	public static bool StartsWithOrdinal(this string a, string b)
	{
		return a.StartsWith(b, StringComparison.Ordinal);
	}

	public static bool EndsWithOrdinal(this string a, string b)
	{
		return a.EndsWith(b, StringComparison.Ordinal);
	}

	public static int CompareOrdinal(this string a, string b)
	{
		return string.CompareOrdinal(a, b);
	}

	public static string UcFirst(this string text)
	{
		return text.Substring(0, 1).ToUpperInvariant() + text.Substring(1);
	}

	public static bool ToBool(this string text, bool defaultValue = false)
	{
		switch (text?.ToLowerInvariant())
		{
		case "true":
		case "yes":
		case "1":
			return true;
		case "false":
		case "no":
		case "0":
			return false;
		default:
			return defaultValue;
		}
	}

	public static string RemoveFileEnding(this string text)
	{
		return text.Substring(0, text.IndexOf('.'));
	}

	public static int ToInt(this string text, int defaultValue = 0)
	{
		if (!int.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static long ToLong(this string text, long defaultValue = 0L)
	{
		if (!long.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static float ToFloat(this string text, float defaultValue = 0f)
	{
		if (!float.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static double ToDouble(this string text, double defaultValue = 0.0)
	{
		if (!double.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static double? ToDoubleOrNull(this string text, double? defaultValue = 0.0)
	{
		if (!double.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static float? ToFloatOrNull(this string text, float? defaultValue = 0f)
	{
		if (!float.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public static int CountChars(this string text, char c)
	{
		int num = 0;
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == c)
			{
				num++;
			}
		}
		return num;
	}

	public static bool ContainsFast(this string value, string reference)
	{
		if (reference.Length > value.Length)
		{
			return false;
		}
		int num = 0;
		for (int i = 0; i < value.Length; i++)
		{
			num = ((value[i] == reference[num]) ? (num + 1) : 0);
			if (num >= reference.Length)
			{
				return true;
			}
		}
		return false;
	}

	public static bool ContainsFast(this string value, char reference)
	{
		for (int i = 0; i < value.Length; i++)
		{
			if (value[i] == reference)
			{
				return true;
			}
		}
		return false;
	}

	public static bool StartsWithFast(this string value, string reference)
	{
		if (reference.Length > value.Length)
		{
			return false;
		}
		for (int num = reference.Length - 1; num >= 0; num--)
		{
			if (value[num] != reference[num])
			{
				return false;
			}
		}
		return true;
	}

	public static bool StartsWithFast(this string value, string reference, int offset)
	{
		if (reference.Length + offset > value.Length)
		{
			return false;
		}
		for (int num = reference.Length + offset - 1; num >= offset; num--)
		{
			if (value[num] != reference[num - offset])
			{
				return false;
			}
		}
		return true;
	}

	public static bool EqualsFast(this string value, string reference)
	{
		if (reference.Length != value.Length)
		{
			return false;
		}
		for (int num = reference.Length - 1; num >= 0; num--)
		{
			if (value[num] != reference[num])
			{
				return false;
			}
		}
		return true;
	}

	public static bool EqualsFastIgnoreCase(this string value, string reference)
	{
		if (reference.Length != value.Length)
		{
			return false;
		}
		for (int num = reference.Length - 1; num >= 0; num--)
		{
			char c;
			char c2;
			if ((c = value[num]) != (c2 = reference[num]) && ((c & 0xFFDF) != (c2 & 0xFFDF) || (c & 0xFFDF) < 65 || (c & 0xFFDF) > 90))
			{
				return false;
			}
		}
		return true;
	}

	public static bool FastStartsWith(string value, string reference, int len)
	{
		if (len > reference.Length)
		{
			throw new ArgumentException("reference must be longer than len");
		}
		if (len > value.Length)
		{
			return false;
		}
		for (int i = 0; i < len; i++)
		{
			if (value[i] != reference[i])
			{
				return false;
			}
		}
		return true;
	}

	public static string ToSearchFriendly(this string stIn)
	{
		string text = stIn.Normalize(NormalizationForm.FormD);
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in text)
		{
			if (c == '«' || c == '»' || c == '"' || c == '(' || c == ')')
			{
				stringBuilder.Append(' ');
			}
			else if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
	}
}
