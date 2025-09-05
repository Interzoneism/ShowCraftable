using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class MapDB : SQLiteDBConnection
{
	private SqliteCommand setMapPieceCmd;

	private SqliteCommand getMapPieceCmd;

	public override string DBTypeCode => "worldmap database";

	public MapDB(ILogger logger)
		: base(logger)
	{
	}

	public override void OnOpened()
	{
		base.OnOpened();
		setMapPieceCmd = sqliteConn.CreateCommand();
		((DbCommand)(object)setMapPieceCmd).CommandText = "INSERT OR REPLACE INTO mappiece (position, data) VALUES (@pos, @data)";
		setMapPieceCmd.Parameters.Add("@pos", (SqliteType)1, 1);
		setMapPieceCmd.Parameters.Add("@data", (SqliteType)4);
		((DbCommand)(object)setMapPieceCmd).Prepare();
		getMapPieceCmd = sqliteConn.CreateCommand();
		((DbCommand)(object)getMapPieceCmd).CommandText = "SELECT data FROM mappiece WHERE position=@pos";
		getMapPieceCmd.Parameters.Add("@pos", (SqliteType)1, 1);
		((DbCommand)(object)getMapPieceCmd).Prepare();
	}

	protected override void CreateTablesIfNotExists(SqliteConnection sqliteConn)
	{
		SqliteCommand val = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "CREATE TABLE IF NOT EXISTS mappiece (position integer PRIMARY KEY, data BLOB);";
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		SqliteCommand val2 = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val2).CommandText = "CREATE TABLE IF NOT EXISTS blockidmapping (id integer PRIMARY KEY, data BLOB);";
			((DbCommand)(object)val2).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
	}

	public void Purge()
	{
		SqliteCommand val = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "delete FROM mappiece";
			((DbCommand)(object)val).ExecuteNonQuery();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public MapPieceDB[] GetMapPieces(List<FastVec2i> chunkCoords)
	{
		MapPieceDB[] array = new MapPieceDB[chunkCoords.Count];
		for (int i = 0; i < chunkCoords.Count; i++)
		{
			((DbParameter)(object)getMapPieceCmd.Parameters["@pos"]).Value = chunkCoords[i].ToChunkIndex();
			SqliteDataReader val = getMapPieceCmd.ExecuteReader();
			try
			{
				while (((DbDataReader)(object)val).Read())
				{
					object obj = ((DbDataReader)(object)val)["data"];
					if (obj == null)
					{
						return null;
					}
					array[i] = SerializerUtil.Deserialize<MapPieceDB>(obj as byte[]);
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		return array;
	}

	public MapPieceDB GetMapPiece(FastVec2i chunkCoord)
	{
		((DbParameter)(object)getMapPieceCmd.Parameters["@pos"]).Value = chunkCoord.ToChunkIndex();
		SqliteDataReader val = getMapPieceCmd.ExecuteReader();
		try
		{
			if (((DbDataReader)(object)val).Read())
			{
				object obj = ((DbDataReader)(object)val)["data"];
				if (obj == null)
				{
					return null;
				}
				return SerializerUtil.Deserialize<MapPieceDB>(obj as byte[]);
			}
			return null;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void SetMapPieces(Dictionary<FastVec2i, MapPieceDB> pieces)
	{
		SqliteTransaction val = sqliteConn.BeginTransaction();
		try
		{
			setMapPieceCmd.Transaction = val;
			foreach (KeyValuePair<FastVec2i, MapPieceDB> piece in pieces)
			{
				((DbParameter)(object)setMapPieceCmd.Parameters["@pos"]).Value = piece.Key.ToChunkIndex();
				((DbParameter)(object)setMapPieceCmd.Parameters["@data"]).Value = SerializerUtil.Serialize(piece.Value);
				((DbCommand)(object)setMapPieceCmd).ExecuteNonQuery();
			}
			((DbTransaction)(object)val).Commit();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public MapBlockIdMappingDB GetMapBlockIdMappingDB()
	{
		SqliteCommand val = sqliteConn.CreateCommand();
		try
		{
			((DbCommand)(object)val).CommandText = "SELECT data FROM blockidmapping WHERE id=1";
			SqliteDataReader val2 = val.ExecuteReader();
			try
			{
				if (((DbDataReader)(object)val2).Read())
				{
					object obj = ((DbDataReader)(object)val2)["data"];
					return (obj == null) ? null : SerializerUtil.Deserialize<MapBlockIdMappingDB>(obj as byte[]);
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

	public void SetMapBlockIdMappingDB(MapBlockIdMappingDB mapping)
	{
		SqliteTransaction val = sqliteConn.BeginTransaction();
		try
		{
			using (DbCommand dbCommand = sqliteConn.CreateCommand())
			{
				dbCommand.Transaction = (DbTransaction?)(object)val;
				byte[] value = SerializerUtil.Serialize(mapping);
				dbCommand.CommandText = "INSERT OR REPLACE INTO mappiece (position, data) VALUES (@position,@data)";
				dbCommand.Parameters.Add(CreateParameter("position", DbType.UInt64, 1, dbCommand));
				dbCommand.Parameters.Add(CreateParameter("data", DbType.Object, value, dbCommand));
				dbCommand.ExecuteNonQuery();
			}
			((DbTransaction)(object)val).Commit();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public override void Close()
	{
		((Component)(object)setMapPieceCmd)?.Dispose();
		((Component)(object)getMapPieceCmd)?.Dispose();
		base.Close();
	}

	public override void Dispose()
	{
		((Component)(object)setMapPieceCmd)?.Dispose();
		((Component)(object)getMapPieceCmd)?.Dispose();
		base.Dispose();
	}
}
