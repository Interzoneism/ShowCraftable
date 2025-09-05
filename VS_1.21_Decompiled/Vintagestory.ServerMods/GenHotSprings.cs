using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods;

public class GenHotSprings : ModStdWorldGen
{
	private Block[] decorBlocks;

	private Block blocksludgygravel;

	private int boilingWaterBlockId;

	private ICoreServerAPI api;

	private IWorldGenBlockAccessor wgenBlockAccessor;

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		this.api = api;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.ChunkColumnGeneration(GenChunkColumn, EnumWorldGenPass.TerrainFeatures, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
			api.Event.InitWorldGenerator(initWorldGen, "standard");
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		wgenBlockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: false);
	}

	public void initWorldGen()
	{
		LoadGlobalConfig(api);
		decorBlocks = new Block[4]
		{
			api.World.GetBlock(new AssetLocation("hotspringbacteria-87deg")),
			api.World.GetBlock(new AssetLocation("hotspringbacteriasmooth-74deg")),
			api.World.GetBlock(new AssetLocation("hotspringbacteriasmooth-65deg")),
			api.World.GetBlock(new AssetLocation("hotspringbacteriasmooth-55deg"))
		};
		blocksludgygravel = api.World.GetBlock(new AssetLocation("sludgygravel"));
		boilingWaterBlockId = api.World.GetBlock(new AssetLocation("boilingwater-still-7")).Id;
	}

	private void GenChunkColumn(IChunkColumnGenerateRequest request)
	{
		Dictionary<Vec3i, HotSpringGenData> moddata = request.Chunks[0].MapChunk.GetModdata<Dictionary<Vec3i, HotSpringGenData>>("hotspringlocations");
		if (moddata == null || GetIntersectingStructure(request.ChunkX * 32 + 16, request.ChunkZ * 32 + 16, ModStdWorldGen.SkipHotSpringsgHashCode) != null)
		{
			return;
		}
		int baseX = request.ChunkX * 32;
		int baseZ = request.ChunkZ * 32;
		foreach (KeyValuePair<Vec3i, HotSpringGenData> item in moddata)
		{
			Vec3i key = item.Key;
			HotSpringGenData value = item.Value;
			genHotspring(baseX, baseZ, key, value);
		}
	}

	private void genHotspring(int baseX, int baseZ, Vec3i centerPos, HotSpringGenData gendata)
	{
		double num = 2.0 * gendata.horRadius;
		int num2 = (int)GameMath.Clamp((double)centerPos.X - num, -32.0, 63.0);
		int num3 = (int)GameMath.Clamp((double)centerPos.X + num + 1.0, -32.0, 63.0);
		int num4 = (int)GameMath.Clamp((double)centerPos.Z - num, -32.0, 63.0);
		int num5 = (int)GameMath.Clamp((double)centerPos.Z + num + 1.0, -32.0, 63.0);
		double num6 = num * num;
		int num7 = 99999;
		int num8 = 0;
		int num9 = 0;
		long num10 = 0L;
		bool flag = false;
		for (int i = num2; i <= num3; i++)
		{
			double num11 = (double)((i - centerPos.X) * (i - centerPos.X)) / num6;
			for (int j = num4; j <= num5; j++)
			{
				double num12 = (double)((j - centerPos.Z) * (j - centerPos.Z)) / num6;
				if (num11 + num12 < 1.0)
				{
					IMapChunk mapChunk = wgenBlockAccessor.GetMapChunk((baseX + i) / 32, (baseZ + j) / 32);
					if (mapChunk == null)
					{
						return;
					}
					int num13 = mapChunk.WorldGenTerrainHeightMap[GameMath.Mod(j, 32) * 32 + GameMath.Mod(i, 32)];
					num7 = Math.Min(num7, num13);
					num8 = Math.Max(num8, num13);
					num9++;
					num10 += num13;
					Block blockRaw = wgenBlockAccessor.GetBlockRaw(baseX + i, num13 + 1, baseZ + j, 2);
					flag |= blockRaw.Id != 0 && blockRaw.LiquidCode != "boilingwater";
				}
			}
		}
		int posy = (int)Math.Round((double)num10 / (double)num9);
		int num14 = num8 - num7;
		if (flag || num14 >= 4 || num7 < api.World.SeaLevel + 1 || (float)num7 > (float)api.WorldManager.MapSizeY * 0.88f)
		{
			return;
		}
		gendata.horRadius = Math.Min(32.0, gendata.horRadius);
		for (int k = num2; k <= num3; k++)
		{
			double num11 = (double)((k - centerPos.X) * (k - centerPos.X)) / num6;
			for (int l = num4; l <= num5; l++)
			{
				double num12 = (double)((l - centerPos.Z) * (l - centerPos.Z)) / num6;
				double num15 = num11 + num12;
				if (num15 < 1.0)
				{
					genhotSpringColumn(baseX + k, posy, baseZ + l, num15);
				}
			}
		}
	}

	private void genhotSpringColumn(int posx, int posy, int posz, double xzdist)
	{
		IMapChunk mapChunk = wgenBlockAccessor.GetChunkAtBlockPos(posx, posy, posz)?.MapChunk;
		if (mapChunk == null)
		{
			return;
		}
		int num = posx % 32;
		int num2 = posz % 32;
		int surfaceY = mapChunk.WorldGenTerrainHeightMap[num2 * 32 + num];
		xzdist += (api.World.Rand.NextDouble() / 6.0 - 1.0 / 12.0) * 0.5;
		BlockPos blockPos = new BlockPos(posx, posy, posz);
		Block block = wgenBlockAccessor.GetBlock(blockPos, 2);
		Block decor = wgenBlockAccessor.GetDecor(blockPos, new DecorBits(BlockFacing.UP));
		int num3 = (int)Math.Max(1.0, xzdist * 10.0);
		Block block2 = ((num3 < decorBlocks.Length) ? decorBlocks[num3] : null);
		for (int i = 0; i < Math.Min(decorBlocks.Length - 1, num3); i++)
		{
			if (decorBlocks[i] == decor)
			{
				block2 = decorBlocks[i];
				break;
			}
		}
		if (block.Id != 0)
		{
			return;
		}
		bool flag = false;
		if (api.World.Rand.NextDouble() > xzdist - 0.4)
		{
			prepareHotSpringBase(posx, posy, posz, surfaceY, preventLiquidSpill: true, block2);
			wgenBlockAccessor.SetBlock(blocksludgygravel.Id, blockPos);
			flag = true;
		}
		if (xzdist < 0.1)
		{
			prepareHotSpringBase(posx, posy, posz, surfaceY, preventLiquidSpill: false);
			wgenBlockAccessor.SetBlock(0, blockPos, 1);
			wgenBlockAccessor.SetBlock(boilingWaterBlockId, blockPos);
			wgenBlockAccessor.SetDecor(decorBlocks[0], blockPos.DownCopy(), BlockFacing.UP);
		}
		else if (block2 != null)
		{
			prepareHotSpringBase(posx, posy, posz, surfaceY, preventLiquidSpill: true, block2);
			Block blockAbove = wgenBlockAccessor.GetBlockAbove(blockPos, 1, 1);
			if (wgenBlockAccessor.GetBlockAbove(blockPos, 2, 1).SideSolid[BlockFacing.UP.Index])
			{
				blockPos.Y += 2;
			}
			else if (blockAbove.SideSolid[BlockFacing.UP.Index])
			{
				blockPos.Y++;
			}
			wgenBlockAccessor.SetDecor(block2, blockPos, BlockFacing.UP);
		}
		else if (xzdist < 0.8 && !flag)
		{
			prepareHotSpringBase(posx, posy, posz, surfaceY, preventLiquidSpill: true, block2);
		}
	}

	private void prepareHotSpringBase(int posx, int posy, int posz, int surfaceY, bool preventLiquidSpill = true, Block sideDecorBlock = null)
	{
		BlockPos blockPos = new BlockPos(posx, posy, posz);
		for (int i = posy + 1; i <= surfaceY + 1; i++)
		{
			blockPos.Y = i;
			Block block = wgenBlockAccessor.GetBlock(blockPos);
			Block block2 = wgenBlockAccessor.GetBlock(blockPos, 2);
			if (preventLiquidSpill && (block == blocksludgygravel || block2.Id == boilingWaterBlockId))
			{
				break;
			}
			wgenBlockAccessor.SetBlock(0, blockPos, 1);
			wgenBlockAccessor.SetBlock(0, blockPos, 2);
			wgenBlockAccessor.SetDecor(api.World.Blocks[0], blockPos, BlockFacing.UP);
			for (int j = 0; j < Cardinal.ALL.Length; j++)
			{
				Cardinal cardinal = Cardinal.ALL[j];
				BlockPos blockPos2 = new BlockPos(blockPos.X + cardinal.Normali.X, blockPos.Y, blockPos.Z + cardinal.Normali.Z);
				if (wgenBlockAccessor.GetBlock(blockPos2, 2).Id != 0)
				{
					wgenBlockAccessor.SetDecor(api.World.Blocks[0], blockPos2.DownCopy(), BlockFacing.UP);
					wgenBlockAccessor.SetBlock(blocksludgygravel.Id, blockPos2, 1);
					if (sideDecorBlock != null)
					{
						wgenBlockAccessor.SetDecor(sideDecorBlock, blockPos2, BlockFacing.UP);
					}
				}
			}
		}
		int num = posx % 32;
		int num2 = posz % 32;
		IMapChunk mapChunk = wgenBlockAccessor.GetMapChunk(posx / 32, posz / 32);
		mapChunk.RainHeightMap[num2 * 32 + num] = (ushort)posy;
		mapChunk.WorldGenTerrainHeightMap[num2 * 32 + num] = (ushort)posy;
		_ = mapChunk.TopRockIdMap[num2 * 32 + num];
		for (int num3 = posy; num3 >= posy - 2; num3--)
		{
			blockPos.Y = num3;
			wgenBlockAccessor.SetDecor(api.World.Blocks[0], blockPos, BlockFacing.UP);
			wgenBlockAccessor.SetBlock(blocksludgygravel.Id, blockPos);
		}
	}
}
