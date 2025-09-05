using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityRideableSeat : EntitySeat
{
	protected EntityPos seatPos = new EntityPos();

	protected Matrixf modelmat = new Matrixf();

	protected string RideableClassName = "rideableanimal";

	public override EnumMountAngleMode AngleMode => EnumMountAngleMode.FixateYaw;

	public override AnimationMetaData SuggestedAnimation
	{
		get
		{
			if (!base.CanControl)
			{
				return (mountedEntity as EntityBehaviorRideable).curAnimPassanger;
			}
			return (mountedEntity as EntityBehaviorRideable).curAnim;
		}
	}

	public override Vec3f LocalEyePos
	{
		get
		{
			modelmat.Identity();
			if (Entity.AnimManager?.Animator?.GetAttachmentPointPose(config.APName) != null)
			{
				modelmat.RotateY((float)Math.PI / 2f + Entity.Pos.Yaw);
				modelmat.RotateX((Entity.Properties.Client.Renderer as EntityShapeRenderer)?.nowSwivelRad ?? 0f);
				modelmat.Translate(0f, config.EyeHeight, 0f);
				modelmat.RotateY(-(float)Math.PI / 2f - Entity.Pos.Yaw);
			}
			return modelmat.TransformVector(new Vec4f(0f, 0f, 0f, 1f)).XYZ;
		}
	}

	public override EntityPos SeatPosition
	{
		get
		{
			loadAttachPointTransform();
			Vec4f vec4f = modelmat.TransformVector(new Vec4f(0f, 0f, 0f, 1f));
			seatPos.SetFrom(mountedEntity.Position).Add(vec4f.X, vec4f.Y, vec4f.Z);
			seatPos.Pitch = (float)mountedEntity.StepPitch * 0.55f;
			return seatPos;
		}
	}

	public override Matrixf RenderTransform
	{
		get
		{
			loadAttachPointTransform();
			Vec4f vec4f = modelmat.TransformVector(new Vec4f(0f, 0f, 0f, 1f));
			return new Matrixf().Translate(0f - vec4f.X, 0f - vec4f.Y, 0f - vec4f.Z).Mul(modelmat).RotateDeg(config.MountRotation);
		}
	}

	public override float FpHandPitchFollow => 0.2f;

	private void loadAttachPointTransform()
	{
		modelmat.Identity();
		AttachmentPointAndPose attachmentPointAndPose = Entity.AnimManager?.Animator?.GetAttachmentPointPose(config.APName);
		if (attachmentPointAndPose != null)
		{
			EntityShapeRenderer entityShapeRenderer = Entity.Properties.Client.Renderer as EntityShapeRenderer;
			modelmat.RotateY((float)Math.PI / 2f + Entity.Pos.Yaw);
			modelmat.Translate(0.0, 0.6, 0.0);
			if (entityShapeRenderer != null)
			{
				modelmat.RotateX(entityShapeRenderer.nowSwivelRad + entityShapeRenderer.xangle);
				modelmat.RotateY(entityShapeRenderer.yangle);
				modelmat.RotateZ(entityShapeRenderer.zangle);
			}
			modelmat.Translate(0.0, -0.6, 0.0);
			modelmat.Translate(-0.5, 0.5, -0.5);
			attachmentPointAndPose.Mul(modelmat);
			if (config.MountOffset != null)
			{
				modelmat.Translate(config.MountOffset);
			}
			modelmat.Translate(-1.0, -0.5, -1.0);
			modelmat.RotateY((float)Math.PI / 2f - Entity.Pos.Yaw);
		}
	}

	public EntityRideableSeat(IMountable mountablesupplier, string seatId, SeatConfig config)
		: base(mountablesupplier, seatId, config)
	{
	}

	public override bool CanMount(EntityAgent entityAgent)
	{
		if (!(entityAgent is EntityPlayer entityPlayer))
		{
			return false;
		}
		EntityBehaviorOwnable behavior = Entity.GetBehavior<EntityBehaviorOwnable>();
		if (behavior != null && !behavior.IsOwner(entityPlayer) && config.Controllable)
		{
			(entityPlayer.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "requiersownership", Lang.Get("mount-interact-requiresownership"));
			return false;
		}
		return true;
	}

	public static IMountableSeat GetMountable(IWorldAccessor world, TreeAttribute tree)
	{
		return (world.GetEntityById(tree.GetLong("entityIdMount", 0L))?.GetBehavior<EntityBehaviorSeatable>())?.Seats.FirstOrDefault((IMountableSeat seat) => seat.SeatId == tree.GetString("seatId"));
	}

	public override void MountableToTreeAttributes(TreeAttribute tree)
	{
		base.MountableToTreeAttributes(tree);
		tree.SetLong("entityIdMount", Entity.EntityId);
		tree.SetString("className", RideableClassName);
	}

	public override void DidMount(EntityAgent entityAgent)
	{
		base.DidMount(entityAgent);
		if (Entity != null)
		{
			Entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager.StopTasks();
			Entity.StartAnimation("idle");
			if (entityAgent.Api is ICoreClientAPI coreClientAPI && coreClientAPI.World.Player.Entity.EntityId == entityAgent.EntityId)
			{
				coreClientAPI.Input.MouseYaw = Entity.Pos.Yaw;
			}
		}
		(mountedEntity as IMountableListener)?.DidMount(entityAgent);
		(Entity as IMountableListener)?.DidMount(entityAgent);
		Entity.Api.Event.TriggerEntityMounted(entityAgent, this);
	}

	public override void DidUnmount(EntityAgent entityAgent)
	{
		if (entityAgent.World.Side == EnumAppSide.Server && base.DoTeleportOnUnmount)
		{
			tryTeleportToFreeLocation();
		}
		if (entityAgent is EntityPlayer entityPlayer)
		{
			entityPlayer.BodyYawLimits = null;
			entityPlayer.HeadYawLimits = null;
		}
		base.DidUnmount(entityAgent);
		(mountedEntity as IMountableListener)?.DidUnmount(entityAgent);
		(Entity as IMountableListener)?.DidUnmount(entityAgent);
		Entity.Api.Event.TriggerEntityUnmounted(entityAgent, this);
	}

	protected virtual void tryTeleportToFreeLocation()
	{
		IWorldAccessor world = base.Passenger.World;
		IBlockAccessor blockAccessor = base.Passenger.World.BlockAccessor;
		Vec3d vec3d = Entity.Pos.XYZ.Add(EntityPos.GetViewVector(0f, Entity.Pos.Yaw + (float)Math.PI / 2f)).Add(0.0, 0.01, 0.0);
		Vec3d vec3d2 = Entity.Pos.XYZ.Add(EntityPos.GetViewVector(0f, Entity.Pos.Yaw - (float)Math.PI / 2f)).Add(0.0, 0.01, 0.0);
		if (GameMath.AngleRadDistance(base.Passenger.Pos.Yaw, Entity.Pos.Yaw + (float)Math.PI / 2f) < (float)Math.PI / 2f)
		{
			Vec3d vec3d3 = vec3d2;
			vec3d2 = vec3d;
			vec3d = vec3d3;
		}
		if (blockAccessor.GetMostSolidBlock((int)vec3d.X, (int)(vec3d.Y - 0.1), (int)vec3d.Z).SideSolid[BlockFacing.UP.Index] && !world.CollisionTester.IsColliding(blockAccessor, base.Passenger.CollisionBox, vec3d, alsoCheckTouch: false))
		{
			base.Passenger.TeleportTo(vec3d);
		}
		else if (blockAccessor.GetMostSolidBlock((int)vec3d2.X, (int)(vec3d2.Y - 0.1), (int)vec3d2.Z).SideSolid[BlockFacing.UP.Index] && !world.CollisionTester.IsColliding(blockAccessor, base.Passenger.CollisionBox, vec3d2, alsoCheckTouch: false))
		{
			base.Passenger.TeleportTo(vec3d2);
		}
	}
}
