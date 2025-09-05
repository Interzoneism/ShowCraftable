using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemUnloadChunks : ClientSystem
{
	public override string Name => "uc";

	public SystemUnloadChunks(ClientMain game)
		: base(game)
	{
		game.PacketHandlers[11] = HandleChunkUnload;
	}

	private void HandleChunkUnload(Packet_Server packet)
	{
		int index3dMulX = game.WorldMap.index3dMulX;
		int index3dMulZ = game.WorldMap.index3dMulZ;
		HashSet<Vec2i> hashSet = new HashSet<Vec2i>();
		int xCount = packet.UnloadChunk.GetXCount();
		for (int i = 0; i < xCount; i++)
		{
			int x = packet.UnloadChunk.X[i];
			int num = packet.UnloadChunk.Y[i];
			int num2 = packet.UnloadChunk.Z[i];
			if (num < 1024)
			{
				hashSet.Add(new Vec2i(x, num2));
			}
			long key = MapUtil.Index3dL(x, num, num2, index3dMulX, index3dMulZ);
			ClientChunk value = null;
			lock (game.WorldMap.chunksLock)
			{
				game.WorldMap.chunks.TryGetValue(key, out value);
			}
			if (value != null)
			{
				UnloadChunk(value);
				RuntimeStats.chunksUnloaded++;
			}
		}
		game.Logger.VerboseDebug("Entities and pool locations removed. Removing from chunk dict");
		lock (game.WorldMap.chunksLock)
		{
			for (int j = 0; j < xCount; j++)
			{
				long key2 = MapUtil.Index3dL(packet.UnloadChunk.X[j], packet.UnloadChunk.Y[j], packet.UnloadChunk.Z[j], index3dMulX, index3dMulZ);
				game.WorldMap.chunks.TryGetValue(key2, out var value2);
				value2?.Dispose();
				game.WorldMap.chunks.Remove(key2);
			}
		}
		foreach (Vec2i item in hashSet)
		{
			int x2 = item.X;
			int y = item.Y;
			bool flag = false;
			int num3 = 0;
			while (!flag && num3 < game.WorldMap.ChunkMapSizeY)
			{
				flag |= game.WorldMap.GetChunk(x2, num3, y) != null;
				num3++;
			}
			if (!flag)
			{
				game.WorldMap.MapChunks.Remove(game.WorldMap.MapChunkIndex2D(x2, y));
			}
		}
		ScreenManager.FrameProfiler.Mark("doneUnlCh");
	}

	private void UnloadChunk(ClientChunk clientchunk)
	{
		if (clientchunk == null)
		{
			return;
		}
		clientchunk.RemoveDataPoolLocations(game.chunkRenderer);
		for (int i = 0; i < clientchunk.EntitiesCount; i++)
		{
			Entity entity = clientchunk.Entities[i];
			if (entity != null && (game.EntityPlayer == null || entity.EntityId != game.EntityPlayer.EntityId))
			{
				EntityDespawnData entityDespawnData = new EntityDespawnData
				{
					Reason = EnumDespawnReason.Unload
				};
				game.eventManager?.TriggerEntityDespawn(entity, entityDespawnData);
				entity.OnEntityDespawn(entityDespawnData);
				game.RemoveEntityRenderer(entity);
				game.LoadedEntities.Remove(entity.EntityId);
			}
		}
		foreach (KeyValuePair<BlockPos, BlockEntity> blockEntity in clientchunk.BlockEntities)
		{
			blockEntity.Value.OnBlockUnloaded();
		}
	}

	public override void Dispose(ClientMain game)
	{
		foreach (KeyValuePair<long, ClientChunk> chunk in game.WorldMap.chunks)
		{
			UnloadChunk(chunk.Value);
		}
		game.EntityPlayer?.Properties.Client?.Renderer?.Dispose();
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
