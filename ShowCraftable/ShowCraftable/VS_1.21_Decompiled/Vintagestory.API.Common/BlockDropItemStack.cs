using System;
using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class BlockDropItemStack
{
	[DocumentAsJson]
	public EnumItemClass Type;

	[DocumentAsJson]
	public AssetLocation Code;

	[DocumentAsJson]
	public NatFloat Quantity = NatFloat.One;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject Attributes;

	[DocumentAsJson]
	public bool LastDrop;

	[DocumentAsJson]
	public EnumTool? Tool;

	public ItemStack ResolvedItemstack;

	[DocumentAsJson]
	public string DropModbyStat;

	private static Random random = new Random();

	public BlockDropItemStack()
	{
	}

	public BlockDropItemStack(ItemStack stack, float chance = 1f)
	{
		Type = stack.Class;
		Code = stack.Collectible.Code;
		Quantity.avg = chance;
		ResolvedItemstack = stack;
	}

	public bool Resolve(IWorldAccessor resolver, string sourceForErrorLogging, AssetLocation assetLoc)
	{
		if (Type == EnumItemClass.Block)
		{
			Block block = resolver.GetBlock(Code);
			if (block == null)
			{
				resolver.Logger.Warning("Failed resolving a blocks block drop or smeltedstack with code {0} in {1}", Code, sourceForErrorLogging + assetLoc);
				return false;
			}
			ResolvedItemstack = new ItemStack(block);
		}
		else
		{
			Item item = resolver.GetItem(Code);
			if (item == null)
			{
				resolver.Logger.Warning("Failed resolving a blocks item drop or smeltedstack with code {0} in {1}", Code, sourceForErrorLogging + assetLoc);
				return false;
			}
			ResolvedItemstack = new ItemStack(item);
		}
		if (Attributes != null)
		{
			IAttribute attribute = Attributes.ToAttribute();
			if (attribute is ITreeAttribute)
			{
				ResolvedItemstack.Attributes = (ITreeAttribute)attribute;
			}
		}
		return true;
	}

	public ItemStack GetNextItemStack(float dropQuantityMultiplier = 1f)
	{
		if (ResolvedItemstack == null)
		{
			return null;
		}
		int num = GameMath.RoundRandom(random, Quantity.nextFloat(dropQuantityMultiplier));
		if (num <= 0)
		{
			return null;
		}
		ItemStack itemStack = ResolvedItemstack.Clone();
		itemStack.StackSize = num;
		return itemStack;
	}

	public BlockDropItemStack Clone()
	{
		BlockDropItemStack blockDropItemStack = new BlockDropItemStack
		{
			Code = Code?.Clone(),
			Quantity = Quantity,
			Type = Type,
			LastDrop = LastDrop,
			Tool = Tool,
			ResolvedItemstack = ResolvedItemstack,
			DropModbyStat = DropModbyStat
		};
		if (Attributes != null)
		{
			blockDropItemStack.Attributes = Attributes.Clone();
		}
		return blockDropItemStack;
	}

	public virtual void FromBytes(BinaryReader reader, IClassRegistryAPI instancer)
	{
		Type = (EnumItemClass)reader.ReadInt16();
		Code = new AssetLocation(reader.ReadString());
		Quantity = NatFloat.One;
		Quantity.FromBytes(reader);
		ResolvedItemstack = new ItemStack(reader);
		LastDrop = reader.ReadBoolean();
		if (reader.ReadBoolean())
		{
			DropModbyStat = reader.ReadString();
		}
	}

	public virtual void ToBytes(BinaryWriter writer)
	{
		writer.Write((short)Type);
		writer.Write(Code.ToShortString());
		Quantity.ToBytes(writer);
		ResolvedItemstack.ToBytes(writer);
		writer.Write(LastDrop);
		writer.Write(DropModbyStat != null);
		if (DropModbyStat != null)
		{
			writer.Write(DropModbyStat);
		}
	}

	public ItemStack ToRandomItemstackForPlayer(IPlayer byPlayer, IWorldAccessor world, float dropQuantityMultiplier)
	{
		if (Tool.HasValue && (byPlayer == null || Tool != byPlayer.InventoryManager.ActiveTool))
		{
			return null;
		}
		float num = 1f;
		if (byPlayer != null && DropModbyStat != null)
		{
			num = byPlayer.Entity.Stats.GetBlended(DropModbyStat);
		}
		ItemStack itemStack = GetNextItemStack(dropQuantityMultiplier * num);
		if (itemStack?.Collectible is IResolvableCollectible resolvableCollectible)
		{
			DummySlot dummySlot = new DummySlot(itemStack);
			resolvableCollectible.Resolve(dummySlot, world);
			itemStack = dummySlot.Itemstack;
		}
		return itemStack;
	}
}
