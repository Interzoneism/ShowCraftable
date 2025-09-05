using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class ChildDepositGenerator : DiscDepositGenerator
{
	[JsonProperty]
	public NatFloat RandomTries;

	public ChildDepositGenerator(ICoreServerAPI api, DepositVariant variant, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
		: base(api, variant, depositRand, noiseGen)
	{
	}

	public override void Init()
	{
	}

	public override void GetYMinMax(BlockPos pos, out double miny, out double maxy)
	{
		variant.parentDeposit.GeneratorInst.GetYMinMax(pos, out miny, out maxy);
	}

	public void ResolveAdd(Block inblock, string key, string value)
	{
		placeBlockByInBlockId[inblock.BlockId] = PlaceBlock.Resolve(variant.fromFile, Api, inblock, key, value);
		if (SurfaceBlock != null)
		{
			surfaceBlockByInBlockId[inblock.BlockId] = SurfaceBlock.Resolve(variant.fromFile, Api, inblock, key, value);
		}
	}

	public override void GenDeposit(IBlockAccessor blockAccessor, IServerChunk[] chunks, int originChunkX, int originChunkZ, BlockPos pos, ref Dictionary<BlockPos, DepositVariant> subDepositsToPlace)
	{
		IMapChunk mapChunk = chunks[0].MapChunk;
		int num = Math.Min(64, (int)Radius.nextFloat(1f, DepositRand));
		if (num <= 0)
		{
			return;
		}
		num++;
		int num2 = ((PlaceBlock.AllowedVariants != null) ? DepositRand.NextInt(PlaceBlock.AllowedVariants.Length) : 0);
		bool flag = DepositRand.NextFloat() > 0.35f && SurfaceBlock != null;
		float num3 = RandomTries.nextFloat(1f, DepositRand);
		for (int i = 0; (float)i < num3; i++)
		{
			targetPos.Set(pos.X + DepositRand.NextInt(2 * num + 1) - num, pos.Y + DepositRand.NextInt(2 * num + 1) - num, pos.Z + DepositRand.NextInt(2 * num + 1) - num);
			int num4 = targetPos.X % 32;
			int num5 = targetPos.Z % 32;
			if (targetPos.Y <= 1 || targetPos.Y >= worldheight || num4 < 0 || num5 < 0 || num4 >= 32 || num5 >= 32)
			{
				continue;
			}
			int index3d = (targetPos.Y % 32 * 32 + num5) * 32 + num4;
			int blockIdUnsafe = chunks[targetPos.Y / 32].Data.GetBlockIdUnsafe(index3d);
			if (!placeBlockByInBlockId.TryGetValue(blockIdUnsafe, out var value))
			{
				continue;
			}
			Block block = value.Blocks[num2];
			if (variant.WithBlockCallback)
			{
				block.TryPlaceBlockForWorldGen(blockAccessor, targetPos, BlockFacing.UP, DepositRand);
			}
			else
			{
				chunks[targetPos.Y / 32].Data[index3d] = block.BlockId;
			}
			if (!flag)
			{
				continue;
			}
			int num6 = Math.Min(mapChunk.RainHeightMap[num5 * 32 + num4], Api.World.BlockAccessor.MapSizeY - 2);
			int num7 = num6 - targetPos.Y;
			float num8 = SurfaceBlockChance * Math.Max(0f, 1f - (float)num7 / 8f);
			if (num6 < worldheight && DepositRand.NextFloat() < num8 && Api.World.Blocks[chunks[num6 / 32].Data.GetBlockIdUnsafe((num6 % 32 * 32 + num5) * 32 + num4)].SideSolid[BlockFacing.UP.Index])
			{
				index3d = ((num6 + 1) % 32 * 32 + num5) * 32 + num4;
				IChunkBlocks data = chunks[(num6 + 1) / 32].Data;
				if (data.GetBlockIdUnsafe(index3d) == 0)
				{
					data[index3d] = surfaceBlockByInBlockId[blockIdUnsafe].Blocks[0].BlockId;
				}
			}
		}
	}

	protected override void beforeGenDeposit(IMapChunk heremapchunk, BlockPos pos)
	{
	}

	protected override void loadYPosAndThickness(IMapChunk heremapchunk, int lx, int lz, BlockPos targetPos, double distanceToEdge)
	{
	}
}
