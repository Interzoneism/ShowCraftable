using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;
using Vintagestory.Server.Database;

namespace Vintagestory.Server;

internal class ServerSystemLoadAndSaveGame : ServerSystem, IChunkProviderThread
{
	private ChunkServerThread chunkthread;

	private object savingLock = new object();

	[ThreadStatic]
	private static FastMemoryStream reusableMemoryStream;

	private BlockAccessorWorldGen blockAccessorWG;

	private BlockAccessorWorldGenUpdateHeightmap blockAccessorWGUpdateHeightMap;

	private Dictionary<long, ServerChunk> chunksCopy = new Dictionary<long, ServerChunk>();

	private bool ignoreSave;

	internal static PlayerSpawnPos SetDefaultSpawnOnce;

	private FastMemoryStream reusableStream => reusableMemoryStream ?? (reusableMemoryStream = new FastMemoryStream());

	public ServerSystemLoadAndSaveGame(ServerMain server, ChunkServerThread chunkthread)
		: base(server)
	{
		this.chunkthread = chunkthread;
		chunkthread.loadsavegame = this;
	}

	public override void OnSeparateThreadTick()
	{
		if (!chunkthread.runOffThreadSaveNow)
		{
			return;
		}
		lock (savingLock)
		{
			if (chunkthread.runOffThreadSaveNow)
			{
				int num = SaveAllDirtyLoadedChunks(isSaveLater: true, reusableStream);
				ServerMain.Logger.Event("Offthread save of {0} chunks done.", num);
				num = SaveAllDirtyGeneratingChunks(reusableStream);
				ServerMain.Logger.Notification("Offthread save of {0} generating chunks done.", num);
				int num2 = SaveAllDirtyMapChunks(reusableStream);
				ServerMain.Logger.Event("Offthread save of {0} map chunks done.", num2);
				server.SaveGameData.UpdateLandClaims(server.WorldMap.All);
				chunkthread.gameDatabase.StoreSaveGame(server.SaveGameData, reusableStream);
				ServerMain.Logger.Event("Offthread save of savegame done.");
				chunkthread.runOffThreadSaveNow = false;
			}
		}
	}

	public override void OnFinalizeAssets()
	{
		server.SaveGameData.WillSave(reusableStream);
		server.SaveGameData.UpdateLandClaims(server.WorldMap.All);
		chunkthread.gameDatabase.StoreSaveGame(server.SaveGameData, reusableStream);
	}

	public override void OnBeginConfiguration()
	{
		chunkthread.gameDatabase = new GameDatabase(ServerMain.Logger);
		string errorMessage = null;
		bool flag = File.Exists(server.GetSaveFilename());
		bool isReadonly = false;
		int foundVersion;
		try
		{
			server.SaveGameData = chunkthread.gameDatabase.ProbeOpenConnection(server.GetSaveFilename(), corruptionProtection: true, out foundVersion, out errorMessage, out isReadonly);
		}
		catch (Exception e)
		{
			ServerMain.Logger.Fatal("Unable to open or create savegame.");
			ServerMain.Logger.Fatal(e);
			server.Stop("Failed opening savegame");
			return;
		}
		if (server.SaveGameData == null && flag && server.Config.RepairMode)
		{
			chunkthread.gameDatabase.CloseConnection();
			chunkthread.gameDatabase.OpenConnection(server.GetSaveFilename(), GameVersion.DatabaseVersion, out errorMessage, requireWriteAccess: true, server.Config.CorruptionProtection, server.Config.RepairMode);
			ServerMain.Logger.Fatal("Failed opening savegame data, possibly corrupted. We are in repair mode, so initializing new savegame data structure.", errorMessage);
			server.SaveGameData = SaveGame.CreateNew(server.Config);
			server.SaveGameData.WorldType = "standard";
			server.SaveGameData.PlayStyle = "surviveandbuild";
			server.SaveGameData.PlayStyleLangCode = "preset-surviveandbuild";
			foundVersion = GameVersion.DatabaseVersion;
		}
		else if (server.Config.RepairMode)
		{
			chunkthread.gameDatabase.IntegrityCheck();
		}
		if (server.SaveGameData == null && flag)
		{
			server.SaveGameData = null;
			ServerMain.Logger.Fatal("Failed opening savegame, possibly corrupted. Error Message: {0}. Will exit server now.", errorMessage);
			server.Stop("Failed opening savegame");
			return;
		}
		if (isReadonly)
		{
			server.SaveGameData = null;
			chunkthread.gameDatabase.CloseConnection();
			ServerMain.Logger.Fatal("Failed opening savegame, have no write access to it. Make sure no other server is accessing it. Will exit server now.");
			server.Stop("Failed opening savegame, it is readonly");
			return;
		}
		try
		{
			FileInfo fileInfo = new FileInfo(chunkthread.gameDatabase.DatabaseFilename);
			long freeDiskSpace = ServerMain.xPlatInterface.GetFreeDiskSpace(fileInfo.DirectoryName);
			if (freeDiskSpace >= 0 && freeDiskSpace < 1048576 * server.Config.DieBelowDiskSpaceMb)
			{
				string message = $"Disk space is below {server.Config.DieBelowDiskSpaceMb} megabytes ({freeDiskSpace / 1024 / 1024} mb left). A full harddisk can heavily corrupt a savegame. Please free up more disk space or adjust the threshold in the serverconfig.json (or set to -1 to disable this check). Will kill server now...";
				ServerMain.Logger.Fatal(message);
				throw new Exception(message);
			}
		}
		catch (ArgumentException)
		{
			ServerMain.Logger.Warning("Exception thrown when trying to check for available disk space. Please manually verify that your hard disk won't run full to avoid savegame corruption");
		}
		if (foundVersion != GameVersion.DatabaseVersion)
		{
			chunkthread.gameDatabase.CloseConnection();
			ServerMain.Logger.Event("Old savegame database version detected, will upgrade now...");
			DatabaseUpgrader databaseUpgrader = new DatabaseUpgrader(server, server.GetSaveFilename(), foundVersion, GameVersion.DatabaseVersion);
			try
			{
				databaseUpgrader.PerformUpgrade();
				chunkthread.gameDatabase.OpenConnection(server.GetSaveFilename(), corruptionProtection: true, doIntegrityCheck: true);
				server.SaveGameData = null;
			}
			catch (Exception innerException)
			{
				ServerMain.Logger.Event("Failed upgrading old savegame, giving up, sorry.");
				throw new InvalidDataException("Failed upgrading savegame {0}", innerException);
			}
		}
		chunkthread.gameDatabase.UpgradeToWriteAccess();
		server.ModEventManager.OnWorldgenStartup += OnWorldgenStartup;
		LoadSaveGame();
	}

	public override void OnBeginModsAndConfigReady()
	{
		if (server.SaveGameData.IsNewWorld)
		{
			server.ModEventManager.TriggerSaveGameCreated();
		}
		server.EventManager.TriggerSaveGameLoaded();
		server.WorldMap.chunkIlluminatorWorldGen.chunkProvider = chunkthread;
		chunkthread.worldgenBlockAccessor = GetBlockAccessor(updateHeightmap: false);
		foreach (WorldGenThreadDelegate item in server.ModEventManager.WorldgenBlockAccessor)
		{
			item(this);
		}
	}

	public void OnWorldgenStartup()
	{
		chunkthread.loadsavechunks.InitWorldgenAndSpawnChunks();
	}

	public override void OnBeginRunGame()
	{
		server.EventManager.OnGameWorldBeingSaved += OnWorldBeingSaved;
	}

	public override void OnBeginShutdown()
	{
		if (server.Saving)
		{
			ServerMain.Logger.Error("Server was saving and a shutdown has begun? Waiting 10 secs before doing save-on-shutdown");
			Thread.Sleep(10000);
		}
		server.Saving = true;
		if (server.SaveGameData != null)
		{
			server.SaveGameData.TotalSecondsPlayed += (int)(server.ElapsedMilliseconds / 1000);
			server.EventManager.TriggerGameWorldBeingSaved();
		}
		server.Saving = false;
	}

	public override void OnSeperateThreadShutDown()
	{
		chunkthread.gameDatabase.Dispose();
	}

	public void OnWorldBeingSaved()
	{
		bool flag = server.RunPhase != EnumServerRunPhase.Shutdown;
		if (ignoreSave)
		{
			return;
		}
		try
		{
			FileInfo fileInfo = new FileInfo(chunkthread.gameDatabase.DatabaseFilename);
			long freeDiskSpace = ServerMain.xPlatInterface.GetFreeDiskSpace(fileInfo.DirectoryName);
			long num = 1048576 * server.Config.DieBelowDiskSpaceMb;
			if (freeDiskSpace >= 0)
			{
				if (freeDiskSpace >= num && freeDiskSpace < num * 2)
				{
					ServerMain.Logger.Warning("Disk space is getting close to configured server shutdown level. Please free up more disk space or adjust the threshold in the serverconfig.json.");
				}
				else if (freeDiskSpace < num)
				{
					string message = $"Disk space is below {server.Config.DieBelowDiskSpaceMb} megabytes ({freeDiskSpace / 1024 / 1024} mb left). A full harddisk can heavily corrupt a savegame. Please free up more disk space or adjust the threshold in the serverconfig.json (or set to -1 to disable this check). Will kill server now...";
					ServerMain.Logger.Fatal(message);
					ignoreSave = true;
					server.Stop("Out of disk space");
					return;
				}
			}
		}
		catch (ArgumentException)
		{
			ServerMain.Logger.Warning("Exception thrown when trying to check for available disk space. Please manually verify that your hard disk won't run full to avoid savegame corruption");
		}
		if (flag && chunkthread.runOffThreadSaveNow)
		{
			ServerMain.Logger.Fatal("Already saving, will ignore save this time");
			return;
		}
		lock (savingLock)
		{
			SaveGameWorld(flag);
		}
	}

	private void LoadSaveGame()
	{
		string saveFilename = server.GetSaveFilename();
		ServerMain.Logger.Notification("Loading savegame");
		if (!File.Exists(saveFilename))
		{
			ServerMain.Logger.Notification("No savegame file found, creating new one");
		}
		if (server.SaveGameData == null)
		{
			server.SaveGameData = chunkthread.gameDatabase.GetSaveGame();
		}
		if (server.SaveGameData == null)
		{
			server.SaveGameData = SaveGame.CreateNew(server.Config);
			server.SaveGameData.WillSave(reusableStream);
			chunkthread.gameDatabase.StoreSaveGame(server.SaveGameData, reusableStream);
			server.EventManager.TriggerSaveGameCreated();
			ServerMain.Logger.Notification("Create new save game data. Playstyle: {0}", server.SaveGameData.PlayStyle);
			if (!server.Standalone)
			{
				ServerMain.Logger.Notification("Default spawn was set in serverconfig, resetting for safety.");
				server.Config.DefaultSpawn = null;
				server.ConfigNeedsSaving = true;
			}
		}
		else
		{
			if (server.PlayerDataManager.WorldDataByUID == null)
			{
				server.PlayerDataManager.WorldDataByUID = new Dictionary<string, ServerWorldPlayerData>();
			}
			server.SaveGameData.Init(server);
			if (server.SaveGameData.PlayerDataByUID != null)
			{
				ServerMain.Logger.Notification("Transferring player data to new db table...");
				foreach (KeyValuePair<string, ServerWorldPlayerData> item in server.SaveGameData.PlayerDataByUID)
				{
					server.PlayerDataManager.WorldDataByUID[item.Key] = item.Value;
					item.Value.Init(server);
				}
				server.SaveGameData.PlayerDataByUID = null;
			}
			if (SetDefaultSpawnOnce != null)
			{
				server.SaveGameData.DefaultSpawn = SetDefaultSpawnOnce;
				SetDefaultSpawnOnce = null;
			}
			server.SaveGameData.IsNewWorld = false;
			ServerMain.Logger.Notification("Loaded existing save game data. Playstyle: {0}, Playstyle Lang code: {1}, WorldType: {1}", server.SaveGameData.PlayStyle, server.SaveGameData.PlayStyleLangCode, server.SaveGameData.WorldType);
		}
		server.WorldMap.Init(server.SaveGameData.MapSizeX, server.SaveGameData.MapSizeY, server.SaveGameData.MapSizeZ);
		int stages = Math.Max(1, Math.Min(6, MagicNum.MaxWorldgenThreads)) + 1;
		if (server.ReducedServerThreads)
		{
			stages = 1;
		}
		chunkthread.requestedChunkColumns = new ConcurrentIndexedFifoQueue<ChunkColumnLoadRequest>(MagicNum.RequestChunkColumnsQueueSize, stages);
		chunkthread.peekingChunkColumns = new IndexedFifoQueue<ChunkColumnLoadRequest>(MagicNum.RequestChunkColumnsQueueSize / 5);
		ServerMain.Logger.Notification("Savegame {0} loaded", saveFilename);
		ServerMain.Logger.Notification("World size = {0} {1} {2}", server.SaveGameData.MapSizeX, server.SaveGameData.MapSizeY, server.SaveGameData.MapSizeZ);
	}

	private void SaveGameWorld(bool saveLater = false)
	{
		if (!saveLater)
		{
			chunkthread.runOffThreadSaveNow = false;
		}
		if (ServerMain.FrameProfiler == null)
		{
			ServerMain.FrameProfiler = new FrameProfilerUtil(delegate(string text)
			{
				ServerMain.Logger.Notification(text);
			});
			ServerMain.FrameProfiler.Begin(null);
		}
		ServerMain.FrameProfiler.Mark("savegameworld-begin");
		ServerMain.Logger.Event("Mods and systems notified, now saving everything...");
		ServerMain.Logger.StoryEvent(Lang.Get("It pauses."));
		server.SaveGameData.WillSave(reusableStream);
		if (saveLater)
		{
			ServerMain.Logger.Event("Will do offthread savegamedata saving...");
		}
		ServerMain.FrameProfiler.Mark("savegameworld-mid-1");
		ServerMain.Logger.StoryEvent(Lang.Get("One last gaze..."));
		foreach (ServerWorldPlayerData value in server.PlayerDataManager.WorldDataByUID.Values)
		{
			value.BeforeSerialization();
			chunkthread.gameDatabase.SetPlayerData(value.PlayerUID, SerializerUtil.Serialize(value, reusableStream));
		}
		ServerMain.FrameProfiler.Mark("savegameworld-mid-2");
		ServerMain.Logger.Event("Saved player world data...");
		int num = SaveAllDirtyMapRegions(reusableStream);
		ServerMain.FrameProfiler.Mark("savegameworld-mid-3");
		ServerMain.Logger.Event("Saved map regions...");
		ServerMain.Logger.StoryEvent(Lang.Get("...then all goes quiet"));
		int num2 = 0;
		if (!saveLater)
		{
			num2 = SaveAllDirtyMapChunks(reusableStream);
			ServerMain.FrameProfiler.Mark("savegameworld-mid-4");
		}
		ServerMain.Logger.Event("Saved map chunks...");
		ServerMain.Logger.StoryEvent(Lang.Get("The waters recede..."));
		ServerMain.FrameProfiler.Mark("savegameworld-mid-5");
		int num3 = 0;
		if (saveLater)
		{
			PopulateChunksCopy();
			chunkthread.runOffThreadSaveNow = true;
		}
		else
		{
			num3 = SaveAllDirtyLoadedChunks(isSaveLater: false, reusableStream);
			ServerMain.Logger.Event("Saved loaded chunks...");
			ServerMain.Logger.StoryEvent(Lang.Get("The mountains fade..."));
			ServerMain.Logger.StoryEvent(Lang.Get("The dark settles in."));
			num3 += SaveAllDirtyGeneratingChunks(reusableStream);
			ServerMain.Logger.Event("Saved generating chunks...");
			server.SaveGameData.UpdateLandClaims(server.WorldMap.All);
			chunkthread.gameDatabase.StoreSaveGame(server.SaveGameData, reusableStream);
			ServerMain.Logger.Event("Saved savegamedata..." + server.SaveGameData.HighestChunkdataVersion);
		}
		ServerMain.Logger.Event("World saved! Saved {0} chunks, {1} mapchunks, {2} mapregions.", num3, num2, num);
		ServerMain.Logger.StoryEvent(Lang.Get("It sighs..."));
		ServerMain.FrameProfiler.Mark("savegameworld-end");
	}

	private int SaveAllDirtyMapRegions(FastMemoryStream ms)
	{
		int num = 0;
		List<DbChunk> list = new List<DbChunk>();
		foreach (KeyValuePair<long, ServerMapRegion> loadedMapRegion in server.loadedMapRegions)
		{
			if (loadedMapRegion.Value.DirtyForSaving)
			{
				loadedMapRegion.Value.DirtyForSaving = false;
				num++;
				list.Add(new DbChunk
				{
					Position = server.WorldMap.MapRegionPosFromIndex2D(loadedMapRegion.Key),
					Data = loadedMapRegion.Value.ToBytes(ms)
				});
			}
		}
		chunkthread.gameDatabase.SetMapRegions(list);
		return num;
	}

	private int SaveAllDirtyMapChunks(FastMemoryStream ms)
	{
		int num = 0;
		List<DbChunk> list = new List<DbChunk>();
		foreach (KeyValuePair<long, ServerMapChunk> loadedMapChunk in server.loadedMapChunks)
		{
			if (loadedMapChunk.Value.DirtyForSaving)
			{
				loadedMapChunk.Value.DirtyForSaving = false;
				ChunkPos position = server.WorldMap.ChunkPosFromChunkIndex2D(loadedMapChunk.Key);
				num++;
				list.Add(new DbChunk
				{
					Position = position,
					Data = loadedMapChunk.Value.ToBytes(ms)
				});
				if (list.Count > 200)
				{
					chunkthread.gameDatabase.SetMapChunks(list);
					list.Clear();
				}
			}
		}
		chunkthread.gameDatabase.SetMapChunks(list);
		return num;
	}

	internal int SaveAllDirtyLoadedChunks(bool isSaveLater, FastMemoryStream ms)
	{
		int num = 0;
		List<DbChunk> list = new List<DbChunk>();
		if (!isSaveLater)
		{
			PopulateChunksCopy();
		}
		foreach (KeyValuePair<long, ServerChunk> item in chunksCopy)
		{
			if (item.Value.DirtyForSaving)
			{
				item.Value.DirtyForSaving = false;
				ChunkPos position = server.WorldMap.ChunkPosFromChunkIndex3D(item.Key);
				list.Add(new DbChunk
				{
					Position = position,
					Data = item.Value.ToBytes(ms)
				});
				num++;
				if (list.Count > 300)
				{
					chunkthread.gameDatabase.SetChunks(list);
					list.Clear();
				}
				if (num > 0 && num % 300 == 0)
				{
					ServerMain.Logger.Event("Saved {0} chunks...", num);
				}
			}
		}
		chunkthread.gameDatabase.SetChunks(list);
		if (num > 0)
		{
			server.SaveGameData.UpdateChunkdataVersion();
		}
		return num;
	}

	private void PopulateChunksCopy()
	{
		chunksCopy.Clear();
		server.loadedChunksLock.AcquireReadLock();
		try
		{
			foreach (KeyValuePair<long, ServerChunk> loadedChunk in server.loadedChunks)
			{
				chunksCopy[loadedChunk.Key] = loadedChunk.Value;
			}
		}
		finally
		{
			server.loadedChunksLock.ReleaseReadLock();
		}
	}

	internal int SaveAllDirtyGeneratingChunks(FastMemoryStream ms)
	{
		int num = 0;
		List<DbChunk> list = new List<DbChunk>();
		if (chunkthread.requestedChunkColumns.Count > 0)
		{
			foreach (ChunkColumnLoadRequest item in chunkthread.requestedChunkColumns.Snapshot())
			{
				if (item.Chunks == null || item.Disposed || item.CurrentIncompletePass <= EnumWorldGenPass.Terrain)
				{
					continue;
				}
				item.generatingLock.AcquireReadLock();
				try
				{
					for (int i = 0; i < item.Chunks.Length; i++)
					{
						if (item.Chunks[i].DirtyForSaving)
						{
							item.Chunks[i].DirtyForSaving = false;
							list.Add(new DbChunk
							{
								Position = new ChunkPos(item.chunkX, i, item.chunkZ, 0),
								Data = item.Chunks[i].ToBytes(ms)
							});
							num++;
							if (num > 0 && num % 300 == 0)
							{
								ServerMain.Logger.Event("Saved {0} generating chunks...", num);
								ServerMain.Logger.StoryEvent("...");
							}
						}
					}
				}
				finally
				{
					item.generatingLock.ReleaseReadLock();
				}
				if (list.Count > 300)
				{
					chunkthread.gameDatabase.SetChunks(list);
					list.Clear();
				}
			}
			chunkthread.gameDatabase.SetChunks(list);
		}
		if (num > 0)
		{
			server.SaveGameData.UpdateChunkdataVersion();
		}
		return num;
	}

	public IWorldGenBlockAccessor GetBlockAccessor(bool updateHeightmap)
	{
		if (updateHeightmap)
		{
			if (blockAccessorWGUpdateHeightMap == null)
			{
				blockAccessorWGUpdateHeightMap = new BlockAccessorWorldGenUpdateHeightmap(server, chunkthread);
			}
			return blockAccessorWGUpdateHeightMap;
		}
		if (blockAccessorWG == null)
		{
			blockAccessorWG = new BlockAccessorWorldGen(server, chunkthread);
		}
		return blockAccessorWG;
	}
}
