using ProtoBuf;

namespace Vintagestory.GameContent.Mechanics;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class MechNetworkPacket
{
	public long networkId;

	public float totalAvailableTorque;

	public float networkResistance;

	public float networkTorque;

	public float speed;

	public int direction;

	public float angle;
}
