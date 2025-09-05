using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class ClothLengthPacket
{
	public int ClothId;

	public double LengthChange;
}
