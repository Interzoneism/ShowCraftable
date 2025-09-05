using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Essentials;

public class PathFindDebug : ModSystem
{
	private BlockPos start;

	private BlockPos end;

	private ICoreServerAPI sapi;

	private EnumAICreatureType ct;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		api.ChatCommands.GetOrCreate("debug").BeginSub("astar").WithDesc("A* path finding debug testing tool")
			.RequiresPrivilege(Privilege.controlserver)
			.RequiresPlayer()
			.WithArgs(api.ChatCommands.Parsers.WordRange("command", "start", "end", "bench", "clear", "ct"), api.ChatCommands.Parsers.OptionalWord("creature type"))
			.HandleWith(onAstarCmd)
			.EndSub();
	}

	private TextCommandResult onAstarCmd(TextCommandCallingArgs args)
	{
		string text = (string)args[0];
		IPlayer player = args.Caller.Player;
		BlockPos asBlockPos = player.Entity.ServerPos.XYZ.AsBlockPos;
		PathfindSystem modSystem = sapi.ModLoader.GetModSystem<PathfindSystem>();
		Cuboidf cuboidf = new Cuboidf(-0.4f, 0f, -0.4f, 0.4f, 1.5f, 0.4f);
		new Cuboidf(-0.2f, 0f, -0.2f, 0.2f, 1.5f, 0.2f);
		new Cuboidf(-0.6f, 0f, -0.6f, 0.6f, 1.5f, 0.6f);
		Cuboidf entityCollBox = cuboidf;
		int maxFallHeight = 3;
		float stepHeight = 1.01f;
		switch (text)
		{
		case "ct":
		{
			string text2 = (string)args[1];
			if (text2 == null)
			{
				return TextCommandResult.Success($"Current creature type is {text2}");
			}
			if (Enum.TryParse<EnumAICreatureType>(text2, out var result))
			{
				ct = result;
				return TextCommandResult.Success($"Creature type set to {text2}");
			}
			return TextCommandResult.Error($"Not a vaild enum type");
		}
		case "start":
			start = asBlockPos.Copy();
			sapi.World.HighlightBlocks(player, 26, new List<BlockPos> { start }, new List<int> { ColorUtil.ColorFromRgba(255, 255, 0, 128) });
			break;
		case "end":
			end = asBlockPos.Copy();
			sapi.World.HighlightBlocks(player, 27, new List<BlockPos> { end }, new List<int> { ColorUtil.ColorFromRgba(255, 0, 255, 128) });
			break;
		case "bench":
		{
			if (start == null || end == null)
			{
				return TextCommandResult.Error("Start/End not set");
			}
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			for (int i = 0; i < 15; i++)
			{
				modSystem.FindPath(start, end, maxFallHeight, stepHeight, entityCollBox);
			}
			stopwatch.Stop();
			float num = (float)stopwatch.ElapsedMilliseconds / 15f;
			return TextCommandResult.Success($"15 searches average: {(int)num} ms");
		}
		case "clear":
			start = null;
			end = null;
			sapi.World.HighlightBlocks(player, 2, new List<BlockPos>());
			sapi.World.HighlightBlocks(player, 26, new List<BlockPos>());
			sapi.World.HighlightBlocks(player, 27, new List<BlockPos>());
			break;
		}
		if (start == null || end == null)
		{
			sapi.World.HighlightBlocks(player, 2, new List<BlockPos>());
		}
		if (start != null && end != null)
		{
			Stopwatch stopwatch2 = new Stopwatch();
			stopwatch2.Start();
			List<PathNode> list = modSystem.FindPath(start, end, maxFallHeight, stepHeight, entityCollBox, ct);
			stopwatch2.Stop();
			int num2 = (int)stopwatch2.ElapsedMilliseconds;
			string text3 = $"Search took {num2} ms, {modSystem.astar.NodesChecked} nodes checked";
			if (list == null)
			{
				sapi.World.HighlightBlocks(player, 2, new List<BlockPos>());
				sapi.World.HighlightBlocks(player, 3, new List<BlockPos>());
				return TextCommandResult.Success(text3 + "\nNo path found");
			}
			List<BlockPos> list2 = new List<BlockPos>();
			foreach (PathNode item in list)
			{
				list2.Add(item);
			}
			sapi.World.HighlightBlocks(player, 2, list2, new List<int> { ColorUtil.ColorFromRgba(128, 128, 128, 30) });
			List<Vec3d> list3 = modSystem.ToWaypoints(list);
			list2 = new List<BlockPos>();
			foreach (Vec3d item2 in list3)
			{
				list2.Add(item2.AsBlockPos);
			}
			sapi.World.HighlightBlocks(player, 3, list2, new List<int> { ColorUtil.ColorFromRgba(128, 0, 0, 100) });
			return TextCommandResult.Success(text3);
		}
		return TextCommandResult.Success();
	}
}
