using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Server;

public interface IChunkProvider
{
	ILogger Logger { get; }

	IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ);

	IWorldChunk GetUnpackedChunkFast(int chunkX, int chunkY, int chunkZ, bool notRecentlyAccessed = false);

	[Obsolete("Use dimension aware overloads instead")]
	long ChunkIndex3D(int chunkX, int chunkY, int chunkZ);

	long ChunkIndex3D(EntityPos pos);
}
