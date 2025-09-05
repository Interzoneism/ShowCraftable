using System;
using System.Runtime.CompilerServices;

namespace Vintagestory.API.Util;

public static class ArrayUtil
{
	public static T[] CreateFilled<T>(int quantity, fillCallback<T> fillCallback)
	{
		T[] array = new T[quantity];
		for (int i = 0; i < quantity; i++)
		{
			array[i] = fillCallback(i);
		}
		return array;
	}

	public static T[] CreateFilled<T>(int quantity, T with)
	{
		T[] array = new T[quantity];
		for (int i = 0; i < quantity; i++)
		{
			array[i] = with;
		}
		return array;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T[] FastCopy<T>(this T[] src, int count)
	{
		T[] array = new T[count];
		if (count > 127)
		{
			Array.Copy(src, 0, array, 0, count);
		}
		else
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = src[i];
			}
		}
		return array;
	}

	public static T[] Slice<T>(this T[] src, int index, int count)
	{
		T[] array = new T[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = src[index + i];
		}
		return array;
	}
}
