using System;
using System.IO;
using System.IO.Compression;

namespace Vintagestory.Common;

public class CompressionGzip : ICompression
{
	private const int SIZE = 4096;

	[ThreadStatic]
	private static byte[] buffer;

	public byte[] Compress(MemoryStream input)
	{
		if (buffer == null)
		{
			buffer = new byte[4096];
		}
		MemoryStream memoryStream = new MemoryStream();
		input.Position = 0L;
		using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
		{
			int count;
			while ((count = input.Read(buffer, 0, 4096)) != 0)
			{
				gZipStream.Write(buffer, 0, count);
			}
		}
		return memoryStream.ToArray();
	}

	public byte[] Compress(byte[] data)
	{
		int num = data.Length;
		MemoryStream memoryStream = new MemoryStream();
		using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
		{
			int i = 0;
			for (int num2 = num - 4096; i < num2; i += 4096)
			{
				gZipStream.Write(data, i, 4096);
			}
			gZipStream.Write(data, i, num - i);
		}
		return memoryStream.ToArray();
	}

	public byte[] Compress(byte[] data, int len, int reserveOffset)
	{
		MemoryStream memoryStream = new MemoryStream((len / 2048 + 1) * 256);
		for (int i = 0; i < reserveOffset; i++)
		{
			memoryStream.WriteByte(0);
		}
		using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
		{
			int j = 0;
			for (int num = len - 4096; j < num; j += 4096)
			{
				gZipStream.Write(data, j, 4096);
			}
			gZipStream.Write(data, j, len - j);
		}
		return memoryStream.ToArray();
	}

	public unsafe byte[] Compress(ushort[] ushortdata)
	{
		if (buffer == null)
		{
			buffer = new byte[4096];
		}
		MemoryStream memoryStream = new MemoryStream();
		fixed (byte* ptr = buffer)
		{
			ushort* ptr2 = (ushort*)ptr;
			using GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress);
			int num = 0;
			int num2 = 0;
			while (num < ushortdata.Length)
			{
				ptr2[num2++] = ushortdata[num++];
				if (num2 == 2048 || num == ushortdata.Length)
				{
					gZipStream.Write(buffer, 0, num2 * 2);
					num2 = 0;
				}
			}
		}
		return memoryStream.ToArray();
	}

	public byte[] Compress(int[] data, int length)
	{
		throw new NotImplementedException();
	}

	public byte[] Compress(uint[] data, int length)
	{
		throw new NotImplementedException();
	}

	public byte[] Decompress(byte[] fi)
	{
		if (buffer == null)
		{
			buffer = new byte[4096];
		}
		MemoryStream memoryStream = new MemoryStream();
		using (MemoryStream stream = new MemoryStream(fi))
		{
			using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress);
			int count;
			while ((count = gZipStream.Read(buffer, 0, 4096)) != 0)
			{
				memoryStream.Write(buffer, 0, count);
			}
		}
		return memoryStream.ToArray();
	}

	public void Decompress(byte[] fi, byte[] dest)
	{
		using MemoryStream stream = new MemoryStream(fi);
		using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress);
		int num = 0;
		int num2 = dest.Length - 4096;
		int num3;
		while ((num3 = gZipStream.Read(dest, num, 4096)) != 0)
		{
			if ((num += num3) > num2)
			{
				if (num < dest.Length)
				{
					gZipStream.Read(dest, num, dest.Length - num);
				}
				break;
			}
		}
	}

	public byte[] Decompress(byte[] fi, int offset, int length)
	{
		throw new NotImplementedException();
	}

	public int DecompressAndSize(byte[] fi, out byte[] buffer)
	{
		throw new NotImplementedException();
	}

	public int DecompressAndSize(byte[] compressedData, int offset, int length, out byte[] buffer)
	{
		throw new NotImplementedException();
	}

	public byte[] DecompressFromOffset(byte[] compressedData, int offset)
	{
		throw new NotImplementedException();
	}
}
