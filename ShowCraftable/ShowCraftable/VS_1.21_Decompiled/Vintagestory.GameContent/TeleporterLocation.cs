using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class TeleporterLocation
{
	public string SourceName;

	public BlockPos SourcePos;

	public string TargetName;

	public BlockPos TargetPos;
}
