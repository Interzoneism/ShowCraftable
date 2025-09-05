using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BEBehaviorMPTransmission : BEBehaviorMPBase
{
	public bool engaged;

	protected float[] rotPrev = new float[2];

	private BlockFacing[] orients = new BlockFacing[2];

	private string orientations;

	public override CompositeShape Shape
	{
		get
		{
			string text = base.Block.Variant["orientation"];
			CompositeShape compositeShape = new CompositeShape();
			compositeShape.Base = new AssetLocation("shapes/block/wood/mechanics/transmission-leftgear.json");
			compositeShape.Overlays = new CompositeShape[1]
			{
				new CompositeShape
				{
					Base = new AssetLocation("shapes/block/wood/mechanics/transmission-rightgear.json")
				}
			};
			CompositeShape compositeShape2 = compositeShape;
			if (text == "ns")
			{
				compositeShape2.rotateY = 90f;
				compositeShape2.Overlays[0].rotateY = 90f;
			}
			return compositeShape2;
		}
		set
		{
		}
	}

	public BEBehaviorMPTransmission(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		orientations = base.Block.Variant["orientation"];
		string text = orientations;
		if (!(text == "ns"))
		{
			if (text == "we")
			{
				AxisSign = new int[3] { 1, 0, 0 };
				orients[0] = BlockFacing.EAST;
				orients[1] = BlockFacing.WEST;
			}
		}
		else
		{
			AxisSign = new int[3] { 0, 0, -1 };
			orients[0] = BlockFacing.NORTH;
			orients[1] = BlockFacing.SOUTH;
		}
		if (engaged)
		{
			ChangeState(newEngaged: true);
		}
	}

	public void CheckEngaged(IBlockAccessor access, bool updateNetwork)
	{
		BlockFacing blockFacing = ((orients[0] == BlockFacing.NORTH) ? BlockFacing.EAST : BlockFacing.NORTH);
		bool flag = false;
		BEClutch bEClutch = access.GetBlockEntity(Position.AddCopy(blockFacing)) as BEClutch;
		if (bEClutch?.Facing == blockFacing.Opposite)
		{
			flag = bEClutch.Engaged;
		}
		if (!flag)
		{
			bEClutch = access.GetBlockEntity(Position.AddCopy(blockFacing.Opposite)) as BEClutch;
			if (bEClutch?.Facing == blockFacing)
			{
				flag = bEClutch.Engaged;
			}
		}
		if (flag != engaged)
		{
			engaged = flag;
			if (updateNetwork)
			{
				ChangeState(flag);
			}
		}
	}

	protected override MechPowerPath[] GetMechPowerExits(MechPowerPath fromExitTurnDir)
	{
		if (!engaged)
		{
			return Array.Empty<MechPowerPath>();
		}
		return base.GetMechPowerExits(fromExitTurnDir);
	}

	public override float GetResistance()
	{
		return 0.0005f;
	}

	private void ChangeState(bool newEngaged)
	{
		if (newEngaged)
		{
			CreateJoinAndDiscoverNetwork(orients[0]);
			CreateJoinAndDiscoverNetwork(orients[1]);
			tryConnect(orients[0]);
			Blockentity.MarkDirty(redrawOnClient: true);
		}
		else if (network != null)
		{
			manager.OnNodeRemoved(this);
		}
	}

	internal float RotationNeighbour(int side, bool allowIndirect)
	{
		BlockPos blockPos = Position.AddCopy(orients[side]);
		IMechanicalPowerBlock mechanicalPowerBlock = Api.World.BlockAccessor.GetBlock(blockPos) as IMechanicalPowerBlock;
		if (mechanicalPowerBlock == null || !mechanicalPowerBlock.HasMechPowerConnectorAt(Api.World, blockPos, orients[side].Opposite))
		{
			mechanicalPowerBlock = null;
		}
		IMechanicalPowerDevice mechanicalPowerDevice = ((mechanicalPowerBlock == null) ? null : Api.World.BlockAccessor.GetBlockEntity(blockPos))?.GetBehavior<BEBehaviorMPBase>();
		if (mechanicalPowerDevice is BEBehaviorMPTransmission { engaged: false })
		{
			mechanicalPowerDevice = null;
		}
		float num;
		if (mechanicalPowerDevice == null || mechanicalPowerDevice.Network == null)
		{
			if (engaged && allowIndirect)
			{
				num = RotationNeighbour(1 - side, allowIndirect: false);
				rotPrev[side] = num;
			}
			else
			{
				num = rotPrev[side];
			}
		}
		else
		{
			num = mechanicalPowerDevice.Network.AngleRad * mechanicalPowerDevice.GearedRatio;
			bool flag = mechanicalPowerDevice.GetPropagationDirection() != orients[side];
			if (side == 1)
			{
				flag = !flag;
			}
			if (flag)
			{
				num = (float)Math.PI * 2f - num;
			}
			rotPrev[side] = num;
		}
		return num;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		engaged = tree.GetBool("engaged");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("engaged", engaged);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		base.OnTesselation(mesher, tesselator);
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
		if (Api.World.EntityDebugMode)
		{
			sb.AppendLine(string.Format(Lang.Get(engaged ? "Engaged" : "Disengaged")));
		}
	}
}
