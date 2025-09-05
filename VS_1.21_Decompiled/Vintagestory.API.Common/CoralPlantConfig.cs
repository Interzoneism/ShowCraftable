using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class CoralPlantConfig
{
	public required NatFloat Height;

	public float Chance;

	[JsonIgnore]
	public required Block[] Block;
}
