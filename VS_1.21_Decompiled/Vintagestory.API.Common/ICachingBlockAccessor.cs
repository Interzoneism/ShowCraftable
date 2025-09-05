namespace Vintagestory.API.Common;

public interface ICachingBlockAccessor : IBlockAccessor
{
	bool LastChunkLoaded { get; }

	void Begin();

	void Dispose();
}
