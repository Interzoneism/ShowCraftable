using ProtoBuf;

namespace Vintagestory.ServerMods;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class LatitudeData
{
	public double ZOffset;

	public bool isRealisticClimate;

	public int polarEquatorDistance;
}
