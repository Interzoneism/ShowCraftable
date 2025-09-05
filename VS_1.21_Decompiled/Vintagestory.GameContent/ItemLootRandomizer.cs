using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemLootRandomizer : Item, IResolvableCollectible
{
	private Random rand;

	public override void OnLoaded(ICoreAPI api)
	{
		rand = new Random();
		base.OnLoaded(api);
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if ((byEntity as EntityPlayer).Player != null)
		{
			TreeAttribute treeAttribute = new TreeAttribute();
			treeAttribute.SetString("inventoryId", slot.Inventory.InventoryID);
			treeAttribute.SetInt("slotId", slot.Inventory.GetSlotId(slot));
			api.Event.PushEvent("OpenLootRandomizerDialog", treeAttribute);
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		int num = 0;
		foreach (KeyValuePair<string, IAttribute> attribute in inSlot.Itemstack.Attributes)
		{
			if (attribute.Key.StartsWithOrdinal("stack") && attribute.Value is TreeAttribute)
			{
				TreeAttribute treeAttribute = attribute.Value as TreeAttribute;
				if (num == 0)
				{
					dsc.AppendLine("Contents: ");
				}
				ItemStack itemstack = treeAttribute.GetItemstack("stack");
				itemstack.ResolveBlockOrItem(world);
				dsc.AppendLine(itemstack.StackSize + "x " + itemstack.GetName() + ": " + treeAttribute.GetFloat("chance") + "%");
				num++;
			}
		}
	}

	public void Resolve(ItemSlot slot, IWorldAccessor worldForResolve, bool resolveImports)
	{
		if (!resolveImports)
		{
			return;
		}
		double num = rand.NextDouble();
		ItemStack itemstack = slot.Itemstack;
		slot.Itemstack = null;
		IAttribute[] values = itemstack.Attributes.Values;
		values.Shuffle(rand);
		IAttribute[] array = values;
		foreach (IAttribute attribute in array)
		{
			if (!(attribute is TreeAttribute))
			{
				continue;
			}
			TreeAttribute treeAttribute = attribute as TreeAttribute;
			float num2 = treeAttribute.GetFloat("chance") / 100f;
			if ((double)num2 > num)
			{
				ItemStack itemStack = treeAttribute.GetItemstack("stack")?.Clone();
				if (itemStack?.Collectible != null)
				{
					itemStack.ResolveBlockOrItem(worldForResolve);
					slot.Itemstack = itemStack;
				}
				else
				{
					slot.Itemstack = null;
				}
				break;
			}
			num -= (double)num2;
		}
	}

	public BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		IAttribute[] values = handbookStack.Attributes.Values;
		List<BlockDropItemStack> list = new List<BlockDropItemStack>();
		IAttribute[] array = values;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is TreeAttribute treeAttribute)
			{
				ItemStack itemStack = treeAttribute.GetItemstack("stack")?.Clone();
				if (itemStack?.Collectible != null)
				{
					itemStack.ResolveBlockOrItem(forPlayer.Entity.World);
					list.Add(new BlockDropItemStack(itemStack));
				}
			}
		}
		return list.ToArray();
	}

	public override void OnStoreCollectibleMappings(IWorldAccessor world, ItemSlot inSlot, Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		base.OnStoreCollectibleMappings(world, inSlot, blockIdMapping, itemIdMapping);
		foreach (KeyValuePair<string, IAttribute> attribute in inSlot.Itemstack.Attributes)
		{
			if (attribute.Key.StartsWithOrdinal("stack") && attribute.Value is TreeAttribute)
			{
				ItemStack itemstack = (attribute.Value as TreeAttribute).GetItemstack("stack");
				itemstack.ResolveBlockOrItem(world);
				if (itemstack.Class == EnumItemClass.Block)
				{
					blockIdMapping[itemstack.Id] = itemstack.Collectible.Code;
				}
				else
				{
					itemIdMapping[itemstack.Id] = itemstack.Collectible.Code;
				}
			}
		}
	}
}
