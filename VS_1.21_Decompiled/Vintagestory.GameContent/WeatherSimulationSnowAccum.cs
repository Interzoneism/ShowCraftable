using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class WeatherSimulationSnowAccum
{
	private int[][] randomShuffles;

	private ICoreServerAPI sapi;

	private WeatherSystemBase ws;

	private Thread snowLayerScannerThread;

	private bool isShuttingDown;

	private UniqueQueue<Vec2i> chunkColsstoCheckQueue = new UniqueQueue<Vec2i>();

	private UniqueQueue<UpdateSnowLayerChunk> updateSnowLayerQueue = new UniqueQueue<UpdateSnowLayerChunk>();

	private BlockPos tmpPos = new BlockPos(0);

	private const int chunksize = 32;

	private int regionsize;

	internal float accum;

	public bool ProcessChunks = true;

	public bool enabled;

	private IBulkBlockAccessor ba;

	private IBulkBlockAccessor cuba;

	private bool shouldPauseThread;

	private bool isThreadPaused;

	public WeatherSimulationSnowAccum(ICoreServerAPI sapi, WeatherSystemBase ws)
	{
		this.sapi = sapi;
		this.ws = ws;
		ba = sapi.World.GetBlockAccessorBulkMinimalUpdate(synchronize: true);
		ba.UpdateSnowAccumMap = false;
		cuba = sapi.World.GetBlockAccessorMapChunkLoading(synchronize: false);
		cuba.UpdateSnowAccumMap = false;
		initRandomShuffles();
		sapi.Event.BeginChunkColumnLoadChunkThread += Event_BeginChunkColLoadChunkThread;
		sapi.Event.ChunkColumnLoaded += Event_ChunkColumnLoaded;
		sapi.Event.SaveGameLoaded += Event_SaveGameLoaded;
		sapi.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, delegate
		{
			isShuttingDown = true;
		});
		sapi.Event.RegisterGameTickListener(OnServerTick3s, 3000);
		sapi.Event.RegisterGameTickListener(OnServerTick100ms, 100);
		sapi.Event.ServerSuspend += Event_ServerSuspend;
		sapi.Event.ServerResume += Event_ServerResume;
		snowLayerScannerThread = TyronThreadPool.CreateDedicatedThread(onThreadStart, "snowlayerScanner");
	}

	private void Event_ServerResume()
	{
		shouldPauseThread = false;
	}

	private EnumSuspendState Event_ServerSuspend()
	{
		shouldPauseThread = true;
		if (isThreadPaused || !enabled)
		{
			return EnumSuspendState.Ready;
		}
		return EnumSuspendState.Wait;
	}

	private void Event_SaveGameLoaded()
	{
		regionsize = sapi.WorldManager.RegionSize;
		if (regionsize == 0)
		{
			sapi.Logger.Notification("Warning: region size was 0 for Snow Accum system");
			regionsize = 16;
		}
		enabled = sapi.World.Config.GetBool("snowAccum", defaultValue: true);
		GlobalConstants.MeltingFreezingEnabled = enabled;
		if (enabled)
		{
			snowLayerScannerThread.Start();
		}
	}

	private void OnServerTick3s(float dt)
	{
		if (!ProcessChunks || !enabled)
		{
			return;
		}
		foreach (KeyValuePair<long, IMapChunk> allLoadedMapchunk in sapi.WorldManager.AllLoadedMapchunks)
		{
			Vec2i item = sapi.WorldManager.MapChunkPosFromChunkIndex2D(allLoadedMapchunk.Key);
			lock (chunkColsstoCheckQueue)
			{
				chunkColsstoCheckQueue.Enqueue(item);
			}
		}
	}

	public void AddToCheckQueue(Vec2i chunkCoord)
	{
		lock (chunkColsstoCheckQueue)
		{
			chunkColsstoCheckQueue.Enqueue(chunkCoord);
		}
	}

	private void OnServerTick100ms(float dt)
	{
		accum += dt;
		if (updateSnowLayerQueue.Count <= 5 && (!(accum > 1f) || updateSnowLayerQueue.Count <= 0))
		{
			return;
		}
		accum = 0f;
		int num = 0;
		int num2 = 10;
		UpdateSnowLayerChunk[] array = new UpdateSnowLayerChunk[num2];
		lock (updateSnowLayerQueue)
		{
			while (updateSnowLayerQueue.Count > 0)
			{
				array[num] = updateSnowLayerQueue.Dequeue();
				num++;
				if (num >= num2)
				{
					break;
				}
			}
		}
		for (int i = 0; i < num; i++)
		{
			IMapChunk mapChunk = sapi.WorldManager.GetMapChunk(array[i].Coords.X, array[i].Coords.Y);
			if (mapChunk != null)
			{
				processBlockUpdates(mapChunk, array[i], ba);
			}
		}
		ba.Commit();
	}

	internal void processBlockUpdates(IMapChunk mc, UpdateSnowLayerChunk updateChunk, IBulkBlockAccessor ba)
	{
		Dictionary<int, BlockIdAndSnowLevel> setBlocks = updateChunk.SetBlocks;
		double lastSnowAccumUpdateTotalHours = updateChunk.LastSnowAccumUpdateTotalHours;
		Vec2i vec2i = new Vec2i();
		int x = updateChunk.Coords.X;
		int y = updateChunk.Coords.Y;
		foreach (KeyValuePair<int, BlockIdAndSnowLevel> item in setBlocks)
		{
			Block block = item.Value.Block;
			float snowLevel = item.Value.SnowLevel;
			tmpPos.SetFromColumnIndex3d(item.Key, x, y);
			Block block2 = ba.GetBlock(tmpPos);
			vec2i.Set(tmpPos.X, tmpPos.Z);
			if (!(snowLevel > 0f) || mc.SnowAccum.ContainsKey(vec2i))
			{
				block2.PerformSnowLevelUpdate(ba, tmpPos, block, snowLevel);
			}
		}
		mc.SetModdata("lastSnowAccumUpdateTotalHours", SerializerUtil.Serialize(lastSnowAccumUpdateTotalHours));
		mc.MarkDirty();
	}

	private void Event_ChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
	{
		if (!ProcessChunks)
		{
			return;
		}
		int regionX = chunkCoord.X * 32 / regionsize;
		int regionZ = chunkCoord.Y * 32 / regionsize;
		WeatherSimulationRegion orCreateWeatherSimForRegion = ws.getOrCreateWeatherSimForRegion(regionX, regionZ);
		if (sapi.WorldManager.GetMapChunk(chunkCoord.X, chunkCoord.Y) == null || orCreateWeatherSimForRegion == null)
		{
			return;
		}
		lock (chunkColsstoCheckQueue)
		{
			chunkColsstoCheckQueue.Enqueue(chunkCoord);
		}
	}

	private void Event_BeginChunkColLoadChunkThread(IServerMapChunk mc, int chunkX, int chunkZ, IWorldChunk[] chunks)
	{
		if (ProcessChunks)
		{
			int regionX = chunkX * 32 / regionsize;
			int regionZ = chunkZ * 32 / regionsize;
			WeatherSimulationRegion orCreateWeatherSimForRegion = ws.getOrCreateWeatherSimForRegion(regionX, regionZ);
			if (orCreateWeatherSimForRegion != null)
			{
				TryImmediateSnowUpdate(orCreateWeatherSimForRegion, mc, new Vec2i(chunkX, chunkZ), chunks);
			}
		}
	}

	private bool TryImmediateSnowUpdate(WeatherSimulationRegion simregion, IServerMapChunk mc, Vec2i chunkCoord, IWorldChunk[] chunksCol)
	{
		UpdateSnowLayerChunk item = new UpdateSnowLayerChunk
		{
			Coords = chunkCoord
		};
		lock (updateSnowLayerQueue)
		{
			if (updateSnowLayerQueue.Contains(item))
			{
				return false;
			}
		}
		if (ws.api.World.Calendar.TotalHours - simregion.LastUpdateTotalHours > 1.0)
		{
			return false;
		}
		UpdateSnowLayerChunk snowUpdate = GetSnowUpdate(simregion, mc, chunkCoord, chunksCol);
		if (snowUpdate == null)
		{
			return true;
		}
		if (snowUpdate.SetBlocks.Count == 0)
		{
			return true;
		}
		cuba.SetChunks(chunkCoord, chunksCol);
		processBlockUpdates(mc, snowUpdate, cuba);
		cuba.Commit();
		lock (updateSnowLayerQueue)
		{
			updateSnowLayerQueue.Enqueue(item);
		}
		return true;
	}

	private void onThreadStart()
	{
		FrameProfilerUtil frameProfilerUtil = new FrameProfilerUtil("[Thread snowaccum] ");
		while (!isShuttingDown)
		{
			Thread.Sleep(5);
			if (shouldPauseThread)
			{
				isThreadPaused = true;
				continue;
			}
			isThreadPaused = false;
			frameProfilerUtil.Begin(null);
			int num = 0;
			while (chunkColsstoCheckQueue.Count > 0 && num++ < 10)
			{
				Vec2i vec2i;
				lock (chunkColsstoCheckQueue)
				{
					vec2i = chunkColsstoCheckQueue.Dequeue();
				}
				int regionX = vec2i.X * 32 / regionsize;
				int regionZ = vec2i.Y * 32 / regionsize;
				WeatherSimulationRegion orCreateWeatherSimForRegion = ws.getOrCreateWeatherSimForRegion(regionX, regionZ);
				IServerMapChunk mapChunk = sapi.WorldManager.GetMapChunk(vec2i.X, vec2i.Y);
				if (mapChunk != null && orCreateWeatherSimForRegion != null)
				{
					UpdateSnowLayerOffThread(orCreateWeatherSimForRegion, mapChunk, vec2i);
					frameProfilerUtil.Mark("update ", vec2i);
				}
			}
			frameProfilerUtil.OffThreadEnd();
		}
	}

	private void initRandomShuffles()
	{
		randomShuffles = new int[50][];
		for (int i = 0; i < randomShuffles.Length; i++)
		{
			int[] array = (randomShuffles[i] = new int[1024]);
			for (int j = 0; j < array.Length; j++)
			{
				array[j] = j;
			}
			GameMath.Shuffle(sapi.World.Rand, array);
		}
	}

	public void UpdateSnowLayerOffThread(WeatherSimulationRegion simregion, IServerMapChunk mc, Vec2i chunkPos)
	{
		UpdateSnowLayerChunk item = new UpdateSnowLayerChunk
		{
			Coords = chunkPos
		};
		lock (updateSnowLayerQueue)
		{
			if (updateSnowLayerQueue.Contains(item))
			{
				return;
			}
		}
		if (simregion == null || ws.api.World.Calendar.TotalHours - simregion.LastUpdateTotalHours > 1.0)
		{
			return;
		}
		item = GetSnowUpdate(simregion, mc, chunkPos, null);
		if (item == null)
		{
			return;
		}
		lock (updateSnowLayerQueue)
		{
			updateSnowLayerQueue.Enqueue(item);
		}
	}

	private UpdateSnowLayerChunk GetSnowUpdate(WeatherSimulationRegion simregion, IServerMapChunk mc, Vec2i chunkPos, IWorldChunk[] chunksCol)
	{
		double num = mc.GetModdata("lastSnowAccumUpdateTotalHours", 0.0);
		double num2 = num;
		int snowAccumResolution = WeatherSimulationRegion.snowAccumResolution;
		SnowAccumSnapshot snowAccumSnapshot = new SnowAccumSnapshot
		{
			SnowAccumulationByRegionCorner = new FloatDataMap3D(snowAccumResolution, snowAccumResolution, snowAccumResolution)
		};
		float[] data = snowAccumSnapshot.SnowAccumulationByRegionCorner.Data;
		float num3 = (float)ws.GeneralConfig.SnowLayerBlocks.Count + 0.6f;
		int length = simregion.SnowAccumSnapshots.Length;
		int num4 = simregion.SnowAccumSnapshots.EndPosition;
		int num5 = 0;
		lock (WeatherSimulationRegion.snowAccumSnapshotLock)
		{
			while (length-- > 0)
			{
				SnowAccumSnapshot snowAccumSnapshot2 = simregion.SnowAccumSnapshots[num4];
				num4 = (num4 + 1) % simregion.SnowAccumSnapshots.Length;
				if (snowAccumSnapshot2 != null && !(num >= snowAccumSnapshot2.TotalHours))
				{
					float[] data2 = snowAccumSnapshot2.SnowAccumulationByRegionCorner.Data;
					for (int i = 0; i < data2.Length; i++)
					{
						data[i] = GameMath.Clamp(data[i] + data2[i], 0f - num3, num3);
					}
					num = Math.Max(num, snowAccumSnapshot2.TotalHours);
					num5++;
				}
			}
		}
		if (num5 == 0)
		{
			return null;
		}
		bool ignoreOldAccum = false;
		if (num - num2 >= (double)((float)sapi.World.Calendar.DaysPerYear * sapi.World.Calendar.HoursPerDay))
		{
			ignoreOldAccum = true;
		}
		UpdateSnowLayerChunk updateSnowLayerChunk = UpdateSnowLayer(snowAccumSnapshot, ignoreOldAccum, mc, chunkPos, chunksCol);
		if (updateSnowLayerChunk != null)
		{
			updateSnowLayerChunk.LastSnowAccumUpdateTotalHours = num;
			updateSnowLayerChunk.Coords = chunkPos.Copy();
		}
		return updateSnowLayerChunk;
	}

	public UpdateSnowLayerChunk UpdateSnowLayer(SnowAccumSnapshot sumsnapshot, bool ignoreOldAccum, IServerMapChunk mc, Vec2i chunkPos, IWorldChunk[] chunksCol)
	{
		UpdateSnowLayerChunk updateSnowLayerChunk = new UpdateSnowLayerChunk();
		OrderedDictionary<Block, int> snowLayerBlocks = ws.GeneralConfig.SnowLayerBlocks;
		int x = chunkPos.X;
		int y = chunkPos.Y;
		int num = x * 32 / regionsize;
		int num2 = y * 32 / regionsize;
		int num3 = num * regionsize;
		int num4 = num2 * regionsize;
		int seaLevel = sapi.World.SeaLevel;
		int count = ws.GeneralConfig.SnowLayerBlocks.Count;
		BlockPos blockPos = new BlockPos(0);
		BlockPos blockPos2 = new BlockPos(0);
		float num5 = sapi.World.BlockAccessor.MapSizeY - seaLevel;
		int[] array = randomShuffles[sapi.World.Rand.Next(randomShuffles.Length)];
		int num6 = -99999;
		IWorldChunk worldChunk = null;
		int max = sapi.World.BlockAccessor.MapSizeY - 1;
		foreach (int num7 in array)
		{
			int num8 = GameMath.Clamp(mc.RainHeightMap[num7], 0, max);
			int num9 = num8 / 32;
			blockPos.Set(x * 32 + num7 % 32, num8, y * 32 + num7 / 32);
			if (num6 != num9)
			{
				worldChunk = ((chunksCol != null) ? chunksCol[num9] : null) ?? sapi.WorldManager.GetChunk(x, num9, y);
				num6 = num9;
			}
			if (worldChunk == null)
			{
				return null;
			}
			float x2 = (float)(blockPos.X - num3) / (float)regionsize;
			float y2 = GameMath.Clamp((float)(blockPos.Y - seaLevel) / num5, 0f, 1f);
			float z = (float)(blockPos.Z - num4) / (float)regionsize;
			Block block = worldChunk.GetLocalBlockAtBlockPos(sapi.World, blockPos);
			Block localBlockAtBlockPos = worldChunk.GetLocalBlockAtBlockPos(sapi.World, blockPos.X, blockPos.Y, blockPos.Z, 2);
			if (localBlockAtBlockPos.Id != 0)
			{
				if (!localBlockAtBlockPos.IsLiquid())
				{
					block = localBlockAtBlockPos;
				}
				else if (block.GetSnowLevel(blockPos) == 0f)
				{
					continue;
				}
			}
			float value = 0f;
			Vec2i key = new Vec2i(blockPos.X, blockPos.Z);
			if (!ignoreOldAccum && !mc.SnowAccum.TryGetValue(key, out value))
			{
				value = block.GetSnowLevel(blockPos);
			}
			float num10 = value + sumsnapshot.GetAvgSnowAccumByRegionCorner(x2, y2, z);
			mc.SnowAccum[key] = GameMath.Clamp(num10, -1f, (float)count + 0.6f);
			float num11 = num10 - (float)GameMath.MurmurHash3Mod(blockPos.X, 0, blockPos.Z, 150) / 300f;
			float num12 = GameMath.Clamp(num11 - 1.1f, -1f, count - 1);
			int num13 = ((num12 < 0f) ? (-1) : ((int)num12));
			blockPos2.Set(blockPos.X, Math.Min(blockPos.Y + 1, sapi.World.BlockAccessor.MapSizeY - 1), blockPos.Z);
			num9 = blockPos2.Y / 32;
			if (num6 != num9)
			{
				worldChunk = ((chunksCol != null) ? chunksCol[num9] : null) ?? sapi.WorldManager.GetChunk(x, num9, y);
				num6 = num9;
			}
			if (worldChunk == null)
			{
				return null;
			}
			Block block2 = worldChunk.GetLocalBlockAtBlockPos(sapi.World, blockPos2);
			Block localBlockAtBlockPos2 = worldChunk.GetLocalBlockAtBlockPos(sapi.World, blockPos2.X, blockPos2.Y, blockPos2.Z, 2);
			if (localBlockAtBlockPos2.Id != 0)
			{
				if (!localBlockAtBlockPos2.IsLiquid())
				{
					block2 = localBlockAtBlockPos2;
				}
				else if (block2.GetSnowLevel(blockPos) == 0f)
				{
					continue;
				}
			}
			blockPos2.Set(blockPos);
			Block snowCoveredVariant = block.GetSnowCoveredVariant(blockPos2, num11);
			if (snowCoveredVariant != null)
			{
				if (block.Id != snowCoveredVariant.Id && block2.Replaceable > 6000)
				{
					updateSnowLayerChunk.SetBlocks[blockPos2.ToColumnIndex3d()] = new BlockIdAndSnowLevel(snowCoveredVariant, num11);
				}
			}
			else
			{
				if (!block.AllowSnowCoverage(sapi.World, blockPos2))
				{
					continue;
				}
				blockPos2.Set(blockPos.X, blockPos.Y + 1, blockPos.Z);
				if (block2.Id != 0)
				{
					snowCoveredVariant = block2.GetSnowCoveredVariant(blockPos2, num11);
					if (snowCoveredVariant != null && block2.Id != snowCoveredVariant.Id)
					{
						updateSnowLayerChunk.SetBlocks[blockPos2.ToColumnIndex3d()] = new BlockIdAndSnowLevel(snowCoveredVariant, num11);
					}
				}
				else if (num13 >= 0)
				{
					Block keyAtIndex = snowLayerBlocks.GetKeyAtIndex(num13);
					updateSnowLayerChunk.SetBlocks[blockPos2.ToColumnIndex3d()] = new BlockIdAndSnowLevel(keyAtIndex, num11);
				}
			}
		}
		return updateSnowLayerChunk;
	}
}
