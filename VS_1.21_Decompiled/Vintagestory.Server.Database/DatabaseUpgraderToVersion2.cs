using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Config;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Server.Database;

public class DatabaseUpgraderToVersion2 : IDatabaseUpgrader
{
	public bool Upgrade(ServerMain server, string worldFilename)
	{
		GameDatabase gameDatabase = new GameDatabase(ServerMain.Logger);
		gameDatabase.OpenConnection(worldFilename, 1, corruptionProtection: true, doIntegrityCheck: false);
		ServerMain.Logger.Event("Old world file opened");
		GameDatabase gameDatabase2 = new GameDatabase(ServerMain.Logger);
		gameDatabase2.OpenConnection(worldFilename + "v2", 2, corruptionProtection: true, doIntegrityCheck: false);
		ServerMain.Logger.Event("New world file created");
		ServerMain.Logger.Event("Migrating savegame");
		gameDatabase2.StoreSaveGame(gameDatabase.GetSaveGame());
		ServerMain.Logger.Event("Migrating map regions");
		gameDatabase2.SetMapRegions(gameDatabase.GetAllMapRegions());
		ServerMain.Logger.Event("Migrating map chunks");
		gameDatabase2.SetMapChunks(gameDatabase.GetAllMapChunks());
		ServerMain.Logger.Event("Migrating chunks...");
		IEnumerable<DbChunk> allChunks = gameDatabase.GetAllChunks();
		List<DbChunk> list = new List<DbChunk>();
		ICompression compressor = new CompressionGzip();
		ICompression compressor2 = new CompressionDeflate();
		int num = 0;
		foreach (DbChunk item in allChunks)
		{
			Compression.compressor = compressor;
			ServerChunk serverChunk = ServerChunk.FromBytes(item.Data, server.serverChunkDataPool, server);
			serverChunk.Unpack();
			Compression.compressor = compressor2;
			serverChunk.TryPackAndCommit();
			item.Data = serverChunk.ToBytes();
			num++;
			list.Add(item);
			if (list.Count >= 100)
			{
				ServerMain.Logger.Event(num + " chunks migrated");
				gameDatabase2.SetChunks(list);
				list.Clear();
			}
		}
		gameDatabase2.SetChunks(list);
		ServerMain.Logger.Event(num + " chunks migrated. Done!");
		gameDatabase.CloseConnection();
		gameDatabase2.CloseConnection();
		ServerMain.Logger.Event("Moving away old world file");
		GamePaths.EnsurePathExists(GamePaths.OldSaves);
		FileInfo fileInfo = new FileInfo(worldFilename);
		string text = Path.Combine(GamePaths.OldSaves, fileInfo.Name);
		if (File.Exists(text))
		{
			File.Delete(text);
		}
		fileInfo.MoveTo(text);
		ServerMain.Logger.Event("Renaming new world file");
		fileInfo = new FileInfo(worldFilename + "v2");
		fileInfo.MoveTo(worldFilename);
		return true;
	}
}
