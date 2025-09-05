using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemWearable : ItemWearableAttachment
{
	public StatModifiers StatModifers;

	public ProtectionModifiers ProtectionModifiers;

	public AssetLocation[] FootStepSounds;

	public EnumCharacterDressType DressType { get; private set; }

	public bool IsArmor
	{
		get
		{
			if (DressType != EnumCharacterDressType.ArmorBody && DressType != EnumCharacterDressType.ArmorHead)
			{
				return DressType == EnumCharacterDressType.ArmorLegs;
			}
			return true;
		}
	}

	public override string GetMeshCacheKey(ItemStack itemstack)
	{
		return "wearableModelRef-" + itemstack.Collectible.Code.ToString();
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Enum.TryParse<EnumCharacterDressType>(Attributes["clothescategory"].AsString(), ignoreCase: true, out var result);
		DressType = result;
		JsonObject jsonObject = Attributes?["footStepSound"];
		if (jsonObject != null && jsonObject.Exists)
		{
			string text = jsonObject.AsString();
			if (text != null)
			{
				AssetLocation assetLocation = AssetLocation.Create(text, Code.Domain).WithPathPrefixOnce("sounds/");
				if (text.EndsWith('*'))
				{
					assetLocation.Path = assetLocation.Path.TrimEnd('*');
					FootStepSounds = api.Assets.GetLocations(assetLocation.Path, assetLocation.Domain).ToArray();
				}
				else
				{
					FootStepSounds = new AssetLocation[1] { assetLocation };
				}
			}
		}
		jsonObject = Attributes?["statModifiers"];
		if (jsonObject != null && jsonObject.Exists)
		{
			try
			{
				StatModifers = jsonObject.AsObject<StatModifiers>();
			}
			catch (Exception e)
			{
				api.World.Logger.Error("Failed loading statModifiers for item/block {0}. Will ignore.", Code);
				api.World.Logger.Error(e);
				StatModifers = null;
			}
		}
		ProtectionModifiers protectionModifiers = null;
		jsonObject = Attributes?["defaultProtLoss"];
		if (jsonObject != null && jsonObject.Exists)
		{
			try
			{
				protectionModifiers = jsonObject.AsObject<ProtectionModifiers>();
			}
			catch (Exception e2)
			{
				api.World.Logger.Error("Failed loading defaultProtLoss for item/block {0}. Will ignore.", Code);
				api.World.Logger.Error(e2);
			}
		}
		jsonObject = Attributes?["protectionModifiers"];
		if (jsonObject != null && jsonObject.Exists)
		{
			try
			{
				ProtectionModifiers = jsonObject.AsObject<ProtectionModifiers>();
			}
			catch (Exception e3)
			{
				api.World.Logger.Error("Failed loading protectionModifiers for item/block {0}. Will ignore.", Code);
				api.World.Logger.Error(e3);
				ProtectionModifiers = null;
			}
		}
		if (ProtectionModifiers != null && ProtectionModifiers.PerTierFlatDamageReductionLoss == null)
		{
			ProtectionModifiers.PerTierFlatDamageReductionLoss = protectionModifiers?.PerTierFlatDamageReductionLoss;
		}
		if (ProtectionModifiers != null && ProtectionModifiers.PerTierRelativeProtectionLoss == null)
		{
			ProtectionModifiers.PerTierRelativeProtectionLoss = protectionModifiers?.PerTierRelativeProtectionLoss;
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		Dictionary<string, MultiTextureMeshRef> dictionary = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "armorMeshRefs");
		if (dictionary == null)
		{
			return;
		}
		foreach (MultiTextureMeshRef value in dictionary.Values)
		{
			value?.Dispose();
		}
		api.ObjectCache.Remove("armorMeshRefs");
	}

	public override void OnHandbookRecipeRender(ICoreClientAPI capi, GridRecipe recipe, ItemSlot dummyslot, double x, double y, double z, double size)
	{
		bool num = recipe.Name.Path.Contains("repair");
		int value = 0;
		if (num)
		{
			value = dummyslot.Itemstack.Collectible.GetRemainingDurability(dummyslot.Itemstack);
			dummyslot.Itemstack.Attributes.SetInt("durability", 0);
		}
		base.OnHandbookRecipeRender(capi, recipe, dummyslot, x, y, z, size);
		if (num)
		{
			dummyslot.Itemstack.Attributes.SetInt("durability", value);
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (byEntity.Controls.ShiftKey)
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		if (player != null)
		{
			IInventory ownInventory = player.InventoryManager.GetOwnInventory("character");
			if (ownInventory != null && DressType != EnumCharacterDressType.Unknown && ownInventory[(int)DressType].TryFlipWith(slot))
			{
				handHandling = EnumHandHandling.PreventDefault;
			}
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string text = "";
		string itemDescText = base.GetItemDescText();
		if (itemDescText.Length > 1)
		{
			int num = dsc.ToString().IndexOfOrdinal(itemDescText);
			if (num >= 0)
			{
				if (num > 0)
				{
					num--;
				}
				else
				{
					text = "\n";
				}
				text += dsc.ToString(num, dsc.Length - num);
				dsc.Remove(num, dsc.Length - num);
			}
		}
		if ((api as ICoreClientAPI).Settings.Bool["extendedDebugInfo"])
		{
			if (DressType == EnumCharacterDressType.Unknown)
			{
				dsc.AppendLine(Lang.Get("Cloth Category: Unknown"));
			}
			else
			{
				dsc.AppendLine(Lang.Get("Cloth Category: {0}", Lang.Get("clothcategory-" + inSlot.Itemstack.ItemAttributes["clothescategory"].AsString())));
			}
		}
		if (ProtectionModifiers != null)
		{
			if (ProtectionModifiers.FlatDamageReduction != 0f)
			{
				dsc.AppendLine(Lang.Get("Flat damage reduction: {0} hp", ProtectionModifiers.FlatDamageReduction));
			}
			if (ProtectionModifiers.RelativeProtection != 0f)
			{
				dsc.AppendLine(Lang.Get("Percent protection: {0}%", (int)(100f * ProtectionModifiers.RelativeProtection)));
			}
			dsc.AppendLine(Lang.Get("Protection tier: {0}", ProtectionModifiers.ProtectionTier));
		}
		if (StatModifers != null)
		{
			if (ProtectionModifiers != null)
			{
				dsc.AppendLine();
			}
			if (StatModifers.healingeffectivness != 0f)
			{
				dsc.AppendLine(Lang.Get("Healing effectivness: {0}%", (int)(100f * StatModifers.healingeffectivness)));
			}
			if (StatModifers.hungerrate != 0f)
			{
				dsc.AppendLine(Lang.Get("Hunger rate: {1}{0}%", (int)(100f * StatModifers.hungerrate), (StatModifers.hungerrate > 0f) ? "+" : ""));
			}
			if (StatModifers.rangedWeaponsAcc != 0f)
			{
				dsc.AppendLine(Lang.Get("Ranged Weapon Accuracy: {1}{0}%", (int)(100f * StatModifers.rangedWeaponsAcc), (StatModifers.rangedWeaponsAcc > 0f) ? "+" : ""));
			}
			if (StatModifers.rangedWeaponsSpeed != 0f)
			{
				dsc.AppendLine(Lang.Get("Ranged Weapon Charge Time: {1}{0}%", -(int)(100f * StatModifers.rangedWeaponsSpeed), (0f - StatModifers.rangedWeaponsSpeed > 0f) ? "+" : ""));
			}
			if (StatModifers.walkSpeed != 0f)
			{
				dsc.AppendLine(Lang.Get("Walk speed: {1}{0}%", (int)(100f * StatModifers.walkSpeed), (StatModifers.walkSpeed > 0f) ? "+" : ""));
			}
		}
		ProtectionModifiers protectionModifiers = ProtectionModifiers;
		if (protectionModifiers != null && protectionModifiers.HighDamageTierResistant)
		{
			dsc.AppendLine("<font color=\"#86aad0\">" + Lang.Get("High damage tier resistant") + "</font> " + Lang.Get("When damaged by a higher tier attack, the loss of protection is only half as much."));
		}
		if (Variant["category"] == "head")
		{
			float num2 = Attributes["rainProtectionPerc"].AsFloat();
			if (num2 > 0f)
			{
				dsc.AppendLine(Lang.Get("Protection from rain: {0}%", (int)(num2 * 100f)));
			}
		}
		JsonObject itemAttributes = inSlot.Itemstack.ItemAttributes;
		if (itemAttributes != null && itemAttributes["warmth"].Exists)
		{
			JsonObject itemAttributes2 = inSlot.Itemstack.ItemAttributes;
			if (itemAttributes2 == null || itemAttributes2["warmth"].AsFloat() != 0f)
			{
				if (!(inSlot is ItemSlotCreative))
				{
					ensureConditionExists(inSlot);
					float num3 = inSlot.Itemstack.Attributes.GetFloat("condition", 1f);
					string text2 = (((double)num3 > 0.5) ? Lang.Get("clothingcondition-good", (int)(num3 * 100f)) : (((double)num3 > 0.4) ? Lang.Get("clothingcondition-worn", (int)(num3 * 100f)) : (((double)num3 > 0.3) ? Lang.Get("clothingcondition-heavilyworn", (int)(num3 * 100f)) : (((double)num3 > 0.2) ? Lang.Get("clothingcondition-tattered", (int)(num3 * 100f)) : ((!((double)num3 > 0.1)) ? Lang.Get("clothingcondition-terrible", (int)(num3 * 100f)) : Lang.Get("clothingcondition-heavilytattered", (int)(num3 * 100f)))))));
					dsc.Append(Lang.Get("Condition:") + " ");
					float warmth = GetWarmth(inSlot);
					string text3 = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, num3 * 200f)]);
					if ((double)warmth < 0.05)
					{
						dsc.AppendLine("<font color=\"" + text3 + "\">" + text2 + "</font>, <font color=\"#ff8484\">" + Lang.Get("+{0:0.#}°C", warmth) + "</font>");
					}
					else
					{
						dsc.AppendLine("<font color=\"" + text3 + "\">" + text2 + "</font>, <font color=\"#84ff84\">" + Lang.Get("+{0:0.#}°C", warmth) + "</font>");
					}
				}
				float num4 = inSlot.Itemstack.ItemAttributes?["warmth"].AsFloat() ?? 0f;
				dsc.AppendLine();
				dsc.AppendLine(Lang.Get("clothing-maxwarmth", num4));
			}
		}
		dsc.Append(text);
	}

	public float GetWarmth(ItemSlot inslot)
	{
		ensureConditionExists(inslot);
		float num = inslot.Itemstack.ItemAttributes?["warmth"].AsFloat() ?? 0f;
		float num2 = inslot.Itemstack.Attributes.GetFloat("condition", 1f);
		return Math.Min(num, num2 * 2f * num);
	}

	public void ChangeCondition(ItemSlot slot, float changeVal)
	{
		if (changeVal != 0f)
		{
			ensureConditionExists(slot);
			slot.Itemstack.Attributes.SetFloat("condition", GameMath.Clamp(slot.Itemstack.Attributes.GetFloat("condition", 1f) + changeVal, 0f, 1f));
			slot.MarkDirty();
		}
	}

	public override bool RequiresTransitionableTicking(IWorldAccessor world, ItemStack itemstack)
	{
		return !itemstack.Attributes.HasAttribute("condition");
	}

	private void ensureConditionExists(ItemSlot slot, bool markdirty = true)
	{
		if (slot is DummySlot || slot.Itemstack.Attributes.HasAttribute("condition") || api.Side != EnumAppSide.Server)
		{
			return;
		}
		JsonObject itemAttributes = slot.Itemstack.ItemAttributes;
		if (itemAttributes == null || !itemAttributes["warmth"].Exists)
		{
			return;
		}
		JsonObject itemAttributes2 = slot.Itemstack.ItemAttributes;
		if (itemAttributes2 == null || itemAttributes2["warmth"].AsFloat() != 0f)
		{
			if (slot is ItemSlotTrade)
			{
				slot.Itemstack.Attributes.SetFloat("condition", (float)api.World.Rand.NextDouble() * 0.25f + 0.75f);
			}
			else
			{
				slot.Itemstack.Attributes.SetFloat("condition", (float)api.World.Rand.NextDouble() * 0.4f);
			}
			if (markdirty)
			{
				slot.MarkDirty();
			}
		}
	}

	public override void OnCreatedByCrafting(ItemSlot[] inSlots, ItemSlot outputSlot, GridRecipe byRecipe)
	{
		base.OnCreatedByCrafting(inSlots, outputSlot, byRecipe);
		if (!(outputSlot is DummySlot))
		{
			ensureConditionExists(outputSlot);
			outputSlot.Itemstack.Attributes.SetFloat("condition", 1f);
			if (byRecipe.Name.Path.Contains("repair"))
			{
				CalculateRepairValue(inSlots, outputSlot, out var repairValue, out var _);
				int remainingDurability = outputSlot.Itemstack.Collectible.GetRemainingDurability(outputSlot.Itemstack);
				int maxDurability = GetMaxDurability(outputSlot.Itemstack);
				outputSlot.Itemstack.Attributes.SetInt("durability", Math.Min(maxDurability, (int)((float)remainingDurability + (float)maxDurability * repairValue)));
			}
		}
	}

	public override bool ConsumeCraftingIngredients(ItemSlot[] inSlots, ItemSlot outputSlot, GridRecipe recipe)
	{
		if (recipe.Name.Path.Contains("repair"))
		{
			CalculateRepairValue(inSlots, outputSlot, out var _, out var matCostPerMatType);
			foreach (ItemSlot itemSlot in inSlots)
			{
				if (!itemSlot.Empty)
				{
					if (itemSlot.Itemstack.Collectible == this)
					{
						itemSlot.Itemstack = null;
					}
					else
					{
						itemSlot.TakeOut(matCostPerMatType);
					}
				}
			}
			return true;
		}
		return false;
	}

	public void CalculateRepairValue(ItemSlot[] inSlots, ItemSlot outputSlot, out float repairValue, out int matCostPerMatType)
	{
		int origMatCount = GetOrigMatCount(inSlots, outputSlot);
		ItemSlot itemSlot = inSlots.FirstOrDefault((ItemSlot slot) => slot.Itemstack?.Collectible is ItemWearable);
		int remainingDurability = outputSlot.Itemstack.Collectible.GetRemainingDurability(itemSlot.Itemstack);
		int maxDurability = GetMaxDurability(outputSlot.Itemstack);
		float num = 2f / (float)origMatCount * (float)maxDurability;
		int val = (int)Math.Max(1.0, Math.Round((float)(maxDurability - remainingDurability) / num));
		int inputRepairCount = GetInputRepairCount(inSlots);
		int repairMatTypeCount = GetRepairMatTypeCount(inSlots);
		int num2 = Math.Min(val, inputRepairCount * repairMatTypeCount);
		matCostPerMatType = Math.Min(val, inputRepairCount);
		repairValue = (float)num2 / (float)origMatCount * 2f;
	}

	private int GetRepairMatTypeCount(ItemSlot[] slots)
	{
		List<ItemStack> list = new List<ItemStack>();
		foreach (ItemSlot itemSlot in slots)
		{
			if (itemSlot.Empty)
			{
				continue;
			}
			bool flag = false;
			if (itemSlot.Itemstack.Collectible is ItemWearable)
			{
				continue;
			}
			foreach (ItemStack item in list)
			{
				if (itemSlot.Itemstack.Satisfies(item))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list.Add(itemSlot.Itemstack);
			}
		}
		return list.Count;
	}

	public int GetInputRepairCount(ItemSlot[] inputSlots)
	{
		OrderedDictionary<int, int> orderedDictionary = new OrderedDictionary<int, int>();
		foreach (ItemSlot itemSlot in inputSlots)
		{
			if (!itemSlot.Empty && !(itemSlot.Itemstack.Collectible is ItemWearable))
			{
				int hashCode = itemSlot.Itemstack.GetHashCode();
				orderedDictionary.TryGetValue(hashCode, out var value);
				orderedDictionary[hashCode] = value + itemSlot.StackSize;
			}
		}
		return orderedDictionary.Values.Min();
	}

	public int GetOrigMatCount(ItemSlot[] inputSlots, ItemSlot outputSlot)
	{
		ItemStack itemstack = outputSlot.Itemstack;
		_ = inputSlots.FirstOrDefault((ItemSlot slot) => !slot.Empty && slot.Itemstack.Collectible != this).Itemstack;
		int num = 0;
		foreach (GridRecipe gridRecipe in api.World.GridRecipes)
		{
			ItemStack resolvedItemstack = gridRecipe.Output.ResolvedItemstack;
			if (resolvedItemstack == null || !resolvedItemstack.Satisfies(itemstack) || gridRecipe.Name.Path.Contains("repair"))
			{
				continue;
			}
			GridRecipeIngredient[] resolvedIngredients = gridRecipe.resolvedIngredients;
			foreach (GridRecipeIngredient gridRecipeIngredient in resolvedIngredients)
			{
				if (gridRecipeIngredient == null)
				{
					continue;
				}
				JsonObject recipeAttributes = gridRecipeIngredient.RecipeAttributes;
				if (recipeAttributes != null && recipeAttributes["repairMat"].Exists)
				{
					JsonItemStack jsonItemStack = gridRecipeIngredient.RecipeAttributes["repairMat"].AsObject<JsonItemStack>();
					jsonItemStack.Resolve(api.World, $"recipe '{gridRecipe.Name}' repair mat");
					if (jsonItemStack.ResolvedItemstack != null)
					{
						num += jsonItemStack.ResolvedItemstack.StackSize;
					}
				}
				else
				{
					num += gridRecipeIngredient.Quantity;
				}
			}
			break;
		}
		return num;
	}

	public override TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
	{
		ensureConditionExists(inslot);
		return base.UpdateAndGetTransitionStates(world, inslot);
	}

	public override TransitionState UpdateAndGetTransitionState(IWorldAccessor world, ItemSlot inslot, EnumTransitionType type)
	{
		if (type != EnumTransitionType.Perish)
		{
			ensureConditionExists(inslot);
		}
		return base.UpdateAndGetTransitionState(world, inslot, type);
	}

	public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
	{
		if (Variant["construction"] == "improvised")
		{
			base.DamageItem(world, byEntity, itemslot, amount);
			return;
		}
		float num = amount;
		if (byEntity is EntityPlayer && (DressType == EnumCharacterDressType.ArmorHead || DressType == EnumCharacterDressType.ArmorBody || DressType == EnumCharacterDressType.ArmorLegs))
		{
			num *= byEntity.Stats.GetBlended("armorDurabilityLoss");
		}
		amount = GameMath.RoundRandom(world.Rand, num);
		int num2 = itemslot.Itemstack.Attributes.GetInt("durability", GetMaxDurability(itemslot.Itemstack));
		if (num2 > 0 && num2 - amount < 0)
		{
			world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.SidedPos.X, byEntity.SidedPos.InternalY, byEntity.SidedPos.Z, (byEntity as EntityPlayer)?.Player);
		}
		itemslot.Itemstack.Attributes.SetInt("durability", Math.Max(0, num2 - amount));
		itemslot.MarkDirty();
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-dress",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}

	public override int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority)
	{
		if (priority == EnumMergePriority.DirectMerge)
		{
			JsonObject itemAttributes = sinkStack.ItemAttributes;
			if (itemAttributes != null && itemAttributes["warmth"].Exists)
			{
				JsonObject itemAttributes2 = sinkStack.ItemAttributes;
				if (itemAttributes2 == null || itemAttributes2["warmth"].AsFloat() != 0f)
				{
					if ((sourceStack?.ItemAttributes?["clothingRepairStrength"].AsFloat()).GetValueOrDefault() > 0f)
					{
						if (sinkStack.Attributes.GetFloat("condition") < 1f)
						{
							return 1;
						}
						return 0;
					}
					goto IL_00c7;
				}
			}
			return base.GetMergableQuantity(sinkStack, sourceStack, priority);
		}
		goto IL_00c7;
		IL_00c7:
		return base.GetMergableQuantity(sinkStack, sourceStack, priority);
	}

	public override void TryMergeStacks(ItemStackMergeOperation op)
	{
		if (op.CurrentPriority == EnumMergePriority.DirectMerge)
		{
			float num = op.SourceSlot.Itemstack.ItemAttributes?["clothingRepairStrength"].AsFloat() ?? 0f;
			if (num > 0f && op.SinkSlot.Itemstack.Attributes.GetFloat("condition") < 1f)
			{
				ChangeCondition(op.SinkSlot, num);
				op.MovedQuantity = 1;
				op.SourceSlot.TakeOut(1);
				return;
			}
		}
		base.TryMergeStacks(op);
	}
}
