using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Vintagestory.ServerMods;

public class NpcControl : ModSystem
{
	private ICoreServerAPI sapi;

	private Dictionary<string, long> currentEntityIdByPlayerUid = new Dictionary<string, long>();

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		CreateCommands();
		api.Event.OnPlayerInteractEntity += Event_OnPlayerInteractEntity;
	}

	private void CreateCommands()
	{
		CommandArgumentParsers parsers = sapi.ChatCommands.Parsers;
		sapi.ChatCommands.Create("npc").WithDescription("npc commands").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("list")
			.WithDescription("list")
			.RequiresPlayer()
			.HandleWith(OnCmdNpcList)
			.EndSubCommand()
			.BeginSubCommand("enqueue")
			.WithAlias("enq")
			.WithDescription("Enqueue a command")
			.BeginSubCommand("tp")
			.WithDescription("tp")
			.RequiresPlayer()
			.WithArgs(parsers.WorldPosition("position"))
			.HandleWith(OnCmdNpcTp)
			.EndSubCommand()
			.BeginSubCommand("goto")
			.WithDescription("Add a goto command that will move then entity from its current position to the new one using specified animation and speed")
			.RequiresPlayer()
			.WithArgs(parsers.WorldPosition("position"), parsers.Word("animcode"), parsers.OptionalFloat("speed", 0.02f), parsers.OptionalFloat("animspeed", 1f))
			.HandleWith((TextCommandCallingArgs args) => OnCmdNpcEnqGoto(args, astar: false))
			.EndSubCommand()
			.BeginSubCommand("playanim")
			.WithDescription("Add a play animation command")
			.RequiresPlayer()
			.WithArgs(parsers.Word("animcode"), parsers.OptionalFloat("animspeed", 1f))
			.HandleWith(OnCmdNpcEnqPlayanim)
			.EndSubCommand()
			.BeginSubCommand("lookat")
			.WithDescription("Make the npc look at a specific direction in radians")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalFloat("yaw"))
			.HandleWith(OnCmdNpcEnqLookat)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("upd")
			.BeginSubCommand("goto")
			.WithDescription("Update a specific goto command in the command list")
			.RequiresPlayer()
			.WithArgs(parsers.Int("id"), parsers.WordRange("type", "gs", "as"), parsers.Float("speed"))
			.HandleWith(OnCmdNpcGotoUpd)
			.EndSubCommand()
			.BeginSubCommand("lookat")
			.WithDescription("Update a specific lookat command in the command list")
			.RequiresPlayer()
			.WithArgs(parsers.Int("id"), parsers.Float("yaw"))
			.HandleWith(OnCmdNpcLookatUpd)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("start")
			.WithDescription("Start executing the command list")
			.RequiresPlayer()
			.HandleWith(OnCmdNpcStart)
			.EndSubCommand()
			.BeginSubCommand("stop")
			.WithDescription("Stop executing the command list")
			.RequiresPlayer()
			.HandleWith(OnCmdNpcStop)
			.EndSubCommand()
			.BeginSubCommand("clear")
			.WithDescription("Clear all commands in the command list")
			.RequiresPlayer()
			.HandleWith(OnCmdNpcClear)
			.EndSubCommand()
			.BeginSubCommand("exec")
			.WithDescription("Execute a command directly without adding it to the command list")
			.RequiresPlayer()
			.BeginSubCommand("tp")
			.WithDescription("tp")
			.RequiresPlayer()
			.WithArgs(parsers.WorldPosition("position"))
			.HandleWith(OnCmdNpcTp)
			.EndSubCommand()
			.BeginSubCommand("goto")
			.WithDescription("Execute a goto command that will move then entity from its current position to the new one using specified animation and speed")
			.RequiresPlayer()
			.WithArgs(parsers.WorldPosition("position"), parsers.Word("animcode"), parsers.OptionalFloat("speed", 0.02f), parsers.OptionalFloat("animspeed", 1f))
			.HandleWith((TextCommandCallingArgs args) => OnCmdNpcEnqGoto(args, astar: false))
			.EndSubCommand()
			.BeginSubCommand("navigate")
			.WithAlias("nav")
			.WithDescription("Execute a navigate command that will move then entity from its current position using A* pathfinding, to the new one using specified animation and speed")
			.RequiresPlayer()
			.WithArgs(parsers.WorldPosition("position"), parsers.Word("animcode"), parsers.OptionalFloat("speed", 0.02f), parsers.OptionalFloat("animspeed", 1f))
			.HandleWith((TextCommandCallingArgs args) => OnCmdNpcEnqGoto(args, astar: true))
			.EndSubCommand()
			.BeginSubCommand("playanim")
			.WithDescription("Execute a play animation command")
			.RequiresPlayer()
			.WithArgs(parsers.Word("animcode"), parsers.OptionalFloat("animspeed", 1f))
			.HandleWith(OnCmdNpcEnqPlayanim)
			.EndSubCommand()
			.BeginSubCommand("lookat")
			.WithDescription("Make the npc look at a specific yaw [radians] now")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalFloat("yaw"))
			.HandleWith(OnCmdNpcEnqLookat)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("loop")
			.WithDescription("Enable looping of all commands in the command list")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalBool("active"))
			.HandleWith(OnCmdNpcLoop)
			.EndSubCommand()
			.BeginSubCommand("remove")
			.WithDescription("Remove a specific command from the list. To see a list with index use /npc list")
			.RequiresPlayer()
			.WithArgs(parsers.Int("id"))
			.HandleWith(OnCmdNpcRemove)
			.EndSubCommand()
			.BeginSubCommand("setname")
			.WithDescription("Set the name of the npc")
			.RequiresPlayer()
			.WithArgs(parsers.Word("name"))
			.HandleWith(OnCmdNpcSetName)
			.EndSubCommand()
			.BeginSubCommand("copyskin")
			.WithDescription("Apply your own skin to the npc if it is skin able")
			.RequiresPlayer()
			.HandleWith(OnCmdNpcCopySkin)
			.EndSubCommand();
		sapi.ChatCommands.Create("npcs").RequiresPrivilege(Privilege.controlserver).WithDescription("Npcs control")
			.BeginSubCommand("startall")
			.WithAlias("startallrandom")
			.WithDescription("Start all loaded npcs")
			.RequiresPlayer()
			.HandleWith(OnCmdNpcsStartAll)
			.EndSubCommand()
			.BeginSubCommand("stopall")
			.WithDescription("Stop all loaded npcs")
			.RequiresPlayer()
			.HandleWith(OnCmdNpcsStopall)
			.EndSubCommand()
			.BeginSubCommand("loopall")
			.WithDescription("Set all loaded npcs loop mode")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalBool("loop_mode"))
			.HandleWith(OnCmdNpcsLoopall)
			.EndSubCommand();
	}

	private TextCommandResult OnCmdNpcsLoopall(TextCommandCallingArgs args)
	{
		bool flag = (bool)args[0];
		foreach (KeyValuePair<long, Entity> loadedEntity in sapi.World.LoadedEntities)
		{
			if (loadedEntity.Value is EntityAnimalBot entityAnimalBot)
			{
				entityAnimalBot.LoopCommands = flag;
			}
		}
		return TextCommandResult.Success("Command list looping is now " + (flag ? "on" : "off"));
	}

	private TextCommandResult OnCmdNpcsStopall(TextCommandCallingArgs args)
	{
		foreach (KeyValuePair<long, Entity> loadedEntity in sapi.World.LoadedEntities)
		{
			if (loadedEntity.Value is EntityAnimalBot entityAnimalBot)
			{
				entityAnimalBot.StopExecuteCommands();
			}
		}
		return TextCommandResult.Success("Command lists stopped");
	}

	private TextCommandResult OnCmdNpcsStartAll(TextCommandCallingArgs args)
	{
		foreach (KeyValuePair<long, Entity> loadedEntity in sapi.World.LoadedEntities)
		{
			Entity value = loadedEntity.Value;
			EntityAnimalBot npc = value as EntityAnimalBot;
			if (npc == null)
			{
				continue;
			}
			if (args.Command.Name == "startallrandom")
			{
				sapi.Event.RegisterCallback(delegate
				{
					npc.StartExecuteCommands();
				}, (int)(sapi.World.Rand.NextDouble() * 200.0));
			}
			else
			{
				npc.StartExecuteCommands();
			}
		}
		return TextCommandResult.Success("Command lists started");
	}

	private TextCommandResult OnCmdNpcCopySkin(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		EntityBehaviorExtraSkinnable? behavior = player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
		EntityBehaviorExtraSkinnable behavior2 = entityNpc.GetBehavior<EntityBehaviorExtraSkinnable>();
		if (behavior == null)
		{
			TextCommandResult.Success("Can't copy, player is not skinnable");
		}
		if (behavior2 == null)
		{
			TextCommandResult.Success("Can't copy, bot is not skinnable");
		}
		foreach (AppliedSkinnablePartVariant appliedSkinPart in behavior.AppliedSkinParts)
		{
			behavior2.selectSkinPart(appliedSkinPart.PartCode, appliedSkinPart.Code, retesselateShape: false);
		}
		entityNpc.WatchedAttributes.MarkPathDirty("skinConfig");
		return TextCommandResult.Success("SkinConfig set.");
	}

	private TextCommandResult OnCmdNpcSetName(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		entityNpc.GetBehavior<EntityBehaviorNameTag>()?.SetName(args[0] as string);
		return TextCommandResult.Success("Name set.");
	}

	private TextCommandResult OnCmdNpcLoop(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		entityNpc.LoopCommands = (args.Parsers[0].IsMissing ? (!entityNpc.LoopCommands) : ((bool)args[0]));
		return TextCommandResult.Success("Command list looping is now " + (entityNpc.LoopCommands ? "on" : "off"));
	}

	private TextCommandResult OnCmdNpcRemove(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		int num = (int)args[0];
		if (num >= 0 && num < entityNpc.Commands.Count)
		{
			entityNpc.Commands.RemoveAt(num);
			return TextCommandResult.Success("Ok, removed given command");
		}
		return TextCommandResult.Success("Index out of range or command list empty");
	}

	private TextCommandResult OnCmdNpcEnqLookat(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		float yaw = (float)args[0];
		if (args.Command.FullName.Contains("exec"))
		{
			entityNpc.ExecutingCommands.Enqueue(new NpcLookatCommand(entityNpc, yaw));
			entityNpc.StartExecuteCommands(enqueue: false);
			return TextCommandResult.Success("Started executing. " + entityNpc.ExecutingCommands.Count + " commands in queue");
		}
		entityNpc.Commands.Add(new NpcLookatCommand(entityNpc, yaw));
		return TextCommandResult.Success("Command enqueued");
	}

	private TextCommandResult OnCmdNpcEnqPlayanim(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		string animCode = args[0] as string;
		float animSpeed = (float)args[1];
		if (args.Command.FullName.Contains("exec"))
		{
			entityNpc.ExecutingCommands.Enqueue(new NpcPlayAnimationCommand(entityNpc, animCode, animSpeed));
			entityNpc.StartExecuteCommands(enqueue: false);
			return TextCommandResult.Success("Started executing. " + entityNpc.ExecutingCommands.Count + " commands in queue");
		}
		entityNpc.Commands.Add(new NpcPlayAnimationCommand(entityNpc, animCode, animSpeed));
		return TextCommandResult.Success("Command enqueued");
	}

	private TextCommandResult OnCmdNpcEnqGoto(TextCommandCallingArgs args, bool astar)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		Vec3d target = (Vec3d)args.Parsers[0].GetValue();
		string animCode = args[1] as string;
		float gotoSpeed = (float)args[2];
		float animSpeed = (float)args[3];
		if (args.Command.FullName.Contains("exec"))
		{
			entityNpc.ExecutingCommands.Enqueue(new NpcGotoCommand(entityNpc, target, astar, animCode, gotoSpeed, animSpeed));
			entityNpc.StartExecuteCommands(enqueue: false);
			return TextCommandResult.Success("Started executing. " + entityNpc.ExecutingCommands.Count + " commands in queue");
		}
		entityNpc.Commands.Add(new NpcGotoCommand(entityNpc, target, astar, animCode, gotoSpeed, animSpeed));
		return TextCommandResult.Success("Command enqueued");
	}

	private TextCommandResult OnCmdNpcTp(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		Vec3d target = (Vec3d)args.Parsers[0].GetValue();
		if (args.Command.FullName.Contains("exec"))
		{
			entityNpc.ExecutingCommands.Enqueue(new NpcTeleportCommand(entityNpc, target));
			entityNpc.StartExecuteCommands(enqueue: false);
			return TextCommandResult.Success("Started executing. " + entityNpc.ExecutingCommands.Count + " commands in queue");
		}
		entityNpc.Commands.Add(new NpcTeleportCommand(entityNpc, target));
		return TextCommandResult.Success("Command enqueued");
	}

	private void Event_OnPlayerInteractEntity(Entity entity, IPlayer byPlayer, ItemSlot slot, Vec3d hitPosition, int mode, ref EnumHandling handling)
	{
		if (entity is EntityAnimalBot && mode == 1)
		{
			currentEntityIdByPlayerUid[byPlayer.PlayerUID] = entity.EntityId;
			(byPlayer as IServerPlayer).SendMessage(GlobalConstants.CurrentChatGroup, "Ok, npc selected", EnumChatType.Notification);
		}
	}

	private TextCommandResult OnCmdNpcExec(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var _, out var msg))
		{
			return msg;
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdNpcClear(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		entityNpc.Commands.Clear();
		if (entityNpc.ExecutingCommands.Count > 0)
		{
			entityNpc.ExecutingCommands.Peek().Stop();
		}
		entityNpc.ExecutingCommands.Clear();
		return TextCommandResult.Success("Command list cleared");
	}

	private TextCommandResult OnCmdNpcStart(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		entityNpc.StartExecuteCommands();
		return TextCommandResult.Success("Started command execution");
	}

	private TextCommandResult OnCmdNpcStop(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		entityNpc.StopExecuteCommands();
		return TextCommandResult.Success("Stopped command execution");
	}

	private TextCommandResult OnCmdNpcGotoUpd(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		int num = (int)args[0];
		if (num < 0 || num > entityNpc.Commands.Count)
		{
			return TextCommandResult.Success("Index out of range");
		}
		if (!(entityNpc.Commands[num] is NpcGotoCommand npcGotoCommand))
		{
			return TextCommandResult.Success("fail");
		}
		string text = (string)args[1];
		float num2 = (float)args[2];
		if (!(text == "gs"))
		{
			if (text == "as")
			{
				npcGotoCommand.AnimSpeed = num2;
				return TextCommandResult.Success("Ok animation speed updated to " + npcGotoCommand.AnimSpeed);
			}
			return TextCommandResult.Success();
		}
		npcGotoCommand.GotoSpeed = num2;
		return TextCommandResult.Success("Ok goto speed updated to " + npcGotoCommand.GotoSpeed);
	}

	private TextCommandResult OnCmdNpcLookatUpd(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		int num = (int)args[0];
		if (num < 0 || num > entityNpc.Commands.Count)
		{
			return TextCommandResult.Success("Index out of range");
		}
		if (!(entityNpc.Commands[num] is NpcLookatCommand npcLookatCommand))
		{
			return TextCommandResult.Success("fail");
		}
		npcLookatCommand.yaw = (float)args[1];
		return TextCommandResult.Success("Yaw " + npcLookatCommand.yaw + " set");
	}

	private TextCommandResult OnCmdNpcList(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		foreach (INpcCommand command in entityNpc.Commands)
		{
			stringBuilder.AppendLine(num + ": " + command);
			num++;
		}
		return TextCommandResult.Success(stringBuilder.ToString());
	}

	private bool TryGetCurrentEntity(IPlayer player, out EntityAnimalBot entityNpc, out TextCommandResult msg)
	{
		if (!currentEntityIdByPlayerUid.TryGetValue(player.PlayerUID, out var value) || value == 0L)
		{
			msg = TextCommandResult.Success("Select a npc first");
			entityNpc = null;
			return false;
		}
		sapi.World.LoadedEntities.TryGetValue(value, out var value2);
		entityNpc = value2 as EntityAnimalBot;
		if (entityNpc == null)
		{
			msg = TextCommandResult.Success("No such npc with this id found");
			return false;
		}
		msg = null;
		return true;
	}
}
