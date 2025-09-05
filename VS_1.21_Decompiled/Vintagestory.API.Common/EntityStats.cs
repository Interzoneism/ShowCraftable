using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

public class EntityStats : IEnumerable<KeyValuePair<string, EntityFloatStats>>, IEnumerable
{
	private IDictionary<string, EntityFloatStats> floatStats = new FastSmallDictionary<string, EntityFloatStats>(0);

	private Entity entity;

	private bool ignoreChange;

	public EntityFloatStats this[string key]
	{
		get
		{
			return floatStats[key];
		}
		set
		{
			floatStats[key] = value;
		}
	}

	public EntityStats(Entity entity)
	{
		this.entity = entity;
	}

	public void Initialize(ICoreAPI api)
	{
		if (api.Side == EnumAppSide.Client)
		{
			entity.WatchedAttributes.RegisterModifiedListener("stats", onStatsChanged);
		}
	}

	private void onStatsChanged()
	{
		if (!ignoreChange)
		{
			FromTreeAttributes(entity.WatchedAttributes);
		}
	}

	public IEnumerator<KeyValuePair<string, EntityFloatStats>> GetEnumerator()
	{
		return floatStats.GetEnumerator();
	}

	IEnumerator<KeyValuePair<string, EntityFloatStats>> IEnumerable<KeyValuePair<string, EntityFloatStats>>.GetEnumerator()
	{
		return floatStats.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return floatStats.GetEnumerator();
	}

	public void ToTreeAttributes(ITreeAttribute tree, bool forClient)
	{
		if (floatStats.Count == 0)
		{
			return;
		}
		TreeAttribute treeAttribute;
		if (tree.TryGetAttribute("stats", out var value))
		{
			treeAttribute = value as TreeAttribute;
			if (treeAttribute != null)
			{
				treeAttribute.Clear();
				goto IL_0041;
			}
		}
		treeAttribute = (TreeAttribute)(tree["stats"] = new TreeAttribute());
		goto IL_0041;
		IL_0041:
		foreach (KeyValuePair<string, EntityFloatStats> floatStat in floatStats)
		{
			TreeAttribute treeAttribute2 = new TreeAttribute();
			floatStat.Value.ToTreeAttributes(treeAttribute2, forClient);
			treeAttribute[floatStat.Key] = treeAttribute2;
		}
	}

	public void FromTreeAttributes(ITreeAttribute tree)
	{
		if (!(tree["stats"] is ITreeAttribute treeAttribute))
		{
			return;
		}
		foreach (KeyValuePair<string, IAttribute> item in treeAttribute)
		{
			EntityFloatStats entityFloatStats = new EntityFloatStats();
			entityFloatStats.FromTreeAttributes(item.Value as ITreeAttribute);
			floatStats[item.Key] = entityFloatStats;
		}
	}

	public EntityStats Register(string category, EnumStatBlendType blendType = EnumStatBlendType.WeightedSum)
	{
		EntityFloatStats entityFloatStats = (floatStats[category] = new EntityFloatStats());
		entityFloatStats.BlendType = blendType;
		return this;
	}

	public EntityStats Set(string category, string code, float value, bool persistent = false)
	{
		ignoreChange = true;
		if (!floatStats.TryGetValue(category, out var value2))
		{
			value2 = (floatStats[category] = new EntityFloatStats());
		}
		value2.Set(code, value, persistent);
		ToTreeAttributes(entity.WatchedAttributes, forClient: true);
		entity.WatchedAttributes.MarkPathDirty("stats");
		ignoreChange = false;
		return this;
	}

	public EntityStats Remove(string category, string code)
	{
		ignoreChange = true;
		if (floatStats.TryGetValue(category, out var value))
		{
			value.Remove(code);
		}
		ToTreeAttributes(entity.WatchedAttributes, forClient: true);
		entity.WatchedAttributes.MarkPathDirty("stats");
		ignoreChange = false;
		return this;
	}

	public float GetBlended(string category)
	{
		if (floatStats.TryGetValue(category, out var value))
		{
			return value.GetBlended();
		}
		return 1f;
	}
}
