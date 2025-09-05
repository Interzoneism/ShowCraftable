using System;
using System.Collections.Generic;
using System.Runtime;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

internal class SystemCompressChunks : ClientSystem
{
	private long chunkCompressScanTimer;

	private float compressionRatio;

	private Queue<long> compactableClientChunks = new Queue<long>();

	private int lastCompactionTime;

	private float megabytesMinimum;

	private int compressed;

	private int[] ttlsByRamMode = new int[3] { 80000, 8000, 4000 };

	public override string Name => "cc";

	public SystemCompressChunks(ClientMain game)
		: base(game)
	{
		game.RegisterGameTickListener(TryCompactLargeObjectHeap, 1000);
		game.eventManager.RegisterRenderer(OnFinalizeFrame, EnumRenderStage.Done, "cc", 0.999);
	}

	private void TryCompactLargeObjectHeap(float dt)
	{
		if (ClientSettings.OptimizeRamMode != 2)
		{
			return;
		}
		int num = Environment.TickCount / 1000 - lastCompactionTime;
		if ((num >= 602 || (num >= 30 && !game.Platform.IsFocused)) && (float)(GC.GetTotalMemory(forceFullCollection: false) / 1024) / 1024f - megabytesMinimum > 512f)
		{
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
			lastCompactionTime = Environment.TickCount / 1000;
			float num2 = (float)(GC.GetTotalMemory(forceFullCollection: false) / 1024) / 1024f;
			if (num2 < megabytesMinimum || megabytesMinimum == 0f)
			{
				megabytesMinimum = num2;
			}
		}
	}

	public override int SeperateThreadTickIntervalMs()
	{
		return 20;
	}

	public override void OnSeperateThreadGameTick(float dt)
	{
		lock (game.compactSyncLock)
		{
			long num = 0L;
			if (compactableClientChunks.Count > 0)
			{
				num = compactableClientChunks.Dequeue();
			}
			if (num == 0L)
			{
				return;
			}
			ClientChunk value = null;
			lock (game.WorldMap.chunksLock)
			{
				game.WorldMap.chunks.TryGetValue(num, out value);
			}
			if (value != null)
			{
				value.Pack();
				if (!value.ChunkHasData())
				{
					Vec3i vec3i = new Vec3i();
					MapUtil.PosInt3d(num, game.WorldMap.index3dMulX, game.WorldMap.index3dMulZ, vec3i);
					throw new Exception($"ACP: Chunk {vec3i.X} {vec3i.Y} {vec3i.Z} has no more block data.");
				}
				game.compactedClientChunks.Enqueue(num);
			}
		}
	}

	public void OnFinalizeFrame(float dt)
	{
		if (game.extendedDebugInfo)
		{
			game.DebugScreenInfo["compactqueuesize"] = "Client Chunks in compact queue: " + compactableClientChunks.Count;
			game.DebugScreenInfo["compactratio"] = "Client chunk compression ratio: " + (compressionRatio * 100f).ToString("0.#") + "%";
		}
		else
		{
			game.DebugScreenInfo["compactqueuesize"] = "";
			game.DebugScreenInfo["compactratio"] = "";
		}
		long ellapsedMs = game.Platform.EllapsedMs;
		if (ellapsedMs - chunkCompressScanTimer < 4000)
		{
			return;
		}
		chunkCompressScanTimer = ellapsedMs;
		int num = ttlsByRamMode[ClientSettings.OptimizeRamMode];
		lock (game.compactSyncLock)
		{
			lock (game.WorldMap.chunksLock)
			{
				while (game.compactedClientChunks.Count > 0)
				{
					long key = game.compactedClientChunks.Dequeue();
					game.WorldMap.chunks.TryGetValue(key, out var value);
					value?.TryCommitPackAndFree();
				}
			}
			Vec3i vec3i = new Vec3i();
			Vec3i vec3i2 = new Vec3i((int)game.EntityPlayer.Pos.X, (int)game.EntityPlayer.Pos.Y, (int)game.EntityPlayer.Pos.Z) / game.WorldMap.ClientChunkSize;
			if (compactableClientChunks.Count == 0)
			{
				compressed = 0;
				lock (game.WorldMap.chunksLock)
				{
					foreach (KeyValuePair<long, ClientChunk> chunk in game.WorldMap.chunks)
					{
						if (chunk.Value.IsPacked())
						{
							compressed++;
						}
						else if (Environment.TickCount - chunk.Value.lastReadOrWrite > num && chunk.Value.centerModelPoolLocations != null && chunk.Value.edgeModelPoolLocations != null)
						{
							MapUtil.PosInt3d(chunk.Key, game.WorldMap.index3dMulX, game.WorldMap.index3dMulZ, vec3i);
							if (Math.Abs(vec3i2.X - vec3i.X) < 2 && Math.Abs(vec3i2.Z - vec3i.Z) < 2 && !chunk.Value.Empty)
							{
								chunk.Value.MarkFresh();
							}
							else
							{
								compactableClientChunks.Enqueue(chunk.Key);
							}
						}
					}
				}
			}
			compressionRatio = (float)compressed / (float)game.WorldMap.chunks.Count;
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
