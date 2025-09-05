using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class ItemStack : IItemStack
{
	public EnumItemClass Class;

	public int Id;

	protected int stacksize;

	private TreeAttribute stackAttributes = new TreeAttribute();

	private TreeAttribute tempAttributes = new TreeAttribute();

	protected Item item;

	protected Block block;

	public CollectibleObject Collectible
	{
		get
		{
			if (Class == EnumItemClass.Block)
			{
				return block;
			}
			return item;
		}
	}

	public Item Item => item;

	public Block Block => block;

	public int StackSize
	{
		get
		{
			return stacksize;
		}
		set
		{
			stacksize = value;
		}
	}

	int IItemStack.Id => Id;

	public ITreeAttribute Attributes
	{
		get
		{
			return stackAttributes;
		}
		set
		{
			stackAttributes = (TreeAttribute)value;
		}
	}

	public ITreeAttribute TempAttributes
	{
		get
		{
			return tempAttributes;
		}
		set
		{
			tempAttributes = (TreeAttribute)value;
		}
	}

	public JsonObject ItemAttributes => Collectible.Attributes;

	EnumItemClass IItemStack.Class => Class;

	public ItemStack()
	{
	}

	public ItemStack(int id, EnumItemClass itemClass, int stacksize, TreeAttribute stackAttributes, IWorldAccessor resolver)
	{
		Id = id;
		Class = itemClass;
		this.stacksize = stacksize;
		this.stackAttributes = stackAttributes;
		if (itemClass == EnumItemClass.Block)
		{
			block = resolver.GetBlock(id);
		}
		else
		{
			item = resolver.GetItem(id);
		}
	}

	public ItemStack(BinaryReader reader)
	{
		FromBytes(reader);
	}

	public ItemStack(byte[] data)
	{
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader stream = new BinaryReader(input);
		FromBytes(stream);
	}

	public ItemStack(BinaryReader reader, IWorldAccessor resolver)
	{
		FromBytes(reader);
		if (Class == EnumItemClass.Block)
		{
			block = resolver.GetBlock(Id);
		}
		else
		{
			item = resolver.GetItem(Id);
		}
	}

	public ItemStack(CollectibleObject collectible, int stacksize = 1)
	{
		if (collectible == null)
		{
			throw new Exception("Can't create itemstack without collectible!");
		}
		if (collectible is Block)
		{
			Class = EnumItemClass.Block;
			Id = collectible.Id;
			block = collectible as Block;
			this.stacksize = stacksize;
		}
		else
		{
			Class = EnumItemClass.Item;
			Id = collectible.Id;
			item = collectible as Item;
			this.stacksize = stacksize;
		}
	}

	public ItemStack(Item item, int stacksize = 1)
	{
		if (item == null)
		{
			throw new Exception("Can't create itemstack without item!");
		}
		Class = EnumItemClass.Item;
		Id = item.ItemId;
		this.item = item;
		this.stacksize = stacksize;
	}

	public ItemStack(Block block, int stacksize = 1)
	{
		if (block == null)
		{
			throw new Exception("Can't create itemstack without block!");
		}
		Class = EnumItemClass.Block;
		Id = block.BlockId;
		this.block = block;
		this.stacksize = stacksize;
	}

	public bool Equals(IWorldAccessor worldForResolve, ItemStack sourceStack, params string[] ignoreAttributeSubTrees)
	{
		if (Collectible == null)
		{
			ResolveBlockOrItem(worldForResolve);
		}
		if (sourceStack != null && Collectible != null)
		{
			return Collectible.Equals(this, sourceStack, ignoreAttributeSubTrees);
		}
		return false;
	}

	public bool Satisfies(ItemStack sourceStack)
	{
		if (sourceStack != null)
		{
			return Collectible.Satisfies(this, sourceStack);
		}
		return false;
	}

	public void SetFrom(ItemStack stack)
	{
		Id = stack.Collectible.Id;
		Class = stack.Class;
		item = stack.item;
		block = stack.block;
		stacksize = stack.stacksize;
		stackAttributes = stack.stackAttributes.Clone() as TreeAttribute;
		tempAttributes = stack.tempAttributes.Clone() as TreeAttribute;
	}

	public override string ToString()
	{
		return stacksize + "x " + ((Class == EnumItemClass.Block) ? "Block" : "Item") + " Id " + Id + ", Code " + Collectible?.Code;
	}

	public byte[] ToBytes()
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter stream = new BinaryWriter(memoryStream))
		{
			ToBytes(stream);
		}
		return memoryStream.ToArray();
	}

	public void ToBytes(BinaryWriter stream)
	{
		stream.Write((int)Class);
		stream.Write(Id);
		stream.Write(stacksize);
		stackAttributes.ToBytes(stream);
	}

	public void FromBytes(BinaryReader stream)
	{
		Class = (EnumItemClass)stream.ReadInt32();
		Id = stream.ReadInt32();
		stacksize = stream.ReadInt32();
		stackAttributes.FromBytes(stream);
	}

	public bool ResolveBlockOrItem(IWorldAccessor resolver)
	{
		if (Class == EnumItemClass.Block)
		{
			block = resolver.GetBlock(Id);
			if (block == null)
			{
				return false;
			}
		}
		else
		{
			item = resolver.GetItem(Id);
			if (item == null)
			{
				return false;
			}
		}
		return true;
	}

	public bool MatchesSearchText(IWorldAccessor world, string searchText)
	{
		if (!GetName().CaseInsensitiveContains(searchText))
		{
			return GetDescription(world, new DummySlot(this)).CaseInsensitiveContains(searchText);
		}
		return true;
	}

	public string GetName()
	{
		return Collectible.GetHeldItemName(this);
	}

	public string GetDescription(IWorldAccessor world, ItemSlot inSlot, bool debug = false)
	{
		StringBuilder stringBuilder = new StringBuilder();
		Collectible.GetHeldItemInfo(inSlot, stringBuilder, world, debug);
		return stringBuilder.ToString();
	}

	public ItemStack Clone()
	{
		ItemStack emptyClone = GetEmptyClone();
		emptyClone.stacksize = stacksize;
		return emptyClone;
	}

	public ItemStack GetEmptyClone()
	{
		ItemStack itemStack = new ItemStack
		{
			item = item,
			block = block,
			Id = Id,
			Class = Class
		};
		if (stackAttributes != null)
		{
			itemStack.Attributes = Attributes.Clone();
		}
		return itemStack;
	}

	public bool FixMapping(Dictionary<int, AssetLocation> oldBlockMapping, Dictionary<int, AssetLocation> oldItemMapping, IWorldAccessor worldForNewMapping)
	{
		AssetLocation value;
		if (Class == EnumItemClass.Item)
		{
			if (oldItemMapping.TryGetValue(Id, out value) && value != null)
			{
				item = worldForNewMapping.GetItem(value);
				if (item == null)
				{
					worldForNewMapping.Logger.Warning("Cannot fix itemstack mapping, item code {0} not found item registry. Will delete stack.", value);
					return false;
				}
				Id = item.Id;
				return true;
			}
		}
		else if (oldBlockMapping.TryGetValue(Id, out value) && value != null)
		{
			block = worldForNewMapping.GetBlock(value);
			if (block == null)
			{
				worldForNewMapping.Logger.Warning("Cannot fix itemstack mapping, block code {0} not found block registry. Will delete stack.", value);
				return false;
			}
			Id = block.Id;
			return true;
		}
		worldForNewMapping.Logger.Warning("Cannot fix itemstack mapping, item/block id {0} not found in old mapping list. Will delete stack. ({1})", Id, Collectible);
		return false;
	}

	public override int GetHashCode()
	{
		return GetHashCode(null);
	}

	public int GetHashCode(string[] ignoredAttributes)
	{
		if (Class == EnumItemClass.Item)
		{
			return 0 ^ Id ^ Attributes.GetHashCode(ignoredAttributes);
		}
		return 0x20000 ^ Id ^ Attributes.GetHashCode(ignoredAttributes);
	}
}
