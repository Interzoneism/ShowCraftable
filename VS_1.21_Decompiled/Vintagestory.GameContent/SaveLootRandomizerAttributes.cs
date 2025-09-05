using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class SaveLootRandomizerAttributes
{
	public string InventoryId;

	public int SlotId;

	public byte[] attributes;
}
