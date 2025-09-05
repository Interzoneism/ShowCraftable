using System.IO;
using System.Text;

public class ProtocolParser
{
	private const int byteHighestBit = 128;

	private const int BitMaskLogicalRightShiftBy7 = 33554431;

	private const long BitMaskLogicalRightShiftBy7L = 144115188075855871L;

	private const int BitMask14bits = -16384;

	public static string ReadString(CitoStream stream)
	{
		int byteCount = ReadUInt32(stream);
		return stream.ReadString(byteCount);
	}

	public static byte[] ReadBytes(CitoStream stream)
	{
		int num = ReadUInt32(stream);
		byte[] array = new byte[num];
		int num2;
		for (int i = 0; i < num; i += num2)
		{
			num2 = stream.Read(array, i, num - i);
			if (num2 == 0)
			{
				throw new InvalidDataException("Expected " + (num - i) + " got " + i);
			}
		}
		return array;
	}

	public static void SkipBytes(CitoStream stream)
	{
		int length = ReadUInt32(stream);
		if (stream.CanSeek())
		{
			stream.Seek(length, CitoSeekOrigin.Current);
		}
		else
		{
			ReadBytes(stream);
		}
	}

	public static void WriteString(CitoStream stream, string s)
	{
		int byteCount = Encoding.UTF8.GetByteCount(s);
		WriteUInt32_(stream, byteCount);
		stream.WriteString(s, byteCount);
	}

	public static void WriteBytes(CitoStream stream, byte[] val)
	{
		WriteUInt32_(stream, val.Length);
		stream.Write(val, 0, val.Length);
	}

	public static Key ReadKey_(byte firstByte, CitoStream stream)
	{
		if (firstByte < 128)
		{
			return Key.Create(firstByte);
		}
		return Key.Create(firstByte, ReadUInt32(stream));
	}

	public static int ReadKeyAsInt(int firstByte, CitoStream stream)
	{
		int num = stream.ReadByte();
		return (firstByte & 0x7F) | (num << 7);
	}

	public static void WriteKey(CitoStream stream, Key key)
	{
		WriteUInt32_(stream, key);
	}

	public static void SkipKey(CitoStream stream, Key key)
	{
		switch (key.WireType)
		{
		case 5:
			stream.Seek(4, CitoSeekOrigin.Current);
			break;
		case 1:
			stream.Seek(8, CitoSeekOrigin.Current);
			break;
		case 2:
			stream.Seek(ReadUInt32(stream), CitoSeekOrigin.Current);
			break;
		case 0:
			ReadSkipVarInt(stream);
			break;
		default:
			throw new InvalidDataException("Unknown wire type: " + key.WireType + " at stream position " + stream.Position());
		}
	}

	public static byte[] ReadValueBytes(CitoStream stream, Key key)
	{
		int i = 0;
		switch (key.WireType)
		{
		case 5:
		{
			byte[] array;
			for (array = new byte[4]; i < 4; i += stream.Read(array, i, 4 - i))
			{
			}
			return array;
		}
		case 1:
		{
			byte[] array;
			for (array = new byte[8]; i < 8; i += stream.Read(array, i, 8 - i))
			{
			}
			return array;
		}
		case 2:
		{
			int num = ReadUInt32(stream);
			CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
			WriteUInt32(citoMemoryStream, num);
			i = citoMemoryStream.Position();
			int num2 = num + i;
			byte[] array = new byte[num2];
			for (int j = 0; j < i; j++)
			{
				array[j] = citoMemoryStream.ToArray()[j];
			}
			for (; i < num2; i += stream.Read(array, i, num2 - i))
			{
			}
			return array;
		}
		case 0:
			return ReadVarIntBytes(stream);
		default:
			throw new InvalidDataException("Unknown wire type: " + key.WireType + " at stream position " + stream.Position());
		}
	}

	public static void ReadSkipVarInt(CitoStream stream)
	{
		int num;
		do
		{
			num = stream.ReadByte();
			if (num < 0)
			{
				throw new IOException("Stream ended too early");
			}
		}
		while ((num & 0x80) != 0);
	}

	public static byte[] ReadVarIntBytes(CitoStream stream)
	{
		byte[] array = new byte[10];
		int num = 0;
		while (true)
		{
			int num2 = stream.ReadByte();
			if (num2 < 0)
			{
				throw new IOException("Stream ended too early");
			}
			array[num] = (byte)num2;
			num++;
			if ((num2 & 0x80) == 0)
			{
				break;
			}
			if (num >= array.Length)
			{
				throw new InvalidDataException("VarInt too long, more than 10 bytes");
			}
		}
		byte[] array2 = new byte[num];
		for (int i = 0; i < num; i++)
		{
			array2[i] = array[i];
		}
		return array2;
	}

	public static int ReadInt32(CitoStream stream)
	{
		return ReadUInt32(stream);
	}

	public static void WriteInt32(CitoStream stream, int val)
	{
		WriteUInt32(stream, val);
	}

	public static int ReadZInt32(CitoStream stream)
	{
		int num = ReadUInt32(stream);
		return (num >> 1) ^ (num << 31 >> 31);
	}

	public static void WriteZInt32(CitoStream stream, int val)
	{
		WriteUInt32_(stream, (val << 1) ^ (val >> 31));
	}

	public static long ReadInt64(CitoStream stream)
	{
		return ReadUInt64(stream);
	}

	public static void WriteInt64(CitoStream stream, long val)
	{
		WriteUInt64(stream, val);
	}

	public static long ReadZInt64(CitoStream stream)
	{
		long num = ReadUInt64(stream);
		return (num >> 1) ^ (num << 63 >> 63);
	}

	public static void WriteZInt64(CitoStream stream, long val)
	{
		WriteUInt64(stream, (val << 1) ^ (val >> 63));
	}

	public static int ReadUInt32(CitoStream stream)
	{
		int num = 0;
		for (int i = 0; i < 5; i++)
		{
			int num2 = stream.ReadByte();
			if (num2 < 0)
			{
				throw new IOException("Stream ended too early");
			}
			if (i == 4 && num2 > 15)
			{
				throw new InvalidDataException("Got larger VarInt than 32 bit unsigned");
			}
			if ((num2 & 0x80) == 0)
			{
				return num | (num2 << 7 * i);
			}
			num |= (num2 & 0x7F) << 7 * i;
		}
		throw new InvalidDataException("Got larger VarInt than 32 bit unsigned");
	}

	public static void WriteUInt32(CitoStream stream, int val)
	{
		if ((val & -16384) == 0)
		{
			stream.WriteSmallInt(val);
			return;
		}
		byte b;
		while (true)
		{
			b = (byte)(val & 0x7F);
			val = (val >> 7) & 0x1FFFFFF;
			if (val == 0)
			{
				break;
			}
			stream.WriteByte((byte)(b + 128));
		}
		stream.WriteByte(b);
	}

	public static void WriteUInt32_(CitoStream stream, int val)
	{
		if (val <= 16383)
		{
			stream.WriteSmallInt(val);
			return;
		}
		byte b;
		while (true)
		{
			b = (byte)(val & 0x7F);
			val >>= 7;
			if (val == 0)
			{
				break;
			}
			stream.WriteByte((byte)(b + 128));
		}
		stream.WriteByte(b);
	}

	public static long ReadUInt64(CitoStream stream)
	{
		long num = 0L;
		for (int i = 0; i < 10; i++)
		{
			int num2 = stream.ReadByte();
			if (num2 < 0)
			{
				throw new IOException("Stream ended too early");
			}
			if (i == 9 && num2 > 1)
			{
				throw new InvalidDataException("Got larger VarInt than 64 bit unsigned");
			}
			if ((num2 & 0x80) == 0)
			{
				return num | ((long)num2 << 7 * i);
			}
			num |= (long)(((ulong)num2 & 0x7FuL) << 7 * i);
		}
		throw new InvalidDataException("Got larger VarInt than 64 bit unsigned");
	}

	public static void WriteUInt64(CitoStream stream, long val)
	{
		if ((val & -16384) == 0L)
		{
			stream.WriteSmallInt((int)val);
			return;
		}
		byte b;
		while (true)
		{
			b = (byte)(val & 0x7F);
			val = (val >> 7) & 0x1FFFFFFFFFFFFFFL;
			if (val == 0L)
			{
				break;
			}
			stream.WriteByte((byte)(b + 128));
		}
		stream.WriteByte(b);
	}

	public static bool ReadBool(CitoStream stream)
	{
		int num = stream.ReadByte();
		if (num == 1)
		{
			return true;
		}
		if (num == 0)
		{
			return false;
		}
		if (num < 0)
		{
			throw new IOException("Stream ended too early");
		}
		throw new InvalidDataException("Invalid boolean value");
	}

	public static void WriteBool(CitoStream stream, bool val)
	{
		byte p = 0;
		if (val)
		{
			p = 1;
		}
		stream.WriteByte(p);
	}

	public static int PeekPacketId(byte[] data)
	{
		if (data.Length == 0)
		{
			return -1;
		}
		int num = data[0];
		if (num >= 128)
		{
			if (data.Length == 1)
			{
				return -1;
			}
			int num2 = data[1];
			if (num2 >= 128)
			{
				return -1;
			}
			num = (num & 0x7F) | (num2 << 7);
		}
		if (!Wire.IsValid(num % 8))
		{
			return -1;
		}
		return num;
	}

	public static int GetSize(int v)
	{
		if (v < 128)
		{
			if (v >= 0)
			{
				return 1;
			}
			return 5;
		}
		if (v < 16384)
		{
			return 2;
		}
		if (v < 2097152)
		{
			return 3;
		}
		if (v < 268435456)
		{
			return 4;
		}
		return 5;
	}

	public static int GetSize(long v)
	{
		if (v < 128)
		{
			if (v >= 0)
			{
				return 1;
			}
			return 10;
		}
		if (v < 16384)
		{
			return 2;
		}
		if (v < 2097152)
		{
			return 3;
		}
		if (v < 268435456)
		{
			return 4;
		}
		if (v < 34359738368L)
		{
			return 5;
		}
		if (v < 4398046511104L)
		{
			return 6;
		}
		if (v < 562949953421312L)
		{
			return 7;
		}
		if (v < 72057594037927936L)
		{
			return 8;
		}
		return 9;
	}

	public static int GetSize(byte[] data)
	{
		return data.Length + GetSize(data.Length);
	}

	public static int GetSize(string s)
	{
		int byteCount = Encoding.UTF8.GetByteCount(s);
		return GetSize(byteCount) + byteCount;
	}
}
