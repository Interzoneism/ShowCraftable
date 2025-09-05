using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockTobiasTeleporter : Block
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		bool num = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
		if (num)
		{
			BlockEntityTobiasTeleporter blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityTobiasTeleporter>(blockSel.Position);
			TobiasTeleporter modSystem = api.ModLoader.GetModSystem<TobiasTeleporter>();
			if (blockEntity != null && world.Api.Side == EnumAppSide.Server)
			{
				blockEntity.OwnerPlayerUid = byPlayer.PlayerUID;
				blockEntity.OwnerName = byPlayer.PlayerName;
				modSystem.AddPlayerLocation(byPlayer.PlayerUID, blockSel.Position);
				blockEntity.MarkDirty();
			}
		}
		return num;
	}

	public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityTobiasTeleporter blockEntityTobiasTeleporter)
		{
			blockEntityTobiasTeleporter.OnEntityCollide(entity);
		}
	}

	public static Vec3d GetTeleportOffset(string side)
	{
		Vec3d a = BlockFacing.FromCode(side).Normald * -1f;
		return new Vec3d(0.5, 1.0, 0.5).Add(a);
	}
}
