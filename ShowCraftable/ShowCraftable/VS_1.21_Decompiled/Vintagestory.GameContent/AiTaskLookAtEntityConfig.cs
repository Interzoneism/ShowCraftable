using System;
using Newtonsoft.Json;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskLookAtEntityConfig : AiTaskBaseTargetableConfig
{
	[JsonProperty]
	public float MaxTurnAngleDeg = 360f;

	[JsonProperty]
	public float SpawnAngleDeg;

	[JsonProperty]
	public float DefaultMinTurnAngleDegPerSec = 250f;

	[JsonProperty]
	public float DefaultMaxTurnAngleDegPerSec = 450f;

	public float MaxTurnAngleRad => MaxTurnAngleDeg * ((float)Math.PI / 180f);

	public float SpawnAngleRad => SpawnAngleDeg * ((float)Math.PI / 180f);
}
