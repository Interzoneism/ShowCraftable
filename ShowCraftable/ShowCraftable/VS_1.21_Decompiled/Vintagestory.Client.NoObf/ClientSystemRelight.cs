using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ClientSystemRelight : ClientSystem
{
	internal ChunkIlluminator chunkIlluminator;

	public override string Name => "relight";

	public ClientSystemRelight(ClientMain game)
		: base(game)
	{
		chunkIlluminator = new ChunkIlluminator(game.WorldMap, new BlockAccessorRelaxed(game.WorldMap, game, synchronize: false, relight: false), game.WorldMap.ClientChunkSize);
	}

	public override void OnBlockTexturesLoaded()
	{
		chunkIlluminator.InitForWorld(game.Blocks, (ushort)game.WorldMap.SunBrightness, game.WorldMap.MapSizeX, game.WorldMap.MapSizeY, game.WorldMap.MapSizeZ);
	}

	public override int SeperateThreadTickIntervalMs()
	{
		return 10;
	}

	public override void OnSeperateThreadGameTick(float dt)
	{
		ProcessLightingQueue();
	}

	public void ProcessLightingQueue()
	{
		EntityPos playerPos = game.player?.Entity?.Pos;
		while (game.WorldMap.LightingTasks.Count > 0)
		{
			UpdateLightingTask updateLightingTask = null;
			lock (game.WorldMap.LightingTasksLock)
			{
				updateLightingTask = game.WorldMap.LightingTasks.Dequeue();
			}
			if (updateLightingTask == null)
			{
				break;
			}
			ProcessLightingTask(playerPos, updateLightingTask);
		}
	}

	private void ProcessLightingTask(EntityPos playerPos, UpdateLightingTask task)
	{
		int num = 32;
		int x = task.pos.X;
		int internalY = task.pos.InternalY;
		int z = task.pos.Z;
		bool priority = playerPos != null && playerPos.SquareDistanceTo(x, internalY, z) < 2304f;
		int num2 = 0;
		int num3 = 0;
		bool flag = false;
		HashSet<long> hashSet = new HashSet<long>();
		hashSet.Add(chunkIlluminator.GetChunkIndexForPos(x, internalY, z));
		if (task.absorbUpdate)
		{
			num2 = task.oldAbsorb;
			num3 = task.newAbsorb;
		}
		else if (task.removeLightHsv != null)
		{
			flag = true;
			hashSet.AddRange(chunkIlluminator.RemoveBlockLight(task.removeLightHsv, x, internalY, z));
		}
		else
		{
			Block block = game.Blocks[task.oldBlockId];
			Block block2 = game.Blocks[task.newBlockId];
			byte[] lightHsv = block.GetLightHsv(game.BlockAccessor, task.pos);
			byte[] lightHsv2 = block2.GetLightHsv(game.BlockAccessor, task.pos);
			if (lightHsv[2] > 0)
			{
				flag = true;
				hashSet.AddRange(chunkIlluminator.RemoveBlockLight(lightHsv, x, internalY, z));
			}
			if (lightHsv2[2] > 0)
			{
				flag = true;
				hashSet.AddRange(chunkIlluminator.PlaceBlockLight(lightHsv2, x, internalY, z));
			}
			num2 = block.GetLightAbsorption(game.BlockAccessor, task.pos);
			num3 = block2.GetLightAbsorption(game.BlockAccessor, task.pos);
			if (lightHsv[2] == 0 && lightHsv2[2] == 0 && num2 != num3)
			{
				hashSet.AddRange(chunkIlluminator.UpdateBlockLight(num2, num3, x, internalY, z));
			}
		}
		bool flag2 = num2 != num3;
		if (flag2)
		{
			hashSet.AddRange(chunkIlluminator.UpdateSunLight(x, internalY, z, num2, num3));
		}
		foreach (long item in hashSet)
		{
			game.WorldMap.SetChunkDirty(item, priority);
		}
		if (!(flag2 || flag))
		{
			return;
		}
		long num4 = game.WorldMap.ChunkIndex3D(x / num, internalY / num, z / num);
		if (!hashSet.Contains(num4))
		{
			game.WorldMap.SetChunkDirty(num4, priority);
		}
		for (int i = -1; i < 2; i++)
		{
			for (int j = -1; j < 2; j++)
			{
				for (int k = -1; k < 2; k++)
				{
					if (k != 0 || j != 0 || i != 0)
					{
						long num5 = game.WorldMap.ChunkIndex3D((x + i) / num, (internalY + j) / num, (z + k) / num);
						if (num5 != num4 && !hashSet.Contains(num5))
						{
							game.WorldMap.SetChunkDirty(num5, priority, relight: false, edgeOnly: true);
						}
					}
				}
			}
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
