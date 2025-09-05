using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class GameEffectsPacket
{
	public bool RainAndFogActive;

	public bool GlitchPresent;

	public bool SlomoActive;
}
