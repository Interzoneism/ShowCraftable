using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Vintagestory.Systems;

public class StoryLockableDoor : ModSystem
{
	private ICoreServerAPI? sapi;

	private bool needsSaving;

	public Dictionary<string, HashSet<string>> StoryLockedLocationCodes;

	private IServerNetworkChannel serverChannel;

	private IClientNetworkChannel clientChannel;

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		sapi.Event.SaveGameLoaded += Event_SaveGameLoaded;
		sapi.Event.GameWorldSave += Event_GameWorldSave;
		sapi.Event.PlayerJoin += OnPlayerJoin;
		serverChannel = api.Network.RegisterChannel("storylockeddoors");
		serverChannel.RegisterMessageType(typeof(StoryLockableDoors));
		sapi.ChatCommands.GetOrCreate("dev").BeginSubCommand("storylock").WithDescription("Manage story locked doors")
			.RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("set")
			.WithDescription("Lock a door to a specific story dialog trigger")
			.WithArgs(sapi.ChatCommands.Parsers.WorldPosition("target"), sapi.ChatCommands.Parsers.OptionalWord("code"))
			.HandleWith(OnLockCmd)
			.EndSubCommand()
			.BeginSubCommand("clear")
			.WithDescription("Clear everyone who activated a story lock trigger for the specified code")
			.WithArgs(sapi.ChatCommands.Parsers.Word("code"))
			.HandleWith(OnClearCmd)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("per-player-looable-reset")
			.WithAlias("pplr")
			.RequiresPrivilege(Privilege.controlserver)
			.WithDescription("Reset storage to be lootable again by everyone")
			.WithArgs(api.ChatCommands.Parsers.WorldPosition("pos"))
			.HandleWith(OnPerPlayerInvRes)
			.EndSubCommand();
	}

	private TextCommandResult OnPerPlayerInvRes(TextCommandCallingArgs args)
	{
		Vec3d vec3d = (Vec3d)args[0];
		if (sapi.World.BlockAccessor.GetBlockEntity(vec3d.AsBlockPos) is BlockEntityGenericTypedContainer { inventory: InventoryPerPlayer inventory } blockEntityGenericTypedContainer)
		{
			inventory.PlayerQuantities.Clear();
			blockEntityGenericTypedContainer.MarkDirty();
			return TextCommandResult.Success("Storage usage reset");
		}
		return TextCommandResult.Success("No reset able storage found");
	}

	private TextCommandResult OnClearCmd(TextCommandCallingArgs args)
	{
		string text = (string)args[1];
		StoryLockedLocationCodes.Remove(text);
		return TextCommandResult.Success("Story lock for code: " + text + " cleared");
	}

	private TextCommandResult OnLockCmd(TextCommandCallingArgs args)
	{
		Vec3d vec3d = args[0] as Vec3d;
		BlockEntity blockEntity = sapi.World.BlockAccessor.GetBlockEntity(vec3d.AsBlockPos);
		BEBehaviorDoor bEBehaviorDoor = blockEntity?.GetBehavior<BEBehaviorDoor>();
		if (bEBehaviorDoor == null)
		{
			return TextCommandResult.Success("Target block is not a Door");
		}
		if (args.Parsers[1].IsMissing)
		{
			bEBehaviorDoor.StoryLockedCode = null;
		}
		else
		{
			bEBehaviorDoor.StoryLockedCode = args[1] as string;
		}
		blockEntity.MarkDirty();
		return TextCommandResult.Success("Story lock for door set to: " + bEBehaviorDoor.StoryLockedCode);
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		clientChannel = api.Network.RegisterChannel("storylockeddoors");
		clientChannel.RegisterMessageType(typeof(StoryLockableDoors));
		clientChannel.SetMessageHandler<StoryLockableDoors>(OnUpdate);
		StoryLockedLocationCodes = new Dictionary<string, HashSet<string>>();
	}

	private void OnUpdate(StoryLockableDoors packet)
	{
		StoryLockedLocationCodes = packet.StoryLockedLocationCodes;
	}

	private void OnPlayerJoin(IServerPlayer byPlayer)
	{
		if (StoryLockedLocationCodes.Count > 0)
		{
			StoryLockableDoors message = new StoryLockableDoors
			{
				StoryLockedLocationCodes = StoryLockedLocationCodes
			};
			serverChannel.SendPacket(message, byPlayer);
		}
	}

	private void Event_GameWorldSave()
	{
		if (needsSaving)
		{
			needsSaving = false;
			sapi.WorldManager.SaveGame.StoreData("storyLockedDoors", StoryLockedLocationCodes);
		}
	}

	private void Event_SaveGameLoaded()
	{
		StoryLockedLocationCodes = sapi.WorldManager.SaveGame.GetData("storyLockedDoors", new Dictionary<string, HashSet<string>>());
		if (StoryLockedLocationCodes == null)
		{
			StoryLockedLocationCodes = new Dictionary<string, HashSet<string>>();
		}
	}

	public void Add(string doorCode, EntityPlayer player)
	{
		if (StoryLockedLocationCodes.TryGetValue(doorCode, out HashSet<string> value))
		{
			value.Add(player.PlayerUID);
		}
		else
		{
			StoryLockedLocationCodes.Add(doorCode, new HashSet<string> { player.PlayerUID });
		}
		if (sapi != null)
		{
			StoryLockableDoors message = new StoryLockableDoors
			{
				StoryLockedLocationCodes = StoryLockedLocationCodes
			};
			serverChannel.SendPacket(message, player.Player as IServerPlayer);
			needsSaving = true;
		}
	}
}
