using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockContainer : Block
{
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
		if (stacks == null || stacks.Length == 0 || stacks.All((ItemStack x) => x == null))
		{
			containerStack.Attributes.RemoveAttribute("contents");
			return;
		}
		TreeAttribute treeAttribute = new TreeAttribute();
		for (int num = 0; num < stacks.Length; num++)
		{
			treeAttribute[num.ToString() ?? ""] = new ItemstackAttribute(stacks[num]);
		}
		containerStack.Attributes["contents"] = treeAttribute;
	}

	public virtual ItemStack[] GetContents(IWorldAccessor world, ItemStack itemstack)
	{
		ITreeAttribute treeAttribute = itemstack?.Attributes?.GetTreeAttribute("contents");
		if (treeAttribute == null)
		{
			return ResolveUcontents(world, itemstack);
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

	public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
	{
		ResolveUcontents(api.World, thisStack);
		if (otherStack.Collectible is BlockContainer)
		{
			ResolveUcontents(api.World, otherStack);
		}
		return base.Equals(thisStack, otherStack, ignoreAttributeSubTrees);
	}

	protected ItemStack[] ResolveUcontents(IWorldAccessor world, ItemStack itemstack)
	{
		if (itemstack != null && itemstack.Attributes.HasAttribute("ucontents"))
		{
			List<ItemStack> list = new List<ItemStack>();
			TreeAttribute[] value = (itemstack.Attributes["ucontents"] as TreeArrayAttribute).value;
			foreach (ITreeAttribute stackAttr in value)
			{
				list.Add(CreateItemStackFromJson(stackAttr, world, Code.Domain));
			}
			ItemStack[] array = list.ToArray();
			SetContents(itemstack, array);
			itemstack.Attributes.RemoveAttribute("ucontents");
			return array;
		}
		return Array.Empty<ItemStack>();
	}

	public virtual ItemStack CreateItemStackFromJson(ITreeAttribute stackAttr, IWorldAccessor world, string defaultDomain)
	{
		AssetLocation assetLocation = AssetLocation.Create(stackAttr.GetString("code"), defaultDomain);
		CollectibleObject collectible = ((!(stackAttr.GetString("type") == "item")) ? ((CollectibleObject)world.GetBlock(assetLocation)) : ((CollectibleObject)world.GetItem(assetLocation)));
		ItemStack itemStack = new ItemStack(collectible, (int)stackAttr.GetDecimal("quantity", 1.0));
		ITreeAttribute treeAttribute = (stackAttr["attributes"] as TreeAttribute)?.Clone();
		if (treeAttribute != null)
		{
			itemStack.Attributes = treeAttribute;
		}
		return itemStack;
	}

	public bool IsEmpty(ItemStack itemstack)
	{
		ITreeAttribute treeAttribute = itemstack?.Attributes?.GetTreeAttribute("contents");
		if (treeAttribute == null)
		{
			return true;
		}
		foreach (KeyValuePair<string, IAttribute> item in treeAttribute)
		{
			if ((item.Value as ItemstackAttribute).value != null)
			{
				return false;
			}
		}
		return true;
	}

	public virtual ItemStack[] GetNonEmptyContents(IWorldAccessor world, ItemStack itemstack)
	{
		return GetContents(world, itemstack)?.Where((ItemStack stack) => stack != null).ToArray();
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = base.OnPickBlock(world, pos);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer blockEntityContainer)
		{
			SetContents(itemStack, blockEntityContainer.GetContentStacks());
		}
		return itemStack;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		bool flag = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			obj.OnBlockBroken(world, pos, byPlayer, ref handling);
			if (handling == EnumHandling.PreventDefault)
			{
				flag = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (flag)
		{
			return;
		}
		if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
		{
			ItemStack[] array = new ItemStack[1] { OnPickBlock(world, pos) };
			for (int j = 0; j < array.Length; j++)
			{
				world.SpawnItemEntity(array[j], pos);
			}
			world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos, -0.5, byPlayer);
		}
		if (EntityClass != null)
		{
			world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken(byPlayer);
		}
		world.BlockAccessor.SetBlock(0, pos);
	}

	public override TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
	{
		if (inslot is ItemSlotCreative)
		{
			return base.UpdateAndGetTransitionStates(world, inslot);
		}
		ItemStack[] contents = GetContents(world, inslot.Itemstack);
		if (inslot.Itemstack.Attributes.GetBool("timeFrozen"))
		{
			ItemStack[] array = contents;
			for (int i = 0; i < array.Length; i++)
			{
				array[i]?.Attributes.SetBool("timeFrozen", value: true);
			}
			return null;
		}
		if (contents != null)
		{
			for (int j = 0; j < contents.Length; j++)
			{
				ItemStack itemStack = contents[j];
				if (itemStack != null)
				{
					ItemSlot contentInDummySlot = GetContentInDummySlot(inslot, itemStack);
					itemStack.Collectible.UpdateAndGetTransitionStates(world, contentInDummySlot);
					if (contentInDummySlot.Itemstack == null)
					{
						contents[j] = null;
					}
				}
			}
		}
		SetContents(inslot.Itemstack, contents);
		return base.UpdateAndGetTransitionStates(world, inslot);
	}

	protected virtual ItemSlot GetContentInDummySlot(ItemSlot inslot, ItemStack itemstack)
	{
		DummyInventory dummyInventory = new DummyInventory(api);
		DummySlot dummySlot = new DummySlot(itemstack, dummyInventory);
		dummySlot.MarkedDirty += delegate
		{
			inslot.Inventory?.DidModifyItemSlot(inslot);
			return true;
		};
		dummyInventory.OnAcquireTransitionSpeed += delegate(EnumTransitionType transType, ItemStack stack, float mulByConfig)
		{
			float num = mulByConfig;
			if (inslot.Inventory != null)
			{
				num = inslot.Inventory.InvokeTransitionSpeedDelegates(transType, stack, mulByConfig);
			}
			return num * GetContainingTransitionModifierContained(api.World, inslot, transType);
		};
		return dummySlot;
	}

	public override void SetTemperature(IWorldAccessor world, ItemStack itemstack, float temperature, bool delayCooldown = true)
	{
		ItemStack[] contents = GetContents(world, itemstack);
		if (contents != null)
		{
			for (int i = 0; i < contents.Length; i++)
			{
				contents[i]?.Collectible.SetTemperature(world, contents[i], temperature, delayCooldown);
			}
		}
		base.SetTemperature(world, itemstack, temperature, delayCooldown);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}

	public string PerishableInfoCompactContainer(ICoreAPI api, ItemSlot inSlot)
	{
		IWorldAccessor world = api.World;
		ItemStack[] nonEmptyContents = GetNonEmptyContents(world, inSlot.Itemstack);
		DummyInventory dummyInventory = new DummyInventory(api);
		ItemSlot dummySlotForFirstPerishableStack = BlockCrock.GetDummySlotForFirstPerishableStack(api.World, nonEmptyContents, null, dummyInventory);
		dummyInventory.OnAcquireTransitionSpeed += delegate(EnumTransitionType transType, ItemStack stack, float mul)
		{
			float num = mul * GetContainingTransitionModifierContained(world, inSlot, transType);
			if (inSlot.Inventory != null)
			{
				num *= inSlot.Inventory.GetTransitionSpeedMul(transType, inSlot.Itemstack);
			}
			return num;
		};
		return BlockEntityShelf.PerishableInfoCompact(api, dummySlotForFirstPerishableStack, 0f, withStackName: false).Replace("\r\n", "");
	}

	public override bool RequiresTransitionableTicking(IWorldAccessor world, ItemStack itemstack)
	{
		ItemStack[] nonEmptyContents = GetNonEmptyContents(world, itemstack);
		for (int i = 0; i < nonEmptyContents.Length; i++)
		{
			TransitionableProperties[] transitionableProperties = nonEmptyContents[i].Collectible.GetTransitionableProperties(world, nonEmptyContents[i], null);
			if (transitionableProperties != null && transitionableProperties.Length != 0)
			{
				return true;
			}
		}
		return base.RequiresTransitionableTicking(world, itemstack);
	}
}
