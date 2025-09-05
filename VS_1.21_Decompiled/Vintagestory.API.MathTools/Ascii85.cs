using System;
using System.IO;
using System.Text;

namespace Vintagestory.API.MathTools;

public static class Ascii85
{
	private const char c_firstCharacter = '!';

	private const char c_lastCharacter = 'u';

	private static readonly uint[] s_powersOf85 = new uint[5] { 52200625u, 614125u, 7225u, 85u, 1u };

	public static string Encode(byte[] bytes)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes");
		}
		StringBuilder stringBuilder = new StringBuilder(bytes.Length * 5 / 4);
		int num = 0;
		uint num2 = 0u;
		foreach (byte b in bytes)
		{
			num2 |= (uint)(b << 24 - num * 8);
			num++;
			if (num == 4)
			{
				if (num2 == 0)
				{
					stringBuilder.Append('z');
				}
				else
				{
					EncodeValue(stringBuilder, num2, 0);
				}
				num = 0;
				num2 = 0u;
			}
		}
		if (num > 0)
		{
			EncodeValue(stringBuilder, num2, 4 - num);
		}
		return stringBuilder.ToString();
	}

	public static byte[] Decode(string encoded)
	{
		if (encoded == null)
		{
			throw new ArgumentNullException("encoded");
		}
		using MemoryStream memoryStream = new MemoryStream(encoded.Length * 4 / 5);
		int num = 0;
		uint num2 = 0u;
		foreach (char c in encoded)
		{
			if (c == 'z' && num == 0)
			{
				DecodeValue(memoryStream, num2, 0);
				continue;
			}
			if (c < '!' || c > 'u')
			{
				throw new FormatException("Invalid character '" + c + "' in Ascii85 block.");
			}
			try
			{
				num2 = checked(num2 + (uint)(s_powersOf85[num] * (c - 33)));
			}
			catch (OverflowException innerException)
			{
				throw new FormatException("The current group of characters decodes to a value greater than UInt32.MaxValue.", innerException);
			}
			num++;
			if (num == 5)
			{
				DecodeValue(memoryStream, num2, 0);
				num = 0;
				num2 = 0u;
			}
		}
		if (num == 1)
		{
			throw new FormatException("The final Ascii85 block must contain more than one character.");
		}
		if (num > 1)
		{
			for (int j = num; j < 5; j++)
			{
				try
				{
					num2 = checked(num2 + 84 * s_powersOf85[j]);
				}
				catch (OverflowException innerException2)
				{
					throw new FormatException("The current group of characters decodes to a value greater than UInt32.MaxValue.", innerException2);
				}
			}
			DecodeValue(memoryStream, num2, 5 - num);
		}
		return memoryStream.ToArray();
	}

	private static void EncodeValue(StringBuilder sb, uint value, int paddingBytes)
	{
		char[] array = new char[5];
		for (int num = 4; num >= 0; num--)
		{
			array[num] = (char)(value % 85 + 33);
			value /= 85;
		}
		if (paddingBytes != 0)
		{
			Array.Resize(ref array, 5 - paddingBytes);
		}
		sb.Append(array);
	}

	private static void DecodeValue(Stream stream, uint value, int paddingChars)
	{
		stream.WriteByte((byte)(value >> 24));
		if (paddingChars == 3)
		{
			return;
		}
		stream.WriteByte((byte)((value >> 16) & 0xFF));
		if (paddingChars != 2)
		{
			stream.WriteByte((byte)((value >> 8) & 0xFF));
			if (paddingChars != 1)
			{
				stream.WriteByte((byte)(value & 0xFF));
			}
		}
	}
}
