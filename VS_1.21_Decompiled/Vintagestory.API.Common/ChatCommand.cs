namespace Vintagestory.API.Common;

public abstract class ChatCommand
{
	public string Command;

	public string Syntax;

	public string Description;

	public string RequiredPrivilege;

	public abstract void CallHandler(IPlayer player, int groupId, CmdArgs args);

	public virtual string GetDescription()
	{
		return Description;
	}

	public virtual string GetSyntax()
	{
		return Syntax;
	}

	public virtual string GetHelpMessage()
	{
		return Command + ": " + Description + "\nSyntax: " + Syntax;
	}
}
