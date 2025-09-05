using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(/*Could not decode attribute arguments.*/)]
public class PrivGrantsData
{
	public Dictionary<string, ReinforcedPrivilegeGrants> privGrantsByOwningPlayerUid = new Dictionary<string, ReinforcedPrivilegeGrants>();

	public Dictionary<int, ReinforcedPrivilegeGrantsGroup> privGrantsByOwningGroupUid = new Dictionary<int, ReinforcedPrivilegeGrantsGroup>();
}
