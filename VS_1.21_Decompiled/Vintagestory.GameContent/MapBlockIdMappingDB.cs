using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class MapBlockIdMappingDB
{
	public Dictionary<AssetLocation, int> BlockIndicesByBlockCode;
}
