using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Common;

public class CreativeTab
{
	public IInventory Inventory { get; set; }

	public string Code { get; set; }

	public Dictionary<int, string> SearchCache { get; set; }

	public Dictionary<int, string> SearchCacheNames { get; set; }

	public int Index { get; set; }

	public CreativeTab(string code, IInventory inventory)
	{
		Code = code;
		Inventory = inventory;
	}

	public Dictionary<int, string> CreateSearchCache(IWorldAccessor world)
	{
		Dictionary<int, string> dictionary = new Dictionary<int, string>();
		Dictionary<int, string> dictionary2 = new Dictionary<int, string>();
		for (int i = 0; i < Inventory.Count; i++)
		{
			if (((ClientCoreAPI)world.Api).disposed)
			{
				break;
			}
			ItemSlot itemSlot = Inventory[i];
			ItemStack itemstack = itemSlot.Itemstack;
			if (itemstack != null)
			{
				string name = itemstack.GetName();
				dictionary2[i] = name.ToSearchFriendly().ToLowerInvariant();
				dictionary[i] = name + " " + ((itemstack.Collectible as ISearchTextProvider)?.GetSearchText(world, itemSlot) ?? itemstack.GetDescription(world, itemSlot).ToSearchFriendly().ToLowerInvariant());
			}
		}
		SearchCacheNames = dictionary2;
		SearchCache = dictionary;
		return SearchCache;
	}
}
