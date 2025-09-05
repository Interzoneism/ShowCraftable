using System;
using System.IO;
using Vintagestory.API.Config;

namespace Vintagestory.Common;

public class CleanInstallCheck
{
	public static bool IsCleanInstall()
	{
		if (RuntimeEnv.IsDevEnvironment)
		{
			return true;
		}
		string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		if (!File.Exists(Path.Combine(baseDirectory, "assets", "survival", "itemtypes", "bag", "backpack.json")))
		{
			bool num = File.Exists(Path.Combine(baseDirectory, "assets", "version-1.21.0.txt"));
			string[] files = Directory.GetFiles(Path.Combine(baseDirectory, "assets"), "version-*.txt", SearchOption.TopDirectoryOnly);
			if (num)
			{
				return files.Length == 1;
			}
			return false;
		}
		return false;
	}
}
