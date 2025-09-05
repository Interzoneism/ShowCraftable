using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockPlaceOnDrop : Block
{
	public override void OnGroundIdle(EntityItem entityItem)
	{
		if (entityItem.World.Side == EnumAppSide.Client || entityItem.ShouldDespawn)
		{
			return;
		}
		if (TryPlace(entityItem, 0, 0, 0))
		{
			entityItem.Die(EnumDespawnReason.Removed);
		}
		else if (TryPlace(entityItem, 0, 1, 0))
		{
			entityItem.Die(EnumDespawnReason.Removed);
		}
		else if (TryPlace(entityItem, 0, -1, 0))
		{
			entityItem.Die(EnumDespawnReason.Removed);
		}
		else
		{
			if (!entityItem.CollidedVertically)
			{
				return;
			}
			List<BlockPos> list = new List<BlockPos>();
			for (int i = -1; i < 1; i++)
			{
				for (int j = -1; j < 1; j++)
				{
					for (int k = -1; k < 1; k++)
					{
						list.Add(new BlockPos(i, j, k));
					}
				}
			}
			BlockPos[] array = list.ToArray().Shuffle(entityItem.World.Rand);
			for (int l = 0; l < array.Length; l++)
			{
				if (TryPlace(entityItem, array[l].X, array[l].Y, array[l].Z))
				{
					entityItem.Die(EnumDespawnReason.Removed);
					break;
				}
			}
		}
	}

	private bool TryPlace(EntityItem entityItem, int offX, int offY, int offZ)
	{
		IWorldAccessor world = entityItem.World;
		BlockPos blockPos = entityItem.ServerPos.AsBlockPos.Add(offX, offY - 1, offZ);
		if (!world.BlockAccessor.GetMostSolidBlock(blockPos).CanAttachBlockAt(world.BlockAccessor, this, blockPos, BlockFacing.UP))
		{
			return false;
		}
		string failureCode = "";
		bool num = TryPlaceBlock(world, null, entityItem.Itemstack, new BlockSelection
		{
			Position = blockPos,
			Face = BlockFacing.UP,
			HitPosition = new Vec3d(0.5, 1.0, 0.5)
		}, ref failureCode);
		if (num)
		{
			entityItem.World.PlaySoundAt(entityItem.Itemstack.Block.Sounds?.Place, blockPos, -0.5);
		}
		return num;
	}
}
