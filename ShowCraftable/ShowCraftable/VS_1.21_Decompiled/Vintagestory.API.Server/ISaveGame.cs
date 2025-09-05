using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Server;

public interface ISaveGame
{
	bool IsNew { get; }

	string CreatedGameVersion { get; }

	string LastSavedGameVersion { get; }

	int Seed { get; set; }

	string SavegameIdentifier { get; }

	long TotalGameSeconds { get; set; }

	string WorldName { get; set; }

	string PlayStyle { get; set; }

	string WorldType { get; set; }

	bool EntitySpawning { get; set; }

	[Obsolete("Use sapi.WorldManager.LandClaims instead.  ISaveGame.LandClaims will be removed in 1.22")]
	List<LandClaim> LandClaims { get; }

	ITreeAttribute WorldConfiguration { get; }

	PlayerSpawnPos DefaultSpawn { get; set; }

	byte[] GetData(string key);

	void StoreData(string key, byte[] data);

	T GetData<T>(string key, T defaultValue = default(T));

	void StoreData<T>(string key, T data);
}
