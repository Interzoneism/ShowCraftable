using System.Collections;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.API.Datastructures;

public abstract class ArrayAttribute<T>
{
	public T[] value;

	public virtual bool Equals(IWorldAccessor worldForResolve, IAttribute attr)
	{
		object obj = attr.GetValue();
		if (!obj.GetType().IsArray)
		{
			return false;
		}
		IList list = value;
		IList list2 = obj as IList;
		if (list.Count != list2.Count)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (!list[i].Equals(list2[i]) && !EqualityUtil.NumberEquals(list[i], list2[i]))
			{
				return false;
			}
		}
		return true;
	}

	public virtual object GetValue()
	{
		return value;
	}

	public virtual string ToJsonToken()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("[");
		for (int i = 0; i < value.Length; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(", ");
			}
			if (value[i] is IAttribute)
			{
				stringBuilder.Append((value[i] as IAttribute).ToJsonToken());
			}
			else
			{
				stringBuilder.Append(value[i]);
			}
		}
		stringBuilder.Append("]");
		return stringBuilder.ToString();
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("[");
		for (int i = 0; i < value.Length; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append(value[i]);
		}
		stringBuilder.Append("]");
		return stringBuilder.ToString();
	}

	public override int GetHashCode()
	{
		int num = 0;
		for (int i = 0; i < value.Length; i++)
		{
			num = ((i != 0) ? (num ^ value[i].GetHashCode()) : value[i].GetHashCode());
		}
		return num;
	}
}
