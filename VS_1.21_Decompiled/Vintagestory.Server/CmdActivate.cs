using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

internal class CmdActivate
{
	private ICoreServerAPI sapi;

	public CmdActivate(ServerMain server)
	{
		sapi = server.api;
		CommandArgumentParsers parsers = sapi.ChatCommands.Parsers;
		sapi.ChatCommands.Create("activate").RequiresPrivilege(Privilege.controlserver).WithDesc("Runs activate on targeted block with supplied arguments.")
			.WithExamples("<code>/activate ~ ~ ~1 { opened: true }</code> - Open door 1 block adjacent in x direction, given there is a placed door")
			.WithArgs(parsers.WorldPosition("position"), parsers.OptionalAll("argument object"))
			.HandleWith(activate);
	}

	private TextCommandResult activate(TextCommandCallingArgs args)
	{
		TreeAttribute activationArgs = null;
		if (!args.Parsers[1].IsMissing)
		{
			activationArgs = (TreeAttribute)JsonObject.FromJson(args[1] as string).ToAttribute();
		}
		Vec3d vec3d = args[0] as Vec3d;
		Block block = sapi.World.BlockAccessor.GetBlock(vec3d.AsBlockPos);
		block.Activate(sapi.World, args.Caller, new BlockSelection
		{
			Position = vec3d.AsBlockPos,
			Face = BlockFacing.NORTH
		}, activationArgs);
		return TextCommandResult.Success("Called activate on block code" + block.Code);
	}
}
