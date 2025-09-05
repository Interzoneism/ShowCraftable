using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server;

[ProtoContract]
public class SaveGame : ISaveGame
{
	private enum EnumPlayStyle
	{
		WildernessSurvival,
		SurviveAndBuild,
		SurviveAndAutomate,
		CreativeBuilding
	}

	[ProtoMember(1, IsRequired = false)]
	public int MapSizeX;

	[ProtoMember(2, IsRequired = false)]
	public int MapSizeY;

	[ProtoMember(3, IsRequired = false)]
	public int MapSizeZ;

	[ProtoMember(4, IsRequired = false)]
	[Obsolete("Now stored in gamedatabase")]
	public Dictionary<string, ServerWorldPlayerData> PlayerDataByUID;

	[ProtoMember(7, IsRequired = false)]
	public int Seed;

	[ProtoMember(8, IsRequired = false)]
	public long SimulationCurrentFrame;

	[ProtoMember(10, IsRequired = false)]
	public long LastEntityId;

	[ProtoMember(11, IsRequired = false)]
	public ConcurrentDictionary<string, byte[]> ModData;

	[ProtoMember(12, IsRequired = false)]
	public long TotalGameSeconds;

	[Obsolete("Replaced with TimeSpeedModifiers")]
	[ProtoMember(19, IsRequired = false)]
	public int GameTimeSpeed;

	[ProtoMember(13, IsRequired = false)]
	public string WorldName;

	[ProtoMember(14, IsRequired = false)]
	public int TotalSecondsPlayed;

	[Obsolete("Replaced with string playstyle")]
	[ProtoMember(16, IsRequired = false)]
	private EnumPlayStyle WorldPlayStyle;

	[ProtoMember(20, IsRequired = false)]
	public int MiniDimensionsCreated;

	[ProtoMember(17, IsRequired = false)]
	public string LastPlayed;

	[ProtoMember(18, IsRequired = false)]
	public string CreatedGameVersion;

	[ProtoMember(33, IsRequired = false)]
	[Obsolete]
	public int LastBlockItemMappingVersion;

	[ProtoMember(21, IsRequired = false)]
	public string LastSavedGameVersion;

	[ProtoMember(22, IsRequired = false)]
	public string CreatedByPlayerName;

	[ProtoMember(23, IsRequired = false)]
	public bool EntitySpawning;

	[ProtoMember(25, IsRequired = false)]
	public float HoursPerDay = 24f;

	[ProtoMember(26, IsRequired = false)]
	public long LastHerdId;

	[ProtoMember(27, IsRequired = false)]
	public List<LandClaim> LandClaims = new List<LandClaim>();

	[ProtoMember(28, IsRequired = false)]
	public Dictionary<string, float> TimeSpeedModifiers;

	[ProtoMember(29, IsRequired = false)]
	public string PlayStyle;

	[ProtoMember(32, IsRequired = false)]
	public string PlayStyleLangCode;

	[ProtoMember(30, IsRequired = false)]
	public string WorldType;

	[ProtoMember(31, IsRequired = false)]
	private byte[] WorldConfigBytes;

	[ProtoMember(34, IsRequired = false)]
	public string SavegameIdentifier;

	[ProtoMember(35, IsRequired = false)]
	public float CalendarSpeedMul;

	[ProtoMember(36, IsRequired = false)]
	public Dictionary<string, bool> RemappingsAppliedByCode = new Dictionary<string, bool>();

	[ProtoMember(37, IsRequired = false)]
	public int HighestChunkdataVersion;

	[ProtoMember(38, IsRequired = false)]
	public long TotalGameSecondsStart;

	[ProtoMember(39, IsRequired = false)]
	public int CreatedWorldGenVersion;

	public ITreeAttribute WorldConfiguration = new TreeAttribute();

	public bool IsNewWorld;

	[ProtoMember(40, IsRequired = false)]
	public PlayerSpawnPos DefaultSpawn { get; set; }

	string ISaveGame.PlayStyle
	{
		get
		{
			return PlayStyle;
		}
		set
		{
			PlayStyle = value;
		}
	}

	string ISaveGame.WorldType
	{
		get
		{
			return WorldType;
		}
		set
		{
			WorldType = value;
		}
	}

	bool ISaveGame.IsNew => IsNewWorld;

	int ISaveGame.Seed
	{
		get
		{
			return Seed;
		}
		set
		{
			Seed = value;
		}
	}

	long ISaveGame.TotalGameSeconds
	{
		get
		{
			return TotalGameSeconds;
		}
		set
		{
			TotalGameSeconds = value;
		}
	}

	string ISaveGame.WorldName
	{
		get
		{
			return WorldName;
		}
		set
		{
			WorldName = value;
		}
	}

	bool ISaveGame.EntitySpawning
	{
		get
		{
			return EntitySpawning;
		}
		set
		{
			EntitySpawning = value;
		}
	}

	List<LandClaim> ISaveGame.LandClaims => LandClaims;

	ITreeAttribute ISaveGame.WorldConfiguration => WorldConfiguration;

	string ISaveGame.CreatedGameVersion => CreatedGameVersion;

	string ISaveGame.LastSavedGameVersion => LastSavedGameVersion;

	string ISaveGame.SavegameIdentifier => SavegameIdentifier;

	public static SaveGame CreateNew(ServerConfig config)
	{
		SaveGame saveGame = new SaveGame();
		StartServerArgs worldConfig = config.WorldConfig;
		saveGame.ModData = new ConcurrentDictionary<string, byte[]>();
		saveGame.Seed = ((!config.Seed.HasValue) ? new Random(Guid.NewGuid().GetHashCode()).Next() : config.Seed.Value);
		saveGame.IsNewWorld = true;
		saveGame.SavegameIdentifier = Guid.NewGuid().ToString();
		saveGame.MapSizeX = Math.Min(config.MapSizeX, 67108864);
		saveGame.MapSizeY = Math.Min(config.MapSizeY, 16384);
		saveGame.MapSizeZ = Math.Min(config.MapSizeZ, 67108864);
		saveGame.EntitySpawning = true;
		saveGame.CalendarSpeedMul = 0.5f;
		saveGame.LastBlockItemMappingVersion = GameVersion.BlockItemMappingVersion;
		saveGame.TimeSpeedModifiers = new Dictionary<string, float> { { "baseline", 60f } };
		saveGame.WorldName = worldConfig.WorldName;
		saveGame.WorldType = worldConfig.WorldType;
		saveGame.PlayStyle = worldConfig.PlayStyle;
		saveGame.PlayStyleLangCode = worldConfig.PlayStyleLangCode;
		saveGame.CreatedByPlayerName = worldConfig.CreatedByPlayerName;
		saveGame.LastHerdId = 1L;
		saveGame.CreatedWorldGenVersion = 3;
		saveGame.LandClaims = new List<LandClaim>();
		saveGame.LastPlayed = DateTime.Now.ToString("O");
		saveGame.CreatedGameVersion = "1.21.0";
		saveGame.LastSavedGameVersion = "1.21.0";
		return saveGame;
	}

	public static void SetNewWorldConfig(SaveGame savegame, ServerMain server)
	{
		ServerConfig config = server.Config;
		WorldConfig worldConfig = new WorldConfig(server.Api.ModLoader.Mods.Select((Mod m) => (ModContainer)m).ToList());
		worldConfig.selectPlayStyle(server.Config.WorldConfig.PlayStyle);
		if (config.WorldConfig.WorldConfiguration.Token.HasValues)
		{
			Dictionary<string, WorldConfigurationValue> dictionary = new Dictionary<string, WorldConfigurationValue>();
			worldConfig.loadWorldConfigValues(config.WorldConfig.WorldConfiguration, dictionary);
			worldConfig.updateJWorldConfigFrom(dictionary);
		}
		savegame.WorldConfiguration = worldConfig.Jworldconfig.ToAttribute() as TreeAttribute;
		int num = Math.Max(1, savegame.WorldConfiguration.GetAsInt("daysPerMonth", 12));
		savegame.TotalGameSecondsStart = (savegame.TotalGameSeconds = 28800 + 86400 * num * 4);
		if (savegame.WorldConfiguration != null && savegame.WorldConfiguration.HasAttribute("worldWidth"))
		{
			savegame.MapSizeX = Math.Min(savegame.WorldConfiguration.GetString("worldWidth").ToInt(config.MapSizeX), 67108864);
		}
		if (savegame.WorldConfiguration != null && savegame.WorldConfiguration.HasAttribute("worldLength"))
		{
			savegame.MapSizeZ = Math.Min(savegame.WorldConfiguration.GetString("worldLength").ToInt(config.MapSizeZ), 67108864);
		}
	}

	public DateTime GetLastPlayed()
	{
		return DateTime.ParseExact(LastPlayed, "O", GlobalConstants.DefaultCultureInfo);
	}

	internal void UpdateChunkdataVersion()
	{
		if (2 > HighestChunkdataVersion)
		{
			HighestChunkdataVersion = 2;
		}
	}

	public void Init(ServerMain server)
	{
		if (LastSavedGameVersion == null)
		{
			LastSavedGameVersion = CreatedGameVersion;
		}
		if (TimeSpeedModifiers == null)
		{
			TimeSpeedModifiers = new Dictionary<string, float> { { "baseline", 60f } };
		}
		server.PlayersByUid.Clear();
		LoadWorldConfig();
		UpdateWorldConfig(server);
	}

	private void UpdateWorldConfig(ServerMain server)
	{
		if (GameVersion.IsLowerVersionThan(LastSavedGameVersion, "1.21.0-dev.1"))
		{
			WorldConfiguration.SetBool("allowFallingBlocks", server.Config.AllowFallingBlocks);
			WorldConfiguration.SetBool("allowFireSpread", server.Config.AllowFireSpread);
		}
	}

	public void LoadWorldConfig()
	{
		if (WorldConfigBytes == null)
		{
			WorldConfiguration = new TreeAttribute();
		}
		else
		{
			using MemoryStream input = new MemoryStream(WorldConfigBytes);
			using BinaryReader stream = new BinaryReader(input);
			WorldConfiguration = new TreeAttribute();
			WorldConfiguration.FromBytes(stream);
		}
		if (GameVersion.IsLowerVersionThan(LastSavedGameVersion, "1.19.0-pre.6"))
		{
			if (!WorldConfiguration.HasAttribute("upheavelCommonness"))
			{
				WorldConfiguration.SetString("upheavelCommonness", "0.4");
			}
			if (!WorldConfiguration.HasAttribute("landformScale"))
			{
				WorldConfiguration.SetString("landformScale", "1.2");
			}
		}
	}

	[ProtoAfterDeserialization]
	private void afterDeserialization()
	{
		if (PlayStyle == null)
		{
			PlayStyle = WorldPlayStyle.ToString().ToLowerInvariant();
			WorldType = ((WorldPlayStyle == EnumPlayStyle.CreativeBuilding) ? "superflat" : "standard");
		}
		if (LastBlockItemMappingVersion >= 1)
		{
			RemappingsAppliedByCode["game:v1.12clayplanters"] = true;
		}
		if (WorldType == null)
		{
			WorldType = "standard";
		}
		if (PlayStyle == null)
		{
			PlayStyle = "surviveandbuild";
		}
		if (WorldConfiguration == null)
		{
			WorldConfiguration = new TreeAttribute();
		}
		if (SavegameIdentifier == null)
		{
			SavegameIdentifier = Guid.NewGuid().ToString();
		}
		if (GameVersion.IsLowerVersionThan(LastSavedGameVersion, "1.13-pre.1"))
		{
			CalendarSpeedMul = 0.5f;
		}
		if (GameVersion.IsLowerVersionThan(LastSavedGameVersion, "1.17-pre.5"))
		{
			int num = Math.Max(1, WorldConfiguration.GetAsInt("daysPerMonth", 12));
			TotalGameSecondsStart = 28800 + 86400 * num * 4;
		}
	}

	public byte[] GetData(string name)
	{
		if (ModData == null)
		{
			return null;
		}
		if (ModData.TryGetValue(name, out var value))
		{
			return value;
		}
		return null;
	}

	public void StoreData(string name, byte[] value)
	{
		if (ModData == null)
		{
			ModData = new ConcurrentDictionary<string, byte[]>();
		}
		ModData[name] = value;
	}

	public T GetData<T>(string name, T defaultValue = default(T))
	{
		if (ModData == null)
		{
			return defaultValue;
		}
		if (ModData.TryGetValue(name, out var value))
		{
			if (value == null)
			{
				return defaultValue;
			}
			return SerializerUtil.Deserialize<T>(value);
		}
		return defaultValue;
	}

	public void StoreData<T>(string name, T data)
	{
		if (ModData == null)
		{
			ModData = new ConcurrentDictionary<string, byte[]>();
		}
		ModData[name] = SerializerUtil.Serialize(data);
	}

	public SaveGame GetSaveGameForSaving(ServerConfig config)
	{
		return this;
	}

	internal void WillSave(FastMemoryStream ms)
	{
		LastPlayed = DateTime.Now.ToString("O");
		LastSavedGameVersion = "1.21.0";
		ms.Reset();
		using (BinaryWriter stream = new BinaryWriter(ms))
		{
			if (WorldConfiguration == null)
			{
				new TreeAttribute().ToBytes(stream);
			}
			else
			{
				WorldConfiguration.ToBytes(stream);
			}
		}
		WorldConfigBytes = ms.ToArray();
	}

	internal void UpdateLandClaims(List<LandClaim> toSave)
	{
		List<LandClaim> list = new List<LandClaim>();
		for (int i = 0; i < toSave.Count; i++)
		{
			list.Add(toSave[i]);
		}
		LandClaims = list;
	}
}
