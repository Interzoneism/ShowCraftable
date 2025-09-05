using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Vintagestory.API.Datastructures;

public class FastMemoryStream : Stream
{
	private byte[] buffer;

	private int bufferlength;

	private const int MaxLength = 2147483616;

	public override long Position { get; set; }

	public override long Length => bufferlength;

	public override bool CanSeek => false;

	public override bool CanRead => true;

	public override bool CanWrite => true;

	public FastMemoryStream()
	{
		bufferlength = 1024;
		buffer = new byte[1024];
		Position = 0L;
	}

	public FastMemoryStream(int capacity)
	{
		bufferlength = capacity;
		buffer = new byte[capacity];
		Position = 0L;
	}

	public FastMemoryStream(byte[] buffer, int length)
	{
		bufferlength = length;
		this.buffer = buffer;
		Position = 0L;
	}

	public override void SetLength(long value)
	{
		if (value > 2147483616)
		{
			throw new IndexOutOfRangeException("FastMemoryStream limited to 2GB in size");
		}
		bufferlength = Math.Min((int)value, buffer.Length);
	}

	public byte[] ToArray()
	{
		return FastCopy(buffer, bufferlength, (int)Position);
	}

	public byte[] GetBuffer()
	{
		return buffer;
	}

	public override int Read(byte[] destBuffer, int offset, int count)
	{
		long position = Position;
		long num = bufferlength;
		byte[] array = buffer;
		for (int i = 0; i < count; i++)
		{
			if (position + i >= num)
			{
				Position += i;
				return i;
			}
			destBuffer[offset + i] = array[position + i];
		}
		Position += count;
		return count;
	}

	public override void Write(byte[] srcBuffer, int srcOffset, int count)
	{
		if (count <= 0)
		{
			return;
		}
		CheckCapacity(count);
		if (count < 128)
		{
			byte[] array = buffer;
			uint num = (uint)Position;
			uint num2 = (uint)srcOffset;
			uint num3 = (uint)(srcOffset + count);
			while (num2 < num3)
			{
				array[num++] = srcBuffer[num2++];
			}
		}
		else
		{
			Array.Copy(srcBuffer, srcOffset, buffer, (int)Position, count);
		}
		Position += count;
	}

	public void Write(FastMemoryStream src)
	{
		Write(src.buffer, 0, (int)src.Position);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CheckCapacity(int count)
	{
		if (Position + count <= bufferlength)
		{
			return;
		}
		int num = ((bufferlength <= 1073741808) ? (bufferlength * 2) : (Math.Min(bufferlength, 1879048164) + 268435452));
		if (Position + count > num)
		{
			if (Position + count > 2147483616)
			{
				throw new IndexOutOfRangeException("FastMemoryStream limited to 2GB in size");
			}
			num = (int)Position + count;
		}
		buffer = FastCopy(buffer, (int)Position, num);
		bufferlength = num;
	}

	public override int ReadByte()
	{
		if (Position >= bufferlength)
		{
			return -1;
		}
		return buffer[Position++];
	}

	public override void WriteByte(byte p)
	{
		CheckCapacity(1);
		buffer[Position++] = p;
	}

	public void WriteTwoBytes(int v)
	{
		CheckCapacity(2);
		buffer[Position] = (byte)(v | 0x80);
		buffer[Position + 1] = (byte)(v >> 7);
		Position += 2L;
	}

	public void WriteThreeBytes(int v)
	{
		CheckCapacity(3);
		buffer[Position] = (byte)(v | 0x80);
		buffer[Position + 1] = (byte)((v >> 7) | 0x80);
		buffer[Position + 2] = (byte)(v >> 14);
		Position += 3L;
	}

	public void WriteUTF8String(string s, int lengthInBytes)
	{
		CheckCapacity(lengthInBytes);
		Position += Encoding.UTF8.GetBytes(s, 0, s.Length, buffer, (int)Position);
	}

	public void WriteAt(int pos, int v, int size)
	{
		if (v < 0 && size != 5)
		{
			throw new Exception("cannot retroactively write a negative number");
		}
		switch (size)
		{
		case 1:
			if (v >= 128)
			{
				throw new Exception("unsupported increase in count while serializing: " + v + " was " + buffer[pos]);
			}
			buffer[pos] = (byte)v;
			break;
		case 2:
			if (v >= 16384)
			{
				throw new Exception("unsupported increase in count while serializing: " + v + " was " + buffer[pos] + " " + buffer[pos + 1]);
			}
			buffer[pos] = (byte)(v | 0x80);
			buffer[pos + 1] = (byte)(v >> 7);
			break;
		case 3:
			if (v >= 2097152)
			{
				throw new Exception("unsupported increase in count while serializing: " + v + " was " + buffer[pos] + " " + buffer[pos + 1] + " " + buffer[pos + 2]);
			}
			buffer[pos] = (byte)(v | 0x80);
			buffer[pos + 1] = (byte)((v >> 7) | 0x80);
			buffer[pos + 2] = (byte)(v >> 14);
			break;
		case 4:
			if (v >= 268435456)
			{
				throw new Exception("unsupported increase in count while serializing: " + v + " was " + buffer[pos] + " " + buffer[pos + 1] + " " + buffer[pos + 2] + " " + buffer[pos + 3]);
			}
			buffer[pos] = (byte)(v | 0x80);
			buffer[pos + 1] = (byte)((v >> 7) | 0x80);
			buffer[pos + 2] = (byte)((v >> 14) | 0x80);
			buffer[pos + 3] = (byte)(v >> 21);
			break;
		case 5:
			buffer[pos] = (byte)(v | 0x80);
			buffer[pos + 1] = (byte)((v >> 7) | 0x80);
			buffer[pos + 2] = (byte)((v >> 14) | 0x80);
			buffer[pos + 3] = (byte)((v >> 21) | 0x80);
			buffer[pos + 4] = (byte)(v >> 28);
			break;
		}
	}

	public void WriteInt32(int v)
	{
		CheckCapacity(4);
		buffer[Position] = (byte)v;
		buffer[Position + 1] = (byte)(v >> 8);
		buffer[Position + 2] = (byte)(v >> 16);
		buffer[Position + 3] = (byte)(v >> 24);
		Position += 4L;
	}

	public void Write(float v)
	{
		CheckCapacity(4);
		BinaryPrimitives.WriteSingleLittleEndian(new Span<byte>(buffer, (int)Position, 4), v);
		Position += 4L;
	}

	public override void Write(ReadOnlySpan<byte> inputBuffer)
	{
		int length = inputBuffer.Length;
		CheckCapacity(length);
		Span<byte> destination = new Span<byte>(buffer, (int)Position, length);
		inputBuffer.CopyTo(destination);
		Position += length;
	}

	private static byte[] FastCopy(byte[] buffer, int oldLength, int newSize)
	{
		if (newSize < oldLength)
		{
			oldLength = newSize;
		}
		byte[] array = new byte[newSize];
		if (oldLength >= 128)
		{
			Array.Copy(buffer, 0, array, 0, oldLength);
		}
		else
		{
			uint num = 0u;
			if (oldLength > 15)
			{
				for (uint num2 = (uint)(oldLength - 3); num < buffer.Length && num < num2; num += 4)
				{
					array[num] = buffer[num];
					array[num + 1] = buffer[num + 1];
					array[num + 2] = buffer[num + 2];
					array[num + 3] = buffer[num + 3];
				}
			}
			for (; num < oldLength; num++)
			{
				array[num] = buffer[num];
			}
		}
		return array;
	}

	public override void Flush()
	{
	}

	public void Reset()
	{
		Position = 0L;
		bufferlength = buffer.Length;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		return -1L;
	}

	public void RemoveFromStart(int newStart)
	{
		int num = (int)Position;
		int num2 = num - newStart;
		if (num2 >= 128)
		{
			Array.Copy(buffer, newStart, buffer, 0, num2);
		}
		else
		{
			byte[] array = buffer;
			uint num3 = (uint)newStart;
			uint num4 = 0u;
			if (num2 > 15)
			{
				for (uint num5 = (uint)(num - 3); num3 < array.Length && num3 < num5; num3 += 4)
				{
					array[num4] = array[num3];
					array[num4 + 1] = array[num3 + 1];
					array[num4 + 2] = array[num3 + 2];
					array[num4 + 3] = array[num3 + 3];
					num4 += 4;
				}
			}
			while (num3 < num)
			{
				array[num4++] = array[num3++];
			}
		}
		Position -= newStart;
	}
}
