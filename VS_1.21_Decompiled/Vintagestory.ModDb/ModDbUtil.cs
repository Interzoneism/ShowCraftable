using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.ModDb;

public class ModDbUtil
{
	private string installPath;

	private string modDbApiUrl;

	private string modDbDownloadUrl;

	private ICoreAPI api;

	private string cmdLetter;

	private static short blockModDownloadTries;

	public static Dictionary<string, string> ModBlockList;

	private GameVersionResponse gameversions;

	public int selfGameVersionId = -1;

	public int[] sameMinorVersionIds = Array.Empty<int>();

	private List<ModContainer> mods;

	public bool IsLoading { get; private set; }

	public ModDbUtil(ICoreAPI api, string modDbUrl, string installPath)
	{
		this.api = api;
		modDbApiUrl = modDbUrl + "api/";
		modDbDownloadUrl = modDbUrl;
		this.installPath = installPath;
		cmdLetter = ((api.Side == EnumAppSide.Client) ? "." : "/");
	}

	private void ensureModsLoaded()
	{
		if (mods == null)
		{
			ModLoader modLoader = api.ModLoader as ModLoader;
			mods = modLoader.LoadModInfos();
		}
	}

	public string preConsoleCommand()
	{
		if (gameversions == null)
		{
			string result = null;
			modDbRequest("gameversions", delegate(EnumModDbResponse state, string text)
			{
				switch (state)
				{
				case EnumModDbResponse.Good:
				{
					gameversions = parseResponse<GameVersionResponse>(text, out var errorText);
					if (errorText != null)
					{
						result = errorText;
					}
					else if (gameversions != null)
					{
						loadVersionIds();
						result = null;
					}
					else
					{
						result = "Bad moddb response - no game versions";
					}
					break;
				}
				case EnumModDbResponse.Offline:
					result = "Mod hub offline";
					break;
				default:
					result = "Bad moddb response - " + text;
					break;
				}
			});
			return result;
		}
		return null;
	}

	public void onInstallCommand(string modid, string forGameVersion, Action<string> onProgressUpdate)
	{
		ensureModsLoaded();
		SearchAndInstall(modid, forGameVersion ?? "1.21.0", delegate(string msg, EnumModInstallState state)
		{
			onProgressUpdate(msg);
		}, deletedOutdated: true);
	}

	public void onRemoveCommand(string modid, Action<string> onProgressUpdate)
	{
		ensureModsLoaded();
		foreach (ModContainer mod in mods)
		{
			if (mod.Status != ModStatus.Errored && mod.Info.ModID == modid)
			{
				File.Delete(mod.SourcePath);
				onProgressUpdate("modutil-modremoved");
				return;
			}
		}
		onProgressUpdate("No such mod found.");
	}

	public void onListCommand(Action<string> onProgressUpdate)
	{
		ensureModsLoaded();
		List<string> list = new List<string>();
		foreach (ModContainer mod in mods)
		{
			if (mod.Status != ModStatus.Errored && mod.Info.ModID != "game" && mod.Info.ModID != "creative" && mod.Info.ModID != "survival")
			{
				list.Add(mod.Info.ModID);
			}
		}
		if (list.Count == 0)
		{
			onProgressUpdate(Lang.Get("modutil-list-none"));
			return;
		}
		onProgressUpdate(Lang.Get("modutil-list", list.Count, string.Join(", ", list)));
	}

	public void onSearchforCommand(string version, string modid, Action<string> onProgressUpdate)
	{
		ensureModsLoaded();
		int num = -1;
		GameVersionEntry[] gameVersions = gameversions.GameVersions;
		foreach (GameVersionEntry gameVersionEntry in gameVersions)
		{
			if (gameVersionEntry.Name == "v" + version)
			{
				num = gameVersionEntry.TagId;
			}
		}
		if (num <= 0)
		{
			onProgressUpdate("No such version is listed on the moddb");
			return;
		}
		int[] gameversionIds = new int[1] { num };
		search(modid, onProgressUpdate, gameversionIds);
	}

	public void onSearchforAndCompatibleCommand(string version, string modid, Action<string> onProgressUpdate)
	{
		ensureModsLoaded();
		string text = version.Substring(0, 1);
		string text2 = version.Substring(2, 3);
		List<int> list = new List<int>();
		GameVersionEntry[] gameVersions = gameversions.GameVersions;
		foreach (GameVersionEntry gameVersionEntry in gameVersions)
		{
			if (gameVersionEntry.Name.StartsWithOrdinal("v" + text + "." + text2))
			{
				list.Add(gameVersionEntry.TagId);
			}
		}
		int[] gameversionIds = list.ToArray();
		search(modid, onProgressUpdate, gameversionIds);
	}

	public void onSearchCommand(string modid, Action<string> onProgressUpdate)
	{
		ensureModsLoaded();
		search(modid, onProgressUpdate, new int[1] { selfGameVersionId });
	}

	public void onSearchCompatibleCommand(string modid, Action<string> onProgressUpdate)
	{
		ensureModsLoaded();
		search(modid, onProgressUpdate, sameMinorVersionIds);
	}

	public void SearchAndInstall(string modid, string forGameVersion, ModInstallProgressUpdate onDone, bool deletedOutdated)
	{
		ensureModsLoaded();
		string[] array = modid.Split('@');
		api.Logger.Debug("ModDbUtil.SearchAndInstall(): Request to mod/" + array[0]);
		modDbRequest("mod/" + array[0], delegate(EnumModDbResponse state, string text)
		{
			api.Logger.Debug("ModDbUtil.SearchAndInstall(): Response: {0}", text);
			switch (state)
			{
			case EnumModDbResponse.Good:
			{
				string errorText;
				ModDbEntryResponse modDbEntryResponse = parseResponse<ModDbEntryResponse>(text, out errorText);
				if (errorText != null)
				{
					if (modDbEntryResponse != null && modDbEntryResponse.StatusCode == 404)
					{
						onDone(Lang.Get("modinstall-notfound", modid), EnumModInstallState.NotFound);
					}
					else
					{
						onDone(errorText, EnumModInstallState.Error);
					}
				}
				else if (api is ICoreServerAPI coreServerAPI && coreServerAPI.Server.Config.HostedMode)
				{
					if (coreServerAPI.Server.Config.HostedModeAllowMods && modDbEntryResponse.Mod.Releases.Any((ModEntryRelease r) => r.HostedModeAllow))
					{
						modDbEntryResponse.Mod.Releases = modDbEntryResponse.Mod.Releases.Where((ModEntryRelease r) => r.HostedModeAllow).ToArray();
						installMod(modDbEntryResponse, onDone, forGameVersion, deletedOutdated, modid);
					}
					else
					{
						onDone(Lang.Get("modinstall-notallowed", modid), EnumModInstallState.Error);
					}
				}
				else
				{
					installMod(modDbEntryResponse, onDone, forGameVersion, deletedOutdated, modid);
				}
				break;
			}
			case EnumModDbResponse.Offline:
				onDone(Lang.Get("modinstall-offline", modid), EnumModInstallState.Offline);
				break;
			default:
				onDone(Lang.Get("modinstall-badresponse", modid, text), EnumModInstallState.Error);
				break;
			}
		});
	}

	private void loadVersionIds()
	{
		List<int> list = new List<int>();
		string text = "1.21.0".Substring(0, 1);
		string text2 = "1.21.0".Substring(2, 3);
		string text3 = "v1.21.0";
		string b = "v" + text + "." + text2;
		GameVersionEntry[] gameVersions = gameversions.GameVersions;
		foreach (GameVersionEntry gameVersionEntry in gameVersions)
		{
			if (gameVersionEntry.Name == text3)
			{
				selfGameVersionId = gameVersionEntry.TagId;
			}
			if (gameVersionEntry.Name.StartsWithOrdinal(b))
			{
				list.Add(gameVersionEntry.TagId);
			}
		}
		sameMinorVersionIds = list.ToArray();
	}

	private void search(string stext, Action<string> onDone, int[] gameversionIds)
	{
		if (stext == null)
		{
			onDone("Syntax: " + cmdLetter + "moddb search [text]");
			return;
		}
		Search(stext, delegate(ModSearchResult searchResult)
		{
			if (searchResult.Mods == null)
			{
				onDone(searchResult.StatusMessage);
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder();
				if (searchResult.Mods.Length == 0)
				{
					stringBuilder.AppendLine(Lang.Get("Found no mods compatible for your game version"));
				}
				else
				{
					stringBuilder.AppendLine(Lang.Get("Found {0} compatible mods. Name and mod id:", searchResult.Mods.Length));
				}
				int num = 0;
				ModDbEntrySearchResponse[] array = searchResult.Mods;
				foreach (ModDbEntrySearchResponse modDbEntrySearchResponse in array)
				{
					stringBuilder.AppendLine(Lang.Get("{0}: <strong>{1}</strong>", modDbEntrySearchResponse.Name, modDbEntrySearchResponse.ModIdStrs[0]));
					num++;
					if (num > 10)
					{
						stringBuilder.AppendLine("and more...");
						break;
					}
				}
				onDone(stringBuilder.ToString());
			}
		}, gameversionIds);
	}

	public void Search(string stext, Action<ModSearchResult> onDone, int[] gameversionIds, string mv = null, string sortBy = null, int limit = 100)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < gameversionIds.Length; i++)
		{
			int num = gameversionIds[i];
			if (num != -1)
			{
				list.Add("gv[]=" + num);
			}
		}
		list.Add("text=" + stext);
		if (mv != null)
		{
			list.Add("mv=" + mv);
		}
		if (sortBy != null)
		{
			list.Add("sortby=" + sortBy);
		}
		list.Add("limit=" + limit);
		modDbRequest("mods?" + string.Join("&", list), delegate(EnumModDbResponse state, string text)
		{
			switch (state)
			{
			case EnumModDbResponse.Good:
			{
				string errorText;
				ModSearchResult modSearchResult = parseResponse<ModSearchResult>(text, out errorText);
				if (errorText != null)
				{
					onDone(new ModSearchResult
					{
						StatusCode = 500,
						StatusMessage = errorText
					});
				}
				else
				{
					modSearchResult.Mods = modSearchResult.Mods.Where((ModDbEntrySearchResponse m) => m.Type.Equals("mod")).ToArray();
					onDone(modSearchResult);
				}
				break;
			}
			case EnumModDbResponse.Offline:
				onDone(new ModSearchResult
				{
					StatusCode = 500,
					StatusMessage = Lang.Get("Mod hub offline")
				});
				break;
			default:
				onDone(new ModSearchResult
				{
					StatusCode = 500,
					StatusMessage = Lang.Get("Bad moddb response - {0}", text)
				});
				break;
			}
		});
	}

	private void installMod(ModDbEntryResponse modentry, ModInstallProgressUpdate onProgressUpdate, string forGameVersion, bool deleteOutdated, string installExactVer = null)
	{
		ModEntryRelease modEntryRelease = null;
		string text = null;
		if (installExactVer != null)
		{
			string[] array = installExactVer?.Split('@');
			text = ((array.Length > 1) ? array[1] : null);
			onProgressUpdate(Lang.Get("Checking {0}...", installExactVer) + " ", EnumModInstallState.InProgress);
		}
		else
		{
			onProgressUpdate(Lang.Get("Checking {0}...", modentry.Mod.Name) + " ", EnumModInstallState.InProgress);
		}
		if (text != null && text != "*")
		{
			ModEntryRelease[] releases = modentry.Mod.Releases;
			foreach (ModEntryRelease modEntryRelease2 in releases)
			{
				if (modEntryRelease2.ModVersion == text)
				{
					modEntryRelease = modEntryRelease2;
				}
			}
			if (modEntryRelease == null)
			{
				onProgressUpdate(Lang.Get("modinstall-versionnotfound", modentry.Mod.Name, text), EnumModInstallState.NotFound);
				return;
			}
		}
		else
		{
			List<ModEntryRelease> list = new List<ModEntryRelease>();
			HashSet<string> hashSet = new HashSet<string>();
			ModEntryRelease[] releases = modentry.Mod.Releases;
			foreach (ModEntryRelease modEntryRelease3 in releases)
			{
				if (modEntryRelease3.Tags.Contains(forGameVersion) || modEntryRelease3.Tags.Contains<string>("v" + forGameVersion))
				{
					list.Add(modEntryRelease3);
				}
				string[] tags = modEntryRelease3.Tags;
				foreach (string text2 in tags)
				{
					hashSet.Add(text2.Substring(1));
				}
			}
			if (list.Count == 0)
			{
				onProgressUpdate(Lang.Get("mod-outdated-notavailable", string.Join(", ", hashSet), cmdLetter, modentry.Mod.Releases[0].ModIdStr), EnumModInstallState.TooOld);
				return;
			}
			list.Sort((ModEntryRelease mod1, ModEntryRelease mod2) => (!(mod1.ModVersion == mod2.ModVersion)) ? ((!GameVersion.IsNewerVersionThan(mod1.ModVersion, mod2.ModVersion)) ? 1 : (-1)) : 0);
			modEntryRelease = list[0];
		}
		foreach (ModContainer mod in mods)
		{
			if (mod.Enabled && mod.Info.ModID == modEntryRelease.ModIdStr)
			{
				if (mod.Info.Version == modEntryRelease.ModVersion)
				{
					onProgressUpdate(Lang.Get("mod-installed-willenable"), EnumModInstallState.InstalledOrReady);
					List<string> disabledMods = ClientSettings.DisabledMods;
					disabledMods.Remove(mod.Info.ModID + "@" + mod.Info.Version);
					ClientSettings.DisabledMods = disabledMods;
					ClientSettings.Inst.Save(force: true);
					return;
				}
				if (deleteOutdated)
				{
					onProgressUpdate(Lang.Get("{0} v{1} is already installed, which is outdated. Will delete it.", modentry.Mod.Name, mod.Info.Version), EnumModInstallState.InstalledOrReady);
					File.Delete(mod.SourcePath);
				}
			}
		}
		onProgressUpdate(Lang.GetWithFallback("mod-found-downloading", "found! Downloading..."), EnumModInstallState.InProgress);
		Console.WriteLine(Lang.Get("Downloading {0}...", modEntryRelease.Filename) + " ");
		string text3 = Path.Combine(installPath, modEntryRelease.Filename);
		GamePaths.EnsurePathExists(installPath);
		try
		{
			using Stream stream = VSWebClient.Inst.GetStreamAsync(new Uri(modDbDownloadUrl + "download?fileid=" + modEntryRelease.Fileid)).Result;
			using FileStream destination = new FileStream(text3, FileMode.Create);
			stream.CopyTo(destination);
			onProgressUpdate(Lang.Get("mod-successfully-downloaded", new FileInfo(text3).Length / 1024), EnumModInstallState.InstalledOrReady);
		}
		catch (Exception ex)
		{
			onProgressUpdate("Failed to download mod " + modEntryRelease.Filename + " | " + ex.Message, EnumModInstallState.Error);
		}
	}

	private void modDbRequest(string action, ModDbResponseDelegate onComplete, FormUrlEncodedContent postData = null)
	{
		IsLoading = true;
		Uri uri = new Uri(modDbApiUrl + action);
		api.Logger.Notification("Send request: {0}", action);
		VSWebClient.Inst.PostAsync(uri, postData, delegate(CompletedArgs args)
		{
			api.Event.EnqueueMainThreadTask(delegate
			{
				if (args.State != CompletionState.Good)
				{
					onComplete(EnumModDbResponse.Offline, null);
				}
				else if (args.Response == null)
				{
					onComplete(EnumModDbResponse.Bad, null);
				}
				else
				{
					IsLoading = false;
					onComplete(EnumModDbResponse.Good, args.Response);
				}
			}, "moddbrequest");
		});
	}

	public T parseResponse<T>(string text, out string errorText) where T : ModDbResponse
	{
		errorText = null;
		T val;
		try
		{
			val = JsonConvert.DeserializeObject<T>(text);
		}
		catch (Exception ex)
		{
			api.Logger.Notification("{0}", ex);
			errorText = LoggerBase.CleanStackTrace(ex.ToString());
			return null;
		}
		if (val.StatusCode != 200)
		{
			errorText = "Invalid request - " + val.StatusCode;
		}
		return val;
	}

	public static async Task GetBlockedModsAsync(ILogger logger)
	{
		if (ModBlockList != null)
		{
			return;
		}
		try
		{
			blockModDownloadTries++;
			ModBlockList = JsonConvert.DeserializeObject<ModBlock[]>(await VSWebClient.Inst.GetStringAsync("https://cdn.vintagestory.at/api/blockedmods.json")).ToDictionary((ModBlock b) => b.Id, (ModBlock b) => b.reason);
		}
		catch (Exception e)
		{
			logger.Warning("Could not get blocked mods from api");
			logger.Warning(e);
			if (blockModDownloadTries < 2)
			{
				logger.Notification("Trying again to get blocked mods list ...");
				Thread.Sleep(100);
				await GetBlockedModsAsync(logger);
				return;
			}
		}
		if (ModBlockList == null)
		{
			ModBlockList = new Dictionary<string, string>();
		}
	}

	public static bool IsModBlocked(string modId, string version, out string reason)
	{
		if (ModBlockList.TryGetValue(modId + "@" + version, out reason))
		{
			return true;
		}
		return ModBlockList.TryGetValue(modId ?? "", out reason);
	}
}
