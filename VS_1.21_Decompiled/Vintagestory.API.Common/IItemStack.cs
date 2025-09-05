using System.IO;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

public interface IItemStack
{
	CollectibleObject Collectible { get; }

	EnumItemClass Class { get; }

	Item Item { get; }

	Block Block { get; }

	int StackSize { get; set; }

	int Id { get; }

	ITreeAttribute Attributes { get; set; }

	bool Equals(IWorldAccessor worldForResolve, ItemStack sourceStack, params string[] ignoreAttributeSubTrees);

	void ToBytes(BinaryWriter stream);

	void FromBytes(BinaryReader stream);

	bool MatchesSearchText(IWorldAccessor world, string searchText);

	string GetName();

	string GetDescription(IWorldAccessor world, ItemSlot inSlot, bool debug = false);

	ItemStack Clone();
}
