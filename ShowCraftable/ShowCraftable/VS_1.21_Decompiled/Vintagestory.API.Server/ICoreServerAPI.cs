using System;
using Vintagestory.API.Common;

namespace Vintagestory.API.Server;

public interface ICoreServerAPI : ICoreAPI, ICoreAPICommon
{
	new IServerEventAPI Event { get; }

	IWorldManagerAPI WorldManager { get; }

	IServerAPI Server { get; }

	IPermissionManager Permissions { get; }

	IGroupManager Groups { get; }

	IPlayerDataManager PlayerData { get; }

	new IServerNetworkAPI Network { get; }

	new IServerWorldAccessor World { get; }

	void SendIngameError(IServerPlayer player, string errorCode, string text = null, params object[] langparams);

	void SendIngameDiscovery(IServerPlayer player, string discoveryCode, string text = null, params object[] langparams);

	void SendMessage(IPlayer player, int groupId, string message, EnumChatType chatType, string data = null);

	void SendMessageToGroup(int groupid, string message, EnumChatType chatType, string data = null);

	void BroadcastMessageToAllGroups(string message, EnumChatType chatType, string data = null);

	void InjectConsole(string message);

	[Obsolete("Use ChatCommand subapi instead")]
	void HandleCommand(IServerPlayer player, string message);

	void RegisterItem(Item item);

	void RegisterBlock(Block block);

	void RegisterCraftingRecipe(GridRecipe recipe);

	void RegisterTreeGenerator(AssetLocation generatorCode, ITreeGenerator gen);

	void RegisterTreeGenerator(AssetLocation generatorCode, GrowTreeDelegate genhandler);

	[Obsolete("Use ChatCommand subapi instead")]
	bool RegisterCommand(ServerChatCommand chatcommand);

	[Obsolete("Use ChatCommand subapi instead")]
	bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ServerChatCommandDelegate handler, string requiredPrivilege = null);

	void TriggerOnAssetsFirstLoaded();
}
