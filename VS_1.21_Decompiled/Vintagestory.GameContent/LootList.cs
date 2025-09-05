using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class LootList
{
	public float Tries;

	public List<LootItem> lootItems = new List<LootItem>();

	public float TotalChance;

	public ItemStack[] GenerateLoot(IWorldAccessor world, IPlayer forPlayer)
	{
		List<ItemStack> list = new List<ItemStack>();
		int variant = world.Rand.Next();
		float num = Tries;
		float dropQuantityMul = forPlayer?.Entity.Stats.GetBlended("vesselContentsDropRate") ?? 1f;
		while (num >= 1f || (double)num > world.Rand.NextDouble())
		{
			lootItems.Shuffle(world.Rand);
			double num2 = world.Rand.NextDouble() * (double)TotalChance;
			foreach (LootItem lootItem in lootItems)
			{
				num2 -= (double)lootItem.chance;
				if (num2 <= 0.0)
				{
					int dropQuantity = lootItem.GetDropQuantity(world, dropQuantityMul);
					ItemStack itemStack = lootItem.GetItemStack(world, variant, dropQuantity);
					if (itemStack != null)
					{
						list.Add(itemStack);
					}
					break;
				}
			}
			num -= 1f;
		}
		return list.ToArray();
	}

	public static LootList Create(float tries, params LootItem[] lootItems)
	{
		LootList lootList = new LootList();
		lootList.Tries = tries;
		lootList.lootItems.AddRange(lootItems);
		for (int i = 0; i < lootItems.Length; i++)
		{
			lootList.TotalChance += lootItems[i].chance;
		}
		return lootList;
	}
}
