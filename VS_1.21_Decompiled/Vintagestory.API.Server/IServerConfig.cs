using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Server;

public interface IServerConfig
{
	int Port { get; }

	string ServerName { get; set; }

	string WelcomeMessage { get; set; }

	int MaxClients { get; set; }

	string Password { get; set; }

	int MaxChunkRadius { get; set; }

	float TickTime { get; set; }

	int BlockTickChunkRange { get; set; }

	int MaxMainThreadBlockTicks { get; set; }

	int RandomBlockTicksPerChunk { get; set; }

	int BlockTickInterval { get; set; }

	List<IPlayerRole> Roles { get; }

	string DefaultRoleCode { get; set; }

	EnumProtectionLevel AntiAbuse { get; set; }

	EnumWhitelistMode WhitelistMode { get; set; }

	[Obsolete("No longer used. Retrieve value from the savegame instead")]
	PlayerSpawnPos DefaultSpawn { get; set; }

	bool AllowPvP { get; set; }

	bool AllowFireSpread { get; set; }

	bool AllowFallingBlocks { get; set; }

	bool HostedMode { get; set; }

	bool HostedModeAllowMods { get; set; }

	float SpawnCapPlayerScaling { get; set; }

	bool LogBlockBreakPlace { get; set; }

	uint LogFileSplitAfterLine { get; set; }
}
