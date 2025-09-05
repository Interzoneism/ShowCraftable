using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class CharacterSelectionPacket
{
	public bool DidSelect;

	public ClothStack[] Clothes;

	public string CharacterClass;

	public Dictionary<string, string> SkinParts;

	public string VoiceType;

	public string VoicePitch;
}
