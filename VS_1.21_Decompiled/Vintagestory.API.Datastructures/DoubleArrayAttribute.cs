using System;
using System.IO;
using System.Text;
using Vintagestory.API.Config;

namespace Vintagestory.API.Datastructures;

public class DoubleArrayAttribute : ArrayAttribute<double>, IAttribute
{
	public DoubleArrayAttribute()
	{
	}

	public DoubleArrayAttribute(double[] value)
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
		value = new double[num];
		for (int i = 0; i < num; i++)
		{
			value[i] = stream.ReadDouble();
		}
	}

	public int GetAttributeId()
	{
		return 13;
	}

	public override string ToJsonToken()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("[");
		for (int i = 0; i < value.Length; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append(value[i].ToString(GlobalConstants.DefaultCultureInfo));
		}
		stringBuilder.Append("]");
		return stringBuilder.ToString();
	}

	public IAttribute Clone()
	{
		return new DoubleArrayAttribute(value);
	}

	Type IAttribute.GetType()
	{
		return GetType();
	}
}
