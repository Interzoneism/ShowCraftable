using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockMeal : BlockContainer, IBlockMealContainer, IContainedMeshSource, IContainedInteractable, IContainedCustomName, IGroundStoredParticleEmitter, IHandBookPageCodeProvider
{
	private MealMeshCache? meshCache;

	private Vec3d gsSmokePos = new Vec3d(0.5, 0.125, 0.5);

	protected bool displayContentsInfo = true;

	protected virtual bool PlacedBlockEating => true;

	public static BlockMeal[]? AllMealBowls(ICoreAPI api)
	{
		return ObjectCacheUtil.TryGet<BlockMeal[]>(api, "allMealBowls");
	}

	public static BlockMeal RandomMealBowl(ICoreAPI api)
	{
		BlockMeal[] array = AllMealBowls(api);
		return array[api.World.Rand.Next(array.Length)];
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (CollisionBoxes[0] != null)
		{
			gsSmokePos.Y = CollisionBoxes[0].MaxY;
		}
		ObjectCacheUtil.GetOrCreate(api, "allMealBowls", delegate
		{
			List<BlockMeal> list = new List<BlockMeal>();
			foreach (Block block in api.World.Blocks)
			{
				if (block is BlockMeal item && block.FirstCodePart().Contains("bowl"))
				{
					list.Add(item);
				}
			}
			return list.ToArray();
		});
		meshCache = api.ModLoader.GetModSystem<MealMeshCache>();
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
	{
		return "eat";
	}

	public virtual float[] GetNutritionHealthMul(BlockPos? pos, ItemSlot slot, EntityAgent? forEntity)
	{
		return new float[2] { 1f, 1f };
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
			BlockCookedContainer.smokeHeld.MinPos = vec3d.AddCopy(-0.05, 0.1, -0.05);
			byEntity.World.SpawnParticles(BlockCookedContainer.smokeHeld);
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (!tryHeldBeginEatMeal(slot, byEntity, ref handHandling))
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		return tryHeldContinueEatMeal(secondsUsed, slot, byEntity);
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		tryFinishEatMeal(secondsUsed, slot, byEntity, handleAllServingsConsumed: true);
	}

	protected virtual bool tryHeldBeginEatMeal(ItemSlot slot, EntityAgent byEntity, ref EnumHandHandling handHandling)
	{
		if (!byEntity.Controls.ShiftKey && GetContentNutritionProperties(api.World, slot, byEntity) != null)
		{
			byEntity.World.RegisterCallback(delegate
			{
				if (byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
				{
					byEntity.PlayEntitySound("eat", (byEntity as EntityPlayer)?.Player);
				}
			}, 500);
			handHandling = EnumHandHandling.PreventDefault;
			return true;
		}
		return false;
	}

	protected bool tryPlacedBeginEatMeal(ItemSlot slot, IPlayer byPlayer)
	{
		if (GetContentNutritionProperties(api.World, slot, byPlayer.Entity) != null)
		{
			api.World.RegisterCallback(delegate
			{
				if (byPlayer.Entity.Controls.HandUse == EnumHandInteract.BlockInteract)
				{
					byPlayer.Entity.PlayEntitySound("eat", byPlayer);
				}
			}, 500);
			byPlayer.Entity.StartAnimation("eat");
			return true;
		}
		return false;
	}

	protected virtual bool tryHeldContinueEatMeal(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
	{
		if (GetContentNutritionProperties(byEntity.World, slot, byEntity) == null)
		{
			return false;
		}
		Vec3d vec3d = byEntity.Pos.AheadCopy(0.4000000059604645).XYZ.Add(byEntity.LocalEyePos);
		vec3d.Y -= 0.4000000059604645;
		IPlayer dualCallByPlayer = (byEntity as EntityPlayer)?.Player;
		if (secondsUsed > 0.5f && (int)(30f * secondsUsed) % 7 == 1)
		{
			ItemStack[] nonEmptyContents = GetNonEmptyContents(byEntity.World, slot.Itemstack);
			if (nonEmptyContents.Length != 0)
			{
				ItemStack item = nonEmptyContents[byEntity.World.Rand.Next(nonEmptyContents.Length)];
				byEntity.World.SpawnCubeParticles(vec3d, item, 0.3f, 4, 1f, dualCallByPlayer);
			}
		}
		if (byEntity.World is IClientWorldAccessor)
		{
			ModelTransform modelTransform = new ModelTransform();
			modelTransform.Origin.Set(1.1f, 0.5f, 0.5f);
			modelTransform.EnsureDefaultValues();
			modelTransform.Translation.X -= Math.Min(1.7f, secondsUsed * 4f * 1.8f) / FpHandTransform.ScaleXYZ.X;
			modelTransform.Translation.Y += Math.Min(0.4f, secondsUsed * 1.8f) / FpHandTransform.ScaleXYZ.X;
			modelTransform.Scale = 1f + Math.Min(0.5f, secondsUsed * 4f * 1.8f) / FpHandTransform.ScaleXYZ.X;
			modelTransform.Rotation.X += Math.Min(40f, secondsUsed * 350f * 0.75f) / FpHandTransform.ScaleXYZ.X;
			if (secondsUsed > 0.5f)
			{
				modelTransform.Translation.Y += GameMath.Sin(30f * secondsUsed) / 10f / FpHandTransform.ScaleXYZ.Y;
			}
			return secondsUsed <= 1.5f;
		}
		return true;
	}

	protected bool tryPlacedContinueEatMeal(float secondsUsed, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (byPlayer.Entity.Controls.ShiftKey && GetContentNutritionProperties(api.World, slot, byPlayer.Entity) != null)
		{
			ItemStack itemstack = slot.Itemstack;
			if (itemstack != null)
			{
				if (api.Side == EnumAppSide.Client)
				{
					ModelTransform modelTransform = new ModelTransform();
					modelTransform.Origin.Set(1.1f, 0.5f, 0.5f);
					modelTransform.EnsureDefaultValues();
					if (ItemClass == EnumItemClass.Item)
					{
						if (secondsUsed > 0.5f)
						{
							modelTransform.Translation.X = GameMath.Sin(30f * secondsUsed) / 10f;
						}
						modelTransform.Translation.Z += 0f - Math.Min(1.6f, secondsUsed * 4f * 1.57f);
						modelTransform.Translation.Y += Math.Min(0.15f, secondsUsed * 2f);
						modelTransform.Rotation.Y -= Math.Min(85f, secondsUsed * 350f * 1.5f);
						modelTransform.Rotation.X += Math.Min(40f, secondsUsed * 350f * 0.75f);
						modelTransform.Rotation.Z += Math.Min(30f, secondsUsed * 350f * 0.75f);
					}
					else
					{
						modelTransform.Translation.X -= Math.Min(1.7f, secondsUsed * 4f * 1.8f) / FpHandTransform.ScaleXYZ.X;
						modelTransform.Translation.Y += Math.Min(0.4f, secondsUsed * 1.8f) / FpHandTransform.ScaleXYZ.X;
						modelTransform.Scale = 1f + Math.Min(0.5f, secondsUsed * 4f * 1.8f) / FpHandTransform.ScaleXYZ.X;
						modelTransform.Rotation.X += Math.Min(40f, secondsUsed * 350f * 0.75f) / FpHandTransform.ScaleXYZ.X;
						if (secondsUsed > 0.5f)
						{
							modelTransform.Translation.Y += GameMath.Sin(30f * secondsUsed) / 10f / FpHandTransform.ScaleXYZ.Y;
						}
					}
					if (secondsUsed > 0.5f && (int)(30f * secondsUsed) % 7 == 1)
					{
						ItemStack[] nonEmptyContents = GetNonEmptyContents(api.World, itemstack);
						if (nonEmptyContents.Length != 0)
						{
							ItemStack item = nonEmptyContents[api.World.Rand.Next(nonEmptyContents.Length)];
							api.World.SpawnCubeParticles(blockSel.Position.ToVec3d().Add(0.5, 0.125, 0.5), item, 0.2f, 4, 0.5f);
						}
					}
					return secondsUsed <= 1.5f;
				}
				return true;
			}
		}
		return false;
	}

	protected virtual bool tryFinishEatMeal(float secondsUsed, ItemSlot slot, EntityAgent byEntity, bool handleAllServingsConsumed)
	{
		FoodNutritionProperties[] contentNutritionProperties = GetContentNutritionProperties(byEntity.World, slot, byEntity);
		if (byEntity.World.Side == EnumAppSide.Client || contentNutritionProperties == null || (double)secondsUsed < 1.45)
		{
			return false;
		}
		ItemStack itemstack = slot.Itemstack;
		if (itemstack != null)
		{
			IPlayer player = (byEntity as EntityPlayer)?.Player;
			if (player != null)
			{
				slot.MarkDirty();
				float quantityServings = GetQuantityServings(byEntity.World, itemstack);
				ItemStack[] nonEmptyContents = GetNonEmptyContents(api.World, itemstack);
				if (nonEmptyContents.Length == 0)
				{
					quantityServings = 0f;
				}
				else
				{
					string recipeCode = GetRecipeCode(api.World, itemstack);
					quantityServings = Consume(byEntity.World, player, slot, nonEmptyContents, quantityServings, string.IsNullOrEmpty(recipeCode));
				}
				if (quantityServings <= 0f)
				{
					if (handleAllServingsConsumed)
					{
						if (Attributes["eatenBlock"].Exists)
						{
							Block block = byEntity.World.GetBlock(new AssetLocation(Attributes["eatenBlock"].AsString()));
							if (slot.Empty || slot.StackSize == 1)
							{
								slot.Itemstack = new ItemStack(block);
							}
							else if (!player.InventoryManager.TryGiveItemstack(new ItemStack(block), slotNotifyEffect: true))
							{
								byEntity.World.SpawnItemEntity(new ItemStack(block), byEntity.SidedPos.XYZ);
							}
						}
						else
						{
							slot.TakeOut(1);
							slot.MarkDirty();
						}
					}
				}
				else if (slot.Empty || slot.StackSize == 1)
				{
					(itemstack.Collectible as BlockMeal)?.SetQuantityServings(byEntity.World, itemstack, quantityServings);
					slot.Itemstack = itemstack;
				}
				else
				{
					ItemStack itemStack = slot.TakeOut(1);
					(itemstack.Collectible as BlockMeal)?.SetQuantityServings(byEntity.World, itemStack, quantityServings);
					ItemStack itemstack2 = slot.Itemstack;
					slot.Itemstack = itemStack;
					if (!player.InventoryManager.TryGiveItemstack(itemstack2, slotNotifyEffect: true))
					{
						byEntity.World.SpawnItemEntity(itemstack2, byEntity.SidedPos.XYZ);
					}
				}
				return true;
			}
		}
		return false;
	}

	public bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			return false;
		}
		return tryPlacedBeginEatMeal(slot, byPlayer);
	}

	public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			return false;
		}
		return tryPlacedContinueEatMeal(secondsUsed, slot, byPlayer, blockSel);
	}

	public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (tryFinishEatMeal(secondsUsed, slot, byPlayer.Entity, handleAllServingsConsumed: true))
		{
			be.MarkDirty(redrawOnClient: true);
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!PlacedBlockEating)
		{
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		ItemStack itemStack = OnPickBlock(world, blockSel.Position);
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			if (byPlayer.InventoryManager.TryGiveItemstack(itemStack, slotNotifyEffect: true))
			{
				world.BlockAccessor.SetBlock(0, blockSel.Position);
				world.PlaySoundAt(Sounds.Place, byPlayer, byPlayer);
				return true;
			}
			return false;
		}
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityMeal blockEntityMeal)
		{
			DummySlot dummySlot = new DummySlot(itemStack, blockEntityMeal.inventory);
			dummySlot.MarkedDirty += () => true;
			return tryPlacedBeginEatMeal(dummySlot, byPlayer);
		}
		return false;
	}

	public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!PlacedBlockEating)
		{
			return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
		}
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			return false;
		}
		ItemStack stack = OnPickBlock(world, blockSel.Position);
		return tryPlacedContinueEatMeal(secondsUsed, new DummySlot(stack), byPlayer, blockSel);
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!PlacedBlockEating)
		{
			base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
		}
		if (!byPlayer.Entity.Controls.ShiftKey || !(world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityMeal blockEntityMeal))
		{
			return;
		}
		ItemStack itemStack = OnPickBlock(world, blockSel.Position);
		DummySlot dummySlot = new DummySlot(itemStack, blockEntityMeal.inventory);
		dummySlot.MarkedDirty += () => true;
		if (tryFinishEatMeal(secondsUsed, dummySlot, byPlayer.Entity, handleAllServingsConsumed: false))
		{
			float quantityServings = GetQuantityServings(world, itemStack);
			if (blockEntityMeal.QuantityServings <= 0f)
			{
				Block block = world.GetBlock(new AssetLocation(Attributes["eatenBlock"].AsString()));
				world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
			}
			else
			{
				blockEntityMeal.QuantityServings = quantityServings;
				blockEntityMeal.MarkDirty(redrawOnClient: true);
			}
		}
	}

	public virtual float Consume(IWorldAccessor world, IPlayer eatingPlayer, ItemSlot inSlot, ItemStack[] contentStacks, float remainingServings, bool mulwithStackSize)
	{
		float[] nutritionHealthMul = GetNutritionHealthMul(null, inSlot, eatingPlayer.Entity);
		FoodNutritionProperties[] contentNutritionProperties = GetContentNutritionProperties(world, inSlot, contentStacks, eatingPlayer.Entity, mulwithStackSize, nutritionHealthMul[0], nutritionHealthMul[1]);
		if (contentNutritionProperties == null)
		{
			return remainingServings;
		}
		float num = 0f;
		EntityBehaviorHunger behavior = eatingPlayer.Entity.GetBehavior<EntityBehaviorHunger>();
		if (behavior == null)
		{
			throw new Exception(eatingPlayer.Entity.Code.ToString() + "does not have EntityBehaviorHunger defined properly");
		}
		float num2 = behavior.MaxSaturation - behavior.Saturation;
		float num3 = 0f;
		foreach (FoodNutritionProperties foodNutritionProperties in contentNutritionProperties)
		{
			if (foodNutritionProperties != null)
			{
				num3 += foodNutritionProperties.Satiety;
			}
		}
		float val = GameMath.Clamp(num2 / Math.Max(1f, num3), 0f, 1f);
		float num4 = Math.Min(remainingServings, val);
		float num5 = inSlot.Itemstack?.Collectible.GetTemperature(world, inSlot.Itemstack) ?? 20f;
		EntityBehaviorBodyTemperature behavior2 = eatingPlayer.Entity.GetBehavior<EntityBehaviorBodyTemperature>();
		if (behavior2 != null && Math.Abs(num5 - behavior2.CurBodyTemperature) > 10f)
		{
			float num6 = Math.Min(1f, (num5 - behavior2.CurBodyTemperature) / 30f);
			behavior2.CurBodyTemperature += GameMath.Clamp(num3 * num4 / 80f * num6, 0f, 5f);
		}
		foreach (FoodNutritionProperties foodNutritionProperties2 in contentNutritionProperties)
		{
			if (foodNutritionProperties2 != null)
			{
				float num7 = num4;
				float num8 = num7 * foodNutritionProperties2.Satiety;
				float saturationLossDelay = Math.Min(1.3f, num7 * 3f) * 10f + num8 / 70f * 60f;
				eatingPlayer.Entity.ReceiveSaturation(num8, foodNutritionProperties2.FoodCategory, saturationLossDelay);
				if (foodNutritionProperties2.EatenStack?.ResolvedItemstack != null && !eatingPlayer.InventoryManager.TryGiveItemstack(foodNutritionProperties2.EatenStack.ResolvedItemstack.Clone(), slotNotifyEffect: true))
				{
					world.SpawnItemEntity(foodNutritionProperties2.EatenStack.ResolvedItemstack.Clone(), eatingPlayer.Entity.SidedPos.XYZ);
				}
				num += num7 * foodNutritionProperties2.Health;
			}
		}
		if (num != 0f)
		{
			eatingPlayer.Entity.ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Internal,
				Type = ((num > 0f) ? EnumDamageType.Heal : EnumDamageType.Poison)
			}, Math.Abs(num));
		}
		return Math.Max(0f, remainingServings - num4);
	}

	public override FoodNutritionProperties? GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
	{
		return null;
	}

	public static FoodNutritionProperties[] GetContentNutritionProperties(IWorldAccessor world, ItemSlot inSlot, ItemStack?[]? contentStacks, EntityAgent? forEntity, bool mulWithStacksize = false, float nutritionMul = 1f, float healthMul = 1f)
	{
		List<FoodNutritionProperties> list = new List<FoodNutritionProperties>();
		if (contentStacks != null)
		{
			ItemStack itemstack = inSlot.Itemstack;
			if (itemstack != null)
			{
				bool flag = itemstack.Attributes.GetBool("timeFrozen");
				foreach (ItemStack itemStack in contentStacks)
				{
					if (itemStack == null)
					{
						continue;
					}
					FoodNutritionProperties ingredientStackNutritionProperties = GetIngredientStackNutritionProperties(world, itemStack, forEntity);
					if (ingredientStackNutritionProperties != null)
					{
						float num = ((!mulWithStacksize) ? 1 : itemStack.StackSize);
						FoodNutritionProperties foodNutritionProperties = ingredientStackNutritionProperties.Clone();
						float spoilState = 0f;
						DummySlot inslot = new DummySlot(itemStack, inSlot.Inventory);
						if (!flag)
						{
							spoilState = itemStack.Collectible.UpdateAndGetTransitionState(world, inslot, EnumTransitionType.Perish)?.TransitionLevel ?? 0f;
						}
						float num2 = GlobalConstants.FoodSpoilageSatLossMul(spoilState, itemstack, forEntity);
						float num3 = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, itemstack, forEntity);
						foodNutritionProperties.Satiety *= num2 * nutritionMul * num;
						foodNutritionProperties.Health *= num3 * healthMul * num;
						list.Add(foodNutritionProperties);
					}
				}
				return list.ToArray();
			}
		}
		return list.ToArray();
	}

	public static FoodNutritionProperties? GetIngredientStackNutritionProperties(IWorldAccessor world, ItemStack? stack, EntityAgent? forEntity)
	{
		if (stack == null)
		{
			return null;
		}
		CollectibleObject collectible = stack.Collectible;
		ItemStack itemStack = collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack;
		WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(stack);
		FoodNutritionProperties foodNutritionProperties = containableProps?.NutritionPropsPerLitreWhenInMeal;
		if (containableProps != null && foodNutritionProperties != null)
		{
			foodNutritionProperties = foodNutritionProperties.Clone();
			float num = (float)stack.StackSize / containableProps.ItemsPerLitre;
			foodNutritionProperties.Health *= num;
			foodNutritionProperties.Satiety *= num;
		}
		if (foodNutritionProperties == null)
		{
			foodNutritionProperties = collectible.Attributes?["nutritionPropsWhenInMeal"]?.AsObject<FoodNutritionProperties>();
		}
		if (itemStack != null)
		{
			collectible = itemStack.Collectible;
			containableProps = BlockLiquidContainerBase.GetContainableProps(itemStack);
		}
		if (containableProps != null && foodNutritionProperties == null)
		{
			foodNutritionProperties = containableProps.NutritionPropsPerLitre;
			if (foodNutritionProperties != null)
			{
				foodNutritionProperties = foodNutritionProperties.Clone();
				float num2 = (float)stack.StackSize / containableProps.ItemsPerLitre;
				foodNutritionProperties.Health *= num2;
				foodNutritionProperties.Satiety *= num2;
			}
		}
		return foodNutritionProperties ?? collectible.GetNutritionProperties(world, stack, forEntity);
	}

	public FoodNutritionProperties[]? GetContentNutritionProperties(IWorldAccessor world, ItemSlot inSlot, EntityAgent forEntity)
	{
		ItemStack[] nonEmptyContents = GetNonEmptyContents(world, inSlot.Itemstack);
		if (nonEmptyContents == null || nonEmptyContents.Length == 0)
		{
			return null;
		}
		float[] nutritionHealthMul = GetNutritionHealthMul(null, inSlot, forEntity);
		return GetContentNutritionProperties(world, inSlot, nonEmptyContents, forEntity, GetRecipeCode(world, inSlot.Itemstack) == null, nutritionHealthMul[0], nutritionHealthMul[1]);
	}

	public virtual string GetContentNutritionFacts(IWorldAccessor world, ItemSlot inSlotorFirstSlot, ItemStack[] contentStacks, EntityAgent? forEntity, bool mulWithStacksize = false, float nutritionMul = 1f, float healthMul = 1f)
	{
		FoodNutritionProperties[] contentNutritionProperties = GetContentNutritionProperties(world, inSlotorFirstSlot, contentStacks, forEntity, mulWithStacksize, nutritionMul, healthMul);
		Dictionary<EnumFoodCategory, float> dictionary = new Dictionary<EnumFoodCategory, float>();
		float num = 0f;
		foreach (FoodNutritionProperties foodNutritionProperties in contentNutritionProperties)
		{
			if (foodNutritionProperties != null)
			{
				dictionary.TryGetValue(foodNutritionProperties.FoodCategory, out var value);
				num += foodNutritionProperties.Health;
				dictionary[foodNutritionProperties.FoodCategory] = value + foodNutritionProperties.Satiety;
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Lang.Get("Nutrition Facts"));
		foreach (KeyValuePair<EnumFoodCategory, float> item in dictionary)
		{
			stringBuilder.AppendLine(Lang.Get("nutrition-facts-line-satiety", Lang.Get("foodcategory-" + item.Key.ToString().ToLowerInvariant()), Math.Round(item.Value)));
		}
		if (num != 0f)
		{
			stringBuilder.AppendLine("- " + Lang.Get("Health: {0}{1} hp", (num > 0f) ? "+" : "", num));
		}
		return stringBuilder.ToString();
	}

	public string GetContentNutritionFacts(IWorldAccessor world, ItemSlot inSlot, EntityAgent? forEntity, bool mulWithStacksize = false)
	{
		float[] nutritionHealthMul = GetNutritionHealthMul(null, inSlot, forEntity);
		return GetContentNutritionFacts(world, inSlot, GetNonEmptyContents(world, inSlot.Itemstack), forEntity, mulWithStacksize, nutritionHealthMul[0], nutritionHealthMul[1]);
	}

	public void SetContents(string? recipeCode, ItemStack containerStack, ItemStack?[] stacks, float quantityServings = 1f)
	{
		base.SetContents(containerStack, stacks);
		if (recipeCode != null)
		{
			containerStack.Attributes.SetString("recipeCode", recipeCode);
		}
		containerStack.Attributes.SetFloat("quantityServings", quantityServings);
		if (stacks.Length != 0)
		{
			SetTemperature(api.World, containerStack, stacks[0]?.Collectible.GetTemperature(api.World, stacks[0]) ?? 20f);
		}
	}

	public float GetQuantityServings(IWorldAccessor world, ItemStack byItemStack)
	{
		return (float)byItemStack.Attributes.GetDecimal("quantityServings");
	}

	public void SetQuantityServings(IWorldAccessor world, ItemStack? byItemStack, float value)
	{
		if (byItemStack != null)
		{
			if (value <= 0f)
			{
				byItemStack.Attributes.RemoveAttribute("recipeCode");
				byItemStack.Attributes.RemoveAttribute("quantityServings");
				byItemStack.Attributes.RemoveAttribute("contents");
			}
			else
			{
				byItemStack.Attributes.SetFloat("quantityServings", value);
			}
		}
	}

	public string? GetRecipeCode(IWorldAccessor world, ItemStack? containerStack)
	{
		return containerStack?.Attributes.GetString("recipeCode");
	}

	public CookingRecipe? GetCookingRecipe(IWorldAccessor world, ItemStack? containerStack)
	{
		string recipeCode = GetRecipeCode(world, containerStack);
		return api.GetCookingRecipe(recipeCode);
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		MultiTextureMeshRef orCreateMealInContainerMeshRef = meshCache.GetOrCreateMealInContainerMeshRef(this, GetCookingRecipe(capi.World, itemstack), GetNonEmptyContents(capi.World, itemstack));
		if (orCreateMealInContainerMeshRef != null)
		{
			renderinfo.ModelRef = orCreateMealInContainerMeshRef;
		}
	}

	public virtual MeshData? GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos? forBlockPos = null)
	{
		if (!(api is ICoreClientAPI coreClientAPI))
		{
			return null;
		}
		return meshCache.GenMealInContainerMesh(this, GetCookingRecipe(coreClientAPI.World, itemstack), GetNonEmptyContents(coreClientAPI.World, itemstack));
	}

	public virtual string GetMeshCacheKey(ItemStack itemstack)
	{
		return meshCache.GetMealHashCode(itemstack).ToString();
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = base.OnPickBlock(world, pos);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityMeal blockEntityMeal)
		{
			SetContents(blockEntityMeal.RecipeCode, itemStack, blockEntityMeal.GetNonEmptyContentStacks(), blockEntityMeal.QuantityServings);
		}
		return itemStack;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return new BlockDropItemStack[1]
		{
			new BlockDropItemStack(handbookStack)
		};
	}

	public virtual string HandbookPageCodeForStack(IWorldAccessor world, ItemStack stack)
	{
		string recipeCode = GetRecipeCode(world, stack);
		if (recipeCode != null)
		{
			return "handbook-mealrecipe-" + recipeCode;
		}
		return GuiHandbookItemStackPage.PageCodeForStack(stack);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1] { OnPickBlock(world, pos) };
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		ItemStack mealStack = inSlot.Itemstack;
		if (mealStack == null)
		{
			return;
		}
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		float temperature = GetTemperature(world, mealStack);
		if (temperature > 20f)
		{
			dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int)temperature));
		}
		CookingRecipe cookingRecipe = GetCookingRecipe(world, mealStack);
		ItemStack[] nonEmptyContents = GetNonEmptyContents(world, mealStack);
		DummyInventory dummyInventory = new DummyInventory(api);
		ItemSlot dummySlotForFirstPerishableStack = BlockCrock.GetDummySlotForFirstPerishableStack(api.World, nonEmptyContents, null, dummyInventory);
		dummyInventory.OnAcquireTransitionSpeed += delegate(EnumTransitionType transType, ItemStack stack, float mul)
		{
			float num = mul * GetContainingTransitionModifierContained(world, inSlot, transType);
			if (inSlot.Inventory != null)
			{
				num *= inSlot.Inventory.GetTransitionSpeedMul(transType, mealStack);
			}
			return num;
		};
		dummySlotForFirstPerishableStack.Itemstack?.Collectible.AppendPerishableInfoText(dummySlotForFirstPerishableStack, dsc, world);
		float quantityServings = GetQuantityServings(world, mealStack);
		if (cookingRecipe != null)
		{
			if (Math.Round(quantityServings, 1) < 0.05)
			{
				dsc.AppendLine(Lang.Get("{1}% serving of {0}", cookingRecipe.GetOutputName(world, nonEmptyContents).UcFirst(), Math.Round(quantityServings * 100f, 0)));
			}
			else
			{
				dsc.AppendLine(Lang.Get("{0} serving of {1}", Math.Round(quantityServings, 1), cookingRecipe.GetOutputName(world, nonEmptyContents).UcFirst()));
			}
		}
		else if (mealStack.Attributes.HasAttribute("quantityServings"))
		{
			if (Math.Round(quantityServings, 1) < 0.05)
			{
				dsc.AppendLine(Lang.Get("meal-servingsleft-percent", Math.Round(quantityServings * 100f, 0)));
			}
			else
			{
				dsc.AppendLine(Lang.Get("{0} servings left", Math.Round(quantityServings, 1)));
			}
		}
		else if (displayContentsInfo && !MealMeshCache.ContentsRotten(nonEmptyContents))
		{
			dsc.AppendLine(Lang.Get("Contents: {0}", Lang.Get("meal-ingredientlist-" + nonEmptyContents.Length, nonEmptyContents.Select((ItemStack stack) => Lang.Get("{0}x {1}", stack.StackSize, stack.GetName())))));
		}
		if (!MealMeshCache.ContentsRotten(nonEmptyContents))
		{
			string contentNutritionFacts = GetContentNutritionFacts(world, inSlot, null, cookingRecipe == null);
			if (contentNutritionFacts != null)
			{
				dsc.Append(contentNutritionFacts);
			}
		}
	}

	public string GetContainedName(ItemSlot inSlot, int quantity)
	{
		return GetHeldItemName(inSlot.Itemstack);
	}

	public string GetContainedInfo(ItemSlot inSlot)
	{
		CookingRecipe cookingRecipe = GetCookingRecipe(api.World, inSlot.Itemstack);
		ItemStack[] nonEmptyContents = GetNonEmptyContents(api.World, inSlot.Itemstack);
		if (!(inSlot.Itemstack?.Block is BlockMeal blockMeal))
		{
			return Lang.Get("unknown");
		}
		string text = blockMeal.Attributes?["eatenBlock"].AsString();
		string name = new ItemStack((text == null) ? blockMeal : api.World.GetBlock(text)).GetName();
		if (nonEmptyContents.Length == 0)
		{
			return Lang.GetWithFallback("contained-empty-container", "{0} (Empty)", name);
		}
		string text2 = cookingRecipe?.GetOutputName(api.World, nonEmptyContents).UcFirst() ?? nonEmptyContents[0].GetName();
		float num = inSlot.Itemstack?.Attributes.GetFloat("quantityServings", 1f) ?? 1f;
		if (MealMeshCache.ContentsRotten(nonEmptyContents))
		{
			text2 = Lang.Get("Rotten Food");
			num = 1f;
		}
		return Lang.Get("contained-food-singleservingmax", Math.Round(num, 1), text2, name, PerishableInfoCompactContainer(api, inSlot));
	}

	public override void OnGroundIdle(EntityItem entityItem)
	{
		base.OnGroundIdle(entityItem);
		IWorldAccessor world = entityItem.World;
		if (world.Side != EnumAppSide.Server)
		{
			return;
		}
		Block block = world.GetBlock(new AssetLocation(Attributes["eatenBlock"].AsString("")));
		if (block == null || !entityItem.Swimming || !(world.Rand.NextDouble() < 0.01))
		{
			return;
		}
		ItemStack[] nonEmptyContents = GetNonEmptyContents(world, entityItem.Itemstack);
		if (MealMeshCache.ContentsRotten(nonEmptyContents))
		{
			for (int i = 0; i < nonEmptyContents.Length; i++)
			{
				if (nonEmptyContents[i] != null && nonEmptyContents[i].StackSize > 0 && nonEmptyContents[i].Collectible.Code.Path == "rot")
				{
					world.SpawnItemEntity(nonEmptyContents[i], entityItem.ServerPos.XYZ);
				}
			}
		}
		else
		{
			ItemStack item = nonEmptyContents[world.Rand.Next(nonEmptyContents.Length)];
			world.SpawnCubeParticles(entityItem.ServerPos.XYZ, item, 0.3f, 25);
		}
		entityItem.Itemstack = new ItemStack(block);
		entityItem.WatchedAttributes.MarkPathDirty("itemstack");
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (capi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer blockEntityContainer)
		{
			ItemStack[] nonEmptyContentStacks = blockEntityContainer.GetNonEmptyContentStacks(cloned: false);
			if (nonEmptyContentStacks != null && nonEmptyContentStacks.Length != 0)
			{
				return GetRandomContentColor(capi, nonEmptyContentStacks);
			}
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}

	public override int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
	{
		ItemStack[] nonEmptyContents = GetNonEmptyContents(capi.World, stack);
		if (nonEmptyContents == null || nonEmptyContents.Length == 0)
		{
			return base.GetRandomColor(capi, stack);
		}
		return GetRandomContentColor(capi, nonEmptyContents);
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
			AssetLocation assetLocation = AssetLocation.CreateOrNull(Attributes?["eatenBlock"]?.AsString());
			if ((object)assetLocation != null)
			{
				Block block = world.GetBlock(assetLocation);
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

	public virtual int GetRandomContentColor(ICoreClientAPI capi, ItemStack[] stacks)
	{
		ItemStack itemStack = stacks[capi.World.Rand.Next(stacks.Length)];
		return itemStack.Collectible.GetRandomColor(capi, itemStack);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return new WorldInteraction[2]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-meal-pickup",
				MouseButton = EnumMouseButton.Right
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-meal-eat",
				MouseButton = EnumMouseButton.Right,
				HotKeyCode = "shift"
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public virtual bool ShouldSpawnGSParticles(IWorldAccessor world, ItemStack stack)
	{
		return world.Rand.NextDouble() < (double)((GetTemperature(world, stack) - 50f) / 320f / 8f);
	}

	public virtual void DoSpawnGSParticles(IAsyncParticleManager manager, BlockPos pos, Vec3f offset)
	{
		BlockCookedContainer.smokeHeld.MinPos = pos.ToVec3d().AddCopy(gsSmokePos).AddCopy(offset);
		manager.Spawn(BlockCookedContainer.smokeHeld);
	}
}
