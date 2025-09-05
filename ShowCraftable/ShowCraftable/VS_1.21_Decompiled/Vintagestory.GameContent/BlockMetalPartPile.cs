using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockMetalPartPile : Block
{
	private class DropChanceEntry
	{
		public double ScrapChance;

		public Dictionary<int, double> ScrapQuantityChances = new Dictionary<int, double>();

		public Dictionary<int, double> PartQuantityChances = new Dictionary<int, double>();
	}

	private Dictionary<string, DropChanceEntry> dropChances;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		dropChances = new Dictionary<string, DropChanceEntry>
		{
			{
				"tiny",
				new DropChanceEntry
				{
					ScrapChance = 0.8,
					ScrapQuantityChances = new Dictionary<int, double>
					{
						{ 1, 0.9 },
						{ 2, 0.1 }
					},
					PartQuantityChances = new Dictionary<int, double>
					{
						{ 1, 0.9 },
						{ 2, 0.1 }
					}
				}
			},
			{
				"small",
				new DropChanceEntry
				{
					ScrapChance = 0.6,
					ScrapQuantityChances = new Dictionary<int, double>
					{
						{ 1, 0.8 },
						{ 2, 0.2 }
					},
					PartQuantityChances = new Dictionary<int, double>
					{
						{ 1, 0.1 },
						{ 2, 0.8 },
						{ 3, 0.1 }
					}
				}
			},
			{
				"medium",
				new DropChanceEntry
				{
					ScrapChance = 0.4,
					ScrapQuantityChances = new Dictionary<int, double>
					{
						{ 2, 0.8 },
						{ 3, 0.2 }
					},
					PartQuantityChances = new Dictionary<int, double>
					{
						{ 2, 0.1 },
						{ 3, 0.8 },
						{ 4, 0.1 }
					}
				}
			},
			{
				"large",
				new DropChanceEntry
				{
					ScrapChance = 0.2,
					ScrapQuantityChances = new Dictionary<int, double>
					{
						{ 2, 0.2 },
						{ 3, 0.8 }
					},
					PartQuantityChances = new Dictionary<int, double>
					{
						{ 3, 0.1 },
						{ 4, 0.8 },
						{ 5, 0.1 }
					}
				}
			}
		};
	}

	public string Size()
	{
		return Variant["size"];
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Random rand = world.Rand;
		DropChanceEntry dropChanceEntry = dropChances[Size()];
		int num = ((!(rand.NextDouble() < dropChanceEntry.ScrapChance)) ? 1 : 0);
		float num2 = (byPlayer?.Entity.Stats.GetBlended("rustyGearDropRate") ?? 0f) - 1f;
		if (num2 > 0f && rand.NextDouble() < (double)num2)
		{
			num = 2;
		}
		ItemStack nextItemStack = Drops[num].GetNextItemStack(dropQuantityMultiplier);
		if (nextItemStack == null)
		{
			return Array.Empty<ItemStack>();
		}
		double num3 = rand.NextDouble();
		foreach (KeyValuePair<int, double> item in (num == 0) ? dropChanceEntry.ScrapQuantityChances : dropChanceEntry.PartQuantityChances)
		{
			num3 -= item.Value;
			if (num3 <= 0.0)
			{
				nextItemStack.StackSize = item.Key;
				break;
			}
		}
		return new ItemStack[1] { nextItemStack };
	}
}
