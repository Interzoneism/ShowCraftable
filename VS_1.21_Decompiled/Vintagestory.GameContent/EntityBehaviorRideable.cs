using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorRideable : EntityBehaviorSeatable, IMountable, IRenderer, IDisposable, IMountableListener
{
	public List<GaitMeta> RideableGaitOrder = new List<GaitMeta>();

	public double ForwardSpeed;

	public double AngularVelocity;

	public bool IsInMidJump;

	public AnimationMetaData curAnim;

	public AnimationMetaData curAnimPassanger;

	protected ICoreAPI api;

	protected float coyoteTimer;

	protected long lastJumpMs;

	protected bool jumpNow;

	protected EntityAgent eagent = entity as EntityAgent;

	protected long lastGaitChangeMs;

	protected float timeSinceLastGaitCheck;

	protected float timeSinceLastGaitFatigue;

	protected ILoadedSound gaitSound;

	protected FastSmallDictionary<string, ControlMeta> Controls;

	protected string[] GaitOrderCodes;

	protected ICoreClientAPI capi;

	protected EntityBehaviorGait ebg;

	protected int minGeneration;

	protected GaitMeta saddleBreakGait;

	protected string saddleBreakGaitCode;

	protected bool onlyTwoGaits;

	private ControlMeta curControlMeta;

	private bool shouldMove;

	internal string prevSoundCode;

	internal string curSoundCode;

	private string curTurnAnim;

	private EnumControlScheme scheme;

	protected float saddleBreakDayInterval;

	protected string tamedEntityCode;

	private bool wasPaused;

	private bool prevForwardKey;

	private bool prevBackwardKey;

	private bool prevSprintKey;

	private float angularMotionWild = 0.1f;

	private bool wasSwimming;

	private float notOnGroundAccum;

	private long mountedTotalMs;

	public Vec3f MountAngle { get; set; } = new Vec3f();

	public EntityPos SeatPosition => entity.SidedPos;

	public double RenderOrder => 1.0;

	public int RenderRange => 100;

	public virtual float SpeedMultiplier => 1f;

	public Entity Mount => entity;

	public int RemainingSaddleBreaks
	{
		get
		{
			return entity.WatchedAttributes.GetInt("remainingSaddleBreaksRequired");
		}
		set
		{
			entity.WatchedAttributes.SetInt("remainingSaddleBreaksRequired", value);
		}
	}

	public double LastSaddleBreakTotalDays
	{
		get
		{
			return entity.WatchedAttributes.GetDouble("lastSaddlebreakTotalDays");
		}
		set
		{
			entity.WatchedAttributes.SetDouble("lastSaddlebreakTotalDays", value);
		}
	}

	public double LastDismountTotalHours
	{
		get
		{
			return entity.WatchedAttributes.GetDouble("lastDismountTotalHours");
		}
		set
		{
			entity.WatchedAttributes.SetDouble("lastDismountTotalHours", value);
		}
	}

	public event CanRideDelegate CanRide;

	public event CanRideDelegate CanTurn;

	public EntityBehaviorRideable(Entity entity)
		: base(entity)
	{
	}

	protected override IMountableSeat CreateSeat(string seatId, SeatConfig config)
	{
		return new EntityRideableSeat(this, seatId, config);
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		api = entity.Api;
		capi = api as ICoreClientAPI;
		if (attributes["saddleBreaksRequired"].Exists)
		{
			if (!entity.WatchedAttributes.HasAttribute("requiredSaddleBreaks") && api.Side == EnumAppSide.Server)
			{
				RemainingSaddleBreaks = GameMath.RoundRandom(api.World.Rand, attributes["saddleBreaksRequired"].AsObject<NatFloat>().nextFloat(1f, api.World.Rand));
			}
			saddleBreakDayInterval = attributes["saddleBreakDayInterval"].AsFloat();
			tamedEntityCode = attributes["tamedEntityCode"].AsString();
			saddleBreakGaitCode = attributes["saddleBreakGait"].AsString();
		}
		Controls = attributes["controls"].AsObject<FastSmallDictionary<string, ControlMeta>>();
		minGeneration = attributes["minGeneration"].AsInt();
		GaitOrderCodes = attributes["rideableGaitOrder"].AsArray<string>();
		foreach (ControlMeta value in Controls.Values)
		{
			value.RiderAnim?.Init();
			value.PassengerAnim?.Init();
		}
		curAnim = Controls["idle"].RiderAnim;
		curAnimPassanger = Controls["idle"].GetPassengerAnim();
		capi?.Event.RegisterRenderer(this, EnumRenderStage.Before, "rideablesim");
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		ebg = eagent.GetBehavior<EntityBehaviorGait>();
		if (ebg == null)
		{
			throw new Exception("EntityBehaviorGait not found on rideable entity. Ensure it is properly registered in the entity's properties.");
		}
		string[] gaitOrderCodes = GaitOrderCodes;
		foreach (string key in gaitOrderCodes)
		{
			GaitMeta gaitMeta = ebg?.Gaits[key];
			if (gaitMeta != null)
			{
				RideableGaitOrder.Add(gaitMeta);
			}
		}
		onlyTwoGaits = RideableGaitOrder.Count((GaitMeta g) => g.MoveSpeed > 0f && !g.Backwards) == 2;
		saddleBreakGait = ebg.Gaits.FirstOrDefault<KeyValuePair<string, GaitMeta>>((KeyValuePair<string, GaitMeta> g) => g.Value.Code == saddleBreakGaitCode).Value;
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		capi?.Event.UnregisterRenderer(this, EnumRenderStage.Before);
	}

	public void UnmnountPassengers()
	{
		IMountableSeat[] seats = base.Seats;
		for (int i = 0; i < seats.Length; i++)
		{
			(seats[i].Passenger as EntityAgent)?.TryUnmount();
		}
	}

	public override void OnEntityLoaded()
	{
		setupTaskBlocker();
	}

	public override void OnEntitySpawn()
	{
		setupTaskBlocker();
	}

	private void setupTaskBlocker()
	{
		EntityBehaviorAttachable behavior = entity.GetBehavior<EntityBehaviorAttachable>();
		if (api.Side == EnumAppSide.Server)
		{
			entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager.OnShouldExecuteTask += TaskManager_OnShouldExecuteTask;
			if (behavior != null)
			{
				behavior.Inventory.SlotModified += Inventory_SlotModified;
			}
		}
		else if (behavior != null)
		{
			entity.WatchedAttributes.RegisterModifiedListener(behavior.InventoryClassName, updateControlScheme);
		}
	}

	private void Inventory_SlotModified(int obj)
	{
		updateControlScheme();
		ebg?.SetIdle();
	}

	private void updateControlScheme()
	{
		EntityBehaviorAttachable behavior = entity.GetBehavior<EntityBehaviorAttachable>();
		if (behavior == null)
		{
			return;
		}
		scheme = EnumControlScheme.Hold;
		foreach (ItemSlot item in behavior.Inventory)
		{
			if (item.Empty)
			{
				continue;
			}
			string text = item.Itemstack.ItemAttributes?["controlScheme"].AsString();
			if (text != null)
			{
				if (Enum.TryParse<EnumControlScheme>(text, out scheme))
				{
					break;
				}
				scheme = EnumControlScheme.Hold;
			}
		}
	}

	private bool TaskManager_OnShouldExecuteTask(IAiTask task)
	{
		if (task is AiTaskWander && api.World.Calendar.TotalHours - LastDismountTotalHours < 24.0)
		{
			return false;
		}
		return !base.Seats.Any((IMountableSeat seat) => seat.Passenger != null);
	}

	public void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		if (!wasPaused && capi.IsGamePaused)
		{
			gaitSound?.Pause();
		}
		if (wasPaused && !capi.IsGamePaused)
		{
			ILoadedSound loadedSound = gaitSound;
			if (loadedSound != null && loadedSound.IsPaused)
			{
				gaitSound?.Start();
			}
		}
		wasPaused = capi.IsGamePaused;
		if (!capi.IsGamePaused)
		{
			updateAngleAndMotion(dt);
		}
	}

	protected virtual void updateAngleAndMotion(float dt)
	{
		dt = Math.Min(0.5f, dt);
		float physicsFrameTime = GlobalConstants.PhysicsFrameTime;
		Vec2d vec2d = SeatsToMotion(physicsFrameTime);
		if (jumpNow)
		{
			updateRidingState();
		}
		ForwardSpeed = Math.Sign(vec2d.X);
		float yawMultiplier = ebg.GetYawMultiplier();
		AngularVelocity = vec2d.Y * (double)yawMultiplier;
		entity.SidedPos.Yaw += (float)vec2d.Y * dt * 30f;
		entity.SidedPos.Yaw = entity.SidedPos.Yaw % ((float)Math.PI * 2f);
		if (entity.World.ElapsedMilliseconds - lastJumpMs < 2000 && entity.World.ElapsedMilliseconds - lastJumpMs > 200 && entity.OnGround)
		{
			eagent.StopAnimation("jump");
		}
	}

	public void SpeedUp()
	{
		SetNextGait(forward: true);
	}

	public void SlowDown()
	{
		SetNextGait(forward: false);
	}

	public GaitMeta GetNextGait(bool forward, GaitMeta currentGait = null)
	{
		if ((object)currentGait == null)
		{
			currentGait = ebg.CurrentGait;
		}
		if (eagent.Swimming)
		{
			if (!forward)
			{
				return ebg.Gaits["swimback"];
			}
			return ebg.Gaits["swim"];
		}
		if (RideableGaitOrder != null && RideableGaitOrder.Count > 0 && this.IsBeingControlled())
		{
			int num = RideableGaitOrder.IndexOf(currentGait);
			int num2 = (forward ? (num + 1) : (num - 1));
			if (num2 < 0)
			{
				num2 = 0;
			}
			if (num2 >= RideableGaitOrder.Count)
			{
				num2 = num - 1;
			}
			return RideableGaitOrder[num2];
		}
		return ebg.IdleGait;
	}

	public void SetNextGait(bool forward, GaitMeta nextGait = null)
	{
		if ((object)nextGait == null)
		{
			nextGait = GetNextGait(forward);
		}
		ebg.CurrentGait = nextGait;
	}

	public GaitMeta GetFirstForwardGait()
	{
		if (RideableGaitOrder == null || RideableGaitOrder.Count == 0)
		{
			return ebg.IdleGait;
		}
		return RideableGaitOrder.FirstOrDefault((GaitMeta g) => !g.Backwards && g.MoveSpeed > 0f) ?? ebg.IdleGait;
	}

	public virtual Vec2d SeatsToMotion(float dt)
	{
		int num = 0;
		double num2 = 0.0;
		double num3 = 0.0;
		jumpNow = false;
		coyoteTimer -= dt;
		base.Controller = null;
		IMountableSeat[] seats = base.Seats;
		foreach (IMountableSeat mountableSeat in seats)
		{
			if (mountableSeat.Config.Controllable && mountableSeat.Passenger != null)
			{
				base.Controller = mountableSeat.Passenger;
				break;
			}
		}
		seats = base.Seats;
		foreach (IMountableSeat mountableSeat2 in seats)
		{
			if (entity.OnGround)
			{
				coyoteTimer = 0.15f;
			}
			if (mountableSeat2.Passenger == null)
			{
				continue;
			}
			if (mountableSeat2.Passenger is EntityPlayer entityPlayer)
			{
				entityPlayer.Controls.LeftMouseDown = mountableSeat2.Controls.LeftMouseDown;
				if (entityPlayer.HeadYawLimits == null)
				{
					entityPlayer.BodyYawLimits = new AngleConstraint(entity.Pos.Yaw + mountableSeat2.Config.MountRotation.Y * ((float)Math.PI / 180f), mountableSeat2.Config.BodyYawLimit ?? ((float)Math.PI / 2f));
					entityPlayer.HeadYawLimits = new AngleConstraint(entity.Pos.Yaw + mountableSeat2.Config.MountRotation.Y * ((float)Math.PI / 180f), (float)Math.PI / 2f);
				}
				else
				{
					entityPlayer.BodyYawLimits.X = entity.Pos.Yaw + mountableSeat2.Config.MountRotation.Y * ((float)Math.PI / 180f);
					entityPlayer.BodyYawLimits.Y = mountableSeat2.Config.BodyYawLimit ?? ((float)Math.PI / 2f);
					entityPlayer.HeadYawLimits.X = entity.Pos.Yaw + mountableSeat2.Config.MountRotation.Y * ((float)Math.PI / 180f);
					entityPlayer.HeadYawLimits.Y = (float)Math.PI / 2f;
				}
			}
			if (base.Controller != mountableSeat2.Passenger)
			{
				continue;
			}
			EntityControls controls = mountableSeat2.Controls;
			bool flag = true;
			bool flag2 = true;
			if (RemainingSaddleBreaks > 0)
			{
				if (api.World.Rand.NextDouble() < 0.05)
				{
					angularMotionWild = ((float)api.World.Rand.NextDouble() * 2f - 1f) / 10f;
				}
				num3 = angularMotionWild;
				flag2 = false;
			}
			if (this.CanRide != null && (controls.Jump || controls.TriesToMove))
			{
				Delegate[] invocationList = this.CanRide.GetInvocationList();
				for (int j = 0; j < invocationList.Length; j++)
				{
					if (!((CanRideDelegate)invocationList[j])(mountableSeat2, out var errorMessage))
					{
						if (capi != null && mountableSeat2.Passenger == capi.World.Player.Entity)
						{
							capi.TriggerIngameError(this, "cantride", Lang.Get("cantride-" + errorMessage));
						}
						flag = false;
						break;
					}
				}
			}
			if (this.CanTurn != null && (controls.Left || controls.Right))
			{
				Delegate[] invocationList = this.CanTurn.GetInvocationList();
				for (int j = 0; j < invocationList.Length; j++)
				{
					if (!((CanRideDelegate)invocationList[j])(mountableSeat2, out var errorMessage2))
					{
						if (capi != null && mountableSeat2.Passenger == capi.World.Player.Entity)
						{
							capi.TriggerIngameError(this, "cantride", Lang.Get("cantride-" + errorMessage2));
						}
						flag2 = false;
						break;
					}
				}
			}
			if (!flag)
			{
				continue;
			}
			if (controls.Jump && entity.World.ElapsedMilliseconds - lastJumpMs > 1500 && entity.Alive && (entity.OnGround || coyoteTimer > 0f || (api.Side == EnumAppSide.Client && entity.EntityId != base.Controller.EntityId)))
			{
				lastJumpMs = entity.World.ElapsedMilliseconds;
				jumpNow = true;
			}
			if (scheme != EnumControlScheme.Hold || controls.TriesToMove)
			{
				float num4 = ((++num == 1) ? 1f : 0.5f);
				bool forward = controls.Forward;
				bool backward = controls.Backward;
				bool sprint = controls.Sprint;
				controls.Sprint = onlyTwoGaits && controls.Sprint && scheme == EnumControlScheme.Hold;
				bool flag3 = forward && !prevForwardKey;
				bool num5 = backward && !prevBackwardKey;
				bool flag4 = sprint && !prevSprintKey;
				long elapsedMilliseconds = entity.World.ElapsedMilliseconds;
				if (flag3 && ebg.IsIdle)
				{
					SpeedUp();
				}
				else if (flag3 && ebg.IsBackward)
				{
					ebg.SetIdle();
				}
				else if (ebg.IsForward && flag4 && elapsedMilliseconds - lastGaitChangeMs > 300)
				{
					SpeedUp();
					lastGaitChangeMs = elapsedMilliseconds;
				}
				if ((num5 || (!sprint && ebg.CurrentGait.IsSprint && scheme == EnumControlScheme.Hold)) && elapsedMilliseconds - lastGaitChangeMs > 300)
				{
					controls.Sprint = false;
					SlowDown();
					lastGaitChangeMs = elapsedMilliseconds;
				}
				prevSprintKey = sprint;
				prevForwardKey = scheme == EnumControlScheme.Press && forward;
				prevBackwardKey = scheme == EnumControlScheme.Press && backward;
				if (flag2 && (controls.Left || controls.Right))
				{
					float num6 = (controls.Left ? 1 : (-1));
					num3 += (double)(ebg.GetYawMultiplier() * num6 * dt);
				}
				if (ebg.IsForward || ebg.IsBackward)
				{
					float num7 = (ebg.IsForward ? 1 : (-1));
					num2 += (double)(num4 * num7 * dt * 2f);
				}
			}
		}
		return new Vec2d(num2, num3);
	}

	protected void updateRidingState()
	{
		if (!AnyMounted())
		{
			return;
		}
		if (RemainingSaddleBreaks > 0)
		{
			ForwardSpeed = 1.0;
			if (api.World.Rand.NextDouble() < 0.05)
			{
				jumpNow = true;
			}
			ebg.CurrentGait = saddleBreakGait;
			if (api.World.ElapsedMilliseconds - mountedTotalMs > 4000)
			{
				IMountableSeat[] seats = base.Seats;
				foreach (IMountableSeat mountableSeat in seats)
				{
					if (mountableSeat?.Passenger != null)
					{
						EntityAgent entityAgent = mountableSeat.Passenger as EntityAgent;
						if (api.World.Rand.NextDouble() < 0.5)
						{
							entityAgent.ReceiveDamage(new DamageSource
							{
								CauseEntity = entity,
								DamageTier = 1,
								Source = EnumDamageSource.Entity,
								SourcePos = base.Position.XYZ,
								Type = EnumDamageType.BluntAttack
							}, 1f + (float)api.World.Rand.Next(8) / 4f);
						}
						entityAgent.TryUnmount();
					}
				}
				jumpNow = false;
				ForwardSpeed = 0.0;
				Stop();
				if (api.World.Calendar.TotalDays - LastSaddleBreakTotalDays > (double)saddleBreakDayInterval)
				{
					RemainingSaddleBreaks--;
					LastSaddleBreakTotalDays = api.World.Calendar.TotalDays;
					if (RemainingSaddleBreaks <= 0)
					{
						ConvertToTamedAnimal();
					}
				}
				return;
			}
		}
		bool isInMidJump = IsInMidJump;
		IsInMidJump &= (entity.World.ElapsedMilliseconds - lastJumpMs < 500 || !entity.OnGround) && !entity.Swimming;
		if (isInMidJump && !IsInMidJump)
		{
			ControlMeta controlMeta = Controls["jump"];
			IMountableSeat[] seats = base.Seats;
			foreach (IMountableSeat mountableSeat2 in seats)
			{
				AnimationMetaData seatAnimation = controlMeta.GetSeatAnimation(mountableSeat2);
				mountableSeat2.Passenger?.AnimManager?.StopAnimation(seatAnimation.Animation);
			}
			eagent.AnimManager.StopAnimation(controlMeta.Animation);
		}
		if (eagent.Swimming)
		{
			ebg.CurrentGait = ((ForwardSpeed > 0.0) ? ebg.Gaits["swim"] : ebg.Gaits["swimback"]);
		}
		else if (!eagent.Swimming && wasSwimming)
		{
			ebg.CurrentGait = ((ForwardSpeed > 0.0) ? ebg.Gaits["walk"] : ebg.Gaits["walkback"]);
		}
		wasSwimming = eagent.Swimming;
		eagent.Controls.Backward = ForwardSpeed < 0.0;
		eagent.Controls.Forward = ForwardSpeed >= 0.0;
		eagent.Controls.Sprint = ebg.CurrentGait.IsSprint && ForwardSpeed > 0.0;
		string text = null;
		if (ForwardSpeed >= 0.0)
		{
			if (AngularVelocity > 0.001)
			{
				text = "turn-left";
			}
			else if (AngularVelocity < -0.001)
			{
				text = "turn-right";
			}
		}
		if (text != curTurnAnim)
		{
			if (curTurnAnim != null)
			{
				eagent.StopAnimation(curTurnAnim);
			}
			string code = (curTurnAnim = ((ForwardSpeed == 0.0) ? "idle-" : "") + text);
			eagent.StartAnimation(code);
		}
		shouldMove = ForwardSpeed != 0.0;
		ControlMeta controlMeta2;
		if (!shouldMove && !jumpNow)
		{
			if (curControlMeta != null)
			{
				Stop();
			}
			curAnim = Controls[eagent.Swimming ? "swim" : "idle"].RiderAnim;
			curAnimPassanger = Controls[eagent.Swimming ? "swim" : "idle"].GetPassengerAnim();
			controlMeta2 = ((!eagent.Swimming) ? null : Controls["swim"]);
		}
		else
		{
			controlMeta2 = Controls.FirstOrDefault((KeyValuePair<string, ControlMeta> c) => c.Key == ebg.CurrentGait.Code).Value;
			if (controlMeta2 == null)
			{
				controlMeta2 = Controls["idle"];
			}
			eagent.Controls.Jump = jumpNow;
			if (jumpNow)
			{
				IsInMidJump = true;
				jumpNow = false;
				if (eagent.Properties.Client.Renderer is EntityShapeRenderer entityShapeRenderer)
				{
					entityShapeRenderer.LastJumpMs = capi.InWorldEllapsedMilliseconds;
				}
				controlMeta2 = Controls["jump"];
				if (ForwardSpeed != 0.0)
				{
					controlMeta2.EaseOutSpeed = 30f;
				}
				IMountableSeat[] seats = base.Seats;
				foreach (IMountableSeat mountableSeat3 in seats)
				{
					if (mountableSeat3.Passenger == base.Controller)
					{
						AnimationMetaData seatAnimation2 = controlMeta2.GetSeatAnimation(mountableSeat3);
						mountableSeat3.Passenger?.AnimManager?.StartAnimation(seatAnimation2);
					}
				}
				IPlayer dualCallByPlayer = ((entity is EntityPlayer entityPlayer) ? entityPlayer.World.PlayerByUid(entityPlayer.PlayerUID) : null);
				entity.PlayEntitySound("jump", dualCallByPlayer, randomizePitch: false);
			}
			else
			{
				curAnim = controlMeta2.RiderAnim;
				curAnimPassanger = controlMeta2.GetPassengerAnim();
			}
		}
		if (controlMeta2 != curControlMeta)
		{
			if (curControlMeta != null && curControlMeta.Animation != "jump")
			{
				eagent.StopAnimation(curControlMeta.Animation);
			}
			curControlMeta = controlMeta2;
			if (api.Side == EnumAppSide.Server)
			{
				eagent.AnimManager.StartAnimation(controlMeta2);
			}
		}
		if (api.Side == EnumAppSide.Server)
		{
			eagent.Controls.Sprint = false;
		}
	}

	private void ConvertToTamedAnimal()
	{
		ICoreAPI coreAPI = base.entity.World.Api;
		if (coreAPI.Side != EnumAppSide.Client)
		{
			EntityProperties entityType = coreAPI.World.GetEntityType(AssetLocation.Create(tamedEntityCode, base.entity.Code.Domain));
			if (entityType != null)
			{
				Entity entity = coreAPI.World.ClassRegistry.CreateEntity(entityType);
				entity.ServerPos.SetFrom(base.entity.Pos);
				entity.WatchedAttributes = base.entity.WatchedAttributes.Clone();
				base.entity.Die(EnumDespawnReason.Expire);
				coreAPI.World.SpawnEntity(entity);
			}
		}
	}

	public override void OnEntityDeath(DamageSource damageSourceForDeath)
	{
		IMountableSeat[] seats = base.Seats;
		for (int i = 0; i < seats.Length; i++)
		{
			(seats[i]?.Entity as EntityAgent)?.TryUnmount();
		}
		base.OnEntityDeath(damageSourceForDeath);
	}

	public void Stop()
	{
		gaitSound?.Stop();
		ebg.SetIdle();
		eagent.Controls.StopAllMovement();
		eagent.Controls.WalkVector.Set(0.0, 0.0, 0.0);
		eagent.Controls.FlyVector.Set(0.0, 0.0, 0.0);
		eagent.StopAnimation(curTurnAnim);
		shouldMove = false;
		if (curControlMeta != null && curControlMeta.Animation != "jump")
		{
			eagent.StopAnimation(curControlMeta.Animation);
		}
		curControlMeta = null;
		eagent.StartAnimation("idle");
	}

	public override void OnGameTick(float dt)
	{
		if (api.Side == EnumAppSide.Server)
		{
			updateAngleAndMotion(dt);
		}
		updateRidingState();
		if (!AnyMounted() && eagent.Controls.TriesToMove && eagent?.MountedOn != null)
		{
			eagent.TryUnmount();
		}
		if (shouldMove)
		{
			float nowMoveSpeed = ((curControlMeta.MoveSpeed > 0f) ? curControlMeta.MoveSpeed : (ebg.CurrentGait.MoveSpeed * curControlMeta.MoveSpeedMultiplier));
			move(dt, eagent.Controls, nowMoveSpeed);
		}
		else if (entity.Swimming)
		{
			eagent.Controls.FlyVector.Y = 0.2;
		}
		updateSoundState(dt);
	}

	private void updateSoundState(float dt)
	{
		if (capi == null)
		{
			return;
		}
		if (eagent.OnGround)
		{
			notOnGroundAccum = 0f;
		}
		else
		{
			notOnGroundAccum += dt;
		}
		gaitSound?.SetPosition((float)entity.Pos.X, (float)entity.Pos.Y, (float)entity.Pos.Z);
		if (!Controls.TryGetValue(ebg.CurrentGait.Code, out var _))
		{
			return;
		}
		GaitMeta currentGait = ebg.CurrentGait;
		curSoundCode = ((eagent.Swimming || (double)notOnGroundAccum > 0.2) ? null : currentGait.Sound);
		if (curSoundCode != prevSoundCode)
		{
			gaitSound?.Stop();
			gaitSound?.Dispose();
			prevSoundCode = curSoundCode;
			if (curSoundCode != null)
			{
				gaitSound = capi.World.LoadSound(new SoundParams
				{
					Location = currentGait.Sound.Clone().WithPathPrefix("sounds/"),
					DisposeOnFinish = false,
					Position = entity.Pos.XYZ.ToVec3f(),
					ShouldLoop = true
				});
				gaitSound?.Start();
			}
		}
	}

	private void move(float dt, EntityControls controls, float nowMoveSpeed)
	{
		double z = Math.Cos(entity.Pos.Yaw);
		double x = Math.Sin(entity.Pos.Yaw);
		controls.WalkVector.Set(x, 0.0, z);
		controls.WalkVector.Mul((double)(nowMoveSpeed * GlobalConstants.OverallSpeedMultiplier) * ForwardSpeed);
		if (entity.Properties.RotateModelOnClimb && controls.IsClimbing && entity.ClimbingOnFace != null && entity.Alive)
		{
			BlockFacing climbingOnFace = entity.ClimbingOnFace;
			if (Math.Sign(climbingOnFace.Normali.X) == Math.Sign(controls.WalkVector.X))
			{
				controls.WalkVector.X = 0.0;
			}
			if (Math.Sign(climbingOnFace.Normali.Z) == Math.Sign(controls.WalkVector.Z))
			{
				controls.WalkVector.Z = 0.0;
			}
		}
		if (entity.Swimming)
		{
			controls.FlyVector.Set(controls.WalkVector);
			Vec3d xYZ = entity.Pos.XYZ;
			Block blockRaw = entity.World.BlockAccessor.GetBlockRaw((int)xYZ.X, (int)xYZ.Y, (int)xYZ.Z, 2);
			Block blockRaw2 = entity.World.BlockAccessor.GetBlockRaw((int)xYZ.X, (int)(xYZ.Y + 1.0), (int)xYZ.Z, 2);
			float num = GameMath.Clamp((float)(int)xYZ.Y + (float)blockRaw.LiquidLevel / 8f + (blockRaw2.IsLiquid() ? 1.125f : 0f) - (float)xYZ.Y - (float)entity.SwimmingOffsetY, 0f, 1f);
			num = Math.Min(1f, num + 0.075f);
			controls.FlyVector.Y = GameMath.Clamp(controls.FlyVector.Y, 0.0020000000949949026, 0.004000000189989805) * (double)num * 3.0;
			if (entity.CollidedHorizontally)
			{
				controls.FlyVector.Y = 0.05000000074505806;
			}
			eagent.Pos.Motion.Y += ((double)num - 0.1) / 300.0;
		}
	}

	public override string PropertyName()
	{
		return "rideable";
	}

	public void Dispose()
	{
	}

	public void DidUnmount(EntityAgent entityAgent)
	{
		Stop();
		LastDismountTotalHours = entity.World.Calendar.TotalHours;
		foreach (ControlMeta value in Controls.Values)
		{
			if (value.RiderAnim?.Animation != null)
			{
				entityAgent.StopAnimation(value.RiderAnim.Animation);
			}
		}
		if (eagent.Swimming)
		{
			eagent.StartAnimation("swim");
		}
	}

	public void DidMount(EntityAgent entityAgent)
	{
		updateControlScheme();
		mountedTotalMs = api.World.ElapsedMilliseconds;
	}

	public override bool ToleratesDamageFrom(Entity eOther, ref EnumHandling handling)
	{
		if (eOther != null && base.Controller == eOther)
		{
			handling = EnumHandling.PreventDefault;
			return true;
		}
		return false;
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		if (RemainingSaddleBreaks > 0)
		{
			infotext.AppendLine(Lang.Get("{0} saddle breaks required every {1} days to fully tame.", RemainingSaddleBreaks, saddleBreakDayInterval));
		}
		base.GetInfoText(infotext);
	}
}
