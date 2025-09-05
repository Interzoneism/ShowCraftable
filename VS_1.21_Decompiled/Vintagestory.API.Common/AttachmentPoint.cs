using Newtonsoft.Json;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AttachmentPoint
{
	public ShapeElement ParentElement;

	[JsonProperty]
	public string Code;

	[JsonProperty]
	public double PosX;

	[JsonProperty]
	public double PosY;

	[JsonProperty]
	public double PosZ;

	[JsonProperty]
	public double RotationX;

	[JsonProperty]
	public double RotationY;

	[JsonProperty]
	public double RotationZ;

	public void DeDuplicate()
	{
		Code = Code.DeDuplicate();
	}
}
