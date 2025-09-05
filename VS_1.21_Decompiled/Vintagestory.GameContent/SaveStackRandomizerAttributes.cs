using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class SaveStackRandomizerAttributes
{
	public string InventoryId;

	public int SlotId;

	public float TotalChance;
}
