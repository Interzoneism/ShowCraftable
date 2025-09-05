using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityElevator : Entity, ISeatInstSupplier, IMountableListener, ICustomInteractionHelpPositioning
{
	private double swimmingOffsetY;

	public float SpeedMultiplier = 1.5f;

	public Dictionary<string, string> MountAnimations = new Dictionary<string, string>();

	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	public ILoadedSound travelSound;

	public ILoadedSound latchSound;

	private float accum;

	private bool isMovingUp;

	private bool isMovingDown;

	private int colliderBlockId;

	private int lastStopIndex;

	public ElevatorSystem ElevatorSys;

	private int CurrentStopIndex;

	private const string UpAp = "UpAP";

	private const string DownAp = "DownAP";

	public override double FrustumSphereRadius => base.FrustumSphereRadius * 2.0;

	public override bool IsCreature => true;

	public override bool ApplyGravity => false;

	public override bool IsInteractable => true;

	public override float MaterialDensity => 100f;

	public override double SwimmingOffsetY => swimmingOffsetY;

	public bool TransparentCenter => true;

	public string NetworkCode
	{
		get
		{
			return Attributes.GetString("networkCode");
		}
		set
		{
			Attributes.SetString("networkCode", value);
		}
	}

	public bool IsMoving
	{
		get
		{
			if (!isMovingUp)
			{
				return isMovingDown;
			}
			return true;
		}
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		swimmingOffsetY = properties.Attributes["swimmingOffsetY"].AsDouble();
		MountAnimations = properties.Attributes["mountAnimations"].AsObject<Dictionary<string, string>>();
		base.Initialize(properties, api, InChunkIndex3d);
		if (api is ICoreClientAPI coreClientAPI)
		{
			capi = coreClientAPI;
			travelSound = capi.World.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/gearbox_turn.ogg"),
				ShouldLoop = true,
				RelativePosition = false,
				DisposeOnFinish = false,
				Volume = 0.15f
			});
			latchSound = capi.World.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/latch.ogg"),
				ShouldLoop = false,
				RelativePosition = false,
				DisposeOnFinish = false
			});
		}
		if (!(api is ICoreServerAPI coreServerAPI))
		{
			return;
		}
		sapi = coreServerAPI;
		ModsystemElevator modSystem = sapi.ModLoader.GetModSystem<ModsystemElevator>();
		if (NetworkCode != null)
		{
			ElevatorSys = modSystem.RegisterElevator(NetworkCode, this);
			if (Attributes.GetBool("isActivated") && !Attributes.HasAttribute("maxHeight"))
			{
				List<int> controlPositions = ElevatorSys.ControlPositions;
				if (controlPositions != null && controlPositions.Count > 0)
				{
					Attributes.SetInt("maxHeight", ElevatorSys.ControlPositions.Last());
				}
			}
		}
		colliderBlockId = sapi.World.BlockAccessor.GetBlock("meta-collider").Id;
	}

	public override void OnGameTick(float dt)
	{
		if (World.Side == EnumAppSide.Server)
		{
			updatePosition(dt);
		}
		if (World.Side == EnumAppSide.Client)
		{
			bool flag = WatchedAttributes.GetBool("isMovingUp");
			bool flag2 = WatchedAttributes.GetBool("isMovingDown");
			if (!flag && !flag2 && IsMoving)
			{
				StopAnimation("gearturndown");
				StopAnimation("gearturnup");
			}
			isMovingUp = flag;
			isMovingDown = flag2;
			NowInMotion(dt);
		}
		base.OnGameTick(dt);
	}

	protected virtual void updatePosition(float dt)
	{
		dt = Math.Min(0.5f, dt);
		if (ElevatorSys == null || ElevatorSys.ControlPositions.Count == 0)
		{
			return;
		}
		int num = ElevatorSys.ControlPositions[CurrentStopIndex];
		double num2 = Math.Abs(ServerPos.Y - (double)num);
		if (num2 >= 0.019999999552965164)
		{
			if (!IsMoving)
			{
				UnSetGround(ServerPos.AsBlockPos, ElevatorSys.ControlPositions[lastStopIndex]);
			}
			double num3 = Math.Max(0.5, Math.Clamp(num2, 0.0, 1.0));
			if (ServerPos.Y < (double)num)
			{
				ServerPos.Y += (double)(dt * SpeedMultiplier) * num3;
				isMovingUp = true;
			}
			else
			{
				ServerPos.Y -= (double)(dt * SpeedMultiplier) * num3;
				isMovingDown = true;
			}
		}
		else
		{
			if (IsMoving)
			{
				lastStopIndex = CurrentStopIndex;
				SetGround(ServerPos.AsBlockPos, num);
			}
			isMovingUp = (isMovingDown = false);
		}
		WatchedAttributes.SetBool("isMovingUp", isMovingUp);
		WatchedAttributes.SetBool("isMovingDown", isMovingDown);
	}

	private void SetGround(BlockPos pos, int elevatorStopHeight)
	{
		BlockPos blockPos = pos.Copy();
		blockPos.Y = elevatorStopHeight;
		for (int i = -1; i < 2; i++)
		{
			for (int j = -1; j < 2; j++)
			{
				blockPos.Set(pos.X + i, elevatorStopHeight, pos.Z + j);
				MakeGroundSolid(blockPos);
			}
		}
	}

	private void MakeGroundSolid(BlockPos tmpPos)
	{
		Block block = sapi.World.BlockAccessor.GetBlock(tmpPos);
		if (block.Id == 0)
		{
			sapi.World.BlockAccessor.SetBlock(colliderBlockId, tmpPos);
		}
		else if (block is BlockToggleCollisionBox)
		{
			BlockEntityGeneric blockEntity = sapi.World.BlockAccessor.GetBlockEntity<BlockEntityGeneric>(tmpPos);
			blockEntity.GetBehavior<BEBehaviorToggleCollisionBox>().Solid = true;
			blockEntity.MarkDirty();
		}
	}

	private void MakeGroundAir(BlockPos tmpPos)
	{
		Block block = sapi.World.BlockAccessor.GetBlock(tmpPos);
		if (block.Id == colliderBlockId)
		{
			sapi.World.BlockAccessor.SetBlock(0, tmpPos);
		}
		else if (block is BlockToggleCollisionBox)
		{
			BlockEntityGeneric blockEntity = sapi.World.BlockAccessor.GetBlockEntity<BlockEntityGeneric>(tmpPos);
			blockEntity.GetBehavior<BEBehaviorToggleCollisionBox>().Solid = false;
			blockEntity.MarkDirty();
		}
	}

	private void UnSetGround(BlockPos pos, int elevatorStopHeight)
	{
		BlockPos blockPos = pos.Copy();
		blockPos.Y = elevatorStopHeight;
		for (int i = -1; i < 2; i++)
		{
			for (int j = -1; j < 2; j++)
			{
				blockPos.Set(pos.X + i, elevatorStopHeight, pos.Z + j);
				MakeGroundAir(blockPos);
			}
		}
	}

	public void NowInMotion(float dt)
	{
		accum += dt;
		if ((double)accum < 0.2)
		{
			return;
		}
		accum = 0f;
		if (isMovingDown || isMovingUp)
		{
			if (isMovingUp && !AnimManager.IsAnimationActive("gearturnup"))
			{
				StopAnimation("gearturndown");
				StartAnimation("gearturnup");
			}
			if (isMovingDown && !AnimManager.IsAnimationActive("gearturndown"))
			{
				StopAnimation("gearturnup");
				StartAnimation("gearturndown");
			}
			if (!travelSound.IsPlaying)
			{
				travelSound.Start();
				travelSound.FadeTo(0.15000000596046448, 0.5f, null);
			}
			travelSound.SetPosition((float)base.SidedPos.X, (float)base.SidedPos.InternalY, (float)base.SidedPos.Z);
		}
		else if (travelSound.IsPlaying)
		{
			travelSound.FadeTo(0.0, 0.5f, delegate
			{
				travelSound.Stop();
			});
		}
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		foreach (EntityBehavior behavior3 in base.SidedProperties.Behaviors)
		{
			behavior3.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		int num = (byEntity as EntityPlayer)?.EntitySelection?.SelectionBoxIndex ?? (-1);
		EntityBehaviorSelectionBoxes behavior = GetBehavior<EntityBehaviorSelectionBoxes>();
		EntityBehaviorSeatable behavior2 = GetBehavior<EntityBehaviorSeatable>();
		if (behavior == null || num <= 0 || behavior2 == null || !behavior2.Seats.Any((IMountableSeat s) => s.Passenger?.EntityId == byEntity.EntityId))
		{
			return;
		}
		string code = behavior.selectionBoxes[num - 1].AttachPoint.Code;
		if (string.Equals(code, "UpAP"))
		{
			if (Api is ICoreServerAPI)
			{
				if (ElevatorSys != null)
				{
					int item = Attributes.GetInt("maxHeight");
					int num2 = ElevatorSys.ControlPositions.IndexOf(item);
					int num3 = Math.Min(CurrentStopIndex + 1, ElevatorSys.ControlPositions.Count - 1);
					if (num2 >= num3)
					{
						CurrentStopIndex = num3;
					}
				}
			}
			else
			{
				StartAnimation("leverUP");
				latchSound.SetPosition((float)base.SidedPos.X, (float)base.SidedPos.InternalY, (float)base.SidedPos.Z);
				latchSound.Start();
			}
		}
		else
		{
			if (!string.Equals(code, "DownAP"))
			{
				return;
			}
			if (Api is ICoreServerAPI)
			{
				if (ElevatorSys != null)
				{
					CurrentStopIndex = Math.Max(CurrentStopIndex - 1, 0);
				}
			}
			else
			{
				StartAnimation("leverDOWN");
				latchSound.SetPosition((float)base.SidedPos.X, (float)base.SidedPos.InternalY, (float)base.SidedPos.Z);
				latchSound.Start();
			}
		}
	}

	public void CallElevator(BlockPos position, int offset)
	{
		int num = position.Y + offset;
		if (Attributes.GetInt("maxHeight") < num)
		{
			Attributes.SetInt("maxHeight", num);
		}
		int num2 = ElevatorSys.ControlPositions.IndexOf(num);
		if (num2 != -1)
		{
			CurrentStopIndex = num2;
		}
	}

	public override bool CanCollect(Entity byEntity)
	{
		return false;
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		travelSound?.Dispose();
		base.OnEntityDespawn(despawn);
	}

	public IMountableSeat CreateSeat(IMountable mountable, string seatId, SeatConfig config)
	{
		return new EntityElevatorSeat(mountable, seatId, config);
	}

	public void DidUnmount(EntityAgent entityAgent)
	{
		MarkShapeModified();
	}

	public void DidMount(EntityAgent entityAgent)
	{
		MarkShapeModified();
	}

	public override void FromBytes(BinaryReader reader, bool isSync)
	{
		base.FromBytes(reader, isSync);
		CurrentStopIndex = Attributes.GetInt("currentStopIndex");
		lastStopIndex = CurrentStopIndex;
	}

	public override void ToBytes(BinaryWriter writer, bool forClient)
	{
		Attributes.SetInt("currentStopIndex", CurrentStopIndex);
		base.ToBytes(writer, forClient);
	}

	public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player)
	{
		int num = player.Entity.EntitySelection?.SelectionBoxIndex ?? (-1);
		EntityBehaviorSelectionBoxes behavior = GetBehavior<EntityBehaviorSelectionBoxes>();
		if (behavior != null && num > 0)
		{
			string code = behavior.selectionBoxes[num - 1].AttachPoint.Code;
			if (string.Equals(code, "UpAP"))
			{
				return new WorldInteraction[1]
				{
					new WorldInteraction
					{
						ActionLangCode = "elevator-leverup",
						MouseButton = EnumMouseButton.Right
					}
				};
			}
			if (string.Equals(code, "DownAP"))
			{
				return new WorldInteraction[1]
				{
					new WorldInteraction
					{
						ActionLangCode = "elevator-leverdown",
						MouseButton = EnumMouseButton.Right
					}
				};
			}
		}
		return base.GetInteractionHelp(world, es, player);
	}

	public Vec3d GetInteractionHelpPosition()
	{
		EntitySelection currentEntitySelection = (Api as ICoreClientAPI).World.Player.CurrentEntitySelection;
		if (currentEntitySelection == null)
		{
			return null;
		}
		int num = currentEntitySelection.SelectionBoxIndex - 1;
		if (num < 0)
		{
			return null;
		}
		AttachmentPoint attachPoint = currentEntitySelection.Entity.GetBehavior<EntityBehaviorSelectionBoxes>().selectionBoxes[num].AttachPoint;
		double y = 0.5;
		if (attachPoint.Code.Equals("UpAP") || attachPoint.Code.Equals("DownAP"))
		{
			y = 0.1;
		}
		return currentEntitySelection.Entity.GetBehavior<EntityBehaviorSelectionBoxes>().GetCenterPosOfBox(num)?.Add(0.0, y, 0.0);
	}
}
