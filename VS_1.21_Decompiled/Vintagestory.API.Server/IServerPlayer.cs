using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Server;

public interface IServerPlayer : IPlayer
{
	int ItemCollectMode { get; set; }

	int CurrentChunkSentRadius { get; set; }

	EnumClientState ConnectionState { get; }

	string IpAddress { get; }

	string LanguageCode { get; }

	float Ping { get; }

	IServerPlayerData ServerData { get; }

	event OnEntityAction InWorldAction;

	void BroadcastPlayerData(bool sendInventory = false);

	void Disconnect();

	void Disconnect(string message);

	void SendIngameError(string code, string message = null, params object[] langparams);

	void SendMessage(int groupId, string message, EnumChatType chatType, string data = null);

	void SendLocalisedMessage(int groupId, string message, params object[] args);

	void SetRole(string roleCode);

	void SetSpawnPosition(PlayerSpawnPos pos);

	void ClearSpawnPosition();

	FuzzyEntityPos GetSpawnPosition(bool consumeSpawnUse);

	void SetModData<T>(string key, T data);

	T GetModData<T>(string key, T defaultValue = default(T));

	void SetModdata(string key, byte[] data);

	void RemoveModdata(string key);

	byte[] GetModdata(string key);
}
