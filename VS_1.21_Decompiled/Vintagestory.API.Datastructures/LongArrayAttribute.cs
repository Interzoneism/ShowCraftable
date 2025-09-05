using System;
using System.IO;

namespace Vintagestory.API.Datastructures;

public class LongArrayAttribute : ArrayAttribute<long>, IAttribute
{
	public uint[] AsUint
	{
		get
		{
			uint[] array = new uint[value.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (uint)value[i];
			}
			return array;
		}
	}

	public LongArrayAttribute()
	{
	}

	public LongArrayAttribute(long[] value)
	{
		base.value = value;
	}

	public void ToBytes(BinaryWriter stream)
	{
		stream.Write(value.Length);
		for (int i = 0; i < value.Length; i++)
		{
			stream.Write(value[i]);
		}
	}

	public void FromBytes(BinaryReader stream)
	{
		int num = stream.ReadInt32();
		value = new long[num];
		for (int i = 0; i < num; i++)
		{
			value[i] = stream.ReadInt64();
		}
	}

	public int GetAttributeId()
	{
		return 15;
	}

	public IAttribute Clone()
	{
		return new LongArrayAttribute((long[])value.Clone());
	}

	Type IAttribute.GetType()
	{
		return GetType();
	}
}
