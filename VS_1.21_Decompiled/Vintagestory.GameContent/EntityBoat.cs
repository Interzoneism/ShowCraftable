using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBoat : Entity, IRenderer, IDisposable, ISeatInstSupplier, IMountableListener
{
	public double ForwardSpeed;

	public double AngularVelocity;

	private ModSystemBoatingSoundAndRatlineStamina modsysSounds;

	private double swimmingOffsetY;

	public Dictionary<string, string> MountAnimations = new Dictionary<string, string>();

	private bool requiresPaddlingTool;

	private bool unfurlSails;

	private string weatherVaneAnimCode;

	private long CurrentlyControllingEntityId;

	private ICoreClientAPI capi;

	private float curRotMountAngleZ;

	public Vec3f mountAngle = new Vec3f();

	public override double FrustumSphereRadius => base.FrustumSphereRadius * 2.5;

	public override bool IsCreature => true;

	public override bool ApplyGravity => true;

	public override bool IsInteractable => true;

	public override float MaterialDensity => 100f;

	public override double SwimmingOffsetY => swimmingOffsetY;

	public virtual float SpeedMultiplier { get; set; } = 1f;

	public double RenderOrder => 0.0;

	public int RenderRange => 999;

	public string CreatedByPlayername => WatchedAttributes.GetString("createdByPlayername");

	public string CreatedByPlayerUID => WatchedAttributes.GetString("createdByPlayerUID");

	protected int sailPosition
	{
		get
		{
			return WatchedAttributes.GetInt("sailPosition");
		}
		set
		{
			WatchedAttributes.SetInt("sailPosition", value);
		}
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		swimmingOffsetY = properties.Attributes["swimmingOffsetY"].AsDouble();
		SpeedMultiplier = properties.Attributes["speedMultiplier"].AsFloat(1f);
		MountAnimations = properties.Attributes["mountAnimations"].AsObject<Dictionary<string, string>>();
		weatherVaneAnimCode = properties.Attributes["weatherVaneAnimCode"].AsString();
		base.Initialize(properties, api, InChunkIndex3d);
		WatchedAttributes.RegisterModifiedListener("sailPosition", MarkShapeModified);
		requiresPaddlingTool = properties.Attributes["requiresPaddlingTool"].AsBool();
		unfurlSails = properties.Attributes["unfurlSails"].AsBool();
		capi = api as ICoreClientAPI;
		if (capi != null)
		{
			capi.Event.RegisterRenderer(this, EnumRenderStage.Before, "boatsim");
			modsysSounds = api.ModLoader.GetModSystem<ModSystemBoatingSoundAndRatlineStamina>();
		}
	}

	public override void OnTesselation(ref Shape entityShape, string shapePathForLogging)
	{
		Shape shape = entityShape;
		if (unfurlSails)
		{
			if (shape == entityShape)
			{
				entityShape = entityShape.Clone();
			}
			switch (sailPosition)
			{
			case 0:
				entityShape.RemoveElementByName("SailUnfurled");
				entityShape.RemoveElementByName("SailHalf");
				break;
			case 1:
				entityShape.RemoveElementByName("SailFurled");
				entityShape.RemoveElementByName("SailUnfurled");
				break;
			case 2:
				entityShape.RemoveElementByName("SailFurled");
				entityShape.RemoveElementByName("SailHalf");
				break;
			}
		}
		base.OnTesselation(ref entityShape, shapePathForLogging);
	}

	public virtual void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		if (capi.IsGamePaused)
		{
			return;
		}
		updateBoatAngleAndMotion(dt);
		long inWorldEllapsedMilliseconds = capi.InWorldEllapsedMilliseconds;
		float num = 0f;
		if (Swimming)
		{
			double num2 = capi.World.Calendar.SpeedOfTime / 60f;
			float num3 = (0.15f + GlobalConstants.CurrentWindSpeedClient.X * 0.9f) * (unfurlSails ? 0.7f : 1f);
			float num4 = (float)Math.PI / 360f * num3;
			mountAngle.X = GameMath.Sin((float)((double)inWorldEllapsedMilliseconds / 1000.0 * 2.0 * num2)) * 8f * num4;
			mountAngle.Y = GameMath.Cos((float)((double)inWorldEllapsedMilliseconds / 2000.0 * 2.0 * num2)) * 3f * num4;
			mountAngle.Z = (0f - GameMath.Sin((float)((double)inWorldEllapsedMilliseconds / 3000.0 * 2.0 * num2))) * 8f * num4;
			curRotMountAngleZ += ((float)AngularVelocity * 5f * (float)Math.Sign(ForwardSpeed) - curRotMountAngleZ) * dt * 5f;
			num = (0f - (float)ForwardSpeed) * 1.3f * (unfurlSails ? 0.5f : 1f);
		}
		if (!(base.Properties.Client.Renderer is EntityShapeRenderer entityShapeRenderer))
		{
			return;
		}
		entityShapeRenderer.xangle = mountAngle.X + curRotMountAngleZ;
		entityShapeRenderer.yangle = mountAngle.Y;
		entityShapeRenderer.zangle = mountAngle.Z + num;
		if (AnimManager.Animator != null)
		{
			if (weatherVaneAnimCode != null && !AnimManager.IsAnimationActive(weatherVaneAnimCode))
			{
				AnimManager.StartAnimation(weatherVaneAnimCode);
			}
			float num5 = GameMath.Mod((float)Math.Atan2(GlobalConstants.CurrentWindSpeedClient.X, GlobalConstants.CurrentWindSpeedClient.Z) + (float)Math.PI * 2f - Pos.Yaw, (float)Math.PI * 2f);
			RunningAnimation animationState = AnimManager.GetAnimationState(weatherVaneAnimCode);
			if (animationState != null)
			{
				animationState.CurrentFrame = num5 * (180f / (float)Math.PI) / 10f;
				animationState.BlendedWeight = 1f;
				animationState.EasingFactor = 1f;
			}
		}
	}

	public override void OnGameTick(float dt)
	{
		if (World.Side == EnumAppSide.Server)
		{
			_ = World.ElapsedMilliseconds;
			if (base.IsOnFire && World.ElapsedMilliseconds - OnFireBeginTotalMs > 10000)
			{
				Die();
			}
			updateBoatAngleAndMotion(dt);
		}
		base.OnGameTick(dt);
	}

	public override void OnAsyncParticleTick(float dt, IAsyncParticleManager manager)
	{
		base.OnAsyncParticleTick(dt, manager);
		double num = Math.Abs(ForwardSpeed) + Math.Abs(AngularVelocity);
		if (num > 0.01)
		{
			float num2 = -3f;
			float num3 = 6f;
			float num4 = -0.75f;
			float num5 = 1.5f;
			EntityPos pos = Pos;
			Random rand = Api.World.Rand;
			Entity.SplashParticleProps.AddVelocity.Set((float)pos.Motion.X * 20f, (float)pos.Motion.Y, (float)pos.Motion.Z * 20f);
			Entity.SplashParticleProps.AddPos.Set(0.10000000149011612, 0.0, 0.10000000149011612);
			Entity.SplashParticleProps.QuantityMul = 0.5f * (float)num;
			double y = pos.Y - 0.15;
			for (int i = 0; i < 10; i++)
			{
				float num6 = num2 + (float)rand.NextDouble() * num3;
				float num7 = num4 + (float)rand.NextDouble() * num5;
				double num8 = (double)(Pos.Yaw + (float)Math.PI / 2f) + Math.Atan2(num6, num7);
				double num9 = Math.Sqrt(num6 * num6 + num7 * num7);
				Entity.SplashParticleProps.BasePos.Set(pos.X + Math.Sin(num8) * num9, y, pos.Z + Math.Cos(num8) * num9);
				manager.Spawn(Entity.SplashParticleProps);
			}
		}
	}

	protected virtual void updateBoatAngleAndMotion(float dt)
	{
		dt = Math.Min(0.5f, dt);
		float physicsFrameTime = GlobalConstants.PhysicsFrameTime;
		Vec2d vec2d = SeatsToMotion(physicsFrameTime);
		if (!Swimming)
		{
			if (!unfurlSails || sailPosition > 0)
			{
				return;
			}
			bool flag = false;
			IMountableSeat[] seats = GetBehavior<EntityBehaviorSeatable>().Seats;
			for (int i = 0; i < seats.Length; i++)
			{
				EntityBoatSeat entityBoatSeat = seats[i] as EntityBoatSeat;
				if (entityBoatSeat.Config.Controllable && entityBoatSeat.Passenger is EntityPlayer && entityBoatSeat.Passenger.EntityId == CurrentlyControllingEntityId)
				{
					EntityControls controls = entityBoatSeat.controls;
					if (controls.Forward || controls.Backward)
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				return;
			}
			vec2d *= 0.5;
		}
		ForwardSpeed += (vec2d.X * (double)SpeedMultiplier - ForwardSpeed) * (double)dt;
		AngularVelocity += (vec2d.Y * (double)SpeedMultiplier - AngularVelocity) * (double)dt;
		EntityPos sidedPos = base.SidedPos;
		if (ForwardSpeed != 0.0)
		{
			Vec3d vec3d = sidedPos.GetViewVector().Mul((float)(0.0 - ForwardSpeed)).ToVec3d();
			sidedPos.Motion.X = vec3d.X;
			sidedPos.Motion.Z = vec3d.Z;
		}
		EntityBehaviorPassivePhysicsMultiBox behavior = GetBehavior<EntityBehaviorPassivePhysicsMultiBox>();
		bool flag2 = true;
		if (AngularVelocity != 0.0)
		{
			float num = (float)AngularVelocity * dt * 30f;
			if (behavior.AdjustCollisionBoxesToYaw(dt, push: true, base.SidedPos.Yaw + num))
			{
				sidedPos.Yaw += num;
			}
			else
			{
				flag2 = false;
			}
		}
		else
		{
			flag2 = behavior.AdjustCollisionBoxesToYaw(dt, push: true, base.SidedPos.Yaw);
		}
		if (!flag2)
		{
			if (behavior.AdjustCollisionBoxesToYaw(dt, push: true, base.SidedPos.Yaw - 0.1f))
			{
				sidedPos.Yaw -= 0.0002f;
			}
			else if (behavior.AdjustCollisionBoxesToYaw(dt, push: true, base.SidedPos.Yaw + 0.1f))
			{
				sidedPos.Yaw += 0.0002f;
			}
		}
		sidedPos.Roll = 0f;
	}

	protected virtual bool HasPaddle(Entity entity)
	{
		if (!requiresPaddlingTool)
		{
			return true;
		}
		if (!(entity is EntityAgent entityAgent))
		{
			return false;
		}
		if (entityAgent.RightHandItemSlot == null || entityAgent.RightHandItemSlot.Empty)
		{
			return false;
		}
		return entityAgent.RightHandItemSlot.Itemstack.Collectible.Attributes?.IsTrue("paddlingTool") ?? false;
	}

	public virtual Vec2d SeatsToMotion(float dt)
	{
		int num = 0;
		double num2 = 0.0;
		double num3 = 0.0;
		EntityBehaviorSeatable behavior = GetBehavior<EntityBehaviorSeatable>();
		behavior.Controller = null;
		IMountableSeat[] seats = behavior.Seats;
		for (int i = 0; i < seats.Length; i++)
		{
			EntityBoatSeat entityBoatSeat = seats[i] as EntityBoatSeat;
			if (entityBoatSeat.Passenger == null)
			{
				continue;
			}
			if (!(entityBoatSeat.Passenger is EntityPlayer))
			{
				entityBoatSeat.Passenger.SidedPos.Yaw = base.SidedPos.Yaw;
			}
			if (entityBoatSeat.Config.BodyYawLimit.HasValue && entityBoatSeat.Passenger is EntityPlayer entityPlayer)
			{
				if (entityPlayer.BodyYawLimits == null)
				{
					entityPlayer.BodyYawLimits = new AngleConstraint(Pos.Yaw + entityBoatSeat.Config.MountRotation.Y * ((float)Math.PI / 180f), entityBoatSeat.Config.BodyYawLimit.Value);
					entityPlayer.HeadYawLimits = new AngleConstraint(Pos.Yaw + entityBoatSeat.Config.MountRotation.Y * ((float)Math.PI / 180f), (float)Math.PI / 2f);
				}
				else
				{
					entityPlayer.BodyYawLimits.X = Pos.Yaw + entityBoatSeat.Config.MountRotation.Y * ((float)Math.PI / 180f);
					entityPlayer.BodyYawLimits.Y = entityBoatSeat.Config.BodyYawLimit.Value;
					entityPlayer.HeadYawLimits.X = Pos.Yaw + entityBoatSeat.Config.MountRotation.Y * ((float)Math.PI / 180f);
					entityPlayer.HeadYawLimits.Y = (float)Math.PI / 2f;
				}
			}
			if (!entityBoatSeat.Config.Controllable || behavior.Controller != null)
			{
				continue;
			}
			EntityControls controls = entityBoatSeat.controls;
			if (entityBoatSeat.Passenger.EntityId != CurrentlyControllingEntityId)
			{
				continue;
			}
			behavior.Controller = entityBoatSeat.Passenger;
			if (!HasPaddle(entityBoatSeat.Passenger))
			{
				entityBoatSeat.Passenger.AnimManager?.StopAnimation(MountAnimations["ready"]);
				entityBoatSeat.actionAnim = null;
				continue;
			}
			if (controls.Left == controls.Right && capi == null)
			{
				StopAnimation("turnLeft");
				StopAnimation("turnRight");
			}
			if (controls.Left && !controls.Right)
			{
				StartAnimation("turnLeft");
				StopAnimation("turnRight");
			}
			if (controls.Right && !controls.Left)
			{
				StopAnimation("turnLeft");
				StartAnimation("turnRight");
			}
			float num4 = ((++num == 1) ? 1f : 0.5f);
			if (unfurlSails && sailPosition > 0)
			{
				num2 += (double)(num4 * dt * (float)sailPosition * 1.5f);
			}
			if (!controls.TriesToMove)
			{
				entityBoatSeat.actionAnim = null;
				if (entityBoatSeat.Passenger.AnimManager != null && !entityBoatSeat.Passenger.AnimManager.IsAnimationActive(MountAnimations["ready"]))
				{
					entityBoatSeat.Passenger.AnimManager.StartAnimation(MountAnimations["ready"]);
				}
				continue;
			}
			if (controls.Right && !controls.Backward && !controls.Forward)
			{
				entityBoatSeat.actionAnim = MountAnimations["backwards"];
			}
			else
			{
				entityBoatSeat.actionAnim = MountAnimations[controls.Backward ? "backwards" : "forwards"];
			}
			entityBoatSeat.Passenger.AnimManager?.StopAnimation(MountAnimations["ready"]);
			if (controls.Left || controls.Right)
			{
				float num5 = (controls.Left ? 1 : (-1));
				num3 += (double)(num4 * num5 * dt);
			}
			if (controls.Forward || controls.Backward)
			{
				float num6 = (controls.Forward ? 1 : (-1));
				if (Math.Abs(GameMath.AngleRadDistance(base.SidedPos.Yaw, entityBoatSeat.Passenger.SidedPos.Yaw)) > (float)Math.PI / 2f && requiresPaddlingTool)
				{
					num6 *= -1f;
				}
				float num7 = 2f;
				if (unfurlSails)
				{
					num7 = ((sailPosition == 0) ? 0.4f : 0f);
				}
				num2 += (double)(num4 * num6 * dt * num7);
			}
		}
		return new Vec2d(num2, num3);
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode)
	{
		if (mode == EnumInteractMode.Interact && AllowPickup() && IsEmpty() && tryPickup(byEntity, mode))
		{
			return;
		}
		EntityBehaviorSelectionBoxes? behavior = GetBehavior<EntityBehaviorSelectionBoxes>();
		if (behavior != null && behavior.IsAPCode((byEntity as EntityPlayer).EntitySelection, "LowerMastAP"))
		{
			sailPosition = (ushort)((sailPosition + 1) % 3);
		}
		EnumHandling handled = EnumHandling.PassThrough;
		foreach (EntityBehavior behavior2 in base.SidedProperties.Behaviors)
		{
			behavior2.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	private bool AllowPickup()
	{
		return base.Properties.Attributes?["rightClickPickup"].AsBool() ?? false;
	}

	private bool IsEmpty()
	{
		EntityBehaviorSeatable? behavior = GetBehavior<EntityBehaviorSeatable>();
		EntityBehaviorRideableAccessories behavior2 = GetBehavior<EntityBehaviorRideableAccessories>();
		if (!behavior.AnyMounted())
		{
			return behavior2?.Inventory.Empty ?? true;
		}
		return false;
	}

	private bool tryPickup(EntityAgent byEntity, EnumInteractMode mode)
	{
		if (byEntity.Controls.ShiftKey)
		{
			ItemStack itemStack = new ItemStack(World.GetItem(Code));
			if (!byEntity.TryGiveItemStack(itemStack))
			{
				World.SpawnItemEntity(itemStack, ServerPos.XYZ);
			}
			Api.World.Logger.Audit("{0} Picked up 1x{1} at {2}.", byEntity.GetName(), itemStack.Collectible.Code, Pos);
			Die();
			return true;
		}
		return false;
	}

	public override bool CanCollect(Entity byEntity)
	{
		return false;
	}

	public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player)
	{
		WorldInteraction[] array = base.GetInteractionHelp(world, es, player);
		EntityBehaviorSelectionBoxes? behavior = GetBehavior<EntityBehaviorSelectionBoxes>();
		if (behavior != null && behavior.IsAPCode(es, "LowerMastAP"))
		{
			array = array.Append(new WorldInteraction
			{
				ActionLangCode = "sailboat-unfurlsails",
				MouseButton = EnumMouseButton.Right
			});
		}
		return array;
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		capi?.Event.UnregisterRenderer(this, EnumRenderStage.Before);
	}

	public void Dispose()
	{
	}

	public IMountableSeat CreateSeat(IMountable mountable, string seatId, SeatConfig config)
	{
		return new EntityBoatSeat(mountable, seatId, config);
	}

	public void DidUnmount(EntityAgent entityAgent)
	{
		if (CurrentlyControllingEntityId == entityAgent.EntityId)
		{
			EntityBehaviorSeatable behavior = GetBehavior<EntityBehaviorSeatable>();
			CurrentlyControllingEntityId = behavior.Seats.FirstOrDefault((IMountableSeat seat) => seat.CanControl && seat.Passenger != null)?.Passenger.EntityId ?? 0;
		}
		MarkShapeModified();
	}

	public void DidMount(EntityAgent entityAgent)
	{
		if (entityAgent.MountedOn.CanControl && CurrentlyControllingEntityId <= 0)
		{
			CurrentlyControllingEntityId = entityAgent.EntityId;
		}
		MarkShapeModified();
	}

	public override string GetInfoText()
	{
		string text = base.GetInfoText();
		if (CreatedByPlayername != null)
		{
			text = text + "\n" + Lang.Get("entity-createdbyplayer", CreatedByPlayername);
		}
		return text;
	}

	public override bool InRangeOf(Vec3d position, float horRangeSq, float vertRange)
	{
		return base.SidedPos.InRangeOf(position, horRangeSq + 64f, vertRange);
	}
}
