using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class PacketHairStyle
{
	[ProtoMember(1)]
	public long HairstylingNpcEntityId;

	[ProtoMember(2)]
	public Dictionary<string, string> Hairstyle;
}
