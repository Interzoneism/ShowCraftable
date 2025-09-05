using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.Server;

public class SpawnArea
{
	public int chunkY;

	public long[] ChunkColumnCoords;

	public Dictionary<AssetLocation, int> spawnCounts = new Dictionary<AssetLocation, int>();
}
