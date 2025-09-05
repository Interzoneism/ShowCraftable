using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Server;

public interface IServerPlayerData
{
	string PlayerUID { get; }

	string RoleCode { get; }

	HashSet<string> PermaPrivileges { get; }

	HashSet<string> DeniedPrivileges { get; }

	Dictionary<int, PlayerGroupMembership> PlayerGroupMemberships { get; }

	bool AllowInvite { get; }

	string LastKnownPlayername { get; }

	Dictionary<string, string> CustomPlayerData { get; }

	int ExtraLandClaimAllowance { get; set; }

	int ExtraLandClaimAreas { get; set; }

	string FirstJoinDate { get; set; }

	string LastJoinDate { get; set; }

	string LastCharacterSelectionDate { get; set; }
}
