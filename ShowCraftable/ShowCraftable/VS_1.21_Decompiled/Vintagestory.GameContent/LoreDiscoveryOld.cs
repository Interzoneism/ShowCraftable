using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class LoreDiscoveryOld
{
	public string Code;

	public List<int> PieceIds;
}
