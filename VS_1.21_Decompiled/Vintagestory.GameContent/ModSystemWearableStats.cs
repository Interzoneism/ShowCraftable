using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemWearableStats : ModSystem
{
	private ICoreAPI api;

	private ICoreClientAPI capi;

	private Dictionary<int, EnumCharacterDressType[]> clothingDamageTargetsByAttackTacket = new Dictionary<int, EnumCharacterDressType[]>
	{
		{
			0,
			new EnumCharacterDressType[3]
			{
				EnumCharacterDressType.Head,
				EnumCharacterDressType.Face,
				EnumCharacterDressType.Neck
			}
		},
		{
			1,
			new EnumCharacterDressType[5]
			{
				EnumCharacterDressType.UpperBody,
				EnumCharacterDressType.UpperBodyOver,
				EnumCharacterDressType.Shoulder,
				EnumCharacterDressType.Arm,
				EnumCharacterDressType.Hand
			}
		},
		{
			2,
			new EnumCharacterDressType[2]
			{
				EnumCharacterDressType.LowerBody,
				EnumCharacterDressType.Foot
			}
		}
	};

	private AssetLocation ripSound = new AssetLocation("sounds/effect/clothrip");

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		api.Event.LevelFinalize += Event_LevelFinalize;
		capi = api;
	}

	private void Event_LevelFinalize()
	{
		capi.World.Player.Entity.OnFootStep += delegate
		{
			onFootStep(capi.World.Player.Entity);
		};
		capi.World.Player.Entity.OnImpact += delegate(double motionY)
		{
			onFallToGround(capi.World.Player.Entity, motionY);
		};
		EntityBehaviorHealth behavior = capi.World.Player.Entity.GetBehavior<EntityBehaviorHealth>();
		if (behavior != null)
		{
			behavior.onDamaged += (float dmg, DamageSource dmgSource) => handleDamaged(capi.World.Player, dmg, dmgSource);
		}
		capi.Logger.VerboseDebug("Done wearable stats");
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		api.Event.PlayerJoin += Event_PlayerJoin;
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		IInventory inv = byPlayer.InventoryManager.GetOwnInventory("character");
		inv.SlotModified += delegate
		{
			updateWearableStats(inv, byPlayer);
		};
		EntityBehaviorHealth behavior = byPlayer.Entity.GetBehavior<EntityBehaviorHealth>();
		if (behavior != null)
		{
			behavior.onDamaged += (float dmg, DamageSource dmgSource) => handleDamaged(byPlayer, dmg, dmgSource);
		}
		byPlayer.Entity.OnFootStep += delegate
		{
			onFootStep(byPlayer.Entity);
		};
		byPlayer.Entity.OnImpact += delegate(double motionY)
		{
			onFallToGround(byPlayer.Entity, motionY);
		};
		updateWearableStats(inv, byPlayer);
	}

	private void onFallToGround(EntityPlayer entity, double motionY)
	{
		if (Math.Abs(motionY) > 0.1)
		{
			onFootStep(entity);
		}
	}

	private void onFootStep(EntityPlayer entity)
	{
		InventoryBase inventoryBase = entity.GetBehavior<EntityBehaviorPlayerInventory>()?.Inventory;
		if (inventoryBase == null)
		{
			return;
		}
		foreach (ItemSlot item in inventoryBase)
		{
			if (!item.Empty && item.Itemstack.Collectible is ItemWearable { FootStepSounds: { } footStepSounds } && footStepSounds.Length != 0)
			{
				AssetLocation location = footStepSounds[api.World.Rand.Next(footStepSounds.Length)];
				float pitch = (float)api.World.Rand.NextDouble() * 0.5f + 0.7f;
				float volume = (entity.Player.Entity.Controls.Sneak ? 0.5f : (1f + (float)api.World.Rand.NextDouble() * 0.3f + 0.7f));
				api.World.PlaySoundAt(location, entity, (api.Side == EnumAppSide.Server) ? entity.Player : null, pitch, 16f, volume);
			}
		}
	}

	private float handleDamaged(IPlayer player, float damage, DamageSource dmgSource)
	{
		EnumDamageType type = dmgSource.Type;
		damage = applyShieldProtection(player, damage, dmgSource);
		if (damage <= 0f)
		{
			return 0f;
		}
		if (api.Side == EnumAppSide.Client)
		{
			return damage;
		}
		if (type != EnumDamageType.BluntAttack && type != EnumDamageType.PiercingAttack && type != EnumDamageType.SlashingAttack)
		{
			return damage;
		}
		if (dmgSource.Source == EnumDamageSource.Internal || dmgSource.Source == EnumDamageSource.Suicide)
		{
			return damage;
		}
		IInventory ownInventory = player.InventoryManager.GetOwnInventory("character");
		double num = api.World.Rand.NextDouble();
		ItemSlot itemSlot;
		int key;
		if ((num -= 0.2) < 0.0)
		{
			itemSlot = ownInventory[12];
			key = 0;
		}
		else if ((num -= 0.5) < 0.0)
		{
			itemSlot = ownInventory[13];
			key = 1;
		}
		else
		{
			itemSlot = ownInventory[14];
			key = 2;
		}
		if (itemSlot.Empty || !(itemSlot.Itemstack.Item is ItemWearable) || itemSlot.Itemstack.Collectible.GetRemainingDurability(itemSlot.Itemstack) <= 0)
		{
			EnumCharacterDressType[] array = clothingDamageTargetsByAttackTacket[key];
			EnumCharacterDressType slotId = array[api.World.Rand.Next(array.Length)];
			ItemSlot itemSlot2 = ownInventory[(int)slotId];
			if (!itemSlot2.Empty)
			{
				float num2 = 0.25f;
				if (type == EnumDamageType.SlashingAttack)
				{
					num2 = 1f;
				}
				if (type == EnumDamageType.PiercingAttack)
				{
					num2 = 0.5f;
				}
				float num3 = (0f - damage) / 100f * num2;
				if ((double)Math.Abs(num3) > 0.05)
				{
					api.World.PlaySoundAt(ripSound, player.Entity);
				}
				(itemSlot2.Itemstack.Collectible as ItemWearable)?.ChangeCondition(itemSlot2, num3);
			}
			return damage;
		}
		ProtectionModifiers protectionModifiers = (itemSlot.Itemstack.Item as ItemWearable).ProtectionModifiers;
		int damageTier = dmgSource.DamageTier;
		float num4 = protectionModifiers.FlatDamageReduction;
		float num5 = protectionModifiers.RelativeProtection;
		for (int i = 1; i <= damageTier; i++)
		{
			bool num6 = i > protectionModifiers.ProtectionTier;
			float num7 = (num6 ? protectionModifiers.PerTierFlatDamageReductionLoss[1] : protectionModifiers.PerTierFlatDamageReductionLoss[0]);
			float num8 = (num6 ? protectionModifiers.PerTierRelativeProtectionLoss[1] : protectionModifiers.PerTierRelativeProtectionLoss[0]);
			if (num6 && protectionModifiers.HighDamageTierResistant)
			{
				num7 /= 2f;
				num8 /= 2f;
			}
			num4 -= num7;
			num5 *= 1f - num8;
		}
		float value = 0.5f + damage * Math.Max(0.5f, (damageTier - protectionModifiers.ProtectionTier) * 3);
		int amount = GameMath.RoundRandom(api.World.Rand, value);
		damage = Math.Max(0f, damage - num4);
		damage *= 1f - Math.Max(0f, num5);
		itemSlot.Itemstack.Collectible.DamageItem(api.World, player.Entity, itemSlot, amount);
		if (itemSlot.Empty)
		{
			api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), player);
		}
		return damage;
	}

	private float applyShieldProtection(IPlayer player, float damage, DamageSource dmgSource)
	{
		double num = 1.0471975803375244;
		float num2 = damage;
		ItemSlot[] array = new ItemSlot[2]
		{
			player.Entity.LeftHandItemSlot,
			player.Entity.RightHandItemSlot
		};
		for (int i = 0; i < array.Length; i++)
		{
			ItemSlot itemSlot = array[i];
			JsonObject jsonObject = itemSlot.Itemstack?.ItemAttributes?["shield"];
			if (jsonObject == null || !jsonObject.Exists)
			{
				continue;
			}
			bool valueOrDefault = dmgSource.SourceEntity?.Properties.Attributes?["isProjectile"].AsBool() == true;
			string text = ((player.Entity.Controls.Sneak && player.Entity.Attributes.GetInt("aiming") != 1) ? "active" : "passive");
			float num3 = 0f;
			float num4 = 0f;
			if (valueOrDefault && jsonObject["protectionChance"][text + "-projectile"].Exists)
			{
				num4 = jsonObject["protectionChance"][text + "-projectile"].AsFloat();
				num3 = jsonObject["projectileDamageAbsorption"].AsFloat(2f);
			}
			else
			{
				num4 = jsonObject["protectionChance"][text].AsFloat();
				num3 = jsonObject["damageAbsorption"].AsFloat(2f);
			}
			if (!dmgSource.GetAttackAngle(player.Entity.Pos.XYZ, out var attackYaw, out var attackPitch))
			{
				break;
			}
			bool flag = Math.Abs(attackPitch) > 1.1344640254974365;
			double num5 = player.Entity.Pos.Yaw;
			double num6 = player.Entity.Pos.Pitch;
			if (valueOrDefault)
			{
				double x = dmgSource.SourceEntity.SidedPos.Motion.X;
				double y = dmgSource.SourceEntity.SidedPos.Motion.Y;
				double z = dmgSource.SourceEntity.SidedPos.Motion.Z;
				flag = Math.Sqrt(x * x + z * z) < Math.Abs(y);
			}
			if ((!flag) ? ((double)Math.Abs(GameMath.AngleRadDistance((float)num5, (float)attackYaw)) < num) : (Math.Abs(GameMath.AngleRadDistance((float)num6, (float)attackPitch)) < (float)Math.PI / 6f))
			{
				float num7 = 0f;
				double num8 = api.World.Rand.NextDouble();
				if (num8 < (double)num4)
				{
					num7 += num3;
				}
				(player as IServerPlayer)?.SendMessage(GlobalConstants.DamageLogChatGroup, Lang.Get("{0:0.#} of {1:0.#} damage blocked by shield ({2} use)", Math.Min(num7, damage), damage, text), EnumChatType.Notification);
				damage = Math.Max(0f, damage - num7);
				string key = "blockSound" + ((num2 > 6f) ? "Heavy" : "Light");
				AssetLocation location = AssetLocation.Create(itemSlot.Itemstack.ItemAttributes["shield"][key].AsString("held/shieldblock-wood-light"), itemSlot.Itemstack.Collectible.Code.Domain).WithPathPrefixOnce("sounds/").WithPathAppendixOnce(".ogg");
				api.World.PlaySoundAt(location, player);
				if (num8 < (double)num4)
				{
					(api as ICoreServerAPI).Network.BroadcastEntityPacket(player.Entity.EntityId, 200, SerializerUtil.Serialize("shieldBlock" + ((i == 0) ? "L" : "R")));
				}
				if (api.Side == EnumAppSide.Server)
				{
					itemSlot.Itemstack.Collectible.DamageItem(api.World, dmgSource.SourceEntity, itemSlot);
					itemSlot.MarkDirty();
				}
			}
		}
		return damage;
	}

	private void updateWearableStats(IInventory inv, IServerPlayer player)
	{
		StatModifiers statModifiers = new StatModifiers();
		float blended = player.Entity.Stats.GetBlended("armorWalkSpeedAffectedness");
		foreach (ItemSlot item in inv)
		{
			if (item.Empty || !(item.Itemstack.Item is ItemWearable))
			{
				continue;
			}
			StatModifiers statModifers = (item.Itemstack.Item as ItemWearable).StatModifers;
			if (statModifers != null)
			{
				bool flag = item.Itemstack.Collectible.GetRemainingDurability(item.Itemstack) == 0;
				statModifiers.canEat &= statModifers.canEat;
				statModifiers.healingeffectivness += (flag ? Math.Min(0f, statModifers.healingeffectivness) : statModifers.healingeffectivness);
				statModifiers.hungerrate += (flag ? Math.Max(0f, statModifers.hungerrate) : statModifers.hungerrate);
				if (statModifers.walkSpeed < 0f)
				{
					statModifiers.walkSpeed += statModifers.walkSpeed * blended;
				}
				else
				{
					statModifiers.walkSpeed += (flag ? 0f : statModifers.walkSpeed);
				}
				statModifiers.rangedWeaponsAcc += (flag ? Math.Min(0f, statModifers.rangedWeaponsAcc) : statModifers.rangedWeaponsAcc);
				statModifiers.rangedWeaponsSpeed += (flag ? Math.Min(0f, statModifers.rangedWeaponsSpeed) : statModifers.rangedWeaponsSpeed);
			}
		}
		EntityPlayer entity = player.Entity;
		entity.Stats.Set("walkspeed", "wearablemod", statModifiers.walkSpeed, persistent: true).Set("healingeffectivness", "wearablemod", statModifiers.healingeffectivness, persistent: true).Set("hungerrate", "wearablemod", statModifiers.hungerrate, persistent: true)
			.Set("rangedWeaponsAcc", "wearablemod", statModifiers.rangedWeaponsAcc, persistent: true)
			.Set("rangedWeaponsSpeed", "wearablemod", statModifiers.rangedWeaponsSpeed, persistent: true);
		entity.walkSpeed = entity.Stats.GetBlended("walkspeed");
		entity.WatchedAttributes.SetBool("canEat", statModifiers.canEat);
	}
}
