using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemSlotTrough : ItemSlotSurvival
{
	private BlockEntityTrough be;

	public ItemSlotTrough(BlockEntityTrough be, InventoryGeneric inventory)
		: base(inventory)
	{
		this.be = be;
	}

	public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
	{
		if (base.CanTakeFrom(sourceSlot, priority))
		{
			return troughable(sourceSlot);
		}
		return false;
	}

	public override bool CanHold(ItemSlot itemstackFromSourceSlot)
	{
		if (base.CanHold(itemstackFromSourceSlot))
		{
			return troughable(itemstackFromSourceSlot);
		}
		return false;
	}

	public bool troughable(ItemSlot sourceSlot)
	{
		if (!Empty && !sourceSlot.Itemstack.Equals(be.Api.World, itemstack, GlobalConstants.IgnoredStackAttributes))
		{
			return false;
		}
		ContentConfig[] contentConfigs = be.contentConfigs;
		ContentConfig contentConfig = getContentConfig(be.Api.World, contentConfigs, sourceSlot);
		if (contentConfig != null)
		{
			return contentConfig.MaxFillLevels * contentConfig.QuantityPerFillLevel > base.StackSize;
		}
		return false;
	}

	public static ContentConfig getContentConfig(IWorldAccessor world, ContentConfig[] contentConfigs, ItemSlot sourceSlot)
	{
		if (sourceSlot.Empty)
		{
			return null;
		}
		foreach (ContentConfig contentConfig in contentConfigs)
		{
			if (contentConfig.Content.Code.Path.Contains('*'))
			{
				if (WildcardUtil.Match(contentConfig.Content.Code, sourceSlot.Itemstack.Collectible.Code))
				{
					return contentConfig;
				}
			}
			else if (sourceSlot.Itemstack.Equals(world, contentConfig.Content.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes))
			{
				return contentConfig;
			}
		}
		return null;
	}
}
