using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public class ServerChatCommand : ChatCommand
{
	public ServerChatCommandDelegate handler;

	public bool HasPrivilege(IServerPlayer player)
	{
		return player.HasPrivilege(RequiredPrivilege);
	}

	public override void CallHandler(IPlayer player, int groupId, CmdArgs args)
	{
		handler((IServerPlayer)player, groupId, args);
	}
}
