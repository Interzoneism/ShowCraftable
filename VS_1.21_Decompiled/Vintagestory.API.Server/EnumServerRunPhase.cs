using System;

namespace Vintagestory.API.Server;

public enum EnumServerRunPhase
{
	Standby = -1,
	Start = 0,
	Initialization = 1,
	Configuration = 2,
	[Obsolete("Use AssetsReady")]
	LoadAssets = 3,
	AssetsReady = 3,
	AssetsFinalize = 4,
	[Obsolete("Use ModsAndConfigReady")]
	LoadGamePre = 5,
	ModsAndConfigReady = 5,
	GameReady = 6,
	[Obsolete("Use GameReady")]
	LoadGame = 6,
	WorldReady = 7,
	RunGame = 8,
	Shutdown = 9,
	Exit = 10
}
