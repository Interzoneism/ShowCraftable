using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Server;

public interface IWorldGenHandler
{
	List<MapRegionGeneratorDelegate> OnMapRegionGen { get; }

	List<MapChunkGeneratorDelegate> OnMapChunkGen { get; }

	List<ChunkColumnGenerationDelegate>[] OnChunkColumnGen { get; }

	void WipeAllHandlers();
}
