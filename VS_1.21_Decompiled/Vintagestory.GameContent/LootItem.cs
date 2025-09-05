using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class LootItem
{
	public AssetLocation[] codes;

	public EnumItemClass type;

	public float chance;

	public float minQuantity;

	public float maxQuantity;

	public static LootItem Item(float chance, float minQuantity, float maxQuantity, params string[] codes)
	{
		return new LootItem
		{
			codes = AssetLocation.toLocations(codes),
			type = EnumItemClass.Item,
			chance = chance,
			minQuantity = minQuantity,
			maxQuantity = maxQuantity
		};
	}

	public static LootItem Item(float chance, float minQuantity, float maxQuantity, params AssetLocation[] codes)
	{
		return new LootItem
		{
			codes = codes,
			type = EnumItemClass.Item,
			chance = chance,
			minQuantity = minQuantity,
			maxQuantity = maxQuantity
		};
	}

	public static LootItem Block(float chance, float minQuantity, float maxQuantity, params string[] codes)
	{
		return new LootItem
		{
			codes = AssetLocation.toLocations(codes),
			type = EnumItemClass.Block,
			chance = chance,
			minQuantity = minQuantity,
			maxQuantity = maxQuantity
		};
	}

	public static LootItem Block(float chance, float minQuantity, float maxQuantity, params AssetLocation[] codes)
	{
		return new LootItem
		{
			codes = codes,
			type = EnumItemClass.Block,
			chance = chance,
			minQuantity = minQuantity,
			maxQuantity = maxQuantity
		};
	}

	public ItemStack GetItemStack(IWorldAccessor world, int variant, int quantity)
	{
		ItemStack result = null;
		AssetLocation assetLocation = codes[variant % codes.Length];
		if (type == EnumItemClass.Block)
		{
			Block block = world.GetBlock(assetLocation);
			if (block != null)
			{
				result = new ItemStack(block, quantity);
			}
			else
			{
				world.Logger.Warning("BlockLootVessel: Failed resolving block code {0}", assetLocation);
			}
		}
		else
		{
			Item item = world.GetItem(assetLocation);
			if (item != null)
			{
				result = new ItemStack(item, quantity);
			}
			else
			{
				world.Logger.Warning("BlockLootVessel: Failed resolving item code {0}", assetLocation);
			}
		}
		return result;
	}

	public int GetDropQuantity(IWorldAccessor world, float dropQuantityMul)
	{
		float value = dropQuantityMul * (minQuantity + (float)world.Rand.NextDouble() * (maxQuantity - minQuantity));
		return GameMath.RoundRandom(world.Rand, value);
	}
}
