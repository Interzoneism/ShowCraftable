using ProtoBuf;

namespace Vintagestory.Common.Network.Packets;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class BulkAnimationPacket
{
	public AnimationPacket[] Packets;
}
