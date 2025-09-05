using System;
using System.IO;

namespace Vintagestory.API.Datastructures;

public class StreamedByteArrayAttribute : ArrayAttribute<byte>, IAttribute
{
	private const int AttributeID = 8;

	private readonly FastMemoryStream ms;

	public StreamedByteArrayAttribute(FastMemoryStream ms)
	{
		this.ms = ms;
	}

	public void BeginDirectWrite(BinaryWriter stream, string key)
	{
		stream.Write((byte)8);
		stream.Write(key);
	}

	public void ToBytes(BinaryWriter stream)
	{
		stream.Write((ushort)ms.Position);
		if (stream.BaseStream is FastMemoryStream fastMemoryStream)
		{
			fastMemoryStream.Write(ms);
		}
		else
		{
			stream.Write(ms.ToArray());
		}
	}

	public int GetAttributeId()
	{
		return 8;
	}

	public void FromBytes(BinaryReader stream)
	{
		throw new NotImplementedException();
	}

	public IAttribute Clone()
	{
		throw new NotImplementedException();
	}

	Type IAttribute.GetType()
	{
		return GetType();
	}
}
