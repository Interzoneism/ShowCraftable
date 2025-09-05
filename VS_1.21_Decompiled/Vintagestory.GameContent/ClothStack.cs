using ProtoBuf;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class ClothStack
{
	public EnumItemClass Class;

	public string Code;

	public int SlotNum;
}
