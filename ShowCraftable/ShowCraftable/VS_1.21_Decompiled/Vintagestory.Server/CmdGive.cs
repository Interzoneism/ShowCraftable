using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

internal class CmdGive
{
	public CmdGive(ServerMain server)
	{
		IChatCommandApi chatCommands = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		_ = server.api;
		chatCommands.Create("giveitem").RequiresPrivilege(Privilege.gamemode).WithDescription("Give items to target")
			.WithArgs(parsers.Item("item code"), parsers.OptionalInt("quantity", 1), parsers.OptionalEntities("target"), parsers.OptionalAll("attributes"))
			.HandleWith((TextCommandCallingArgs args) => args.Parsers[2].IsMissing ? give(args.Caller.Entity, args) : CmdUtil.EntityEach(args, (Entity e) => give(e, args), 2));
		chatCommands.Create("giveblock").RequiresPrivilege(Privilege.gamemode).WithDescription("Give blocks to target")
			.WithArgs(parsers.Block("block code"), parsers.OptionalInt("quantity", 1), parsers.OptionalEntities("target"), parsers.OptionalAll("attributes"))
			.HandleWith((TextCommandCallingArgs args) => args.Parsers[2].IsMissing ? give(args.Caller.Entity, args) : CmdUtil.EntityEach(args, (Entity e) => give(e, args), 2));
	}

	private TextCommandResult give(Entity target, TextCommandCallingArgs args)
	{
		ItemStack itemStack = args[0] as ItemStack;
		int num = (itemStack.StackSize = (int)args[1]);
		string text = (string)args.LastArg;
		if (text != null)
		{
			itemStack.Attributes.MergeTree(TreeAttribute.FromJson(text) as TreeAttribute);
		}
		if (target.TryGiveItemStack(itemStack.Clone()))
		{
			return TextCommandResult.Success("Ok, gave " + num + "x " + itemStack.GetName());
		}
		return TextCommandResult.Error("Failed, target players inventory is likely full or cant accept this item for other reasons");
	}
}
