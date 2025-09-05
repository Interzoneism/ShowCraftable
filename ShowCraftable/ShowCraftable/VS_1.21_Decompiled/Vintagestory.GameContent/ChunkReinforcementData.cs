using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class ChunkReinforcementData
{
	public byte[] Data;

	public int chunkX;

	public int chunkY;

	public int chunkZ;
}
