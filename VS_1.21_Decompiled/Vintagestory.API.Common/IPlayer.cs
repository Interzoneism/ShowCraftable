using System.Collections.Generic;

namespace Vintagestory.API.Common;

public interface IPlayer
{
	IPlayerRole Role { get; set; }

	PlayerGroupMembership[] Groups { get; }

	List<Entitlement> Entitlements { get; }

	BlockSelection CurrentBlockSelection { get; }

	EntitySelection CurrentEntitySelection { get; }

	string PlayerName { get; }

	string PlayerUID { get; }

	int ClientId { get; }

	EntityPlayer Entity { get; }

	IWorldPlayerData WorldData { get; }

	IPlayerInventoryManager InventoryManager { get; }

	string[] Privileges { get; }

	bool ImmersiveFpMode { get; }

	PlayerGroupMembership[] GetGroups();

	PlayerGroupMembership GetGroup(int groupId);

	bool HasPrivilege(string privilegeCode);
}
