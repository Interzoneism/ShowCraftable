namespace Vintagestory.API.Server;

public interface IChunkProviderThread
{
	IWorldGenBlockAccessor GetBlockAccessor(bool updateHeightmap);
}
