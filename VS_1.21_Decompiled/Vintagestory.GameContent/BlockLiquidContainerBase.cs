using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public abstract class BlockLiquidContainerBase : BlockContainer, ILiquidSource, ILiquidInterface, ILiquidSink
{
	public enum EnumLiquidDirection
	{
		Fill,
		Pour
	}

	protected float capacityLitresFromAttributes = 10f;

	private Dictionary<string, ItemStack[]> recipeLiquidContents = new Dictionary<string, ItemStack[]>();

	public virtual float CapacityLitres => capacityLitresFromAttributes;

	public virtual int ContainerSlotId => 0;

	public virtual float TransferSizeLitres => 1f;

	public virtual bool CanDrinkFrom => Attributes["canDrinkFrom"].AsBool();

	public virtual bool IsTopOpened => Attributes["isTopOpened"].AsBool();

	public virtual bool AllowHeldLiquidTransfer => Attributes["allowHeldLiquidTransfer"].AsBool();

	public WorldInteraction[] interactions { get; protected set; }

	public override void OnHandbookRecipeRender(ICoreClientAPI capi, GridRecipe gridRecipe, ItemSlot dummyslot, double x, double y, double z, double size)
	{
		int num = dummyslot.BackgroundIcon.ToInt();
		JsonObject jsonObject = gridRecipe.resolvedIngredients[num].RecipeAttributes;
		if (jsonObject == null || !jsonObject.Exists || jsonObject == null || !jsonObject["requiresContent"].Exists)
		{
			jsonObject = gridRecipe.Attributes?["liquidContainerProps"];
		}
		if (jsonObject == null || !jsonObject.Exists)
		{
			base.OnHandbookRecipeRender(capi, gridRecipe, dummyslot, x, y, z, size);
			return;
		}
		string text = jsonObject?["requiresContent"]?["code"]?.AsString() ?? gridRecipe.Attributes["liquidContainerProps"]["requiresContent"]["code"].AsString();
		string text2 = jsonObject?["requiresContent"]?["type"]?.AsString() ?? gridRecipe.Attributes["liquidContainerProps"]["requiresContent"]["type"].AsString();
		float num2 = jsonObject?["requiresLitres"]?.AsFloat() ?? gridRecipe.Attributes["liquidContainerProps"]["requiresLitres"].AsFloat();
		string key = text2 + "-" + text;
		if (!recipeLiquidContents.TryGetValue(key, out var value))
		{
			if (text.Contains('*'))
			{
				EnumItemClass enumItemClass = ((!(text2 == "block")) ? EnumItemClass.Item : EnumItemClass.Block);
				List<ItemStack> list = new List<ItemStack>();
				AssetLocation needle = AssetLocation.Create(text, Code.Domain);
				foreach (CollectibleObject collectible in api.World.Collectibles)
				{
					if (collectible.ItemClass == enumItemClass && WildcardUtil.Match(needle, collectible.Code))
					{
						ItemStack itemStack = new ItemStack(collectible);
						WaterTightContainableProps containableProps = GetContainableProps(itemStack);
						if (containableProps != null)
						{
							itemStack.StackSize = (int)(containableProps.ItemsPerLitre * num2);
							list.Add(itemStack);
						}
					}
				}
				value = list.ToArray();
			}
			else
			{
				value = (recipeLiquidContents[key] = new ItemStack[1]);
				if (text2 == "item")
				{
					value[0] = new ItemStack(capi.World.GetItem(new AssetLocation(text)));
				}
				else
				{
					value[0] = new ItemStack(capi.World.GetBlock(new AssetLocation(text)));
				}
				WaterTightContainableProps containableProps2 = GetContainableProps(value[0]);
				value[0].StackSize = (int)((containableProps2?.ItemsPerLitre ?? 1f) * num2);
			}
		}
		ItemStack itemStack2 = dummyslot.Itemstack.Clone();
		int num3 = (int)(capi.ElapsedMilliseconds / 1000) % value.Length;
		SetContent(itemStack2, value[num3]);
		dummyslot.Itemstack = itemStack2;
		capi.Render.RenderItemstackToGui(dummyslot, x, y, z, (float)size * 0.58f, -1);
	}

	public virtual int GetContainerSlotId(BlockPos pos)
	{
		return ContainerSlotId;
	}

	public virtual int GetContainerSlotId(ItemStack containerStack)
	{
		return ContainerSlotId;
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		JsonObject attributes = Attributes;
		if (attributes != null && attributes["capacityLitres"].Exists)
		{
			capacityLitresFromAttributes = Attributes["capacityLitres"].AsInt(10);
		}
		else
		{
			LiquidTopOpenContainerProps liquidTopOpenContainerProps = Attributes?["liquidContainerProps"]?.AsObject<LiquidTopOpenContainerProps>(null, Code.Domain);
			if (liquidTopOpenContainerProps != null)
			{
				capacityLitresFromAttributes = liquidTopOpenContainerProps.CapacityLitres;
			}
		}
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "liquidContainerBase", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject collectible in api.World.Collectibles)
			{
				if (collectible is BlockLiquidContainerBase { IsTopOpened: not false, AllowHeldLiquidTransfer: not false })
				{
					list.Add(new ItemStack(collectible));
				}
			}
			ItemStack[] itemstacks = list.ToArray();
			return new WorldInteraction[3]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-bucket-rightclick",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = itemstacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-bucket-rightclick-sneak",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "shift",
					Itemstacks = itemstacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-bucket-rightclick-sprint",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "ctrl",
					Itemstacks = itemstacks
				}
			};
		});
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(interactions);
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[3]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-fill",
				MouseButton = EnumMouseButton.Right,
				ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => GetCurrentLitres(inSlot.Itemstack) < CapacityLitres
			},
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-empty",
				HotKeyCode = "ctrl",
				MouseButton = EnumMouseButton.Right,
				ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => GetCurrentLitres(inSlot.Itemstack) > 0f
			},
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-place",
				HotKeyCode = "shift",
				MouseButton = EnumMouseButton.Right,
				ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => true
			}
		};
	}

	public bool SetCurrentLitres(ItemStack containerStack, float litres)
	{
		WaterTightContainableProps contentProps = GetContentProps(containerStack);
		if (contentProps == null)
		{
			return false;
		}
		ItemStack content = GetContent(containerStack);
		content.StackSize = (int)(litres * contentProps.ItemsPerLitre);
		SetContent(containerStack, content);
		return true;
	}

	public float GetCurrentLitres(ItemStack containerStack)
	{
		WaterTightContainableProps contentProps = GetContentProps(containerStack);
		if (contentProps == null)
		{
			return 0f;
		}
		return (float)GetContent(containerStack).StackSize / contentProps.ItemsPerLitre;
	}

	public float GetCurrentLitres(BlockPos pos)
	{
		WaterTightContainableProps contentProps = GetContentProps(pos);
		if (contentProps == null)
		{
			return 0f;
		}
		return (float)GetContent(pos).StackSize / contentProps.ItemsPerLitre;
	}

	public bool IsFull(ItemStack containerStack)
	{
		return GetCurrentLitres(containerStack) >= CapacityLitres;
	}

	public bool IsFull(BlockPos pos)
	{
		return GetCurrentLitres(pos) >= CapacityLitres;
	}

	public WaterTightContainableProps? GetContentProps(ItemStack containerStack)
	{
		return GetContainableProps(GetContent(containerStack));
	}

	public static int GetTransferStackSize(ILiquidInterface containerBlock, ItemStack contentStack, IPlayer player = null)
	{
		return GetTransferStackSize(containerBlock, contentStack, player != null && player.Entity?.Controls.ShiftKey == true);
	}

	public static int GetTransferStackSize(ILiquidInterface containerBlock, ItemStack contentStack, bool maxCapacity)
	{
		if (contentStack == null)
		{
			return 0;
		}
		float transferSizeLitres = containerBlock.TransferSizeLitres;
		float num = GetContainableProps(contentStack)?.ItemsPerLitre ?? 1f;
		int result = (int)(num * transferSizeLitres);
		if (maxCapacity)
		{
			result = (int)(containerBlock.CapacityLitres * num);
		}
		return result;
	}

	public static WaterTightContainableProps? GetContainableProps(ItemStack? stack)
	{
		try
		{
			JsonObject jsonObject = stack?.ItemAttributes?["waterTightContainerProps"];
			if (jsonObject != null && jsonObject.Exists)
			{
				return jsonObject.AsObject<WaterTightContainableProps>(null, stack.Collectible.Code.Domain);
			}
			return null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public WaterTightContainableProps? GetContentProps(BlockPos pos)
	{
		if (!(api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer blockEntityContainer))
		{
			return null;
		}
		int containerSlotId = GetContainerSlotId(pos);
		if (containerSlotId >= blockEntityContainer.Inventory.Count)
		{
			return null;
		}
		ItemStack itemStack = blockEntityContainer.Inventory[containerSlotId]?.Itemstack;
		if (itemStack == null)
		{
			return null;
		}
		return GetContainableProps(itemStack);
	}

	public void SetContent(ItemStack containerStack, ItemStack content)
	{
		if (content == null)
		{
			SetContents(containerStack, null);
			return;
		}
		SetContents(containerStack, new ItemStack[1] { content });
	}

	public void SetContent(BlockPos pos, ItemStack content)
	{
		if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer blockEntityContainer)
		{
			new DummySlot(content).TryPutInto(api.World, blockEntityContainer.Inventory[GetContainerSlotId(pos)], content.StackSize);
			blockEntityContainer.Inventory[GetContainerSlotId(pos)].MarkDirty();
			blockEntityContainer.MarkDirty(redrawOnClient: true);
		}
	}

	public ItemStack? GetContent(ItemStack containerStack)
	{
		ItemStack[] contents = GetContents(api.World, containerStack);
		int containerSlotId = GetContainerSlotId(containerStack);
		if (contents == null || contents.Length == 0)
		{
			return null;
		}
		return contents[Math.Min(contents.Length - 1, containerSlotId)];
	}

	public ItemStack? GetContent(BlockPos pos)
	{
		if (!(api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer blockEntityContainer))
		{
			return null;
		}
		return blockEntityContainer.Inventory[GetContainerSlotId(pos)].Itemstack;
	}

	public override ItemStack CreateItemStackFromJson(ITreeAttribute stackAttr, IWorldAccessor world, string domain)
	{
		bool num = stackAttr.HasAttribute("makefull");
		stackAttr.RemoveAttribute("makefull");
		ItemStack itemStack = base.CreateItemStackFromJson(stackAttr, world, domain);
		if (num)
		{
			WaterTightContainableProps containableProps = GetContainableProps(itemStack);
			itemStack.StackSize = (int)(CapacityLitres * (containableProps?.ItemsPerLitre ?? 1f));
		}
		return itemStack;
	}

	public ItemStack TryTakeContent(ItemStack containerStack, int quantityItems)
	{
		ItemStack content = GetContent(containerStack);
		if (content == null)
		{
			return null;
		}
		ItemStack itemStack = content.Clone();
		itemStack.StackSize = quantityItems;
		content.StackSize -= quantityItems;
		if (content.StackSize <= 0)
		{
			SetContent(containerStack, null);
			return itemStack;
		}
		SetContent(containerStack, content);
		return itemStack;
	}

	public ItemStack TryTakeContent(BlockPos pos, int quantityItem)
	{
		if (!(api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer blockEntityContainer))
		{
			return null;
		}
		ItemStack itemstack = blockEntityContainer.Inventory[GetContainerSlotId(pos)].Itemstack;
		if (itemstack == null)
		{
			return null;
		}
		ItemStack itemStack = itemstack.Clone();
		itemStack.StackSize = quantityItem;
		itemstack.StackSize -= quantityItem;
		if (itemstack.StackSize <= 0)
		{
			blockEntityContainer.Inventory[GetContainerSlotId(pos)].Itemstack = null;
		}
		else
		{
			blockEntityContainer.Inventory[GetContainerSlotId(pos)].Itemstack = itemstack;
		}
		blockEntityContainer.Inventory[GetContainerSlotId(pos)].MarkDirty();
		blockEntityContainer.MarkDirty(redrawOnClient: true);
		return itemStack;
	}

	public ItemStack TryTakeLiquid(ItemStack containerStack, float desiredLitres)
	{
		WaterTightContainableProps containableProps = GetContainableProps(GetContent(containerStack));
		if (containableProps == null)
		{
			return null;
		}
		return TryTakeContent(containerStack, (int)(desiredLitres * containableProps.ItemsPerLitre));
	}

	public virtual int TryPutLiquid(ItemStack containerStack, ItemStack liquidStack, float desiredLitres)
	{
		if (liquidStack == null)
		{
			return 0;
		}
		WaterTightContainableProps containableProps = GetContainableProps(liquidStack);
		if (containableProps == null)
		{
			return 0;
		}
		float num = 1E-05f;
		int num2 = (int)(containableProps.ItemsPerLitre * desiredLitres + num);
		int stackSize = liquidStack.StackSize;
		ItemStack content = GetContent(containerStack);
		ILiquidSink liquidSink = containerStack.Collectible as ILiquidSink;
		if (content == null)
		{
			if (!containableProps.Containable)
			{
				return 0;
			}
			int num3 = (int)(liquidSink.CapacityLitres * containableProps.ItemsPerLitre + num);
			ItemStack itemStack = liquidStack.Clone();
			itemStack.StackSize = GameMath.Min(stackSize, num2, num3);
			SetContent(containerStack, itemStack);
			return Math.Min(num2, num3);
		}
		if (!content.Equals(api.World, liquidStack, GlobalConstants.IgnoredStackAttributes))
		{
			return 0;
		}
		int num4 = (int)(liquidSink.CapacityLitres * containableProps.ItemsPerLitre - (float)content.StackSize);
		int num5 = GameMath.Min(stackSize, num4, num2);
		content.StackSize += num5;
		return num5;
	}

	public virtual int TryPutLiquid(BlockPos pos, ItemStack liquidStack, float desiredLitres)
	{
		if (liquidStack == null)
		{
			return 0;
		}
		WaterTightContainableProps containableProps = GetContainableProps(liquidStack);
		float num = containableProps?.ItemsPerLitre ?? 1f;
		int num2 = (int)(num * desiredLitres);
		float num3 = liquidStack.StackSize;
		float num4 = CapacityLitres * num;
		ItemStack content = GetContent(pos);
		if (content == null)
		{
			if (containableProps == null || !containableProps.Containable)
			{
				return 0;
			}
			int val = (int)GameMath.Min(num2, num4, num3);
			int num5 = Math.Min(num2, val);
			ItemStack itemStack = liquidStack.Clone();
			itemStack.StackSize = num5;
			SetContent(pos, itemStack);
			return num5;
		}
		if (!content.Equals(api.World, liquidStack, GlobalConstants.IgnoredStackAttributes))
		{
			return 0;
		}
		int num6 = Math.Min((int)Math.Min(num3, num4 - (float)content.StackSize), num2);
		content.StackSize += num6;
		api.World.BlockAccessor.GetBlockEntity(pos).MarkDirty(redrawOnClient: true);
		(api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityContainer).Inventory[GetContainerSlotId(pos)].MarkDirty();
		return num6;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (!activeHotbarSlot.Empty)
		{
			JsonObject attributes = activeHotbarSlot.Itemstack.Collectible.Attributes;
			if (attributes != null && attributes.IsTrue("handleLiquidContainerInteract"))
			{
				EnumHandHandling handling = EnumHandHandling.NotHandled;
				activeHotbarSlot.Itemstack.Collectible.OnHeldInteractStart(activeHotbarSlot, byPlayer.Entity, blockSel, null, firstEvent: true, ref handling);
				if (handling == EnumHandHandling.PreventDefault || handling == EnumHandHandling.PreventDefaultAction)
				{
					return true;
				}
			}
		}
		if (activeHotbarSlot.Empty || !(activeHotbarSlot.Itemstack.Collectible is ILiquidInterface))
		{
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		CollectibleObject collectible = activeHotbarSlot.Itemstack.Collectible;
		bool shiftKey = byPlayer.WorldData.EntityControls.ShiftKey;
		bool ctrlKey = byPlayer.WorldData.EntityControls.CtrlKey;
		ILiquidSource objLso = collectible as ILiquidSource;
		if (objLso != null && !shiftKey)
		{
			if (!objLso.AllowHeldLiquidTransfer)
			{
				return false;
			}
			ItemStack content = objLso.GetContent(activeHotbarSlot.Itemstack);
			float desiredLitres = (ctrlKey ? objLso.TransferSizeLitres : objLso.CapacityLitres);
			int moved = TryPutLiquid(blockSel.Position, content, desiredLitres);
			if (moved > 0)
			{
				SplitStackAndPerformAction(byPlayer.Entity, activeHotbarSlot, delegate(ItemStack stack)
				{
					objLso.TryTakeContent(stack, moved);
					return moved;
				});
				DoLiquidMovedEffects(byPlayer, content, moved, EnumLiquidDirection.Pour);
				return true;
			}
		}
		ILiquidSink objLsi = collectible as ILiquidSink;
		if (objLsi != null && !ctrlKey)
		{
			if (!objLsi.AllowHeldLiquidTransfer)
			{
				return false;
			}
			ItemStack owncontentStack = GetContent(blockSel.Position);
			if (owncontentStack == null)
			{
				return base.OnBlockInteractStart(world, byPlayer, blockSel);
			}
			ItemStack contentStack = owncontentStack.Clone();
			float litres = (shiftKey ? objLsi.TransferSizeLitres : objLsi.CapacityLitres);
			int num = SplitStackAndPerformAction(byPlayer.Entity, activeHotbarSlot, (ItemStack stack) => objLsi.TryPutLiquid(stack, owncontentStack, litres));
			if (num > 0)
			{
				TryTakeContent(blockSel.Position, num);
				DoLiquidMovedEffects(byPlayer, contentStack, num, EnumLiquidDirection.Fill);
				return true;
			}
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public void DoLiquidMovedEffects(IPlayer player, ItemStack contentStack, int moved, EnumLiquidDirection dir)
	{
		if (player != null)
		{
			WaterTightContainableProps containableProps = GetContainableProps(contentStack);
			float num = (float)moved / (containableProps?.ItemsPerLitre ?? 1f);
			(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			api.World.PlaySoundAt((dir != EnumLiquidDirection.Fill) ? (containableProps?.PourSound ?? ((AssetLocation)"sounds/effect/water-pour.ogg")) : (containableProps?.FillSound ?? ((AssetLocation)"sounds/effect/water-fill.ogg")), player.Entity, player, randomizePitch: true, 16f, GameMath.Clamp(num / 5f, 0.35f, 1f));
			api.World.SpawnCubeParticles(player.Entity.Pos.AheadCopy(0.25).XYZ.Add(0.0, player.Entity.SelectionBox.Y2 / 2f, 0.0), contentStack, 0.75f, (int)num * 2, 0.45f);
		}
	}

	protected override void tryEatBegin(ItemSlot slot, EntityAgent byEntity, ref EnumHandHandling handling, string eatSound = "eat", int eatSoundRepeats = 1)
	{
		base.tryEatBegin(slot, byEntity, ref handling, "drink", 4);
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel != null && api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage blockEntityGroundStorage)
		{
			ItemSlot slotAt = blockEntityGroundStorage.GetSlotAt(blockSel);
			if (!slotAt.Empty && slotAt.Itemstack.Collectible is ILiquidInterface)
			{
				return;
			}
		}
		if (blockSel == null || byEntity.Controls.ShiftKey)
		{
			if (byEntity.Controls.ShiftKey)
			{
				base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			}
			if (handHandling != EnumHandHandling.PreventDefaultAction && CanDrinkFrom && GetNutritionProperties(byEntity.World, itemslot.Itemstack, byEntity) != null)
			{
				tryEatBegin(itemslot, byEntity, ref handHandling, "drink", 4);
			}
			else if (!byEntity.Controls.ShiftKey)
			{
				base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			}
			return;
		}
		if (AllowHeldLiquidTransfer)
		{
			IPlayer player = (byEntity as EntityPlayer)?.Player;
			ItemStack content = GetContent(itemslot.Itemstack);
			WaterTightContainableProps waterTightContainableProps = ((content == null) ? null : GetContentProps(content));
			Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
			if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
			{
				byEntity.World.BlockAccessor.MarkBlockDirty(blockSel.Position.AddCopy(blockSel.Face));
				player?.InventoryManager.ActiveHotbarSlot?.MarkDirty();
				return;
			}
			if (!TryFillFromBlock(itemslot, byEntity, blockSel.Position))
			{
				if (block is BlockLiquidContainerTopOpened blockLiquidContainerTopOpened)
				{
					if (blockLiquidContainerTopOpened.TryPutLiquid(blockSel.Position, content, blockLiquidContainerTopOpened.CapacityLitres) > 0)
					{
						TryTakeContent(itemslot.Itemstack, 1);
						byEntity.World.PlaySoundAt(waterTightContainableProps?.FillSpillSound ?? ((AssetLocation)"sounds/block/water"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, player);
					}
				}
				else if (byEntity.Controls.CtrlKey)
				{
					SpillContents(itemslot, byEntity, blockSel);
				}
			}
		}
		if (CanDrinkFrom && GetNutritionProperties(byEntity.World, itemslot.Itemstack, byEntity) != null)
		{
			tryEatBegin(itemslot, byEntity, ref handHandling, "drink", 4);
		}
		else if (AllowHeldLiquidTransfer || CanDrinkFrom)
		{
			handHandling = EnumHandHandling.PreventDefaultAction;
		}
	}

	protected override bool tryEatStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, ItemStack spawnParticleStack = null)
	{
		return base.tryEatStep(secondsUsed, slot, byEntity, GetContent(slot.Itemstack));
	}

	protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
	{
		FoodNutritionProperties nutritionProperties = GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);
		if (byEntity.World is IServerWorldAccessor && nutritionProperties != null && secondsUsed >= 0.95f)
		{
			float num = 1f;
			float currentLitres = GetCurrentLitres(slot.Itemstack);
			float val = currentLitres * (float)slot.StackSize;
			if (currentLitres > num)
			{
				nutritionProperties.Satiety /= currentLitres;
				nutritionProperties.Health /= currentLitres;
			}
			float spoilState = UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish)?.TransitionLevel ?? 0f;
			float num2 = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, byEntity);
			float num3 = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, byEntity);
			byEntity.ReceiveSaturation(nutritionProperties.Satiety * num2, nutritionProperties.FoodCategory);
			IPlayer player = null;
			if (byEntity is EntityPlayer)
			{
				player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			float num4 = Math.Min(num, val);
			TryTakeLiquid(slot.Itemstack, num4 / (float)slot.Itemstack.StackSize);
			float num5 = nutritionProperties.Health * num3;
			float num6 = byEntity.WatchedAttributes.GetFloat("intoxication");
			byEntity.WatchedAttributes.SetFloat("intoxication", Math.Min(1.1f, num6 + nutritionProperties.Intoxication));
			if (num5 != 0f)
			{
				byEntity.ReceiveDamage(new DamageSource
				{
					Source = EnumDamageSource.Internal,
					Type = ((num5 > 0f) ? EnumDamageType.Heal : EnumDamageType.Poison)
				}, Math.Abs(num5));
			}
			slot.MarkDirty();
			player.InventoryManager.BroadcastHotbarSlot();
		}
	}

	public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
	{
		ItemStack content = GetContent(itemstack);
		WaterTightContainableProps waterTightContainableProps = ((content == null) ? null : GetContainableProps(content));
		if (waterTightContainableProps?.NutritionPropsPerLitre != null)
		{
			FoodNutritionProperties foodNutritionProperties = waterTightContainableProps.NutritionPropsPerLitre.Clone();
			float num = (float)content.StackSize / waterTightContainableProps.ItemsPerLitre;
			foodNutritionProperties.Health *= num;
			foodNutritionProperties.Satiety *= num;
			foodNutritionProperties.EatenStack = new JsonItemStack();
			foodNutritionProperties.EatenStack.ResolvedItemstack = itemstack.Clone();
			foodNutritionProperties.EatenStack.ResolvedItemstack.StackSize = 1;
			(foodNutritionProperties.EatenStack.ResolvedItemstack.Collectible as BlockLiquidContainerBase).SetContent(foodNutritionProperties.EatenStack.ResolvedItemstack, null);
			return foodNutritionProperties;
		}
		return base.GetNutritionProperties(world, itemstack, forEntity);
	}

	public bool TryFillFromBlock(ItemSlot itemslot, EntityAgent byEntity, BlockPos pos)
	{
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		Block block = byEntity.World.BlockAccessor.GetBlock(pos, 3);
		JsonObject attributes = block.Attributes;
		if (attributes != null && !attributes["waterTightContainerProps"].Exists)
		{
			return false;
		}
		WaterTightContainableProps waterTightContainableProps = block.Attributes?["waterTightContainerProps"]?.AsObject<WaterTightContainableProps>();
		if (waterTightContainableProps?.WhenFilled == null || !waterTightContainableProps.Containable)
		{
			return false;
		}
		waterTightContainableProps.WhenFilled.Stack.Resolve(byEntity.World, "liquidcontainerbase");
		if (GetCurrentLitres(itemslot.Itemstack) >= CapacityLitres)
		{
			return false;
		}
		ItemStack contentStack = waterTightContainableProps.WhenFilled.Stack.ResolvedItemstack;
		if (contentStack == null)
		{
			return false;
		}
		contentStack = contentStack.Clone();
		contentStack.StackSize = 999999;
		int num = SplitStackAndPerformAction(byEntity, itemslot, (ItemStack stack) => TryPutLiquid(stack, contentStack, CapacityLitres));
		if (num > 0)
		{
			DoLiquidMovedEffects(player, contentStack, num, EnumLiquidDirection.Fill);
		}
		return true;
	}

	public virtual void TryFillFromBlock(EntityItem byEntityItem, BlockPos pos)
	{
		IWorldAccessor world = byEntityItem.World;
		Block block = world.BlockAccessor.GetBlock(pos);
		JsonObject attributes = block.Attributes;
		if (attributes != null && !attributes["waterTightContainerProps"].Exists)
		{
			return;
		}
		WaterTightContainableProps waterTightContainableProps = block.Attributes?["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
		if (waterTightContainableProps?.WhenFilled == null || !waterTightContainableProps.Containable)
		{
			return;
		}
		if (waterTightContainableProps.WhenFilled.Stack.ResolvedItemstack == null)
		{
			waterTightContainableProps.WhenFilled.Stack.Resolve(world, "liquidcontainerbase");
		}
		ItemStack whenFilledStack = waterTightContainableProps.WhenFilled.Stack.ResolvedItemstack;
		ItemStack content = GetContent(byEntityItem.Itemstack);
		if (content == null || (content.Equals(world, whenFilledStack, GlobalConstants.IgnoredStackAttributes) && GetCurrentLitres(byEntityItem.Itemstack) < CapacityLitres))
		{
			whenFilledStack.StackSize = 999999;
			if (SplitStackAndPerformAction(byEntityItem, byEntityItem.Slot, (ItemStack stack) => TryPutLiquid(stack, whenFilledStack, CapacityLitres)) > 0)
			{
				world.PlaySoundAt(waterTightContainableProps.FillSound, pos, -0.4);
			}
		}
	}

	private bool SpillContents(ItemSlot containerSlot, EntityAgent byEntity, BlockSelection blockSel)
	{
		BlockPos position = blockSel.Position;
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		IBlockAccessor blockAccessor = byEntity.World.BlockAccessor;
		BlockPos blockPos = blockSel.Position.AddCopy(blockSel.Face);
		ItemStack contentStack = GetContent(containerSlot.Itemstack);
		WaterTightContainableProps contentProps = GetContentProps(containerSlot.Itemstack);
		if (contentProps == null || !contentProps.AllowSpill || contentProps.WhenSpilled == null)
		{
			return false;
		}
		if (!byEntity.World.Claims.TryAccess(player, blockPos, EnumBlockAccessFlags.BuildOrBreak))
		{
			return false;
		}
		WaterTightContainableProps.EnumSpilledAction enumSpilledAction = contentProps.WhenSpilled.Action;
		float currentLitres = GetCurrentLitres(containerSlot.Itemstack);
		if (currentLitres > 0f && currentLitres < 10f)
		{
			enumSpilledAction = WaterTightContainableProps.EnumSpilledAction.DropContents;
		}
		if (enumSpilledAction == WaterTightContainableProps.EnumSpilledAction.PlaceBlock)
		{
			Block block = byEntity.World.GetBlock(contentProps.WhenSpilled.Stack.Code);
			if (contentProps.WhenSpilled.StackByFillLevel != null)
			{
				contentProps.WhenSpilled.StackByFillLevel.TryGetValue((int)currentLitres, out var value);
				if (value != null)
				{
					block = byEntity.World.GetBlock(value.Code);
				}
			}
			if (!blockAccessor.GetBlock(position).DisplacesLiquids(blockAccessor, position))
			{
				blockAccessor.SetBlock(block.BlockId, position, 2);
				blockAccessor.TriggerNeighbourBlockUpdate(position);
				block.OnNeighbourBlockChange(byEntity.World, position, blockPos);
				blockAccessor.MarkBlockDirty(position);
			}
			else
			{
				if (blockAccessor.GetBlock(blockPos).DisplacesLiquids(blockAccessor, position))
				{
					return false;
				}
				blockAccessor.SetBlock(block.BlockId, blockPos, 2);
				blockAccessor.TriggerNeighbourBlockUpdate(blockPos);
				block.OnNeighbourBlockChange(byEntity.World, blockPos, position);
				blockAccessor.MarkBlockDirty(blockPos);
			}
		}
		if (enumSpilledAction == WaterTightContainableProps.EnumSpilledAction.DropContents)
		{
			contentProps.WhenSpilled.Stack.Resolve(byEntity.World, "liquidcontainerbasespill");
			ItemStack itemStack = contentProps.WhenSpilled.Stack.ResolvedItemstack.Clone();
			itemStack.StackSize = contentStack.StackSize;
			byEntity.World.SpawnItemEntity(itemStack, blockSel.Position.ToVec3d().Add(blockSel.HitPosition));
		}
		int moved = SplitStackAndPerformAction(byEntity, containerSlot, delegate(ItemStack stack)
		{
			SetContent(stack, null);
			return contentStack.StackSize;
		});
		DoLiquidMovedEffects(player, contentStack, moved, EnumLiquidDirection.Pour);
		return true;
	}

	public int SplitStackAndPerformAction(Entity byEntity, ItemSlot slot, System.Func<ItemStack, int> action)
	{
		if (slot.Itemstack == null)
		{
			return 0;
		}
		if (slot.Itemstack.StackSize == 1)
		{
			int num = action(slot.Itemstack);
			if (num > 0)
			{
				_ = slot.Itemstack.Collectible.MaxStackSize;
				EntityPlayer obj = byEntity as EntityPlayer;
				if (obj == null)
				{
					return num;
				}
				obj.WalkInventory(delegate(ItemSlot pslot)
				{
					if (pslot.Empty || pslot is ItemSlotCreative || pslot.StackSize == pslot.Itemstack.Collectible.MaxStackSize)
					{
						return true;
					}
					int mergableQuantity = slot.Itemstack.Collectible.GetMergableQuantity(slot.Itemstack, pslot.Itemstack, EnumMergePriority.DirectMerge);
					if (mergableQuantity == 0)
					{
						return true;
					}
					BlockLiquidContainerBase obj3 = slot.Itemstack.Collectible as BlockLiquidContainerBase;
					BlockLiquidContainerBase blockLiquidContainerBase = pslot.Itemstack.Collectible as BlockLiquidContainerBase;
					if ((obj3?.GetContent(slot.Itemstack)?.StackSize).GetValueOrDefault() != (blockLiquidContainerBase?.GetContent(pslot.Itemstack)?.StackSize).GetValueOrDefault())
					{
						return true;
					}
					slot.Itemstack.StackSize += mergableQuantity;
					pslot.TakeOut(mergableQuantity);
					slot.MarkDirty();
					pslot.MarkDirty();
					return true;
				});
			}
			return num;
		}
		ItemStack itemStack = slot.Itemstack.Clone();
		itemStack.StackSize = 1;
		int num2 = action(itemStack);
		if (num2 > 0)
		{
			slot.TakeOut(1);
			EntityPlayer obj2 = byEntity as EntityPlayer;
			if (obj2 == null || !obj2.Player.InventoryManager.TryGiveItemstack(itemStack, slotNotifyEffect: true))
			{
				api.World.SpawnItemEntity(itemStack, byEntity.SidedPos.XYZ);
			}
			slot.MarkDirty();
		}
		return num2;
	}

	public override void OnGroundIdle(EntityItem entityItem)
	{
		base.OnGroundIdle(entityItem);
		IWorldAccessor world = entityItem.World;
		if (world.Side != EnumAppSide.Server)
		{
			return;
		}
		if (entityItem.Swimming && world.Rand.NextDouble() < 0.03)
		{
			TryFillFromBlock(entityItem, entityItem.SidedPos.AsBlockPos);
		}
		if (!entityItem.Swimming || !(world.Rand.NextDouble() < 0.01))
		{
			return;
		}
		ItemStack[] contents = GetContents(world, entityItem.Itemstack);
		if (!MealMeshCache.ContentsRotten(contents))
		{
			return;
		}
		for (int i = 0; i < contents.Length; i++)
		{
			if (contents[i] != null && contents[i].StackSize > 0 && contents[i].Collectible.Code.Path == "rot")
			{
				world.SpawnItemEntity(contents[i], entityItem.ServerPos.XYZ);
			}
		}
		SetContent(entityItem.Itemstack, null);
	}

	public override void AddExtraHeldItemInfoPostMaterial(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
	{
		GetContentInfo(inSlot, dsc, world);
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		float currentLitres = GetCurrentLitres(pos);
		StringBuilder stringBuilder = new StringBuilder();
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer blockEntityContainer)
		{
			if (currentLitres <= 0f)
			{
				stringBuilder.AppendLine(Lang.Get("Empty"));
			}
			else
			{
				ItemSlot itemSlot = blockEntityContainer.Inventory[GetContainerSlotId(pos)];
				ItemStack itemstack = itemSlot.Itemstack;
				string text = Lang.Get(itemstack.Collectible.Code.Domain + ":incontainer-" + itemstack.Class.ToString().ToLowerInvariant() + "-" + itemstack.Collectible.Code.Path);
				stringBuilder.AppendLine(Lang.Get("Contents:"));
				stringBuilder.AppendLine(" " + Lang.Get("{0} litres of {1}", currentLitres, text));
				string text2 = PerishableInfoCompact(api, itemSlot, 0f, withStackName: false);
				if (text2.Length > 2)
				{
					stringBuilder.AppendLine(text2.Substring(2));
				}
			}
		}
		StringBuilder stringBuilder2 = new StringBuilder();
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior blockBehavior in blockBehaviors)
		{
			stringBuilder2.Append(blockBehavior.GetPlacedBlockInfo(world, pos, forPlayer));
		}
		if (stringBuilder2.Length > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(stringBuilder2.ToString());
		}
		return stringBuilder.ToString();
	}

	public virtual void GetContentInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
	{
		float currentLitres = GetCurrentLitres(inSlot.Itemstack);
		ItemStack content = GetContent(inSlot.Itemstack);
		if (currentLitres <= 0f)
		{
			dsc.AppendLine(Lang.Get("Empty"));
			return;
		}
		string text = Lang.Get(content.Collectible.Code.Domain + ":incontainer-" + content.Class.ToString().ToLowerInvariant() + "-" + content.Collectible.Code.Path);
		dsc.AppendLine(Lang.Get("{0} litres of {1}", currentLitres, text));
		ItemSlot contentInDummySlot = GetContentInDummySlot(inSlot, content);
		TransitionState[] array = content.Collectible.UpdateAndGetTransitionStates(api.World, contentInDummySlot);
		if (array != null && !contentInDummySlot.Empty)
		{
			bool flag = false;
			TransitionState[] array2 = array;
			foreach (TransitionState state in array2)
			{
				flag |= AppendPerishableInfoText(contentInDummySlot, dsc, world, state, flag) > 0f;
			}
		}
	}

	public override void TryMergeStacks(ItemStackMergeOperation op)
	{
		op.MovableQuantity = GetMergableQuantity(op.SinkSlot.Itemstack, op.SourceSlot.Itemstack, op.CurrentPriority);
		if (op.MovableQuantity == 0 || !op.SinkSlot.CanTakeFrom(op.SourceSlot, op.CurrentPriority))
		{
			return;
		}
		ItemStack content = GetContent(op.SinkSlot.Itemstack);
		ItemStack content2 = GetContent(op.SourceSlot.Itemstack);
		if (content == null && content2 == null)
		{
			base.TryMergeStacks(op);
			return;
		}
		if (content == null || content2 == null)
		{
			op.MovableQuantity = 0;
			return;
		}
		if (!content.Equals(op.World, content2, GlobalConstants.IgnoredStackAttributes))
		{
			op.MovableQuantity = 0;
			return;
		}
		float val = GetCurrentLitres(op.SourceSlot.Itemstack) * (float)op.SourceSlot.StackSize;
		float num = GetCurrentLitres(op.SinkSlot.Itemstack) * (float)op.SinkSlot.StackSize;
		float valueOrDefault = ((float)op.SourceSlot.StackSize * (op.SourceSlot.Itemstack.Collectible as BlockLiquidContainerBase)?.CapacityLitres).GetValueOrDefault();
		float valueOrDefault2 = ((float)op.SinkSlot.StackSize * (op.SinkSlot.Itemstack.Collectible as BlockLiquidContainerBase)?.CapacityLitres).GetValueOrDefault();
		if (valueOrDefault == 0f || valueOrDefault2 == 0f)
		{
			base.TryMergeStacks(op);
			return;
		}
		if (GetCurrentLitres(op.SourceSlot.Itemstack) == GetCurrentLitres(op.SinkSlot.Itemstack))
		{
			if (op.MovableQuantity > 0)
			{
				base.TryMergeStacks(op);
			}
			else
			{
				op.MovedQuantity = 0;
			}
			return;
		}
		if (op.CurrentPriority == EnumMergePriority.DirectMerge)
		{
			float num2 = Math.Min(valueOrDefault2 - num, val);
			int num3 = TryPutLiquid(op.SinkSlot.Itemstack, content2, num2 / (float)op.SinkSlot.StackSize);
			DoLiquidMovedEffects(op.ActingPlayer, content, num3, EnumLiquidDirection.Pour);
			num3 *= op.SinkSlot.StackSize;
			TryTakeContent(op.SourceSlot.Itemstack, (int)(0.51f + (float)num3 / (float)op.SourceSlot.StackSize));
			op.SourceSlot.MarkDirty();
			op.SinkSlot.MarkDirty();
		}
		op.MovableQuantity = 0;
	}

	public override bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
	{
		JsonObject jsonObject = ingredient.RecipeAttributes;
		if (jsonObject == null || !jsonObject.Exists || jsonObject == null || !jsonObject["requiresContent"].Exists)
		{
			jsonObject = gridRecipe.Attributes?["liquidContainerProps"];
		}
		if (jsonObject == null || !jsonObject.Exists)
		{
			return base.MatchesForCrafting(inputStack, gridRecipe, ingredient);
		}
		string domainAndPath = jsonObject["requiresContent"]["code"].AsString();
		string text = jsonObject["requiresContent"]["type"].AsString();
		ItemStack content = GetContent(inputStack);
		if (content == null)
		{
			return false;
		}
		float num = jsonObject["requiresLitres"].AsFloat();
		int num2 = (int)((GetContainableProps(content)?.ItemsPerLitre ?? 1f) * num) / inputStack.StackSize;
		bool num3 = content.Class.ToString().ToLowerInvariant() == text.ToLowerInvariant();
		bool flag = WildcardUtil.Match(new AssetLocation(domainAndPath), content.Collectible.Code);
		bool flag2 = content.StackSize >= num2;
		return num3 && flag && flag2;
	}

	public override void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
	{
		JsonObject jsonObject = fromIngredient.RecipeAttributes;
		if (jsonObject == null || !jsonObject.Exists || jsonObject == null || !jsonObject["requiresContent"].Exists)
		{
			jsonObject = gridRecipe.Attributes?["liquidContainerProps"];
		}
		if (jsonObject == null || !jsonObject.Exists)
		{
			base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
			return;
		}
		ItemStack? content = GetContent(stackInSlot.Itemstack);
		float num = jsonObject["requiresLitres"].AsFloat();
		int quantityItems = (int)((GetContainableProps(content)?.ItemsPerLitre ?? 1f) * num / (float)stackInSlot.StackSize);
		if (jsonObject.IsTrue("consumeContainer"))
		{
			stackInSlot.Itemstack.StackSize -= quantity;
			if (stackInSlot.Itemstack.StackSize <= 0)
			{
				stackInSlot.Itemstack = null;
				stackInSlot.MarkDirty();
			}
		}
		else
		{
			TryTakeContent(stackInSlot.Itemstack, quantityItems);
		}
	}

	public static string PerishableInfoCompact(ICoreAPI Api, ItemSlot contentSlot, float ripenRate, bool withStackName = true)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (withStackName)
		{
			stringBuilder.Append(contentSlot.Itemstack.GetName());
		}
		TransitionState[] array = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(Api.World, contentSlot);
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				string value = ", ";
				TransitionState transitionState = array[i];
				TransitionableProperties props = transitionState.Props;
				float transitionRateMul = contentSlot.Itemstack.Collectible.GetTransitionRateMul(Api.World, contentSlot, props.Type);
				if (transitionRateMul <= 0f)
				{
					continue;
				}
				float transitionLevel = transitionState.TransitionLevel;
				float num = transitionState.FreshHoursLeft / transitionRateMul;
				switch (props.Type)
				{
				case EnumTransitionType.Perish:
				{
					stringBuilder.Append(value);
					if (transitionLevel > 0f)
					{
						stringBuilder.Append(Lang.Get("{0}% spoiled", (int)Math.Round(transitionLevel * 100f)));
						break;
					}
					double num3 = Api.World.Calendar.HoursPerDay;
					if ((double)num / num3 >= (double)Api.World.Calendar.DaysPerYear)
					{
						stringBuilder.Append(Lang.Get("fresh for {0} years", Math.Round((double)num / num3 / (double)Api.World.Calendar.DaysPerYear, 1)));
					}
					else if ((double)num > num3)
					{
						stringBuilder.Append(Lang.Get("fresh for {0} days", Math.Round((double)num / num3, 1)));
					}
					else
					{
						stringBuilder.Append(Lang.Get("fresh for {0} hours", Math.Round(num, 1)));
					}
					break;
				}
				case EnumTransitionType.Ripen:
				{
					stringBuilder.Append(value);
					if (transitionLevel > 0f)
					{
						stringBuilder.Append(Lang.Get("{1:0.#} days left to ripen ({0}%)", (int)Math.Round(transitionLevel * 100f), (transitionState.TransitionHours - transitionState.TransitionedHours) / Api.World.Calendar.HoursPerDay / ripenRate));
						break;
					}
					double num2 = Api.World.Calendar.HoursPerDay;
					if ((double)num / num2 >= (double)Api.World.Calendar.DaysPerYear)
					{
						stringBuilder.Append(Lang.Get("will ripen in {0} years", Math.Round((double)num / num2 / (double)Api.World.Calendar.DaysPerYear, 1)));
					}
					else if ((double)num > num2)
					{
						stringBuilder.Append(Lang.Get("will ripen in {0} days", Math.Round((double)num / num2, 1)));
					}
					else
					{
						stringBuilder.Append(Lang.Get("will ripen in {0} hours", Math.Round(num, 1)));
					}
					break;
				}
				}
			}
		}
		return stringBuilder.ToString();
	}
}
