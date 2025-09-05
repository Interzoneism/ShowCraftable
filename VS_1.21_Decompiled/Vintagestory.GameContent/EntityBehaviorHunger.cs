using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorHunger : EntityBehavior
{
	private ITreeAttribute hungerTree;

	private EntityAgent entityAgent;

	private float hungerCounter;

	private int sprintCounter;

	private long listenerId;

	private long lastMoveMs;

	private ICoreAPI api;

	private float detoxCounter;

	public float SaturationLossDelayFruit
	{
		get
		{
			return hungerTree.GetFloat("saturationlossdelayfruit");
		}
		set
		{
			hungerTree.SetFloat("saturationlossdelayfruit", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float SaturationLossDelayVegetable
	{
		get
		{
			return hungerTree.GetFloat("saturationlossdelayvegetable");
		}
		set
		{
			hungerTree.SetFloat("saturationlossdelayvegetable", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float SaturationLossDelayProtein
	{
		get
		{
			return hungerTree.GetFloat("saturationlossdelayprotein");
		}
		set
		{
			hungerTree.SetFloat("saturationlossdelayprotein", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float SaturationLossDelayGrain
	{
		get
		{
			return hungerTree.GetFloat("saturationlossdelaygrain");
		}
		set
		{
			hungerTree.SetFloat("saturationlossdelaygrain", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float SaturationLossDelayDairy
	{
		get
		{
			return hungerTree.GetFloat("saturationlossdelaydairy");
		}
		set
		{
			hungerTree.SetFloat("saturationlossdelaydairy", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float Saturation
	{
		get
		{
			return hungerTree.GetFloat("currentsaturation");
		}
		set
		{
			hungerTree.SetFloat("currentsaturation", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float MaxSaturation
	{
		get
		{
			return hungerTree.GetFloat("maxsaturation");
		}
		set
		{
			hungerTree.SetFloat("maxsaturation", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float FruitLevel
	{
		get
		{
			return hungerTree.GetFloat("fruitLevel");
		}
		set
		{
			hungerTree.SetFloat("fruitLevel", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float VegetableLevel
	{
		get
		{
			return hungerTree.GetFloat("vegetableLevel");
		}
		set
		{
			hungerTree.SetFloat("vegetableLevel", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float ProteinLevel
	{
		get
		{
			return hungerTree.GetFloat("proteinLevel");
		}
		set
		{
			hungerTree.SetFloat("proteinLevel", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float GrainLevel
	{
		get
		{
			return hungerTree.GetFloat("grainLevel");
		}
		set
		{
			hungerTree.SetFloat("grainLevel", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float DairyLevel
	{
		get
		{
			return hungerTree.GetFloat("dairyLevel");
		}
		set
		{
			hungerTree.SetFloat("dairyLevel", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public EntityBehaviorHunger(Entity entity)
		: base(entity)
	{
		entityAgent = entity as EntityAgent;
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		hungerTree = entity.WatchedAttributes.GetTreeAttribute("hunger");
		api = entity.World.Api;
		if (hungerTree == null)
		{
			entity.WatchedAttributes.SetAttribute("hunger", hungerTree = new TreeAttribute());
			Saturation = typeAttributes["currentsaturation"].AsFloat(1500f);
			MaxSaturation = typeAttributes["maxsaturation"].AsFloat(1500f);
			SaturationLossDelayFruit = 0f;
			SaturationLossDelayVegetable = 0f;
			SaturationLossDelayGrain = 0f;
			SaturationLossDelayProtein = 0f;
			SaturationLossDelayDairy = 0f;
			FruitLevel = 0f;
			VegetableLevel = 0f;
			GrainLevel = 0f;
			ProteinLevel = 0f;
			DairyLevel = 0f;
		}
		listenerId = entity.World.RegisterGameTickListener(SlowTick, 6000);
		UpdateNutrientHealthBoost();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		entity.World.UnregisterGameTickListener(listenerId);
	}

	public override void DidAttack(DamageSource source, EntityAgent targetEntity, ref EnumHandling handled)
	{
		ConsumeSaturation(3f);
	}

	public virtual void ConsumeSaturation(float amount)
	{
		ReduceSaturation(amount / 10f);
	}

	public override void OnEntityReceiveSaturation(float saturation, EnumFoodCategory foodCat = EnumFoodCategory.Unknown, float saturationLossDelay = 10f, float nutritionGainMultiplier = 1f)
	{
		float maxSaturation = MaxSaturation;
		bool flag = Saturation >= maxSaturation;
		Saturation = Math.Min(maxSaturation, Saturation + saturation);
		switch (foodCat)
		{
		case EnumFoodCategory.Fruit:
			if (!flag)
			{
				FruitLevel = Math.Min(maxSaturation, FruitLevel + saturation / 2.5f * nutritionGainMultiplier);
			}
			SaturationLossDelayFruit = Math.Max(SaturationLossDelayFruit, saturationLossDelay);
			break;
		case EnumFoodCategory.Vegetable:
			if (!flag)
			{
				VegetableLevel = Math.Min(maxSaturation, VegetableLevel + saturation / 2.5f * nutritionGainMultiplier);
			}
			SaturationLossDelayVegetable = Math.Max(SaturationLossDelayVegetable, saturationLossDelay);
			break;
		case EnumFoodCategory.Protein:
			if (!flag)
			{
				ProteinLevel = Math.Min(maxSaturation, ProteinLevel + saturation / 2.5f * nutritionGainMultiplier);
			}
			SaturationLossDelayProtein = Math.Max(SaturationLossDelayProtein, saturationLossDelay);
			break;
		case EnumFoodCategory.Grain:
			if (!flag)
			{
				GrainLevel = Math.Min(maxSaturation, GrainLevel + saturation / 2.5f * nutritionGainMultiplier);
			}
			SaturationLossDelayGrain = Math.Max(SaturationLossDelayGrain, saturationLossDelay);
			break;
		case EnumFoodCategory.Dairy:
			if (!flag)
			{
				DairyLevel = Math.Min(maxSaturation, DairyLevel + saturation / 2.5f * nutritionGainMultiplier);
			}
			SaturationLossDelayDairy = Math.Max(SaturationLossDelayDairy, saturationLossDelay);
			break;
		}
		UpdateNutrientHealthBoost();
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity is EntityPlayer)
		{
			EntityPlayer entityPlayer = (EntityPlayer)entity;
			EnumGameMode currentGameMode = entity.World.PlayerByUid(entityPlayer.PlayerUID).WorldData.CurrentGameMode;
			detox(deltaTime);
			if (currentGameMode == EnumGameMode.Creative || currentGameMode == EnumGameMode.Spectator)
			{
				return;
			}
			if (entityPlayer.Controls.TriesToMove || entityPlayer.Controls.Jump || entityPlayer.Controls.LeftMouseDown || entityPlayer.Controls.RightMouseDown)
			{
				lastMoveMs = entity.World.ElapsedMilliseconds;
			}
		}
		if (entityAgent != null && entityAgent.Controls.Sprint)
		{
			sprintCounter++;
		}
		hungerCounter += deltaTime;
		if (hungerCounter > 10f)
		{
			bool num = entity.World.ElapsedMilliseconds - lastMoveMs > 3000;
			float num2 = entity.Api.World.Calendar.SpeedOfTime * entity.Api.World.Calendar.CalendarSpeedMul;
			float num3 = GlobalConstants.HungerSpeedModifier / 30f;
			if (num)
			{
				num3 /= 4f;
			}
			num3 *= 1.2f * (8f + (float)sprintCounter / 15f) / 10f;
			num3 *= entity.Stats.GetBlended("hungerrate");
			ReduceSaturation(num3 * num2);
			hungerCounter = 0f;
			sprintCounter = 0;
			detox(deltaTime);
		}
	}

	private void detox(float dt)
	{
		detoxCounter += dt;
		if (detoxCounter > 1f)
		{
			float num = entity.WatchedAttributes.GetFloat("intoxication");
			if (num > 0f)
			{
				entity.WatchedAttributes.SetFloat("intoxication", Math.Max(0f, num - 0.005f));
			}
			detoxCounter = 0f;
		}
	}

	private bool ReduceSaturation(float satLossMultiplier)
	{
		bool flag = false;
		satLossMultiplier *= GlobalConstants.HungerSpeedModifier;
		if (SaturationLossDelayFruit > 0f)
		{
			SaturationLossDelayFruit -= 10f * satLossMultiplier;
			flag = true;
		}
		else
		{
			FruitLevel = Math.Max(0f, FruitLevel - Math.Max(0.5f, 0.001f * FruitLevel) * satLossMultiplier * 0.25f);
		}
		if (SaturationLossDelayVegetable > 0f)
		{
			SaturationLossDelayVegetable -= 10f * satLossMultiplier;
			flag = true;
		}
		else
		{
			VegetableLevel = Math.Max(0f, VegetableLevel - Math.Max(0.5f, 0.001f * VegetableLevel) * satLossMultiplier * 0.25f);
		}
		if (SaturationLossDelayProtein > 0f)
		{
			SaturationLossDelayProtein -= 10f * satLossMultiplier;
			flag = true;
		}
		else
		{
			ProteinLevel = Math.Max(0f, ProteinLevel - Math.Max(0.5f, 0.001f * ProteinLevel) * satLossMultiplier * 0.25f);
		}
		if (SaturationLossDelayGrain > 0f)
		{
			SaturationLossDelayGrain -= 10f * satLossMultiplier;
			flag = true;
		}
		else
		{
			GrainLevel = Math.Max(0f, GrainLevel - Math.Max(0.5f, 0.001f * GrainLevel) * satLossMultiplier * 0.25f);
		}
		if (SaturationLossDelayDairy > 0f)
		{
			SaturationLossDelayDairy -= 10f * satLossMultiplier;
			flag = true;
		}
		else
		{
			DairyLevel = Math.Max(0f, DairyLevel - Math.Max(0.5f, 0.001f * DairyLevel) * satLossMultiplier * 0.25f / 2f);
		}
		UpdateNutrientHealthBoost();
		if (flag)
		{
			hungerCounter -= 10f;
			return true;
		}
		float saturation = Saturation;
		if (saturation > 0f)
		{
			Saturation = Math.Max(0f, saturation - satLossMultiplier * 10f);
			sprintCounter = 0;
		}
		return false;
	}

	public void UpdateNutrientHealthBoost()
	{
		float num = FruitLevel / MaxSaturation;
		float num2 = GrainLevel / MaxSaturation;
		float num3 = VegetableLevel / MaxSaturation;
		float num4 = ProteinLevel / MaxSaturation;
		float num5 = DairyLevel / MaxSaturation;
		EntityBehaviorHealth? behavior = entity.GetBehavior<EntityBehaviorHealth>();
		float value = 2.5f * (num + num2 + num3 + num4 + num5);
		behavior.SetMaxHealthModifiers("nutrientHealthMod", value);
	}

	private void SlowTick(float dt)
	{
		if (entity is EntityPlayer)
		{
			EntityPlayer entityPlayer = (EntityPlayer)entity;
			if (entity.World.PlayerByUid(entityPlayer.PlayerUID).WorldData.CurrentGameMode == EnumGameMode.Creative)
			{
				return;
			}
		}
		bool flag = entity.World.Config.GetString("harshWinters").ToBool(defaultValue: true);
		float temperature = entity.World.BlockAccessor.GetClimateAt(entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, entity.World.Calendar.TotalDays).Temperature;
		if (temperature >= 2f || !flag)
		{
			entity.Stats.Remove("hungerrate", "resistcold");
		}
		else
		{
			float num = GameMath.Clamp(2f - temperature, 0f, 10f);
			Room roomForPosition = entity.World.Api.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(entity.Pos.AsBlockPos);
			entity.Stats.Set("hungerrate", "resistcold", (roomForPosition.ExitCount == 0) ? 0f : (num / 40f), persistent: true);
		}
		if (Saturation <= 0f)
		{
			entity.ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Internal,
				Type = EnumDamageType.Hunger
			}, 0.125f);
			sprintCounter = 0;
		}
	}

	public override string PropertyName()
	{
		return "hunger";
	}

	public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
	{
		if (damageSource.Type == EnumDamageType.Heal && damageSource.Source == EnumDamageSource.Revive)
		{
			SaturationLossDelayFruit = 60f;
			SaturationLossDelayVegetable = 60f;
			SaturationLossDelayProtein = 60f;
			SaturationLossDelayGrain = 60f;
			SaturationLossDelayDairy = 60f;
			Saturation = MaxSaturation / 2f;
			VegetableLevel /= 2f;
			ProteinLevel /= 2f;
			FruitLevel /= 2f;
			DairyLevel /= 2f;
			GrainLevel /= 2f;
		}
	}
}
