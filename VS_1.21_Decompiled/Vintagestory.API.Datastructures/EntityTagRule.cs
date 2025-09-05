using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Datastructures;

public readonly struct EntityTagRule
{
	public readonly EntityTagArray TagsThatShouldBePresent;

	public readonly EntityTagArray TagsThatShouldBeAbsent;

	public const string NotPrefix = "not-";

	public static readonly EntityTagRule Empty = new EntityTagRule(EntityTagArray.Empty, EntityTagArray.Empty);

	public EntityTagRule(EntityTagArray tagsThatShouldBePresent, EntityTagArray tagsThatShouldBeAbsent)
	{
		TagsThatShouldBePresent = tagsThatShouldBePresent;
		TagsThatShouldBeAbsent = tagsThatShouldBeAbsent;
	}

	public EntityTagRule(ICoreAPI api, IEnumerable<string> tags)
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (string tag in tags)
		{
			if (tag.StartsWith("not-"))
			{
				string text = tag;
				int length = "not-".Length;
				list2.Add(text.Substring(length, text.Length - length));
			}
			else
			{
				list.Add(tag);
			}
		}
		TagsThatShouldBePresent = api.TagRegistry.EntityTagsToTagArray(list.ToArray());
		TagsThatShouldBeAbsent = api.TagRegistry.EntityTagsToTagArray(list2.ToArray());
	}

	public bool Intersects(EntityTagArray tags)
	{
		if (TagsThatShouldBePresent != EntityTagArray.Empty && !tags.Intersect(TagsThatShouldBePresent))
		{
			return false;
		}
		if (TagsThatShouldBeAbsent != EntityTagArray.Empty && tags.ContainsAll(TagsThatShouldBeAbsent))
		{
			return false;
		}
		return true;
	}

	public static bool IntersectsWithEach(EntityTagArray entityTag, EntityTagRule[] rules)
	{
		for (int i = 0; i < rules.Length; i++)
		{
			EntityTagRule entityTagRule = rules[i];
			if ((entityTagRule.TagsThatShouldBePresent != EntityTagArray.Empty && !entityTag.Intersect(entityTagRule.TagsThatShouldBePresent)) || (entityTagRule.TagsThatShouldBeAbsent != EntityTagArray.Empty && entityTag.ContainsAll(entityTagRule.TagsThatShouldBeAbsent)))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ContainsAllFromAtLeastOne(EntityTagArray entityTag, EntityTagRule[] rules)
	{
		for (int i = 0; i < rules.Length; i++)
		{
			EntityTagRule entityTagRule = rules[i];
			if (entityTag.ContainsAll(entityTagRule.TagsThatShouldBePresent) && (!(entityTagRule.TagsThatShouldBeAbsent != EntityTagArray.Empty) || !entityTag.Intersect(entityTagRule.TagsThatShouldBeAbsent)))
			{
				return true;
			}
		}
		return false;
	}

	public static bool operator ==(EntityTagRule first, EntityTagRule second)
	{
		if (first.TagsThatShouldBePresent == second.TagsThatShouldBePresent)
		{
			return first.TagsThatShouldBeAbsent == second.TagsThatShouldBeAbsent;
		}
		return false;
	}

	public static bool operator !=(EntityTagRule first, EntityTagRule second)
	{
		return !(first == second);
	}

	public override bool Equals(object? obj)
	{
		if (obj is EntityTagRule entityTagRule)
		{
			return this == entityTagRule;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return TagsThatShouldBePresent.GetHashCode() ^ TagsThatShouldBeAbsent.GetHashCode();
	}

	public override string ToString()
	{
		return $"{TagsThatShouldBePresent}-{TagsThatShouldBeAbsent}";
	}
}
