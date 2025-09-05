using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Datastructures;

public readonly struct BlockTagRule
{
	public readonly BlockTagArray TagsThatShouldBePresent;

	public readonly BlockTagArray TagsThatShouldBeAbsent;

	public const string NotPrefix = "not-";

	public static readonly BlockTagRule Empty = new BlockTagRule(BlockTagArray.Empty, BlockTagArray.Empty);

	public BlockTagRule(BlockTagArray tagsThatShouldBePresent, BlockTagArray tagsThatShouldBeAbsent)
	{
		TagsThatShouldBePresent = tagsThatShouldBePresent;
		TagsThatShouldBeAbsent = tagsThatShouldBeAbsent;
	}

	public BlockTagRule(ICoreAPI api, IEnumerable<string> tags)
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
		TagsThatShouldBePresent = api.TagRegistry.BlockTagsToTagArray(list.ToArray());
		TagsThatShouldBeAbsent = api.TagRegistry.BlockTagsToTagArray(list2.ToArray());
	}

	public bool Intersects(BlockTagArray tags)
	{
		if (TagsThatShouldBePresent != BlockTagArray.Empty && !tags.Intersect(TagsThatShouldBePresent))
		{
			return false;
		}
		if (TagsThatShouldBeAbsent != BlockTagArray.Empty && tags.ContainsAll(TagsThatShouldBeAbsent))
		{
			return false;
		}
		return true;
	}

	public static bool IntersectsWithEach(BlockTagArray blockTag, BlockTagRule[] rules)
	{
		for (int i = 0; i < rules.Length; i++)
		{
			BlockTagRule blockTagRule = rules[i];
			if ((blockTagRule.TagsThatShouldBePresent != BlockTagArray.Empty && !blockTag.Intersect(blockTagRule.TagsThatShouldBePresent)) || (blockTagRule.TagsThatShouldBeAbsent != BlockTagArray.Empty && blockTag.ContainsAll(blockTagRule.TagsThatShouldBeAbsent)))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ContainsAllFromAtLeastOne(BlockTagArray blockTag, BlockTagRule[] rules)
	{
		for (int i = 0; i < rules.Length; i++)
		{
			BlockTagRule blockTagRule = rules[i];
			if (blockTag.ContainsAll(blockTagRule.TagsThatShouldBePresent) && (!(blockTagRule.TagsThatShouldBeAbsent != BlockTagArray.Empty) || !blockTag.Intersect(blockTagRule.TagsThatShouldBeAbsent)))
			{
				return true;
			}
		}
		return false;
	}

	public static bool operator ==(BlockTagRule first, BlockTagRule second)
	{
		if (first.TagsThatShouldBePresent == second.TagsThatShouldBePresent)
		{
			return first.TagsThatShouldBeAbsent == second.TagsThatShouldBeAbsent;
		}
		return false;
	}

	public static bool operator !=(BlockTagRule first, BlockTagRule second)
	{
		return !(first == second);
	}

	public override bool Equals(object? obj)
	{
		if (obj is BlockTagRule blockTagRule)
		{
			return this == blockTagRule;
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
