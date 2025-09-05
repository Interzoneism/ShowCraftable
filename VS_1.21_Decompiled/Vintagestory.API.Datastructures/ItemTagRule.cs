using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Datastructures;

public readonly struct ItemTagRule
{
	public readonly ItemTagArray TagsThatShouldBePresent;

	public readonly ItemTagArray TagsThatShouldBeAbsent;

	public const string NotPrefix = "not-";

	public static readonly ItemTagRule Empty = new ItemTagRule(ItemTagArray.Empty, ItemTagArray.Empty);

	public ItemTagRule(ItemTagArray tagsThatShouldBePresent, ItemTagArray tagsThatShouldBeAbsent)
	{
		TagsThatShouldBePresent = tagsThatShouldBePresent;
		TagsThatShouldBeAbsent = tagsThatShouldBeAbsent;
	}

	public ItemTagRule(ICoreAPI api, IEnumerable<string> tags)
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
		TagsThatShouldBePresent = api.TagRegistry.ItemTagsToTagArray(list.ToArray());
		TagsThatShouldBeAbsent = api.TagRegistry.ItemTagsToTagArray(list2.ToArray());
	}

	public bool Intersects(ItemTagArray tags)
	{
		if (TagsThatShouldBePresent != ItemTagArray.Empty && !tags.Intersect(TagsThatShouldBePresent))
		{
			return false;
		}
		if (TagsThatShouldBeAbsent != ItemTagArray.Empty && tags.ContainsAll(TagsThatShouldBeAbsent))
		{
			return false;
		}
		return true;
	}

	public static bool IntersectsWithEach(ItemTagArray itemTag, ItemTagRule[] rules)
	{
		for (int i = 0; i < rules.Length; i++)
		{
			ItemTagRule itemTagRule = rules[i];
			if ((itemTagRule.TagsThatShouldBePresent != ItemTagArray.Empty && !itemTag.Intersect(itemTagRule.TagsThatShouldBePresent)) || (itemTagRule.TagsThatShouldBeAbsent != ItemTagArray.Empty && itemTag.ContainsAll(itemTagRule.TagsThatShouldBeAbsent)))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ContainsAllFromAtLeastOne(ItemTagArray itemTag, ItemTagRule[] rules)
	{
		for (int i = 0; i < rules.Length; i++)
		{
			ItemTagRule itemTagRule = rules[i];
			if (itemTag.ContainsAll(itemTagRule.TagsThatShouldBePresent) && (!(itemTagRule.TagsThatShouldBeAbsent != ItemTagArray.Empty) || !itemTag.Intersect(itemTagRule.TagsThatShouldBeAbsent)))
			{
				return true;
			}
		}
		return false;
	}

	public static bool operator ==(ItemTagRule first, ItemTagRule second)
	{
		if (first.TagsThatShouldBePresent == second.TagsThatShouldBePresent)
		{
			return first.TagsThatShouldBeAbsent == second.TagsThatShouldBeAbsent;
		}
		return false;
	}

	public static bool operator !=(ItemTagRule first, ItemTagRule second)
	{
		return !(first == second);
	}

	public override bool Equals(object? obj)
	{
		if (obj is ItemTagRule itemTagRule)
		{
			return this == itemTagRule;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return TagsThatShouldBePresent.GetHashCode() ^ TagsThatShouldBeAbsent.GetHashCode();
	}

	public override string ToString()
	{
		return $"+{TagsThatShouldBePresent}\n-{TagsThatShouldBeAbsent}";
	}
}
