using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class ClothPointPacket
{
	public int ClothId;

	public int PointX;

	public int PointY;

	public ClothPoint Point;
}
