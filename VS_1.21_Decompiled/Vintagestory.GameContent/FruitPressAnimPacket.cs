using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class FruitPressAnimPacket
{
	public EnumFruitPressAnimState AnimationState;

	public float AnimationSpeed;

	public float CurrentFrame;
}
