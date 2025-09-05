using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent.Mechanics;

public class BEBehaviorMPAxle : BEBehaviorMPBase
{
	private Vec3f center = new Vec3f(0.5f, 0.5f, 0.5f);

	private BlockFacing[] orients = new BlockFacing[2];

	private ICoreClientAPI capi;

	private string orientations;

	private AssetLocation axleStandLocWest;

	private AssetLocation axleStandLocEast;

	protected virtual bool AddStands => true;

	public BEBehaviorMPAxle(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		axleStandLocWest = AssetLocation.Create("block/wood/mechanics/axle-stand-west", base.Block.Code?.Domain);
		axleStandLocEast = AssetLocation.Create("block/wood/mechanics/axle-stand-east", base.Block.Code?.Domain);
		JsonObject attributes = base.Block.Attributes;
		if (attributes != null && attributes["axleStandLocWest"].Exists)
		{
			axleStandLocWest = base.Block.Attributes["axleStandLocWest"].AsObject<AssetLocation>();
		}
		JsonObject attributes2 = base.Block.Attributes;
		if (attributes2 != null && attributes2["axleStandLocEast"].Exists)
		{
			axleStandLocEast = base.Block.Attributes["axleStandLocEast"].AsObject<AssetLocation>();
		}
		axleStandLocWest.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		axleStandLocEast.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		if (api.Side == EnumAppSide.Client)
		{
			capi = api as ICoreClientAPI;
		}
		orientations = base.Block.Variant["rotation"];
		switch (orientations)
		{
		case "ns":
			AxisSign = new int[3] { 0, 0, -1 };
			orients[0] = BlockFacing.NORTH;
			orients[1] = BlockFacing.SOUTH;
			break;
		case "we":
			AxisSign = new int[3] { -1, 0, 0 };
			orients[0] = BlockFacing.WEST;
			orients[1] = BlockFacing.EAST;
			break;
		case "ud":
			AxisSign = new int[3] { 0, 1, 0 };
			orients[0] = BlockFacing.DOWN;
			orients[1] = BlockFacing.UP;
			break;
		}
	}

	public override float GetResistance()
	{
		return 0.0005f;
	}

	protected virtual MeshData getStandMesh(string orient)
	{
		return ObjectCacheUtil.GetOrCreate(Api, string.Concat(base.Block.Code, "-", orient, "-stand"), delegate
		{
			Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, (orient == "west") ? axleStandLocWest : axleStandLocEast);
			capi.Tesselator.TesselateShape(base.Block, shape, out var modeldata);
			return modeldata;
		});
	}

	public static bool IsAttachedToBlock(IBlockAccessor blockaccessor, Block block, BlockPos Position)
	{
		string text = block.Variant["rotation"];
		if (text == "ns" || text == "we")
		{
			if (blockaccessor.GetBlockBelow(Position, 1, 1).SideSolid[BlockFacing.UP.Index] || blockaccessor.GetBlockAbove(Position, 1, 1).SideSolid[BlockFacing.DOWN.Index])
			{
				return true;
			}
			BlockFacing blockFacing = ((text == "ns") ? BlockFacing.WEST : BlockFacing.NORTH);
			if (!blockaccessor.GetBlockOnSide(Position, blockFacing, 1).SideSolid[blockFacing.Opposite.Index])
			{
				return blockaccessor.GetBlockOnSide(Position, blockFacing.Opposite, 1).SideSolid[blockFacing.Index];
			}
			return true;
		}
		for (int i = 0; i < 4; i++)
		{
			BlockFacing blockFacing2 = BlockFacing.HORIZONTALS[i];
			if (blockaccessor.GetBlockOnSide(Position, blockFacing2, 1).SideSolid[blockFacing2.Opposite.Index])
			{
				return true;
			}
		}
		return false;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (AddStands)
		{
			if (RequiresStand(Api.World, Position, orients[0].Normali))
			{
				MeshData standMesh = getStandMesh("west");
				standMesh = rotStand(standMesh);
				if (standMesh != null)
				{
					mesher.AddMeshData(standMesh);
				}
			}
			if (RequiresStand(Api.World, Position, orients[1].Normali))
			{
				MeshData standMesh2 = getStandMesh("east");
				standMesh2 = rotStand(standMesh2);
				if (standMesh2 != null)
				{
					mesher.AddMeshData(standMesh2);
				}
			}
		}
		return base.OnTesselation(mesher, tesselator);
	}

	private bool RequiresStand(IWorldAccessor world, BlockPos pos, Vec3i vector)
	{
		try
		{
			if (!(world.BlockAccessor.GetBlockRaw(pos.X + vector.X, pos.InternalY + vector.Y, pos.Z + vector.Z, 1) is BlockMPBase blockMPBase))
			{
				return true;
			}
			BlockPos blockPos = new BlockPos(pos.X + vector.X, pos.Y + vector.Y, pos.Z + vector.Z, pos.dimension);
			BEBehaviorMPBase bEBehaviorMPBase = world.BlockAccessor.GetBlockEntity(blockPos)?.GetBehavior<BEBehaviorMPBase>();
			if (bEBehaviorMPBase == null)
			{
				return true;
			}
			if (!(bEBehaviorMPBase is BEBehaviorMPAxle bEBehaviorMPAxle))
			{
				if (bEBehaviorMPBase is BEBehaviorMPBrake || bEBehaviorMPBase is BEBehaviorMPCreativeRotor)
				{
					BlockFacing blockFacing = BlockFacing.FromNormal(vector);
					if (blockFacing != null && blockMPBase.HasMechPowerConnectorAt(world, blockPos, blockFacing.Opposite))
					{
						return false;
					}
				}
				return true;
			}
			if (bEBehaviorMPAxle.orientations == orientations && IsAttachedToBlock(world.BlockAccessor, blockMPBase, blockPos))
			{
				return false;
			}
			return bEBehaviorMPAxle.RequiresStand(world, blockPos, vector);
		}
		catch (Exception e)
		{
			world.Logger.Error("Exception thrown in RequiresStand, will log exception but silently ignore it: at " + pos);
			world.Logger.Error(e);
			return false;
		}
	}

	private MeshData rotStand(MeshData mesh)
	{
		if (orientations == "ns" || orientations == "we")
		{
			mesh = mesh.Clone();
			if (orientations == "ns")
			{
				mesh = mesh.Rotate(center, 0f, -(float)Math.PI / 2f, 0f);
			}
			if (!Api.World.BlockAccessor.GetBlockBelow(Position, 1, 1).SideSolid[BlockFacing.UP.Index])
			{
				if (Api.World.BlockAccessor.GetBlockAbove(Position, 1, 1).SideSolid[BlockFacing.DOWN.Index])
				{
					mesh = mesh.Rotate(center, (float)Math.PI, 0f, 0f);
				}
				else if (orientations == "ns")
				{
					BlockFacing eAST = BlockFacing.EAST;
					if (Api.World.BlockAccessor.GetBlockOnSide(Position, eAST, 1).SideSolid[eAST.Opposite.Index])
					{
						mesh = mesh.Rotate(center, 0f, 0f, (float)Math.PI / 2f);
					}
					else
					{
						eAST = BlockFacing.WEST;
						if (!Api.World.BlockAccessor.GetBlockOnSide(Position, eAST, 1).SideSolid[eAST.Opposite.Index])
						{
							return null;
						}
						mesh = mesh.Rotate(center, 0f, 0f, -(float)Math.PI / 2f);
					}
				}
				else
				{
					BlockFacing nORTH = BlockFacing.NORTH;
					if (Api.World.BlockAccessor.GetBlockOnSide(Position, nORTH, 1).SideSolid[nORTH.Opposite.Index])
					{
						mesh = mesh.Rotate(center, (float)Math.PI / 2f, 0f, 0f);
					}
					else
					{
						nORTH = BlockFacing.SOUTH;
						if (!Api.World.BlockAccessor.GetBlockOnSide(Position, nORTH, 1).SideSolid[nORTH.Opposite.Index])
						{
							return null;
						}
						mesh = mesh.Rotate(center, -(float)Math.PI / 2f, 0f, 0f);
					}
				}
			}
			return mesh;
		}
		BlockFacing blockFacing = null;
		for (int i = 0; i < 4; i++)
		{
			BlockFacing blockFacing2 = BlockFacing.HORIZONTALS[i];
			if (Api.World.BlockAccessor.GetBlockOnSide(Position, blockFacing2, 1).SideSolid[blockFacing2.Opposite.Index])
			{
				blockFacing = blockFacing2;
				break;
			}
		}
		if (blockFacing != null)
		{
			mesh = mesh.Clone().Rotate(center, 0f, 0f, (float)Math.PI / 2f).Rotate(center, 0f, (float)(blockFacing.HorizontalAngleIndex * 90) * ((float)Math.PI / 180f), 0f);
			return mesh;
		}
		return null;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
		if (Api.World.EntityDebugMode)
		{
			string text = base.Block.Variant["orientation"];
			sb.AppendLine(string.Format(Lang.Get("Orientation: {0}", text)));
		}
	}
}
