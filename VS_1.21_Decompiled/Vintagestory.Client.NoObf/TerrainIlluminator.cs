using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Client.NoObf;

public class TerrainIlluminator : IChunkProvider
{
	private ChunkIlluminator chunkIlluminator;

	private ClientMain game;

	public ILogger Logger => game.Logger;

	public TerrainIlluminator(ClientMain game)
	{
		this.game = game;
		chunkIlluminator = new ChunkIlluminator(this, new BlockAccessorRelaxed(game.WorldMap, game, synchronize: false, relight: false), game.WorldMap.ClientChunkSize);
	}

	public void OnBlockTexturesLoaded()
	{
		chunkIlluminator.InitForWorld(game.Blocks, (ushort)game.WorldMap.SunBrightness, game.WorldMap.MapSizeX, game.WorldMap.MapSizeY, game.WorldMap.MapSizeZ);
	}

	internal void SunRelightChunk(ClientChunk chunk, long index3d)
	{
		ChunkPos chunkPos = game.WorldMap.ChunkPosFromChunkIndex3D(index3d);
		SunRelightChunk(chunk, chunkPos.X, chunkPos.Y, chunkPos.Z);
	}

	public void SunRelightChunk(ClientChunk chunk, int chunkX, int chunkY, int chunkZ)
	{
		ClientChunk[] array = new ClientChunk[game.WorldMap.ChunkMapSizeY];
		for (int i = 0; i < game.WorldMap.ChunkMapSizeY; i++)
		{
			array[i] = game.WorldMap.GetClientChunk(chunkX, i, chunkZ);
			array[i].shouldSunRelight = false;
			array[i].quantityRelit++;
			array[i].Unpack();
		}
		chunk.Lighting.ClearAllSunlight();
		ChunkIlluminator obj = chunkIlluminator;
		IWorldChunk[] chunks = array;
		obj.Sunlight(chunks, chunkX, chunkY, chunkZ, 0);
		ChunkIlluminator obj2 = chunkIlluminator;
		chunks = array;
		obj2.SunlightFlood(chunks, chunkX, chunkY, chunkZ);
		ChunkIlluminator obj3 = chunkIlluminator;
		chunks = array;
		byte b = obj3.SunLightFloodNeighbourChunks(chunks, chunkX, chunkY, chunkZ, 0);
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing in aLLFACES)
		{
			if ((blockFacing.Flag & b) > 0)
			{
				int chunkX2 = chunkX + blockFacing.Normali.X;
				int chunkY2 = chunkY + blockFacing.Normali.Y;
				int chunkZ2 = chunkZ + blockFacing.Normali.Z;
				game.WorldMap.MarkChunkDirty(chunkX2, chunkY2, chunkZ2, priority: true);
			}
		}
	}

	public IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ)
	{
		ClientChunk clientChunk = game.WorldMap.GetClientChunk(chunkX, chunkY, chunkZ);
		clientChunk?.Unpack();
		return clientChunk;
	}

	public IWorldChunk GetUnpackedChunkFast(int chunkX, int chunkY, int chunkZ, bool notRecentlyAccessed = false)
	{
		return ((IChunkProvider)game.WorldMap).GetUnpackedChunkFast(chunkX, chunkY, chunkZ, notRecentlyAccessed);
	}

	public long ChunkIndex3D(int chunkX, int chunkY, int chunkZ)
	{
		return ((long)chunkY * (long)game.WorldMap.index3dMulZ + chunkZ) * game.WorldMap.index3dMulX + chunkX;
	}

	public long ChunkIndex3D(EntityPos pos)
	{
		return game.WorldMap.ChunkIndex3D(pos);
	}
}
