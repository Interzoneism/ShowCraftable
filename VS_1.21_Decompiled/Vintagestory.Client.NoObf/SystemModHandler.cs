using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ProperVersion;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.ClientNative;
using Vintagestory.Common;
using Vintagestory.ModDb;

namespace Vintagestory.Client.NoObf;

public class SystemModHandler : ClientSystem
{
	private ModLoader loader;

	public override string Name => "modhandler";

	public SystemModHandler(ClientMain game)
		: base(game)
	{
	}

	public override void OnServerIdentificationReceived()
	{
		if (game.IsSingleplayer)
		{
			return;
		}
		List<string> list = new List<string>(ClientSettings.ModPaths);
		if (ScreenManager.ParsedArgs.AddModPath != null)
		{
			list.AddRange(ScreenManager.ParsedArgs.AddModPath);
		}
		if (game.Connectdata.Host != null)
		{
			string text = Path.Combine(GamePaths.DataPathServerMods, GamePaths.ReplaceInvalidChars(game.Connectdata.Host + "-" + game.Connectdata.Port));
			if (Directory.Exists(text))
			{
				list.Add(text);
			}
		}
		game.Logger.Notification("Loading and pre-starting client side mods...");
		loader = new ModLoader(game.api, list, ScreenManager.ParsedArgs.TraceLog);
		game.api.modLoader = loader;
		List<ModContainer> list2 = game.api.modLoader.LoadModInfos();
		List<string> list3 = new List<string>();
		Dictionary<string, ModId> dictionary = game.ServerMods.ToDictionary((ModId t) => t.Id, (ModId t) => t);
		foreach (ModContainer item in list2)
		{
			if (item.Info != null && dictionary.TryGetValue(item.Info.ModID, out var value) && value.Version != item.Info.Version && value.Id != "game" && value.Id != "creative" && value.Id != "survival")
			{
				list3.Add(value.Id + "@" + item.Info.Version);
			}
		}
		List<ModContainer> mods = game.api.modLoader.DisableAndVerify(list2, list3);
		list3.AddRange(ClientSettings.DisabledMods);
		List<string> list4 = (from mod in mods
			where mod.Info.Side == EnumAppSide.Universal && !mod.Error.HasValue
			select mod.Info.ModID + "@" + mod.Info.NetworkVersion).ToList();
		List<string> first = (from mod in mods
			where mod.Info.Side == EnumAppSide.Universal && mod.Info.RequiredOnServer && !mod.Error.HasValue
			select mod.Info.ModID + "@" + mod.Info.NetworkVersion).ToList();
		List<string> list5 = (from modidver in (from mod in game.ServerMods
				where mod.RequiredOnClient
				select mod.Id + "@" + mod.NetworkVersion).ToList().Except(list4)
			where !modidver.StartsWithOrdinal("game@") && !modidver.StartsWithOrdinal("creative@") && !modidver.StartsWithOrdinal("survival@")
			select game.ServerMods.FirstOrDefault((ModId mod) => mod.Id + "@" + mod.NetworkVersion == modidver) into mod
			select mod.Id + "@" + mod.Version).ToList();
		List<string> list6 = (from text2 in (from modidver in list5
				select mods.FirstOrDefault((ModContainer mod) => mod.Info.ModID + "@" + mod.Info.Version == modidver && mod.Error == ModError.Dependency) into mod
				where mod?.MissingDependencies != null
				select mod).SelectMany((ModContainer mod) => mod.MissingDependencies)
			where text2.StartsWithOrdinal("game@") || text2.StartsWithOrdinal("creative@") || text2.StartsWithOrdinal("survival@")
			select text2.Split("@")[1]).ToList();
		if (list6.Count > 0)
		{
			list6.Sort((string x, string y) => SemVer.Compare(SemVer.Parse(x), SemVer.Parse(y)));
			game.disconnectReason = Lang.Get("disconnect-modrequiresnewerclient", list6[0]);
			game.exitReason = "client<=>server game version mismatch";
			game.DestroyGameSession(gotDisconnected: true);
			return;
		}
		if (list5.Count > 0)
		{
			List<string> list7 = new List<string>();
			foreach (string modid in list5)
			{
				ModContainer modContainer = mods.FirstOrDefault((ModContainer mod) => modid == mod.Info.ModID + "@" + mod.Info.NetworkVersion && mod.Error.HasValue);
				if (modContainer != null)
				{
					list7.Add(modContainer.Info.ModID + "@" + modContainer.Info.Version);
				}
			}
			foreach (string item2 in list7)
			{
				list5.Remove(item2);
			}
			game.Logger.Notification("Disconnected, modded server with lacking mods on the client side. Mods in question: {0}, our available mods: {1}", string.Join(", ", list5), string.Join(", ", list4));
			if (list7.Count > 0)
			{
				game.disconnectReason = Lang.Get("joinerror-modsmissing-modserroring", string.Join(", ", list5).Replace("@", " v"), string.Join(", ", list7).Replace("@", " v"));
			}
			else
			{
				game.disconnectReason = Lang.Get("joinerror-modsmissing", string.Join(", ", list5).Replace("@", " v"));
			}
			game.disconnectAction = "trydownloadmods";
			game.disconnectMissingMods = list5;
			game.DestroyGameSession(gotDisconnected: true);
			return;
		}
		foreach (ModId serverMod in game.ServerMods)
		{
			list3.Remove(serverMod.Id + "@" + serverMod.Version);
		}
		List<string> second = game.ServerMods.Select((ModId mod) => mod.Id + "@" + mod.NetworkVersion).ToList();
		List<string> collection = first.Except(second).ToList();
		list3.AddRange(collection);
		list3.AddRange(game.ServerModIdBlacklist);
		if (game.ServerModIdWhitelist.Count > 0)
		{
			List<string> modWhitelist = game.ServerModIdWhitelist.ToList();
			if (game.ServerModIdWhitelist.Count == 1 && game.ServerModIdWhitelist[0].Contains("game"))
			{
				modWhitelist = new List<string>();
			}
			IEnumerable<string> collection2 = from mod in mods
				where mod.Info.Side == EnumAppSide.Client
				where modWhitelist.All((string serverModId) => !(mod.Info.ModID + "@" + mod.Info.Version).Contains(serverModId))
				select mod.Info.ModID + "@" + mod.Info.Version;
			list3.AddRange(collection2);
		}
		loader.LoadMods(mods, list3);
		CrashReporter.LoadedMods = mods.Where((ModContainer mod) => mod.Enabled).ToList();
		game.textureSize = loader.TextureSize;
		PreStartMods();
		StartMods();
		ReloadExternalAssets();
	}

	internal void SinglePlayerStart()
	{
		List<string> list = new List<string>(ClientSettings.ModPaths);
		if (ScreenManager.ParsedArgs.AddModPath != null)
		{
			list.AddRange(ScreenManager.ParsedArgs.AddModPath);
		}
		game.Logger.Notification("Loading and pre-starting client side mods...");
		loader = new ModLoader(game.api, list, ScreenManager.ParsedArgs.TraceLog);
		game.api.modLoader = loader;
		List<ModContainer> mods = loader.LoadModInfos();
		List<string> list2 = new List<string>();
		list2.AddRange(ClientSettings.DisabledMods);
		List<ModContainer> list3 = loader.DisableAndVerify(mods, list2);
		if (loader.MissingDependencies.Count > 0)
		{
			List<string> list4 = (from modid in loader.MissingDependencies
				where modid.StartsWithOrdinal("game@") || modid.StartsWithOrdinal("creative@") || modid.StartsWithOrdinal("survival@")
				select modid.Split("@")[1]).ToList();
			if (list4.Count > 0)
			{
				list4.Sort((string x, string y) => SemVer.Compare(SemVer.Parse(x), SemVer.Parse(y)));
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine(Lang.Get("disconnect-modrequiresnewerclient", list4[0]));
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(Lang.Get("disconnect-modrequiresnewerclient-sp"));
				game.disconnectReason = stringBuilder.ToString();
				game.disconnectAction = "disconnectSP";
				game.exitReason = "mod requiers newer game version";
				game.DestroyGameSession(gotDisconnected: true);
			}
			else
			{
				game.disconnectReason = Lang.Get("joinerror-modsmissing", string.Join(", ", loader.MissingDependencies).Replace("@", " v"));
				game.disconnectAction = "trydownloadmods";
				game.disconnectMissingMods = loader.MissingDependencies;
				game.DestroyGameSession(gotDisconnected: true);
			}
			return;
		}
		if (!ClientSettings.DisableModSafetyCheck)
		{
			while (ModDbUtil.ModBlockList == null)
			{
				Thread.Sleep(20);
			}
		}
		loader.LoadMods(list3, list2);
		CrashReporter.LoadedMods = list3.Where((ModContainer mod) => mod.Enabled).ToList();
		game.textureSize = loader.TextureSize;
	}

	internal void PreStartMods()
	{
		loader.RunModPhase(ModRunPhase.Pre);
		game.Logger.Notification("Done loading and pre-starting client side mods.");
	}

	internal void ReloadExternalAssets()
	{
		game.Logger.VerboseDebug("Searching file system (including mods) for asset files");
		game.Platform.AssetManager.AddExternalAssets(game.Logger, loader);
		game.Logger.VerboseDebug("Finished the search for asset files");
		foreach (KeyValuePair<string, ITranslationService> availableLanguage in Lang.AvailableLanguages)
		{
			availableLanguage.Value.Invalidate();
		}
		Lang.Load(game.Logger, game.AssetManager, ClientSettings.Language);
		game.Logger.Notification("Reloaded lang file now with mod assets");
		game.Logger.VerboseDebug("Loaded lang file: " + ClientSettings.Language);
	}

	internal void OnAssetsLoaded()
	{
		loader.RunModPhase(ModRunPhase.AssetsLoaded);
	}

	internal override void OnLevelFinalize()
	{
		loader.RunModPhase(ModRunPhase.AssetsFinalize);
	}

	internal void StartMods()
	{
		loader.RunModPhase(ModRunPhase.Start);
	}

	internal void StartModsFully()
	{
		loader.RunModPhase(ModRunPhase.Normal);
	}

	private void onReloadMods(int groupId, CmdArgs args)
	{
	}

	public override void OnBlockTexturesLoaded()
	{
		game.api.Logger.VerboseDebug("Trigger mod event OnBlockTexturesLoaded");
		game.api.eventapi.TriggerBlockTexturesLoaded();
	}

	public override void Dispose(ClientMain game)
	{
		base.Dispose(game);
		loader?.Dispose();
		CrashReporter.LoadedMods.Clear();
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
