using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonConverter(typeof(DialogueComponentJsonConverter))]
public abstract class DialogueComponent
{
	public string Code;

	public string Owner;

	public string Sound;

	public string Type;

	public Dictionary<string, string> SetVariables;

	public string JumpTo;

	public string Trigger;

	public JsonObject TriggerData;

	protected DialogueController controller;

	protected GuiDialogueDialog dialog;

	public virtual void SetReferences(DialogueController controller, GuiDialogueDialog dialogue)
	{
		this.controller = controller;
		dialog = dialogue;
	}

	public abstract string Execute();

	protected void setVars()
	{
		if (Trigger != null)
		{
			controller.Trigger(controller.PlayerEntity, Trigger, TriggerData);
		}
		if (Sound != null)
		{
			controller.PlayerEntity.Api.World.PlaySoundAt(new AssetLocation(Sound).WithPathPrefixOnce("sounds/"), controller.NPCEntity, controller.PlayerEntity?.Player, randomizePitch: false, 16f);
		}
		if (SetVariables == null)
		{
			return;
		}
		foreach (KeyValuePair<string, string> setVariable in SetVariables)
		{
			string[] array = setVariable.Key.Split('.');
			EnumActivityVariableScope enumActivityVariableScope = scopeFromString(array[0]);
			controller.VarSys.SetVariable((enumActivityVariableScope == EnumActivityVariableScope.Player) ? controller.PlayerEntity : controller.NPCEntity, enumActivityVariableScope, array[1], setVariable.Value);
		}
	}

	protected bool IsConditionMet(string variable, string isValue, bool invertCheck)
	{
		switch (variable)
		{
		case "player.inventory":
		{
			JsonItemStack jsonItemStack2 = JsonItemStack.FromString(isValue);
			if (!jsonItemStack2.Resolve(controller.NPCEntity.World, Code + "dialogue talk component quest item"))
			{
				return false;
			}
			ItemStack resolvedItemstack2 = jsonItemStack2.ResolvedItemstack;
			ItemSlot itemSlot = FindDesiredItem(controller.PlayerEntity, resolvedItemstack2);
			if (!invertCheck)
			{
				return itemSlot != null;
			}
			return itemSlot == null;
		}
		case "player.inventorywildcard":
		{
			ItemSlot itemSlot2 = FindDesiredItem(controller.PlayerEntity, isValue);
			if (!invertCheck)
			{
				return itemSlot2 != null;
			}
			return itemSlot2 == null;
		}
		case "player.heldstack":
		{
			if (isValue == "damagedtool")
			{
				ItemSlot rightHandItemSlot = controller.PlayerEntity.RightHandItemSlot;
				if (rightHandItemSlot.Empty)
				{
					return false;
				}
				int remainingDurability = rightHandItemSlot.Itemstack.Collectible.GetRemainingDurability(rightHandItemSlot.Itemstack);
				int maxDurability = rightHandItemSlot.Itemstack.Collectible.GetMaxDurability(rightHandItemSlot.Itemstack);
				if (rightHandItemSlot.Itemstack.Collectible.Tool.HasValue)
				{
					return remainingDurability < maxDurability;
				}
				return false;
			}
			if (isValue == "damagedarmor")
			{
				ItemSlot rightHandItemSlot2 = controller.PlayerEntity.RightHandItemSlot;
				if (rightHandItemSlot2.Empty)
				{
					return false;
				}
				int remainingDurability2 = rightHandItemSlot2.Itemstack.Collectible.GetRemainingDurability(rightHandItemSlot2.Itemstack);
				int maxDurability2 = rightHandItemSlot2.Itemstack.Collectible.GetMaxDurability(rightHandItemSlot2.Itemstack);
				if (rightHandItemSlot2.Itemstack.Collectible.FirstCodePart() == "armor")
				{
					return remainingDurability2 < maxDurability2;
				}
				return false;
			}
			JsonItemStack jsonItemStack = JsonItemStack.FromString(isValue);
			if (!jsonItemStack.Resolve(controller.NPCEntity.World, Code + "dialogue talk component quest item"))
			{
				return false;
			}
			ItemStack resolvedItemstack = jsonItemStack.ResolvedItemstack;
			ItemSlot rightHandItemSlot3 = controller.PlayerEntity.RightHandItemSlot;
			if (matches(controller.PlayerEntity, resolvedItemstack, rightHandItemSlot3, getIgnoreAttrs()))
			{
				return true;
			}
			break;
		}
		}
		string[] array = variable.Split(new char[1] { '.' }, 2);
		EnumActivityVariableScope enumActivityVariableScope = scopeFromString(array[0]);
		string variable2 = controller.VarSys.GetVariable(enumActivityVariableScope, array[1], (enumActivityVariableScope == EnumActivityVariableScope.Player) ? controller.PlayerEntity : controller.NPCEntity);
		if (!invertCheck)
		{
			return variable2 == isValue;
		}
		return variable2 != isValue;
	}

	public static ItemSlot FindDesiredItem(EntityAgent eagent, ItemStack wantStack)
	{
		ItemSlot foundSlot = null;
		string[] ignoredAttrs = getIgnoreAttrs();
		eagent.WalkInventory(delegate(ItemSlot slot)
		{
			if (slot.Empty)
			{
				return true;
			}
			if (matches(eagent, wantStack, slot, ignoredAttrs))
			{
				foundSlot = slot;
				return false;
			}
			return true;
		});
		return foundSlot;
	}

	public static ItemSlot FindDesiredItem(EntityAgent eagent, AssetLocation wildcardcode)
	{
		ItemSlot foundSlot = null;
		eagent.WalkInventory(delegate(ItemSlot slot)
		{
			if (slot.Empty)
			{
				return true;
			}
			if (WildcardUtil.Match(wildcardcode, slot.Itemstack.Collectible.Code))
			{
				foundSlot = slot;
				return false;
			}
			return true;
		});
		return foundSlot;
	}

	private static string[] getIgnoreAttrs()
	{
		return GlobalConstants.IgnoredStackAttributes.Append("backpack").Append("condition").Append("durability")
			.Append("randomX")
			.Append("randomZ");
	}

	private static bool matches(EntityAgent eagent, ItemStack wantStack, ItemSlot slot, string[] ignoredAttrs)
	{
		ItemStack itemstack = slot.Itemstack;
		if ((wantStack.Equals(eagent.World, itemstack, ignoredAttrs) || itemstack.Satisfies(wantStack)) && itemstack.Collectible.IsReasonablyFresh(eagent.World, itemstack) && itemstack.StackSize >= wantStack.StackSize)
		{
			return true;
		}
		return false;
	}

	private static EnumActivityVariableScope scopeFromString(string name)
	{
		EnumActivityVariableScope result = EnumActivityVariableScope.Global;
		if (name == "global")
		{
			result = EnumActivityVariableScope.Global;
		}
		if (name == "player")
		{
			result = EnumActivityVariableScope.Player;
		}
		if (name == "entity")
		{
			result = EnumActivityVariableScope.Entity;
		}
		if (name == "group")
		{
			result = EnumActivityVariableScope.Group;
		}
		return result;
	}

	public virtual void Init(ref int uniqueIdCounter)
	{
	}
}
