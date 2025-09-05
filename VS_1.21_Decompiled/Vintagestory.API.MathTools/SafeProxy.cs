namespace Vintagestory.API.MathTools;

internal class SafeProxy
{
	private const uint Poly = 3988292384u;

	private static readonly uint[] _table;

	static SafeProxy()
	{
		_table = new uint[4096];
		for (uint num = 0u; num < 256; num++)
		{
			uint num2 = num;
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					num2 = (((num2 & 1) == 1) ? (0xEDB88320u ^ (num2 >> 1)) : (num2 >> 1));
				}
				_table[i * 256 + num] = num2;
			}
		}
	}

	public uint Append(uint crc, byte[] input, int offset, int length)
	{
		uint num = 0xFFFFFFFFu ^ crc;
		uint[] table = _table;
		while (length >= 16)
		{
			num = table[3840 + ((num ^ input[offset]) & 0xFF)] ^ table[3584 + (((num >> 8) ^ input[offset + 1]) & 0xFF)] ^ table[3328 + (((num >> 16) ^ input[offset + 2]) & 0xFF)] ^ table[3072 + (((num >> 24) ^ input[offset + 3]) & 0xFF)] ^ table[2816 + input[offset + 4]] ^ table[2560 + input[offset + 5]] ^ table[2304 + input[offset + 6]] ^ table[2048 + input[offset + 7]] ^ table[1792 + input[offset + 8]] ^ table[1536 + input[offset + 9]] ^ table[1280 + input[offset + 10]] ^ table[1024 + input[offset + 11]] ^ table[768 + input[offset + 12]] ^ table[512 + input[offset + 13]] ^ table[256 + input[offset + 14]] ^ table[input[offset + 15]];
			offset += 16;
			length -= 16;
		}
		while (--length >= 0)
		{
			num = table[(num ^ input[offset++]) & 0xFF] ^ (num >> 8);
		}
		return num ^ 0xFFFFFFFFu;
	}
}
