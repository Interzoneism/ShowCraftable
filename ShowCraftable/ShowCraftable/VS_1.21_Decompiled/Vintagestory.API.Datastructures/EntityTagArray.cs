using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;

namespace Vintagestory.API.Datastructures;

public readonly struct EntityTagArray
{
	public readonly ulong BitMask1;

	public readonly ulong BitMask2;

	public const byte MasksNumber = 2;

	public const int Size = 128;

	public static readonly EntityTagArray Empty = new EntityTagArray();

	public EntityTagArray(IEnumerable<ushort> tags)
	{
		BitMask1 = 0uL;
		BitMask2 = 0uL;
		foreach (ushort tag in tags)
		{
			WriteTagToBitMasks(tag, ref BitMask1, ref BitMask2);
		}
	}

	public EntityTagArray(ushort tag)
	{
		BitMask1 = 0uL;
		BitMask2 = 0uL;
		WriteTagToBitMasks(tag, ref BitMask1, ref BitMask2);
	}

	public EntityTagArray()
	{
		BitMask1 = 0uL;
		BitMask2 = 0uL;
	}

	public EntityTagArray(ulong bitMask1, ulong bitMask2)
	{
		BitMask1 = bitMask1;
		BitMask2 = bitMask2;
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
	}

	public IEnumerable<string> ToArray(ICoreAPI api)
	{
		return ToArray().Select(api.TagRegistry.EntityTagIdToTag);
	}

	public bool ContainsAll(EntityTagArray other)
	{
		if ((BitMask1 & other.BitMask1) == other.BitMask1)
		{
			return (BitMask2 & other.BitMask2) == other.BitMask2;
		}
		return false;
	}

	public bool IntersectsWithEach(EntityTagArray[] tags)
	{
		foreach (EntityTagArray second in tags)
		{
			if (!Intersect(this, second))
			{
				return false;
			}
		}
		return true;
	}

	public bool ContainsAllFromAtLeastOne(EntityTagArray[] tags)
	{
		foreach (EntityTagArray other in tags)
		{
			if (ContainsAll(other))
			{
				return true;
			}
		}
		return false;
	}

	public static bool Intersect(EntityTagArray first, EntityTagArray second)
	{
		ulong num = first.BitMask1 & second.BitMask1;
		ulong num2 = first.BitMask2 & second.BitMask2;
		return (num | num2) != 0;
	}

	public bool Intersect(EntityTagArray other)
	{
		return Intersect(this, other);
	}

	public EntityTagArray Remove(EntityTagArray other)
	{
		return new EntityTagArray(BitMask1 & ~other.BitMask1, BitMask2 & ~other.BitMask2);
	}

	public static EntityTagArray And(EntityTagArray first, EntityTagArray second)
	{
		return new EntityTagArray(first.BitMask1 & second.BitMask1, first.BitMask2 & second.BitMask2);
	}

	public static EntityTagArray Or(EntityTagArray first, EntityTagArray second)
	{
		return new EntityTagArray(first.BitMask1 | second.BitMask1, first.BitMask2 | second.BitMask2);
	}

	public static EntityTagArray Not(EntityTagArray value)
	{
		return new EntityTagArray(~value.BitMask1, ~value.BitMask2);
	}

	public static EntityTagArray operator &(EntityTagArray first, EntityTagArray second)
	{
		return And(first, second);
	}

	public static EntityTagArray operator |(EntityTagArray first, EntityTagArray second)
	{
		return Or(first, second);
	}

	public static EntityTagArray operator ~(EntityTagArray value)
	{
		return Not(value);
	}

	public static bool operator ==(EntityTagArray first, EntityTagArray second)
	{
		if (first.BitMask1 == second.BitMask1)
		{
			return first.BitMask2 == second.BitMask2;
		}
		return false;
	}

	public static bool operator !=(EntityTagArray first, EntityTagArray second)
	{
		return !(first == second);
	}

	public override bool Equals(object? obj)
	{
		if (obj is EntityTagArray entityTagArray)
		{
			return this == entityTagArray;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)(BitMask1 ^ BitMask2);
	}

	public override string ToString()
	{
		return PrintBitMask(BitMask2) + ":" + PrintBitMask(BitMask1);
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(BitMask1);
		writer.Write(BitMask2);
	}

	public static EntityTagArray FromBytes(BinaryReader reader)
	{
		ulong bitMask = reader.ReadUInt64();
		ulong bitMask2 = reader.ReadUInt64();
		return new EntityTagArray(bitMask, bitMask2);
	}

	private static void WriteTagToBitMasks(ushort tag, ref ulong bitMask1, ref ulong bitMask2)
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
			}
		}
	}

	private static string PrintBitMask(ulong bitMask)
	{
		return (from chunk in $"{bitMask:X16}".Chunk(4)
			select new string(chunk)).Aggregate((string first, string second) => first + "." + second);
	}
}
