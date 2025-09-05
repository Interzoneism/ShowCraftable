namespace Vintagestory.API.Common;

public interface IClientChunk : IWorldChunk
{
	bool LoadedFromServer { get; }

	void SetVisibility(bool visible);
}
