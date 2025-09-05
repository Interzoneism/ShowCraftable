using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Server;

public interface IServerChunk : IWorldChunk
{
	string GameVersionCreated { get; }

	bool NotAtEdge { get; }

	int BlocksPlaced { get; }

	int BlocksRemoved { get; }

	void SetServerModdata(string key, byte[] data);

	byte[] GetServerModdata(string key);

	bool RemoveBlockEntity(BlockPos pos);
}
