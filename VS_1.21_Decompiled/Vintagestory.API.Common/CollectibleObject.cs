using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public abstract class CollectibleObject : RegistryObject
{
	public static readonly Size3f DefaultSize = new Size3f(0.5f, 0.5f, 0.5f);

	public EnumMatterState MatterState = EnumMatterState.Solid;

	public int MaxStackSize = 64;

	public int Durability = 1;

	public Size3f Dimensions = DefaultSize;

	public bool LiquidSelectable;

	public float AttackPower = 0.5f;

	public bool HeldPriorityInteract;

	public float AttackRange = GlobalConstants.DefaultAttackRange;

	public EnumItemDamageSource[] DamagedBy;

	public Dictionary<EnumBlockMaterial, float> MiningSpeed;

	public int ToolTier;

	public HeldSounds HeldSounds;

	public string[] CreativeInventoryTabs;

	public CreativeTabAndStackList[] CreativeInventoryStacks;

	public float RenderAlphaTest = 0.05f;

	public ModelTransform GuiTransform;

	public ModelTransform FpHandTransform;

	public ModelTransform TpHandTransform;

	public ModelTransform TpOffHandTransform;

	public ModelTransform GroundTransform;

	public JsonObject Attributes;

	public CombustibleProperties CombustibleProps;

	public FoodNutritionProperties NutritionProps;

	public TransitionableProperties[] TransitionableProps;

	public GrindingProperties GrindingProps;

	public CrushingProperties CrushingProps;

	public AdvancedParticleProperties[] ParticleProperties;

	public Vec3f TopMiddlePos = new Vec3f(0.5f, 1f, 0.5f);

	public EnumTool? Tool;

	public EnumItemStorageFlags StorageFlags = EnumItemStorageFlags.General;

	public int MaterialDensity = 2000;

	public string HeldTpHitAnimation = "breakhand";

	public string HeldRightTpIdleAnimation;

	public string HeldLeftTpIdleAnimation;

	public string HeldLeftReadyAnimation;

	public string HeldRightReadyAnimation;

	public string HeldTpUseAnimation = "interactstatic";

	protected ICoreAPI api;

	public CollectibleBehavior[] CollectibleBehaviors = Array.Empty<CollectibleBehavior>();

	public ThreeBytes LightHsv;

	public bool IsMissing { get; set; }

	public abstract int Id { get; }

	public abstract EnumItemClass ItemClass { get; }

	[Obsolete("Use tool tier")]
	public int MiningTier
	{
		get
		{
			return ToolTier;
		}
		set
		{
			ToolTier = value;
		}
	}

	public override int GetHashCode()
	{
		return Id;
	}

	public void OnLoadedNative(ICoreAPI api)
	{
		this.api = api;
		OnLoaded(api);
	}

	public virtual void OnLoaded(ICoreAPI api)
	{
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		for (int i = 0; i < collectibleBehaviors.Length; i++)
		{
			collectibleBehaviors[i].OnLoaded(api);
		}
	}

	public virtual void OnUnloaded(ICoreAPI api)
	{
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		for (int i = 0; i < collectibleBehaviors.Length; i++)
		{
			collectibleBehaviors[i].OnUnloaded(api);
		}
	}

	public virtual byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		return LightHsv;
	}

	public virtual FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
	{
		return NutritionProps;
	}

	public virtual TransitionableProperties[] GetTransitionableProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
	{
		return TransitionableProps;
	}

	public virtual bool RequiresTransitionableTicking(IWorldAccessor world, ItemStack itemstack)
	{
		if (TransitionableProps != null)
		{
			return TransitionableProps.Length != 0;
		}
		return false;
	}

	public virtual EnumItemStorageFlags GetStorageFlags(ItemStack itemstack)
	{
		bool flag = false;
		EnumItemStorageFlags enumItemStorageFlags = StorageFlags;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior obj in collectibleBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			EnumItemStorageFlags storageFlags = obj.GetStorageFlags(itemstack, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag = true;
				enumItemStorageFlags = storageFlags;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return enumItemStorageFlags;
			}
		}
		if (flag)
		{
			return enumItemStorageFlags;
		}
		IHeldBag collectibleInterface = GetCollectibleInterface<IHeldBag>();
		if (collectibleInterface != null && (enumItemStorageFlags & EnumItemStorageFlags.Backpack) > (EnumItemStorageFlags)0 && collectibleInterface.IsEmpty(itemstack))
		{
			return EnumItemStorageFlags.General | EnumItemStorageFlags.Backpack;
		}
		return enumItemStorageFlags;
	}

	public virtual int GetItemDamageColor(ItemStack itemstack)
	{
		int maxDurability = GetMaxDurability(itemstack);
		if (maxDurability == 0)
		{
			return 0;
		}
		int num = GameMath.Clamp(100 * itemstack.Collectible.GetRemainingDurability(itemstack) / maxDurability, 0, 99);
		return GuiStyle.DamageColorGradient[num];
	}

	public virtual bool ShouldDisplayItemDamage(ItemStack itemstack)
	{
		return GetMaxDurability(itemstack) != GetRemainingDurability(itemstack);
	}

	public virtual void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		for (int i = 0; i < CollectibleBehaviors.Length; i++)
		{
			CollectibleBehaviors[i].OnBeforeRender(capi, itemstack, target, ref renderinfo);
		}
	}

	[Obsolete("Use GetMaxDurability instead")]
	public virtual int GetDurability(IItemStack itemstack)
	{
		return GetMaxDurability(itemstack as ItemStack);
	}

	public virtual int GetMaxDurability(ItemStack itemstack)
	{
		int durability = 0;
		EnumHandling bhHandling = EnumHandling.PassThrough;
		WalkBehaviors(delegate(CollectibleBehavior bh, ref EnumHandling hd)
		{
			int num = bh.OnGetMaxDurability(itemstack, ref bhHandling);
			if (bhHandling != EnumHandling.PassThrough)
			{
				durability += num;
			}
		}, delegate
		{
			durability += Durability;
		});
		return durability;
	}

	public virtual int GetRemainingDurability(ItemStack itemstack)
	{
		int durability = 0;
		EnumHandling bhHandling = EnumHandling.PassThrough;
		WalkBehaviors(delegate(CollectibleBehavior bh, ref EnumHandling hd)
		{
			int num = bh.OnGetMaxDurability(itemstack, ref bhHandling);
			if (bhHandling != EnumHandling.PassThrough)
			{
				durability += num;
			}
		}, delegate
		{
			durability += (int)itemstack.Attributes.GetDecimal("durability", GetMaxDurability(itemstack));
		});
		return durability;
	}

	public virtual float GetAttackPower(IItemStack withItemStack)
	{
		return AttackPower;
	}

	public virtual float GetAttackRange(IItemStack withItemStack)
	{
		return AttackRange;
	}

	public virtual float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		bool flag = false;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior obj in collectibleBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			float num = obj.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter, ref handled);
			if (handled != EnumHandling.PassThrough)
			{
				remainingResistance = num;
				flag = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return remainingResistance;
			}
		}
		if (flag)
		{
			return remainingResistance;
		}
		Block block = player.Entity.World.BlockAccessor.GetBlock(blockSel.Position);
		EnumBlockMaterial blockMaterial = block.GetBlockMaterial(api.World.BlockAccessor, blockSel.Position);
		Vec3f normalf = blockSel.Face.Normalf;
		Random rand = player.Entity.World.Rand;
		bool flag2 = block.RequiredMiningTier > 0 && itemslot.Itemstack?.Collectible != null && (itemslot.Itemstack.Collectible.ToolTier < block.RequiredMiningTier || MiningSpeed == null || !MiningSpeed.ContainsKey(blockMaterial));
		double num2 = ((blockMaterial == EnumBlockMaterial.Ore) ? 0.72 : 0.12);
		if (counter % 5 == 0 && (rand.NextDouble() < num2 || flag2) && (blockMaterial == EnumBlockMaterial.Stone || blockMaterial == EnumBlockMaterial.Ore) && (Tool == EnumTool.Pickaxe || Tool == EnumTool.Hammer))
		{
			double num3 = (double)blockSel.Position.X + blockSel.HitPosition.X;
			double num4 = (double)blockSel.Position.Y + blockSel.HitPosition.Y;
			double num5 = (double)blockSel.Position.Z + blockSel.HitPosition.Z;
			player.Entity.World.SpawnParticles(new SimpleParticleProperties
			{
				MinQuantity = 0f,
				AddQuantity = 8f,
				Color = ColorUtil.ToRgba(255, 255, 255, 128),
				MinPos = new Vec3d(num3 + (double)(normalf.X * 0.01f), num4 + (double)(normalf.Y * 0.01f), num5 + (double)(normalf.Z * 0.01f)),
				AddPos = new Vec3d(0.0, 0.0, 0.0),
				MinVelocity = new Vec3f(4f * normalf.X, 4f * normalf.Y, 4f * normalf.Z),
				AddVelocity = new Vec3f(8f * ((float)rand.NextDouble() - 0.5f), 8f * ((float)rand.NextDouble() - 0.5f), 8f * ((float)rand.NextDouble() - 0.5f)),
				LifeLength = 0.025f,
				GravityEffect = 0f,
				MinSize = 0.03f,
				MaxSize = 0.4f,
				ParticleModel = EnumParticleModel.Cube,
				VertexFlags = 200,
				SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.15f)
			}, player);
		}
		if (flag2)
		{
			return remainingResistance;
		}
		return remainingResistance - GetMiningSpeed(itemslot.Itemstack, blockSel, block, player) * dt;
	}

	public virtual void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
	{
	}

	public virtual bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1f)
	{
		bool flag = true;
		bool flag2 = false;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior obj in collectibleBehaviors)
		{
			EnumHandling bhHandling = EnumHandling.PassThrough;
			bool flag3 = obj.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier, ref bhHandling);
			if (bhHandling != EnumHandling.PassThrough)
			{
				flag = flag && flag3;
				flag2 = true;
			}
			if (bhHandling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		if (flag2)
		{
			return flag;
		}
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		(blockSel.Block ?? world.BlockAccessor.GetBlock(blockSel.Position)).OnBlockBroken(world, blockSel.Position, byPlayer, dropQuantityMultiplier);
		if (DamagedBy != null && DamagedBy.Contains(EnumItemDamageSource.BlockBreaking))
		{
			DamageItem(world, byEntity, itemslot);
		}
		return true;
	}

	public virtual float GetMiningSpeed(IItemStack itemstack, BlockSelection blockSel, Block block, IPlayer forPlayer)
	{
		float traitRate = 1f;
		EnumBlockMaterial material = block.GetBlockMaterial(api.World.BlockAccessor, blockSel.Position);
		if (material == EnumBlockMaterial.Ore || material == EnumBlockMaterial.Stone)
		{
			traitRate = forPlayer.Entity.Stats.GetBlended("miningSpeedMul");
		}
		float toolMiningSpeed = 1f;
		EnumHandling bhHandling = EnumHandling.PassThrough;
		WalkBehaviors(delegate(CollectibleBehavior bh, ref EnumHandling hd)
		{
			float num = bh.OnGetMiningSpeed(itemstack, blockSel, block, forPlayer, ref bhHandling);
			if (bhHandling != EnumHandling.PassThrough)
			{
				toolMiningSpeed *= num;
			}
		}, delegate
		{
			if (MiningSpeed == null || !MiningSpeed.ContainsKey(material))
			{
				toolMiningSpeed *= traitRate;
			}
			else
			{
				toolMiningSpeed *= MiningSpeed[material] * traitRate * GlobalConstants.ToolMiningSpeedModifier;
			}
		});
		return toolMiningSpeed;
	}

	[Obsolete]
	public virtual ModelTransformKeyFrame[] GeldHeldFpHitAnimation(ItemSlot slot, Entity byEntity)
	{
		return null;
	}

	public virtual string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
	{
		EnumHandling bhHandling = EnumHandling.PassThrough;
		string anim = null;
		WalkBehaviors(delegate(CollectibleBehavior bh, ref EnumHandling hd)
		{
			string heldTpHitAnimation = bh.GetHeldTpHitAnimation(slot, byEntity, ref bhHandling);
			if (bhHandling != EnumHandling.PassThrough)
			{
				anim = heldTpHitAnimation;
			}
		}, delegate
		{
			anim = HeldTpHitAnimation;
		});
		return anim;
	}

	public virtual string GetHeldReadyAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
	{
		EnumHandling bhHandling = EnumHandling.PassThrough;
		string anim = null;
		WalkBehaviors(delegate(CollectibleBehavior bh, ref EnumHandling hd)
		{
			string heldReadyAnimation = bh.GetHeldReadyAnimation(activeHotbarSlot, forEntity, hand, ref bhHandling);
			if (bhHandling != EnumHandling.PassThrough)
			{
				anim = heldReadyAnimation;
			}
		}, delegate
		{
			anim = ((hand == EnumHand.Left) ? HeldLeftReadyAnimation : HeldRightReadyAnimation);
		});
		return anim;
	}

	public virtual string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
	{
		EnumHandling bhHandling = EnumHandling.PassThrough;
		string anim = null;
		WalkBehaviors(delegate(CollectibleBehavior bh, ref EnumHandling hd)
		{
			string heldTpIdleAnimation = bh.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand, ref bhHandling);
			if (bhHandling != EnumHandling.PassThrough)
			{
				anim = heldTpIdleAnimation;
			}
		}, delegate
		{
			anim = ((hand == EnumHand.Left) ? HeldLeftTpIdleAnimation : HeldRightTpIdleAnimation);
		});
		return anim;
	}

	public virtual string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
	{
		EnumHandling bhHandling = EnumHandling.PassThrough;
		string anim = null;
		WalkBehaviors(delegate(CollectibleBehavior bh, ref EnumHandling hd)
		{
			string heldTpUseAnimation = bh.GetHeldTpUseAnimation(activeHotbarSlot, forEntity, ref bhHandling);
			if (bhHandling != EnumHandling.PassThrough)
			{
				anim = heldTpUseAnimation;
			}
		}, delegate
		{
			if (GetNutritionProperties(forEntity.World, activeHotbarSlot.Itemstack, forEntity) == null)
			{
				anim = HeldTpUseAnimation;
			}
		});
		return anim;
	}

	public virtual void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
	{
		if (DamagedBy != null && DamagedBy.Contains(EnumItemDamageSource.Attacking) && attackedEntity != null && attackedEntity.Alive)
		{
			DamageItem(world, byEntity, itemslot);
		}
	}

	public virtual bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
	{
		if (ingredient.IsTool && ingredient.ToolDurabilityCost > inputStack.Collectible.GetRemainingDurability(inputStack))
		{
			return false;
		}
		return true;
	}

	public virtual void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
	{
		JsonObject attributes = Attributes;
		if (attributes != null && attributes["noConsumeOnCrafting"].AsBool())
		{
			return;
		}
		if (fromIngredient.IsTool)
		{
			if (fromIngredient.ToolDurabilityCost > 0)
			{
				stackInSlot.Itemstack.Collectible.DamageItem(byPlayer.Entity.World, byPlayer.Entity, stackInSlot, fromIngredient.ToolDurabilityCost);
			}
			return;
		}
		stackInSlot.Itemstack.StackSize -= quantity;
		if (stackInSlot.Itemstack.StackSize <= 0)
		{
			stackInSlot.Itemstack = null;
			stackInSlot.MarkDirty();
		}
		if (fromIngredient.ReturnedStack != null)
		{
			ItemStack itemstack = fromIngredient.ReturnedStack.ResolvedItemstack.Clone();
			if (!byPlayer.InventoryManager.TryGiveItemstack(itemstack, slotNotifyEffect: true))
			{
				api.World.SpawnItemEntity(itemstack, byPlayer.Entity.Pos.XYZ);
			}
		}
	}

	public virtual void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
	{
		EnumHandling bhHandling = EnumHandling.PassThrough;
		WalkBehaviors(delegate(CollectibleBehavior bh, ref EnumHandling hd)
		{
			bh.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe, ref bhHandling);
		}, delegate
		{
			float num = 0f;
			float num2 = 0f;
			if (byRecipe.AverageDurability)
			{
				GridRecipeIngredient[] resolvedIngredients = byRecipe.resolvedIngredients;
				ItemSlot[] array = allInputslots;
				foreach (ItemSlot itemSlot in array)
				{
					if (!itemSlot.Empty)
					{
						ItemStack itemstack = itemSlot.Itemstack;
						int maxDurability = itemstack.Collectible.GetMaxDurability(itemstack);
						if (maxDurability == 0)
						{
							num += 0.125f;
							num2 += 0.125f;
						}
						else
						{
							bool flag = false;
							GridRecipeIngredient[] array2 = resolvedIngredients;
							foreach (GridRecipeIngredient gridRecipeIngredient in array2)
							{
								if (gridRecipeIngredient != null && gridRecipeIngredient.IsTool && gridRecipeIngredient.SatisfiesAsIngredient(itemstack))
								{
									flag = true;
									break;
								}
							}
							if (!flag)
							{
								num2 += 1f;
								int remainingDurability = itemstack.Collectible.GetRemainingDurability(itemstack);
								num += (float)remainingDurability / (float)maxDurability;
							}
						}
					}
				}
				float num3 = num / num2;
				if (num3 < 1f)
				{
					outputSlot.Itemstack.Attributes.SetInt("durability", (int)Math.Max(1f, num3 * (float)outputSlot.Itemstack.Collectible.GetMaxDurability(outputSlot.Itemstack)));
				}
			}
			TransitionableProperties transitionableProperties = outputSlot.Itemstack.Collectible.GetTransitionableProperties(api.World, outputSlot.Itemstack, null)?.FirstOrDefault((TransitionableProperties p) => p.Type == EnumTransitionType.Perish);
			if (transitionableProperties != null)
			{
				transitionableProperties.TransitionedStack.Resolve(api.World, "oncrafted perished stack", Code);
				CarryOverFreshness(api, allInputslots, new ItemStack[1] { outputSlot.Itemstack }, transitionableProperties);
			}
		});
	}

	public virtual bool ConsumeCraftingIngredients(ItemSlot[] slots, ItemSlot outputSlot, GridRecipe matchingRecipe)
	{
		return false;
	}

	public virtual void SetDurability(ItemStack itemstack, int amount)
	{
		EnumHandling bhHandling = EnumHandling.PassThrough;
		WalkBehaviors(delegate(CollectibleBehavior bh, ref EnumHandling hd)
		{
			bh.OnSetDurability(itemstack, ref amount, ref bhHandling);
		}, delegate
		{
			itemstack.Attributes.SetInt("durability", amount);
		});
	}

	public virtual void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
	{
		EnumHandling bhHandling = EnumHandling.PassThrough;
		WalkBehaviors(delegate(CollectibleBehavior bh, ref EnumHandling hd)
		{
			bh.OnDamageItem(world, byEntity, itemslot, ref amount, ref bhHandling);
		}, delegate
		{
			ItemStack itemstack = itemslot.Itemstack;
			int remainingDurability = itemstack.Collectible.GetRemainingDurability(itemstack);
			remainingDurability -= amount;
			itemstack.Attributes.SetInt("durability", remainingDurability);
			if (remainingDurability <= 0)
			{
				itemslot.Itemstack = null;
				IPlayer player = (byEntity as EntityPlayer)?.Player;
				if (player != null)
				{
					if (Tool.HasValue)
					{
						string ident = Attributes?["slotRefillIdentifier"].ToString();
						RefillSlotIfEmpty(itemslot, byEntity as EntityAgent, (ItemStack stack) => (ident == null) ? (stack.Collectible.Tool == Tool) : (stack.ItemAttributes?["slotRefillIdentifier"]?.ToString() == ident));
						if (!itemslot.Empty)
						{
							JsonObject attributes = Attributes;
							if (attributes != null && attributes.IsTrue("rememberToolModeWhenBroken"))
							{
								itemslot.Itemstack.Collectible.SetToolMode(itemslot, player, null, GetToolMode(new DummySlot(itemstack), player, null));
							}
						}
					}
					if (itemslot.Itemstack != null && !itemslot.Itemstack.Attributes.HasAttribute("durability"))
					{
						itemstack = itemslot.Itemstack;
						itemstack.Attributes.SetInt("durability", itemstack.Collectible.GetMaxDurability(itemstack));
					}
					world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), player);
				}
				else
				{
					world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.SidedPos.X, byEntity.SidedPos.Y, byEntity.SidedPos.Z, null, 1f, 16f);
				}
				world.SpawnCubeParticles(byEntity.SidedPos.XYZ.Add(byEntity.SelectionBox.Y2 / 2f), itemstack, 0.25f, 30, 1f, player);
			}
		});
		itemslot.MarkDirty();
	}

	public virtual void RefillSlotIfEmpty(ItemSlot slot, EntityAgent byEntity, ActionConsumable<ItemStack> matcher)
	{
		if (!slot.Empty)
		{
			return;
		}
		byEntity.WalkInventory(delegate(ItemSlot invslot)
		{
			if (invslot is ItemSlotCreative)
			{
				return true;
			}
			InventoryBase inventory = invslot.Inventory;
			if (!(inventory is InventoryBasePlayer) && !inventory.HasOpened((byEntity as EntityPlayer).Player))
			{
				return true;
			}
			if (invslot.Itemstack != null && matcher(invslot.Itemstack))
			{
				invslot.TryPutInto(byEntity.World, slot);
				invslot.Inventory?.PerformNotifySlot(invslot.Inventory.GetSlotId(invslot));
				slot.Inventory?.PerformNotifySlot(slot.Inventory.GetSlotId(slot));
				slot.MarkDirty();
				invslot.MarkDirty();
				return false;
			}
			return true;
		});
	}

	public virtual SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		for (int i = 0; i < CollectibleBehaviors.Length; i++)
		{
			SkillItem[] toolModes = CollectibleBehaviors[i].GetToolModes(slot, forPlayer, blockSel);
			if (toolModes != null)
			{
				return toolModes;
			}
		}
		return null;
	}

	public virtual int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
	{
		for (int i = 0; i < CollectibleBehaviors.Length; i++)
		{
			int toolMode = CollectibleBehaviors[i].GetToolMode(slot, byPlayer, blockSelection);
			if (toolMode != 0)
			{
				return toolMode;
			}
		}
		return 0;
	}

	public virtual void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
	{
		for (int i = 0; i < CollectibleBehaviors.Length; i++)
		{
			CollectibleBehaviors[i].SetToolMode(slot, byPlayer, blockSelection, toolMode);
		}
	}

	public virtual void OnHeldRenderOpaque(ItemSlot inSlot, IClientPlayer byPlayer)
	{
	}

	public virtual void OnHeldRenderOit(ItemSlot inSlot, IClientPlayer byPlayer)
	{
	}

	public virtual void OnHeldRenderOrtho(ItemSlot inSlot, IClientPlayer byPlayer)
	{
	}

	public virtual void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
	{
	}

	public virtual void OnHeldActionAnimStart(ItemSlot slot, EntityAgent byEntity, EnumHandInteract type)
	{
	}

	public virtual void OnGroundIdle(EntityItem entityItem)
	{
		if (!entityItem.Swimming || api.Side != EnumAppSide.Server)
		{
			return;
		}
		JsonObject attributes = Attributes;
		if (attributes != null && attributes.IsTrue("dissolveInWater"))
		{
			if (api.World.Rand.NextDouble() < 0.01)
			{
				api.World.SpawnCubeParticles(entityItem.ServerPos.XYZ, entityItem.Itemstack.Clone(), 0.1f, 80, 0.3f);
				entityItem.Die();
			}
			else if (api.World.Rand.NextDouble() < 0.2)
			{
				api.World.SpawnCubeParticles(entityItem.ServerPos.XYZ, entityItem.Itemstack.Clone(), 0.1f, 2, 0.2f + (float)api.World.Rand.NextDouble() / 5f);
			}
		}
	}

	public virtual void InGuiIdle(IWorldAccessor world, ItemStack stack)
	{
	}

	public virtual void OnCollected(ItemStack stack, Entity entity)
	{
	}

	public virtual void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
	{
		switch (useType)
		{
		case EnumHandInteract.HeldItemAttack:
			OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
			break;
		case EnumHandInteract.HeldItemInteract:
			OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
			break;
		}
	}

	public EnumHandInteract OnHeldUseCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		EnumHandInteract handUse = byEntity.Controls.HandUse;
		if (!((handUse == EnumHandInteract.HeldItemAttack) ? OnHeldAttackCancel(secondsPassed, slot, byEntity, blockSel, entitySel, cancelReason) : OnHeldInteractCancel(secondsPassed, slot, byEntity, blockSel, entitySel, cancelReason)))
		{
			return handUse;
		}
		return EnumHandInteract.None;
	}

	public EnumHandInteract OnHeldUseStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		EnumHandInteract handUse = byEntity.Controls.HandUse;
		if (!((handUse == EnumHandInteract.HeldItemAttack) ? OnHeldAttackStep(secondsPassed, slot, byEntity, blockSel, entitySel) : OnHeldInteractStep(secondsPassed, slot, byEntity, blockSel, entitySel)))
		{
			return EnumHandInteract.None;
		}
		return handUse;
	}

	public void OnHeldUseStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType)
	{
		if (useType == EnumHandInteract.HeldItemAttack)
		{
			OnHeldAttackStop(secondsPassed, slot, byEntity, blockSel, entitySel);
		}
		else
		{
			OnHeldInteractStop(secondsPassed, slot, byEntity, blockSel, entitySel);
		}
	}

	public virtual void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		EnumHandHandling bhHandHandling = EnumHandHandling.NotHandled;
		WalkBehaviors(delegate(CollectibleBehavior bh, ref EnumHandling hd)
		{
			bh.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref bhHandHandling, ref hd);
		}, delegate
		{
			if (HeldSounds?.Attack != null && api.World.Side == EnumAppSide.Client)
			{
				api.World.PlaySoundAt(HeldSounds.Attack, 0.0, 0.0, 0.0, null, 0.9f + (float)api.World.Rand.NextDouble() * 0.2f);
			}
		});
		handling = bhHandHandling;
	}

	public virtual bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		bool retval = false;
		WalkBehaviors(delegate(CollectibleBehavior bh, ref EnumHandling hd)
		{
			bool flag = bh.OnHeldAttackCancel(secondsPassed, slot, byEntity, blockSelection, entitySel, cancelReason, ref hd);
			if (hd != EnumHandling.PassThrough)
			{
				retval = flag;
			}
		}, delegate
		{
		});
		return retval;
	}

	public virtual bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
	{
		bool retval = false;
		WalkBehaviors(delegate(CollectibleBehavior bh, ref EnumHandling hd)
		{
			bool flag = bh.OnHeldAttackStep(secondsPassed, slot, byEntity, blockSelection, entitySel, ref hd);
			if (hd != EnumHandling.PassThrough)
			{
				retval = flag;
			}
		}, delegate
		{
		});
		return retval;
	}

	public virtual void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
	{
		WalkBehaviors(delegate(CollectibleBehavior bh, ref EnumHandling hd)
		{
			bh.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSelection, entitySel, ref hd);
		}, delegate
		{
		});
	}

	public virtual void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		EnumHandHandling handHandling = EnumHandHandling.NotHandled;
		bool flag = false;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior obj in collectibleBehaviors)
		{
			EnumHandling handling2 = EnumHandling.PassThrough;
			obj.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling2);
			if (handling2 != EnumHandling.PassThrough)
			{
				handling = handHandling;
				flag = true;
			}
			if (handling2 == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (!flag)
		{
			tryEatBegin(slot, byEntity, ref handHandling);
			handling = handHandling;
		}
	}

	public virtual bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		bool flag = true;
		bool flag2 = false;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior obj in collectibleBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag3 = obj.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag = flag && flag3;
				flag2 = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		if (flag2)
		{
			return flag;
		}
		return tryEatStep(secondsUsed, slot, byEntity);
	}

	public virtual void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		bool flag = false;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior obj in collectibleBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			obj.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag = true;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (!flag)
		{
			tryEatStop(secondsUsed, slot, byEntity);
		}
	}

	public virtual bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		bool flag = true;
		bool flag2 = false;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior obj in collectibleBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool flag3 = obj.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason, ref handled);
			if (handled != EnumHandling.PassThrough)
			{
				flag = flag && flag3;
				flag2 = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		if (flag2)
		{
			return flag;
		}
		return true;
	}

	protected virtual void tryEatBegin(ItemSlot slot, EntityAgent byEntity, ref EnumHandHandling handling, string eatSound = "eat", int eatSoundRepeats = 1)
	{
		if (!slot.Empty && GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity) != null)
		{
			byEntity.World.RegisterCallback(delegate
			{
				playEatSound(byEntity, eatSound, eatSoundRepeats);
			}, 500);
			byEntity.AnimManager?.StartAnimation("eat");
			handling = EnumHandHandling.PreventDefault;
		}
	}

	protected void playEatSound(EntityAgent byEntity, string eatSound = "eat", int eatSoundRepeats = 1)
	{
		if (byEntity.Controls.HandUse != EnumHandInteract.HeldItemInteract)
		{
			return;
		}
		IPlayer dualCallByPlayer = null;
		if (byEntity is EntityPlayer)
		{
			dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		byEntity.PlayEntitySound(eatSound, dualCallByPlayer);
		eatSoundRepeats--;
		if (eatSoundRepeats > 0)
		{
			byEntity.World.RegisterCallback(delegate
			{
				playEatSound(byEntity, eatSound, eatSoundRepeats);
			}, 300);
		}
	}

	protected virtual bool tryEatStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, ItemStack spawnParticleStack = null)
	{
		if (GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity) == null)
		{
			return false;
		}
		Vec3d xYZ = byEntity.Pos.AheadCopy(0.4000000059604645).XYZ;
		xYZ.X += byEntity.LocalEyePos.X;
		xYZ.Y += byEntity.LocalEyePos.Y - 0.4000000059604645;
		xYZ.Z += byEntity.LocalEyePos.Z;
		if (secondsUsed > 0.5f && (int)(30f * secondsUsed) % 7 == 1)
		{
			byEntity.World.SpawnCubeParticles(xYZ, spawnParticleStack ?? slot.Itemstack, 0.3f, 4, 0.5f, (byEntity as EntityPlayer)?.Player);
		}
		if (byEntity.World is IClientWorldAccessor)
		{
			return secondsUsed <= 1f;
		}
		return true;
	}

	protected virtual void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
	{
		FoodNutritionProperties nutritionProperties = GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);
		if (!(byEntity.World is IServerWorldAccessor) || nutritionProperties == null || !(secondsUsed >= 0.95f))
		{
			return;
		}
		float spoilState = UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish)?.TransitionLevel ?? 0f;
		float num = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, byEntity);
		float num2 = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, byEntity);
		byEntity.ReceiveSaturation(nutritionProperties.Satiety * num, nutritionProperties.FoodCategory);
		IPlayer player = null;
		if (byEntity is EntityPlayer)
		{
			player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		slot.TakeOut(1);
		if (nutritionProperties.EatenStack != null)
		{
			if (slot.Empty)
			{
				slot.Itemstack = nutritionProperties.EatenStack.ResolvedItemstack.Clone();
			}
			else if (player == null || !player.InventoryManager.TryGiveItemstack(nutritionProperties.EatenStack.ResolvedItemstack.Clone(), slotNotifyEffect: true))
			{
				byEntity.World.SpawnItemEntity(nutritionProperties.EatenStack.ResolvedItemstack.Clone(), byEntity.SidedPos.XYZ);
			}
		}
		float num3 = nutritionProperties.Health * num2;
		float num4 = byEntity.WatchedAttributes.GetFloat("intoxication");
		byEntity.WatchedAttributes.SetFloat("intoxication", Math.Min(1.1f, num4 + nutritionProperties.Intoxication));
		if (num3 != 0f)
		{
			float valueOrDefault = (slot.Itemstack?.Collectible?.Attributes?["eatHealthEffectDurationSec"].AsFloat()).GetValueOrDefault();
			int ticksPerDuration = slot.Itemstack?.Collectible?.Attributes?["eatHealthEffectTicks"].AsInt(1) ?? 1;
			byEntity.ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Internal,
				Type = ((num3 > 0f) ? EnumDamageType.Heal : EnumDamageType.Poison),
				Duration = TimeSpan.FromSeconds(valueOrDefault),
				TicksPerDuration = ticksPerDuration,
				DamageOverTimeTypeEnum = ((!(num3 > 0f)) ? EnumDamageOverTimeEffectType.Poison : EnumDamageOverTimeEffectType.Unknown)
			}, Math.Abs(num3));
		}
		slot.MarkDirty();
		player.InventoryManager.BroadcastHotbarSlot();
	}

	public virtual void OnHeldDropped(IWorldAccessor world, IPlayer byPlayer, ItemSlot slot, int quantity, ref EnumHandling handling)
	{
	}

	public virtual string GetHeldItemName(ItemStack itemStack)
	{
		if (Code == null)
		{
			return "Invalid block, id " + Id;
		}
		string text = ItemClass.Name();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(Lang.GetMatching(Code?.Domain + ":" + text + "-" + Code?.Path));
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		for (int i = 0; i < collectibleBehaviors.Length; i++)
		{
			collectibleBehaviors[i].GetHeldItemName(stringBuilder, itemStack);
		}
		return stringBuilder.ToString();
	}

	public virtual void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		ItemStack itemstack = inSlot.Itemstack;
		string itemDescText = GetItemDescText();
		if (withDebugInfo)
		{
			dsc.AppendLine("<font color=\"#bbbbbb\">Id:" + Id + "</font>");
			dsc.AppendLine(string.Concat("<font color=\"#bbbbbb\">Code: ", Code, "</font>"));
			ICoreAPI coreAPI = api;
			if (coreAPI != null && coreAPI.Side == EnumAppSide.Client && (api as ICoreClientAPI).Input.KeyboardKeyStateRaw[1])
			{
				dsc.AppendLine("<font color=\"#bbbbbb\">Attributes: " + inSlot.Itemstack.Attributes.ToJsonToken() + "</font>\n");
			}
		}
		int maxDurability = GetMaxDurability(itemstack);
		if (maxDurability > 1)
		{
			dsc.AppendLine(Lang.Get("Durability: {0} / {1}", itemstack.Collectible.GetRemainingDurability(itemstack), maxDurability));
		}
		if (MiningSpeed != null && MiningSpeed.Count > 0)
		{
			dsc.AppendLine(Lang.Get("Tool Tier: {0}", ToolTier));
			dsc.Append(Lang.Get("item-tooltip-miningspeed"));
			int num = 0;
			foreach (KeyValuePair<EnumBlockMaterial, float> item in MiningSpeed)
			{
				if (!((double)item.Value < 1.1))
				{
					if (num > 0)
					{
						dsc.Append(", ");
					}
					dsc.Append(Lang.Get(item.Key.ToString()) + " " + item.Value.ToString("#.#") + "x");
					num++;
				}
			}
			dsc.Append("\n");
		}
		IHeldBag collectibleInterface = GetCollectibleInterface<IHeldBag>();
		if (collectibleInterface != null)
		{
			dsc.AppendLine(Lang.Get("Storage Slots: {0}", collectibleInterface.GetQuantitySlots(itemstack)));
			bool flag = false;
			ItemStack[] contents = collectibleInterface.GetContents(itemstack, world);
			if (contents != null)
			{
				ItemStack[] array = contents;
				foreach (ItemStack itemStack in array)
				{
					if (itemStack != null && itemStack.StackSize != 0)
					{
						if (!flag)
						{
							dsc.AppendLine(Lang.Get("Contents: "));
							flag = true;
						}
						itemStack.ResolveBlockOrItem(world);
						dsc.AppendLine("- " + itemStack.StackSize + "x " + itemStack.GetName());
					}
				}
				if (!flag)
				{
					dsc.AppendLine(Lang.Get("Empty"));
				}
			}
		}
		EntityPlayer entityPlayer = ((world.Side == EnumAppSide.Client) ? (world as IClientWorldAccessor).Player.Entity : null);
		float spoilState = AppendPerishableInfoText(inSlot, dsc, world);
		FoodNutritionProperties nutritionProperties = GetNutritionProperties(world, itemstack, entityPlayer);
		if (nutritionProperties != null)
		{
			float num2 = GlobalConstants.FoodSpoilageSatLossMul(spoilState, itemstack, entityPlayer);
			float num3 = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, itemstack, entityPlayer);
			if (Math.Abs(nutritionProperties.Health * num3) > 0.001f)
			{
				dsc.AppendLine(Lang.Get((MatterState == EnumMatterState.Liquid) ? "liquid-when-drunk-saturation-hp" : "When eaten: {0} sat, {1} hp", Math.Round(nutritionProperties.Satiety * num2), Math.Round(nutritionProperties.Health * num3, 2)));
			}
			else
			{
				dsc.AppendLine(Lang.Get((MatterState == EnumMatterState.Liquid) ? "liquid-when-drunk-saturation" : "When eaten: {0} sat", Math.Round(nutritionProperties.Satiety * num2)));
			}
			dsc.AppendLine(Lang.Get("Food Category: {0}", Lang.Get("foodcategory-" + nutritionProperties.FoodCategory.ToString().ToLowerInvariant())));
		}
		if (GrindingProps?.GroundStack?.ResolvedItemstack != null)
		{
			dsc.AppendLine(Lang.Get("When ground: Turns into {0}x {1}", GrindingProps.GroundStack.ResolvedItemstack.StackSize, GrindingProps.GroundStack.ResolvedItemstack.GetName()));
		}
		if (CrushingProps != null)
		{
			float num4 = CrushingProps.Quantity.avg * (float)CrushingProps.CrushedStack.ResolvedItemstack.StackSize;
			dsc.AppendLine(Lang.Get("When pulverized: Turns into {0:0.#}x {1}", num4, CrushingProps.CrushedStack.ResolvedItemstack.GetName()));
			dsc.AppendLine(Lang.Get("Requires Pulverizer tier: {0}", CrushingProps.HardnessTier));
		}
		if (GetAttackPower(itemstack) > 0.5f)
		{
			dsc.AppendLine(Lang.Get("Attack power: -{0} hp", GetAttackPower(itemstack).ToString("0.#")));
			dsc.AppendLine(Lang.Get("Attack tier: {0}", ToolTier));
		}
		if (GetAttackRange(itemstack) > GlobalConstants.DefaultAttackRange)
		{
			dsc.AppendLine(Lang.Get("Attack range: {0} m", GetAttackRange(itemstack).ToString("0.#")));
		}
		if (CombustibleProps != null)
		{
			string text = CombustibleProps.SmeltingType.ToString().ToLowerInvariant();
			if (text == "fire")
			{
				dsc.AppendLine(Lang.Get("itemdesc-fireinkiln"));
			}
			else
			{
				if (CombustibleProps.BurnTemperature > 0)
				{
					dsc.AppendLine(Lang.Get("Burn temperature: {0}°C", CombustibleProps.BurnTemperature));
					dsc.AppendLine(Lang.Get("Burn duration: {0}s", CombustibleProps.BurnDuration));
				}
				if (CombustibleProps.MeltingPoint > 0)
				{
					dsc.AppendLine(Lang.Get("game:smeltpoint-" + text, CombustibleProps.MeltingPoint));
				}
			}
			if (CombustibleProps.SmeltedStack?.ResolvedItemstack != null)
			{
				int smeltedRatio = CombustibleProps.SmeltedRatio;
				int stackSize = CombustibleProps.SmeltedStack.ResolvedItemstack.StackSize;
				string value = ((smeltedRatio == 1) ? Lang.Get("game:smeltdesc-" + text + "-singular", stackSize, CombustibleProps.SmeltedStack.ResolvedItemstack.GetName()) : Lang.Get("game:smeltdesc-" + text + "-plural", smeltedRatio, stackSize, CombustibleProps.SmeltedStack.ResolvedItemstack.GetName()));
				dsc.AppendLine(value);
			}
		}
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		for (int i = 0; i < collectibleBehaviors.Length; i++)
		{
			collectibleBehaviors[i].GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		}
		if (itemDescText.Length > 0 && dsc.Length > 0)
		{
			dsc.Append("\n");
		}
		dsc.Append(itemDescText);
		float temperature = GetTemperature(world, itemstack);
		if (temperature > 20f)
		{
			dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int)temperature));
		}
		if (Code != null && Code.Domain != "game")
		{
			Mod mod = api.ModLoader.GetMod(Code.Domain);
			dsc.AppendLine(Lang.Get("Mod: {0}", mod?.Info.Name ?? Code.Domain));
		}
	}

	public virtual string GetItemDescText()
	{
		string text = Code?.Domain + ":" + ItemClass.ToString().ToLowerInvariant() + "desc-" + Code?.Path;
		string matching = Lang.GetMatching(text);
		if (matching == text)
		{
			return "";
		}
		return matching + "\n";
	}

	public virtual WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		WorldInteraction[] array = ((GetNutritionProperties(api.World, inSlot.Itemstack, null) == null) ? Array.Empty<WorldInteraction>() : new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-eat",
				MouseButton = EnumMouseButton.Right
			}
		});
		EnumHandling handling = EnumHandling.PassThrough;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		for (int i = 0; i < collectibleBehaviors.Length; i++)
		{
			WorldInteraction[] heldInteractionHelp = collectibleBehaviors[i].GetHeldInteractionHelp(inSlot, ref handling);
			array = array.Append(heldInteractionHelp);
			if (handling == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		return array;
	}

	public virtual float AppendPerishableInfoText(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
	{
		float num = 0f;
		TransitionState[] array = UpdateAndGetTransitionStates(api.World, inSlot);
		bool flag = false;
		if (array == null)
		{
			return 0f;
		}
		for (int i = 0; i < array.Length; i++)
		{
			num = Math.Max(num, AppendPerishableInfoText(inSlot, dsc, world, array[i], flag));
			flag = flag || num > 0f;
		}
		return num;
	}

	protected virtual float AppendPerishableInfoText(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, TransitionState state, bool nowSpoiling)
	{
		TransitionableProperties props = state.Props;
		float num = GetTransitionRateMul(world, inSlot, props.Type);
		if (inSlot.Inventory is CreativeInventoryTab)
		{
			num = 1f;
		}
		float transitionLevel = state.TransitionLevel;
		float num2 = state.FreshHoursLeft / num;
		switch (props.Type)
		{
		case EnumTransitionType.Perish:
		{
			if (transitionLevel > 0f)
			{
				dsc.AppendLine(Lang.Get("itemstack-perishable-spoiling", (int)Math.Round(transitionLevel * 100f)));
				return transitionLevel;
			}
			if (num <= 0f)
			{
				dsc.AppendLine(Lang.Get("itemstack-perishable"));
				break;
			}
			float hoursPerDay = api.World.Calendar.HoursPerDay;
			float num7 = num2 / hoursPerDay / (float)api.World.Calendar.DaysPerYear;
			if (num7 >= 1f)
			{
				if (num7 <= 1.05f)
				{
					dsc.AppendLine(Lang.Get("itemstack-perishable-fresh-one-year"));
					break;
				}
				dsc.AppendLine(Lang.Get("itemstack-perishable-fresh-years", Math.Round(num7, 1)));
			}
			else if (num2 > hoursPerDay)
			{
				dsc.AppendLine(Lang.Get("itemstack-perishable-fresh-days", Math.Round(num2 / hoursPerDay, 1)));
			}
			else
			{
				dsc.AppendLine(Lang.Get("itemstack-perishable-fresh-hours", Math.Round(num2, 1)));
			}
			break;
		}
		case EnumTransitionType.Cure:
		{
			if (nowSpoiling)
			{
				break;
			}
			if (transitionLevel > 0f || (num2 <= 0f && num > 0f))
			{
				dsc.AppendLine(Lang.Get("itemstack-curable-curing", (int)Math.Round(transitionLevel * 100f)));
				break;
			}
			double num8 = api.World.Calendar.HoursPerDay;
			if (num <= 0f)
			{
				dsc.AppendLine(Lang.Get("itemstack-curable"));
			}
			else if ((double)num2 > num8)
			{
				dsc.AppendLine(Lang.Get("itemstack-curable-duration-days", Math.Round((double)num2 / num8, 1)));
			}
			else
			{
				dsc.AppendLine(Lang.Get("itemstack-curable-duration-hours", Math.Round(num2, 1)));
			}
			break;
		}
		case EnumTransitionType.Ripen:
		{
			if (nowSpoiling)
			{
				break;
			}
			if (transitionLevel > 0f || (num2 <= 0f && num > 0f))
			{
				dsc.AppendLine(Lang.Get("itemstack-ripenable-ripening", (int)Math.Round(transitionLevel * 100f)));
				break;
			}
			double num4 = api.World.Calendar.HoursPerDay;
			if (num <= 0f)
			{
				dsc.AppendLine(Lang.Get("itemstack-ripenable"));
			}
			else if ((double)num2 > num4)
			{
				dsc.AppendLine(Lang.Get("itemstack-ripenable-duration-days", Math.Round((double)num2 / num4, 1)));
			}
			else
			{
				dsc.AppendLine(Lang.Get("itemstack-ripenable-duration-hours", Math.Round(num2, 1)));
			}
			break;
		}
		case EnumTransitionType.Dry:
		{
			if (nowSpoiling)
			{
				break;
			}
			if (transitionLevel > 0f)
			{
				dsc.AppendLine(Lang.Get("itemstack-dryable-dried", (int)Math.Round(transitionLevel * 100f)));
				dsc.AppendLine(Lang.Get("Drying rate in this container: {0:0.##}x", num));
				break;
			}
			double num5 = api.World.Calendar.HoursPerDay;
			if (num <= 0f)
			{
				dsc.AppendLine(Lang.Get("itemstack-dryable"));
			}
			else if ((double)num2 > num5)
			{
				dsc.AppendLine(Lang.Get("itemstack-dryable-duration-days", Math.Round((double)num2 / num5, 1)));
			}
			else
			{
				dsc.AppendLine(Lang.Get("itemstack-dryable-duration-hours", Math.Round(num2, 1)));
			}
			break;
		}
		case EnumTransitionType.Melt:
		{
			if (nowSpoiling)
			{
				break;
			}
			if (transitionLevel > 0f || num2 <= 0f)
			{
				dsc.AppendLine(Lang.Get("itemstack-meltable-melted", (int)Math.Round(transitionLevel * 100f)));
				dsc.AppendLine(Lang.Get("Melting rate in this container: {0:0.##}x", num));
				break;
			}
			double num6 = api.World.Calendar.HoursPerDay;
			if (num <= 0f)
			{
				dsc.AppendLine(Lang.Get("itemstack-meltable"));
			}
			else if ((double)num2 > num6)
			{
				dsc.AppendLine(Lang.Get("itemstack-meltable-duration-days", Math.Round((double)num2 / num6, 1)));
			}
			else
			{
				dsc.AppendLine(Lang.Get("itemstack-meltable-duration-hours", Math.Round(num2, 1)));
			}
			break;
		}
		case EnumTransitionType.Harden:
		{
			if (nowSpoiling)
			{
				break;
			}
			if (transitionLevel > 0f || num2 <= 0f)
			{
				dsc.AppendLine(Lang.Get("itemstack-hardenable-hardened", (int)Math.Round(transitionLevel * 100f)));
				break;
			}
			double num3 = api.World.Calendar.HoursPerDay;
			if (num <= 0f)
			{
				dsc.AppendLine(Lang.Get("itemstack-hardenable"));
			}
			else if ((double)num2 > num3)
			{
				dsc.AppendLine(Lang.Get("itemstack-hardenable-duration-days", Math.Round((double)num2 / num3, 1)));
			}
			else
			{
				dsc.AppendLine(Lang.Get("itemstack-hardenable-duration-hours", Math.Round(num2, 1)));
			}
			break;
		}
		}
		return 0f;
	}

	public virtual void OnHandbookRecipeRender(ICoreClientAPI capi, GridRecipe recipe, ItemSlot slot, double x, double y, double z, double size)
	{
		capi.Render.RenderItemstackToGui(slot, x, y, z, (float)size * 0.58f, -1);
		for (int i = 0; i < CollectibleBehaviors.Length; i++)
		{
			CollectibleBehaviors[i].OnHandbookRecipeRender(capi, recipe, slot, x, y, z, size);
		}
	}

	public virtual List<ItemStack> GetHandBookStacks(ICoreClientAPI capi)
	{
		if (Code == null)
		{
			return null;
		}
		JsonObject jsonObject = Attributes?["handbook"];
		if (jsonObject != null && jsonObject["exclude"].AsBool())
		{
			return null;
		}
		bool num = CreativeInventoryTabs != null && CreativeInventoryTabs.Length != 0;
		bool flag = CreativeInventoryStacks != null && CreativeInventoryStacks.Length != 0;
		if (!num && !flag && jsonObject?["include"].AsBool() != true)
		{
			return null;
		}
		List<ItemStack> list = new List<ItemStack>();
		if (flag && (jsonObject == null || !jsonObject["ignoreCreativeInvStacks"].AsBool()))
		{
			for (int i = 0; i < CreativeInventoryStacks.Length; i++)
			{
				JsonItemStack[] stacks = CreativeInventoryStacks[i].Stacks;
				for (int j = 0; j < stacks.Length; j++)
				{
					ItemStack stack = stacks[j].ResolvedItemstack;
					stack.ResolveBlockOrItem(capi.World);
					stack = stack.Clone();
					stack.StackSize = stack.Collectible.MaxStackSize;
					if (!list.Any((ItemStack itemStack) => itemStack.Equals(stack)))
					{
						list.Add(stack);
					}
				}
			}
		}
		else
		{
			ItemStack item = new ItemStack(this);
			list.Add(item);
		}
		return list;
	}

	public virtual bool CanBePlacedInto(ItemStack stack, ItemSlot slot)
	{
		if (slot.StorageType != 0)
		{
			return (slot.StorageType & GetStorageFlags(stack)) > (EnumItemStorageFlags)0;
		}
		return true;
	}

	public virtual int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority)
	{
		if (Equals(sinkStack, sourceStack, GlobalConstants.IgnoredStackAttributes) && sinkStack.StackSize < MaxStackSize)
		{
			return Math.Min(MaxStackSize - sinkStack.StackSize, sourceStack.StackSize);
		}
		return 0;
	}

	public virtual void TryMergeStacks(ItemStackMergeOperation op)
	{
		op.MovableQuantity = GetMergableQuantity(op.SinkSlot.Itemstack, op.SourceSlot.Itemstack, op.CurrentPriority);
		if (op.MovableQuantity == 0 || !op.SinkSlot.CanTakeFrom(op.SourceSlot, op.CurrentPriority))
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		op.MovedQuantity = GameMath.Min(op.SinkSlot.GetRemainingSlotSpace(op.SourceSlot.Itemstack), op.MovableQuantity, op.RequestedQuantity);
		if (HasTemperature(op.SinkSlot.Itemstack) || HasTemperature(op.SourceSlot.Itemstack))
		{
			if (op.CurrentPriority < EnumMergePriority.DirectMerge && Math.Abs(GetTemperature(op.World, op.SinkSlot.Itemstack) - GetTemperature(op.World, op.SourceSlot.Itemstack)) > 30f)
			{
				op.MovedQuantity = 0;
				op.MovableQuantity = 0;
				op.RequiredPriority = EnumMergePriority.DirectMerge;
				return;
			}
			flag = true;
		}
		TransitionState[] array = UpdateAndGetTransitionStates(op.World, op.SourceSlot);
		TransitionState[] array2 = UpdateAndGetTransitionStates(op.World, op.SinkSlot);
		Dictionary<EnumTransitionType, TransitionState> dictionary = null;
		if (array != null)
		{
			bool flag3 = true;
			bool flag4 = true;
			if (array2 == null)
			{
				op.MovedQuantity = 0;
				op.MovableQuantity = 0;
				return;
			}
			dictionary = new Dictionary<EnumTransitionType, TransitionState>();
			TransitionState[] array3 = array2;
			foreach (TransitionState transitionState in array3)
			{
				dictionary[transitionState.Props.Type] = transitionState;
			}
			array3 = array;
			foreach (TransitionState transitionState2 in array3)
			{
				if (!dictionary.TryGetValue(transitionState2.Props.Type, out var value))
				{
					flag4 = false;
					flag3 = false;
					break;
				}
				if (Math.Abs(value.TransitionedHours - transitionState2.TransitionedHours) > 4f && Math.Abs(value.TransitionedHours - transitionState2.TransitionedHours) / transitionState2.FreshHours > 0.03f)
				{
					flag4 = false;
				}
			}
			if (!flag4 && op.CurrentPriority < EnumMergePriority.DirectMerge)
			{
				op.MovedQuantity = 0;
				op.MovableQuantity = 0;
				op.RequiredPriority = EnumMergePriority.DirectMerge;
				return;
			}
			if (!flag3)
			{
				op.MovedQuantity = 0;
				op.MovableQuantity = 0;
				return;
			}
			flag2 = true;
		}
		if (op.SourceSlot.Itemstack == null)
		{
			op.MovedQuantity = 0;
		}
		else
		{
			if (op.MovedQuantity <= 0)
			{
				return;
			}
			if (op.SinkSlot.Itemstack == null)
			{
				op.SinkSlot.Itemstack = new ItemStack(op.SourceSlot.Itemstack.Collectible, 0);
			}
			if (flag)
			{
				SetTemperature(op.World, op.SinkSlot.Itemstack, ((float)op.SinkSlot.StackSize * GetTemperature(op.World, op.SinkSlot.Itemstack) + (float)op.MovedQuantity * GetTemperature(op.World, op.SourceSlot.Itemstack)) / (float)(op.SinkSlot.StackSize + op.MovedQuantity));
			}
			if (flag2)
			{
				float num = (float)op.MovedQuantity / (float)(op.MovedQuantity + op.SinkSlot.StackSize);
				TransitionState[] array3 = array;
				foreach (TransitionState transitionState3 in array3)
				{
					TransitionState transitionState4 = dictionary[transitionState3.Props.Type];
					SetTransitionState(op.SinkSlot.Itemstack, transitionState3.Props.Type, transitionState3.TransitionedHours * num + transitionState4.TransitionedHours * (1f - num));
				}
			}
			op.SinkSlot.Itemstack.StackSize += op.MovedQuantity;
			op.SourceSlot.Itemstack.StackSize -= op.MovedQuantity;
			if (op.SourceSlot.Itemstack.StackSize <= 0)
			{
				op.SourceSlot.Itemstack = null;
			}
		}
	}

	public virtual float GetMeltingDuration(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
	{
		if (CombustibleProps != null)
		{
			return CombustibleProps.MeltingDuration;
		}
		return 0f;
	}

	public virtual float GetMeltingPoint(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
	{
		return (CombustibleProps != null) ? CombustibleProps.MeltingPoint : 0;
	}

	public virtual bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
	{
		ItemStack itemStack = CombustibleProps?.SmeltedStack?.ResolvedItemstack;
		if (itemStack != null && inputStack.StackSize >= CombustibleProps.SmeltedRatio && CombustibleProps.MeltingPoint > 0 && (CombustibleProps.SmeltingType != EnumSmeltType.Fire || world.Config.GetString("allowOpenFireFiring").ToBool()))
		{
			if (outputStack != null)
			{
				return outputStack.Collectible.GetMergableQuantity(outputStack, itemStack, EnumMergePriority.AutoMerge) >= itemStack.StackSize;
			}
			return true;
		}
		return false;
	}

	public virtual void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
	{
		if (CanSmelt(world, cookingSlotsProvider, inputSlot.Itemstack, outputSlot.Itemstack))
		{
			ItemStack itemStack = CombustibleProps.SmeltedStack.ResolvedItemstack.Clone();
			TransitionState transitionState = UpdateAndGetTransitionState(world, new DummySlot(inputSlot.Itemstack), EnumTransitionType.Perish);
			if (transitionState != null)
			{
				TransitionState transitionState2 = itemStack.Collectible.UpdateAndGetTransitionState(world, new DummySlot(itemStack), EnumTransitionType.Perish);
				float val = transitionState.TransitionedHours / (transitionState.TransitionHours + transitionState.FreshHours) * 0.8f * (transitionState2.TransitionHours + transitionState2.FreshHours) - 1f;
				itemStack.Collectible.SetTransitionState(itemStack, EnumTransitionType.Perish, Math.Max(0f, val));
			}
			int num = 1;
			if (outputSlot.Itemstack == null)
			{
				outputSlot.Itemstack = itemStack;
				outputSlot.Itemstack.StackSize = num * itemStack.StackSize;
			}
			else
			{
				itemStack.StackSize = num * itemStack.StackSize;
				ItemStackMergeOperation itemStackMergeOperation = new ItemStackMergeOperation(world, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.ConfirmedMerge, num * itemStack.StackSize);
				itemStackMergeOperation.SourceSlot = new DummySlot(itemStack);
				itemStackMergeOperation.SinkSlot = new DummySlot(outputSlot.Itemstack);
				outputSlot.Itemstack.Collectible.TryMergeStacks(itemStackMergeOperation);
				outputSlot.Itemstack = itemStackMergeOperation.SinkSlot.Itemstack;
			}
			inputSlot.Itemstack.StackSize -= num * CombustibleProps.SmeltedRatio;
			if (inputSlot.Itemstack.StackSize <= 0)
			{
				inputSlot.Itemstack = null;
			}
			outputSlot.MarkDirty();
		}
	}

	public virtual bool CanSpoil(ItemStack itemstack)
	{
		if (itemstack == null || itemstack.Attributes == null)
		{
			return false;
		}
		if (itemstack.Collectible.NutritionProps != null)
		{
			return itemstack.Attributes.HasAttribute("spoilstate");
		}
		return false;
	}

	public virtual TransitionState UpdateAndGetTransitionState(IWorldAccessor world, ItemSlot inslot, EnumTransitionType type)
	{
		TransitionState[] array = UpdateAndGetTransitionStates(world, inslot);
		if (array == null)
		{
			return null;
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Props.Type == type)
			{
				return array[i];
			}
		}
		return null;
	}

	public virtual void SetTransitionState(ItemStack stack, EnumTransitionType type, float transitionedHours)
	{
		ITreeAttribute treeAttribute = (ITreeAttribute)stack.Attributes["transitionstate"];
		if (treeAttribute == null)
		{
			UpdateAndGetTransitionState(api.World, new DummySlot(stack), type);
			treeAttribute = (ITreeAttribute)stack.Attributes["transitionstate"];
		}
		TransitionableProperties[] transitionableProperties = GetTransitionableProperties(api.World, stack, null);
		for (int i = 0; i < transitionableProperties.Length; i++)
		{
			if (transitionableProperties[i].Type == type)
			{
				(treeAttribute["transitionedHours"] as FloatArrayAttribute).value[i] = transitionedHours;
				break;
			}
		}
	}

	public virtual float GetTransitionRateMul(IWorldAccessor world, ItemSlot inSlot, EnumTransitionType transType)
	{
		float num = ((inSlot.Inventory == null) ? 1f : inSlot.Inventory.GetTransitionSpeedMul(transType, inSlot.Itemstack));
		if (transType == EnumTransitionType.Perish)
		{
			if (inSlot.Itemstack.Collectible.GetTemperature(world, inSlot.Itemstack) > 75f)
			{
				num = 0f;
			}
			num *= GlobalConstants.PerishSpeedModifier;
		}
		return num;
	}

	public virtual TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
	{
		return UpdateAndGetTransitionStatesNative(world, inslot);
	}

	protected virtual TransitionState[] UpdateAndGetTransitionStatesNative(IWorldAccessor world, ItemSlot inslot)
	{
		if (inslot is ItemSlotCreative)
		{
			return null;
		}
		ItemStack itemstack = inslot.Itemstack;
		TransitionableProperties[] transitionableProperties = GetTransitionableProperties(world, inslot.Itemstack, null);
		if (itemstack == null || transitionableProperties == null || transitionableProperties.Length == 0)
		{
			return null;
		}
		if (itemstack.Attributes == null)
		{
			itemstack.Attributes = new TreeAttribute();
		}
		if (itemstack.Attributes.GetBool("timeFrozen"))
		{
			return null;
		}
		if (!(itemstack.Attributes["transitionstate"] is ITreeAttribute))
		{
			itemstack.Attributes["transitionstate"] = new TreeAttribute();
		}
		ITreeAttribute treeAttribute = (ITreeAttribute)itemstack.Attributes["transitionstate"];
		TransitionState[] array = new TransitionState[transitionableProperties.Length];
		float[] array2;
		float[] array3;
		float[] array4;
		if (!treeAttribute.HasAttribute("createdTotalHours"))
		{
			treeAttribute.SetDouble("createdTotalHours", world.Calendar.TotalHours);
			treeAttribute.SetDouble("lastUpdatedTotalHours", world.Calendar.TotalHours);
			array2 = new float[transitionableProperties.Length];
			array3 = new float[transitionableProperties.Length];
			array4 = new float[transitionableProperties.Length];
			for (int i = 0; i < transitionableProperties.Length; i++)
			{
				array4[i] = 0f;
				array2[i] = transitionableProperties[i].FreshHours.nextFloat(1f, world.Rand);
				array3[i] = transitionableProperties[i].TransitionHours.nextFloat(1f, world.Rand);
			}
			treeAttribute["freshHours"] = new FloatArrayAttribute(array2);
			treeAttribute["transitionHours"] = new FloatArrayAttribute(array3);
			treeAttribute["transitionedHours"] = new FloatArrayAttribute(array4);
		}
		else
		{
			array2 = (treeAttribute["freshHours"] as FloatArrayAttribute).value;
			array3 = (treeAttribute["transitionHours"] as FloatArrayAttribute).value;
			array4 = (treeAttribute["transitionedHours"] as FloatArrayAttribute).value;
			if (transitionableProperties.Length - array2.Length > 0)
			{
				for (int j = array2.Length; j < transitionableProperties.Length; j++)
				{
					array2 = array2.Append(transitionableProperties[j].FreshHours.nextFloat(1f, world.Rand));
					array3 = array3.Append(transitionableProperties[j].TransitionHours.nextFloat(1f, world.Rand));
					array4 = array4.Append(0f);
				}
				(treeAttribute["freshHours"] as FloatArrayAttribute).value = array2;
				(treeAttribute["transitionHours"] as FloatArrayAttribute).value = array3;
				(treeAttribute["transitionedHours"] as FloatArrayAttribute).value = array4;
			}
		}
		double num = treeAttribute.GetDouble("lastUpdatedTotalHours");
		double totalHours = world.Calendar.TotalHours;
		bool flag = false;
		float num2 = (float)(totalHours - num);
		for (int k = 0; k < transitionableProperties.Length; k++)
		{
			TransitionableProperties transitionableProperties2 = transitionableProperties[k];
			if (transitionableProperties2 == null)
			{
				continue;
			}
			float transitionRateMul = GetTransitionRateMul(world, inslot, transitionableProperties2.Type);
			if (num2 > 0.05f)
			{
				float num3 = num2 * transitionRateMul;
				array4[k] += num3;
			}
			float freshHoursLeft = Math.Max(0f, array2[k] - array4[k]);
			float num4 = Math.Max(0f, array4[k] - array2[k]) / array3[k];
			if (num4 > 0f)
			{
				if (transitionableProperties2.Type == EnumTransitionType.Perish)
				{
					flag = true;
				}
				else if (flag)
				{
					continue;
				}
			}
			if (num4 >= 1f && world.Side == EnumAppSide.Server)
			{
				ItemStack itemStack = OnTransitionNow(inslot, itemstack.Collectible.TransitionableProps[k]);
				if (itemStack.StackSize <= 0)
				{
					inslot.Itemstack = null;
				}
				else
				{
					itemstack.SetFrom(itemStack);
				}
				inslot.MarkDirty();
				break;
			}
			array[k] = new TransitionState
			{
				FreshHoursLeft = freshHoursLeft,
				TransitionLevel = Math.Min(1f, num4),
				TransitionedHours = array4[k],
				TransitionHours = array3[k],
				FreshHours = array2[k],
				Props = transitionableProperties2
			};
		}
		if (num2 > 0.05f)
		{
			treeAttribute.SetDouble("lastUpdatedTotalHours", totalHours);
		}
		return (from s in array
			where s != null
			orderby (int)s.Props.Type
			select s).ToArray();
	}

	public virtual ItemStack OnTransitionNow(ItemSlot slot, TransitionableProperties props)
	{
		bool flag = false;
		ItemStack itemStack = props.TransitionedStack.ResolvedItemstack.Clone();
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior obj in collectibleBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			ItemStack itemStack2 = obj.OnTransitionNow(slot, props, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				flag = true;
				itemStack = itemStack2;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return itemStack;
			}
		}
		if (flag)
		{
			return itemStack;
		}
		itemStack.StackSize = GameMath.RoundRandom(api.World.Rand, (float)slot.Itemstack.StackSize * props.TransitionRatio);
		return itemStack;
	}

	public static void CarryOverFreshness(ICoreAPI api, ItemSlot inputSlot, ItemStack outputStack, TransitionableProperties perishProps)
	{
		CarryOverFreshness(api, new ItemSlot[1] { inputSlot }, new ItemStack[1] { outputStack }, perishProps);
	}

	public static void CarryOverFreshness(ICoreAPI api, ItemSlot[] inputSlots, ItemStack[] outStacks, TransitionableProperties perishProps)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		int num4 = 0;
		foreach (ItemSlot itemSlot in inputSlots)
		{
			if (!itemSlot.Empty)
			{
				TransitionState transitionState = itemSlot.Itemstack?.Collectible?.UpdateAndGetTransitionState(api.World, itemSlot, EnumTransitionType.Perish);
				if (transitionState != null)
				{
					num4++;
					float num5 = transitionState.TransitionedHours / (transitionState.TransitionHours + transitionState.FreshHours);
					float num6 = Math.Max(0f, (transitionState.TransitionedHours - transitionState.FreshHours) / transitionState.TransitionHours);
					num2 = Math.Max(num6, num2);
					num += num5;
					num3 += num6;
				}
			}
		}
		num /= (float)Math.Max(1, num4);
		num3 /= (float)Math.Max(1, num4);
		for (int j = 0; j < outStacks.Length; j++)
		{
			if (outStacks[j] != null)
			{
				if (!(outStacks[j].Attributes["transitionstate"] is ITreeAttribute))
				{
					outStacks[j].Attributes["transitionstate"] = new TreeAttribute();
				}
				float num7 = perishProps.TransitionHours.nextFloat(1f, api.World.Rand);
				float num8 = perishProps.FreshHours.nextFloat(1f, api.World.Rand);
				ITreeAttribute treeAttribute = (ITreeAttribute)outStacks[j].Attributes["transitionstate"];
				treeAttribute.SetDouble("createdTotalHours", api.World.Calendar.TotalHours);
				treeAttribute.SetDouble("lastUpdatedTotalHours", api.World.Calendar.TotalHours);
				treeAttribute["freshHours"] = new FloatArrayAttribute(new float[1] { num8 });
				treeAttribute["transitionHours"] = new FloatArrayAttribute(new float[1] { num7 });
				if (num3 > 0f)
				{
					num3 *= 0.6f;
					treeAttribute["transitionedHours"] = new FloatArrayAttribute(new float[1] { num8 + Math.Max(0f, num7 * num3 - 2f) });
				}
				else
				{
					treeAttribute["transitionedHours"] = new FloatArrayAttribute(new float[1] { Math.Max(0f, num * (0.8f + (float)(2 + num4) * num2) * (num7 + num8)) });
				}
			}
		}
	}

	public virtual bool IsReasonablyFresh(IWorldAccessor world, ItemStack itemstack)
	{
		if (GetMaxDurability(itemstack) > 1 && (float)GetRemainingDurability(itemstack) / (float)GetMaxDurability(itemstack) < 0.95f)
		{
			return false;
		}
		if (itemstack == null)
		{
			return true;
		}
		TransitionableProperties[] transitionableProperties = GetTransitionableProperties(world, itemstack, null);
		if (transitionableProperties == null)
		{
			return true;
		}
		ITreeAttribute treeAttribute = (ITreeAttribute)itemstack.Attributes["transitionstate"];
		if (treeAttribute == null)
		{
			return true;
		}
		float[] value = (treeAttribute["freshHours"] as FloatArrayAttribute).value;
		float[] value2 = (treeAttribute["transitionedHours"] as FloatArrayAttribute).value;
		for (int i = 0; i < transitionableProperties.Length; i++)
		{
			TransitionableProperties obj = transitionableProperties[i];
			if (obj != null && obj.Type == EnumTransitionType.Perish && value2[i] > value[i] / 2f)
			{
				return false;
			}
		}
		return true;
	}

	public virtual bool HasTemperature(IItemStack itemstack)
	{
		if (itemstack == null || itemstack.Attributes == null)
		{
			return false;
		}
		return itemstack.Attributes.HasAttribute("temperature");
	}

	public virtual float GetTemperature(IWorldAccessor world, ItemStack itemstack, double didReceiveHeat)
	{
		if (!(itemstack?.Attributes?["temperature"] is ITreeAttribute))
		{
			return 20f;
		}
		ITreeAttribute treeAttribute = (ITreeAttribute)itemstack.Attributes["temperature"];
		double totalHours = world.Calendar.TotalHours;
		double num = treeAttribute.GetDouble("temperatureLastUpdate");
		double num2 = totalHours - (num + didReceiveHeat);
		float num3 = treeAttribute.GetFloat("temperature", 20f);
		if (num2 > 0.0117647061124444 && num3 > 0f)
		{
			num3 = Math.Max(0f, num3 - Math.Max(0f, (float)(totalHours - num) * treeAttribute.GetFloat("cooldownSpeed", 90f)));
			treeAttribute.SetFloat("temperature", num3);
		}
		treeAttribute.SetDouble("temperatureLastUpdate", totalHours);
		return num3;
	}

	public virtual float GetTemperature(IWorldAccessor world, ItemStack itemstack)
	{
		if (!(itemstack?.Attributes?["temperature"] is ITreeAttribute))
		{
			return 20f;
		}
		ITreeAttribute treeAttribute = (ITreeAttribute)itemstack.Attributes["temperature"];
		double totalHours = world.Calendar.TotalHours;
		double num = treeAttribute.GetDecimal("temperatureLastUpdate");
		double num2 = totalHours - num;
		float num3 = (float)treeAttribute.GetDecimal("temperature", 20.0);
		if (itemstack.Attributes.GetBool("timeFrozen"))
		{
			return num3;
		}
		if (num2 > 0.0117647061124444 && num3 > 0f)
		{
			num3 = Math.Max(0f, num3 - Math.Max(0f, (float)(totalHours - num) * treeAttribute.GetFloat("cooldownSpeed", 90f)));
			treeAttribute.SetFloat("temperature", num3);
			treeAttribute.SetDouble("temperatureLastUpdate", totalHours);
		}
		return num3;
	}

	public virtual void SetTemperature(IWorldAccessor world, ItemStack itemstack, float temperature, bool delayCooldown = true)
	{
		if (itemstack != null)
		{
			ITreeAttribute treeAttribute = (ITreeAttribute)itemstack.Attributes["temperature"];
			if (treeAttribute == null)
			{
				treeAttribute = (ITreeAttribute)(itemstack.Attributes["temperature"] = new TreeAttribute());
			}
			double num = world.Calendar.TotalHours;
			if (delayCooldown && treeAttribute.GetDecimal("temperature") < (double)temperature)
			{
				num += 0.5;
			}
			treeAttribute.SetDouble("temperatureLastUpdate", num);
			treeAttribute.SetFloat("temperature", temperature);
		}
	}

	public virtual bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
	{
		if (thisStack.Class == otherStack.Class && thisStack.Id == otherStack.Id)
		{
			return thisStack.Attributes.Equals(api.World, otherStack.Attributes, ignoreAttributeSubTrees);
		}
		return false;
	}

	public virtual bool Satisfies(ItemStack thisStack, ItemStack otherStack)
	{
		if (thisStack.Class == otherStack.Class && thisStack.Id == otherStack.Id)
		{
			return thisStack.Attributes.IsSubSetOf(api.World, otherStack.Attributes);
		}
		return false;
	}

	public virtual void OnStoreCollectibleMappings(IWorldAccessor world, ItemSlot inSlot, Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		if (this is Item)
		{
			itemIdMapping[Id] = Code;
		}
		else
		{
			blockIdMapping[Id] = Code;
		}
		OnStoreCollectibleMappings(world, inSlot.Itemstack.Attributes, blockIdMapping, itemIdMapping);
		ITreeAttribute obj = inSlot.Itemstack.Attributes["temperature"] as ITreeAttribute;
		if (obj != null && obj.HasAttribute("temperatureLastUpdate"))
		{
			GetTemperature(world, inSlot.Itemstack);
		}
	}

	[Obsolete("Use the variant with resolveImports parameter")]
	public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, ItemSlot inSlot, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping)
	{
		OnLoadCollectibleMappings(worldForResolve, inSlot, oldBlockIdMapping, oldItemIdMapping, resolveImports: true);
	}

	public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, ItemSlot inSlot, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, bool resolveImports)
	{
		OnLoadCollectibleMappings(worldForResolve, inSlot.Itemstack.Attributes, oldBlockIdMapping, oldItemIdMapping);
	}

	private void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, ITreeAttribute tree, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping)
	{
		foreach (KeyValuePair<string, IAttribute> item in tree)
		{
			if (item.Value is ITreeAttribute tree2)
			{
				OnLoadCollectibleMappings(worldForResolve, tree2, oldBlockIdMapping, oldItemIdMapping);
			}
			else if (item.Value is ItemstackAttribute { value: var value } itemstackAttribute)
			{
				if (value != null && !value.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
				{
					itemstackAttribute.value = null;
				}
				else
				{
					value?.Collectible.OnLoadCollectibleMappings(worldForResolve, value.Attributes, oldBlockIdMapping, oldItemIdMapping);
				}
			}
		}
		if (tree.HasAttribute("temperatureLastUpdate"))
		{
			tree.SetDouble("temperatureLastUpdate", worldForResolve.Calendar.TotalHours);
		}
		if (tree.HasAttribute("createdTotalHours"))
		{
			double num = tree.GetDouble("createdTotalHours");
			double num2 = tree.GetDouble("lastUpdatedTotalHours") - num;
			tree.SetDouble("lastUpdatedTotalHours", worldForResolve.Calendar.TotalHours);
			tree.SetDouble("createdTotalHours", worldForResolve.Calendar.TotalHours - num2);
		}
	}

	private void OnStoreCollectibleMappings(IWorldAccessor world, ITreeAttribute tree, Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		foreach (KeyValuePair<string, IAttribute> item in tree)
		{
			if (item.Value is ITreeAttribute tree2)
			{
				OnStoreCollectibleMappings(world, tree2, blockIdMapping, itemIdMapping);
			}
			else if (item.Value is ItemstackAttribute { value: { } value })
			{
				if (value.Collectible == null)
				{
					value.ResolveBlockOrItem(world);
				}
				if (value.Class == EnumItemClass.Item)
				{
					itemIdMapping[value.Id] = value.Collectible?.Code;
				}
				else
				{
					blockIdMapping[value.Id] = value.Collectible?.Code;
				}
			}
		}
	}

	public virtual int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
	{
		return 0;
	}

	public virtual bool IsLiquid()
	{
		return MatterState == EnumMatterState.Liquid;
	}

	private void WalkBehaviors(CollectibleBehaviorDelegate onBehavior, Action defaultAction)
	{
		bool flag = true;
		CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
		foreach (CollectibleBehavior behavior in collectibleBehaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			onBehavior(behavior, ref handling);
			switch (handling)
			{
			case EnumHandling.PreventSubsequent:
				return;
			case EnumHandling.PreventDefault:
				flag = false;
				break;
			}
		}
		if (flag)
		{
			defaultAction();
		}
	}

	public CollectibleBehavior GetCollectibleBehavior(Type type, bool withInheritance)
	{
		return GetBehavior(CollectibleBehaviors, type, withInheritance);
	}

	public T GetCollectibleBehavior<T>(bool withInheritance) where T : CollectibleBehavior
	{
		return GetBehavior(CollectibleBehaviors, typeof(T), withInheritance) as T;
	}

	protected virtual CollectibleBehavior GetBehavior(CollectibleBehavior[] fromList, Type type, bool withInheritance)
	{
		if (withInheritance)
		{
			for (int i = 0; i < fromList.Length; i++)
			{
				Type type2 = fromList[i].GetType();
				if (type2 == type || type.IsAssignableFrom(type2))
				{
					return fromList[i];
				}
			}
			return null;
		}
		for (int j = 0; j < fromList.Length; j++)
		{
			if (fromList[j].GetType() == type)
			{
				return fromList[j];
			}
		}
		return null;
	}

	public virtual T GetCollectibleInterface<T>() where T : class
	{
		if (this is T result)
		{
			return result;
		}
		CollectibleBehavior collectibleBehavior = GetCollectibleBehavior(typeof(T), withInheritance: true);
		if (collectibleBehavior != null)
		{
			return collectibleBehavior as T;
		}
		return null;
	}

	public virtual bool HasBehavior<T>(bool withInheritance = false) where T : CollectibleBehavior
	{
		return (T)GetCollectibleBehavior(typeof(T), withInheritance) != null;
	}

	public virtual bool HasBehavior(Type type, bool withInheritance = false)
	{
		return GetCollectibleBehavior(type, withInheritance) != null;
	}

	public virtual bool HasBehavior(string type, IClassRegistryAPI classRegistry)
	{
		return GetBehavior(classRegistry.GetBlockBehaviorClass(type)) != null;
	}

	public CollectibleBehavior GetBehavior(Type type)
	{
		return GetCollectibleBehavior(type, withInheritance: false);
	}

	public T GetBehavior<T>() where T : CollectibleBehavior
	{
		return (T)GetCollectibleBehavior(typeof(T), withInheritance: false);
	}

	public virtual bool OnSmeltAttempt(InventoryBase inventorySmelting)
	{
		return false;
	}

	[Obsolete]
	public static bool IsEmptyBackPack(IItemStack itemstack)
	{
		if (!IsBackPack(itemstack))
		{
			return false;
		}
		ITreeAttribute treeAttribute = itemstack.Attributes.GetTreeAttribute("backpack");
		if (treeAttribute == null)
		{
			return true;
		}
		foreach (KeyValuePair<string, IAttribute> item in treeAttribute.GetTreeAttribute("slots"))
		{
			IItemStack itemStack = (IItemStack)(item.Value?.GetValue());
			if (itemStack != null && itemStack.StackSize > 0)
			{
				return false;
			}
		}
		return true;
	}

	[Obsolete]
	public static bool IsBackPack(IItemStack itemstack)
	{
		if (itemstack == null || itemstack.Collectible.Attributes == null)
		{
			return false;
		}
		return itemstack.Collectible.Attributes["backpack"]["quantitySlots"].AsInt() > 0;
	}

	[Obsolete]
	public static int QuantityBackPackSlots(IItemStack itemstack)
	{
		if (itemstack == null || itemstack.Collectible.Attributes == null)
		{
			return 0;
		}
		return itemstack.Collectible.Attributes["backpack"]["quantitySlots"].AsInt();
	}
}
