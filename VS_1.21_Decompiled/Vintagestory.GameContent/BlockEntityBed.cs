using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityBed : BlockEntity, IMountableSeat, IMountable
{
	private long restingListener;

	private static Vec3f eyePos = new Vec3f(0f, 0.3f, 0f);

	private float sleepEfficiency = 0.5f;

	private BlockFacing facing;

	private float y2 = 0.5f;

	private double hoursTotal;

	public EntityAgent MountedBy;

	private bool blockBroken;

	private long mountedByEntityId;

	private string mountedByPlayerUid;

	private EntityControls controls = new EntityControls();

	private EntityPos mountPos = new EntityPos();

	private AnimationMetaData meta = new AnimationMetaData
	{
		Code = "sleep",
		Animation = "lie"
	}.Init();

	public bool DoTeleportOnUnmount { get; set; } = true;

	public EntityPos SeatPosition => Position;

	public double StepPitch => 0.0;

	public EntityPos Position
	{
		get
		{
			BlockFacing opposite = facing.Opposite;
			mountPos.SetPos(Pos);
			mountPos.Yaw = (float)facing.HorizontalAngleIndex * ((float)Math.PI / 2f) + (float)Math.PI / 2f;
			if (opposite == BlockFacing.NORTH)
			{
				return mountPos.Add(0.5, y2, 1.0);
			}
			if (opposite == BlockFacing.EAST)
			{
				return mountPos.Add(0.0, y2, 0.5);
			}
			if (opposite == BlockFacing.SOUTH)
			{
				return mountPos.Add(0.5, y2, 0.0);
			}
			if (opposite == BlockFacing.WEST)
			{
				return mountPos.Add(1.0, y2, 0.5);
			}
			return null;
		}
	}

	public AnimationMetaData SuggestedAnimation => meta;

	public EntityControls Controls => controls;

	public IMountable MountSupplier => this;

	public EnumMountAngleMode AngleMode => EnumMountAngleMode.FixateYaw;

	public Vec3f LocalEyePos => eyePos;

	Entity IMountableSeat.Passenger => MountedBy;

	public bool CanControl => false;

	public Entity Entity => null;

	public Matrixf RenderTransform => null;

	public IMountableSeat[] Seats => new IMountableSeat[1] { this };

	public bool SkipIdleAnimation => false;

	public float FpHandPitchFollow => 1f;

	public string SeatId
	{
		get
		{
			return "bed-0";
		}
		set
		{
		}
	}

	public SeatConfig Config
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	public long PassengerEntityIdForInit
	{
		get
		{
			return mountedByEntityId;
		}
		set
		{
			mountedByEntityId = value;
		}
	}

	public Entity Controller => MountedBy;

	public Entity OnEntity => null;

	public EntityControls ControllingControls => null;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		controls.OnAction = onControls;
		if (base.Block.Attributes != null)
		{
			sleepEfficiency = base.Block.Attributes["sleepEfficiency"].AsFloat(0.5f);
		}
		Cuboidf[] collisionBoxes = base.Block.GetCollisionBoxes(api.World.BlockAccessor, Pos);
		if (collisionBoxes != null && collisionBoxes.Length != 0)
		{
			y2 = collisionBoxes[0].Y2;
		}
		facing = BlockFacing.FromCode(base.Block.LastCodePart());
		if (MountedBy == null && (mountedByEntityId != 0L || mountedByPlayerUid != null))
		{
			EntityAgent entityAgent = ((mountedByPlayerUid == null) ? (api.World.GetEntityById(mountedByEntityId) as EntityAgent) : api.World.PlayerByUid(mountedByPlayerUid)?.Entity);
			if (entityAgent?.SidedProperties != null)
			{
				entityAgent.TryMount(this);
			}
		}
	}

	private void onControls(EnumEntityAction action, bool on, ref EnumHandling handled)
	{
		if (action == EnumEntityAction.Sneak && on)
		{
			MountedBy?.TryUnmount();
			controls.StopAllMovement();
			handled = EnumHandling.PassThrough;
		}
	}

	private void RestPlayer(float dt)
	{
		double num = Api.World.Calendar.TotalHours - hoursTotal;
		float num2 = sleepEfficiency - 1f / 12f;
		if (!(num > 0.0))
		{
			return;
		}
		if (Api.World.Config.GetString("temporalStormSleeping", "0").ToInt() == 0 && Api.ModLoader.GetModSystem<SystemTemporalStability>().StormStrength > 0f)
		{
			MountedBy.TryUnmount();
			return;
		}
		if (MountedBy?.GetBehavior("tiredness") is EntityBehaviorTiredness entityBehaviorTiredness)
		{
			float num3 = (entityBehaviorTiredness.Tiredness = Math.Max(0f, entityBehaviorTiredness.Tiredness - (float)num / num2));
			if (num3 <= 0f)
			{
				MountedBy.TryUnmount();
			}
		}
		hoursTotal = Api.World.Calendar.TotalHours;
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		base.OnBlockBroken(byPlayer);
		blockBroken = true;
		MountedBy?.TryUnmount();
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		mountedByEntityId = tree.GetLong("mountedByEntityId", 0L);
		mountedByPlayerUid = tree.GetString("mountedByPlayerUid");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetLong("mountedByEntityId", mountedByEntityId);
		tree.SetString("mountedByPlayerUid", mountedByPlayerUid);
	}

	public void MountableToTreeAttributes(TreeAttribute tree)
	{
		tree.SetString("className", "bed");
		tree.SetInt("posx", Pos.X);
		tree.SetInt("posy", Pos.InternalY);
		tree.SetInt("posz", Pos.Z);
	}

	public void DidUnmount(EntityAgent entityAgent)
	{
		if (MountedBy?.GetBehavior("tiredness") is EntityBehaviorTiredness entityBehaviorTiredness)
		{
			entityBehaviorTiredness.IsSleeping = false;
		}
		MountedBy = null;
		if (!blockBroken)
		{
			_ = BlockFacing.FromCode(base.Block.LastCodePart()).Opposite;
			BlockPos blockPos = Pos.AddCopy(facing);
			BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
			foreach (BlockFacing blockFacing in hORIZONTALS)
			{
				Vec3d vec3d = Pos.ToVec3d().AddCopy(blockFacing).Add(0.5, 0.001, 0.5);
				Vec3d vec3d2 = blockPos.ToVec3d().AddCopy(blockFacing).Add(0.5, 0.001, 0.5);
				if (!Api.World.CollisionTester.IsColliding(Api.World.BlockAccessor, entityAgent.SelectionBox, vec3d, alsoCheckTouch: false))
				{
					entityAgent.TeleportTo(vec3d);
					break;
				}
				if (!Api.World.CollisionTester.IsColliding(Api.World.BlockAccessor, entityAgent.SelectionBox, vec3d2, alsoCheckTouch: false))
				{
					entityAgent.TeleportTo(vec3d2);
					break;
				}
			}
		}
		mountedByEntityId = 0L;
		mountedByPlayerUid = null;
		UnregisterGameTickListener(restingListener);
		restingListener = 0L;
	}

	public void DidMount(EntityAgent entityAgent)
	{
		if (MountedBy != null && MountedBy != entityAgent)
		{
			entityAgent.TryUnmount();
		}
		else
		{
			if (MountedBy == entityAgent)
			{
				return;
			}
			MountedBy = entityAgent;
			mountedByPlayerUid = (entityAgent as EntityPlayer)?.PlayerUID;
			mountedByEntityId = MountedBy.EntityId;
			ICoreAPI api = entityAgent.Api;
			if (api != null && api.Side == EnumAppSide.Server)
			{
				if (restingListener == 0L)
				{
					ICoreAPI api2 = Api;
					Api = entityAgent.Api;
					restingListener = RegisterGameTickListener(RestPlayer, 200);
					Api = api2;
				}
				hoursTotal = entityAgent.Api.World.Calendar.TotalHours;
			}
			if (MountedBy != null)
			{
				entityAgent.Api.Event.EnqueueMainThreadTask(delegate
				{
					if (MountedBy != null && MountedBy.GetBehavior("tiredness") is EntityBehaviorTiredness entityBehaviorTiredness)
					{
						entityBehaviorTiredness.IsSleeping = true;
					}
				}, "issleeping");
			}
			MarkDirty();
		}
	}

	public bool IsMountedBy(Entity entity)
	{
		return MountedBy == entity;
	}

	public bool IsBeingControlled()
	{
		return false;
	}

	public bool CanUnmount(EntityAgent entityAgent)
	{
		return true;
	}

	public bool CanMount(EntityAgent entityAgent)
	{
		return !AnyMounted();
	}

	public bool AnyMounted()
	{
		return MountedBy != null;
	}
}
