using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

public class EntityFloatStats
{
	public OrderedDictionary<string, EntityStat<float>> ValuesByKey = new OrderedDictionary<string, EntityStat<float>>();

	public EnumStatBlendType BlendType = EnumStatBlendType.WeightedSum;

	public EntityFloatStats()
	{
		ValuesByKey["base"] = new EntityStat<float>
		{
			Value = 1f,
			Persistent = true
		};
	}

	public float GetBlended()
	{
		float num = 0f;
		bool flag = true;
		switch (BlendType)
		{
		case EnumStatBlendType.FlatMultiply:
			foreach (EntityStat<float> value in ValuesByKey.Values)
			{
				if (flag)
				{
					num = value.Value;
					flag = false;
				}
				num *= value.Value;
			}
			break;
		case EnumStatBlendType.FlatSum:
			foreach (EntityStat<float> value2 in ValuesByKey.Values)
			{
				num += value2.Value;
			}
			break;
		case EnumStatBlendType.WeightedSum:
			foreach (EntityStat<float> value3 in ValuesByKey.Values)
			{
				num += value3.Value * value3.Weight;
			}
			break;
		case EnumStatBlendType.WeightedOverlay:
			foreach (EntityStat<float> value4 in ValuesByKey.Values)
			{
				if (flag)
				{
					num = value4.Value;
					flag = false;
				}
				num = value4.Value * value4.Weight + num * (1f - value4.Weight);
			}
			break;
		}
		return num;
	}

	public void Set(string code, float value, bool persistent = false)
	{
		ValuesByKey[code] = new EntityStat<float>
		{
			Value = value,
			Persistent = persistent
		};
	}

	public void Remove(string code)
	{
		ValuesByKey.Remove(code);
	}

	public void ToTreeAttributes(ITreeAttribute tree, bool forClient)
	{
		foreach (KeyValuePair<string, EntityStat<float>> item in ValuesByKey)
		{
			if (item.Value.Persistent || forClient)
			{
				tree.SetFloat(item.Key, item.Value.Value);
			}
		}
	}

	public void FromTreeAttributes(ITreeAttribute tree)
	{
		foreach (KeyValuePair<string, IAttribute> item in tree)
		{
			ValuesByKey[item.Key] = new EntityStat<float>
			{
				Value = (item.Value as FloatAttribute).value,
				Persistent = true
			};
		}
	}
}
