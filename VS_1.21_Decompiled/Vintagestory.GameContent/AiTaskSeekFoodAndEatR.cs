using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskSeekFoodAndEatR : AiTaskBaseR
{
	protected struct FailedAttempt
	{
		public long LastTryMs;

		public int Count;
	}

	protected long lastPOISearchTotalMs;

	protected long stuckAtMs;

	protected bool stuck;

	protected float currentEatTime;

	protected Dictionary<IAnimalFoodSource, FailedAttempt> failedSeekTargets = new Dictionary<IAnimalFoodSource, FailedAttempt>();

	protected bool soundPlayed;

	protected bool eatAnimationStarted;

	protected float quantityEaten;

	protected POIRegistry poiRegistry;

	protected IAnimalFoodSource? targetPoi;

	protected EntityBehaviorMultiplyBase? multiplyBaseBehavior;

	private readonly Cuboidd cuboidBuffer = new Cuboidd();

	private AiTaskSeekFoodAndEatConfig Config => GetConfig<AiTaskSeekFoodAndEatConfig>();

	public AiTaskSeekFoodAndEatR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		poiRegistry = entity.Api.ModLoader.GetModSystem<POIRegistry>() ?? throw new ArgumentException("Could not find POIRegistry modsystem");
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskSeekFoodAndEatConfig>(entity, taskConfig, aiConfig);
		entity.WatchedAttributes.SetBool("doesEat", value: true);
	}

	public override void AfterInitialize()
	{
		base.AfterInitialize();
		multiplyBaseBehavior = entity.GetBehavior<EntityBehaviorMultiplyBase>();
	}

	public override bool ShouldExecute()
	{
		if (lastPOISearchTotalMs + Config.PoiSearchCooldown > entity.World.ElapsedMilliseconds)
		{
			return false;
		}
		if (!PreconditionsSatisficed())
		{
			return false;
		}
		if (multiplyBaseBehavior != null && !multiplyBaseBehavior.ShouldEat && entity.World.Rand.NextDouble() >= (double)Config.ChanceToSeekFoodWithoutEating)
		{
			return false;
		}
		targetPoi = null;
		lastPOISearchTotalMs = entity.World.ElapsedMilliseconds;
		if (Config.EatLooseItems)
		{
			partitionUtil.WalkEntities(entity.ServerPos.XYZ, Config.LooseItemsSearchRange, delegate(Entity target)
			{
				if (target is EntityItem entityItem && SuitableFoodSource(entityItem.Itemstack))
				{
					targetPoi = new LooseItemFoodSource(entityItem);
					return false;
				}
				return true;
			}, EnumEntitySearchType.Inanimate);
		}
		if (targetPoi == null && Config.EatFoodSources)
		{
			targetPoi = poiRegistry.GetNearestPoi(entity.ServerPos.XYZ, Config.PoiSearchRange, delegate(IPointOfInterest poi)
			{
				if (poi.Type != Config.PoiType)
				{
					return false;
				}
				FailedAttempt value;
				return (poi is IAnimalFoodSource animalFoodSource && animalFoodSource.IsSuitableFor(entity, Config.Diet) && (!failedSeekTargets.TryGetValue(animalFoodSource, out value) || value.Count < Config.SeekPoiMaxAttempts || value.LastTryMs < world.ElapsedMilliseconds - Config.SeekPoiRetryCooldown)) ? true : false;
			}) as IAnimalFoodSource;
		}
		return targetPoi != null;
	}

	public override void StartExecute()
	{
		if (targetPoi == null)
		{
			stopTask = true;
			return;
		}
		base.StartExecute();
		stuckAtMs = long.MinValue;
		stuck = false;
		soundPlayed = false;
		currentEatTime = 0f;
		pathTraverser.NavigateTo_Async(targetPoi.Position, Config.MoveSpeed, MinDistanceToTarget() - 0.1f, OnGoalReached, OnStuck, null, 1000, 1, Config.AiCreatureType);
		eatAnimationStarted = false;
	}

	public override bool ContinueExecute(float dt)
	{
		if (!base.ContinueExecute(dt))
		{
			return false;
		}
		if (targetPoi == null)
		{
			return false;
		}
		FastVec3d fastVec3d = default(FastVec3d).Set(targetPoi.Position);
		pathTraverser.CurrentTarget.X = fastVec3d.X;
		pathTraverser.CurrentTarget.Y = fastVec3d.Y;
		pathTraverser.CurrentTarget.Z = fastVec3d.Z;
		cuboidBuffer.Set(entity.SelectionBox.ToDouble().Translate(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z));
		double num = cuboidBuffer.ShortestDistanceFrom(fastVec3d.X, fastVec3d.Y, fastVec3d.Z);
		float num2 = MinDistanceToTarget();
		if (num <= (double)num2)
		{
			if (!EatTheTarget(dt))
			{
				return false;
			}
		}
		else if (!pathTraverser.Active)
		{
			float x = (float)base.Rand.NextDouble() * 0.3f - 0.15f;
			float z = (float)base.Rand.NextDouble() * 0.3f - 0.15f;
			if (!pathTraverser.NavigateTo(targetPoi.Position.AddCopy(x, 0f, z), Config.MoveSpeed, num2 - 0.15f, OnGoalReached, OnStuck, null, giveUpWhenNoPath: false, 500, 1, Config.AiCreatureType))
			{
				return false;
			}
		}
		if (stuck && (float)entity.World.ElapsedMilliseconds > (float)stuckAtMs + Config.EatTimeSec * 1000f)
		{
			return false;
		}
		return true;
	}

	public override void FinishExecute(bool cancelled)
	{
		double num = cooldownUntilTotalHours;
		base.FinishExecute(cancelled);
		EntityBehaviorMultiply behavior = entity.GetBehavior<EntityBehaviorMultiply>();
		if (behavior != null && behavior.PortionsLeftToEat > 0f && !behavior.IsPregnant)
		{
			cooldownUntilTotalHours = num + Config.MinCooldownHours + entity.World.Rand.NextDouble() * (Config.MaxCooldownHours - Config.MinCooldownHours);
		}
		else
		{
			cooldownUntilTotalHours = entity.Api.World.Calendar.TotalHours + Config.MinCooldownHours + entity.World.Rand.NextDouble() * (Config.MaxCooldownHours - Config.MinCooldownHours);
		}
		pathTraverser.Stop();
		if (Config.EatAnimationMeta != null)
		{
			entity.AnimManager.StopAnimation(Config.EatAnimationMeta.Code);
		}
		if (cancelled)
		{
			cooldownUntilTotalHours = 0.0;
		}
		if (quantityEaten < 1f)
		{
			cooldownUntilTotalHours = 0.0;
		}
		else
		{
			quantityEaten = 0f;
		}
	}

	public override bool CanContinueExecute()
	{
		return pathTraverser.Ready;
	}

	protected virtual float MinDistanceToTarget()
	{
		return Math.Max(Config.ExtraTargetDistance, entity.SelectionBox.XSize / 2f + 0.05f);
	}

	protected virtual bool SuitableFoodSource(ItemStack itemStack)
	{
		return Config.Diet?.Matches(itemStack) ?? true;
	}

	protected virtual void OnStuck()
	{
		if (targetPoi != null)
		{
			stuckAtMs = entity.World.ElapsedMilliseconds;
			stuck = true;
			failedSeekTargets.TryGetValue(targetPoi, out var value);
			value.Count++;
			value.LastTryMs = world.ElapsedMilliseconds;
			failedSeekTargets[targetPoi] = value;
		}
	}

	protected virtual void OnGoalReached()
	{
		if (targetPoi != null)
		{
			pathTraverser.Active = true;
			failedSeekTargets.Remove(targetPoi);
		}
	}

	protected virtual bool EatTheTarget(float dt)
	{
		if (targetPoi == null)
		{
			return false;
		}
		pathTraverser.Stop();
		if (Config.AnimationMeta != null)
		{
			entity.AnimManager.StopAnimation(Config.AnimationMeta.Code);
		}
		if (multiplyBaseBehavior != null && !multiplyBaseBehavior.ShouldEat)
		{
			return false;
		}
		if (!targetPoi.IsSuitableFor(entity, Config.Diet))
		{
			return false;
		}
		if (Config.EatAnimationMeta != null && !eatAnimationStarted)
		{
			entity.AnimManager.StartAnimation((targetPoi is LooseItemFoodSource && Config.EatAnimationMetaLooseItems != null) ? Config.EatAnimationMetaLooseItems : Config.EatAnimationMeta);
			eatAnimationStarted = true;
		}
		currentEatTime += dt;
		if (targetPoi is LooseItemFoodSource looseItemFoodSource)
		{
			entity.World.SpawnCubeParticles(targetPoi.Position, looseItemFoodSource.ItemStack, 0.25f, 1, 0.25f + 0.5f * (float)entity.World.Rand.NextDouble());
		}
		if (currentEatTime > Config.EatTimeSoundSec && !soundPlayed)
		{
			soundPlayed = true;
			if (Config.EatSound != null)
			{
				entity.World.PlaySoundAt(Config.EatSound, entity, null, randomizePitch: true, Config.EatSoundRange, Config.EatSoundVolume);
			}
		}
		if (currentEatTime >= Config.EatTimeSec)
		{
			ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("hunger");
			if (treeAttribute == null)
			{
				treeAttribute = (ITreeAttribute)(entity.WatchedAttributes["hunger"] = new TreeAttribute());
			}
			if (Config.DoConsumePortion)
			{
				float num = targetPoi.ConsumeOnePortion(entity);
				float num2 = num * Config.SaturationPerPortion;
				quantityEaten += num;
				treeAttribute.SetFloat("saturation", num2 + treeAttribute.GetFloat("saturation"));
				entity.WatchedAttributes.SetDouble("lastMealEatenTotalHours", entity.World.Calendar.TotalHours);
				entity.WatchedAttributes.MarkPathDirty("hunger");
			}
			else
			{
				quantityEaten = 1f;
			}
			failedSeekTargets.Remove(targetPoi);
			return false;
		}
		return true;
	}
}
