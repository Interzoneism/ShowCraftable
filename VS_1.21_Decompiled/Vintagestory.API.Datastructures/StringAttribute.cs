using System;
using System.IO;

namespace Vintagestory.API.Datastructures;

public class StringAttribute : ScalarAttribute<string>, IAttribute
{
	private const int AttributeID = 5;

	public StringAttribute()
	{
		value = "";
	}

	public StringAttribute(string value)
	{
		base.value = value;
	}

	public void ToBytes(BinaryWriter stream)
	{
		if (value == null)
		{
			value = "";
		}
		stream.Write(value);
	}

	public void FromBytes(BinaryReader stream)
	{
		value = stream.ReadString();
	}

	public int GetAttributeId()
	{
		return 5;
	}

	public override string ToJsonToken()
	{
		return "\"" + value + "\"";
	}

	public IAttribute Clone()
	{
		return new StringAttribute(value);
	}

	internal static void DirectWrite(BinaryWriter writer, string key, string value)
	{
		writer.Write((byte)5);
		writer.Write(key);
		writer.Write(value);
	}

	Type IAttribute.GetType()
	{
		return GetType();
	}
}
