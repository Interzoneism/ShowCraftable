using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class VariablesModSystem : ModSystem
{
	public VariableData VarData;

	public ICoreAPI Api;

	protected ICoreServerAPI sapi;

	public event OnDialogueControllerInitDelegate OnDialogueControllerInit;

	public override void Start(ICoreAPI api)
	{
		Api = api;
		api.Network.RegisterChannel("variable").RegisterMessageType<VariableData>();
		api.ChatCommands.GetOrCreate("debug").BeginSubCommand("clearvariables").WithDescription("clearvariables")
			.HandleWith(cmdClearVariables);
		OnDialogueControllerInit += setDefaultVariables;
	}

	private TextCommandResult cmdClearVariables(TextCommandCallingArgs args)
	{
		VarData.GlobalVariables = new EntityVariables();
		VarData.PlayerVariables = new Dictionary<string, EntityVariables>();
		VarData.GroupVariables = new Dictionary<string, EntityVariables>();
		return TextCommandResult.Success("Variables cleared");
	}

	public void SetVariable(Entity callingEntity, EnumActivityVariableScope scope, string name, string value)
	{
		switch (scope)
		{
		case EnumActivityVariableScope.Entity:
		{
			ITreeAttribute treeAttribute2 = callingEntity.WatchedAttributes.GetTreeAttribute("variables");
			if (treeAttribute2 == null)
			{
				treeAttribute2 = (ITreeAttribute)(callingEntity.WatchedAttributes["variables"] = new TreeAttribute());
			}
			treeAttribute2[name] = new StringAttribute(value);
			break;
		}
		case EnumActivityVariableScope.Global:
			VarData.GlobalVariables[name] = value;
			break;
		case EnumActivityVariableScope.Group:
		{
			string key = callingEntity.WatchedAttributes.GetString("groupCode");
			if (!VarData.GroupVariables.TryGetValue(key, out var value2))
			{
				value2 = (VarData.GroupVariables[key] = new EntityVariables());
			}
			value2[name] = value;
			break;
		}
		case EnumActivityVariableScope.Player:
		{
			string playerUID2 = (callingEntity as EntityPlayer).Player.PlayerUID;
			if (!VarData.PlayerVariables.TryGetValue(playerUID2, out var value3))
			{
				value3 = (VarData.PlayerVariables[playerUID2] = new EntityVariables());
			}
			value3[name] = value;
			break;
		}
		case EnumActivityVariableScope.EntityPlayer:
		{
			string playerUID = (callingEntity as EntityPlayer).Player.PlayerUID;
			ITreeAttribute treeAttribute = callingEntity.WatchedAttributes.GetTreeAttribute("variables");
			if (treeAttribute == null)
			{
				treeAttribute = (ITreeAttribute)(callingEntity.WatchedAttributes["variables"] = new TreeAttribute());
			}
			treeAttribute[playerUID + "-" + name] = new StringAttribute(value);
			break;
		}
		}
	}

	public void SetPlayerVariable(string playerUid, string name, string value)
	{
		if (!VarData.PlayerVariables.TryGetValue(playerUid, out var value2))
		{
			value2 = (VarData.PlayerVariables[playerUid] = new EntityVariables());
		}
		value2[name] = value;
	}

	public string GetVariable(EnumActivityVariableScope scope, string name, Entity callingEntity)
	{
		switch (scope)
		{
		case EnumActivityVariableScope.Entity:
		{
			ITreeAttribute treeAttribute2 = callingEntity.WatchedAttributes.GetTreeAttribute("variables");
			if (treeAttribute2 != null)
			{
				return (treeAttribute2[name] as StringAttribute)?.value;
			}
			return null;
		}
		case EnumActivityVariableScope.Global:
			return VarData.GlobalVariables[name];
		case EnumActivityVariableScope.Group:
		{
			string key = callingEntity.WatchedAttributes.GetString("groupCode");
			if (!VarData.GroupVariables.TryGetValue(key, out var value2))
			{
				return null;
			}
			return value2[name];
		}
		case EnumActivityVariableScope.Player:
		{
			string playerUID2 = (callingEntity as EntityPlayer).Player.PlayerUID;
			if (!VarData.PlayerVariables.TryGetValue(playerUID2, out var value))
			{
				return null;
			}
			return value[name];
		}
		case EnumActivityVariableScope.EntityPlayer:
		{
			string playerUID = (callingEntity as EntityPlayer).Player.PlayerUID;
			ITreeAttribute treeAttribute = callingEntity.WatchedAttributes.GetTreeAttribute("variables");
			if (treeAttribute != null)
			{
				return (treeAttribute[playerUID + "-" + name] as StringAttribute)?.value;
			}
			return null;
		}
		default:
			return null;
		}
	}

	public string GetPlayerVariable(string playerUid, string name)
	{
		if (!VarData.PlayerVariables.TryGetValue(playerUid, out var value))
		{
			return null;
		}
		return value[name];
	}

	private void setDefaultVariables(VariableData data, EntityPlayer playerEntity, EntityAgent npcEntity)
	{
		if (!data.PlayerVariables.TryGetValue(playerEntity.PlayerUID, out var value))
		{
			EntityVariables entityVariables = (data.PlayerVariables[playerEntity.PlayerUID] = new EntityVariables());
			value = entityVariables;
		}
		value["characterclass"] = playerEntity.WatchedAttributes.GetString("characterClass");
	}

	public void OnControllerInit(EntityPlayer playerEntity, EntityAgent npcEntity)
	{
		if (VarData == null)
		{
			playerEntity.Api.Logger.Warning("Variable system has not received initial state from server, may produce wrong dialogue for state-dependent cases eg. Treasure Hunter trader.");
			VarData = new VariableData();
		}
		this.OnDialogueControllerInit?.Invoke(VarData, playerEntity, npcEntity);
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		api.Network.GetChannel("variable").SetMessageHandler<VariableData>(onDialogueData);
	}

	private void onDialogueData(VariableData dlgData)
	{
		VarData = dlgData;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.PlayerJoin += Event_PlayerJoin;
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		sapi.Network.GetChannel("variable").SendPacket(VarData, byPlayer);
	}

	private void Event_GameWorldSave()
	{
		sapi.WorldManager.SaveGame.StoreData("dialogueData", SerializerUtil.Serialize(VarData));
	}

	private void Event_SaveGameLoaded()
	{
		VarData = sapi.WorldManager.SaveGame.GetData("dialogueData", new VariableData());
	}
}
