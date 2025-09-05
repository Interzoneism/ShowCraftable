using System;
using System.Diagnostics;
using System.Threading;

namespace Vintagestory.Server;

internal class SpawnerOffthread
{
	private readonly ServerSystemEntitySpawner entitySpawner;

	public SpawnerOffthread(ServerSystemEntitySpawner serverSystem)
	{
		entitySpawner = serverSystem;
	}

	internal void Start()
	{
		long num = 0L;
		Stopwatch stopwatch = new Stopwatch();
		while (!entitySpawner.ShouldExit())
		{
			Thread.Sleep(Math.Max(0, 500 - (int)num));
			if (!entitySpawner.ShouldExit())
			{
				if (entitySpawner.paused)
				{
					num = 0L;
					continue;
				}
				stopwatch.Reset();
				stopwatch.Start();
				entitySpawner.FindMobSpawnPositions_offthread(0.5f);
				stopwatch.Stop();
				num = stopwatch.ElapsedMilliseconds;
				continue;
			}
			break;
		}
	}
}
