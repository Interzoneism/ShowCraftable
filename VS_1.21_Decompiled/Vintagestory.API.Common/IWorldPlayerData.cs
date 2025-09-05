namespace Vintagestory.API.Common;

public interface IWorldPlayerData
{
	string PlayerUID { get; }

	EntityPlayer EntityPlayer { get; }

	EntityControls EntityControls { get; }

	int LastApprovedViewDistance { get; set; }

	int DesiredViewDistance { get; set; }

	EnumGameMode CurrentGameMode { get; set; }

	bool FreeMove { get; set; }

	EnumFreeMovAxisLock FreeMovePlaneLock { get; set; }

	bool NoClip { get; set; }

	float MoveSpeedMultiplier { get; set; }

	float PickingRange { get; set; }

	bool AreaSelectionMode { get; set; }

	int Deaths { get; }

	void SetModdata(string key, byte[] data);

	void RemoveModdata(string key);

	byte[] GetModdata(string key);

	void SetModData<T>(string key, T data);

	T GetModData<T>(string key, T defaultValue = default(T));
}
