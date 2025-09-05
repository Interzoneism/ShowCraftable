using System;
using csogg;

namespace Vintagestory.Client.NoObf;

public class OggPage : Page
{
	[ThreadStatic]
	private static uint[] crc_lookup;

	private static uint crc_entry(uint index)
	{
		uint num = index << 24;
		for (int i = 0; i < 8; i++)
		{
			num = (((num & 0x80000000u) == 0) ? (num << 1) : ((num << 1) ^ 0x4C11DB7));
		}
		return num & 0xFFFFFFFFu;
	}

	internal int version()
	{
		return base.header_base[base.header + 4] & 0xFF;
	}

	internal int continued()
	{
		return base.header_base[base.header + 5] & 1;
	}

	public int bos()
	{
		return base.header_base[base.header + 5] & 2;
	}

	public int eos()
	{
		return base.header_base[base.header + 5] & 4;
	}

	public long granulepos()
	{
		return ((((((((((((((long)(base.header_base[base.header + 13] & 0xFF) << 8) | (uint)(base.header_base[base.header + 12] & 0xFF)) << 8) | (uint)(base.header_base[base.header + 11] & 0xFF)) << 8) | (uint)(base.header_base[base.header + 10] & 0xFF)) << 8) | (uint)(base.header_base[base.header + 9] & 0xFF)) << 8) | (uint)(base.header_base[base.header + 8] & 0xFF)) << 8) | (uint)(base.header_base[base.header + 7] & 0xFF)) << 8) | (uint)(base.header_base[base.header + 6] & 0xFF);
	}

	public int serialno()
	{
		return (base.header_base[base.header + 14] & 0xFF) | ((base.header_base[base.header + 15] & 0xFF) << 8) | ((base.header_base[base.header + 16] & 0xFF) << 16) | ((base.header_base[base.header + 17] & 0xFF) << 24);
	}

	internal int pageno()
	{
		return (base.header_base[base.header + 18] & 0xFF) | ((base.header_base[base.header + 19] & 0xFF) << 8) | ((base.header_base[base.header + 20] & 0xFF) << 16) | ((base.header_base[base.header + 21] & 0xFF) << 24);
	}

	internal void checksum()
	{
		uint num = 0u;
		for (int i = 0; i < base.header_len; i++)
		{
			uint num2 = (uint)(base.header_base[base.header + i] & 0xFF);
			uint num3 = (num >> 24) & 0xFF;
			num = (num << 8) ^ crc_lookup[num2 ^ num3];
		}
		for (int j = 0; j < base.body_len; j++)
		{
			uint num2 = (uint)(base.body_base[base.body + j] & 0xFF);
			uint num3 = (num >> 24) & 0xFF;
			num = (num << 8) ^ crc_lookup[num2 ^ num3];
		}
		base.header_base[base.header + 22] = (byte)num;
		base.header_base[base.header + 23] = (byte)(num >> 8);
		base.header_base[base.header + 24] = (byte)(num >> 16);
		base.header_base[base.header + 25] = (byte)(num >> 24);
	}

	public OggPage()
	{
		if (crc_lookup == null)
		{
			crc_lookup = new uint[256];
			for (uint num = 0u; num < crc_lookup.Length; num++)
			{
				crc_lookup[num] = crc_entry(num);
			}
		}
	}
}
