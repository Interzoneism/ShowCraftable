using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Util;

public static class EnumerableExtensions
{
	public static int IndexOf<T>(this IEnumerable<T> source, ActionBoolReturn<T> onelem)
	{
		int num = 0;
		foreach (T item in source)
		{
			if (onelem(item))
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	public static void Foreach<T>(this IEnumerable<T> array, Action<T> onelement)
	{
		foreach (T item in array)
		{
			onelement(item);
		}
	}

	public static T Nearest<T>(this IEnumerable<T> array, System.Func<T, double> getDistance)
	{
		double num = double.MaxValue;
		T result = default(T);
		foreach (T item in array)
		{
			double num2 = getDistance(item);
			if (num2 < num)
			{
				num = num2;
				result = item;
			}
		}
		return result;
	}

	public static double NearestDistance<T>(this IEnumerable<T> array, System.Func<T, double> getDistance)
	{
		double num = double.MaxValue;
		foreach (T item in array)
		{
			double num2 = getDistance(item);
			if (num2 < num)
			{
				num = num2;
			}
		}
		return num;
	}
}
