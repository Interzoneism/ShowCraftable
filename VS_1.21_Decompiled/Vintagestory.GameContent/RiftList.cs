using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class RiftList
{
	public List<Rift> rifts = new List<Rift>();
}
