using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.Systems;

[ProtoContract]
public class StoryLockableDoors
{
	[ProtoMember(1)]
	public Dictionary<string, HashSet<string>> StoryLockedLocationCodes { get; set; }
}
