using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockCookedContainer : BlockCookedContainerBase, IInFirepitRendererSupplier, IContainedMeshSource, IContainedInteractable, IGroundStoredParticleEmitter
{
	public static SimpleParticleProperties smokeHeld;

	public static SimpleParticleProperties foodSparks;

	private Vec3d gsSmokePos = new Vec3d(0.5, 0.3125, 0.5);

	private WorldInteraction[]? interactions;

	private MealMeshCache? meshCache;

	public float yoff = 2.5f;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (CollisionBoxes[0] != null)
		{
			gsSmokePos.Y = CollisionBoxes[0].MaxY;
		}
		ICoreClientAPI capi = api as ICoreClientAPI;
		if (capi == null)
		{
			return;
		}
		meshCache = api.ModLoader.GetModSystem<MealMeshCache>();
		interactions = ObjectCacheUtil.GetOrCreate(api, "cookedContainerBlockInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject collectible in api.World.Collectibles)
			{
				JsonObject attributes = collectible.Attributes;
				if (attributes != null && attributes.IsTrue("mealContainer"))
				{
					List<ItemStack> handBookStacks = collectible.GetHandBookStacks(capi);
					if (handBookStacks != null)
					{
						list.AddRange(handBookStacks);
					}
				}
			}
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-cookedcontainer-takefood",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-cookedcontainer-pickup",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right
				}
			};
		});
	}

	static BlockCookedContainer()
	{
		smokeHeld = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(50, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.05f, 0.1f, -0.05f), new Vec3f(0.05f, 0.15f, 0.05f), 1.5f, 0f, 0.25f, 0.35f, EnumParticleModel.Quad);
		smokeHeld.SelfPropelled = true;
		smokeHeld.AddPos.Set(0.1, 0.1, 0.1);
		foodSparks = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(255, 83, 233, 255), new Vec3d(), new Vec3d(), new Vec3f(-3f, 1f, -3f), new Vec3f(3f, 8f, 3f), 0.5f, 1f, 0.25f, 0.25f);
		foodSparks.VertexFlags = 0;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		if (meshCache == null)
		{
			meshCache = capi.ModLoader.GetModSystem<MealMeshCache>();
		}
		CookingRecipe cookingRecipe = GetCookingRecipe(capi.World, itemstack);
		ItemStack[] nonEmptyContents = GetNonEmptyContents(capi.World, itemstack);
		MultiTextureMeshRef orCreateMealInContainerMeshRef = meshCache.GetOrCreateMealInContainerMeshRef(this, cookingRecipe, nonEmptyContents, new Vec3f(0f, yoff / 16f, 0f));
		if (orCreateMealInContainerMeshRef != null)
		{
			renderinfo.ModelRef = orCreateMealInContainerMeshRef;
		}
	}

	public virtual string GetMeshCacheKey(ItemStack itemstack)
	{
		return meshCache.GetMealHashCode(itemstack).ToString();
	}

	public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos? forBlockPos = null)
	{
		return meshCache.GenMealInContainerMesh(this, GetCookingRecipe(api.World, itemstack), GetNonEmptyContents(api.World, itemstack), new Vec3f(0f, yoff / 16f, 0f));
	}

	public override TransitionState[]? UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
	{
		ItemStack itemstack = inslot.Itemstack;
		if (itemstack == null)
		{
			return null;
		}
		ItemStack[] nonEmptyContents = GetNonEmptyContents(world, itemstack);
		ItemStack[] array = nonEmptyContents;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].StackSize *= (int)Math.Max(1f, itemstack.Attributes.TryGetFloat("quantityServings") ?? 1f);
		}
		SetContents(itemstack, nonEmptyContents);
		TransitionState[] result = base.UpdateAndGetTransitionStates(world, inslot);
		nonEmptyContents = GetNonEmptyContents(world, itemstack);
		if (nonEmptyContents.Length == 0 || MealMeshCache.ContentsRotten(nonEmptyContents))
		{
			for (int j = 0; j < nonEmptyContents.Length; j++)
			{
				TransitionableProperties transitionableProperties = nonEmptyContents[j].Collectible.GetTransitionableProperties(world, nonEmptyContents[j], null)?.FirstOrDefault((TransitionableProperties props) => props.Type == EnumTransitionType.Perish);
				if (transitionableProperties != null)
				{
					nonEmptyContents[j] = nonEmptyContents[j].Collectible.OnTransitionNow(GetContentInDummySlot(inslot, nonEmptyContents[j]), transitionableProperties);
				}
			}
			SetContents(itemstack, nonEmptyContents);
			itemstack.Attributes.RemoveAttribute("recipeCode");
			itemstack.Attributes.RemoveAttribute("quantityServings");
		}
		array = nonEmptyContents;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].StackSize /= (int)Math.Max(1f, itemstack.Attributes.TryGetFloat("quantityServings") ?? 1f);
		}
		SetContents(itemstack, nonEmptyContents);
		if (nonEmptyContents.Length == 0)
		{
			string text = Attributes?["emptiedBlockCode"]?.AsString();
			if (text != null)
			{
				Block block = world.GetBlock(new AssetLocation(text));
				if (block != null)
				{
					inslot.Itemstack = new ItemStack(block);
					inslot.MarkDirty();
				}
			}
		}
		return result;
	}

	public override string GetHeldItemName(ItemStack? itemStack)
	{
		ItemStack[] contents = GetContents(api.World, itemStack);
		string text = itemStack?.Collectible.GetCollectibleInterface<IBlockMealContainer>()?.GetRecipeCode(api.World, itemStack);
		string text2 = Lang.Get("mealrecipe-name-" + text + "-in-container");
		if (text == null)
		{
			if (MealMeshCache.ContentsRotten(contents))
			{
				text2 = Lang.Get("Rotten Food");
			}
			else if (contents.Length != 0)
			{
				text2 = contents[0].GetName();
			}
		}
		return Lang.GetMatching(Code?.Domain + ":" + ItemClass.Name() + "-" + Code?.Path, text2);
	}

	public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
	{
		if (byEntity.World.Side == EnumAppSide.Client && GetTemperature(byEntity.World, slot.Itemstack) > 50f && byEntity.World.Rand.NextDouble() < 0.07)
		{
			float num = 0.35f;
			if ((byEntity as EntityPlayer)?.Player is IClientPlayer { CameraMode: not EnumCameraMode.FirstPerson })
			{
				num = 0f;
			}
			Vec3d vec3d = byEntity.Pos.XYZ.Add(0.0, byEntity.LocalEyePos.Y - 0.5, 0.0).Ahead(0.33000001311302185, byEntity.Pos.Pitch, byEntity.Pos.Yaw).Ahead(num, 0f, byEntity.Pos.Yaw + (float)Math.PI / 2f);
			smokeHeld.MinPos = vec3d.AddCopy(-0.05, 0.1, -0.05);
			byEntity.World.SpawnParticles(smokeHeld);
		}
	}

	public override void OnGroundIdle(EntityItem entityItem)
	{
		base.OnGroundIdle(entityItem);
		IWorldAccessor world = entityItem.World;
		if (world.Side != EnumAppSide.Server || !entityItem.Swimming || !(world.Rand.NextDouble() < 0.01))
		{
			return;
		}
		ItemStack[] contents = GetContents(world, entityItem.Itemstack);
		if (MealMeshCache.ContentsRotten(contents))
		{
			for (int i = 0; i < contents.Length; i++)
			{
				if (contents[i] != null && contents[i].StackSize > 0 && contents[i].Collectible.Code.Path == "rot")
				{
					world.SpawnItemEntity(contents[i], entityItem.ServerPos.XYZ);
				}
			}
		}
		else
		{
			ItemStack item = contents[world.Rand.Next(contents.Length)];
			world.SpawnCubeParticles(entityItem.ServerPos.XYZ, item, 0.3f, 25);
		}
		Block block = world.GetBlock(new AssetLocation(Attributes["emptiedBlockCode"].AsString()));
		entityItem.Itemstack = new ItemStack(block);
		entityItem.WatchedAttributes.MarkPathDirty("itemstack");
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = base.OnPickBlock(world, pos);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCookedContainer blockEntityCookedContainer)
		{
			ItemStack[] nonEmptyContentStacks = blockEntityCookedContainer.GetNonEmptyContentStacks();
			SetContents(blockEntityCookedContainer.RecipeCode, blockEntityCookedContainer.QuantityServings, itemStack, nonEmptyContentStacks);
			float temperature = ((nonEmptyContentStacks.Length != 0) ? nonEmptyContentStacks[0].Collectible.GetTemperature(world, nonEmptyContentStacks[0]) : 0f);
			SetTemperature(world, itemStack, temperature, delayCooldown: false);
		}
		return itemStack;
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (!(api is ICoreClientAPI coreClientAPI) || !coreClientAPI.ObjectCache.TryGetValue("cookedMeshRefs", out var value))
		{
			return;
		}
		if (value is Dictionary<int, MultiTextureMeshRef> dictionary)
		{
			foreach (KeyValuePair<int, MultiTextureMeshRef> item in dictionary)
			{
				item.Value.Dispose();
			}
		}
		coreClientAPI.ObjectCache.Remove("cookedMeshRefs");
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1] { OnPickBlock(world, pos) };
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		ItemStack cookedContStack = inSlot.Itemstack;
		if (cookedContStack == null)
		{
			return;
		}
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		float temperature = GetTemperature(world, cookedContStack);
		if (temperature > 20f)
		{
			dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int)temperature));
		}
		CookingRecipe mealRecipe = GetMealRecipe(world, cookedContStack);
		float num = cookedContStack.Attributes.GetFloat("quantityServings");
		ItemStack[] nonEmptyContents = GetNonEmptyContents(world, cookedContStack);
		if (mealRecipe != null)
		{
			string outputName = mealRecipe.GetOutputName(world, nonEmptyContents);
			string key = ((mealRecipe.CooksInto == null) ? "{0} servings of {1}" : "nonfood-portions");
			dsc.AppendLine(Lang.Get(key, Math.Round(num, 1), outputName));
		}
		BlockMeal[]? array = BlockMeal.AllMealBowls(api);
		string text = ((array == null) ? null : array[0]?.GetContentNutritionFacts(api.World, inSlot, nonEmptyContents, null));
		if (text != null && mealRecipe?.CooksInto == null)
		{
			dsc.AppendLine(text);
		}
		if (cookedContStack.Attributes.GetBool("timeFrozen"))
		{
			return;
		}
		DummyInventory dummyInventory = new DummyInventory(api);
		ItemSlot dummySlotForFirstPerishableStack = BlockCrock.GetDummySlotForFirstPerishableStack(api.World, nonEmptyContents, null, dummyInventory);
		dummyInventory.OnAcquireTransitionSpeed += delegate(EnumTransitionType transType, ItemStack stack, float mul)
		{
			float num2 = mul * GetContainingTransitionModifierContained(world, inSlot, transType);
			if (inSlot.Inventory != null)
			{
				num2 *= inSlot.Inventory.GetTransitionSpeedMul(transType, cookedContStack);
			}
			return num2;
		};
		dummySlotForFirstPerishableStack.Itemstack?.Collectible.AppendPerishableInfoText(dummySlotForFirstPerishableStack, dsc, world);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (!activeHotbarSlot.Empty)
		{
			JsonObject attributes = activeHotbarSlot.Itemstack.Collectible.Attributes;
			if (attributes != null && attributes.IsTrue("mealContainer"))
			{
				return (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityCookedContainer)?.ServeInto(byPlayer, activeHotbarSlot) ?? false;
			}
		}
		ItemStack itemstack = OnPickBlock(world, blockSel.Position);
		if (byPlayer.InventoryManager.TryGiveItemstack(itemstack, slotNotifyEffect: true))
		{
			world.BlockAccessor.SetBlock(0, blockSel.Position);
			world.PlaySoundAt(Sounds.Place, byPlayer, byPlayer);
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel != null)
		{
			BlockPos position = blockSel.Position;
			Block block = byEntity.World.BlockAccessor.GetBlock(position);
			if (block is BlockClayOven)
			{
				return;
			}
			Block block2 = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
			if (block2 != null && block2.Attributes?.IsTrue("mealContainer") == true)
			{
				if (byEntity.Controls.ShiftKey)
				{
					ServeIntoBowl(block2, blockSel.Position, slot, byEntity.World);
					handHandling = EnumHandHandling.PreventDefault;
				}
				return;
			}
			float num = (float)(slot.Itemstack?.Attributes.GetDecimal("quantityServings") ?? 0.0);
			if (block is BlockGroundStorage)
			{
				if (!byEntity.Controls.ShiftKey || !(api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage blockEntityGroundStorage))
				{
					return;
				}
				ItemSlot slotAt = blockEntityGroundStorage.GetSlotAt(blockSel);
				if (slotAt == null || slotAt.Empty)
				{
					return;
				}
				JsonObject itemAttributes = slotAt.Itemstack.ItemAttributes;
				if (itemAttributes != null && itemAttributes.IsTrue("mealContainer"))
				{
					if (num > 0f)
					{
						ServeIntoStack(slotAt, slot, byEntity.World);
						slotAt.MarkDirty();
						blockEntityGroundStorage.updateMeshes();
						blockEntityGroundStorage.MarkDirty(redrawOnClient: true);
					}
					handHandling = EnumHandHandling.PreventDefault;
					return;
				}
			}
		}
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (capi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityCookedContainer blockEntityCookedContainer)
		{
			ItemStack[] nonEmptyContentStacks = blockEntityCookedContainer.GetNonEmptyContentStacks();
			if (nonEmptyContentStacks != null && nonEmptyContentStacks.Length != 0)
			{
				ItemStack itemStack = nonEmptyContentStacks[capi.World.Rand.Next(nonEmptyContentStacks.Length)];
				if (capi.World.Rand.NextDouble() < 0.4)
				{
					return capi.BlockTextureAtlas.GetRandomColor(Textures["ceramic"].Baked.TextureSubId);
				}
				if (itemStack.Class == EnumItemClass.Block)
				{
					return itemStack.Block.GetRandomColor(capi, pos, facing, rndIndex);
				}
				return capi.ItemTextureAtlas.GetRandomColor(itemStack.Item.FirstTexture.Baked.TextureSubId, rndIndex);
			}
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}

	public override int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
	{
		ItemStack[] nonEmptyContents = GetNonEmptyContents(capi.World, stack);
		if (nonEmptyContents.Length == 0)
		{
			return base.GetRandomColor(capi, stack);
		}
		ItemStack itemStack = nonEmptyContents[capi.World.Rand.Next(nonEmptyContents.Length)];
		if (capi.World.Rand.NextDouble() < 0.4)
		{
			return capi.BlockTextureAtlas.GetRandomColor(Textures["ceramic"].Baked.TextureSubId);
		}
		return itemStack.Collectible.GetRandomColor(capi, stack);
	}

	public IInFirepitRenderer GetRendererWhenInFirepit(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
	{
		return new PotInFirepitRenderer(api as ICoreClientAPI, stack, firepit.Pos, forOutputSlot);
	}

	public EnumFirepitModel GetDesiredFirepitModel(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
	{
		return EnumFirepitModel.Wide;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append<WorldInteraction>(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override bool OnSmeltAttempt(InventoryBase inventorySmelting)
	{
		JsonObject attributes = Attributes;
		if (attributes != null && attributes["isDirtyPot"]?.AsBool() == true)
		{
			InventorySmelting inventorySmelting2 = (InventorySmelting)inventorySmelting;
			int num = (int)((float)(inventorySmelting2[1].Itemstack?.Attributes.GetDecimal("quantityServings") ?? 0.0) + 0.001f);
			if (num > 0)
			{
				ItemStack[] nonEmptyContents = GetNonEmptyContents(api.World, inventorySmelting2[1].Itemstack);
				if (nonEmptyContents.Length != 0)
				{
					nonEmptyContents[0].StackSize = num;
					inventorySmelting2.CookingSlots[0].Itemstack = nonEmptyContents[0];
				}
			}
			AssetLocation blockCode = AssetLocation.CreateOrNull(Attributes?["emptiedBlockCode"]?.AsString());
			Block block = api.World.GetBlock(blockCode);
			if (block != null)
			{
				inventorySmelting2[1].Itemstack = new ItemStack(block);
			}
			return true;
		}
		return false;
	}

	public virtual bool ShouldSpawnGSParticles(IWorldAccessor world, ItemStack stack)
	{
		return world.Rand.NextDouble() < (double)((GetTemperature(world, stack) - 50f) / 160f / 8f);
	}

	public virtual void DoSpawnGSParticles(IAsyncParticleManager manager, BlockPos pos, Vec3f offset)
	{
		smokeHeld.MinPos = pos.ToVec3d().AddCopy(gsSmokePos).AddCopy(offset);
		manager.Spawn(smokeHeld);
	}
}
