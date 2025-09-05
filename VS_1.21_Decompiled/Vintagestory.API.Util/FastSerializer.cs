using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Util;

public static class FastSerializer
{
	public static void Write(FastMemoryStream stream, int field, byte[] val)
	{
		if (val != null)
		{
			WriteTagLengthDelim(stream, field, val.Length);
			stream.Write(val, 0, val.Length);
		}
	}

	public static void Write(FastMemoryStream stream, int field, FastMemoryStream val)
	{
		WriteTagLengthDelim(stream, field, (int)val.Position);
		stream.Write(val);
	}

	public static void Write(FastMemoryStream stream, int field, int val)
	{
		if (val != 0)
		{
			WriteTagVarInt(stream, field);
			Write(stream, val);
		}
	}

	public static void Write(FastMemoryStream stream, int field, float val)
	{
		if (val != 0f)
		{
			WriteTagFixed32(stream, field);
			stream.Write(val);
		}
	}

	public static void Write(FastMemoryStream stream, int field, bool val)
	{
		if (val)
		{
			WriteTagVarInt(stream, field);
			Write(stream, (byte)1);
		}
	}

	public static void Write(FastMemoryStream stream, int field, string s)
	{
		if (s != null)
		{
			int byteCount = Encoding.UTF8.GetByteCount(s);
			WriteTagLengthDelim(stream, field, byteCount);
			stream.WriteUTF8String(s, byteCount);
		}
	}

	public static void Write(FastMemoryStream stream, int field, BlockPos val)
	{
		if (!(val == null))
		{
			int x = val.X;
			int internalY = val.InternalY;
			int z = val.Z;
			WriteTagLengthDelim(stream, field, 3 + GetSize(x) + GetSize(internalY) + GetSize(z));
			Write(stream, 1, x);
			Write(stream, 2, internalY);
			Write(stream, 3, z);
		}
	}

	public static void Write(FastMemoryStream stream, int field, Vec2i val)
	{
		if (!(val == null))
		{
			int x = val.X;
			int y = val.Y;
			if (x == 0 && y == 0)
			{
				WriteTagLengthDelim(stream, field, 3);
				WriteTagVarInt(stream, 1);
				Write(stream, 0);
			}
			else
			{
				WriteTagLengthDelim(stream, field, 2 + GetSize(x) + GetSize(y));
				Write(stream, 1, x);
				Write(stream, 2, y);
			}
		}
	}

	public static void Write(FastMemoryStream stream, int field, Vec4i val)
	{
		if (val != null)
		{
			int x = val.X;
			int y = val.Y;
			int z = val.Z;
			int w = val.W;
			WriteTagLengthDelim(stream, field, 4 + GetSize(x) + GetSize(y) + GetSize(z) + GetSize(w));
			Write(stream, 1, x);
			Write(stream, 2, y);
			Write(stream, 3, z);
			Write(stream, 4, w);
		}
	}

	public static void Write(FastMemoryStream stream, int field, IEnumerable<byte[]> collection)
	{
		if (collection == null)
		{
			return;
		}
		foreach (byte[] item in collection)
		{
			Write(stream, field, item ?? Array.Empty<byte>());
		}
	}

	public static void Write(FastMemoryStream stream, int field, IEnumerable<int> collection)
	{
		if (collection == null)
		{
			return;
		}
		int num = field * 8;
		if (num < 128)
		{
			foreach (int item in collection)
			{
				stream.WriteByte((byte)num);
				Write(stream, item);
			}
			return;
		}
		foreach (int item2 in collection)
		{
			stream.WriteTwoBytes(num);
			Write(stream, item2);
		}
	}

	public static void Write(FastMemoryStream stream, int field, IEnumerable<ushort> collection)
	{
		if (collection == null)
		{
			return;
		}
		int num = field * 8;
		if (num < 128)
		{
			foreach (ushort item in collection)
			{
				stream.WriteByte((byte)num);
				Write(stream, item);
			}
			return;
		}
		foreach (ushort item2 in collection)
		{
			stream.WriteTwoBytes(num);
			Write(stream, item2);
		}
	}

	public static void Write(FastMemoryStream stream, int field, IEnumerable<string> collection)
	{
		if (collection == null)
		{
			return;
		}
		foreach (string item in collection)
		{
			Write(stream, field, item ?? "");
		}
	}

	public static void Write(FastMemoryStream stream, int field, IEnumerable<BlockPos> collection)
	{
		if (collection == null)
		{
			return;
		}
		foreach (BlockPos item in collection)
		{
			Write(stream, field, item);
		}
	}

	public static void Write(FastMemoryStream stream, int field, IEnumerable<Vec4i> collection)
	{
		if (collection == null)
		{
			return;
		}
		foreach (Vec4i item in collection)
		{
			Write(stream, field, item);
		}
	}

	public static void Write(FastMemoryStream stream, int field, IDictionary<string, byte[]> dict)
	{
		if (dict == null)
		{
			return;
		}
		foreach (KeyValuePair<string, byte[]> item in dict)
		{
			WriteTagLengthDelim(stream, field, 2 + GetSize(item.Key) + GetSize(item.Value));
			Write(stream, 1, item.Key);
			Write(stream, 2, item.Value);
		}
	}

	public static void Write(FastMemoryStream stream, int field, IDictionary<Vec2i, float> dict)
	{
		if (dict == null)
		{
			return;
		}
		foreach (KeyValuePair<Vec2i, float> item in dict)
		{
			WriteTagLengthDelim(stream, field, 2 + GetSize(item.Key) + GetSize(item.Value));
			Write(stream, 1, item.Key);
			Write(stream, 2, item.Value);
		}
	}

	public static void WritePacked(FastMemoryStream stream, int field, IEnumerable<ushort> collection)
	{
		if (collection == null)
		{
			return;
		}
		int num = 0;
		foreach (ushort item in collection)
		{
			num += GetSize(item);
		}
		WriteTagLengthDelim(stream, field, num);
		foreach (ushort item2 in collection)
		{
			Write(stream, item2);
		}
	}

	public static void WritePacked(FastMemoryStream stream, int field, IEnumerable<int> collection)
	{
		if (collection == null)
		{
			return;
		}
		int num = 0;
		foreach (int item in collection)
		{
			num += GetSize(item);
		}
		WriteTagLengthDelim(stream, field, num);
		foreach (int item2 in collection)
		{
			Write(stream, item2);
		}
	}

	public static int GetSize(ushort v)
	{
		if (v < 128)
		{
			return 1;
		}
		if (v < 16384)
		{
			return 2;
		}
		return 3;
	}

	public static int GetSize(int v)
	{
		if (v < 128)
		{
			if (v <= 0)
			{
				if (v != 0)
				{
					return 5;
				}
				return -1;
			}
			return 1;
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

	public static int GetSize(float v)
	{
		if (v != 0f)
		{
			return 4;
		}
		return -1;
	}

	public static int GetSize(byte[] val)
	{
		if (val == null)
		{
			return -1;
		}
		return val.Length + GetSize(val.Length);
	}

	public static int GetSize(string s)
	{
		if (s == null)
		{
			return -1;
		}
		int byteCount = Encoding.UTF8.GetByteCount(s);
		return byteCount + GetSize(byteCount);
	}

	public static int GetSize(Vec2i val)
	{
		if (val == null)
		{
			return -1;
		}
		int num = 2 + GetSize(val.X) + GetSize(val.Y);
		return num + GetSize(num);
	}

	private static void Write(FastMemoryStream stream, byte val)
	{
		stream.WriteByte(val);
	}

	private static void Write(FastMemoryStream stream, ushort val)
	{
		if (val < 128)
		{
			stream.WriteByte((byte)val);
		}
		else if (val < 16384)
		{
			stream.WriteTwoBytes(val);
		}
		else
		{
			stream.WriteThreeBytes(val);
		}
	}

	private static void Write(FastMemoryStream stream, int val)
	{
		if (val >= 0)
		{
			if (val < 128)
			{
				stream.WriteByte((byte)val);
				return;
			}
			if (val < 16384)
			{
				stream.WriteTwoBytes(val);
				return;
			}
			if (val < 2097152)
			{
				stream.WriteThreeBytes(val);
				return;
			}
		}
		stream.WriteThreeBytes((val & 0x1FFFFF) | 0x200000);
		val >>>= 21;
		if (val < 128)
		{
			stream.WriteByte((byte)val);
		}
		else
		{
			stream.WriteTwoBytes(val);
		}
	}

	private static void Write(FastMemoryStream stream, long val)
	{
		if (val >= 0)
		{
			if (val < 128)
			{
				stream.WriteByte((byte)val);
				return;
			}
			if (val < 16384)
			{
				stream.WriteTwoBytes((int)val);
				return;
			}
			if (val < 2097152)
			{
				stream.WriteThreeBytes((int)val);
				return;
			}
		}
		stream.WriteThreeBytes(((int)val & 0x1FFFFF) | 0x200000);
		Write(stream, val >>> 21);
	}

	private static void WriteTagVarInt(FastMemoryStream stream, int field)
	{
		if (field < 16)
		{
			stream.WriteByte((byte)(field * 8));
		}
		else
		{
			stream.WriteTwoBytes(field * 8);
		}
	}

	private static void WriteTagFixed32(FastMemoryStream stream, int field)
	{
		if (field < 16)
		{
			stream.WriteByte((byte)(field * 8 + 5));
		}
		else
		{
			stream.WriteTwoBytes(field * 8 + 5);
		}
	}

	public static void WriteTagLengthDelim(FastMemoryStream stream, int field, int length)
	{
		if (field < 16)
		{
			stream.WriteByte((byte)(field * 8 + 2));
		}
		else
		{
			stream.WriteTwoBytes(field * 8 + 2);
		}
		if (length < 128)
		{
			stream.WriteByte((byte)length);
		}
		else if (length < 16384)
		{
			stream.WriteTwoBytes(length);
		}
		else if (length < 2097152)
		{
			stream.WriteThreeBytes(length);
		}
		else
		{
			Write(stream, length);
		}
	}
}
