using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class WindPatternState
{
	public int Index;

	public float BaseStrength;

	public double ActiveUntilTotalHours;
}
