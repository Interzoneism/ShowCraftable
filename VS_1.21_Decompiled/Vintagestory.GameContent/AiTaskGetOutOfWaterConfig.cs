using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskGetOutOfWaterConfig : AiTaskBaseConfig
{
	[JsonProperty]
	public float MoveSpeed = 0.06f;

	[JsonProperty]
	public int MinimumRangeToSeekLand = 50;

	[JsonProperty]
	public float RangeSearchAttemptsFactor = 2f;

	[JsonProperty]
	public float ChanceToStopTask = 0.1f;
}
