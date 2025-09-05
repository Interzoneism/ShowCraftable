using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent.Mechanics;

namespace Vintagestory.GameContent;

public class BlockQuern : BlockMPBase
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		bool num = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
		if (num && !tryConnect(world, byPlayer, blockSel.Position, BlockFacing.UP))
		{
			tryConnect(world, byPlayer, blockSel.Position, BlockFacing.DOWN);
		}
		return num;
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityQuern blockEntityQuern && blockEntityQuern.CanGrind() && (blockSel.SelectionBoxIndex == 1 || blockEntityQuern.Inventory.openedByPlayerGUIds.Contains(byPlayer.PlayerUID)))
		{
			blockEntityQuern.SetPlayerGrinding(byPlayer, playerGrinding: true);
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityQuern blockEntityQuern && (blockSel.SelectionBoxIndex == 1 || blockEntityQuern.Inventory.openedByPlayerGUIds.Contains(byPlayer.PlayerUID)))
		{
			blockEntityQuern.IsGrinding(byPlayer);
			return blockEntityQuern.CanGrind();
		}
		return false;
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityQuern blockEntityQuern)
		{
			blockEntityQuern.SetPlayerGrinding(byPlayer, playerGrinding: false);
		}
	}

	public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityQuern blockEntityQuern)
		{
			blockEntityQuern.SetPlayerGrinding(byPlayer, playerGrinding: false);
		}
		return true;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		if (selection.SelectionBoxIndex == 0)
		{
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-quern-addremoveitems",
					MouseButton = EnumMouseButton.Right
				}
			}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
		}
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-quern-grind",
				MouseButton = EnumMouseButton.Right,
				ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityQuern blockEntityQuern && blockEntityQuern.CanGrind()
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
	}

	public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
		if (face != BlockFacing.UP)
		{
			return face == BlockFacing.DOWN;
		}
		return true;
	}

	public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
		if (facing != BlockFacing.UP)
		{
			return;
		}
		if (entity.World.Side == EnumAppSide.Server)
		{
			float physicsFrameTime = GlobalConstants.PhysicsFrameTime;
			BEBehaviorMPConsumer bEBehavior = GetBEBehavior<BEBehaviorMPConsumer>(pos);
			if (bEBehavior != null)
			{
				entity.SidedPos.Yaw += physicsFrameTime * bEBehavior.TrueSpeed * 2.5f * (float)((!bEBehavior.isRotationReversed()) ? 1 : (-1));
			}
			return;
		}
		float physicsFrameTime2 = GlobalConstants.PhysicsFrameTime;
		BEBehaviorMPConsumer bEBehavior2 = GetBEBehavior<BEBehaviorMPConsumer>(pos);
		ICoreClientAPI coreClientAPI = api as ICoreClientAPI;
		if (coreClientAPI.World.Player.Entity.EntityId == entity.EntityId)
		{
			int num = ((!bEBehavior2.isRotationReversed()) ? 1 : (-1));
			if (coreClientAPI.World.Player.CameraMode != EnumCameraMode.Overhead)
			{
				coreClientAPI.Input.MouseYaw += physicsFrameTime2 * bEBehavior2.TrueSpeed * 2.5f * (float)num;
			}
			coreClientAPI.World.Player.Entity.BodyYaw += physicsFrameTime2 * bEBehavior2.TrueSpeed * 2.5f * (float)num;
			coreClientAPI.World.Player.Entity.WalkYaw += physicsFrameTime2 * bEBehavior2.TrueSpeed * 2.5f * (float)num;
			coreClientAPI.World.Player.Entity.Pos.Yaw += physicsFrameTime2 * bEBehavior2.TrueSpeed * 2.5f * (float)num;
		}
	}
}
