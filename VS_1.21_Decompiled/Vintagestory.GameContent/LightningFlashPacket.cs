using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class LightningFlashPacket
{
	public Vec3d Pos;

	public int Seed;
}
