using System;
using System.Collections.Generic;
using System.IO;
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

public class EntityBehaviorHealth : EntityBehavior
{
	public const double FallDamageYMotionThreshold = -0.19;

	public const float FallDamageFallenDistanceThreshold = 3.5f;

	protected Dictionary<string, float>? maxHealthModifiers;

	protected string? HurtAnimationCode = "hurt";

	protected string HurtEntitySoundCode = "hurt";

	protected float timeSinceLastDoTTickSec;

	protected float timeBetweenDoTTicksSec = 0.5f;

	private float secondsSinceLastUpdate;

	public float Health
	{
		get
		{
			return healthTree.GetFloat("currenthealth");
		}
		set
		{
			healthTree.SetFloat("currenthealth", value);
			entity.WatchedAttributes.MarkPathDirty("health");
		}
	}

	public float? FutureHealth
	{
		get
		{
			return healthTree.GetFloat("futureHealth");
		}
		set
		{
			if (!value.HasValue)
			{
				healthTree.RemoveAttribute("futureHealth");
			}
			else
			{
				healthTree.SetFloat("futureHealth", value.Value);
			}
			entity.WatchedAttributes.MarkPathDirty("health");
		}
	}

	public float PreviousHealth
	{
		get
		{
			return healthTree.GetFloat("previousHealthValue");
		}
		set
		{
			healthTree.SetFloat("previousHealthValue", value);
			entity.WatchedAttributes.MarkPathDirty("health");
		}
	}

	public float HealthChangeRate
	{
		get
		{
			return healthTree.GetFloat("healthChangeRate");
		}
		set
		{
			healthTree.SetFloat("healthChangeRate", value);
			entity.WatchedAttributes.MarkPathDirty("health");
		}
	}

	public float BaseMaxHealth
	{
		get
		{
			return healthTree.GetFloat("basemaxhealth");
		}
		set
		{
			healthTree.SetFloat("basemaxhealth", value);
			entity.WatchedAttributes.MarkPathDirty("health");
		}
	}

	public float MaxHealth
	{
		get
		{
			return healthTree.GetFloat("maxhealth");
		}
		set
		{
			healthTree.SetFloat("maxhealth", value);
			entity.WatchedAttributes.MarkPathDirty("health");
		}
	}

	public List<DamageOverTimeEffect> ActiveDoTEffects { get; } = new List<DamageOverTimeEffect>();

	[Obsolete("Please call SetMaxHealthModifiers() instead of writing to it directly.")]
	public Dictionary<string, float>? MaxHealthModifiers { get; set; }

	protected virtual float HealthUpdateCooldownSec => 1f;

	protected virtual bool ReceiveHailDamage { get; set; }

	protected virtual float AutoRegenSaturationThreshold { get; set; } = 0.75f;

	protected virtual float SaturationPerHealthPoint { get; set; } = 150f;

	private ITreeAttribute healthTree => entity.WatchedAttributes.GetTreeAttribute("health");

	public event OnDamagedDelegate onDamaged = (float dmg, DamageSource dmgSource) => dmg;

	public EntityBehaviorHealth(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("health");
		if (treeAttribute == null)
		{
			entity.WatchedAttributes.SetAttribute("health", treeAttribute = new TreeAttribute());
			BaseMaxHealth = attributes["maxhealth"].AsFloat(20f);
			Health = attributes["currenthealth"].AsFloat(BaseMaxHealth);
			PreviousHealth = Health;
			MarkDirty();
			return;
		}
		if (treeAttribute.GetFloat("basemaxhealth") == 0f)
		{
			BaseMaxHealth = attributes["maxhealth"].AsFloat(20f);
			MarkDirty();
		}
		secondsSinceLastUpdate = (float)entity.World.Rand.NextDouble();
		ReceiveHailDamage = entity is EntityPlayer;
		if (attributes.KeyExists("receiveHailDamage"))
		{
			ReceiveHailDamage = attributes["receiveHailDamage"].AsBool(entity is EntityPlayer);
		}
		if (attributes.KeyExists("autoRegenSaturationThreshold"))
		{
			AutoRegenSaturationThreshold = attributes["autoRegenSaturationThreshold"].AsFloat();
		}
		if (attributes.KeyExists("saturationPerHealthPoint"))
		{
			SaturationPerHealthPoint = attributes["saturationPerHealthPoint"].AsFloat();
		}
		if (attributes.KeyExists("hurtAnimationCode"))
		{
			HurtAnimationCode = attributes["hurtAnimationCode"].AsString();
		}
		timeBetweenDoTTicksSec = attributes["timeBetweenTicksSec"].AsFloat(0.5f);
		TimeSpan previousTickTime = TimeSpan.FromMilliseconds(entity.World.ElapsedMilliseconds);
		byte[] bytes = entity.WatchedAttributes.GetBytes("damageovertime-activeeffects");
		if (bytes == null)
		{
			return;
		}
		ActiveDoTEffectsFromBytes(bytes);
		foreach (DamageOverTimeEffect activeDoTEffect in ActiveDoTEffects)
		{
			activeDoTEffect.PreviousTickTime = previousTickTime;
		}
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.World.Side == EnumAppSide.Client)
		{
			return;
		}
		ProcessDoTEffects(deltaTime);
		if (entity.Alive)
		{
			DamageIfFallingIntoVoid();
			secondsSinceLastUpdate += deltaTime;
			if (!(secondsSinceLastUpdate < HealthUpdateCooldownSec))
			{
				ApplyRegenAndHunger();
				ApplyHailDamage();
				secondsSinceLastUpdate = 0f;
			}
		}
	}

	public override void OnEntityDeath(DamageSource damageSourceForDeath)
	{
		base.OnEntityDeath(damageSourceForDeath);
		Health = 0f;
	}

	public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
	{
		if (entity.World.Side == EnumAppSide.Client || TurnIntoDoTEffect(damageSource, damage))
		{
			return;
		}
		float damageBeforeDelegatesApplied = damage;
		ApplyOnDamageDelegates(damageSource, ref damage);
		if (damageSource.Type == EnumDamageType.Heal)
		{
			ApplyHealing(damageSource, damage);
			entity.OnHurt(damageSource, damage);
			UpdateMaxHealth();
		}
		else
		{
			if (!entity.Alive || damage <= 0f)
			{
				return;
			}
			LogPlayerToPlayerDamage(damageSource, damage, damageBeforeDelegatesApplied);
			PreviousHealth = Health;
			Health -= damage;
			entity.OnHurt(damageSource, damage);
			UpdateMaxHealth();
			if (Health <= 0f)
			{
				Health = 0f;
				entity.Die(EnumDespawnReason.Death, damageSource);
				return;
			}
			if (damage > 1f && HurtAnimationCode != null)
			{
				entity.AnimManager.StartAnimation(HurtAnimationCode);
			}
			entity.PlayEntitySound(HurtEntitySoundCode);
		}
	}

	public override void OnFallToGround(Vec3d lastTerrainContact, double withYMotion)
	{
		if (!entity.Properties.FallDamage)
		{
			return;
		}
		bool flag = (entity as EntityAgent)?.ServerControls.Gliding ?? false;
		double num = Math.Abs(lastTerrainContact.Y - entity.Pos.Y);
		if (num < 3.5)
		{
			return;
		}
		if (flag)
		{
			num = Math.Min(num / 2.0, Math.Min(14.0, num));
			withYMotion /= 2.0;
			if ((double)entity.ServerPos.Pitch < 3.9269909262657166)
			{
				num = 0.0;
			}
		}
		if (!(withYMotion > -0.19))
		{
			num *= (double)entity.Properties.FallDamageMultiplier;
			double num2 = Math.Max(0.0, num - 3.5);
			double num3 = -0.04100000113248825 * Math.Pow(num2, 0.75) - 0.2199999988079071;
			double num4 = Math.Max(0.0, 0.0 - num3 + withYMotion);
			num2 -= 20.0 * num4;
			if (!(num2 <= 0.0))
			{
				entity.ReceiveDamage(new DamageSource
				{
					Source = EnumDamageSource.Fall,
					Type = EnumDamageType.Gravity,
					IgnoreInvFrames = true
				}, (float)num2);
			}
		}
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		ICoreClientAPI obj = entity.Api as ICoreClientAPI;
		if (obj != null && obj.World.Player?.WorldData?.CurrentGameMode == EnumGameMode.Creative)
		{
			infotext.AppendLine(Lang.Get("Health: {0}/{1}", Health, MaxHealth));
		}
	}

	public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
	{
		if (IsHealable(player.Entity))
		{
			ICanHealCreature canHealCreature = player.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible?.GetCollectibleInterface<ICanHealCreature>();
			if (canHealCreature != null)
			{
				return canHealCreature.GetHealInteractionHelp(world, es, player).Append<WorldInteraction>(base.GetInteractionHelp(world, es, player, ref handled));
			}
		}
		return base.GetInteractionHelp(world, es, player, ref handled);
	}

	public override string PropertyName()
	{
		return "health";
	}

	public override void ToBytes(bool forClient)
	{
		base.ToBytes(forClient);
		entity.WatchedAttributes.SetBytes("damageovertime-activeeffects", ActiveDoTEffectsToBytes());
	}

	public void SetMaxHealthModifiers(string key, float value)
	{
		bool flag = true;
		float value2;
		if (maxHealthModifiers == null)
		{
			maxHealthModifiers = new Dictionary<string, float>();
			if (value == 0f)
			{
				flag = false;
			}
		}
		else if (maxHealthModifiers.TryGetValue(key, out value2) && value2 == value)
		{
			flag = false;
		}
		maxHealthModifiers[key] = value;
		if (flag)
		{
			MarkDirty();
		}
	}

	public void MarkDirty()
	{
		UpdateMaxHealth();
		entity.WatchedAttributes.MarkPathDirty("health");
	}

	public void UpdateMaxHealth()
	{
		float num = BaseMaxHealth;
		Dictionary<string, float> dictionary = maxHealthModifiers;
		if (dictionary != null)
		{
			foreach (KeyValuePair<string, float> item in dictionary)
			{
				num += item.Value;
			}
		}
		num += entity.Stats.GetBlended("maxhealthExtraPoints") - 1f;
		bool num2 = Health >= MaxHealth;
		MaxHealth = num;
		if (num2)
		{
			Health = MaxHealth;
		}
	}

	public bool IsHealable(EntityAgent eagent, ItemSlot? slot = null)
	{
		ICanHealCreature canHealCreature = (slot ?? eagent.RightHandItemSlot)?.Itemstack?.Collectible?.GetCollectibleInterface<ICanHealCreature>();
		if (Health < MaxHealth)
		{
			return canHealCreature?.CanHeal(entity) ?? false;
		}
		return false;
	}

	public virtual int ApplyDoTEffect(EnumDamageSource damageSource, EnumDamageType damageType, int damageTier, float totalDamage, TimeSpan totalTime, int ticksNumber, EnumDamageOverTimeEffectType effectType = EnumDamageOverTimeEffectType.Unknown)
	{
		return ApplyDoTEffect(damageSource, damageType, damageTier, totalDamage, totalTime, ticksNumber, (int)effectType);
	}

	public virtual int ApplyDoTEffect(EnumDamageSource damageSource, EnumDamageType damageType, int damageTier, float totalDamage, TimeSpan totalTime, int ticksNumber, int effectType = 0)
	{
		DamageOverTimeEffect item = new DamageOverTimeEffect
		{
			DamageSource = damageSource,
			DamageType = damageType,
			DamageTier = damageTier,
			Damage = totalDamage / (float)ticksNumber,
			TickDuration = totalTime / ticksNumber,
			PreviousTickTime = TimeSpan.FromMilliseconds(entity.World.ElapsedMilliseconds),
			TicksLeft = ticksNumber,
			EffectType = effectType
		};
		ActiveDoTEffects.Add(item);
		return ActiveDoTEffects.Count - 1;
	}

	public virtual void StopDoTEffect(int effectType, int amount = int.MaxValue)
	{
		int num = 0;
		foreach (DamageOverTimeEffect item in ActiveDoTEffects.Where((DamageOverTimeEffect effect) => effect.EffectType == effectType))
		{
			item.TicksLeft = 0;
			num++;
			if (num >= amount)
			{
				break;
			}
		}
	}

	protected virtual void DamageIfFallingIntoVoid()
	{
		if (entity.Pos.Y < -30.0)
		{
			entity.ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Void,
				Type = EnumDamageType.Gravity
			}, 4f);
		}
	}

	protected virtual void ApplyHailDamage()
	{
		if (!ReceiveHailDamage)
		{
			return;
		}
		int rainMapHeightAt = entity.World.BlockAccessor.GetRainMapHeightAt((int)entity.ServerPos.X, (int)entity.ServerPos.Z);
		if (entity.ServerPos.Y >= (double)rainMapHeightAt)
		{
			PrecipitationState precipitationState = entity.Api.ModLoader.GetModSystem<WeatherSystemBase>().GetPrecipitationState(entity.ServerPos.XYZ);
			if (precipitationState != null && precipitationState.ParticleSize >= 0.5 && precipitationState.Type == EnumPrecipitationType.Hail && entity.World.Rand.NextDouble() < precipitationState.Level / 2.0)
			{
				entity.ReceiveDamage(new DamageSource
				{
					Source = EnumDamageSource.Weather,
					Type = EnumDamageType.BluntAttack
				}, (float)precipitationState.ParticleSize / 15f);
			}
		}
	}

	protected virtual void ApplyRegenAndHunger()
	{
		float health = Health;
		float maxHealth = MaxHealth;
		if (health >= maxHealth)
		{
			return;
		}
		float num = ((entity is EntityPlayer) ? entity.Api.World.Config.GetString("playerHealthRegenSpeed", "1").ToFloat() : entity.WatchedAttributes.GetFloat("regenSpeed", 1f));
		float num2 = 0.000333333f * num;
		float num3 = secondsSinceLastUpdate * entity.Api.World.Calendar.SpeedOfTime * entity.Api.World.Calendar.CalendarSpeedMul;
		if (entity is EntityPlayer entityPlayer)
		{
			EntityBehaviorHunger behavior = entity.GetBehavior<EntityBehaviorHunger>();
			if (behavior != null && entityPlayer.Player.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				num2 = GameMath.Clamp(num2 * behavior.Saturation / behavior.MaxSaturation * 1f / AutoRegenSaturationThreshold, 0f, num2);
				behavior.ConsumeSaturation(SaturationPerHealthPoint * num3 * num2);
			}
		}
		Health = Math.Min(health + num3 * num2, maxHealth);
	}

	protected virtual void ApplyOnDamageDelegates(DamageSource damageSource, ref float damage)
	{
		if (this.onDamaged == null)
		{
			return;
		}
		foreach (OnDamagedDelegate item in this.onDamaged.GetInvocationList().OfType<OnDamagedDelegate>())
		{
			damage = item(damage, damageSource);
		}
	}

	protected virtual void ApplyHealing(DamageSource damageSource, float damage)
	{
		if (damageSource.Source != EnumDamageSource.Revive)
		{
			Health = Math.Min(Health + damage, MaxHealth);
			return;
		}
		damage = Math.Min(damage, MaxHealth);
		Health = damage;
	}

	protected virtual void LogPlayerToPlayerDamage(DamageSource damageSource, float damage, float damageBeforeDelegatesApplied)
	{
		if (entity is EntityPlayer entityPlayer && damageSource.GetCauseEntity() is EntityPlayer entityPlayer2)
		{
			string value = ((damageSource.SourceEntity == entityPlayer2) ? (entityPlayer2.Player.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible.Code.ToString() ?? "hands") : damageSource.SourceEntity.Code.ToString());
			entity.Api.Logger.Audit($"{entityPlayer.Player.PlayerName} at {entity.Pos.AsBlockPos} got {damage}/{damageBeforeDelegatesApplied} damage {damageSource.Type.ToString().ToLowerInvariant()} {value} by {entityPlayer2.GetName()}");
		}
	}

	protected virtual void ProcessDoTEffects(float dt)
	{
		if (!entity.Alive)
		{
			ActiveDoTEffects.Clear();
			return;
		}
		if (timeSinceLastDoTTickSec < timeBetweenDoTTicksSec)
		{
			timeSinceLastDoTTickSec += dt;
			return;
		}
		timeSinceLastDoTTickSec = 0f;
		TimeSpan timeSpan = TimeSpan.FromMilliseconds(entity.World.ElapsedMilliseconds);
		float num = 0f;
		float num2 = 0f;
		for (int num3 = ActiveDoTEffects.Count - 1; num3 >= 0; num3--)
		{
			DamageOverTimeEffect damageOverTimeEffect = ActiveDoTEffects[num3];
			if (timeSpan - damageOverTimeEffect.PreviousTickTime >= damageOverTimeEffect.TickDuration)
			{
				entity.ReceiveDamage(new DamageSource
				{
					Source = damageOverTimeEffect.DamageSource,
					Type = damageOverTimeEffect.DamageType,
					DamageTier = damageOverTimeEffect.DamageTier
				}, damageOverTimeEffect.Damage);
				damageOverTimeEffect.TicksLeft--;
				if (damageOverTimeEffect.TicksLeft <= 0)
				{
					ActiveDoTEffects.RemoveAt(num3);
				}
				else
				{
					damageOverTimeEffect.PreviousTickTime = timeSpan;
				}
			}
			if (damageOverTimeEffect.DamageType == EnumDamageType.Heal)
			{
				num += damageOverTimeEffect.Damage * (float)damageOverTimeEffect.TicksLeft;
				num2 += damageOverTimeEffect.Damage * (float)damageOverTimeEffect.TicksLeft;
			}
			else
			{
				num2 -= damageOverTimeEffect.Damage * (float)damageOverTimeEffect.TicksLeft;
			}
		}
		if ((double)Math.Abs(num) > 0.1)
		{
			FutureHealth = Health + num;
		}
		else
		{
			FutureHealth = null;
		}
		HealthChangeRate = num2;
	}

	protected virtual byte[] ActiveDoTEffectsToBytes()
	{
		MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(ActiveDoTEffects.Count);
		foreach (DamageOverTimeEffect activeDoTEffect in ActiveDoTEffects)
		{
			activeDoTEffect.ToBytes(binaryWriter);
		}
		return memoryStream.ToArray();
	}

	protected virtual void ActiveDoTEffectsFromBytes(byte[] bytes)
	{
		BinaryReader binaryReader = new BinaryReader(new MemoryStream(bytes));
		ActiveDoTEffects.Clear();
		int num = binaryReader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			ActiveDoTEffects.Add(DamageOverTimeEffect.FromBytes(binaryReader));
		}
	}

	protected virtual bool TurnIntoDoTEffect(DamageSource damageSource, float damage)
	{
		if (damageSource.Duration <= TimeSpan.Zero)
		{
			return false;
		}
		ApplyDoTEffect(damageSource.Source, damageSource.Type, damageSource.DamageTier, damage, damageSource.Duration, damageSource.TicksPerDuration, damageSource.DamageOverTimeType);
		return true;
	}
}
