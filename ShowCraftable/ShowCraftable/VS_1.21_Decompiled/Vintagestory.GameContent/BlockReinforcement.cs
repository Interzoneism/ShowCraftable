using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class BlockReinforcement
{
	public int Strength;

	public string PlayerUID;

	public string LastPlayername;

	public bool Locked;

	public string LockedByItemCode;

	public int GroupUid;

	public string LastGroupname;
}
