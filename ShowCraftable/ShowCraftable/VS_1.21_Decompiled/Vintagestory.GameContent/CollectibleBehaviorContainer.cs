using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class CollectibleBehaviorContainer : CollectibleBehavior
{
	public CollectibleBehaviorContainer(CollectibleObject collObj)
		: base(collObj)
	{
	}

	public virtual float GetContainingTransitionModifierContained(IWorldAccessor world, ItemSlot inSlot, EnumTransitionType transType)
	{
		return 1f;
	}

	public virtual float GetContainingTransitionModifierPlaced(IWorldAccessor world, BlockPos pos, EnumTransitionType transType)
	{
		return 1f;
	}

	public virtual void SetContents(ItemStack containerStack, ItemStack[] stacks)
	{
		TreeAttribute treeAttribute = new TreeAttribute();
		for (int i = 0; i < stacks.Length; i++)
		{
			treeAttribute[i.ToString() ?? ""] = new ItemstackAttribute(stacks[i]);
		}
		containerStack.Attributes["contents"] = treeAttribute;
	}

	public virtual ItemStack[] GetContents(IWorldAccessor world, ItemStack itemstack)
	{
		ITreeAttribute treeAttribute = itemstack.Attributes.GetTreeAttribute("contents");
		if (treeAttribute == null)
		{
			return Array.Empty<ItemStack>();
		}
		ItemStack[] array = new ItemStack[treeAttribute.Count];
		foreach (KeyValuePair<string, IAttribute> item in treeAttribute)
		{
			ItemStack value = (item.Value as ItemstackAttribute).value;
			value?.ResolveBlockOrItem(world);
			if (int.TryParse(item.Key, out var result))
			{
				array[result] = value;
			}
		}
		return array;
	}

	public virtual ItemStack[] GetNonEmptyContents(IWorldAccessor world, ItemStack itemstack)
	{
		return (from stack in GetContents(world, itemstack)
			where stack != null
			select stack).ToArray();
	}
}
