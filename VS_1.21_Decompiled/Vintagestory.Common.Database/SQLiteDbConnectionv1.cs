using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.IO;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common.Database;

public class SQLiteDbConnectionv1 : IGameDbConnection, IDisposable
{
	private SqliteConnection sqliteConn;

	private string databaseFileName;

	public ILogger logger = new NullLogger();

	private static int MapChunkYCoord = 99998;

	private static int MapRegionYCoord = 99999;

	private static ulong pow20minus1 = 1048575uL;

	public bool IsReadOnly => false;

	public SQLiteDbConnectionv1(ILogger logger)
	{
		this.logger = logger;
	}

	public bool OpenOrCreate(string filename, ref string errorMessage, bool requireWriteAccess, bool corruptionProtection, bool doIntegrityCheck)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		try
		{
			databaseFileName = filename;
			bool flag = !File.Exists(databaseFileName);
			DbConnectionStringBuilder dbConnectionStringBuilder = new DbConnectionStringBuilder
			{
				{ "Data Source", databaseFileName },
				{ "Pooling", "false" }
			};
			sqliteConn = new SqliteConnection(dbConnectionStringBuilder.ToString());
			((DbConnection)(object)sqliteConn).Open();
			SqliteCommand val = sqliteConn.CreateCommand();
			try
			{
				((DbCommand)(object)val).CommandText = "PRAGMA journal_mode=Off;";
				((DbCommand)(object)val).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
			if (flag)
			{
				CreateTables(sqliteConn);
			}
			if (doIntegrityCheck && !integrityCheck(sqliteConn))
			{
				logger.Error("Database is possibly corrupted.");
			}
		}
		catch (Exception e)
		{
			logger.Error(errorMessage = "Failed opening savegame.");
			logger.Error(e);
			return false;
		}
		return true;
	}

	public void Close()
	{
		((DbConnection)(object)sqliteConn).Close();
		((Component)(object)sqliteConn).Dispose();
	}

	public void Dispose()
	{
		Close();
	}

	private void CreateTables(SqliteConnection sqliteConn)
	{
		SqliteCommand val = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "CREATE TABLE chunks (position integer PRIMARY KEY, data BLOB);";
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void CreateBackup(string backupFilename)
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		if (databaseFileName == backupFilename)
		{
			logger.Warning("Cannot overwrite current running database. Chose another destination.");
			return;
		}
		if (File.Exists(backupFilename))
		{
			logger.Notification("File " + backupFilename + " exists. Overwriting file.");
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

	private bool integrityCheck(SqliteConnection sqliteConn)
	{
		bool result = false;
		SqliteCommand val = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "PRAGMA integrity_check";
			SqliteDataReader val2 = val.ExecuteReader();
			try
			{
				logger.Notification($"Database: {((DbConnection)(object)sqliteConn).DataSource}. Running SQLite integrity check...");
				while (((DbDataReader)(object)val2).Read())
				{
					logger.Notification("Integrity check " + ((DbDataReader)(object)val2)[0].ToString());
					if (((DbDataReader)(object)val2)[0].ToString() == "ok")
					{
						result = true;
						break;
					}
				}
				return result;
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

	public int QuantityChunks()
	{
		SqliteCommand val = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "SELECT count(*) FROM chunks";
			return System.Convert.ToInt32(((DbCommand)(object)val).ExecuteScalar());
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public IEnumerable<byte[]> GetChunks(IEnumerable<Vec3i> chunkpositions)
	{
		SqliteTransaction transaction = sqliteConn.BeginTransaction();
		try
		{
			foreach (Vec3i chunkposition in chunkpositions)
			{
				ulong position = ToMapPos(chunkposition.X, chunkposition.Y, chunkposition.Z);
				yield return GetChunk(position);
			}
			((DbTransaction)(object)transaction).Commit();
		}
		finally
		{
			((IDisposable)transaction)?.Dispose();
		}
	}

	public IEnumerable<DbChunk> GetAllChunks()
	{
		SqliteCommand cmd = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)cmd).CommandText = "SELECT position, data FROM chunks";
			SqliteDataReader reader = cmd.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)reader).Read())
				{
					object value = ((DbDataReader)(object)reader)["position"];
					object obj = ((DbDataReader)(object)reader)["data"];
					ulong num = System.Convert.ToUInt64(value);
					Vec3i vec3i = FromMapPos(num);
					if (vec3i.Y != MapChunkYCoord && vec3i.Y != MapRegionYCoord && num != long.MaxValue)
					{
						yield return new DbChunk
						{
							Position = new ChunkPos(vec3i),
							Data = (obj as byte[])
						};
					}
				}
			}
			finally
			{
				((IDisposable)reader)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)cmd)?.Dispose();
		}
	}

	public IEnumerable<DbChunk> GetAllMapChunks()
	{
		SqliteCommand cmd = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)cmd).CommandText = "SELECT position, data FROM chunks";
			SqliteDataReader sqlite_datareader = cmd.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)sqlite_datareader).Read())
				{
					object value = ((DbDataReader)(object)sqlite_datareader)["position"];
					object obj = ((DbDataReader)(object)sqlite_datareader)["data"];
					ulong num = System.Convert.ToUInt64(value);
					Vec3i vec3i = FromMapPos(num);
					if (vec3i.Y == MapChunkYCoord && num != long.MaxValue)
					{
						yield return new DbChunk
						{
							Position = new ChunkPos(vec3i),
							Data = (obj as byte[])
						};
					}
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

	public IEnumerable<DbChunk> GetAllMapRegions()
	{
		SqliteCommand cmd = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)cmd).CommandText = "SELECT position, data FROM chunks";
			SqliteDataReader sqlite_datareader = cmd.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)sqlite_datareader).Read())
				{
					object value = ((DbDataReader)(object)sqlite_datareader)["position"];
					object obj = ((DbDataReader)(object)sqlite_datareader)["data"];
					ulong num = System.Convert.ToUInt64(value);
					Vec3i vec3i = FromMapPos(num);
					if (vec3i.Y == MapRegionYCoord && num != long.MaxValue)
					{
						yield return new DbChunk
						{
							Position = new ChunkPos(vec3i),
							Data = (obj as byte[])
						};
					}
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

	public byte[] GetMapChunk(ulong position)
	{
		Vec3i vec3i = FromMapPos(position);
		position = ToMapPos(vec3i.X, MapChunkYCoord, vec3i.Z);
		return GetChunk(position);
	}

	public byte[] GetMapRegion(ulong position)
	{
		Vec3i vec3i = FromMapPos(position);
		position = ToMapPos(vec3i.X, MapRegionYCoord, vec3i.Z);
		return GetChunk(position);
	}

	public byte[] GetChunk(ulong position)
	{
		SqliteCommand val = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "SELECT data FROM chunks WHERE position=@position";
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

	public void DeleteMapChunks(IEnumerable<ChunkPos> coords)
	{
		SqliteTransaction val = sqliteConn.BeginTransaction();
		try
		{
			foreach (ChunkPos coord in coords)
			{
				DeleteChunk(ToMapPos(coord.X, MapChunkYCoord, coord.Z));
			}
			((DbTransaction)(object)val).Commit();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void DeleteMapRegions(IEnumerable<ChunkPos> coords)
	{
		SqliteTransaction val = sqliteConn.BeginTransaction();
		try
		{
			foreach (ChunkPos coord in coords)
			{
				DeleteChunk(ToMapPos(coord.X, MapRegionYCoord, coord.Z));
			}
			((DbTransaction)(object)val).Commit();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void DeleteChunks(IEnumerable<ChunkPos> coords)
	{
		SqliteTransaction val = sqliteConn.BeginTransaction();
		try
		{
			foreach (ChunkPos coord in coords)
			{
				DeleteChunk(ToMapPos(coord.X, coord.Y, coord.Z));
			}
			((DbTransaction)(object)val).Commit();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void DeleteChunk(ulong position)
	{
		using DbCommand dbCommand = sqliteConn.CreateCommand();
		dbCommand.CommandText = "DELETE FROM chunks WHERE position=@position";
		dbCommand.Parameters.Add(CreateParameter("position", DbType.UInt64, position, dbCommand));
		dbCommand.ExecuteNonQuery();
	}

	public void SetChunks(IEnumerable<DbChunk> chunks)
	{
		SqliteTransaction val = sqliteConn.BeginTransaction();
		try
		{
			foreach (DbChunk chunk in chunks)
			{
				ulong position = ToMapPos(chunk.Position.X, chunk.Position.Y, chunk.Position.Z);
				InsertChunk(position, chunk.Data);
			}
			((DbTransaction)(object)val).Commit();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void SetMapChunks(IEnumerable<DbChunk> mapchunks)
	{
		foreach (DbChunk mapchunk in mapchunks)
		{
			mapchunk.Position.Y = MapChunkYCoord;
		}
		SetChunks(mapchunks);
	}

	public void SetMapRegions(IEnumerable<DbChunk> mapregions)
	{
		foreach (DbChunk mapregion in mapregions)
		{
			mapregion.Position.Y = MapRegionYCoord;
		}
		SetChunks(mapregions);
	}

	private void InsertChunk(ulong position, byte[] data)
	{
		using DbCommand dbCommand = sqliteConn.CreateCommand();
		dbCommand.CommandText = "INSERT OR REPLACE INTO chunks (position, data) VALUES (@position,@data)";
		dbCommand.Parameters.Add(CreateParameter("position", DbType.UInt64, position, dbCommand));
		dbCommand.Parameters.Add(CreateParameter("data", DbType.Object, data, dbCommand));
		dbCommand.ExecuteNonQuery();
	}

	private DbParameter CreateParameter(string parameterName, DbType dbType, object value, DbCommand command)
	{
		DbParameter dbParameter = command.CreateParameter();
		dbParameter.ParameterName = parameterName;
		dbParameter.DbType = dbType;
		dbParameter.Value = value;
		return dbParameter;
	}

	public byte[] GetGameData()
	{
		try
		{
			return GetChunk(9223372036854775807uL);
		}
		catch (Exception ex)
		{
			logger.Warning("Exception thrown on GetGlobalData: " + ex.Message);
			return null;
		}
	}

	public void StoreGameData(byte[] data)
	{
		SqliteTransaction val = sqliteConn.BeginTransaction();
		try
		{
			InsertChunk(9223372036854775807uL, data);
			((DbTransaction)(object)val).Commit();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public bool QuickCorrectSaveGameVersionTest()
	{
		return true;
	}

	public static Vec3i FromMapPos(ulong v)
	{
		uint z = (uint)(v & pow20minus1);
		v >>= 20;
		uint y = (uint)(v & pow20minus1);
		v >>= 20;
		return new Vec3i((int)(v & pow20minus1), (int)y, (int)z);
	}

	public static ulong ToMapPos(int x, int y, int z)
	{
		return (ulong)(((long)x << 40) | ((long)y << 20) | (uint)z);
	}

	public byte[] GetPlayerData(string playeruid)
	{
		throw new NotImplementedException();
	}

	public void SetPlayerData(string playeruid, byte[] data)
	{
		throw new NotImplementedException();
	}

	public void UpgradeToWriteAccess()
	{
	}

	public bool IntegrityCheck()
	{
		throw new NotImplementedException();
	}

	public bool ChunkExists(ulong position)
	{
		throw new NotImplementedException();
	}

	public bool MapChunkExists(ulong position)
	{
		throw new NotImplementedException();
	}

	public bool MapRegionExists(ulong position)
	{
		throw new NotImplementedException();
	}

	public void Vacuum()
	{
		SqliteCommand val = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "VACUUM;";
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}
}
