using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ServerSystemLoadConfig : ServerSystem
{
	public ServerSystemLoadConfig(ServerMain server)
		: base(server)
	{
		server.EventManager.OnSaveGameLoaded += OnSaveGameLoaded;
	}

	public override int GetUpdateInterval()
	{
		return 100;
	}

	public override void OnServerTick(float dt)
	{
		if (server.ConfigNeedsSaving)
		{
			server.ConfigNeedsSaving = false;
			SaveConfig(server);
		}
	}

	public override void OnBeginConfiguration()
	{
		EnsureConfigExists(server);
		LoadConfig(server);
		if (server.Standalone)
		{
			server.Config.ApplyStartServerArgs(server.Config.WorldConfig);
		}
		else
		{
			server.Config.ApplyStartServerArgs(server.serverStartArgs);
		}
		if (server.Config.Roles == null || server.Config.Roles.Count == 0)
		{
			server.Config.InitializeRoles();
		}
		if (server.Config.LoadedConfigVersion == "1.0")
		{
			server.Config.InitializeRoles();
			SaveConfig(server);
		}
	}

	public static void EnsureConfigExists(ServerMain server)
	{
		string path = "serverconfig.json";
		if (!File.Exists(Path.Combine(GamePaths.Config, path)))
		{
			ServerMain.Logger.Notification("serverconfig.json not found, creating new one");
			GenerateConfig(server);
			SaveConfig(server);
		}
	}

	private void OnSaveGameLoaded()
	{
		ServerConfig config = server.Config;
		ServerWorldMap worldMap = server.WorldMap;
		server.AddChunkColumnToForceLoadedList(worldMap.MapChunkIndex2D(worldMap.ChunkMapSizeX / 2, worldMap.ChunkMapSizeZ / 2));
		PlayerSpawnPos defaultSpawn = server.SaveGameData.DefaultSpawn;
		if (defaultSpawn != null)
		{
			server.AddChunkColumnToForceLoadedList(worldMap.MapChunkIndex2D(defaultSpawn.x / 32, defaultSpawn.z / 32));
		}
		foreach (PlayerRole value in config.RolesByCode.Values)
		{
			if (value.DefaultSpawn != null)
			{
				server.AddChunkColumnToForceLoadedList(worldMap.MapChunkIndex2D(value.DefaultSpawn.x / 32, value.DefaultSpawn.z / 32));
			}
			if (value.ForcedSpawn != null)
			{
				server.AddChunkColumnToForceLoadedList(worldMap.MapChunkIndex2D(value.ForcedSpawn.x / 32, value.ForcedSpawn.z / 32));
			}
		}
	}

	public override void OnBeginRunGame()
	{
		base.OnBeginRunGame();
		if (server.Config.StartupCommands != null)
		{
			ServerMain.Logger.Notification("Running startup commands");
			string[] array = server.Config.StartupCommands.Split(new string[1] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string message in array)
			{
				server.ReceiveServerConsole(message);
			}
		}
	}

	public static void GenerateConfig(ServerMain server)
	{
		server.Config = new ServerConfig();
		server.Config.InitializeRoles();
		if (server.Standalone)
		{
			server.Config.ApplyStartServerArgs(server.Config.WorldConfig);
		}
		else
		{
			server.Config.ApplyStartServerArgs(server.serverStartArgs);
		}
	}

	public static void LoadConfig(ServerMain server)
	{
		//IL_0026: Expected O, but got Unknown
		string path = "serverconfig.json";
		try
		{
			string text = File.ReadAllText(Path.Combine(GamePaths.Config, path));
			server.Config = JsonConvert.DeserializeObject<ServerConfig>(text);
		}
		catch (JsonReaderException ex)
		{
			JsonReaderException e = ex;
			ServerMain.Logger.Error("Failed to read serverconfig.json");
			ServerMain.Logger.Error((Exception)(object)e);
			ServerMain.Logger.StoryEvent("Failed to read serverconfig.json. Did you modify it? See server-main.log for the affected line. Will stop the server.");
			server.Config = new ServerConfig();
			server.Stop("serverconfig.json read error");
			return;
		}
		if (server.Config == null)
		{
			ServerMain.Logger?.Notification("The deserialized serverconfig.json was null? Creating new one.");
			server.Config = new ServerConfig();
			server.Config.InitializeRoles();
			SaveConfig(server);
		}
		if (server.progArgs.WithConfig != null)
		{
			JObject val;
			using (TextReader textReader = new StreamReader(Path.Combine(GamePaths.Config, path)))
			{
				JToken obj = JToken.Parse(textReader.ReadToEnd());
				val = (JObject)(object)((obj is JObject) ? obj : null);
				textReader.Close();
			}
			JToken obj2 = JToken.Parse(server.progArgs.WithConfig);
			JObject val2 = (JObject)(object)((obj2 is JObject) ? obj2 : null);
			((JContainer)val).Merge((object)val2);
			server.Config = ((JToken)val).ToObject<ServerConfig>();
			SaveConfig(server);
		}
		Logger.LogFileSplitAfterLine = server.Config.LogFileSplitAfterLine;
		if (server.progArgs.MaxClients != null && int.TryParse(server.progArgs.MaxClients, out var result))
		{
			server.Config.MaxClientsProgArgs = result;
		}
	}

	public static void SaveConfig(ServerMain server)
	{
		if (server.Standalone)
		{
			server.Config.FileEditWarning = "";
		}
		else
		{
			server.Config.FileEditWarning = "PLEASE NOTE: This file is also loaded when you start a single player world. If you want to run a dedicated server without affecting single player, we recommend you install the game into a different folder and run the server from there.";
		}
		server.Config.Save();
	}
}
