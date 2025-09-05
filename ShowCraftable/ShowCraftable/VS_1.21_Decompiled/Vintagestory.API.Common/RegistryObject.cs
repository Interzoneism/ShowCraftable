using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public abstract class RegistryObject
{
	public AssetLocation Code;

	public OrderedDictionary<string, string> VariantStrict = new OrderedDictionary<string, string>();

	public RelaxedReadOnlyDictionary<string, string> Variant;

	public string Class;

	public RegistryObject()
	{
		Variant = new RelaxedReadOnlyDictionary<string, string>(VariantStrict);
	}

	public AssetLocation CodeWithPath(string path)
	{
		return Code.CopyWithPath(path);
	}

	public string CodeWithoutParts(int componentsToRemove)
	{
		int num = Code.Path.Length;
		int length = 0;
		while (--num > 0 && componentsToRemove > 0)
		{
			if (Code.Path[num] == '-')
			{
				length = num;
				componentsToRemove--;
			}
		}
		return Code.Path.Substring(0, length);
	}

	public string CodeEndWithoutParts(int componentsToRemove)
	{
		int num = 0;
		int num2 = 0;
		while (++num < Code.Path.Length && componentsToRemove > 0)
		{
			if (Code.Path[num] == '-')
			{
				num2 = num + 1;
				componentsToRemove--;
			}
		}
		return Code.Path.Substring(num2, Code.Path.Length - num2);
	}

	public AssetLocation CodeWithParts(params string[] components)
	{
		if (Code == null)
		{
			return null;
		}
		AssetLocation assetLocation = Code.CopyWithPath(CodeWithoutParts(components.Length));
		for (int i = 0; i < components.Length; i++)
		{
			assetLocation.Path = assetLocation.Path + "-" + components[i];
		}
		return assetLocation;
	}

	public AssetLocation CodeWithParts(string component)
	{
		if (Code == null)
		{
			return null;
		}
		return Code.CopyWithPath(CodeWithoutParts(1) + "-" + component);
	}

	public AssetLocation CodeWithVariant(string type, string value)
	{
		StringBuilder stringBuilder = new StringBuilder(FirstCodePart());
		foreach (KeyValuePair<string, string> item in Variant)
		{
			stringBuilder.Append("-");
			if (item.Key == type)
			{
				stringBuilder.Append(value);
			}
			else
			{
				stringBuilder.Append(item.Value);
			}
		}
		return new AssetLocation(Code.Domain, stringBuilder.ToString());
	}

	public AssetLocation CodeWithVariants(Dictionary<string, string> valuesByType)
	{
		StringBuilder stringBuilder = new StringBuilder(FirstCodePart());
		foreach (KeyValuePair<string, string> item in Variant)
		{
			stringBuilder.Append("-");
			if (valuesByType.TryGetValue(item.Key, out var value))
			{
				stringBuilder.Append(value);
			}
			else
			{
				stringBuilder.Append(item.Value);
			}
		}
		return new AssetLocation(Code.Domain, stringBuilder.ToString());
	}

	public AssetLocation CodeWithVariants(string[] types, string[] values)
	{
		StringBuilder stringBuilder = new StringBuilder(FirstCodePart());
		foreach (KeyValuePair<string, string> item in Variant)
		{
			stringBuilder.Append("-");
			int num = types.IndexOf(item.Key);
			if (num >= 0)
			{
				stringBuilder.Append(values[num]);
			}
			else
			{
				stringBuilder.Append(item.Value);
			}
		}
		return new AssetLocation(Code.Domain, stringBuilder.ToString());
	}

	public AssetLocation CodeWithPart(string part, int atPosition = 0)
	{
		if (Code == null)
		{
			return null;
		}
		AssetLocation assetLocation = Code.Clone();
		string[] array = assetLocation.Path.Split('-');
		array[atPosition] = part;
		assetLocation.Path = string.Join("-", array);
		return assetLocation;
	}

	public string LastCodePart(int posFromRight = 0)
	{
		if (Code == null)
		{
			return null;
		}
		if (posFromRight == 0 && !Code.Path.Contains('-'))
		{
			return Code.Path;
		}
		string[] array = Code.Path.Split('-');
		if (array.Length - 1 - posFromRight < 0)
		{
			return null;
		}
		return array[array.Length - 1 - posFromRight];
	}

	public string FirstCodePart(int posFromLeft = 0)
	{
		if (Code == null)
		{
			return null;
		}
		if (posFromLeft == 0 && !Code.Path.Contains('-'))
		{
			return Code.Path;
		}
		string[] array = Code.Path.Split('-');
		if (posFromLeft > array.Length - 1)
		{
			return null;
		}
		return array[posFromLeft];
	}

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
		if (Code != null)
		{
			return WildcardUtil.Match(wildCard, Code);
		}
		return false;
	}

	public bool WildCardMatch(string[] wildcards)
	{
		foreach (string wildCard in wildcards)
		{
			if (WildCardMatch(wildCard))
			{
				return true;
			}
		}
		return false;
	}

	public bool WildCardMatch(string wildCard)
	{
		if (Code != null)
		{
			return WildcardUtil.Match(wildCard, Code.Path);
		}
		return false;
	}

	public static AssetLocation FillPlaceHolder(AssetLocation input, OrderedDictionary<string, string> searchReplace)
	{
		foreach (KeyValuePair<string, string> item in searchReplace)
		{
			input.Path = FillPlaceHolder(input.Path, item.Key, item.Value);
		}
		return input;
	}

	public static string FillPlaceHolder(string input, OrderedDictionary<string, string> searchReplace)
	{
		foreach (KeyValuePair<string, string> item in searchReplace)
		{
			input = FillPlaceHolder(input, item.Key, item.Value);
		}
		return input;
	}

	public static string FillPlaceHolder(string input, string search, string replace)
	{
		string pattern = "\\{((" + search + ")|([^\\{\\}]*\\|" + search + ")|(" + search + "\\|[^\\{\\}]*)|([^\\{\\}]*\\|" + search + "\\|[^\\{\\}]*))\\}";
		return Regex.Replace(input, pattern, replace);
	}
}
