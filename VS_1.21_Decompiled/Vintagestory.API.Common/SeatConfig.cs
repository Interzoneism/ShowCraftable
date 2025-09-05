using Newtonsoft.Json;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class SeatConfig
{
	public string APName;

	public string SelectionBox;

	public string SeatId;

	public bool Controllable;

	public Vec3f MountOffset;

	public Vec3f MountRotation = new Vec3f();

	public float? BodyYawLimit;

	public float EyeHeight = 1.5f;

	public EnumMountAngleMode AngleMode = EnumMountAngleMode.FixateYaw;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject Attributes;

	public string Animation { get; set; }
}
