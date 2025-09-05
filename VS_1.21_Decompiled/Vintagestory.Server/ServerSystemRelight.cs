using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ServerSystemRelight : ServerSystem
{
	public ChunkIlluminator chunkIlluminator;

	public ServerSystemRelight(ServerMain server)
		: base(server)
	{
	}

	public override void OnBeginGameReady(SaveGame savegame)
	{
		chunkIlluminator = new ChunkIlluminator(server.WorldMap, new BlockAccessorRelaxed(server.WorldMap, server, synchronize: false, relight: false), MagicNum.ServerChunkSize);
		chunkIlluminator.InitForWorld(server.Blocks, (ushort)server.sunBrightness, server.WorldMap.MapSizeX, server.WorldMap.MapSizeY, server.WorldMap.MapSizeZ);
	}

	public override void OnSeparateThreadTick()
	{
		ProcessLightingQueue();
	}

	public override int GetUpdateInterval()
	{
		return 10;
	}

	public void ProcessLightingQueue()
	{
		while (server.WorldMap.LightingTasks.Count > 0)
		{
			UpdateLightingTask updateLightingTask = null;
			lock (server.WorldMap.LightingTasksLock)
			{
				updateLightingTask = server.WorldMap.LightingTasks.Dequeue();
			}
			if (updateLightingTask == null)
			{
				break;
			}
			if (server.WorldMap.IsValidPos(updateLightingTask.pos))
			{
				ProcessLightingTask(updateLightingTask, updateLightingTask.pos);
				if (server.Suspended)
				{
					break;
				}
			}
		}
	}

	public void ProcessLightingTask(UpdateLightingTask task, BlockPos pos)
	{
		int num = 0;
		int num2 = 0;
		bool flag = false;
		int x = task.pos.X;
		int internalY = task.pos.InternalY;
		int z = task.pos.Z;
		HashSet<long> hashSet = new HashSet<long>();
		if (task.absorbUpdate)
		{
			num = task.oldAbsorb;
			num2 = task.newAbsorb;
		}
		else if (task.removeLightHsv != null)
		{
			flag = true;
			hashSet.AddRange(chunkIlluminator.RemoveBlockLight(task.removeLightHsv, x, internalY, z));
		}
		else
		{
			int oldBlockId = task.oldBlockId;
			int newBlockId = task.newBlockId;
			Block block = server.Blocks[oldBlockId];
			Block block2 = server.Blocks[newBlockId];
			byte[] lightHsv = block.GetLightHsv(server.BlockAccessor, pos);
			byte[] lightHsv2 = block2.GetLightHsv(server.BlockAccessor, pos);
			if (lightHsv[2] > 0)
			{
				flag = true;
				hashSet.AddRange(chunkIlluminator.RemoveBlockLight(lightHsv, pos.X, pos.InternalY, pos.Z));
			}
			if (lightHsv2[2] > 0)
			{
				flag = true;
				hashSet.AddRange(chunkIlluminator.PlaceBlockLight(lightHsv2, pos.X, pos.InternalY, pos.Z));
			}
			num = block.GetLightAbsorption(server.BlockAccessor, pos);
			num2 = block2.GetLightAbsorption(server.BlockAccessor, pos);
			if (lightHsv[2] == 0 && lightHsv2[2] == 0 && num != num2)
			{
				hashSet.AddRange(chunkIlluminator.UpdateBlockLight(num, num2, pos.X, pos.InternalY, pos.Z));
			}
			server.WorldMap.MarkChunksDirty(pos, GameMath.Max(1, lightHsv2[2], lightHsv[2]));
		}
		bool flag2 = num2 != num;
		if (flag2 || flag)
		{
			for (int i = 0; i < 6; i++)
			{
				Vec3i vec3i = BlockFacing.ALLNORMALI[i];
				long item = server.WorldMap.ChunkIndex3D((pos.X + vec3i.X) / 32, (pos.InternalY + vec3i.Y) / 32, (pos.Z + vec3i.Z) / 32);
				hashSet.Add(item);
			}
		}
		if (flag2)
		{
			hashSet.AddRange(chunkIlluminator.UpdateSunLight(pos.X, pos.InternalY, pos.Z, num, num2));
		}
		foreach (long item2 in hashSet)
		{
			server.WorldMap.GetServerChunk(item2)?.MarkModified();
		}
	}
}
