using System;
using System.Text;
using Vintagestory.API.Common;

public class CitoMemoryStream : CitoStream
{
	private const int byteHighestBit = 128;

	private byte[] buffer_;

	private int bufferlength;

	private int position_;

	private readonly BoxedArray ba;

	public CitoMemoryStream()
	{
		bufferlength = 16;
		buffer_ = new byte[16];
		position_ = 0;
	}

	public CitoMemoryStream(BoxedArray reusableBuffer)
	{
		ba = reusableBuffer.CheckCreated();
		bufferlength = ba.buffer.Length;
		buffer_ = ba.buffer;
		position_ = 0;
	}

	public CitoMemoryStream(byte[] buffer, int length)
	{
		bufferlength = length;
		buffer_ = buffer;
		position_ = 0;
	}

	public override int Position()
	{
		return position_;
	}

	internal int GetLength()
	{
		return bufferlength;
	}

	internal void SetLength(int value)
	{
		bufferlength = Math.Min(value, buffer_.Length);
	}

	public byte[] ToArray()
	{
		return buffer_;
	}

	public byte[] GetBuffer()
	{
		return buffer_;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		int num = position_;
		int num2 = bufferlength - num;
		if (count > num2)
		{
			count = num2;
		}
		byte[] array = buffer_;
		for (int i = 0; i < count; i++)
		{
			buffer[offset + i] = array[num + i];
		}
		position_ = num + count;
		return count;
	}

	public override bool CanSeek()
	{
		return false;
	}

	public override void Seek(int length, CitoSeekOrigin seekOrigin)
	{
		if (seekOrigin == CitoSeekOrigin.Current)
		{
			position_ += length;
		}
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		if (count <= 0)
		{
			return;
		}
		EnsureCapacityFor(count);
		if (count > 200)
		{
			Array.Copy(buffer, offset, buffer_, position_, count);
			position_ += count;
			return;
		}
		int num = offset;
		count += offset;
		int num2 = (num + 3) / 4 * 4;
		byte[] array = buffer_;
		int num3 = position_;
		while (num < num2)
		{
			array[num3++] = buffer[num++];
		}
		int num4 = count / 4 * 4;
		for (num += 3; num < buffer.Length && num < num4; num += 4)
		{
			array[num3] = buffer[num - 3];
			array[num3 + 1] = buffer[num - 2];
			array[num3 + 2] = buffer[num - 1];
			array[num3 + 3] = buffer[num];
			num3 += 4;
		}
		num -= 3;
		while (num < count)
		{
			array[num3++] = buffer[num++];
		}
		position_ = num3;
	}

	private void EnsureCapacityFor(int count)
	{
		if (position_ + count > bufferlength)
		{
			int num = bufferlength * 2;
			if (num < position_ + count)
			{
				num = position_ + count;
			}
			buffer_ = FastCopy(buffer_, position_, num);
			bufferlength = num;
		}
	}

	public override void Seek_(int p, CitoSeekOrigin seekOrigin)
	{
	}

	public override int ReadByte()
	{
		if (position_ >= bufferlength)
		{
			return -1;
		}
		return buffer_[position_++];
	}

	public override void WriteByte(byte p)
	{
		if (position_ >= bufferlength)
		{
			buffer_ = FastCopy(buffer_, position_, bufferlength *= 2);
		}
		buffer_[position_++] = p;
	}

	public override void WriteSmallInt(int v)
	{
		if (v < 128)
		{
			WriteByte((byte)v);
			return;
		}
		if (position_ >= bufferlength - 1)
		{
			buffer_ = FastCopy(buffer_, position_, bufferlength *= 2);
		}
		buffer_[position_++] = (byte)(v | 0x80);
		buffer_[position_++] = (byte)(v >> 7);
	}

	public override void WriteKey(byte field, byte wiretype)
	{
		WriteSmallInt(new Key(field, wiretype));
	}

	private byte[] FastCopy(byte[] buffer, int oldLength, int newSize)
	{
		byte[] array = new byte[newSize];
		if (oldLength > 256)
		{
			Array.Copy(buffer, 0, array, 0, oldLength);
		}
		else
		{
			int i = 0;
			if (oldLength >= 4)
			{
				int num = oldLength / 4 * 4;
				for (i = 3; i < buffer.Length && i < num; i += 4)
				{
					array[i - 3] = buffer[i - 3];
					array[i - 2] = buffer[i - 2];
					array[i - 1] = buffer[i - 1];
					array[i] = buffer[i];
				}
				i -= 3;
			}
			for (; i < oldLength; i++)
			{
				array[i] = buffer[i];
			}
		}
		if (ba != null)
		{
			ba.buffer = array;
		}
		return array;
	}

	public override string ReadString(int byteCount)
	{
		string result = Encoding.UTF8.GetString(buffer_, position_, byteCount);
		position_ += byteCount;
		return result;
	}

	public override void WriteString(string s, int byteCount)
	{
		EnsureCapacityFor(byteCount);
		position_ += Encoding.UTF8.GetBytes(s, 0, s.Length, buffer_, position_);
	}

	public static void NetworkTest(ILogger Logger)
	{
		int num = int.MaxValue;
		int num2 = int.MinValue;
		int num3 = 129;
		int num4 = 2000000;
		int num5 = -2000000;
		int num6 = -1;
		uint num7 = uint.MaxValue;
		long num8 = long.MaxValue;
		long num9 = 129L;
		long num10 = 2000000L;
		long num11 = -2000000L;
		long num12 = long.MinValue;
		long num13 = -1L;
		ulong num14 = ulong.MaxValue;
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		int num15 = citoMemoryStream.Position();
		bool flag = true;
		ProtocolParser.WriteUInt32(citoMemoryStream, num);
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize(num))
		{
			flag = false;
			Logger.Notification("wrongsize a");
		}
		num15 = citoMemoryStream.Position();
		ProtocolParser.WriteUInt32(citoMemoryStream, num2);
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize(num2))
		{
			flag = false;
			Logger.Notification("wrongsize b");
		}
		num15 = citoMemoryStream.Position();
		ProtocolParser.WriteUInt32(citoMemoryStream, num3);
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize(num3))
		{
			flag = false;
			Logger.Notification("wrongsize ba");
		}
		num15 = citoMemoryStream.Position();
		ProtocolParser.WriteUInt32(citoMemoryStream, num4);
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize(num4))
		{
			flag = false;
			Logger.Notification("wrongsize bb");
		}
		num15 = citoMemoryStream.Position();
		ProtocolParser.WriteUInt32(citoMemoryStream, num5);
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize(num5))
		{
			flag = false;
			Logger.Notification("wrongsize bc");
		}
		num15 = citoMemoryStream.Position();
		ProtocolParser.WriteUInt32(citoMemoryStream, num6);
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize(num6))
		{
			flag = false;
			Logger.Notification("wrongsize c");
		}
		num15 = citoMemoryStream.Position();
		ProtocolParser.WriteUInt32(citoMemoryStream, (int)num7);
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize((int)num7))
		{
			flag = false;
			Logger.Notification("wrongsize d");
		}
		num15 = citoMemoryStream.Position();
		ProtocolParser.WriteUInt64(citoMemoryStream, num8);
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize(num8))
		{
			flag = false;
			Logger.Notification("wrongsize e");
		}
		num15 = citoMemoryStream.Position();
		ProtocolParser.WriteUInt64(citoMemoryStream, num9);
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize(num9))
		{
			flag = false;
			Logger.Notification("wrongsize ea");
		}
		num15 = citoMemoryStream.Position();
		ProtocolParser.WriteUInt64(citoMemoryStream, num10);
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize(num10))
		{
			flag = false;
			Logger.Notification("wrongsize eb");
		}
		num15 = citoMemoryStream.Position();
		ProtocolParser.WriteUInt64(citoMemoryStream, num11);
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize(num11))
		{
			flag = false;
			Logger.Notification("wrongsize ec");
		}
		num15 = citoMemoryStream.Position();
		ProtocolParser.WriteUInt64(citoMemoryStream, num12);
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize(num12))
		{
			flag = false;
			Logger.Notification("wrongsize f");
		}
		num15 = citoMemoryStream.Position();
		ProtocolParser.WriteUInt64(citoMemoryStream, num13);
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize(num13))
		{
			flag = false;
			Logger.Notification("wrongsize g");
		}
		num15 = citoMemoryStream.Position();
		ProtocolParser.WriteUInt64(citoMemoryStream, (long)num14);
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize((long)num14))
		{
			flag = false;
			Logger.Notification("wrongsize h");
		}
		num15 = citoMemoryStream.Position();
		ProtocolParser.WriteString(citoMemoryStream, "testString");
		if (citoMemoryStream.Position() - num15 != ProtocolParser.GetSize("testString"))
		{
			flag = false;
			Logger.Notification("wrongsize string");
		}
		citoMemoryStream.position_ = 0;
		Logger.Notification("Test positive int.   Wrote " + num + "  Read " + ProtocolParser.ReadUInt32(citoMemoryStream));
		Logger.Notification("Test negative int.   Wrote " + num2 + "  Read " + ProtocolParser.ReadUInt32(citoMemoryStream));
		Logger.Notification("Test positive int.   Wrote " + num3 + "  Read " + ProtocolParser.ReadUInt32(citoMemoryStream));
		Logger.Notification("Test positive int.   Wrote " + num4 + "  Read " + ProtocolParser.ReadUInt32(citoMemoryStream));
		Logger.Notification("Test negative int.   Wrote " + num5 + "  Read " + ProtocolParser.ReadUInt32(citoMemoryStream));
		Logger.Notification("Test negative int.   Wrote " + num6 + "  Read " + ProtocolParser.ReadUInt32(citoMemoryStream));
		Logger.Notification("Test unsigned uint.  Wrote " + num7 + "  Read " + (uint)ProtocolParser.ReadUInt32(citoMemoryStream));
		Logger.Notification("Test positive long.  Wrote " + num8 + "  Read " + ProtocolParser.ReadUInt64(citoMemoryStream));
		Logger.Notification("Test positive long.  Wrote " + num9 + "  Read " + ProtocolParser.ReadUInt64(citoMemoryStream));
		Logger.Notification("Test positive long.  Wrote " + num10 + "  Read " + ProtocolParser.ReadUInt64(citoMemoryStream));
		Logger.Notification("Test negative long.  Wrote " + num11 + "  Read " + ProtocolParser.ReadUInt64(citoMemoryStream));
		Logger.Notification("Test negative long.  Wrote " + num12 + "  Read " + ProtocolParser.ReadUInt64(citoMemoryStream));
		Logger.Notification("Test negative long.  Wrote " + num13 + "  Read " + ProtocolParser.ReadUInt64(citoMemoryStream));
		Logger.Notification("Test unsigned ulong.  Wrote " + num14 + "  Read " + (ulong)ProtocolParser.ReadUInt64(citoMemoryStream));
		Logger.Notification("Test string.  Wrote 'testString'  Read '" + ProtocolParser.ReadString(citoMemoryStream) + "'");
		Logger.Notification(flag ? "All sizes were OK" : "Size error!");
	}
}
