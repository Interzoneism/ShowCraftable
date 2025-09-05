using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockLocustNest : Block
{
	public Block[] DecoBlocksCeiling;

	public Block[] DecoBlocksFloor;

	public Block[] DecorBlocksWall;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		DecorBlocksWall = new Block[1] { api.World.GetBlock(new AssetLocation("oxidation-rust-normal")) };
		DecoBlocksCeiling = new Block[7]
		{
			api.World.GetBlock(new AssetLocation("locustnest-cage")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-none-upsidedown")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-none-upsidedown")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-none-upsidedown")),
			api.World.GetBlock(new AssetLocation("locustnest-stalagmite-main1")),
			api.World.GetBlock(new AssetLocation("locustnest-stalagmite-small1")),
			api.World.GetBlock(new AssetLocation("locustnest-stalagmite-small2"))
		};
		DecoBlocksFloor = new Block[10]
		{
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-none")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-none")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-none")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-tiny")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-tiny")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-tiny")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-small")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-small")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-medium")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-large"))
		};
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(world, blockPos, byItemStack);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		if (itemStack != null && itemStack.Attributes?.GetBool("spawnOnlyAfterImport") == true)
		{
			return base.GetHeldItemName(itemStack) + " " + Lang.Get("(delayed spawn)");
		}
		return base.GetHeldItemName(itemStack);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		ItemStack itemstack = inSlot.Itemstack;
		if (itemstack != null && itemstack.Attributes?.GetBool("spawnOnlyAfterImport") == true)
		{
			dsc.AppendLine(Lang.Get("Spawns locust nests only after import/world generation"));
		}
	}

	public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		(api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityLocustNest)?.OnBlockBreaking();
		return base.OnGettingBroken(player, blockSel, itemslot, remainingResistance, dt, counter);
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (blockAccessor.GetBlockId(pos.X, pos.Y, pos.Z) != 0)
		{
			return false;
		}
		if (blockAccessor.GetTerrainMapheightAt(pos) - pos.Y < 30 || pos.Y < 25)
		{
			return false;
		}
		BlockPos semiLargeCavePos = getSemiLargeCavePos(blockAccessor, pos);
		if (semiLargeCavePos == null)
		{
			return false;
		}
		int i;
		for (i = 0; i < 15 && !blockAccessor.IsSideSolid(semiLargeCavePos.X, semiLargeCavePos.Y + i, semiLargeCavePos.Z, BlockFacing.UP); i++)
		{
		}
		if (i >= 15)
		{
			return false;
		}
		blockAccessor.SetBlock(BlockId, semiLargeCavePos.AddCopy(0, i, 0));
		if (EntityClass != null)
		{
			blockAccessor.SpawnBlockEntity(EntityClass, semiLargeCavePos.AddCopy(0, i, 0));
		}
		BlockPos tmppos = new BlockPos();
		int num = 55 + worldGenRand.NextInt(55);
		while (num-- > 0)
		{
			int num2 = worldGenRand.NextInt(15) - 7;
			int num3 = worldGenRand.NextInt(15) - 7;
			int num4 = worldGenRand.NextInt(15) - 7;
			if (worldGenRand.NextDouble() < 0.4)
			{
				if (num2 != 0 || num4 != 0 || num3 > i)
				{
					tryPlaceDecoUp(tmppos.Set(semiLargeCavePos.X + num2, semiLargeCavePos.Y + num3, semiLargeCavePos.Z + num4), blockAccessor, worldGenRand);
				}
			}
			else if (num2 != 0 || num4 != 0 || num3 < i)
			{
				tryPlaceDecoDown(tmppos.Set(semiLargeCavePos.X + num2, semiLargeCavePos.Y + num3, semiLargeCavePos.Z + num4), blockAccessor, worldGenRand);
			}
		}
		blockAccessor.WalkBlocks(pos.AddCopy(-7, -7, -7), pos.AddCopy(7, 7, 7), delegate(Block block, int x, int y, int z)
		{
			if (block.Replaceable < 6000 && !(api.World.Rand.NextDouble() < 0.5))
			{
				for (int j = 0; j < 6; j++)
				{
					if (block.SideSolid[j])
					{
						BlockFacing blockFacing = BlockFacing.ALLFACES[j];
						if (blockAccessor.GetBlock(x + blockFacing.Normali.X, y + blockFacing.Normali.Y, z + blockFacing.Normali.Z).Id == 0)
						{
							blockAccessor.SetDecor(DecorBlocksWall[0], tmppos.Set(x, y, z), blockFacing);
							if (api.World.Rand.NextDouble() < 0.5)
							{
								break;
							}
						}
					}
				}
			}
		});
		return true;
	}

	private void tryPlaceDecoDown(BlockPos blockPos, IBlockAccessor blockAccessor, IRandom worldGenRand)
	{
		if (blockAccessor.GetBlock(blockPos).Id != 0)
		{
			return;
		}
		int num = 7;
		while (num-- > 0)
		{
			blockPos.Y--;
			if (blockAccessor.GetBlock(blockPos).SideSolid[BlockFacing.UP.Index])
			{
				blockPos.Y++;
				blockAccessor.SetBlock(DecoBlocksFloor[worldGenRand.NextInt(DecoBlocksFloor.Length)].BlockId, blockPos);
				break;
			}
		}
	}

	private void tryPlaceDecoUp(BlockPos blockPos, IBlockAccessor blockAccessor, IRandom worldgenRand)
	{
		if (blockAccessor.GetBlock(blockPos).Id != 0)
		{
			return;
		}
		int num = 7;
		while (num-- > 0)
		{
			blockPos.Y++;
			if (blockAccessor.GetBlock(blockPos).SideSolid[BlockFacing.DOWN.Index])
			{
				blockPos.Y--;
				Block block = DecoBlocksCeiling[worldgenRand.NextInt(DecoBlocksCeiling.Length)];
				blockAccessor.SetBlock(block.BlockId, blockPos);
				break;
			}
		}
	}

	private BlockPos getSemiLargeCavePos(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockPos blockPos = pos.Copy();
		int i = pos.Y;
		int num = pos.Y;
		int num2 = pos.X;
		int j = pos.X;
		int num3 = pos.Z;
		int k = pos.Z;
		while (pos.Y - num < 12 && blockAccessor.GetBlockId(pos.X, num - 1, pos.Z) == 0)
		{
			num--;
		}
		for (; i - pos.Y < 12 && blockAccessor.GetBlockId(pos.X, i + 1, pos.Z) == 0; i++)
		{
		}
		blockPos.Y = (i + num) / 2;
		if (i - num < 4 || i - num >= 10)
		{
			return null;
		}
		while (pos.X - num2 < 12 && blockAccessor.GetBlockId(num2 - 1, pos.Y, pos.Z) == 0)
		{
			num2--;
		}
		for (; j - pos.X < 12 && blockAccessor.GetBlockId(j + 1, pos.Y, pos.Z) == 0; j++)
		{
		}
		if (j - num2 < 3)
		{
			return null;
		}
		blockPos.X = (j + num2) / 2;
		while (pos.Z - num3 < 12 && blockAccessor.GetBlockId(pos.X, pos.Y, num3 - 1) == 0)
		{
			num3--;
		}
		for (; k - pos.Z < 12 && blockAccessor.GetBlockId(pos.X, pos.Y, k + 1) == 0; k++)
		{
		}
		if (k - num3 < 3)
		{
			return null;
		}
		blockPos.Z = (k + num3) / 2;
		return blockPos;
	}
}
