using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;

namespace Vintagestory.API.Datastructures;

public readonly struct ItemTagArray
{
	public readonly ulong BitMask1;

	public readonly ulong BitMask2;

	public readonly ulong BitMask3;

	public readonly ulong BitMask4;

	public const byte MasksNumber = 4;

	public const int Size = 256;

	public static readonly ItemTagArray Empty = new ItemTagArray();

	public ItemTagArray(IEnumerable<ushort> tags)
	{
		BitMask1 = 0uL;
		BitMask2 = 0uL;
		BitMask3 = 0uL;
		BitMask4 = 0uL;
		foreach (ushort tag in tags)
		{
			WriteTagToBitMasks(tag, ref BitMask1, ref BitMask2, ref BitMask3, ref BitMask4);
		}
	}

	public ItemTagArray(ushort tag)
	{
		BitMask1 = 0uL;
		BitMask2 = 0uL;
		BitMask3 = 0uL;
		BitMask4 = 0uL;
		WriteTagToBitMasks(tag, ref BitMask1, ref BitMask2, ref BitMask3, ref BitMask4);
	}

	public ItemTagArray()
	{
		BitMask1 = 0uL;
		BitMask2 = 0uL;
		BitMask3 = 0uL;
		BitMask4 = 0uL;
	}

	public ItemTagArray(ulong bitMask1, ulong bitMask2, ulong bitMask3, ulong bitMask4)
	{
		BitMask1 = bitMask1;
		BitMask2 = bitMask2;
		BitMask3 = bitMask3;
		BitMask4 = bitMask4;
	}

	public IEnumerable<ushort> ToArray()
	{
		for (ushort index = 0; index < 64; index++)
		{
			if ((BitMask1 & (ulong)(1L << (int)index)) != 0L)
			{
				yield return (ushort)(index + 1);
			}
		}
		for (ushort index = 0; index < 64; index++)
		{
			if ((BitMask2 & (ulong)(1L << (int)index)) != 0L)
			{
				yield return (ushort)(index + 64 + 1);
			}
		}
		for (ushort index = 0; index < 64; index++)
		{
			if ((BitMask3 & (ulong)(1L << (int)index)) != 0L)
			{
				yield return (ushort)(index + 128 + 1);
			}
		}
		for (ushort index = 0; index < 64; index++)
		{
			if ((BitMask4 & (ulong)(1L << (int)index)) != 0L)
			{
				yield return (ushort)(index + 192 + 1);
			}
		}
	}

	public IEnumerable<string> ToArray(ICoreAPI api)
	{
		return ToArray().Select(api.TagRegistry.ItemTagIdToTag);
	}

	public bool ContainsAll(ItemTagArray other)
	{
		if ((BitMask1 & other.BitMask1) == other.BitMask1 && (BitMask2 & other.BitMask2) == other.BitMask2 && (BitMask3 & other.BitMask3) == other.BitMask3)
		{
			return (BitMask4 & other.BitMask4) == other.BitMask4;
		}
		return false;
	}

	public bool IntersectsWithEach(ItemTagArray[] tags)
	{
		foreach (ItemTagArray second in tags)
		{
			if (!Intersect(this, second))
			{
				return false;
			}
		}
		return true;
	}

	public bool ContainsAllFromAtLeastOne(ItemTagArray[] tags)
	{
		foreach (ItemTagArray other in tags)
		{
			if (ContainsAll(other))
			{
				return true;
			}
		}
		return false;
	}

	public static bool Intersect(ItemTagArray first, ItemTagArray second)
	{
		ulong num = first.BitMask1 & second.BitMask1;
		ulong num2 = first.BitMask2 & second.BitMask2;
		ulong num3 = first.BitMask3 & second.BitMask3;
		ulong num4 = first.BitMask4 & second.BitMask4;
		return (num | num2 | num3 | num4) != 0;
	}

	public bool Intersect(ItemTagArray other)
	{
		return Intersect(this, other);
	}

	public static ItemTagArray And(ItemTagArray first, ItemTagArray second)
	{
		return new ItemTagArray(first.BitMask1 & second.BitMask1, first.BitMask2 & second.BitMask2, first.BitMask3 & second.BitMask3, first.BitMask4 & second.BitMask4);
	}

	public static ItemTagArray Or(ItemTagArray first, ItemTagArray second)
	{
		return new ItemTagArray(first.BitMask1 | second.BitMask1, first.BitMask2 | second.BitMask2, first.BitMask3 | second.BitMask3, first.BitMask4 | second.BitMask4);
	}

	public static ItemTagArray Not(ItemTagArray value)
	{
		return new ItemTagArray(~value.BitMask1, ~value.BitMask2, ~value.BitMask3, ~value.BitMask4);
	}

	public static ItemTagArray operator &(ItemTagArray first, ItemTagArray second)
	{
		return And(first, second);
	}

	public static ItemTagArray operator |(ItemTagArray first, ItemTagArray second)
	{
		return Or(first, second);
	}

	public static ItemTagArray operator ~(ItemTagArray value)
	{
		return Not(value);
	}

	public static bool operator ==(ItemTagArray first, ItemTagArray second)
	{
		if (first.BitMask1 == second.BitMask1 && first.BitMask2 == second.BitMask2 && first.BitMask3 == second.BitMask3)
		{
			return first.BitMask4 == second.BitMask4;
		}
		return false;
	}

	public static bool operator !=(ItemTagArray first, ItemTagArray second)
	{
		return !(first == second);
	}

	public override bool Equals(object? obj)
	{
		if (obj is ItemTagArray itemTagArray)
		{
			return this == itemTagArray;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)(BitMask1 ^ BitMask2 ^ BitMask3 ^ BitMask4);
	}

	public override string ToString()
	{
		return PrintBitMask(BitMask4) + ":" + PrintBitMask(BitMask3) + ":" + PrintBitMask(BitMask2) + ":" + PrintBitMask(BitMask1);
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write((byte)4);
		writer.Write(BitMask1);
		writer.Write(BitMask2);
		writer.Write(BitMask3);
		writer.Write(BitMask4);
	}

	public static ItemTagArray FromBytes(BinaryReader reader)
	{
		int num = reader.ReadInt32();
		if (num != 4)
		{
			throw new ArgumentException($"Trying to read 'ItemTagArray' from BinaryReader, but size of the array in reader ({num * 64}) is not equal current size of all item tag arrays ({256}).");
		}
		ulong bitMask = reader.ReadUInt64();
		ulong bitMask2 = reader.ReadUInt64();
		ulong bitMask3 = reader.ReadUInt64();
		ulong bitMask4 = reader.ReadUInt64();
		return new ItemTagArray(bitMask, bitMask2, bitMask3, bitMask4);
	}

	private static void WriteTagToBitMasks(ushort tag, ref ulong bitMask1, ref ulong bitMask2, ref ulong bitMask3, ref ulong bitMask4)
	{
		if (tag != 0)
		{
			int num = (tag - 1) % 64;
			switch ((tag - 1) / 64)
			{
			case 0:
				bitMask1 |= (ulong)(1L << num);
				break;
			case 1:
				bitMask2 |= (ulong)(1L << num);
				break;
			case 2:
				bitMask3 |= (ulong)(1L << num);
				break;
			case 3:
				bitMask4 |= (ulong)(1L << num);
				break;
			}
		}
	}

	private static string PrintBitMask(ulong bitMask)
	{
		return (from chunk in $"{bitMask:X16}".Chunk(4)
			select new string(chunk)).Aggregate((string first, string second) => first + "." + second);
	}

	public bool isPresentIn(ref ItemTagArray other)
	{
		if ((BitMask1 & other.BitMask1) != BitMask1)
		{
			return false;
		}
		if ((BitMask2 & other.BitMask2) != BitMask2)
		{
			return false;
		}
		if ((BitMask3 & other.BitMask3) != BitMask3)
		{
			return false;
		}
		if ((BitMask4 & other.BitMask4) != BitMask4)
		{
			return false;
		}
		return true;
	}
}
