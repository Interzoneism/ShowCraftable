using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.IO;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Common.Database;

public class SQLiteDbConnectionv2 : SQLiteDBConnection, IGameDbConnection, IDisposable
{
	private SqliteCommand setChunksCmd;

	private SqliteCommand setMapChunksCmd;

	public override string DBTypeCode => "savegame database";

	public SQLiteDbConnectionv2(ILogger logger)
		: base(logger)
	{
		base.logger = logger;
	}

	public override void OnOpened()
	{
		setChunksCmd = sqliteConn.CreateCommand();
		((DbCommand)(object)setChunksCmd).CommandText = "INSERT OR REPLACE INTO chunk (position, data) VALUES (@position,@data)";
		((DbParameterCollection)(object)setChunksCmd.Parameters).Add((object)CreateParameter("position", DbType.UInt64, 0, (DbCommand)(object)setChunksCmd));
		((DbParameterCollection)(object)setChunksCmd.Parameters).Add((object)CreateParameter("data", DbType.Object, null, (DbCommand)(object)setChunksCmd));
		((DbCommand)(object)setChunksCmd).Prepare();
		setMapChunksCmd = sqliteConn.CreateCommand();
		((DbCommand)(object)setMapChunksCmd).CommandText = "INSERT OR REPLACE INTO mapchunk (position, data) VALUES (@position,@data)";
		((DbParameterCollection)(object)setMapChunksCmd.Parameters).Add((object)CreateParameter("position", DbType.UInt64, 0, (DbCommand)(object)setMapChunksCmd));
		((DbParameterCollection)(object)setMapChunksCmd.Parameters).Add((object)CreateParameter("data", DbType.Object, null, (DbCommand)(object)setMapChunksCmd));
		((DbCommand)(object)setMapChunksCmd).Prepare();
	}

	public void UpgradeToWriteAccess()
	{
		CreateTablesIfNotExists(sqliteConn);
	}

	public bool IntegrityCheck()
	{
		if (!DoIntegrityCheck(sqliteConn))
		{
			string message = "Database integrity check failed. Attempt basic repair procedure (via VACUUM), this might take minutes to hours depending on the size of the save game...";
			logger.Notification(message);
			logger.StoryEvent(message);
			try
			{
				SqliteCommand val = sqliteConn.CreateCommand();
				try
				{
					((DbCommand)(object)val).CommandText = "PRAGMA writable_schema=ON;";
					((DbCommand)(object)val).ExecuteNonQuery();
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
				SqliteCommand val2 = sqliteConn.CreateCommand();
				try
				{
					((DbCommand)(object)val2).CommandText = "VACUUM;";
					((DbCommand)(object)val2).ExecuteNonQuery();
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			catch
			{
				logger.StoryEvent("Unable to repair :(");
				logger.Notification("Unable to repair :(\nRecommend any of the solutions posted here: https://wiki.vintagestory.at/index.php/Repairing_a_corrupt_savegame_or_worldmap\nWill exit now");
				throw new Exception("Database integrity bad");
			}
			if (!DoIntegrityCheck(sqliteConn, logResults: false))
			{
				logger.StoryEvent("Unable to repair :(");
				logger.Notification("Database integrity still bad :(\nRecommend any of the solutions posted here: https://wiki.vintagestory.at/index.php/Repairing_a_corrupt_savegame_or_worldmap\nWill exit now");
				throw new Exception("Database integrity bad");
			}
			logger.Notification("Database integrity check now okay, yay!");
		}
		return true;
	}

	public int QuantityChunks()
	{
		SqliteCommand val = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "SELECT count(*) FROM chunk";
			return System.Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar());
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public IEnumerable<DbChunk> GetAllChunks(string tablename)
	{
		SqliteCommand cmd = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)cmd).CommandText = "SELECT position, data FROM " + tablename;
			SqliteDataReader sqlite_datareader = cmd.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)sqlite_datareader).Read())
				{
					object obj = ((DbDataReader)(object)sqlite_datareader)["data"];
					ChunkPos position = ChunkPos.FromChunkIndex_saveGamev2((ulong)(long)((DbDataReader)(object)sqlite_datareader)["position"]);
					yield return new DbChunk
					{
						Position = position,
						Data = (obj as byte[])
					};
				}
			}
			finally
			{
				((IDisposable)sqlite_datareader)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)cmd)?.Dispose();
		}
	}

	public IEnumerable<DbChunk> GetAllChunks()
	{
		return GetAllChunks("chunk");
	}

	public IEnumerable<DbChunk> GetAllMapChunks()
	{
		return GetAllChunks("mapchunk");
	}

	public IEnumerable<DbChunk> GetAllMapRegions()
	{
		return GetAllChunks("mapregion");
	}

	public byte[] GetPlayerData(string playeruid)
	{
		SqliteCommand val = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "SELECT data FROM playerdata WHERE playeruid=@playeruid";
			((DbParameterCollection)(object)val.Parameters).Add((object)CreateParameter("playeruid", DbType.String, playeruid, (DbCommand)(object)val));
			SqliteDataReader val2 = val.ExecuteReader();
			try
			{
				if (((DbDataReader)(object)val2).Read())
				{
					return ((DbDataReader)(object)val2)["data"] as byte[];
				}
				return null;
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void SetPlayerData(string playeruid, byte[] data)
	{
		if (data == null)
		{
			using (DbCommand dbCommand = sqliteConn.CreateCommand())
			{
				dbCommand.CommandText = "DELETE FROM playerdata WHERE playeruid=@playeruid";
				dbCommand.Parameters.Add(CreateParameter("playeruid", DbType.String, playeruid, dbCommand));
				dbCommand.ExecuteNonQuery();
				return;
			}
		}
		if (GetPlayerData(playeruid) == null)
		{
			using (DbCommand dbCommand2 = sqliteConn.CreateCommand())
			{
				dbCommand2.CommandText = "INSERT INTO playerdata (playeruid, data) VALUES (@playeruid,@data)";
				dbCommand2.Parameters.Add(CreateParameter("playeruid", DbType.String, playeruid, dbCommand2));
				dbCommand2.Parameters.Add(CreateParameter("data", DbType.Object, data, dbCommand2));
				dbCommand2.ExecuteNonQuery();
				return;
			}
		}
		using DbCommand dbCommand3 = sqliteConn.CreateCommand();
		dbCommand3.CommandText = "UPDATE playerdata set data=@data where playeruid=@playeruid";
		dbCommand3.Parameters.Add(CreateParameter("data", DbType.Object, data, dbCommand3));
		dbCommand3.Parameters.Add(CreateParameter("playeruid", DbType.String, playeruid, dbCommand3));
		dbCommand3.ExecuteNonQuery();
	}

	public IEnumerable<byte[]> GetChunks(IEnumerable<ChunkPos> chunkpositions)
	{
		lock (transactionLock)
		{
			SqliteTransaction transaction = sqliteConn.BeginTransaction();
			try
			{
				foreach (ChunkPos chunkposition in chunkpositions)
				{
					yield return GetChunk(chunkposition.ToChunkIndex(), "chunk");
				}
				((DbTransaction)(object)transaction).Commit();
			}
			finally
			{
				((IDisposable)transaction)?.Dispose();
			}
		}
	}

	public byte[] GetChunk(ulong position)
	{
		return GetChunk(position, "chunk");
	}

	public byte[] GetMapChunk(ulong position)
	{
		return GetChunk(position, "mapchunk");
	}

	public byte[] GetMapRegion(ulong position)
	{
		return GetChunk(position, "mapregion");
	}

	public bool ChunkExists(ulong position)
	{
		return ChunkExists(position, "chunk");
	}

	public bool MapChunkExists(ulong position)
	{
		return ChunkExists(position, "mapchunk");
	}

	public bool MapRegionExists(ulong position)
	{
		return ChunkExists(position, "mapregion");
	}

	public bool ChunkExists(ulong position, string tablename)
	{
		SqliteCommand val = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "SELECT position FROM " + tablename + " WHERE position=@position";
			((DbParameterCollection)(object)val.Parameters).Add((object)CreateParameter("position", DbType.UInt64, position, (DbCommand)(object)val));
			SqliteDataReader val2 = val.ExecuteReader();
			try
			{
				return ((DbDataReader)(object)val2).HasRows;
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public byte[] GetChunk(ulong position, string tablename)
	{
		SqliteCommand val = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "SELECT data FROM " + tablename + " WHERE position=@position";
			((DbParameterCollection)(object)val.Parameters).Add((object)CreateParameter("position", DbType.UInt64, position, (DbCommand)(object)val));
			SqliteDataReader val2 = val.ExecuteReader();
			try
			{
				if (((DbDataReader)(object)val2).Read())
				{
					return ((DbDataReader)(object)val2)["data"] as byte[];
				}
				return null;
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void DeleteChunks(IEnumerable<ChunkPos> chunkpositions)
	{
		DeleteChunks(chunkpositions, "chunk");
	}

	public void DeleteMapChunks(IEnumerable<ChunkPos> mapchunkpositions)
	{
		DeleteChunks(mapchunkpositions, "mapchunk");
	}

	public void DeleteMapRegions(IEnumerable<ChunkPos> mapchunkregions)
	{
		DeleteChunks(mapchunkregions, "mapregion");
	}

	public void DeleteChunks(IEnumerable<ChunkPos> chunkpositions, string tablename)
	{
		lock (transactionLock)
		{
			SqliteTransaction val = sqliteConn.BeginTransaction();
			try
			{
				foreach (ChunkPos chunkposition in chunkpositions)
				{
					DeleteChunk(chunkposition.ToChunkIndex(), tablename);
				}
				((DbTransaction)(object)val).Commit();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}

	public void DeleteChunk(ulong position, string tablename)
	{
		using DbCommand dbCommand = sqliteConn.CreateCommand();
		dbCommand.CommandText = "DELETE FROM " + tablename + " WHERE position=@position";
		dbCommand.Parameters.Add(CreateParameter("position", DbType.UInt64, position, dbCommand));
		dbCommand.ExecuteNonQuery();
	}

	public void SetChunks(IEnumerable<DbChunk> chunks)
	{
		lock (transactionLock)
		{
			SqliteTransaction val = sqliteConn.BeginTransaction();
			try
			{
				setChunksCmd.Transaction = val;
				foreach (DbChunk chunk in chunks)
				{
					((DbParameter)(object)setChunksCmd.Parameters["position"]).Value = chunk.Position.ToChunkIndex();
					((DbParameter)(object)setChunksCmd.Parameters["data"]).Value = chunk.Data;
					((DbCommand)(object)setChunksCmd).ExecuteNonQuery();
				}
				((DbTransaction)(object)val).Commit();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}

	public void SetMapChunks(IEnumerable<DbChunk> mapchunks)
	{
		lock (transactionLock)
		{
			SqliteTransaction val = sqliteConn.BeginTransaction();
			try
			{
				setMapChunksCmd.Transaction = val;
				foreach (DbChunk mapchunk in mapchunks)
				{
					mapchunk.Position.Y = 0;
					((DbParameter)(object)setMapChunksCmd.Parameters["position"]).Value = mapchunk.Position.ToChunkIndex();
					((DbParameter)(object)setMapChunksCmd.Parameters["data"]).Value = mapchunk.Data;
					((DbCommand)(object)setMapChunksCmd).ExecuteNonQuery();
				}
				((DbTransaction)(object)val).Commit();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}

	public void SetMapRegions(IEnumerable<DbChunk> mapregions)
	{
		lock (transactionLock)
		{
			SqliteTransaction val = sqliteConn.BeginTransaction();
			try
			{
				foreach (DbChunk mapregion in mapregions)
				{
					mapregion.Position.Y = 0;
					InsertChunk(mapregion.Position.ToChunkIndex(), mapregion.Data, "mapregion");
				}
				((DbTransaction)(object)val).Commit();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}

	private void InsertChunk(ulong position, byte[] data, string tablename)
	{
		using DbCommand dbCommand = sqliteConn.CreateCommand();
		dbCommand.CommandText = "INSERT OR REPLACE INTO " + tablename + " (position, data) VALUES (@position,@data)";
		dbCommand.Parameters.Add(CreateParameter("position", DbType.UInt64, position, dbCommand));
		dbCommand.Parameters.Add(CreateParameter("data", DbType.Object, data, dbCommand));
		dbCommand.ExecuteNonQuery();
	}

	public byte[] GetGameData()
	{
		try
		{
			SqliteCommand val = sqliteConn.CreateCommand();
			try
			{
				((DbCommand)(object)val).CommandText = "SELECT data FROM gamedata LIMIT 1";
				SqliteDataReader val2 = val.ExecuteReader();
				try
				{
					if (((DbDataReader)(object)val2).Read())
					{
						return ((DbDataReader)(object)val2)["data"] as byte[];
					}
					return null;
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			logger.Warning("Exception thrown on GetGlobalData: " + ex.Message);
			return null;
		}
	}

	public void StoreGameData(byte[] data)
	{
		lock (transactionLock)
		{
			SqliteTransaction val = sqliteConn.BeginTransaction();
			try
			{
				using (DbCommand dbCommand = sqliteConn.CreateCommand())
				{
					dbCommand.CommandText = "INSERT OR REPLACE INTO gamedata (savegameid, data) VALUES (@savegameid,@data)";
					dbCommand.Parameters.Add(CreateParameter("savegameid", DbType.UInt64, 1, dbCommand));
					dbCommand.Parameters.Add(CreateParameter("data", DbType.Object, data, dbCommand));
					dbCommand.ExecuteNonQuery();
				}
				((DbTransaction)(object)val).Commit();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}

	public bool QuickCorrectSaveGameVersionTest()
	{
		SqliteCommand val = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'gamedata';";
			return ((DbCommand)(object)val).ExecuteScalar() != null;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	protected override void CreateTablesIfNotExists(SqliteConnection sqliteConn)
	{
		SqliteCommand val = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "CREATE TABLE IF NOT EXISTS chunk (position integer PRIMARY KEY, data BLOB);";
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		SqliteCommand val2 = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val2).CommandText = "CREATE TABLE IF NOT EXISTS mapchunk (position integer PRIMARY KEY, data BLOB);";
			((DbCommand)(object)val2).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
		SqliteCommand val3 = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val3).CommandText = "CREATE TABLE IF NOT EXISTS mapregion (position integer PRIMARY KEY, data BLOB);";
			((DbCommand)(object)val3).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val3)?.Dispose();
		}
		SqliteCommand val4 = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val4).CommandText = "CREATE TABLE IF NOT EXISTS gamedata (savegameid integer PRIMARY KEY, data BLOB);";
			((DbCommand)(object)val4).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val4)?.Dispose();
		}
		SqliteCommand val5 = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val5).CommandText = "CREATE TABLE IF NOT EXISTS playerdata (playerid integer PRIMARY KEY AUTOINCREMENT, playeruid TEXT, data BLOB);";
			((DbCommand)(object)val5).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val5)?.Dispose();
		}
		SqliteCommand val6 = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val6).CommandText = "CREATE index IF NOT EXISTS index_playeruid on playerdata(playeruid);";
			((DbCommand)(object)val6).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val6)?.Dispose();
		}
	}

	public void CreateBackup(string backupFilename)
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		if (databaseFileName == backupFilename)
		{
			logger.Error("Cannot overwrite current running database. Chose another destination.");
			return;
		}
		if (File.Exists(backupFilename))
		{
			logger.Error("File " + backupFilename + " exists. Overwriting file.");
		}
		SqliteConnection val = new SqliteConnection(new DbConnectionStringBuilder
		{
			{
				"Data Source",
				Path.Combine(GamePaths.Backups, backupFilename)
			},
			{ "Pooling", "false" }
		}.ToString());
		((DbConnection)(object)val).Open();
		SqliteCommand val2 = val.CreateCommand();
		try
		{
			((DbCommand)(object)val2).CommandText = "PRAGMA journal_mode=Off;";
			((DbCommand)(object)val2).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
		sqliteConn.BackupDatabase(val, ((DbConnection)(object)val).Database, ((DbConnection)(object)sqliteConn).Database);
		((DbConnection)(object)val).Close();
		((Component)(object)val).Dispose();
	}

	public override void Close()
	{
		((Component)(object)setChunksCmd)?.Dispose();
		((Component)(object)setMapChunksCmd)?.Dispose();
		base.Close();
	}

	public override void Dispose()
	{
		((Component)(object)setChunksCmd)?.Dispose();
		((Component)(object)setMapChunksCmd)?.Dispose();
		base.Dispose();
	}

	bool IGameDbConnection.get_IsReadOnly()
	{
		return base.IsReadOnly;
	}

	bool IGameDbConnection.OpenOrCreate(string filename, ref string errorMessage, bool requireWriteAccess, bool corruptionProtection, bool doIntegrityCheck)
	{
		return OpenOrCreate(filename, ref errorMessage, requireWriteAccess, corruptionProtection, doIntegrityCheck);
	}

	void IGameDbConnection.Vacuum()
	{
		Vacuum();
	}
}
