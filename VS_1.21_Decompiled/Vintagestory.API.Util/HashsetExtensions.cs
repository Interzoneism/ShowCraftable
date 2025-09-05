using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vintagestory.API.Util;

public static class HashsetExtensions
{
	public static void AddRange<T>(this HashSet<T> hashset, IEnumerable<T> elements)
	{
		foreach (T element in elements)
		{
			hashset.Add(element);
		}
	}

	public static void AddRange<T>(this HashSet<T> hashset, HashSet<T> elements)
	{
		foreach (T element in elements)
		{
			hashset.Add(element);
		}
	}

	public static void AddRange<T>(this HashSet<T> hashset, T[] elements)
	{
		foreach (T item in elements)
		{
			hashset.Add(item);
		}
	}

	public static string Implode<T>(this HashSet<T> hashset, string seperator = ", ")
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		foreach (T item in hashset)
		{
			if (num > 0)
			{
				stringBuilder.Append(seperator);
			}
			stringBuilder.Append(item.ToString());
			num++;
		}
		return stringBuilder.ToString();
	}

	public static T PopOne<T>(this ICollection<T> items)
	{
		T val = items.FirstOrDefault();
		if (val != null)
		{
			items.Remove(val);
		}
		return val;
	}
}
