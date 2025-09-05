using System;
using System.Collections.Generic;

namespace Vintagestory.API.Server;

public interface IPlayerDataManager
{
	Dictionary<string, IServerPlayerData> PlayerDataByUid { get; }

	IServerPlayerData GetPlayerDataByUid(string playerUid);

	IServerPlayerData GetPlayerDataByLastKnownName(string name);

	void ResolvePlayerName(string playername, Action<EnumServerResponse, string> onPlayerReceived);

	void ResolvePlayerUid(string playeruid, Action<EnumServerResponse, string> onPlayerReceived);
}
