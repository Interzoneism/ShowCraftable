namespace Vintagestory.API.Common;

public class ClientChatCommand : ChatCommand
{
	public ClientChatCommandDelegate handler;

	public override void CallHandler(IPlayer player, int groupId, CmdArgs args)
	{
		handler(groupId, args);
	}
}
