using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.ModDb;

namespace Vintagestory.Server;

public class ServerSystemModHandler : ServerSystem
{
	private ModLoader loader;

	public ServerSystemModHandler(ServerMain server)
		: base(server)
	{
		server.api = new ServerCoreAPI(server);
	}

	public override void OnBeginInitialization()
	{
		server.api.eventapi.OnServerStage(EnumServerRunPhase.Initialization);
	}

	public override void OnLoadAssets()
	{
		List<string> list = new List<string>(server.Config.ModPaths);
		if (server.progArgs.AddModPath != null)
		{
			list.AddRange(server.progArgs.AddModPath);
		}
		loader = new ModLoader(server.api, list, server.progArgs.TraceLog);
		server.api.modLoader = loader;
		if (!server.Config.DisableModSafetyCheck)
		{
			while (ModDbUtil.ModBlockList == null)
			{
				Thread.Sleep(20);
			}
		}
		loader.LoadMods(server.Config.WorldConfig.DisabledMods);
		if (server.SaveGameData.IsNewWorld)
		{
			SaveGame.SetNewWorldConfig(server.SaveGameData, server);
			server.SaveGameData.WillSave(new FastMemoryStream());
		}
		loader.RunModPhase(ModRunPhase.Pre);
		ServerMain.Logger.VerboseDebug("Searching file system (including mods) for asset files");
		server.AssetManager.AddExternalAssets(ServerMain.Logger, loader);
		ServerMain.Logger.VerboseDebug("Finished building index of asset files");
		foreach (KeyValuePair<string, ITranslationService> availableLanguage in Lang.AvailableLanguages)
		{
			availableLanguage.Value.Invalidate();
		}
		Lang.Load(ServerMain.Logger, server.AssetManager, server.Config.ServerLanguage);
		ServerMain.Logger.Notification("Reloaded lang file with mod assets");
		ServerMain.Logger.VerboseDebug("Reloaded lang file with mod assets");
		loader.RunModPhase(ModRunPhase.Start);
		ServerMain.Logger.VerboseDebug("Started mods");
		loader.RunModPhase(ModRunPhase.AssetsLoaded);
	}

	public override void OnFinalizeAssets()
	{
		loader.RunModPhase(ModRunPhase.AssetsFinalize);
	}

	public override void OnBeginConfiguration()
	{
		if (!server.Config.DisableModSafetyCheck)
		{
			TyronThreadPool.QueueTask(async delegate
			{
				await ModDbUtil.GetBlockedModsAsync(ServerMain.Logger);
			});
		}
		server.api.eventapi.OnServerStage(EnumServerRunPhase.Configuration);
	}

	public override void OnBeginModsAndConfigReady()
	{
		loader.RunModPhase(ModRunPhase.Normal);
		server.api.eventapi.OnServerStage(EnumServerRunPhase.LoadGamePre);
	}

	public override void OnBeginWorldReady()
	{
		server.api.eventapi.OnServerStage(EnumServerRunPhase.WorldReady);
	}

	public override void OnBeginGameReady(SaveGame savegame)
	{
		server.api.eventapi.OnServerStage(EnumServerRunPhase.GameReady);
	}

	public override void OnBeginRunGame()
	{
		server.api.eventapi.OnServerStage(EnumServerRunPhase.RunGame);
	}

	public override void OnBeginShutdown()
	{
		server.api.eventapi.OnServerStage(EnumServerRunPhase.Shutdown);
		loader?.Dispose();
	}
}
