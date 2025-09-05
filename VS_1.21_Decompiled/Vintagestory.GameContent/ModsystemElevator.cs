using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModsystemElevator : ModSystem
{
	private ICoreServerAPI sapi;

	public Dictionary<string, ElevatorSystem> Networks = new Dictionary<string, ElevatorSystem>();

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		CommandArgumentParsers parsers = sapi.ChatCommands.Parsers;
		sapi.ChatCommands.GetOrCreate("dev").BeginSubCommand("elevator").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("set-entity-net")
			.WithAlias("sen")
			.WithDescription("Set Elevator network code")
			.WithArgs(parsers.Entities("entity"), parsers.Word("network code"))
			.HandleWith(OnEntityNetworkSet)
			.EndSubCommand()
			.BeginSubCommand("set-block-net")
			.WithAlias("sbn")
			.WithDescription("Set Elevator network code")
			.WithArgs(parsers.Word("network code"), parsers.WorldPosition("pos"), parsers.OptionalInt("offset"))
			.HandleWith(OnsetBlockNetwork)
			.EndSubCommand()
			.EndSubCommand();
	}

	private TextCommandResult OnsetBlockNetwork(TextCommandCallingArgs args)
	{
		string text = args[0] as string;
		Vec3d vec3d = args.Parsers[1].GetValue() as Vec3d;
		int offset = (args.Parsers[2].IsMissing ? (-1) : ((int)args[2]));
		BEBehaviorElevatorControl bEBehavior = sapi.World.BlockAccessor.GetBlock(vec3d.AsBlockPos).GetBEBehavior<BEBehaviorElevatorControl>(vec3d.AsBlockPos);
		if (bEBehavior == null)
		{
			return TextCommandResult.Success("Target was not a ElevatorControl block");
		}
		bEBehavior.NetworkCode = text;
		bEBehavior.Offset = offset;
		sapi.ModLoader.GetModSystem<ModsystemElevator>().RegisterControl(text, vec3d.AsBlockPos, offset);
		return TextCommandResult.Success("Network code set to " + text);
	}

	private TextCommandResult OnEntityNetworkSet(TextCommandCallingArgs args)
	{
		return CmdUtil.EntityEach(args, delegate(Entity e)
		{
			string text = args[1] as string;
			if (!(e is EntityElevator entityElevator))
			{
				return TextCommandResult.Success("Target was not a elevator");
			}
			entityElevator.NetworkCode = text;
			ElevatorSystem elevatorSys = sapi.ModLoader.GetModSystem<ModsystemElevator>().RegisterElevator(text, entityElevator);
			entityElevator.ElevatorSys = elevatorSys;
			return TextCommandResult.Success("Network code set to " + text);
		});
	}

	public void EnsureNetworkExists(string networkCode)
	{
		if (!string.IsNullOrEmpty(networkCode) && !Networks.ContainsKey(networkCode))
		{
			Networks.TryAdd(networkCode, new ElevatorSystem());
		}
	}

	public ElevatorSystem GetElevator(string networkCode)
	{
		EnsureNetworkExists(networkCode);
		return Networks.GetValueOrDefault(networkCode);
	}

	public ElevatorSystem RegisterElevator(string networkCode, EntityElevator elevator)
	{
		if (Networks.TryGetValue(networkCode, out var value))
		{
			value.Entity = elevator;
			return value;
		}
		Networks.TryAdd(networkCode, new ElevatorSystem
		{
			Entity = elevator
		});
		return Networks[networkCode];
	}

	public void CallElevator(string networkCode, BlockPos position, int offset)
	{
		GetElevator(networkCode)?.Entity.CallElevator(position, offset);
	}

	public void RegisterControl(string networkCode, BlockPos pos, int offset)
	{
		ElevatorSystem elevator = GetElevator(networkCode);
		int num = pos.Y + offset;
		if (elevator == null || elevator.ControlPositions.Contains(num))
		{
			return;
		}
		elevator.ControlPositions.Add(num);
		elevator.ControlPositions.Sort();
		if (elevator.Entity != null && elevator.Entity.Attributes.GetBool("isActivated"))
		{
			if (!elevator.Entity.Attributes.HasAttribute("maxHeight"))
			{
				elevator.ShouldUpdate = true;
			}
			if (elevator.ShouldUpdate && elevator.Entity.Attributes.GetInt("maxHeight") < num)
			{
				elevator.Entity.Attributes.SetInt("maxHeight", elevator.ControlPositions.Last());
			}
		}
	}
}
