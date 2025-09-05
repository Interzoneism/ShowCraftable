using System;

namespace Vintagestory.Server.Database;

public class DatabaseUpgrader
{
	private string worldFilename;

	private int curVersion;

	private int destVersion;

	private ServerMain server;

	public DatabaseUpgrader(ServerMain server, string worldFilename, int curVersion, int destVersion)
	{
		this.server = server;
		this.worldFilename = worldFilename;
		this.curVersion = curVersion;
		this.destVersion = destVersion;
	}

	public void PerformUpgrade()
	{
		while (curVersion < destVersion)
		{
			ApplyUpgrader(curVersion + 1);
			curVersion++;
		}
	}

	private void ApplyUpgrader(int curVersion)
	{
		IDatabaseUpgrader databaseUpgrader = null;
		if (curVersion == 2)
		{
			databaseUpgrader = new DatabaseUpgraderToVersion2();
		}
		if (databaseUpgrader == null)
		{
			ServerMain.Logger.Event("No upgrader to " + curVersion + " found.");
			throw new Exception("No upgrader to " + curVersion + " found.");
		}
		databaseUpgrader.Upgrade(server, worldFilename);
	}
}
