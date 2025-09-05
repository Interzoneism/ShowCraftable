using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class AiTaskUseInventory : AiTaskBase
{
	protected AssetLocation useSound;

	protected float useTime = 1f;

	protected float useTimeNow;

	protected bool soundPlayed;

	protected bool doConsumePortion = true;

	protected HashSet<EnumFoodCategory> eatItemCategories = new HashSet<EnumFoodCategory>();

	protected HashSet<AssetLocation> eatItemCodes = new HashSet<AssetLocation>();

	protected bool isEdible;

	public AiTaskUseInventory(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		JsonObject jsonObject = taskConfig["useSound"];
		if (jsonObject.Exists)
		{
			string text = jsonObject.AsString();
			if (text != null)
			{
				useSound = new AssetLocation(text).WithPathPrefix("sounds/");
			}
		}
		useTime = taskConfig["useTime"].AsFloat(1.5f);
		EnumFoodCategory[] array = taskConfig["eatItemCategories"].AsArray(Array.Empty<EnumFoodCategory>());
		foreach (EnumFoodCategory item in array)
		{
			eatItemCategories.Add(item);
		}
		AssetLocation[] array2 = taskConfig["eatItemCodes"].AsArray(Array.Empty<AssetLocation>());
		foreach (AssetLocation item2 in array2)
		{
			eatItemCodes.Add(item2);
		}
	}

	public override bool ShouldExecute()
	{
		if (entity.World.Rand.NextDouble() < 0.005)
		{
			return false;
		}
		if (cooldownUntilMs > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		if (cooldownUntilTotalHours > entity.World.Calendar.TotalHours)
		{
			return false;
		}
		if (!PreconditionsSatisifed())
		{
			return false;
		}
		EntityBehaviorMultiplyBase behavior = entity.GetBehavior<EntityBehaviorMultiplyBase>();
		if (behavior != null && !behavior.ShouldEat && entity.World.Rand.NextDouble() < 0.996)
		{
			return false;
		}
		ItemSlot leftHandItemSlot = entity.LeftHandItemSlot;
		if (leftHandItemSlot.Empty)
		{
			return false;
		}
		isEdible = false;
		EnumFoodCategory? enumFoodCategory = leftHandItemSlot.Itemstack.Collectible?.NutritionProps?.FoodCategory;
		if (enumFoodCategory.HasValue && eatItemCategories.Contains(enumFoodCategory.Value))
		{
			isEdible = true;
			return true;
		}
		AssetLocation assetLocation = leftHandItemSlot.Itemstack?.Collectible?.Code;
		if (assetLocation != null && eatItemCodes.Contains(assetLocation))
		{
			isEdible = true;
			return true;
		}
		if (!leftHandItemSlot.Empty)
		{
			entity.World.SpawnItemEntity(leftHandItemSlot.TakeOutWhole(), entity.ServerPos.XYZ);
		}
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		soundPlayed = false;
		useTimeNow = 0f;
	}

	public override bool ContinueExecute(float dt)
	{
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		useTimeNow += dt;
		if (useTimeNow > useTime * 0.75f && !soundPlayed)
		{
			soundPlayed = true;
			if (useSound != null)
			{
				entity.World.PlaySoundAt(useSound, entity, null, randomizePitch: true, 16f);
			}
		}
		if (entity.LeftHandItemSlot == null || entity.LeftHandItemSlot.Empty)
		{
			return false;
		}
		entity.World.SpawnCubeParticles(entity.ServerPos.XYZ, entity.LeftHandItemSlot.Itemstack, 0.25f, 1, 0.25f + 0.5f * (float)entity.World.Rand.NextDouble());
		if (useTimeNow >= useTime)
		{
			if (isEdible)
			{
				ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("hunger");
				if (treeAttribute == null)
				{
					treeAttribute = (ITreeAttribute)(entity.WatchedAttributes["hunger"] = new TreeAttribute());
				}
				if (doConsumePortion)
				{
					float num = 1f;
					treeAttribute.SetFloat("saturation", num + treeAttribute.GetFloat("saturation"));
				}
			}
			entity.LeftHandItemSlot.TakeOut(1);
			return false;
		}
		return true;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		if (cancelled)
		{
			cooldownUntilTotalHours = 0.0;
		}
	}
}
