using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.Common;

public class TagRegistry : ITagRegistry
{
	public const int MaxEntityTags = 128;

	public const int MaxItemTags = 256;

	public const int MaxBlockTags = 256;

	public const string TagsAssetPath = "config/preloaded-tags";

	internal List<string> entityTags = new List<string>();

	internal List<string> itemTags = new List<string>();

	internal List<string> blockTags = new List<string>();

	private readonly OrderedDictionary<string, ushort> entityTagsToTagIds = new OrderedDictionary<string, ushort>();

	private readonly OrderedDictionary<string, ushort> itemTagsToTagIds = new OrderedDictionary<string, ushort>();

	private readonly OrderedDictionary<string, ushort> blockTagsToTagIds = new OrderedDictionary<string, ushort>();

	internal bool restrictNewTags { get; set; }

	internal EnumAppSide Side { get; set; } = EnumAppSide.Server;

	public void RegisterEntityTags(params string[] tags)
	{
		ProcessTags(tags, entityTags, entityTagsToTagIds, ignoreClientSide: false, 128);
	}

	public void RegisterItemTags(params string[] tags)
	{
		ProcessTags(tags, itemTags, itemTagsToTagIds, ignoreClientSide: false, 256);
	}

	public void RegisterBlockTags(params string[] tags)
	{
		ProcessTags(tags, blockTags, blockTagsToTagIds, ignoreClientSide: false, 256);
	}

	public ushort[] EntityTagsToTagIds(string[] tags, bool removeUnknownTags = false)
	{
		return (from id in tags.Select(EntityTagToTagId)
			where !removeUnknownTags || id != 0
			select id).Order().ToArray();
	}

	public ushort[] ItemTagsToTagIds(string[] tags, bool removeUnknownTags = false)
	{
		return (from id in tags.Select(ItemTagToTagId)
			where !removeUnknownTags || id != 0
			select id).Order().ToArray();
	}

	public ushort[] BlockTagsToTagIds(string[] tags, bool removeUnknownTags = false)
	{
		return (from id in tags.Select(BlockTagToTagId)
			where !removeUnknownTags || id != 0
			select id).Order().ToArray();
	}

	public EntityTagArray EntityTagsToTagArray(params string[] tags)
	{
		return new EntityTagArray(tags.Select(EntityTagToTagId));
	}

	public ItemTagArray ItemTagsToTagArray(params string[] tags)
	{
		return new ItemTagArray(tags.Select(ItemTagToTagId));
	}

	public BlockTagArray BlockTagsToTagArray(params string[] tags)
	{
		return new BlockTagArray(tags.Select(BlockTagToTagId));
	}

	public ushort EntityTagToTagId(string tag)
	{
		return TryGetTagId(tag, entityTagsToTagIds);
	}

	public ushort ItemTagToTagId(string tag)
	{
		return TryGetTagId(tag, itemTagsToTagIds);
	}

	public ushort BlockTagToTagId(string tag)
	{
		return TryGetTagId(tag, blockTagsToTagIds);
	}

	public string EntityTagIdToTag(ushort id)
	{
		if (id != 0)
		{
			return entityTags[id - 1];
		}
		return "";
	}

	public string ItemTagIdToTag(ushort id)
	{
		if (id != 0)
		{
			return itemTags[id - 1];
		}
		return "";
	}

	public string BlockTagIdToTag(ushort id)
	{
		if (id != 0)
		{
			return blockTags[id - 1];
		}
		return "";
	}

	public void LoadTagsFromAssets(ICoreServerAPI api)
	{
		foreach (var (assetLocation2, preloadedTagsStructure2) in api.Assets.GetMany<PreloadedTagsStructure>(api.Logger, "config/preloaded-tags"))
		{
			try
			{
				RegisterEntityTags(preloadedTagsStructure2.EntityTags);
				RegisterItemTags(preloadedTagsStructure2.ItemTags);
				RegisterBlockTags(preloadedTagsStructure2.BlockTags);
			}
			catch (Exception value)
			{
				api.Logger.Error($"Error while loading tags from domain '{assetLocation2.Domain}': \n{value}");
				continue;
			}
			api.Logger.Notification($"Loaded {preloadedTagsStructure2.EntityTags.Length} entity tags, {preloadedTagsStructure2.ItemTags.Length} item tags and {preloadedTagsStructure2.BlockTags.Length} block tags from '{assetLocation2.Domain}' domain");
			if (preloadedTagsStructure2.EntityTags.Length != 0)
			{
				string value2 = preloadedTagsStructure2.EntityTags.Aggregate((string first, string second) => first + ", " + second);
				api.Logger.VerboseDebug($"Loaded {preloadedTagsStructure2.EntityTags.Length} entity tags from '{assetLocation2.Domain}' domain: {value2}");
			}
			if (preloadedTagsStructure2.ItemTags.Length != 0)
			{
				string value3 = preloadedTagsStructure2.ItemTags.Aggregate((string first, string second) => first + ", " + second);
				api.Logger.VerboseDebug($"Loaded {preloadedTagsStructure2.ItemTags.Length} item tags from '{assetLocation2.Domain}' domain: {value3}");
			}
			if (preloadedTagsStructure2.BlockTags.Length != 0)
			{
				string value4 = preloadedTagsStructure2.BlockTags.Aggregate((string first, string second) => first + ", " + second);
				api.Logger.VerboseDebug($"Loaded {preloadedTagsStructure2.BlockTags.Length} block tags from '{assetLocation2.Domain}' domain: {value4}");
			}
		}
	}

	internal void RegisterEntityTagsOnClient(IEnumerable<string> tags)
	{
		ProcessTags(tags, entityTags, entityTagsToTagIds, ignoreClientSide: true);
	}

	internal void RegisterItemTagsOnClient(IEnumerable<string> tags)
	{
		ProcessTags(tags, itemTags, itemTagsToTagIds, ignoreClientSide: true);
	}

	internal void RegisterBlockTagsOnClient(IEnumerable<string> tags)
	{
		ProcessTags(tags, blockTags, blockTagsToTagIds, ignoreClientSide: true);
	}

	private static ushort TryGetTagId(string tag, OrderedDictionary<string, ushort> mapping)
	{
		if (mapping.TryGetValue(tag, out var value))
		{
			return value;
		}
		return 0;
	}

	private void ProcessTags(IEnumerable<string> objectTags, List<string> idsToTags, OrderedDictionary<string, ushort> tagsToIds, bool ignoreClientSide = false, int maximumTags = 0)
	{
		if (!objectTags.Any())
		{
			return;
		}
		if (!ignoreClientSide && Side == EnumAppSide.Client)
		{
			throw new InvalidOperationException("Error when registering tags: " + objectTags.Aggregate((string first, string second) => first + ", " + second) + ".\nCannot register new tags on client side.");
		}
		if (restrictNewTags)
		{
			throw new InvalidOperationException("Error when registering tags: " + objectTags.Aggregate((string first, string second) => first + ", " + second) + ".\nCannot add new tags. The registry is synchronized and locked.");
		}
		List<string> list = new List<string>();
		foreach (string objectTag in objectTags)
		{
			if (!idsToTags.Contains(objectTag))
			{
				list.Add(objectTag);
			}
		}
		foreach (string item in list)
		{
			ushort num = (ushort)(idsToTags.Count + 1);
			if (maximumTags > 0 && num > maximumTags)
			{
				throw new InvalidOperationException($"Error when registering tags: {objectTags.Aggregate((string first, string second) => first + ", " + second)}.\nCannot register more than {maximumTags} tags. The registry is full.");
			}
			idsToTags.Add(item);
			tagsToIds.Add(item, num);
		}
	}
}
