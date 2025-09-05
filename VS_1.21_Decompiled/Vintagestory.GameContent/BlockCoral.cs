using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Datastructures;

namespace Vintagestory.GameContent;

public class BlockCoral : BlockWaterPlant
{
	private Block saltwater;

	public override bool skipPlantCheck { get; set; } = true;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		saltwater = api.World.BlockAccessor.GetBlock(new AssetLocation("saltwater-still-7"));
	}

	public override bool TryPlaceBlockForWorldGenUnderwater(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, int minWaterDepth, int maxWaterDepth, BlockPatchAttributes? attributes = null)
	{
		if (attributes == null)
		{
			return false;
		}
		int num = ((attributes.CoralMinSize != -1) ? attributes.CoralMinSize : 800);
		int num2 = ((attributes.CoralRandomSize != -1) ? attributes.CoralRandomSize : 400);
		float num3 = ((attributes.CoralPlantsChance != -1f) ? attributes.CoralPlantsChance : 0.03f);
		float num4 = ((attributes.CoralReplaceOtherPatches != -1f) ? attributes.CoralReplaceOtherPatches : 0.03f);
		NaturalShape naturalShape = new NaturalShape(worldGenRand);
		int num5 = ((num2 != 0) ? worldGenRand.NextInt(num2) : 0);
		naturalShape.Grow(num + num5);
		foreach (BlockPos position in naturalShape.GetPositions(pos))
		{
			for (int i = 1; i < maxWaterDepth; i++)
			{
				position.Down();
				Block block = blockAccessor.GetBlock(position);
				if (block is BlockCoral)
				{
					break;
				}
				if (block is BlockWaterPlant)
				{
					if (!(worldGenRand.NextFloat() < num4))
					{
						break;
					}
					do
					{
						blockAccessor.SetBlock(saltwater.BlockId, position);
						position.Down();
						block = blockAccessor.GetBlock(position);
					}
					while (block is BlockWaterPlant blockWaterPlant && !(blockWaterPlant is BlockCoral));
				}
				if (block.IsLiquid())
				{
					continue;
				}
				if (i < minWaterDepth)
				{
					break;
				}
				Dictionary<string, CoralPlantConfig>? coralPlants = attributes.CoralPlants;
				if (coralPlants != null && coralPlants.Count > 0 && worldGenRand.NextFloat() <= num3)
				{
					Block randomBlock = GetRandomBlock(worldGenRand, attributes.CoralBaseBlock);
					blockAccessor.SetBlock(randomBlock.BlockId, position);
					if (i + 1 >= minWaterDepth)
					{
						SpawnSeaPlantWeighted(blockAccessor, worldGenRand, attributes, position, i);
					}
				}
				else
				{
					PlaceCoral(blockAccessor, position, worldGenRand, i, minWaterDepth, attributes);
				}
				break;
			}
		}
		return true;
	}

	private static void SpawnSeaPlantWeighted(IBlockAccessor blockAccessor, IRandom worldGenRand, BlockPatchAttributes attributes, BlockPos tmpPos, int depth)
	{
		float num = attributes.CoralPlants.Sum<KeyValuePair<string, CoralPlantConfig>>((KeyValuePair<string, CoralPlantConfig> c) => c.Value.Chance);
		float num2 = worldGenRand.NextFloat() * num;
		float num3 = 0f;
		foreach (CoralPlantConfig value in attributes.CoralPlants.Values)
		{
			num3 += value.Chance;
			if (num2 < num3)
			{
				int num4 = worldGenRand.NextInt(value.Block.Length);
				if (value.Block[num4] is BlockSeaweed blockSeaweed)
				{
					blockSeaweed.PlaceSeaweed(blockAccessor, tmpPos, depth - 1, worldGenRand, value.Height);
				}
				break;
			}
		}
	}

	public void PlaceCoral(IBlockAccessor blockAccessor, BlockPos pos, IRandom worldGenRand, int depth, int minDepth, BlockPatchAttributes attributes)
	{
		float num = ((attributes.CoralVerticalGrowChance != -1f) ? attributes.CoralVerticalGrowChance : 0.6f);
		float num2 = ((attributes.CoralShelveChance != -1f) ? attributes.CoralShelveChance : 0.3f);
		float num3 = ((attributes.CoralStructureChance != -1f) ? attributes.CoralStructureChance : 0.5f);
		float num4 = ((attributes.CoralDecorChance != -1f) ? attributes.CoralDecorChance : 0.5f);
		int coralBaseHeight = attributes.CoralBaseHeight;
		pos.Add(0, -(coralBaseHeight - 1), 0);
		for (int i = 0; i < coralBaseHeight; i++)
		{
			if (blockAccessor.GetBlock(pos) is BlockCoral)
			{
				pos.Up();
				break;
			}
			Block randomBlock = GetRandomBlock(worldGenRand, attributes.CoralBaseBlock);
			blockAccessor.SetBlock(randomBlock.BlockId, pos);
			pos.Up();
		}
		depth--;
		if (depth <= 0)
		{
			return;
		}
		float num5 = worldGenRand.NextFloat();
		bool flag = true;
		if (num5 < num2)
		{
			List<int> solidSides = GetSolidSides(blockAccessor, pos);
			if (solidSides.Count > 0)
			{
				int index = worldGenRand.NextInt(solidSides.Count);
				Block[] randomShelve = GetRandomShelve(worldGenRand, attributes.CoralShelveBlock);
				GetRandomShelve(worldGenRand, attributes.CoralShelveBlock);
				blockAccessor.SetBlock(randomShelve[solidSides[index]].BlockId, pos);
				flag = false;
			}
		}
		if (flag)
		{
			if (worldGenRand.NextFloat() < num3)
			{
				Block randomBlock2 = GetRandomBlock(worldGenRand, attributes.CoralStructureBlock);
				blockAccessor.SetBlock(randomBlock2.BlockId, pos);
				pos.Up();
				depth--;
			}
			if (depth > 0)
			{
				if (worldGenRand.NextFloat() < attributes.CoralChance)
				{
					Block randomBlock3 = GetRandomBlock(worldGenRand, attributes.CoralBlock);
					blockAccessor.SetBlock(randomBlock3.BlockId, pos);
				}
				if (attributes.StructureDecorBlock != null && worldGenRand.NextFloat() < num4)
				{
					Block randomBlock4 = GetRandomBlock(worldGenRand, attributes.StructureDecorBlock);
					blockAccessor.SetDecor(randomBlock4, pos.DownCopy(), BlockFacing.UP);
				}
				if (attributes.CoralDecorBlock != null)
				{
					foreach (int solidSide in GetSolidSides(blockAccessor, pos))
					{
						if (worldGenRand.NextFloat() < num4)
						{
							Block randomBlock5 = GetRandomBlock(worldGenRand, attributes.CoralDecorBlock);
							blockAccessor.SetDecor(randomBlock5, pos.AddCopy(BlockFacing.HORIZONTALS[solidSide]), BlockFacing.HORIZONTALS[solidSide].Opposite);
						}
					}
				}
			}
		}
		if (depth - minDepth == 0)
		{
			return;
		}
		pos.Up();
		depth--;
		for (int j = 0; j < depth - minDepth; j++)
		{
			if (worldGenRand.NextFloat() > num)
			{
				pos.Up();
				depth--;
				continue;
			}
			List<int> solidSides2 = GetSolidSides(blockAccessor, pos);
			if (solidSides2.Count != 0)
			{
				int num6 = solidSides2[worldGenRand.NextInt(solidSides2.Count)];
				if (worldGenRand.NextFloat() < num4)
				{
					Block randomBlock6 = GetRandomBlock(worldGenRand, attributes.CoralDecorBlock);
					blockAccessor.SetDecor(randomBlock6, pos.AddCopy(BlockFacing.HORIZONTALS[num6]), BlockFacing.HORIZONTALS[num6].Opposite);
					pos.Up();
					depth--;
				}
				else
				{
					Block[] randomShelve2 = GetRandomShelve(worldGenRand, attributes.CoralShelveBlock);
					blockAccessor.SetBlock(randomShelve2[num6].BlockId, pos);
					pos.Up();
					depth--;
				}
			}
		}
	}

	private static Block[] GetRandomShelve(IRandom worldGenRand, Block[][] blocks)
	{
		return blocks[worldGenRand.NextInt(blocks.Length)];
	}

	private static Block GetRandomBlock(IRandom worldGenRand, Block[] blocks)
	{
		return blocks[worldGenRand.NextInt(blocks.Length)];
	}

	private static List<int> GetSolidSides(IBlockAccessor blockAccessor, BlockPos pos)
	{
		List<int> list = new List<int>();
		BlockPos blockPos = pos.NorthCopy();
		if (blockAccessor.GetBlock(blockPos).SideSolid[BlockFacing.SOUTH.Index])
		{
			list.Add(BlockFacing.NORTH.Index);
		}
		blockPos.Z += 2;
		if (blockAccessor.GetBlock(blockPos).SideSolid[BlockFacing.NORTH.Index])
		{
			list.Add(BlockFacing.SOUTH.Index);
		}
		blockPos.Z--;
		blockPos.X++;
		if (blockAccessor.GetBlock(blockPos).SideSolid[BlockFacing.WEST.Index])
		{
			list.Add(BlockFacing.EAST.Index);
		}
		blockPos.X -= 2;
		if (blockAccessor.GetBlock(blockPos).SideSolid[BlockFacing.EAST.Index])
		{
			list.Add(BlockFacing.WEST.Index);
		}
		return list;
	}
}
