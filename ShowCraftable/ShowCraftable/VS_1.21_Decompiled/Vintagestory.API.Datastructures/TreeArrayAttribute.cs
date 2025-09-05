using System;
using System.IO;

namespace Vintagestory.API.Datastructures;

public class TreeArrayAttribute : ArrayAttribute<TreeAttribute>, IAttribute
{
	public TreeArrayAttribute()
	{
	}

	public TreeArrayAttribute(TreeAttribute[] value)
	{
		base.value = value;
	}

	public void ToBytes(BinaryWriter stream)
	{
		stream.Write(value.Length);
		for (int i = 0; i < value.Length; i++)
		{
			value[i].ToBytes(stream);
		}
	}

	public void FromBytes(BinaryReader stream)
	{
		int num = stream.ReadInt32();
		value = new TreeAttribute[num];
		for (int i = 0; i < num; i++)
		{
			value[i] = new TreeAttribute();
			value[i].FromBytes(stream);
		}
	}

	public int GetAttributeId()
	{
		return 14;
	}

	public IAttribute Clone()
	{
		TreeAttribute[] array = new TreeAttribute[value.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = value[i].Clone() as TreeAttribute;
		}
		return new TreeArrayAttribute(array);
	}

	Type IAttribute.GetType()
	{
		return GetType();
	}
}
