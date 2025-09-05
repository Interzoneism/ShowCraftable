using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenTerraPostProcess : ModStdWorldGen
{
	private ICoreServerAPI api;

	private IWorldGenBlockAccessor blockAccessor;

	private HashSet<int> chunkVisitedNodes = new HashSet<int>();

	private List<int> solidNodes = new List<int>(40);

	private QueueOfInt bfsQueue = new QueueOfInt();

	private const int ARRAYSIZE = 41;

	private readonly int[] currentVisited = new int[68921];

	private int iteration;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.01;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.TerrainFeatures, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		blockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: true);
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		blockAccessor.BeginColumn();
		int num = TerraGenConfig.seaLevel - 1;
		int num2 = num / 32;
		int yMax = chunks[0].MapChunk.YMax;
		int num3 = Math.Min(yMax / 32 + 1, api.World.BlockAccessor.MapSizeY / 32);
		chunkVisitedNodes.Clear();
		for (int i = num2; i < num3; i++)
		{
			IChunkBlocks data = chunks[i].Data;
			int num4 = ((i == 0) ? 1 : 0);
			int num5 = i * 32;
			if (num5 < num)
			{
				num4 = num - num5;
			}
			int num6 = 31;
			if (num5 + num6 > yMax)
			{
				num6 = yMax - num5;
			}
			for (int j = 0; j < 1024; j++)
			{
				int num7 = j + (num4 - 1) * 1024;
				int num8 = ((num4 != 0) ? data.GetBlockIdUnsafe(num7) : chunks[i - 1].Data.GetBlockIdUnsafe(num7 + 32768));
				for (int k = num4; k <= num6; k++)
				{
					num7 += 1024;
					int blockIdUnsafe = data.GetBlockIdUnsafe(num7);
					if (blockIdUnsafe != 0 && num8 == 0)
					{
						int num9 = j % 32;
						int num10 = j / 32;
						if (!chunkVisitedNodes.Contains(num7))
						{
							deletePotentialFloatingBlocks(chunkX * 32 + num9, num5 + k, chunkZ * 32 + num10);
						}
					}
					num8 = blockIdUnsafe;
				}
			}
		}
	}

	private void deletePotentialFloatingBlocks(int X, int Y, int Z)
	{
		int num = 20;
		solidNodes.Clear();
		bfsQueue.Clear();
		int num2 = (num << 12) | (num << 6) | num;
		bfsQueue.Enqueue(num2);
		solidNodes.Add(num2);
		int num3 = ++iteration;
		int num4 = (num * 41 + num) * 41 + num;
		currentVisited[num4] = num3;
		int num5 = X - num;
		int num6 = Y - num;
		int num7 = Z - num;
		BlockPos blockPos = new BlockPos();
		int mapSizeY = api.World.BlockAccessor.MapSizeY;
		int num8 = 1;
		while (bfsQueue.Count > 0)
		{
			num2 = bfsQueue.Dequeue();
			int num9 = num2 >> 12;
			int num10 = (num2 >> 6) & 0x3F;
			int num11 = num2 & 0x3F;
			blockPos.Set(num5 + num9, num6 + num10, num7 + num11);
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			for (int i = 0; i < aLLFACES.Length; i++)
			{
				aLLFACES[i].IterateThruFacingOffsets(blockPos);
				if (blockPos.Y >= mapSizeY)
				{
					continue;
				}
				num9 = blockPos.X - num5;
				num10 = blockPos.Y - num6;
				num11 = blockPos.Z - num7;
				num4 = (num9 * 41 + num10) * 41 + num11;
				if (currentVisited[num4] == num3)
				{
					continue;
				}
				currentVisited[num4] = num3;
				if (blockAccessor.GetBlockId(blockPos.X, blockPos.Y, blockPos.Z) == 0)
				{
					continue;
				}
				int num12 = (num9 << 12) | (num10 << 6) | num11;
				if (++num8 > 40)
				{
					if (!solidNodes.Contains(num12 - 64))
					{
						AddToChunkVisitedNodesIfSameChunk(blockPos.X, blockPos.Y, blockPos.Z, X, Y, Z);
					}
					{
						foreach (int solidNode in solidNodes)
						{
							if (!solidNodes.Contains(solidNode - 64))
							{
								num9 = solidNode >> 12;
								num10 = (solidNode >> 6) & 0x3F;
								num11 = solidNode & 0x3F;
								AddToChunkVisitedNodesIfSameChunk(num5 + num9, num6 + num10, num7 + num11, X, Y, Z);
							}
						}
						return;
					}
				}
				solidNodes.Add(num12);
				bfsQueue.Enqueue(num12);
			}
		}
		foreach (int solidNode2 in solidNodes)
		{
			int num9 = solidNode2 >> 12;
			int num10 = (solidNode2 >> 6) & 0x3F;
			int num11 = solidNode2 & 0x3F;
			blockPos.Set(num5 + num9, num6 + num10, num7 + num11);
			blockAccessor.SetBlock(0, blockPos);
		}
	}

	private void AddToChunkVisitedNodesIfSameChunk(int nposX, int nposY, int nposZ, int origX, int origY, int origZ)
	{
		if (nposY >= origY && (nposY != origY || (nposZ >= origZ && (nposZ != origZ || nposX >= origX))) && ((nposX ^ origX) & -32) == 0 && ((nposZ ^ origZ) & -32) == 0 && ((nposY ^ origY) & -32) == 0)
		{
			int item = ((nposY & 0x1F) * 32 + (nposZ & 0x1F)) * 32 + (nposX & 0x1F);
			chunkVisitedNodes.Add(item);
		}
	}
}
