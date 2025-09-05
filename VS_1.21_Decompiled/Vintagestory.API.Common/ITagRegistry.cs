using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public interface ITagRegistry
{
	void RegisterEntityTags(params string[] tags);

	void RegisterItemTags(params string[] tags);

	void RegisterBlockTags(params string[] tags);

	ushort[] EntityTagsToTagIds(string[] tags, bool removeUnknownTags = false);

	ushort[] ItemTagsToTagIds(string[] tags, bool removeUnknownTags = false);

	ushort[] BlockTagsToTagIds(string[] tags, bool removeUnknownTags = false);

	EntityTagArray EntityTagsToTagArray(params string[] tags);

	ItemTagArray ItemTagsToTagArray(params string[] tags);

	BlockTagArray BlockTagsToTagArray(params string[] tags);

	ushort EntityTagToTagId(string tag);

	ushort ItemTagToTagId(string tag);

	ushort BlockTagToTagId(string tag);

	string EntityTagIdToTag(ushort id);

	string ItemTagIdToTag(ushort id);

	string BlockTagIdToTag(ushort id);

	void LoadTagsFromAssets(ICoreServerAPI api);
}
