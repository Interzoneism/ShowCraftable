using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public static class ArrayConvert
{
	public static byte[] UshortToByte(ushort[] shorts)
	{
		byte[] array = new byte[shorts.Length * 2];
		UshortToByte(shorts, array);
		return array;
	}

	public unsafe static void UshortToByte(ushort[] shorts, byte[] output)
	{
		fixed (byte* ptr = output)
		{
			ushort* ptr2 = (ushort*)ptr;
			for (int i = 0; i < shorts.Length; i++)
			{
				ptr2[i] = shorts[i];
			}
		}
	}

	internal static ushort[] ByteToUshort(byte[] data)
	{
		ushort[] array = new ushort[data.Length / 2];
		ByteToUshort(data, array);
		return array;
	}

	internal static ushort[] ByteToUshort(byte[] data, int length)
	{
		ushort[] array = new ushort[length / 2];
		ByteToUshort(data, array);
		return array;
	}

	internal unsafe static void ByteToUshort(byte[] data, ushort[] output)
	{
		fixed (byte* ptr = data)
		{
			ushort* ptr2 = (ushort*)ptr;
			int num = output.Length / 2;
			num *= 2;
			for (int i = 0; i < num; i += 2)
			{
				output[i] = ptr2[i];
				output[i + 1] = ptr2[i + 1];
			}
			if (num < output.Length)
			{
				output[num] = ptr2[num];
			}
		}
	}

	internal static byte[] ByteToByte(byte[] data, int length)
	{
		byte[] array = new byte[length];
		if (length == 0)
		{
			return array;
		}
		int num = length / 4 * 4;
		int i;
		for (i = 0; i < num; i += 4)
		{
			array[i] = data[i];
			array[i + 1] = data[i + 1];
			array[i + 2] = data[i + 2];
			array[i + 3] = data[i + 3];
		}
		while (i < length)
		{
			array[i] = data[i++];
		}
		return array;
	}

	internal static byte[] ByteToByte(byte[] data, int length, int reserveOffset)
	{
		byte[] array = new byte[length + reserveOffset];
		if (length == 0)
		{
			return array;
		}
		int num = reserveOffset;
		int num2 = length / 4 * 4;
		int i;
		for (i = 0; i < num2; i += 4)
		{
			array[num] = data[i];
			array[num + 1] = data[i + 1];
			array[num + 2] = data[i + 2];
			array[num + 3] = data[i + 3];
			num += 4;
		}
		while (i < length)
		{
			array[num++] = data[i++];
		}
		return array;
	}

	internal static byte[] Build(int lengthA, byte[] dataA, byte[] data, int length)
	{
		byte[] array = new byte[length + lengthA + 4];
		if (length + lengthA == 0)
		{
			return array;
		}
		array[0] = (byte)lengthA;
		array[1] = (byte)(lengthA >> 8);
		array[2] = (byte)(lengthA >> 16);
		array[3] = (byte)(lengthA >> 24);
		int num = 4;
		int num2 = lengthA / 4 * 4;
		int i;
		for (i = 0; i < num2; i += 4)
		{
			array[num] = dataA[i];
			array[num + 1] = dataA[i + 1];
			array[num + 2] = dataA[i + 2];
			array[num + 3] = dataA[i + 3];
			num += 4;
		}
		while (i < lengthA)
		{
			array[num++] = dataA[i++];
		}
		num2 = length / 4 * 4;
		for (i = 0; i < num2; i += 4)
		{
			array[num] = data[i];
			array[num + 1] = data[i + 1];
			array[num + 2] = data[i + 2];
			array[num + 3] = data[i + 3];
			num += 4;
		}
		while (i < length)
		{
			array[num++] = data[i++];
		}
		return array;
	}

	internal unsafe static byte[] Build(int lengthA, int[] intData, byte[] data, int length)
	{
		byte[] array = new byte[length + lengthA * 4 + 4];
		if (length + lengthA == 0)
		{
			return array;
		}
		int num = -4 * lengthA;
		array[0] = (byte)num;
		array[1] = (byte)(num >> 8);
		array[2] = (byte)(num >> 16);
		array[3] = (byte)(num >> 24);
		int i = 0;
		fixed (byte* ptr = array)
		{
			int* ptr2 = (int*)ptr + 1;
			for (; i < lengthA; i++)
			{
				ptr2[i] = intData[i];
			}
		}
		i = (i + 1) * 4;
		int num2 = length / 4 * 4;
		int j;
		for (j = 0; j < num2; j += 4)
		{
			array[i] = data[j];
			array[i + 1] = data[j + 1];
			array[i + 2] = data[j + 2];
			array[i + 3] = data[j + 3];
			i += 4;
		}
		while (j < length)
		{
			array[i++] = data[j++];
		}
		return array;
	}

	internal static int GetInt(byte[] output)
	{
		int num = output[0] & 0xFF;
		int num2 = output[1] & 0xFF;
		int num3 = output[2] & 0xFF;
		return ((((output[3] & 0xFF) << 8) + num3 << 8) + num2 << 8) + num;
	}

	public static byte[] IntToByte(int[] ints)
	{
		byte[] array = new byte[ints.Length * 4];
		IntToByte(ints, array);
		return array;
	}

	public unsafe static void IntToByte(int[] ints, byte[] output)
	{
		fixed (byte* ptr = output)
		{
			int* ptr2 = (int*)ptr;
			for (int i = 0; i < ints.Length; i++)
			{
				ptr2[i] = ints[i];
			}
		}
	}

	public static int[] ByteToInt(byte[] data)
	{
		int[] array = new int[data.Length / 4];
		ByteToInt(data, array);
		return array;
	}

	public unsafe static void ByteToInt(byte[] data, int[] output)
	{
		fixed (byte* ptr = data)
		{
			int* ptr2 = (int*)ptr;
			for (int i = 0; i < output.Length; i++)
			{
				output[i] = ptr2[i];
			}
		}
	}

	public unsafe static void ByteToInt(byte[] data, int[] output, int length)
	{
		fixed (byte* ptr = data)
		{
			int* ptr2 = (int*)ptr;
			for (int i = 0; i < length; i++)
			{
				output[i] = ptr2[i];
			}
		}
	}

	public unsafe static void ByteToInt(byte[] data, int offset, int[] output, int length)
	{
		fixed (byte* ptr = data)
		{
			int* ptr2 = (int*)ptr;
			ptr2 += offset / 4;
			for (int i = 0; i < length; i++)
			{
				output[i] = ptr2[i];
			}
		}
	}

	public unsafe static void ByteToIntArrays(byte[] data, int[][] output, int count, Func<int[]> newArray)
	{
		for (int i = 0; i < count; i++)
		{
			if (output[i] == null)
			{
				output[i] = newArray();
			}
		}
		fixed (byte* ptr = data)
		{
			int* ptr2 = (int*)ptr;
			for (int j = 0; j < count; j++)
			{
				int[] array = output[j];
				for (int k = 0; k < array.Length; k += 4)
				{
					array[k] = *ptr2;
					array[k + 1] = ptr2[1];
					array[k + 2] = ptr2[2];
					array[k + 3] = ptr2[3];
					ptr2 += 4;
				}
			}
		}
	}

	public unsafe static void ByteToUint(byte[] data, uint[] output, int length)
	{
		fixed (byte* ptr = data)
		{
			uint* ptr2 = (uint*)ptr;
			for (int i = 0; i < length; i++)
			{
				output[i] = ptr2[i];
			}
		}
	}

	public static T[] Copy<T>(this IEnumerable<T> data, long index, long length)
	{
		T[] array = new T[length];
		Array.Copy(data.ToArray(), index, array, 0L, length);
		return array;
	}

	internal static void IntToInt(int[] src, int[] dest, int offset)
	{
		Array.Copy(src, 0, dest, offset, src.Length);
	}

	public static Vec3f[] ToVec3fs(this byte[] bytes)
	{
		return bytes.ToFloats().ToVec3fs();
	}

	public static Vec2f[] ToVec2fs(this byte[] bytes)
	{
		return bytes.ToFloats().ToVec2fs();
	}

	public static Vec4s[] ToVec4Ss(this byte[] bytes)
	{
		return bytes.ToShorts().ToVec4ss();
	}

	public static Vec4us[] ToVec4uss(this byte[] bytes)
	{
		return bytes.ToUShorts().ToVec4uss();
	}

	public static int[] ToInts(this ushort[] shorts)
	{
		int[] array = new int[shorts.Length];
		for (int i = 0; i < shorts.Length; i++)
		{
			array[i] = shorts[i];
		}
		return array;
	}

	public static Vec4s[] ToVec4ss(this IEnumerable<short> shorts1)
	{
		short[] array = shorts1.ToArray();
		Vec4s[] array2 = new Vec4s[array.Length / 4];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = new Vec4s(array[i * 4], array[i * 4 + 1], array[i * 4 + 2], array[i * 4 + 3]);
		}
		return array2;
	}

	public static Vec4us[] ToVec4uss(this IEnumerable<ushort> shorts1)
	{
		ushort[] array = shorts1.ToArray();
		Vec4us[] array2 = new Vec4us[array.Length / 4];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = new Vec4us(array[i * 4], array[i * 4 + 1], array[i * 4 + 2], array[i * 4 + 3]);
		}
		return array2;
	}

	public static Vec3f[] ToVec3fs(this IEnumerable<float> floats1)
	{
		float[] array = floats1.ToArray();
		Vec3f[] array2 = new Vec3f[array.Length / 3];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = new Vec3f(array[i * 3], array[i * 3 + 1], array[i * 3 + 2]);
		}
		return array2;
	}

	public static Vec2f[] ToVec2fs(this IEnumerable<float> floats1)
	{
		float[] array = floats1.ToArray();
		Vec2f[] array2 = new Vec2f[array.Length / 2];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = new Vec2f(array[i * 2], array[i * 2 + 1]);
		}
		return array2;
	}

	public static int[] ToInts(this IEnumerable<byte> bytes)
	{
		int[] array = new int[bytes.Count() / 4];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = BitConverter.ToInt32(bytes.Copy(i * 4, 4L), 0);
		}
		return array;
	}

	public static ulong[] BytesToULongs(this IEnumerable<byte> bytes)
	{
		return bytes.ToUShorts().FourShortsToULong();
	}

	public static ulong[] FourShortsToULong(this IEnumerable<ushort> shorts1)
	{
		long[] array = shorts1.FourShortsToLong();
		ulong[] array2 = new ulong[array.Count()];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = (ulong)array[i];
		}
		return array2;
	}

	public static long[] FourShortsToLong(this IEnumerable<ushort> shorts1)
	{
		ushort[] array = shorts1.ToArray();
		long[] array2 = new long[array.Count() / 4];
		for (int i = 0; i < array2.Length; i++)
		{
			long num = (array[i * 4] << 16) | array[i * 4 + 1] | (array[i * 4 + 2] << 16) | array[i * 4 + 3];
			array2[i] = num;
		}
		return array2;
	}

	public static float[] ToFloats(this IEnumerable<byte> bytes)
	{
		float[] array = new float[bytes.Count() / 4];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = BitConverter.ToSingle(bytes.Copy(i * 4, 4L), 0);
		}
		return array;
	}

	public static ushort[] ToUShorts(this IEnumerable<byte> bytes)
	{
		ushort[] array = new ushort[bytes.Count() / 2];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = BitConverter.ToUInt16(bytes.Copy(i * 2, 2L), 0);
		}
		return array;
	}

	public static short[] ToShorts(this IEnumerable<byte> bytes)
	{
		short[] array = new short[bytes.Count() / 2];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = BitConverter.ToInt16(bytes.Copy(i * 2, 2L), 0);
		}
		return array;
	}

	public static int GetRoundedUpSize(int value)
	{
		int num = value - 1;
		int num2 = num | (num >> 1);
		int num3 = num2 | (num2 >> 2);
		int num4 = num3 | (num3 >> 4);
		int num5 = num4 | (num4 >> 8);
		return (num5 | (num5 >> 16)) + 1;
	}
}
