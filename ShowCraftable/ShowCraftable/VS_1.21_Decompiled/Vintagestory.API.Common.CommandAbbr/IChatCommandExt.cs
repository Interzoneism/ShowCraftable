namespace Vintagestory.API.Common.CommandAbbr;

public static class IChatCommandExt
{
	public static IChatCommand WithDesc(this IChatCommand cmd, string description)
	{
		return cmd.WithDescription(description);
	}

	public static IChatCommand BeginSub(this IChatCommand cmd, string name)
	{
		return cmd.BeginSubCommand(name);
	}

	public static IChatCommand BeginSubs(this IChatCommand cmd, params string[] name)
	{
		return cmd.BeginSubCommands(name);
	}

	public static IChatCommand EndSub(this IChatCommand cmd)
	{
		return cmd.EndSubCommand();
	}
}
