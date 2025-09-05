using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class BehaviorHealingItem : CollectibleBehavior, ICanHealCreature
{
	protected IProgressBar? progressBarRender;

	protected ILoadedSound? applicationSound;

	protected ICoreAPI? api;

	protected float secondsUsedToCancel;

	public HealOverTimeConfig Config { get; set; } = new HealOverTimeConfig();

	public BehaviorHealingItem(CollectibleObject collectable)
		: base(collectable)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		Config = properties.AsObject<HealOverTimeConfig>();
	}

	public override void OnLoaded(ICoreAPI api)
	{
		if (Config.Sound != null)
		{
			applicationSound = (api as ICoreClientAPI)?.World.LoadSound(new SoundParams
			{
				DisposeOnFinish = false,
				Location = Config.Sound,
				ShouldLoop = true,
				Range = Config.SoundRange
			});
		}
		this.api = api;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
	{
		if (!CancelApplication(byEntity))
		{
			handHandling = EnumHandHandling.PreventDefault;
			handling = EnumHandling.PreventSubsequent;
			api?.World.RegisterCallback(delegate
			{
				applicationSound?.Stop();
			}, (int)GetApplicationTime(byEntity) * 1000);
			ICoreAPI? coreAPI = api;
			if (coreAPI != null && coreAPI.Side == EnumAppSide.Client)
			{
				ModSystemProgressBar modSystem = api.ModLoader.GetModSystem<ModSystemProgressBar>();
				modSystem.RemoveProgressbar(progressBarRender);
				progressBarRender = modSystem.AddProgressbar();
			}
			secondsUsedToCancel = 0f;
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
	{
		if (!CancelApplication(byEntity))
		{
			secondsUsedToCancel = 0f;
		}
		else if (secondsUsedToCancel == 0f)
		{
			secondsUsedToCancel = secondsUsed;
		}
		if (CancelApplication(byEntity) && (double)(secondsUsed - secondsUsedToCancel) > 0.5)
		{
			return false;
		}
		ILoadedSound? loadedSound = applicationSound;
		if (loadedSound != null && loadedSound.HasStopped)
		{
			applicationSound.Start();
		}
		applicationSound?.SetPosition((float)byEntity.Pos.X, (float)byEntity.Pos.InternalY, (float)byEntity.Pos.Z);
		handling = EnumHandling.Handled;
		float num = secondsUsed / (GetApplicationTime(byEntity) + ((byEntity.World.Side == EnumAppSide.Client) ? 0.3f : 0f));
		if (progressBarRender != null)
		{
			progressBarRender.Progress = num;
		}
		return num < 1f;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handled)
	{
		applicationSound?.Stop();
		api?.ModLoader.GetModSystem<ModSystemProgressBar>()?.RemoveProgressbar(progressBarRender);
		return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason, ref handled);
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection? entitySel, ref EnumHandling handling)
	{
		applicationSound?.Stop();
		api?.ModLoader.GetModSystem<ModSystemProgressBar>()?.RemoveProgressbar(progressBarRender);
		handling = EnumHandling.Handled;
		if (!(secondsUsed < GetApplicationTime(byEntity)) && byEntity.World.Side == EnumAppSide.Server)
		{
			Entity targetEntity = GetTargetEntity(slot, byEntity, entitySel);
			EntityBehaviorPlayerRevivable behavior = targetEntity.GetBehavior<EntityBehaviorPlayerRevivable>();
			if (behavior != null && Config.CanRevive && !targetEntity.Alive)
			{
				behavior.AttemptRevive();
			}
			else
			{
				DamageSource damageSource = new DamageSource
				{
					Source = EnumDamageSource.Internal,
					Type = EnumDamageType.Heal,
					DamageTier = 0,
					Duration = TimeSpan.FromSeconds(Config.EffectDurationSec),
					TicksPerDuration = Config.Ticks
				};
				targetEntity.ReceiveDamage(damageSource, Config.Health);
			}
			if (Config.AppliedSound != null)
			{
				byEntity.World.PlaySoundAt(Config.AppliedSound, byEntity, null, randomizePitch: false, Config.SoundRange);
			}
			slot.TakeOut(1);
			slot.MarkDirty();
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		dsc.AppendLine(Lang.Get("healing-item-info", $"{Config.Health:F1}", $"{Config.EffectDurationSec:F1}", $"{Config.ApplicationTimeSec:F1}"));
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "game:heldhelp-heal",
				MouseButton = EnumMouseButton.Right
			}
		};
	}

	public virtual WorldInteraction[] GetHealInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-heal",
				HotKeyCode = "ctrl",
				MouseButton = EnumMouseButton.Right
			}
		};
	}

	public virtual bool CanHeal(Entity target)
	{
		int num = target.Properties.Attributes?["minGenerationToAllowHealing"].AsInt(-1) ?? (-1);
		if (!(target is EntityPlayer))
		{
			if (num >= 0)
			{
				return num >= target.WatchedAttributes.GetInt("generation");
			}
			return false;
		}
		return true;
	}

	protected virtual float GetApplicationTime(Entity byEntity)
	{
		float num = 0f;
		if (Config.AffectedByArmor)
		{
			num = byEntity.Stats.GetBlended("healingeffectivness");
			num = Math.Clamp(num, 0f, 2f) - 1f;
		}
		if (num < 0f)
		{
			return Config.ApplicationTimeSec + (Config.ApplicationTimeSec - Config.MaxApplicationTimeSec) * num;
		}
		if (num > 0f)
		{
			return Config.ApplicationTimeSec * (1f - num);
		}
		return Config.ApplicationTimeSec;
	}

	protected virtual Entity GetTargetEntity(ItemSlot slot, EntityAgent byEntity, EntitySelection? entitySelection)
	{
		Entity result = byEntity;
		Entity entity = entitySelection?.Entity;
		if (entity == null)
		{
			return result;
		}
		EntityBehaviorHealth behavior = entity.GetBehavior<EntityBehaviorHealth>();
		if (byEntity.Controls.CtrlKey && !byEntity.Controls.Forward && !byEntity.Controls.Backward && !byEntity.Controls.Left && !byEntity.Controls.Right && behavior != null && behavior.IsHealable(byEntity, slot))
		{
			result = entity;
		}
		return result;
	}

	protected virtual bool CancelApplication(Entity entity)
	{
		if (entity.OnGround || entity.Swimming || !Config.CancelInAir)
		{
			if (entity.Swimming)
			{
				return Config.CancelWhileSwimming;
			}
			return false;
		}
		return true;
	}
}
