using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Datastructures;

public struct SmallBoolArray : IEquatable<int>
{
	public const int OnAllSides = 63;

	private int bits;

	public bool this[int i]
	{
		get
		{
			return (bits & (1 << i)) != 0;
		}
		set
		{
			if (value)
			{
				bits |= 1 << i;
			}
			else
			{
				bits &= ~(1 << i);
			}
		}
	}

	public bool Any => bits != 0;

	public bool All
	{
		get
		{
			return bits == 63;
		}
		set
		{
			bits = (value ? 63 : 0);
		}
	}

	public bool SidesAndBase => (bits & 0x2F) == 47;

	public bool Horizontals => (bits & 0xF) == 15;

	public bool Verticals => (bits & 0x30) == 48;

	public static implicit operator int(SmallBoolArray a)
	{
		return a.bits;
	}

	public SmallBoolArray(int values)
	{
		bits = values;
	}

	public SmallBoolArray(int[] values)
	{
		bits = 0;
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i] != 0)
			{
				bits |= 1 << i;
			}
		}
	}

	public SmallBoolArray(bool[] values)
	{
		bits = 0;
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i])
			{
				bits |= 1 << i;
			}
		}
	}

	public bool Equals(int other)
	{
		return bits == other;
	}

	public override bool Equals(object o)
	{
		if (o is int num)
		{
			return bits == num;
		}
		if (o is SmallBoolArray smallBoolArray)
		{
			return bits == smallBoolArray.bits;
		}
		return false;
	}

	public static bool operator ==(SmallBoolArray left, int right)
	{
		return right == left.bits;
	}

	public static bool operator !=(SmallBoolArray left, int right)
	{
		return right != left.bits;
	}

	public void Fill(bool b)
	{
		bits = (b ? 63 : 0);
	}

	public int[] ToIntArray(int size)
	{
		int[] array = new int[size];
		int num = bits;
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = num & 1;
			num >>= 1;
		}
		return array;
	}

	public bool Opposite(int i)
	{
		return (bits & (1 << (i ^ (2 - i / 4)))) != 0;
	}

	public bool OnSide(BlockFacing face)
	{
		return (bits & (1 << face.Index)) != 0;
	}

	public int Value()
	{
		return bits;
	}

	public override int GetHashCode()
	{
		return 1537853281 + bits.GetHashCode();
	}
}
