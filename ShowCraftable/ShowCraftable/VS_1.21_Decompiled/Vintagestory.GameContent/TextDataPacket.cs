using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class TextDataPacket
{
	[ProtoMember(1)]
	public string Text;

	[ProtoMember(2)]
	public float FontSize;
}
