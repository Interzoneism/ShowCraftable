using System;
using System.IO;

namespace Vintagestory.API.Datastructures;

public class ByteArrayAttribute : ArrayAttribute<byte>, IAttribute
{
	private const int AttributeID = 8;

	public ByteArrayAttribute()
	{
	}

	public ByteArrayAttribute(byte[] value)
	{
		base.value = value;
	}

	public ByteArrayAttribute(FastMemoryStream ms)
	{
		value = ms.ToArray();
	}

	public void ToBytes(BinaryWriter stream)
	{
		stream.Write((ushort)value.Length);
		stream.Write(value);
	}

	public void FromBytes(BinaryReader stream)
	{
		int count = stream.ReadInt16();
		value = stream.ReadBytes(count);
	}

	public int GetAttributeId()
	{
		return 8;
	}

	public IAttribute Clone()
	{
		return new ByteArrayAttribute((byte[])value.Clone());
	}

	Type IAttribute.GetType()
	{
		return GetType();
	}
}
