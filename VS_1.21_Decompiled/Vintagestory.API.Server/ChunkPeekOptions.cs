using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Server;

public class ChunkPeekOptions
{
	public EnumWorldGenPass UntilPass = EnumWorldGenPass.Done;

	public OnChunkPeekedDelegate OnGenerated;

	public ITreeAttribute ChunkGenParams = new TreeAttribute();
}
