using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockMeteorite : Block
{
	private BlockPos tmpPos = new BlockPos();

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blAcc, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRand, BlockPatchAttributes attributes = null)
	{
		int num = 2 + worldgenRand.NextInt(25);
		float num2 = GameMath.Sqrt(GameMath.Sqrt(num));
		float num3 = GameMath.Sqrt(num) * 1.25f;
		if (pos.Y > 250 || !IsSolid(blAcc, pos.X, pos.Y - 3, pos.Z) || !IsSolid(blAcc, pos.X, pos.Y - (int)num2, pos.Z) || !IsSolid(blAcc, pos.X + (int)num3, pos.Y - 1, pos.Z) || !IsSolid(blAcc, pos.X - (int)num3, pos.Y - 1, pos.Z) || !IsSolid(blAcc, pos.X, pos.Y - 1, pos.Z - (int)num3) || !IsSolid(blAcc, pos.X, pos.Y - 1, pos.Z + (int)num3))
		{
			return false;
		}
		int terrainMapheightAt = blAcc.GetTerrainMapheightAt(tmpPos.Set(pos.X - 5, pos.Y, pos.Z));
		int terrainMapheightAt2 = blAcc.GetTerrainMapheightAt(tmpPos.Set(pos.X + 5, pos.Y, pos.Z));
		int terrainMapheightAt3 = blAcc.GetTerrainMapheightAt(tmpPos.Set(pos.X, pos.Y, pos.Z + 5));
		int terrainMapheightAt4 = blAcc.GetTerrainMapheightAt(tmpPos.Set(pos.X, pos.Y, pos.Z - 5));
		if (GameMath.Max(terrainMapheightAt, terrainMapheightAt2, terrainMapheightAt3, terrainMapheightAt4) - GameMath.Min(terrainMapheightAt, terrainMapheightAt2, terrainMapheightAt3, terrainMapheightAt4) > 4)
		{
			return false;
		}
		tmpPos = tmpPos.Set(pos.X, pos.Y - (int)num2 - 2, pos.Z);
		while (num-- > 0)
		{
			tmpPos.X += ((worldgenRand.NextInt(3) == 0) ? (worldgenRand.NextInt(3) - 1) : 0);
			tmpPos.Y += ((worldgenRand.NextInt(8) == 0) ? (worldgenRand.NextInt(3) - 1) : 0);
			tmpPos.Z += ((worldgenRand.NextInt(3) == 0) ? (worldgenRand.NextInt(3) - 1) : 0);
			blAcc.SetBlock(BlockId, tmpPos);
		}
		int blockId = api.World.GetBlock(new AssetLocation("rock-suevite")).BlockId;
		int blockId2 = api.World.GetBlock(new AssetLocation("loosestones-meteorite-iron-free")).BlockId;
		int blockId3 = api.World.GetBlock(new AssetLocation("loosestones-suevite-free")).BlockId;
		float num4 = num3 * 1.2f;
		int num5 = (int)Math.Ceiling(num4);
		Vec2i vec2i = new Vec2i();
		for (int i = -num5; i <= num5; i++)
		{
			for (int j = -num5; j <= num5; j++)
			{
				float num6 = (float)(i * i + j * j) / (num4 * num4);
				if (num6 > 1f)
				{
					continue;
				}
				tmpPos.X = pos.X + i;
				tmpPos.Z = pos.Z + j;
				int terrainMapheightAt5 = blAcc.GetTerrainMapheightAt(tmpPos);
				tmpPos.Y = terrainMapheightAt5 - (int)num2;
				vec2i.X = tmpPos.X / 32;
				vec2i.Y = tmpPos.Z / 32;
				IMapChunk mapChunk = blAcc.GetMapChunk(vec2i);
				float num7 = 3f * Math.Max(0f, 2f * (1f - num6) - 0.2f);
				tmpPos.Y -= (int)num7 + 1;
				while (num7 > 0f && (!(num7 < 1f) || !(worldgenRand.NextDouble() > (double)num7)))
				{
					Block block = blAcc.GetBlock(tmpPos);
					if (block != this && block.BlockMaterial == EnumBlockMaterial.Stone)
					{
						blAcc.SetBlock(blockId, tmpPos);
					}
					num7 -= 1f;
					tmpPos.Y++;
				}
				float num8 = (float)(i * i + j * j) / (num3 * num3) + (float)worldgenRand.NextDouble() * 0.1f;
				if (num8 > 1f)
				{
					continue;
				}
				num7 = num2 * (1f - num8);
				tmpPos.Y = terrainMapheightAt5;
				ItemStack BEStack;
				TreeAttribute BETree;
				Block blockAndBEdata = GetBlockAndBEdata(blAcc, tmpPos, out BEStack, out BETree);
				tmpPos.Y++;
				ItemStack BEStack2;
				TreeAttribute BETree2;
				Block blockAndBEdata2 = GetBlockAndBEdata(blAcc, tmpPos, out BEStack2, out BETree2);
				tmpPos.Y++;
				ItemStack BEStack3;
				TreeAttribute BETree3;
				Block blockAndBEdata3 = GetBlockAndBEdata(blAcc, tmpPos, out BEStack3, out BETree3);
				for (int k = -2; k <= (int)num7; k++)
				{
					tmpPos.Y = terrainMapheightAt5 - k;
					int num9 = ((k == (int)num7) ? blockAndBEdata.BlockId : 0);
					if (!blAcc.GetBlock(tmpPos, 2).IsLiquid())
					{
						blAcc.SetBlock(num9, tmpPos);
						if (num9 > 0)
						{
							MaybeSpawnBlockEntity(blockAndBEdata, blAcc, tmpPos, BEStack, BETree);
						}
					}
				}
				mapChunk.WorldGenTerrainHeightMap[tmpPos.Z % 32 * 32 + tmpPos.X % 32] -= (ushort)num7;
				tmpPos.Y = blAcc.GetTerrainMapheightAt(tmpPos) + 1;
				if (blockAndBEdata2.BlockId > 0)
				{
					blAcc.SetBlock(blockAndBEdata2.BlockId, tmpPos);
					MaybeSpawnBlockEntity(blockAndBEdata2, blAcc, tmpPos, BEStack2, BETree2);
				}
				tmpPos.Y++;
				if (blockAndBEdata3.BlockId > 0)
				{
					blAcc.SetBlock(blockAndBEdata3.BlockId, tmpPos);
					MaybeSpawnBlockEntity(blockAndBEdata3, blAcc, tmpPos, BEStack3, BETree3);
				}
			}
		}
		int num10 = 0;
		if (worldgenRand.NextInt(10) == 0)
		{
			num10 = worldgenRand.NextInt(10);
		}
		else if (worldgenRand.NextInt(5) == 0)
		{
			num10 = worldgenRand.NextInt(5);
		}
		while (num10-- > 0)
		{
			tmpPos.Set(pos.X + (worldgenRand.NextInt(11) + worldgenRand.NextInt(11)) / 2 - 5, 0, pos.Z + (worldgenRand.NextInt(11) + worldgenRand.NextInt(11)) / 2 - 5);
			tmpPos.Y = blAcc.GetTerrainMapheightAt(tmpPos) + 1;
			if (blAcc.IsSideSolid(tmpPos.X, tmpPos.Y - 1, tmpPos.Z, BlockFacing.UP))
			{
				if (worldgenRand.NextDouble() < 0.3)
				{
					blAcc.SetBlock(blockId2, tmpPos);
				}
				else
				{
					blAcc.SetBlock(blockId3, tmpPos);
				}
			}
		}
		return true;
	}

	private Block GetBlockAndBEdata(IBlockAccessor blAcc, BlockPos pos, out ItemStack BEStack, out TreeAttribute BETree)
	{
		BEStack = null;
		BETree = null;
		Block block = blAcc.GetBlock(pos, 1);
		try
		{
			if (block.EntityClass != null)
			{
				BlockEntity blockEntity = blAcc.GetBlockEntity(pos);
				if (blockEntity != null)
				{
					BEStack = blockEntity.stackForWorldgen;
					if (BEStack == null)
					{
						BETree = new TreeAttribute();
						blockEntity.ToTreeAttributes(BETree);
					}
				}
				if (BEStack == null && BETree == null)
				{
					BEStack = block.OnPickBlock(api.World, pos);
				}
			}
		}
		catch
		{
			BEStack = null;
			BETree = null;
		}
		return block;
	}

	private void MaybeSpawnBlockEntity(Block block, IBlockAccessor blAcc, BlockPos pos, ItemStack BEStack, TreeAttribute BETree)
	{
		if (block.EntityClass == null)
		{
			return;
		}
		try
		{
			blAcc.SpawnBlockEntity(block.EntityClass, pos, BEStack);
			if (BETree != null)
			{
				BlockEntity blockEntity = blAcc.GetBlockEntity(pos);
				BETree.SetInt("posy", pos.Y);
				blockEntity?.FromTreeAttributes(BETree, api.World);
			}
		}
		catch (Exception e)
		{
			api.Logger.Error(e);
		}
	}

	private bool IsSolid(IBlockAccessor blAcc, int x, int y, int z)
	{
		return blAcc.IsSideSolid(x, y, z, BlockFacing.UP);
	}
}
