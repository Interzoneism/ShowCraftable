using System;
using System.IO;

namespace Vintagestory.Common;

public class Compression
{
	private static ICompression Compression0 = new CompressionDeflate();

	private static ICompression Compression1 = new CompressionZSTD();

	public static ICompression compressor = Compression1;

	public static void Reset()
	{
		compressor = Compression1;
	}

	public static byte[] Compress(byte[] data)
	{
		return compressor.Compress(data);
	}

	public static byte[] CompressOffset4(byte[] data, int len)
	{
		return compressor.Compress(data, len, 4);
	}

	public static byte[] Compress(byte[] data, int version)
	{
		return ((version != 0) ? Compression1 : Compression0).Compress(data);
	}

	public static byte[] Compress(ushort[] data, int version)
	{
		return ((version != 0) ? Compression1 : Compression0).Compress(data);
	}

	public static byte[] Compress(int[] data, int length, int version)
	{
		return ((version != 0) ? Compression1 : Compression0).Compress(data, length);
	}

	public static byte[] Compress(uint[] data, int length, int version)
	{
		return ((version != 0) ? Compression1 : Compression0).Compress(data, length);
	}

	public static byte[] CompressAndCombine(int[] data, int[] intdata, int intLength)
	{
		int num = 0;
		int num2 = intdata.Length;
		while ((num2 >>= 1) > 0)
		{
			num++;
		}
		int length = num * 1024;
		if (Compression1 is CompressionZSTD compressionZSTD)
		{
			int length2 = compressionZSTD.CompressAndSize(data, length);
			if (intLength > 18)
			{
				return ArrayConvert.Build(compressionZSTD.Compress_To2ndBuffer(intdata, intLength), CompressionZSTD.reusableBuffer2, compressionZSTD.Buffer, length2);
			}
			return ArrayConvert.Build(intLength, intdata, compressionZSTD.Buffer, length2);
		}
		byte[] array = Compression1.Compress(data, length);
		if (intLength > 18)
		{
			byte[] array2 = Compression1.Compress(intdata, intLength);
			return ArrayConvert.Build(array2.Length, array2, array, array.Length);
		}
		return ArrayConvert.Build(intLength, intdata, array, array.Length);
	}

	public static byte[] Decompress(byte[] data)
	{
		return compressor.Decompress(data);
	}

	public static byte[] Decompress(byte[] data, int offset, int length)
	{
		return compressor.Decompress(data, offset, length);
	}

	public static void Decompress(byte[] data, byte[] dest, int version)
	{
		if (version != 0)
		{
			Compression1.Decompress(data, dest);
		}
		else
		{
			Compression0.Decompress(data, dest);
		}
	}

	public static void DecompressToUshort(byte[] data, ushort[] container, byte[] reusableBytes, int version)
	{
		if (version != 0)
		{
			Compression1.Decompress(data, reusableBytes);
		}
		else
		{
			Compression0.Decompress(data, reusableBytes);
		}
		ArrayConvert.ByteToUshort(reusableBytes, container);
	}

	internal static int[] DecompressCombined(byte[] blocksCompressed, ref int[][] blocks, ref int refCount, Func<int[]> newArray)
	{
		int num = ArrayConvert.GetInt(blocksCompressed);
		if (num == 0)
		{
			return null;
		}
		int num2;
		int[] array;
		if (num < 0)
		{
			num *= -1;
			num2 = num / 4;
			if (num2 == 1)
			{
				return null;
			}
			array = new int[ArrayConvert.GetRoundedUpSize(num2)];
			ArrayConvert.ByteToInt(blocksCompressed, 4, array, num2);
		}
		else
		{
			num2 = Compression1.DecompressAndSize(blocksCompressed, 4, num, out var buffer) / 4;
			if (num2 <= 1)
			{
				return null;
			}
			array = new int[ArrayConvert.GetRoundedUpSize(num2)];
			ArrayConvert.ByteToInt(buffer, array, num2);
		}
		int num3 = 0;
		int num4 = array.Length;
		while ((num4 >>= 1) > 0)
		{
			num3++;
		}
		if (blocks == null)
		{
			blocks = new int[15][];
		}
		byte[] array2 = Compression1.Decompress(blocksCompressed, num + 4, blocksCompressed.Length - (num + 4));
		if (array2.Length < num3 * 1024 * 4)
		{
			throw new InvalidDataException();
		}
		ArrayConvert.ByteToIntArrays(array2, blocks, num3, newArray);
		refCount = num2;
		return array;
	}

	internal static int[] DecompressToInts(byte[] dataCompressed, ref int intCount)
	{
		byte[] buffer;
		int num = Compression1.DecompressAndSize(dataCompressed, out buffer);
		if (num < 0)
		{
			return null;
		}
		intCount = num / 4;
		int[] array = new int[ArrayConvert.GetRoundedUpSize(intCount)];
		ArrayConvert.ByteToInt(buffer, array, intCount);
		return array;
	}

	internal static int Decompress(byte[] dataCompressed, ref int[][] output, Func<int[]> createNewSlice)
	{
		byte[] buffer;
		int num = Compression1.DecompressAndSize(dataCompressed, out buffer);
		if (num < 0)
		{
			return 0;
		}
		int num2 = num / 4096;
		if (num != 4096 * num2)
		{
			throw new InvalidDataException("size was " + num + ", should be " + 4096 * num2);
		}
		if (output == null)
		{
			output = new int[15][];
		}
		ArrayConvert.ByteToIntArrays(buffer, output, num2, createNewSlice);
		return num2;
	}
}
