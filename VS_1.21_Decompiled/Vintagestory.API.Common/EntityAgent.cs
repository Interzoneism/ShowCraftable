using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class EntityAgent : Entity
{
	public enum EntityServerPacketId
	{
		Teleport = 1,
		Revive = 196,
		Emote = 197,
		Death = 198,
		Hurt = 199,
		PlayPlayerAnim = 200,
		PlayMusic = 201,
		StopMusic = 202,
		Talk = 203
	}

	public enum EntityClientPacketId
	{
		SitfloorEdge = 296
	}

	public float sidewaysSwivelAngle;

	public bool DeadNotify;

	protected long herdId;

	protected EntityControls controls;

	protected EntityControls servercontrols;

	protected bool alwaysRunIdle;

	public EnumEntityActivity CurrentControls;

	public bool AllowDespawn = true;

	private AnimationMetaData curMountedAnim;

	protected bool ignoreTeleportCall;

	protected Block insideBlock;

	protected BlockPos insidePos = new BlockPos();

	public override bool IsCreature => true;

	public override bool CanSwivel
	{
		get
		{
			if (base.CanSwivel)
			{
				return MountedOn == null;
			}
			return false;
		}
	}

	public override bool CanStepPitch
	{
		get
		{
			if (base.CanStepPitch)
			{
				return MountedOn == null;
			}
			return false;
		}
	}

	public virtual float BodyYaw { get; set; }

	public virtual float BodyYawServer { get; set; }

	public long HerdId
	{
		get
		{
			return herdId;
		}
		set
		{
			WatchedAttributes.SetLong("herdId", value);
			herdId = value;
		}
	}

	public IMountableSeat MountedOn { get; protected set; }

	internal virtual bool LoadControlsFromServer => true;

	public virtual ItemSlot LeftHandItemSlot { get; set; }

	public virtual ItemSlot RightHandItemSlot { get; set; }

	public virtual ItemSlot ActiveHandItemSlot => RightHandItemSlot;

	public override bool ShouldDespawn
	{
		get
		{
			if (!Alive)
			{
				return AllowDespawn;
			}
			return false;
		}
	}

	public EntityControls Controls => controls;

	public EntityControls ServerControls => servercontrols;

	public EntityAgent()
	{
		controls = new EntityControls();
		servercontrols = new EntityControls();
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		if (World.Side == EnumAppSide.Server)
		{
			servercontrols = controls;
		}
		WatchedAttributes.RegisterModifiedListener("mountedOn", updateMountedState);
		if (WatchedAttributes["mountedOn"] != null)
		{
			MountedOn = World.ClassRegistry.GetMountable(WatchedAttributes["mountedOn"] as TreeAttribute);
			if (MountedOn != null && TryMount(MountedOn) && Api.Side == EnumAppSide.Server)
			{
				Entity entity = MountedOn.MountSupplier?.OnEntity;
				if (entity != null)
				{
					Api.World.Logger.Audit("{0} loaded already mounted/seated on a {1} at {2}", GetName(), entity.Code.ToShortString(), entity.ServerPos.AsBlockPos);
				}
			}
		}
		herdId = WatchedAttributes.GetLong("herdId", 0L);
	}

	public bool IsEyesSubmerged()
	{
		BlockPos pos = base.SidedPos.AsBlockPos.Add(0f, (float)(Swimming ? base.Properties.SwimmingEyeHeight : base.Properties.EyeHeight), 0f);
		return World.BlockAccessor.GetBlock(pos).MatterState == EnumMatterState.Liquid;
	}

	public virtual bool TryMount(IMountableSeat onmount)
	{
		if (!onmount.CanMount(this))
		{
			return false;
		}
		onmount.Controls.FromInt(Controls.ToInt());
		if (MountedOn != null && MountedOn != onmount)
		{
			IMountableSeat seatOfMountedEntity = MountedOn.MountSupplier.GetSeatOfMountedEntity(this);
			if (seatOfMountedEntity != null)
			{
				seatOfMountedEntity.DoTeleportOnUnmount = false;
			}
			if (!TryUnmount())
			{
				return false;
			}
			if (seatOfMountedEntity != null)
			{
				seatOfMountedEntity.DoTeleportOnUnmount = true;
			}
		}
		doMount(onmount);
		TreeAttribute treeAttribute = new TreeAttribute();
		onmount.MountableToTreeAttributes(treeAttribute);
		WatchedAttributes["mountedOn"] = treeAttribute;
		if (World.Side == EnumAppSide.Server)
		{
			EntityBehavior entityBehavior = MountedOn?.MountSupplier.OnEntity?.GetBehavior("seatable");
			if (entityBehavior != null)
			{
				entityBehavior.ToBytes(forClient: true);
				MountedOn?.MountSupplier.OnEntity.WatchedAttributes.MarkPathDirty("seatdata");
			}
			WatchedAttributes.MarkPathDirty("mountedOn");
		}
		return true;
	}

	protected virtual void updateMountedState()
	{
		if (WatchedAttributes.HasAttribute("mountedOn"))
		{
			IMountableSeat mountable = World.ClassRegistry.GetMountable(WatchedAttributes["mountedOn"] as TreeAttribute);
			IMountableSeat mountableSeat = MountedOn?.MountSupplier.GetSeatOfMountedEntity(this);
			if (MountedOn != null && mountable != null && (MountedOn.Entity?.EntityId != mountable.Entity?.EntityId || mountable.SeatId != mountableSeat?.SeatId))
			{
				if (mountableSeat != null)
				{
					mountableSeat.DoTeleportOnUnmount = false;
				}
				TryUnmount();
				if (mountableSeat != null)
				{
					mountableSeat.DoTeleportOnUnmount = true;
				}
			}
			if (MountedOn == null || (mountable != null && mountable.SeatId != mountableSeat?.SeatId))
			{
				doMount(mountable);
			}
		}
		else
		{
			TryUnmount();
		}
	}

	protected virtual void doMount(IMountableSeat mountable)
	{
		MountedOn = mountable;
		controls.StopAllMovement();
		if (mountable == null)
		{
			WatchedAttributes.RemoveAttribute("mountedOn");
			return;
		}
		if (MountedOn?.SuggestedAnimation != null)
		{
			curMountedAnim = MountedOn.SuggestedAnimation;
			AnimManager?.StartAnimation(curMountedAnim);
		}
		mountable.DidMount(this);
	}

	public bool TryUnmount()
	{
		IMountableSeat mountedOn = MountedOn;
		if (mountedOn != null && !mountedOn.CanUnmount(this))
		{
			return false;
		}
		if (curMountedAnim != null)
		{
			AnimManager?.StopAnimation(curMountedAnim.Animation);
			curMountedAnim = null;
		}
		IMountableSeat mountedOn2 = MountedOn;
		MountedOn = null;
		mountedOn2?.DidUnmount(this);
		if (WatchedAttributes.HasAttribute("mountedOn"))
		{
			WatchedAttributes.RemoveAttribute("mountedOn");
		}
		if (World.Side == EnumAppSide.Server)
		{
			EntityBehavior entityBehavior = mountedOn2?.MountSupplier.OnEntity?.GetBehavior("seatable");
			if (entityBehavior != null)
			{
				entityBehavior.ToBytes(forClient: true);
				mountedOn2?.MountSupplier.OnEntity.WatchedAttributes.MarkPathDirty("seatdata");
			}
		}
		if (Api.Side == EnumAppSide.Server && mountedOn2 != null)
		{
			Entity entity = mountedOn2.MountSupplier?.OnEntity;
			if (entity != null)
			{
				Api.World.Logger.Audit("{0} dismounts/disembarks from a {1} at {2}", GetName(), entity.Code.ToShortString(), entity.ServerPos.AsBlockPos);
			}
		}
		return true;
	}

	public override void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
	{
		if (Alive && reason == EnumDespawnReason.Death)
		{
			PlayEntitySound("death");
			if (damageSourceForDeath?.GetCauseEntity() is EntityPlayer entityPlayer)
			{
				Api.Logger.Audit("Player {0} killed {1} at {2}", entityPlayer.GetName(), Code, Pos.AsBlockPos);
			}
		}
		if (reason != EnumDespawnReason.Death)
		{
			AllowDespawn = true;
		}
		controls.WalkVector.Set(0.0, 0.0, 0.0);
		controls.FlyVector.Set(0.0, 0.0, 0.0);
		ClimbingOnFace = null;
		base.Die(reason, damageSourceForDeath);
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		if (despawn != null && despawn.Reason == EnumDespawnReason.Removed && (this is EntityHumanoid || MountedOn != null))
		{
			TryUnmount();
		}
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		foreach (EntityBehavior behavior in base.SidedProperties.Behaviors)
		{
			behavior.OnInteract(byEntity, slot, hitPosition, mode, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		if (handled == EnumHandling.PreventDefault || handled == EnumHandling.PreventSubsequent || mode != EnumInteractMode.Attack)
		{
			return;
		}
		float num = ((slot.Itemstack == null) ? 0.5f : slot.Itemstack.Collectible.GetAttackPower(slot.Itemstack));
		int damageTier = ((slot.Itemstack != null) ? slot.Itemstack.Collectible.ToolTier : 0);
		float num2 = byEntity.Stats.GetBlended("meleeWeaponsDamage");
		JsonObject attributes = base.Properties.Attributes;
		if (attributes != null && attributes["isMechanical"].AsBool())
		{
			num2 *= byEntity.Stats.GetBlended("mechanicalsDamage");
		}
		num *= num2;
		IPlayer dualCallByPlayer = null;
		if (byEntity is EntityPlayer)
		{
			dualCallByPlayer = (byEntity as EntityPlayer).Player;
			World.PlaySoundAt(new AssetLocation("sounds/player/slap"), ServerPos.X, ServerPos.InternalY, ServerPos.Z, dualCallByPlayer);
			slot?.Itemstack?.Collectible.OnAttackingWith(byEntity.World, byEntity, this, slot);
		}
		if (Api.Side == EnumAppSide.Client && num > 1f)
		{
			JsonObject attributes2 = base.Properties.Attributes;
			if (attributes2 != null && attributes2["spawnDamageParticles"].AsBool())
			{
				Vec3d vec3d = base.SidedPos.XYZ + hitPosition;
				Vec3d minPos = vec3d.AddCopy(-0.15, -0.15, -0.15);
				Vec3d maxPos = vec3d.AddCopy(0.15, 0.15, 0.15);
				int textureSubId = base.Properties.Client.FirstTexture.Baked.TextureSubId;
				Vec3f vec3f = new Vec3f();
				for (int i = 0; i < 10; i++)
				{
					int randomColor = (Api as ICoreClientAPI).EntityTextureAtlas.GetRandomColor(textureSubId);
					vec3f.Set(1f - 2f * (float)World.Rand.NextDouble(), 2f * (float)World.Rand.NextDouble(), 1f - 2f * (float)World.Rand.NextDouble());
					World.SpawnParticles(1f, randomColor, minPos, maxPos, vec3f, vec3f, 1.5f, 1f, 0.25f + (float)World.Rand.NextDouble() * 0.25f, EnumParticleModel.Cube, dualCallByPlayer);
				}
			}
		}
		DamageSource damageSource = new DamageSource
		{
			Source = (((byEntity as EntityPlayer).Player != null) ? EnumDamageSource.Player : EnumDamageSource.Entity),
			SourceEntity = byEntity,
			Type = EnumDamageType.BluntAttack,
			HitPosition = hitPosition,
			DamageTier = damageTier
		};
		IMountable mountable = GetInterface<IMountable>();
		if ((mountable == null || mountable.Controller != byEntity) && ReceiveDamage(damageSource, num))
		{
			byEntity.DidAttack(damageSource, this);
		}
	}

	public override void TeleportToDouble(double x, double y, double z, Action onTeleported = null)
	{
		if (ignoreTeleportCall)
		{
			return;
		}
		ignoreTeleportCall = true;
		if (MountedOn != null)
		{
			if (MountedOn.Entity != null)
			{
				MountedOn.Entity.TeleportToDouble(x, y, z, onTeleported);
				ignoreTeleportCall = false;
				return;
			}
			TryUnmount();
		}
		base.TeleportToDouble(x, y, z, onTeleported);
		ignoreTeleportCall = false;
	}

	public virtual void DidAttack(DamageSource source, EntityAgent targetEntity)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		foreach (EntityBehavior behavior in base.SidedProperties.Behaviors)
		{
			behavior.DidAttack(source, targetEntity, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		if (handled != EnumHandling.PreventDefault)
		{
			_ = 3;
		}
	}

	public override bool ShouldReceiveDamage(DamageSource damageSource, float damage)
	{
		if (!Alive)
		{
			return false;
		}
		return true;
	}

	public override bool ReceiveDamage(DamageSource damageSource, float damage)
	{
		return base.ReceiveDamage(damageSource, damage);
	}

	public virtual void ReceiveSaturation(float saturation, EnumFoodCategory foodCat = EnumFoodCategory.Unknown, float saturationLossDelay = 10f, float nutritionGainMultiplier = 1f)
	{
		if (!Alive || !ShouldReceiveSaturation(saturation, foodCat, saturationLossDelay))
		{
			return;
		}
		foreach (EntityBehavior behavior in base.SidedProperties.Behaviors)
		{
			behavior.OnEntityReceiveSaturation(saturation, foodCat, saturationLossDelay, nutritionGainMultiplier);
		}
	}

	public virtual bool ShouldReceiveSaturation(float saturation, EnumFoodCategory foodCat = EnumFoodCategory.Unknown, float saturationLossDelay = 10f, float nutritionGainMultiplier = 1f)
	{
		return true;
	}

	public override void OnGameTick(float dt)
	{
		if (MountedOn != null)
		{
			AnimationMetaData suggestedAnimation = MountedOn.SuggestedAnimation;
			if (curMountedAnim?.Code != suggestedAnimation?.Code)
			{
				if (curMountedAnim != null)
				{
					AnimManager?.StopAnimation(curMountedAnim.Code);
				}
				if (suggestedAnimation != null)
				{
					AnimManager?.StartAnimation(suggestedAnimation);
				}
				curMountedAnim = suggestedAnimation;
			}
		}
		else if (curMountedAnim != null)
		{
			AnimManager?.StopAnimation(curMountedAnim.Code);
			curMountedAnim = null;
		}
		if (World.Side == EnumAppSide.Client)
		{
			if (Alive)
			{
				CurrentControls = (EnumEntityActivity)(((!servercontrols.TriesToMove && ((!servercontrols.Jump && !servercontrols.Sneak) || !servercontrols.IsClimbing)) ? 1 : 2) | ((Swimming && !servercontrols.FloorSitting) ? 32 : 0) | (servercontrols.FloorSitting ? 512 : 0) | ((servercontrols.Sneak && !servercontrols.IsClimbing && !servercontrols.FloorSitting && !Swimming) ? 8 : 0) | ((servercontrols.TriesToMove && servercontrols.Sprint && !Swimming && !servercontrols.Sneak) ? 4 : 0) | (servercontrols.IsFlying ? (servercontrols.Gliding ? 8192 : 16) : 0) | (servercontrols.IsClimbing ? 256 : 0) | ((servercontrols.Jump && OnGround) ? 64 : 0) | ((!OnGround && !Swimming && !FeetInLiquid && !servercontrols.IsClimbing && !servercontrols.IsFlying && base.SidedPos.Motion.Y < -0.05) ? 128 : 0) | ((MountedOn != null) ? 16384 : 0));
			}
			else
			{
				CurrentControls = EnumEntityActivity.Dead;
			}
			CurrentControls = ((CurrentControls == EnumEntityActivity.None) ? EnumEntityActivity.Idle : CurrentControls);
			if (MountedOn != null && MountedOn.SkipIdleAnimation)
			{
				CurrentControls &= ~EnumEntityActivity.Idle;
			}
			HandleHandAnimations(dt);
			AnimationMetaData animationMetaData = null;
			bool flag = false;
			bool flag2 = false;
			AnimationMetaData[] animations = base.Properties.Client.Animations;
			int num = 0;
			while (animations != null && num < animations.Length)
			{
				AnimationMetaData animationMetaData2 = animations[num];
				bool flag3 = AnimManager.IsAnimationActive(animationMetaData2.Animation);
				bool flag4 = animationMetaData2 != null && animationMetaData2.TriggeredBy?.DefaultAnim == true;
				bool flag5 = animationMetaData2.Matches((int)CurrentControls) || (flag4 && CurrentControls == EnumEntityActivity.Idle);
				flag |= !flag4 && flag3 && animationMetaData2.BlendMode == EnumAnimationBlendMode.Average;
				flag2 |= (flag5 || (flag3 && !animationMetaData2.WasStartedFromTrigger)) && animationMetaData2.SupressDefaultAnimation;
				if (flag4)
				{
					animationMetaData = animationMetaData2;
				}
				if (!onAnimControls(animationMetaData2, flag3, flag5))
				{
					if (!flag3 && flag5)
					{
						animationMetaData2.WasStartedFromTrigger = true;
						AnimManager.StartAnimation(animationMetaData2);
					}
					if (!flag4 && flag3 && !flag5 && animationMetaData2.WasStartedFromTrigger)
					{
						animationMetaData2.WasStartedFromTrigger = false;
						AnimManager.StopAnimation(animationMetaData2.Animation);
					}
				}
				num++;
			}
			if (animationMetaData != null && Alive && !flag2)
			{
				if (flag || MountedOn != null)
				{
					if (!alwaysRunIdle && AnimManager.IsAnimationActive(animationMetaData.Animation))
					{
						AnimManager.StopAnimation(animationMetaData.Animation);
					}
				}
				else
				{
					animationMetaData.WasStartedFromTrigger = true;
					if (!AnimManager.IsAnimationActive(animationMetaData.Animation))
					{
						AnimManager.StartAnimation(animationMetaData);
					}
				}
			}
			if ((!Alive || flag2) && animationMetaData != null)
			{
				AnimManager.StopAnimation(animationMetaData.Code);
			}
			bool flag6 = (Api as ICoreClientAPI).World.Player.Entity.EntityId == EntityId;
			Block block = insideBlock;
			if (block != null && block.GetBlockMaterial(Api.World.BlockAccessor, insidePos) == EnumBlockMaterial.Snow && flag6)
			{
				SpawnSnowStepParticles();
			}
		}
		else
		{
			HandleHandAnimations(dt);
			if (base.Properties.RotateModelOnClimb)
			{
				if (!OnGround && Alive && Controls.IsClimbing && ClimbingOnFace != null && (double)ClimbingOnCollBox.Y2 > 0.2)
				{
					ServerPos.Pitch = (float)ClimbingOnFace.HorizontalAngleIndex * ((float)Math.PI / 2f);
				}
				else
				{
					ServerPos.Pitch = 0f;
				}
			}
		}
		World.FrameProfiler.Mark("entityAgent-ticking");
		base.OnGameTick(dt);
	}

	protected virtual void SpawnSnowStepParticles()
	{
		ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
		EntityPos entityPos = ((coreClientAPI.World.Player.Entity.EntityId == EntityId) ? Pos : ServerPos);
		float num = (float)Math.Sqrt(Pos.Motion.X * Pos.Motion.X + Pos.Motion.Z * Pos.Motion.Z);
		if (Api.World.Rand.NextDouble() < (double)(10f * num))
		{
			Random rand = coreClientAPI.World.Rand;
			Vec3f velocity = new Vec3f(1f - 2f * (float)rand.NextDouble() + GameMath.Clamp((float)Pos.Motion.X * 15f, -5f, 5f), 0.5f + 3.5f * (float)rand.NextDouble(), 1f - 2f * (float)rand.NextDouble() + GameMath.Clamp((float)Pos.Motion.Z * 15f, -5f, 5f));
			float radius = Math.Min(SelectionBox.XSize, SelectionBox.ZSize) * 0.9f;
			World.SpawnCubeParticles(entityPos.AsBlockPos, entityPos.XYZ.Add(0.0, 0.0, 0.0), radius, 2 + (int)(rand.NextDouble() * (double)num * 5.0), 0.5f + (float)rand.NextDouble() * 0.5f, null, velocity);
		}
	}

	protected virtual void SpawnFloatingSediment(IAsyncParticleManager manager)
	{
		ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
		EntityPos entityPos = ((coreClientAPI.World.Player.Entity.EntityId == EntityId) ? Pos : ServerPos);
		double num = SelectionBox.XSize * 0.75f;
		Entity.SplashParticleProps.BasePos.Set(entityPos.X - num / 2.0, entityPos.InternalY + 0.0, entityPos.Z - num / 2.0);
		Entity.SplashParticleProps.AddPos.Set(num, 0.5, num);
		float num2 = (float)entityPos.Motion.Length();
		Entity.SplashParticleProps.AddVelocity.Set((float)entityPos.Motion.X * 20f, 0f, (float)entityPos.Motion.Z * 20f);
		float num3 = base.Properties.Attributes?["extraSplashParticlesMul"].AsFloat(1f) ?? 1f;
		Entity.SplashParticleProps.QuantityMul = 0.15f * num2 * 5f * 2f * num3;
		World.SpawnParticles(Entity.SplashParticleProps);
		SpawnWaterMovementParticles(Math.Max(Swimming ? 0.04f : 0f, num2 * 5f));
		FloatingSedimentParticles floatingSedimentParticles = new FloatingSedimentParticles();
		floatingSedimentParticles.SedimentPos.Set((int)entityPos.X, (int)entityPos.InternalY - 1, (int)entityPos.Z);
		Block block = (floatingSedimentParticles.SedimentBlock = World.BlockAccessor.GetBlock(floatingSedimentParticles.SedimentPos));
		if (insideBlock != null && (block.BlockMaterial == EnumBlockMaterial.Gravel || block.BlockMaterial == EnumBlockMaterial.Soil || block.BlockMaterial == EnumBlockMaterial.Sand))
		{
			floatingSedimentParticles.BasePos.Set(Entity.SplashParticleProps.BasePos);
			floatingSedimentParticles.AddPos.Set(Entity.SplashParticleProps.AddPos);
			floatingSedimentParticles.quantity = num2 * 150f;
			floatingSedimentParticles.waterColor = insideBlock.GetColor(coreClientAPI, floatingSedimentParticles.SedimentPos);
			manager.Spawn(floatingSedimentParticles);
		}
	}

	protected virtual bool onAnimControls(AnimationMetaData anim, bool wasActive, bool nowActive)
	{
		return false;
	}

	protected virtual void HandleHandAnimations(float dt)
	{
	}

	public virtual double GetWalkSpeedMultiplier(double groundDragFactor = 0.3)
	{
		int num = (int)(base.SidedPos.InternalY - 0.05000000074505806);
		int num2 = (int)(base.SidedPos.InternalY + 0.009999999776482582);
		Block blockRaw = World.BlockAccessor.GetBlockRaw((int)base.SidedPos.X, num, (int)base.SidedPos.Z);
		insidePos.Set((int)base.SidedPos.X, num2, (int)base.SidedPos.Z);
		insideBlock = World.BlockAccessor.GetBlock(insidePos);
		double num3 = (servercontrols.Sneak ? ((double)GlobalConstants.SneakSpeedMultiplier) : 1.0) * (servercontrols.Sprint ? GlobalConstants.SprintSpeedMultiplier : 1.0);
		if (FeetInLiquid)
		{
			num3 /= 2.5;
		}
		return num3 * (double)(blockRaw.WalkSpeedMultiplier * ((num == num2) ? 1f : insideBlock.WalkSpeedMultiplier));
	}

	public override void ToBytes(BinaryWriter writer, bool forClient)
	{
		if (MountedOn != null)
		{
			TreeAttribute treeAttribute = new TreeAttribute();
			MountedOn?.MountableToTreeAttributes(treeAttribute);
			WatchedAttributes["mountedOn"] = treeAttribute;
		}
		else if (WatchedAttributes.HasAttribute("mountedOn"))
		{
			WatchedAttributes.RemoveAttribute("mountedOn");
		}
		base.ToBytes(writer, forClient);
		controls.ToBytes(writer);
	}

	public override void FromBytes(BinaryReader reader, bool forClient)
	{
		try
		{
			base.FromBytes(reader, forClient);
			controls.FromBytes(reader, LoadControlsFromServer);
		}
		catch (EndOfStreamException innerException)
		{
			throw new Exception("EndOfStreamException thrown while reading entity, you might be able to recover your savegame through repair mode", innerException);
		}
	}

	protected override void SetHeadPositionToWatchedAttributes()
	{
		WatchedAttributes["headYaw"] = new FloatAttribute(ServerPos.HeadYaw);
		WatchedAttributes["headPitch"] = new FloatAttribute(ServerPos.HeadPitch);
	}

	protected override void GetHeadPositionFromWatchedAttributes()
	{
		ServerPos.HeadYaw = WatchedAttributes.GetFloat("headYaw");
		ServerPos.HeadPitch = WatchedAttributes.GetFloat("headPitch");
	}

	public virtual bool TryStopHandAction(bool isCancel, EnumItemUseCancelReason cancelReason = EnumItemUseCancelReason.ReleasedMouse)
	{
		if (controls.HandUse == EnumHandInteract.None || RightHandItemSlot?.Itemstack == null)
		{
			return true;
		}
		float secondsPassed = (float)(World.ElapsedMilliseconds - controls.UsingBeginMS) / 1000f;
		if (isCancel)
		{
			controls.HandUse = RightHandItemSlot.Itemstack.Collectible.OnHeldUseCancel(secondsPassed, RightHandItemSlot, this, null, null, cancelReason);
		}
		else
		{
			controls.HandUse = EnumHandInteract.None;
			RightHandItemSlot.Itemstack.Collectible.OnHeldUseStop(secondsPassed, RightHandItemSlot, this, null, null, controls.HandUse);
		}
		return controls.HandUse == EnumHandInteract.None;
	}

	public virtual void WalkInventory(OnInventorySlot handler)
	{
	}

	public override void UpdateDebugAttributes()
	{
		base.UpdateDebugAttributes();
		DebugAttributes.SetString("Herd Id", HerdId.ToString() ?? "");
	}

	public override bool TryGiveItemStack(ItemStack itemstack)
	{
		if (itemstack == null || itemstack.StackSize == 0)
		{
			return false;
		}
		List<EntityBehavior> list = base.SidedProperties?.Behaviors;
		EnumHandling handling = EnumHandling.PassThrough;
		if (list != null)
		{
			foreach (EntityBehavior item in list)
			{
				item.TryGiveItemStack(itemstack, ref handling);
				if (handling == EnumHandling.PreventSubsequent)
				{
					break;
				}
			}
		}
		return handling != EnumHandling.PassThrough;
	}

	public bool ToleratesDamageFrom(Entity eOther)
	{
		bool result = false;
		foreach (EntityBehavior item in base.SidedProperties?.Behaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			bool flag = item.ToleratesDamageFrom(eOther, ref handling);
			if (handling != EnumHandling.PassThrough)
			{
				result = flag;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				return flag;
			}
		}
		return result;
	}

	public override string GetInfoText()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(base.GetInfoText());
		if (Api is ICoreClientAPI coreClientAPI && coreClientAPI.Settings.Bool["extendedDebugInfo"])
		{
			stringBuilder.AppendLine("<font color=\"#bbbbbb\">Herd id: " + HerdId + "</font>");
			if (DebugAttributes.HasAttribute("AI Tasks"))
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(39, 1, stringBuilder2);
				handler.AppendLiteral("<font color=\"#bbbbbb\">AI tasks: ");
				handler.AppendFormatted(DebugAttributes.GetString("AI Tasks"));
				handler.AppendLiteral("</font>");
				stringBuilder2.AppendLine(ref handler);
			}
		}
		return stringBuilder.ToString();
	}
}
