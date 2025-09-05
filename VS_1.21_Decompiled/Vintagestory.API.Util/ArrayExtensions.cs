using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Util;

public static class ArrayExtensions
{
	public static T Nearest<T>(this T[] array, Func<T, double> getDistance)
	{
		double num = double.MaxValue;
		T result = default(T);
		for (int i = 0; i < array.Length; i++)
		{
			double num2 = getDistance(array[i]);
			if (num2 < num)
			{
				num = num2;
				result = array[i];
			}
		}
		return result;
	}

	public static List<T> InRange<T>(this T[] array, Func<T, double> getDistance, double range)
	{
		List<T> list = new List<T>();
		for (int i = 0; i < array.Length; i++)
		{
			if (getDistance(array[i]) < range)
			{
				list.Add(array[i]);
			}
		}
		return list;
	}

	public static bool Contains<T>(this T[] array, Func<T, bool> predicate)
	{
		return array.IndexOf(predicate) >= 0;
	}

	public static int IndexOf<T>(this T[] array, Func<T, bool> predicate)
	{
		for (int i = 0; i < array.Length; i++)
		{
			if (predicate(array[i]))
			{
				return i;
			}
		}
		return -1;
	}

	public static int IndexOf<T>(this T[] array, T value)
	{
		for (int i = 0; i < array.Length; i++)
		{
			if (object.Equals(value, array[i]))
			{
				return i;
			}
		}
		return -1;
	}

	public static bool Contains<T>(this T[] array, T value)
	{
		for (int i = 0; i < array.Length; i++)
		{
			if (object.Equals(array[i], value))
			{
				return true;
			}
		}
		return false;
	}

	public static T[] Remove<T>(this T[] array, T value)
	{
		List<T> list = new List<T>(array);
		list.Remove(value);
		return list.ToArray();
	}

	[Obsolete("Use RemoveAt instead")]
	public static T[] RemoveEntry<T>(this T[] array, int index)
	{
		return array.RemoveAt(index);
	}

	public static T[] RemoveAt<T>(this T[] array, int index)
	{
		T[] array2 = new T[array.Length - 1];
		if (index == 0)
		{
			if (array2.Length == 0)
			{
				return array2;
			}
			Array.Copy(array, 1, array2, 0, array.Length - 1);
		}
		else if (index == array.Length - 1)
		{
			Array.Copy(array, array2, array.Length - 1);
		}
		else
		{
			Array.Copy(array, 0, array2, 0, index);
			Array.Copy(array, index + 1, array2, index, array.Length - index - 1);
		}
		return array2;
	}

	public static T[] Append<T>(this T[] array, T value)
	{
		T[] array2 = new T[array.Length + 1];
		Array.Copy(array, array2, array.Length);
		array2[array.Length] = value;
		return array2;
	}

	public static T[] InsertAt<T>(this T[] array, T value, int index)
	{
		T[] array2 = new T[array.Length + 1];
		if (index > 0)
		{
			Array.Copy(array, array2, index);
		}
		array2[index] = value;
		Array.Copy(array, index, array2, index + 1, array.Length - index);
		return array2;
	}

	public static T[] Append<T>(this T[] array, params T[] value)
	{
		if (array == null)
		{
			return null;
		}
		if (value == null || value.Length == 0)
		{
			return array;
		}
		T[] array2 = new T[array.Length + value.Length];
		Array.Copy(array, array2, array.Length);
		for (int i = 0; i < value.Length; i++)
		{
			array2[array.Length + i] = value[i];
		}
		return array2;
	}

	public static T[] Append<T>(this T[] array, IEnumerable<T> values)
	{
		if (array == null)
		{
			return null;
		}
		if (values == null)
		{
			return array;
		}
		T[] array2 = new T[array.Length + values.Count()];
		Array.Copy(array, array2, array.Length);
		int num = 0;
		foreach (T value in values)
		{
			array2[array.Length + num] = value;
		}
		return array2;
	}

	public static T[] Fill<T>(this T[] originalArray, T with)
	{
		for (int i = 0; i < originalArray.Length; i++)
		{
			originalArray[i] = with;
		}
		return originalArray;
	}

	public static T[] Fill<T>(this T[] originalArray, fillCallback<T> fillCallback)
	{
		for (int i = 0; i < originalArray.Length; i++)
		{
			originalArray[i] = fillCallback(i);
		}
		return originalArray;
	}

	public static T[] Shuffle<T>(this T[] array, Random rand)
	{
		int num = array.Length;
		while (num > 1)
		{
			int num2 = rand.Next(num);
			num--;
			T val = array[num];
			array[num] = array[num2];
			array[num2] = val;
		}
		return array;
	}

	public static T[] Shuffle<T>(this T[] array, LCGRandom rand)
	{
		int num = array.Length;
		while (num > 1)
		{
			int num2 = rand.NextInt(num);
			num--;
			T val = array[num];
			array[num] = array[num2];
			array[num2] = val;
		}
		return array;
	}
}
