using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

[ProtoContract(/*Could not decode attribute arguments.*/)]
internal class UpgradeHerePacket
{
	public required BlockPos Pos;
}
