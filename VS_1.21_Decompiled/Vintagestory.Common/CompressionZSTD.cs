using System;
using Vintagestory.Common.Convert;

namespace Vintagestory.Common;

public class CompressionZSTD : ICompression
{
	public const int ZSTDCompressionLevel = -3;

	private const int MaxUnusedLargeBufferCount = 100;

	private const int LargeBufferResetInterval = 960000;

	private const int LargeBufferSize = 524288;

	private const int LargeBufferHeadroom = 32768;

	[ThreadStatic]
	private static IZStdCompressor reusableCompressor;

	[ThreadStatic]
	private static IZStdDecompressor reusableDecompressor;

	[ThreadStatic]
	private static byte[] reusableBuffer;

	[ThreadStatic]
	internal static byte[] reusableBuffer2;

	[ThreadStatic]
	private static int largebufferUnusedCounter1;

	[ThreadStatic]
	private static int largebufferUnusedCounter2;

	[ThreadStatic]
	private static int largebufferMaxUsed1;

	[ThreadStatic]
	private static int largebufferMaxUsed2;

	[ThreadStatic]
	private static long largebufferLastReductionTime1;

	[ThreadStatic]
	private static long largebufferLastReductionTime2;

	public byte[] Buffer => reusableBuffer ?? (reusableBuffer = new byte[4096]);

	private IZStdCompressor ConstructCompressor()
	{
		return ZStdWrapper.ConstructCompressor(-3);
	}

	private byte[] GetOrCreateBuffer(int size)
	{
		byte[] array = reusableBuffer;
		if (array == null || array.Length < size)
		{
			array = (reusableBuffer = new byte[size]);
			largebufferUnusedCounter1 = 0;
			largebufferMaxUsed1 = 0;
		}
		else if (array.Length > 524288)
		{
			if (size > largebufferMaxUsed1)
			{
				largebufferMaxUsed1 = size;
			}
			if (size > array.Length * 3 / 4)
			{
				largebufferUnusedCounter1 = 0;
			}
			else if (largebufferUnusedCounter1++ >= 100)
			{
				largebufferUnusedCounter1 = 0;
				if (Environment.TickCount64 > largebufferLastReductionTime1 + 960000)
				{
					largebufferLastReductionTime1 = Environment.TickCount64;
					array = (reusableBuffer = new byte[Math.Max(largebufferMaxUsed1 + 32768, 524288)]);
					largebufferMaxUsed1 = 0;
				}
			}
		}
		return array;
	}

	private byte[] GetOrCreateBuffer2(int size)
	{
		byte[] array = reusableBuffer2;
		if (array == null || array.Length < size)
		{
			array = (reusableBuffer2 = new byte[size]);
			largebufferUnusedCounter2 = 0;
			largebufferMaxUsed2 = 0;
		}
		else if (array.Length > 524288)
		{
			if (size > largebufferMaxUsed2)
			{
				largebufferMaxUsed2 = size;
			}
			if (size > array.Length * 3 / 4)
			{
				largebufferUnusedCounter2 = 0;
			}
			else if (largebufferUnusedCounter2++ >= 100)
			{
				largebufferUnusedCounter2 = 0;
				if (Environment.TickCount64 > largebufferLastReductionTime2 + 960000)
				{
					largebufferLastReductionTime2 = Environment.TickCount64;
					array = (reusableBuffer2 = new byte[Math.Max(largebufferMaxUsed2 + 32768, 524288)]);
					largebufferMaxUsed2 = 0;
				}
			}
		}
		return array;
	}

	public byte[] Compress(byte[] data)
	{
		int size = (int)ZstdNative.ZSTD_compressBound((nuint)data.Length);
		byte[] orCreateBuffer = GetOrCreateBuffer(size);
		int length = (reusableCompressor ?? (reusableCompressor = ConstructCompressor())).Compress(orCreateBuffer, data);
		return ArrayConvert.ByteToByte(orCreateBuffer, length);
	}

	public byte[] Compress(byte[] data, int length, int reserveOffset)
	{
		int size = (int)ZstdNative.ZSTD_compressBound((nuint)length);
		byte[] orCreateBuffer = GetOrCreateBuffer(size);
		ReadOnlySpan<byte> src = new ReadOnlySpan<byte>(data, 0, length);
		int length2 = (reusableCompressor ?? (reusableCompressor = ConstructCompressor())).Compress(orCreateBuffer, src);
		return ArrayConvert.ByteToByte(orCreateBuffer, length2, reserveOffset);
	}

	public unsafe byte[] Compress(ushort[] ushortdata)
	{
		int num = ushortdata.Length * 2;
		int size = (int)ZstdNative.ZSTD_compressBound((nuint)num);
		byte[] orCreateBuffer = GetOrCreateBuffer(size);
		IZStdCompressor obj = reusableCompressor ?? (reusableCompressor = ConstructCompressor());
		int length;
		fixed (ushort* pointer = ushortdata)
		{
			ReadOnlySpan<byte> src = new ReadOnlySpan<byte>(pointer, num);
			length = obj.Compress(orCreateBuffer, src);
		}
		return ArrayConvert.ByteToByte(orCreateBuffer, length);
	}

	public unsafe byte[] Compress(int[] intdata, int length)
	{
		int num = length * 4;
		int size = (int)ZstdNative.ZSTD_compressBound((nuint)num);
		byte[] orCreateBuffer = GetOrCreateBuffer(size);
		IZStdCompressor obj = reusableCompressor ?? (reusableCompressor = ConstructCompressor());
		int length2;
		fixed (int* pointer = intdata)
		{
			ReadOnlySpan<byte> src = new ReadOnlySpan<byte>(pointer, num);
			length2 = obj.Compress(orCreateBuffer, src);
		}
		return ArrayConvert.ByteToByte(orCreateBuffer, length2);
	}

	public unsafe byte[] Compress(uint[] uintdata, int length)
	{
		int num = length * 4;
		int size = (int)ZstdNative.ZSTD_compressBound((nuint)num);
		byte[] orCreateBuffer = GetOrCreateBuffer(size);
		IZStdCompressor obj = reusableCompressor ?? (reusableCompressor = ConstructCompressor());
		int length2;
		fixed (uint* pointer = uintdata)
		{
			ReadOnlySpan<byte> src = new ReadOnlySpan<byte>(pointer, num);
			length2 = obj.Compress(orCreateBuffer, src);
		}
		return ArrayConvert.ByteToByte(orCreateBuffer, length2);
	}

	internal unsafe int CompressAndSize(int[] intdata, int length)
	{
		int num = length * 4;
		int size = (int)ZstdNative.ZSTD_compressBound((nuint)num);
		byte[] orCreateBuffer = GetOrCreateBuffer(size);
		IZStdCompressor obj = reusableCompressor ?? (reusableCompressor = ConstructCompressor());
		int result;
		fixed (int* pointer = intdata)
		{
			ReadOnlySpan<byte> src = new ReadOnlySpan<byte>(pointer, num);
			result = obj.Compress(orCreateBuffer, src);
		}
		return result;
	}

	internal unsafe int Compress_To2ndBuffer(int[] intdata, int intLength)
	{
		int num = intLength * 4;
		int size = (int)ZstdNative.ZSTD_compressBound((nuint)num);
		byte[] orCreateBuffer = GetOrCreateBuffer2(size);
		IZStdCompressor obj = reusableCompressor ?? (reusableCompressor = ConstructCompressor());
		int result;
		fixed (int* pointer = intdata)
		{
			ReadOnlySpan<byte> src = new ReadOnlySpan<byte>(pointer, num);
			result = obj.Compress(orCreateBuffer, src);
		}
		return result;
	}

	public void Decompress(byte[] fi, byte[] dest)
	{
		(reusableDecompressor ?? (reusableDecompressor = ZStdWrapper.CreateDecompressor())).Decompress(dest, fi);
	}

	public byte[] Decompress(byte[] fi)
	{
		byte[] buffer;
		int num = DecompressAndSize(fi, out buffer);
		if (num < 0)
		{
			return Array.Empty<byte>();
		}
		return ArrayConvert.ByteToByte(buffer, num);
	}

	public byte[] Decompress(byte[] fi, int offset, int length)
	{
		byte[] buffer;
		int num = DecompressAndSize(fi, offset, length, out buffer);
		if (num < 0)
		{
			return Array.Empty<byte>();
		}
		return ArrayConvert.ByteToByte(buffer, num);
	}

	public int DecompressAndSize(byte[] compressedData, out byte[] buffer)
	{
		ReadOnlySpan<byte> compressedFrame = new ReadOnlySpan<byte>(compressedData);
		int size = (int)ZStdWrapper.GetDecompressedSize(compressedFrame);
		buffer = GetOrCreateBuffer(size);
		return (reusableDecompressor ?? (reusableDecompressor = ZStdWrapper.CreateDecompressor())).Decompress(buffer, compressedFrame);
	}

	public int DecompressAndSize(byte[] compressedData, int offset, int length, out byte[] buffer)
	{
		ReadOnlySpan<byte> compressedFrame = new ReadOnlySpan<byte>(compressedData, offset, length);
		int size = (int)ZStdWrapper.GetDecompressedSize(compressedFrame);
		buffer = GetOrCreateBuffer(size);
		return (reusableDecompressor ?? (reusableDecompressor = ZStdWrapper.CreateDecompressor())).Decompress(buffer, compressedFrame);
	}
}
