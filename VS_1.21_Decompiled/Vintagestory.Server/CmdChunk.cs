using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

internal class CmdChunk
{
	private ServerMain server;

	public CmdChunk(ServerMain server)
	{
		this.server = server;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.commandapi.GetOrCreate("chunk").WithDescription("Commands affecting chunks.").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("forceload")
			.WithDescription("Force the server to preload all chunk columns in given area.")
			.WithAdditionalInformation("These chunks will not be unloaded until the server is restarted.  Area is given by specifying block x,z coordinates for two opposite corners (both corners will be included).  Coordinates can be relative to the player using (~) prefix")
			.WithArgs(parsers.WorldPosition2D("position1"), parsers.WorldPosition2D("position2"))
			.HandleWith(handleForceLoadChunks)
			.EndSubCommand();
	}

	private TextCommandResult handleForceLoadChunks(TextCommandCallingArgs args)
	{
		Vec2i obj = args[0] as Vec2i;
		Vec2i vec2i = args[1] as Vec2i;
		int num = Math.Min(obj.X, vec2i.X) / 32;
		int num2 = Math.Max(obj.X, vec2i.X) / 32;
		int num3 = Math.Min(obj.Y, vec2i.Y) / 32;
		int num4 = Math.Max(obj.Y, vec2i.Y) / 32;
		int num5 = 0;
		for (int i = num; i <= num2; i++)
		{
			for (int j = num3; j <= num4; j++)
			{
				num5++;
				server.LoadChunkColumnFast(i, j, new ChunkLoadOptions
				{
					KeepLoaded = true
				});
			}
		}
		return TextCommandResult.Success("Ok, will force load " + num5 + " chunk columns");
	}
}
