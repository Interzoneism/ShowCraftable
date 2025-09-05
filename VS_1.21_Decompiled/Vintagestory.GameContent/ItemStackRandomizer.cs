using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemStackRandomizer : Item, IResolvableCollectible
{
	private RandomStack[] Stacks;

	private Random rand;

	public override void OnLoaded(ICoreAPI api)
	{
		rand = new Random();
		Stacks = Attributes["stacks"].AsObject<RandomStack[]>();
		float num = 0f;
		for (int i = 0; i < Stacks.Length; i++)
		{
			num += Stacks[i].Chance;
			Stacks[i].Resolve(api.World);
		}
		float num2 = 1f / num;
		for (int j = 0; j < Stacks.Length; j++)
		{
			Stacks[j].Chance *= num2;
		}
		base.OnLoaded(api);
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		IPlayer player = null;
		if (byEntity is EntityPlayer)
		{
			player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (player != null)
		{
			TreeAttribute treeAttribute = new TreeAttribute();
			treeAttribute.SetFloat("totalChance", slot.Itemstack.Attributes.GetFloat("totalChance", 1f));
			treeAttribute.SetString("inventoryId", slot.Inventory.InventoryID);
			treeAttribute.SetInt("slotId", slot.Inventory.GetSlotId(slot));
			api.Event.PushEvent("OpenStackRandomizerDialog", treeAttribute);
		}
	}

	public void Resolve(ItemSlot intoslot, IWorldAccessor worldForResolve, bool resolveImports = true)
	{
		if (!resolveImports)
		{
			return;
		}
		double num = rand.NextDouble();
		if ((double)intoslot.Itemstack.Attributes.GetFloat("totalChance", 1f) < rand.NextDouble())
		{
			intoslot.Itemstack = null;
			return;
		}
		intoslot.Itemstack = null;
		if (Stacks == null)
		{
			worldForResolve.Logger.Warning("ItemStackRandomizer 'Stacks' was null! Won't resolve into something.");
			return;
		}
		Stacks.Shuffle(rand);
		for (int i = 0; i < Stacks.Length; i++)
		{
			if ((double)Stacks[i].Chance > num)
			{
				if (Stacks[i].ResolvedStack != null)
				{
					intoslot.Itemstack = Stacks[i].ResolvedStack.Clone();
					intoslot.Itemstack.StackSize = (int)Stacks[i].Quantity.nextFloat(1f, rand);
					break;
				}
			}
			else
			{
				num -= (double)Stacks[i].Chance;
			}
		}
	}

	public BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		List<BlockDropItemStack> list = new List<BlockDropItemStack>();
		RandomStack[] stacks = Stacks;
		foreach (RandomStack randomStack in stacks)
		{
			list.Add(new BlockDropItemStack(randomStack.ResolvedStack.Clone()));
		}
		return list.ToArray();
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		float num = inSlot.Itemstack.Attributes.GetFloat("totalChance", 1f);
		dsc.Append("<font size=\"12\">");
		dsc.AppendLine(Lang.Get("With a {0}% chance, will generate one of the following:", (num * 100f).ToString("0.#")));
		IEnumerable<RandomStack> enumerable = (from stack in Stacks
			where stack.ResolvedStack != null
			orderby stack.Chance
			select stack).Reverse();
		int num2 = 0;
		foreach (RandomStack item in enumerable)
		{
			if (item.Quantity.var == 0f)
			{
				dsc.AppendLine(Lang.Get("{0}%\t {1}x {2}", (item.Chance * 100f).ToString("0.#"), item.Quantity.avg, item.ResolvedStack.GetName()));
			}
			else
			{
				dsc.AppendLine(Lang.Get("{0}%\t {1}-{2}x {3}", (item.Chance * 100f).ToString("0.#"), item.Quantity.avg - item.Quantity.var, item.Quantity.avg + item.Quantity.var, item.ResolvedStack.GetName()));
			}
			if (num2++ > 50)
			{
				dsc.AppendLine(Lang.Get("{0} more items. Check itemtype json file for full list.", enumerable.ToList().Count - num2));
				break;
			}
		}
		dsc.Append("</font>");
	}
}
