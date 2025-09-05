using System;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class BlockAccessorWorldGenUpdateHeightmap : BlockAccessorWorldGen
{
	public BlockAccessorWorldGenUpdateHeightmap(ServerMain server, ChunkServerThread chunkdbthread)
		: base(server, chunkdbthread)
	{
	}

	public override void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack = null)
	{
		ServerChunk serverChunk = chunkdbthread.GetGeneratingChunkAtPos(pos);
		if (serverChunk == null)
		{
			serverChunk = server.WorldMap.GetChunkAtPos(pos.X, pos.Y, pos.Z) as ServerChunk;
		}
		if (serverChunk != null)
		{
			Block block = worldAccessor.GetBlock(blockId);
			int num = SetSolidBlock(serverChunk, pos, block, blockId);
			if (block.LightHsv[2] > 0)
			{
				(chunkdbthread.worldgenBlockAccessor as IWorldGenBlockAccessor).ScheduleBlockLightUpdate(pos, num, blockId);
			}
			bool rainPermeable = worldmap.Blocks[num].RainPermeable;
			bool rainPermeable2 = block.RainPermeable;
			int num2 = serverChunk.MapChunk.YMax;
			if (blockId != 0)
			{
				num2 = Math.Max(pos.Y, num2);
			}
			int num3 = (pos.X & MagicNum.ServerChunkSizeMask) + 32 * (pos.Z & MagicNum.ServerChunkSizeMask);
			if (rainPermeable && !rainPermeable2)
			{
				serverChunk.MapChunk.RainHeightMap[num3] = Math.Max(serverChunk.MapChunk.RainHeightMap[num3], (ushort)pos.Y);
			}
			if (!rainPermeable && rainPermeable2 && serverChunk.MapChunk.RainHeightMap[num3] == pos.Y)
			{
				BlockPos blockPos = pos.DownCopy();
				while (worldmap.Blocks[GetBlockId(blockPos, 3)].RainPermeable && blockPos.Y > 0)
				{
					blockPos.Down();
				}
				serverChunk.MapChunk.RainHeightMap[num3] = (ushort)blockPos.Y;
			}
			if (serverChunk.serverMapChunk.CurrentIncompletePass < EnumWorldGenPass.Vegetation)
			{
				bool num4 = worldmap.Blocks[num].SideSolid[BlockFacing.UP.Index];
				bool flag = block.SideSolid[BlockFacing.UP.Index];
				if (!num4 && flag)
				{
					serverChunk.MapChunk.WorldGenTerrainHeightMap[num3] = Math.Max(serverChunk.MapChunk.WorldGenTerrainHeightMap[num3], (ushort)pos.Y);
				}
				if (num4 && !flag && serverChunk.MapChunk.WorldGenTerrainHeightMap[num3] == pos.Y)
				{
					BlockPos blockPos2 = pos.DownCopy();
					while (worldmap.Blocks[GetBlockId(blockPos2, 3)].RainPermeable && blockPos2.Y > 0)
					{
						blockPos2.Down();
					}
					serverChunk.MapChunk.WorldGenTerrainHeightMap[num3] = (ushort)blockPos2.Y;
				}
			}
			serverChunk.MapChunk.YMax = (ushort)num2;
		}
		else if (RuntimeEnv.DebugOutOfRangeBlockAccess)
		{
			ServerMain.Logger.Notification("Tried to set block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5})", pos.X, pos.Y, pos.Z, pos.X / MagicNum.ServerChunkSize, pos.Y / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize);
			ServerMain.Logger.VerboseDebug(new StackTrace()?.ToString() ?? "");
		}
	}
}
