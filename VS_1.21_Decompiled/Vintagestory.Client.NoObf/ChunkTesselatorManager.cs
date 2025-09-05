using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ChunkTesselatorManager : ClientSystem
{
	internal int chunksize;

	private object tessChunksQueueLock = new object();

	private SortableQueue<TesselatedChunk> tessChunksQueue = new SortableQueue<TesselatedChunk>();

	private object tessChunksQueuePriorityLock = new object();

	private Queue<TesselatedChunk> tessChunksQueuePriority = new Queue<TesselatedChunk>();

	private Vec3i chunkPos = new Vec3i();

	private Vec3i tmpPos = new Vec3i();

	private int singleUploadDelayCounter;

	private bool processPrioQueue;

	public static long cumulativeTime;

	public static int cumulativeCount;

	public override string Name => "tete";

	public ChunkTesselatorManager(ClientMain game)
		: base(game)
	{
		chunksize = game.WorldMap.ClientChunkSize;
		game.eventManager.RegisterRenderer(OnBeforeFrame, EnumRenderStage.Before, "chtema", 0.99);
	}

	public override void Dispose(ClientMain game)
	{
		game.ShouldTesselateTerrain = false;
		lock (tessChunksQueueLock)
		{
			tessChunksQueue?.Clear();
			tessChunksQueue = null;
		}
		lock (tessChunksQueuePriorityLock)
		{
			tessChunksQueuePriority?.Clear();
			tessChunksQueuePriority = null;
		}
	}

	public override int SeperateThreadTickIntervalMs()
	{
		return 0;
	}

	public override void OnBlockTexturesLoaded()
	{
		game.TerrainChunkTesselator.BlockTexturesLoaded();
	}

	public void OnBeforeFrame(float dt)
	{
		RuntimeStats.chunksAwaitingTesselation = game.dirtyChunksPriority.Count + game.dirtyChunks.Count + game.dirtyChunksLast.Count;
		RuntimeStats.chunksAwaitingPooling = tessChunksQueuePriority.Count + tessChunksQueue.Count;
		int num = game.frustumCuller.ViewDistanceSq / 48 + 350;
		int num2 = 0;
		if (processPrioQueue)
		{
			lock (tessChunksQueuePriorityLock)
			{
				while (tessChunksQueuePriority.Count > 0)
				{
					TesselatedChunk tesselatedChunk = tessChunksQueuePriority.Dequeue();
					tesselatedChunk.chunk.queuedForUpload = false;
					ClientChunk chunkAtBlockPos = game.WorldMap.GetChunkAtBlockPos(tesselatedChunk.positionX, tesselatedChunk.positionYAndDimension, tesselatedChunk.positionZ);
					if (chunkAtBlockPos != null)
					{
						game.chunkRenderer.AddTesselatedChunk(tesselatedChunk, chunkAtBlockPos);
						singleUploadDelayCounter = 10;
						num2 += tesselatedChunk.VerticesCount;
						tmpPos.Set(tesselatedChunk.positionX / 32, tesselatedChunk.positionYAndDimension / 32, tesselatedChunk.positionZ / 32);
						game.eventManager?.TriggerChunkRetesselated(tmpPos, chunkAtBlockPos);
					}
					else
					{
						tesselatedChunk.UnusedDispose();
					}
				}
			}
			processPrioQueue = false;
		}
		int count = tessChunksQueue.Count;
		int num3 = num * (3 + count / (1 << ClientSettings.ChunkVerticesUploadRateLimiter));
		if (num2 >= num3 || (count < 2 && (count == 0 || singleUploadDelayCounter++ < 10)))
		{
			return;
		}
		singleUploadDelayCounter = 0;
		lock (tessChunksQueueLock)
		{
			tessChunksQueue.RunForEach(delegate(TesselatedChunk eachTC)
			{
				eachTC.RecalcPriority(game.player);
			});
			tessChunksQueue.Sort();
			while (tessChunksQueue.Count > 0 && num2 < num3)
			{
				TesselatedChunk tesselatedChunk = tessChunksQueue.Dequeue();
				tesselatedChunk.chunk.queuedForUpload = false;
				ClientChunk chunkAtBlockPos2 = game.WorldMap.GetChunkAtBlockPos(tesselatedChunk.positionX, tesselatedChunk.positionYAndDimension, tesselatedChunk.positionZ);
				if (chunkAtBlockPos2 != null)
				{
					game.chunkRenderer.AddTesselatedChunk(tesselatedChunk, chunkAtBlockPos2);
					num2 += tesselatedChunk.VerticesCount;
					tmpPos.Set(tesselatedChunk.positionX / 32, tesselatedChunk.positionYAndDimension / 32, tesselatedChunk.positionZ / 32);
					game.eventManager?.TriggerChunkRetesselated(tmpPos, chunkAtBlockPos2);
				}
				else
				{
					tesselatedChunk.UnusedDispose();
				}
			}
		}
	}

	public override void OnSeperateThreadGameTick(float dt)
	{
		if (!game.TerrainChunkTesselator.started)
		{
			return;
		}
		MeshDataRecycler recycler = MeshData.Recycler;
		if (!game.ShouldTesselateTerrain)
		{
			recycler?.DoRecycling();
			return;
		}
		long num = 0L;
		int count = game.dirtyChunksPriority.Count;
		while (count-- > 0)
		{
			lock (game.dirtyChunksPriorityLock)
			{
				num = game.dirtyChunksPriority.Dequeue();
			}
			long num2 = num;
			if (num < 0)
			{
				num2 = num & 0x7FFFFFFFFFFFFFFFL;
				if (game.dirtyChunksPriority.Contains(num2))
				{
					continue;
				}
			}
			MapUtil.PosInt3d(num2, game.WorldMap.index3dMulX, game.WorldMap.index3dMulZ, chunkPos);
			if (!game.ShouldTesselateTerrain)
			{
				break;
			}
			TesselateChunk(chunkPos.X, chunkPos.Y, chunkPos.Z, priority: true, num < 0, out var requeue);
			if (requeue)
			{
				lock (game.dirtyChunksPriorityLock)
				{
					game.dirtyChunksPriority.Enqueue(num);
				}
			}
		}
		int num3 = (game.frustumCuller.ViewDistanceSq + 16800) * 3 / 2;
		int num4 = 0;
		count = game.dirtyChunks.Count;
		while (count-- > 0 && num4 < num3)
		{
			lock (game.dirtyChunksLock)
			{
				if (game.dirtyChunks.Count <= 0)
				{
					break;
				}
				num = game.dirtyChunks.Dequeue();
				goto IL_01e4;
			}
			IL_01e4:
			long num5 = num;
			if (num < 0)
			{
				num5 = num & 0x7FFFFFFFFFFFFFFFL;
				if (game.dirtyChunks.Contains(num5))
				{
					continue;
				}
			}
			if (!game.ShouldTesselateTerrain)
			{
				break;
			}
			MapUtil.PosInt3d(num5, game.WorldMap.index3dMulX, game.WorldMap.index3dMulZ, chunkPos);
			num4 += TesselateChunk(chunkPos.X, chunkPos.Y, chunkPos.Z, priority: false, num < 0, out var requeue2);
			if (requeue2)
			{
				lock (game.dirtyChunksLock)
				{
					game.dirtyChunks.Enqueue(num);
				}
			}
		}
		int num6 = 5;
		while (game.dirtyChunksLast.Count > 0 && num6-- > 0)
		{
			lock (game.dirtyChunksLastLock)
			{
				num = game.dirtyChunksLast.Dequeue();
			}
			MapUtil.PosInt3d(num & 0x7FFFFFFFFFFFFFFFL, game.WorldMap.index3dMulX, game.WorldMap.index3dMulZ, chunkPos);
			if (!game.ShouldTesselateTerrain)
			{
				break;
			}
			TesselateChunk(chunkPos.X, chunkPos.Y, chunkPos.Z, priority: false, num < 0, out var requeue3);
			if (requeue3)
			{
				lock (game.dirtyChunksLastLock)
				{
					game.dirtyChunksLast.Enqueue(num);
				}
			}
		}
		recycler?.DoRecycling();
	}

	public int TesselateChunk(int chunkX, int chunkY, int chunkZ, bool priority, bool skipChunkCenter, out bool requeue)
	{
		requeue = false;
		ClientChunk clientChunk = game.WorldMap.GetClientChunk(chunkX, chunkY, chunkZ);
		if (clientChunk == null || clientChunk.Empty)
		{
			if (clientChunk != null)
			{
				clientChunk.quantityDrawn++;
				clientChunk.enquedForRedraw = false;
			}
			return 0;
		}
		ChunkTesselator terrainChunkTesselator = game.TerrainChunkTesselator;
		lock (clientChunk.packUnpackLock)
		{
			if (!clientChunk.loadedFromServer)
			{
				requeue = true;
				return 0;
			}
			if (clientChunk.Unpack_ReadOnly())
			{
				RuntimeStats.TCTpacked++;
			}
			else
			{
				RuntimeStats.TCTunpacked++;
			}
			clientChunk.queuedForUpload = true;
			clientChunk.lastTesselationMs = game.Platform.EllapsedMs;
			clientChunk.enquedForRedraw = false;
			clientChunk.quantityDrawn++;
			terrainChunkTesselator.vars.blockEntitiesOfChunk = clientChunk.BlockEntities;
			terrainChunkTesselator.vars.rainHeightMap = clientChunk.MapChunk?.RainHeightMap ?? CreateDummyHeightMap();
		}
		if (RuntimeStats.chunksTesselatedTotal == 0)
		{
			RuntimeStats.tesselationStart = game.Platform.EllapsedMs;
		}
		RuntimeStats.chunksTesselatedPerSecond++;
		RuntimeStats.chunksTesselatedTotal++;
		if (skipChunkCenter)
		{
			RuntimeStats.chunksTesselatedEdgeOnly++;
		}
		if (clientChunk.shouldSunRelight)
		{
			game.terrainIlluminator.SunRelightChunk(clientChunk, chunkX, chunkY, chunkZ);
		}
		int num = 0;
		TesselatedChunk tesselatedChunk = null;
		tesselatedChunk = new TesselatedChunk
		{
			chunk = clientChunk,
			CullVisible = clientChunk.CullVisible,
			positionX = chunkX * chunksize,
			positionYAndDimension = chunkY * chunksize,
			positionZ = chunkZ * chunksize
		};
		num = (tesselatedChunk.VerticesCount = terrainChunkTesselator.NowProcessChunk(chunkX, chunkY, chunkZ, tesselatedChunk, skipChunkCenter));
		if (priority)
		{
			lock (tessChunksQueuePriorityLock)
			{
				tessChunksQueuePriority?.Enqueue(tesselatedChunk);
			}
			processPrioQueue = true;
		}
		else
		{
			lock (tessChunksQueueLock)
			{
				tessChunksQueue?.EnqueueOrMerge(tesselatedChunk);
			}
		}
		clientChunk.lastTesselationMs = 0L;
		return num;
	}

	private ushort[] CreateDummyHeightMap()
	{
		ushort[] array = new ushort[game.WorldMap.MapChunkSize * game.WorldMap.MapChunkSize];
		ushort num = (ushort)(game.WorldMap.MapSizeY - 1);
		int num2;
		for (num2 = 0; num2 < array.Length; num2++)
		{
			array[num2] = num;
			array[++num2] = num;
		}
		return array;
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
