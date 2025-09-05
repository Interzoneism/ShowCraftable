using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

internal class CmdSetBlock
{
	private ICoreServerAPI sapi;

	public CmdSetBlock(ServerMain server)
	{
		sapi = server.api;
		CommandArgumentParsers parsers = sapi.ChatCommands.Parsers;
		sapi.ChatCommands.Create("setblock").RequiresPrivilege(Privilege.gamemode).WithDesc("Set a block at a given location")
			.WithArgs(parsers.Block("block code"), parsers.WorldPosition("target"))
			.HandleWith(handleSetBlock);
	}

	private TextCommandResult handleSetBlock(TextCommandCallingArgs args)
	{
		ItemStack itemStack = args[0] as ItemStack;
		Vec3d vec3d = args[1] as Vec3d;
		if (vec3d == null)
		{
			return TextCommandResult.Error("Missing/Invalid target");
		}
		sapi.World.BlockAccessor.SetBlock(itemStack.Block.Id, vec3d.AsBlockPos, itemStack);
		return TextCommandResult.Error(string.Concat(itemStack.Block.Code, " + placed."));
	}
}
