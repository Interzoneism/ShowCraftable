using Vintagestory.API.Common;

namespace Vintagestory.API.Server;

public interface IPermissionManager
{
	IPlayerRole GetRole(string code);

	void SetRole(IServerPlayer player, IPlayerRole role);

	void SetRole(IServerPlayer player, string roleCode);

	void RegisterPrivilege(string code, string shortdescription, bool adminAutoGrant = true);

	void GrantTemporaryPrivilege(string code);

	void DropTemporaryPrivilege(string code);

	bool GrantPrivilege(string playerUID, string code, bool permanent = false);

	bool DenyPrivilege(string playerUID, string code);

	bool RemovePrivilegeDenial(string playerUID, string code);

	bool RevokePrivilege(string playerUID, string code, bool permanent = false);

	bool AddPrivilegeToGroup(string groupCode, string privilegeCode);

	bool RemovePrivilegeFromGroup(string groupCode, string privilegeCode);

	int GetPlayerPermissionLevel(int player);
}
