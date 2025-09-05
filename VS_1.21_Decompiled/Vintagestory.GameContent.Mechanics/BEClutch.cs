using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BEClutch : BlockEntity, IMechanicalPowerRenderable
{
	private static double DEGREES15 = Math.PI / 12.0;

	private static double DEGREES30 = 2.0 * DEGREES15;

	private static float REVERSEANGLE = (float)Math.PI * 2f;

	protected MechanicalPowerMod manager;

	private BlockPos transmissionPos;

	public Vec4f lightRbs = new Vec4f();

	public Vec3f hinge = new Vec3f(0.375f, 0f, 0.5f);

	private double armAngle;

	private float drumAngle;

	private float drumSpeed;

	private float drumAngleOffset = -1E-06f;

	private float transmissionAngleLast = -1E-06f;

	private float catchUpAngle;

	private CompositeShape shape;

	public bool Engaged { get; protected set; }

	public BlockFacing Facing { get; protected set; }

	public virtual BlockPos Position => Pos;

	public virtual Vec4f LightRgba => lightRbs;

	public virtual int[] AxisSign { get; protected set; }

	public virtual float AngleRad => (float)armAngle;

	public virtual CompositeShape Shape
	{
		get
		{
			return shape;
		}
		set
		{
			if (Shape != null && manager != null)
			{
				manager.RemoveDeviceForRender(this);
				shape = value;
				manager.AddDeviceForRender(this);
			}
			else
			{
				shape = value;
			}
		}
	}

	private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>().animUtil;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		Shape = base.Block.Shape;
		Facing = BlockFacing.FromCode(base.Block.Variant["side"]);
		transmissionPos = Pos.AddCopy(Facing);
		manager = Api.ModLoader.GetModSystem<MechanicalPowerMod>();
		manager.AddDeviceForRender(this);
		AxisSign = new int[3];
		switch (Facing.Index)
		{
		case 0:
			AxisSign[0] = -1;
			hinge = new Vec3f(0.5f, 0f, 0.375f);
			break;
		case 2:
			AxisSign[0] = 1;
			hinge = new Vec3f(0.5f, 0f, 0.625f);
			break;
		case 1:
			AxisSign[2] = -1;
			hinge = new Vec3f(0.625f, 0f, 0.5f);
			break;
		default:
			AxisSign[2] = 1;
			break;
		}
		if (api.World.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(OnClientGameTick, 16);
		}
	}

	public float RotationNeighbour()
	{
		if (armAngle > DEGREES15)
		{
			BEBehaviorMPTransmission bEBehaviorMPTransmission = Api.World.BlockAccessor.GetBlockEntity(transmissionPos)?.GetBehavior<BEBehaviorMPTransmission>();
			if (bEBehaviorMPTransmission != null)
			{
				float num = ((Facing == BlockFacing.EAST || Facing == BlockFacing.NORTH) ? (REVERSEANGLE - bEBehaviorMPTransmission.AngleRad) : bEBehaviorMPTransmission.AngleRad) % ((float)Math.PI * 2f) + drumAngleOffset;
				if (armAngle < DEGREES30)
				{
					float num2 = catchUpAngle * (float)(2.0 - armAngle / DEGREES15);
					drumSpeed = num - num2 - drumAngle;
					drumAngle = num - num2;
				}
				else
				{
					drumSpeed = num - drumAngle;
					drumAngle = num;
					catchUpAngle = 0f;
				}
			}
		}
		else if (Engaged)
		{
			BEBehaviorMPTransmission bEBehaviorMPTransmission2 = Api.World.BlockAccessor.GetBlockEntity(transmissionPos)?.GetBehavior<BEBehaviorMPTransmission>();
			if (bEBehaviorMPTransmission2 != null)
			{
				float num3 = ((Facing == BlockFacing.EAST || Facing == BlockFacing.NORTH) ? (REVERSEANGLE - bEBehaviorMPTransmission2.AngleRad) : bEBehaviorMPTransmission2.AngleRad) % ((float)Math.PI * 2f);
				float num4 = ((transmissionAngleLast == -1E-06f) ? 0f : (num3 - transmissionAngleLast));
				transmissionAngleLast = num3;
				if (drumAngleOffset < 0f)
				{
					int num5 = (int)((drumAngle - num3) % ((float)Math.PI * 2f) / ((float)Math.PI * 2f) * 8f);
					if (num5 < 0)
					{
						num5 += 8;
					}
					drumAngleOffset = (float)num5 * ((float)Math.PI * 2f) / 8f;
					drumSpeed = 0f;
				}
				float num6 = num4 - drumSpeed;
				if (Math.Abs(num6) > 0.00045f)
				{
					num6 = ((num6 < 0f) ? (-0.00045f) : 0.00045f);
				}
				drumSpeed += num6;
				drumAngle += drumSpeed;
				catchUpAngle = (num3 + drumAngleOffset - drumAngle) % ((float)Math.PI * 2f);
				if (catchUpAngle > (float)Math.PI)
				{
					catchUpAngle -= (float)Math.PI * 2f;
				}
				if (catchUpAngle < -(float)Math.PI)
				{
					catchUpAngle += (float)Math.PI * 2f;
				}
			}
		}
		else
		{
			drumAngle += drumSpeed;
			if (drumAngle > (float)Math.PI * 2f)
			{
				drumAngle %= (float)Math.PI * 2f;
			}
			drumSpeed *= 0.99f;
			if (drumSpeed > 0.0001f)
			{
				drumSpeed -= 0.0001f;
			}
			else if (drumSpeed > 0f)
			{
				drumSpeed = 0f;
			}
			else if (drumSpeed < -0.0001f)
			{
				drumSpeed += 0.0001f;
			}
			else if (drumSpeed < 0f)
			{
				drumSpeed = 0f;
			}
			drumAngleOffset = -1E-06f;
			transmissionAngleLast = -1E-06f;
		}
		return drumAngle;
	}

	private void OnClientGameTick(float dt)
	{
		if (Engaged)
		{
			if (armAngle < DEGREES30)
			{
				armAngle += DEGREES15 * (double)dt;
				if (armAngle > DEGREES30)
				{
					armAngle = DEGREES30;
				}
			}
		}
		else if (armAngle > 0.0)
		{
			armAngle -= DEGREES15 * (double)dt;
			if (armAngle < 0.0)
			{
				armAngle = 0.0;
			}
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		lightRbs = Api.World.BlockAccessor.GetLightRGBs(Pos);
		ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
		Shape shape = Vintagestory.API.Common.Shape.TryGet(coreClientAPI, "shapes/block/wood/mechanics/clutch-rest.json");
		float y = 0f;
		switch (Facing.Index)
		{
		case 0:
			y = 180f;
			break;
		case 1:
			y = 90f;
			break;
		case 3:
			y = 270f;
			break;
		}
		coreClientAPI.Tesselator.TesselateShape(base.Block, shape, out var modeldata, new Vec3f(0f, y, 0f));
		mesher.AddMeshData(modeldata);
		return true;
	}

	public bool OnInteract(IPlayer byPlayer)
	{
		BEBehaviorMPTransmission bEBehaviorMPTransmission = Api.World.BlockAccessor.GetBlockEntity(transmissionPos)?.GetBehavior<BEBehaviorMPTransmission>();
		if (!Engaged && bEBehaviorMPTransmission != null && bEBehaviorMPTransmission.engaged)
		{
			return true;
		}
		Engaged = !Engaged;
		Api.World.PlaySoundAt(new AssetLocation("sounds/effect/woodswitch.ogg"), Pos, 0.0, byPlayer);
		bEBehaviorMPTransmission?.CheckEngaged(Api.World.BlockAccessor, updateNetwork: true);
		MarkDirty(redrawOnClient: true);
		return true;
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		manager.RemoveDeviceForRender(this);
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		manager?.RemoveDeviceForRender(this);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		Engaged = tree.GetBool("engaged");
		if (Engaged && armAngle == 0.0)
		{
			armAngle = DEGREES30;
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("engaged", Engaged);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
		sb.AppendLine(string.Format(Lang.Get(Engaged ? "Engaged" : "Disengaged")));
	}

	public void onNeighbourChange(BlockPos neibpos)
	{
		if (Engaged && neibpos.Equals(transmissionPos))
		{
			Engaged = false;
		}
	}

	Block IMechanicalPowerRenderable.get_Block()
	{
		return base.Block;
	}
}
