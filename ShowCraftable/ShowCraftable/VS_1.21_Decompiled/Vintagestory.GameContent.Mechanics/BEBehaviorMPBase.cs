using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent.Mechanics;

public abstract class BEBehaviorMPBase : BlockEntityBehavior, IMechanicalPowerDevice, IMechanicalPowerRenderable, IMechanicalPowerNode
{
	private static readonly bool DEBUG;

	protected MechanicalPowerMod manager;

	protected MechanicalNetwork network;

	public Vec4f lightRbs = new Vec4f();

	private CompositeShape shape;

	protected BlockFacing propagationDir = BlockFacing.NORTH;

	private float gearedRatio = 1f;

	protected float lastKnownAngleRad;

	public bool disconnected;

	public virtual BlockPos Position => Blockentity.Pos;

	public virtual Vec4f LightRgba => lightRbs;

	public virtual CompositeShape Shape
	{
		get
		{
			return shape;
		}
		set
		{
			CompositeShape compositeShape = Shape;
			if (compositeShape != null && manager != null && compositeShape != value)
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

	public virtual int[] AxisSign { get; protected set; }

	public long NetworkId { get; set; }

	public MechanicalNetwork Network => network;

	public virtual BlockFacing OutFacingForNetworkDiscovery { get; protected set; }

	public float GearedRatio
	{
		get
		{
			return gearedRatio;
		}
		set
		{
			gearedRatio = value;
		}
	}

	public virtual float AngleRad
	{
		get
		{
			if (network == null)
			{
				return lastKnownAngleRad;
			}
			if (isRotationReversed())
			{
				return lastKnownAngleRad = (float)Math.PI * 2f - network.AngleRad * gearedRatio % ((float)Math.PI * 2f);
			}
			return lastKnownAngleRad = network.AngleRad * gearedRatio % ((float)Math.PI * 2f);
		}
	}

	public BlockPos GetPosition()
	{
		return Position;
	}

	public virtual float GetGearedRatio(BlockFacing face)
	{
		return gearedRatio;
	}

	public virtual bool isRotationReversed()
	{
		if (propagationDir == null)
		{
			return false;
		}
		if (propagationDir != BlockFacing.DOWN && propagationDir != BlockFacing.EAST)
		{
			return propagationDir == BlockFacing.SOUTH;
		}
		return true;
	}

	public virtual bool isInvertedNetworkFor(BlockPos pos)
	{
		if (propagationDir == null || pos == null)
		{
			return false;
		}
		return !Position.AddCopy(propagationDir).Equals(pos);
	}

	public BEBehaviorMPBase(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		Shape = GetShape();
		manager = Api.ModLoader.GetModSystem<MechanicalPowerMod>();
		if (Api.World.Side == EnumAppSide.Client)
		{
			lightRbs = Api.World.BlockAccessor.GetLightRGBs(Blockentity.Pos);
			if (NetworkId > 0)
			{
				network = manager.GetOrCreateNetwork(NetworkId);
				JoinNetwork(network);
			}
		}
		manager.AddDeviceForRender(this);
		AxisSign = new int[3] { 0, 0, 1 };
		SetOrientations();
		if (api.Side == EnumAppSide.Server && OutFacingForNetworkDiscovery != null)
		{
			CreateJoinAndDiscoverNetwork(OutFacingForNetworkDiscovery);
		}
	}

	protected virtual CompositeShape GetShape()
	{
		return base.Block.Shape;
	}

	public virtual void SetOrientations()
	{
	}

	public virtual void WasPlaced(BlockFacing connectedOnFacing)
	{
		if ((Api.Side != EnumAppSide.Client && OutFacingForNetworkDiscovery != null) || connectedOnFacing == null)
		{
			return;
		}
		if (!tryConnect(connectedOnFacing))
		{
			if (DEBUG)
			{
				Api.Logger.Notification("Was placed fail connect 2nd: " + connectedOnFacing?.ToString() + " at " + Position);
			}
		}
		else if (DEBUG)
		{
			Api.Logger.Notification("Was placed connected 1st: " + connectedOnFacing?.ToString() + " at " + Position);
		}
	}

	public bool tryConnect(BlockFacing toFacing)
	{
		if (Api == null)
		{
			return false;
		}
		BlockPos blockPos = Position.AddCopy(toFacing);
		IMechanicalPowerBlock mechanicalPowerBlock = Api.World.BlockAccessor.GetBlock(blockPos) as IMechanicalPowerBlock;
		if (DEBUG)
		{
			Api.Logger.Notification("tryConnect at " + Position?.ToString() + " towards " + toFacing?.ToString() + " " + blockPos);
		}
		if (mechanicalPowerBlock == null || !mechanicalPowerBlock.HasMechPowerConnectorAt(Api.World, blockPos, toFacing.Opposite))
		{
			return false;
		}
		MechanicalNetwork mechanicalNetwork = mechanicalPowerBlock.GetNetwork(Api.World, blockPos);
		if (mechanicalNetwork != null)
		{
			IMechanicalPowerDevice behavior = Api.World.BlockAccessor.GetBlockEntity(blockPos).GetBehavior<BEBehaviorMPBase>();
			mechanicalPowerBlock.DidConnectAt(Api.World, blockPos, toFacing.Opposite);
			MechPowerPath mechPowerPath = new MechPowerPath(toFacing, behavior.GetGearedRatio(toFacing), blockPos, !behavior.IsPropagationDirection(Position, toFacing));
			SetPropagationDirection(mechPowerPath);
			MechPowerPath[] mechPowerExits = GetMechPowerExits(mechPowerPath);
			JoinNetwork(mechanicalNetwork);
			for (int i = 0; i < mechPowerExits.Length; i++)
			{
				if (DEBUG)
				{
					Api.Logger.Notification("== spreading path " + (mechPowerExits[i].invert ? "-" : "") + mechPowerExits[i].OutFacing?.ToString() + "  " + mechPowerExits[i].gearingRatio);
				}
				BlockPos exitPos = Position.AddCopy(mechPowerExits[i].OutFacing);
				if (!spreadTo(Api, mechanicalNetwork, exitPos, mechPowerExits[i], out var _))
				{
					LeaveNetwork();
					return true;
				}
			}
			return true;
		}
		if (network != null)
		{
			BEBehaviorMPBase bEBehaviorMPBase = Api.World.BlockAccessor.GetBlockEntity(blockPos)?.GetBehavior<BEBehaviorMPBase>();
			if (bEBehaviorMPBase != null)
			{
				return bEBehaviorMPBase.tryConnect(toFacing.Opposite);
			}
		}
		return false;
	}

	public virtual void JoinNetwork(MechanicalNetwork network)
	{
		if (this.network != null && this.network != network)
		{
			LeaveNetwork();
		}
		if (this.network == null)
		{
			this.network = network;
			network?.Join(this);
		}
		if (network == null)
		{
			NetworkId = 0L;
		}
		else
		{
			NetworkId = network.networkId;
		}
		Blockentity.MarkDirty();
	}

	public virtual void LeaveNetwork()
	{
		if (DEBUG)
		{
			Api.Logger.Notification("Leaving network " + NetworkId + " at " + Position);
		}
		network?.Leave(this);
		network = null;
		NetworkId = 0L;
		Blockentity.MarkDirty();
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		disconnected = true;
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (network != null)
		{
			manager.OnNodeRemoved(this);
		}
		LeaveNetwork();
		manager.RemoveDeviceForRender(this);
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		network?.DidUnload(this);
		manager?.RemoveDeviceForRender(this);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		lightRbs = Api.World.BlockAccessor.GetLightRGBs(Blockentity.Pos);
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		long num = tree.GetLong("networkid", 0L);
		if (worldAccessForResolve.Side == EnumAppSide.Client)
		{
			propagationDir = BlockFacing.ALLFACES[tree.GetInt("turnDirFromFacing")];
			gearedRatio = tree.GetFloat("g");
			if (NetworkId != num)
			{
				NetworkId = 0L;
				if (worldAccessForResolve.Side == EnumAppSide.Client)
				{
					NetworkId = num;
					if (NetworkId == 0L)
					{
						LeaveNetwork();
						network = null;
					}
					else if (manager != null)
					{
						network = manager.GetOrCreateNetwork(NetworkId);
						JoinNetwork(network);
						Blockentity.MarkDirty();
					}
				}
			}
		}
		SetOrientations();
		updateShape(worldAccessForResolve);
	}

	protected virtual void updateShape(IWorldAccessor worldForResolve)
	{
		Shape = base.Block?.Shape;
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetLong("networkid", NetworkId);
		tree.SetInt("turnDirFromFacing", propagationDir.Index);
		tree.SetFloat("g", gearedRatio);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		if (DEBUG || Api.World.EntityDebugMode)
		{
			sb.AppendLine($"networkid: {NetworkId}  turnDir: {propagationDir}  {network?.TurnDir.ToString()}  {gearedRatio:G3}");
			sb.AppendLine($"speed: {network?.Speed * GearedRatio:G4}  avail torque: {network?.TotalAvailableTorque / GearedRatio:G4}  torque sum: {network?.NetworkTorque / GearedRatio:G4}  resist sum: {network?.NetworkResistance / GearedRatio:G4}");
		}
	}

	public virtual BlockFacing GetPropagationDirection()
	{
		return propagationDir;
	}

	public virtual BlockFacing GetPropagationDirectionInput()
	{
		return propagationDir;
	}

	public virtual bool IsPropagationDirection(BlockPos fromPos, BlockFacing test)
	{
		return propagationDir == test;
	}

	public virtual void SetPropagationDirection(MechPowerPath path)
	{
		BlockFacing blockFacing = path.NetworkDir();
		if (propagationDir == blockFacing.Opposite && network != null)
		{
			if (!network.DirectionHasReversed)
			{
				network.TurnDir = ((network.TurnDir == EnumRotDirection.Clockwise) ? EnumRotDirection.Counterclockwise : EnumRotDirection.Clockwise);
			}
			network.DirectionHasReversed = true;
		}
		propagationDir = blockFacing;
		GearedRatio = path.gearingRatio;
		if (DEBUG)
		{
			Api.Logger.Notification("setting dir " + propagationDir?.ToString() + " " + Position);
		}
	}

	public virtual float GetTorque(long tick, float speed, out float resistance)
	{
		resistance = GetResistance();
		return 0f;
	}

	public abstract float GetResistance();

	public virtual void DestroyJoin(BlockPos pos)
	{
	}

	public virtual MechanicalNetwork CreateJoinAndDiscoverNetwork(BlockFacing powerOutFacing)
	{
		BlockPos blockPos = Position.AddCopy(powerOutFacing);
		IMechanicalPowerBlock mechanicalPowerBlock = null;
		MechanicalNetwork mechanicalNetwork = ((!(Api.World.BlockAccessor.GetBlock(blockPos) is IMechanicalPowerBlock mechanicalPowerBlock2)) ? null : mechanicalPowerBlock2.GetNetwork(Api.World, blockPos));
		if (mechanicalNetwork == null || !mechanicalNetwork.Valid)
		{
			MechanicalNetwork mechanicalNetwork2 = network;
			if (mechanicalNetwork2 == null)
			{
				mechanicalNetwork2 = manager.CreateNetwork(this);
				JoinNetwork(mechanicalNetwork2);
				if (DEBUG)
				{
					Api.Logger.Notification("===setting inturn at " + Position?.ToString() + " " + powerOutFacing);
				}
				SetPropagationDirection(new MechPowerPath(powerOutFacing, 1f));
			}
			Vec3i missingChunkPos;
			bool flag = spreadTo(Api, mechanicalNetwork2, blockPos, new MechPowerPath(GetPropagationDirection(), gearedRatio), out missingChunkPos);
			if (network == null)
			{
				if (DEBUG)
				{
					Api.Logger.Notification("Incomplete chunkloading, possible issues with mechanical network around block " + blockPos);
				}
				return null;
			}
			if (!flag)
			{
				network.AwaitChunkThenDiscover(missingChunkPos);
				manager.testFullyLoaded(network);
				return network;
			}
			IMechanicalPowerDevice mechanicalPowerDevice = Api.World.BlockAccessor.GetBlockEntity(blockPos)?.GetBehavior<BEBehaviorMPBase>();
			if (mechanicalPowerDevice != null)
			{
				BlockFacing blockFacing = (mechanicalPowerDevice.IsPropagationDirection(Position, powerOutFacing) ? powerOutFacing : powerOutFacing.Opposite);
				SetPropagationDirection(new MechPowerPath(blockFacing, mechanicalPowerDevice.GetGearedRatio(blockFacing.Opposite), blockPos));
			}
		}
		else
		{
			BEBehaviorMPBase behavior = Api.World.BlockAccessor.GetBlockEntity(blockPos).GetBehavior<BEBehaviorMPBase>();
			if (OutFacingForNetworkDiscovery != null)
			{
				if (tryConnect(OutFacingForNetworkDiscovery))
				{
					gearedRatio = behavior.GetGearedRatio(OutFacingForNetworkDiscovery);
				}
			}
			else
			{
				JoinNetwork(mechanicalNetwork);
				SetPropagationDirection(new MechPowerPath(behavior.propagationDir, behavior.GetGearedRatio(behavior.propagationDir), blockPos));
			}
		}
		return network;
	}

	public virtual bool JoinAndSpreadNetworkToNeighbours(ICoreAPI api, MechanicalNetwork network, MechPowerPath exitTurnDir, out Vec3i missingChunkPos)
	{
		missingChunkPos = null;
		if (this.network?.networkId == network?.networkId)
		{
			return true;
		}
		if (DEBUG)
		{
			api.Logger.Notification("Spread to " + Position?.ToString() + " with direction " + exitTurnDir.OutFacing?.ToString() + (exitTurnDir.invert ? "-" : "") + " Network:" + network.networkId);
		}
		SetPropagationDirection(exitTurnDir);
		JoinNetwork(network);
		(base.Block as IMechanicalPowerBlock).DidConnectAt(api.World, Position, exitTurnDir.OutFacing.Opposite);
		MechPowerPath[] mechPowerExits = GetMechPowerExits(exitTurnDir);
		for (int i = 0; i < mechPowerExits.Length; i++)
		{
			if (DEBUG)
			{
				api.Logger.Notification("-- spreading path " + (mechPowerExits[i].invert ? "-" : "") + mechPowerExits[i].OutFacing?.ToString() + "  " + mechPowerExits[i].gearingRatio);
			}
			BlockPos exitPos = Position.AddCopy(mechPowerExits[i].OutFacing);
			if (!spreadTo(api, network, exitPos, mechPowerExits[i], out missingChunkPos))
			{
				return false;
			}
		}
		return true;
	}

	protected virtual bool spreadTo(ICoreAPI api, MechanicalNetwork network, BlockPos exitPos, MechPowerPath propagatePath, out Vec3i missingChunkPos)
	{
		missingChunkPos = null;
		BEBehaviorMPBase bEBehaviorMPBase = api.World.BlockAccessor.GetBlockEntity(exitPos)?.GetBehavior<BEBehaviorMPBase>();
		IMechanicalPowerBlock mechanicalPowerBlock = bEBehaviorMPBase?.Block as IMechanicalPowerBlock;
		if (DEBUG)
		{
			api.Logger.Notification("attempting spread to " + exitPos?.ToString() + ((bEBehaviorMPBase == null) ? " -" : ""));
		}
		if (bEBehaviorMPBase == null && api.World.BlockAccessor.GetChunkAtBlockPos(exitPos) == null)
		{
			if (OutsideMap(api.World.BlockAccessor, exitPos))
			{
				return true;
			}
			missingChunkPos = new Vec3i(exitPos.X / 32, exitPos.Y / 32, exitPos.Z / 32);
			return false;
		}
		if (bEBehaviorMPBase != null && mechanicalPowerBlock.HasMechPowerConnectorAt(api.World, exitPos, propagatePath.OutFacing.Opposite))
		{
			bEBehaviorMPBase.Api = api;
			if (!bEBehaviorMPBase.JoinAndSpreadNetworkToNeighbours(api, network, propagatePath, out missingChunkPos))
			{
				return false;
			}
		}
		else if (DEBUG)
		{
			api.Logger.Notification("no connector at " + exitPos?.ToString() + " " + propagatePath.OutFacing.Opposite);
		}
		return true;
	}

	private bool OutsideMap(IBlockAccessor blockAccessor, BlockPos exitPos)
	{
		if (exitPos.X < 0 || exitPos.X >= blockAccessor.MapSizeX)
		{
			return true;
		}
		if (exitPos.Y < 0 || exitPos.Y >= blockAccessor.MapSizeY)
		{
			return true;
		}
		if (exitPos.Z < 0 || exitPos.Z >= blockAccessor.MapSizeZ)
		{
			return true;
		}
		return false;
	}

	protected virtual MechPowerPath[] GetMechPowerExits(MechPowerPath entryDir)
	{
		return new MechPowerPath[2]
		{
			entryDir,
			new MechPowerPath(entryDir.OutFacing.Opposite, entryDir.gearingRatio, Position, !entryDir.invert)
		};
	}

	public override void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
	{
		base.OnPlacementBySchematic(api, blockAccessor, pos, replaceBlocks, centerrockblockid, layerBlock, resolveImports);
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing toFacing in aLLFACES)
		{
			tryConnect(toFacing);
		}
	}

	Block IMechanicalPowerRenderable.get_Block()
	{
		return base.Block;
	}
}
