using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Config;

public static class GameVersion
{
	public const string OverallVersion = "1.21.0";

	public const EnumGameBranch Branch = EnumGameBranch.Stable;

	public const string ShortGameVersion = "1.21.0";

	public static string LongGameVersion = "v1.21.0 (" + EnumGameBranch.Stable.ToString() + ")";

	public const string AssemblyVersion = "1.0.0.0";

	public const string APIVersion = "1.21.0";

	public const string NetworkVersion = "1.21.7";

	public const int WorldGenVersion = 3;

	public static int DatabaseVersion = 2;

	public const int ChunkdataVersion = 2;

	public static int BlockItemMappingVersion = 1;

	public const string CopyRight = "Copyright © 2016-2024 Anego Studios";

	private static string[] separators = new string[2] { ".", "-" };

	public static EnumReleaseType ReleaseType => GetReleaseType("1.21.0");

	public static int[] SplitVersionString(string version)
	{
		int num = version.IndexOf('-');
		string text = ((num < 1) ? version : version.Substring(0, num));
		if (text.CountChars('.') == 1)
		{
			text += ".0";
			version = ((num < 1) ? text : (text + version.Substring(num)));
		}
		string[] array = version.Split(separators, StringSplitOptions.None);
		if (array.Length <= 3)
		{
			array = array.Append("3");
		}
		else if (array[3] == "rc")
		{
			array[3] = "2";
		}
		else if (array[3] == "pre")
		{
			array[3] = "1";
		}
		else
		{
			array[3] = "0";
		}
		int[] array2 = new int[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			int.TryParse(array[i], out var result);
			array2[i] = result;
		}
		return array2;
	}

	public static EnumReleaseType GetReleaseType(string version)
	{
		return SplitVersionString(version)[3] switch
		{
			0 => EnumReleaseType.Development, 
			1 => EnumReleaseType.Preview, 
			2 => EnumReleaseType.Candidate, 
			3 => EnumReleaseType.Stable, 
			_ => throw new ArgumentException("Unknown release type"), 
		};
	}

	public static bool IsCompatibleApiVersion(string version)
	{
		int[] array = SplitVersionString(version);
		int[] array2 = SplitVersionString("1.21.0");
		if (array.Length < 2)
		{
			return false;
		}
		if (array2[0] == array[0])
		{
			return array2[1] == array[1];
		}
		return false;
	}

	public static bool IsCompatibleNetworkVersion(string version)
	{
		int[] array = SplitVersionString(version);
		int[] array2 = SplitVersionString("1.21.7");
		if (array.Length < 2)
		{
			return false;
		}
		if (array2[0] == array[0])
		{
			return array2[1] == array[1];
		}
		return false;
	}

	public static bool IsAtLeastVersion(string version)
	{
		return IsAtLeastVersion(version, "1.21.0");
	}

	public static bool IsAtLeastVersion(string version, string reference)
	{
		int[] array = SplitVersionString(reference);
		int[] array2 = SplitVersionString(version);
		for (int i = 0; i < array.Length; i++)
		{
			if (i >= array2.Length)
			{
				return false;
			}
			if (array[i] > array2[i])
			{
				return false;
			}
			if (array[i] < array2[i])
			{
				return true;
			}
		}
		return true;
	}

	public static bool IsLowerVersionThan(string version, string reference)
	{
		if (version != reference)
		{
			return !IsNewerVersionThan(version, reference);
		}
		return false;
	}

	public static bool IsNewerVersionThan(string version, string reference)
	{
		int[] array = SplitVersionString(reference);
		int[] array2 = SplitVersionString(version);
		for (int i = 0; i < array.Length; i++)
		{
			if (i >= array2.Length)
			{
				return false;
			}
			if (array[i] > array2[i])
			{
				return false;
			}
			if (array[i] < array2[i])
			{
				return true;
			}
		}
		return false;
	}

	public static void EnsureEqualVersionOrKillExecutable(ICoreAPI api, string version, string reference, string modName)
	{
		if (version != reference)
		{
			if (api.Side == EnumAppSide.Server)
			{
				Exception ex = new Exception(Lang.Get("versionmismatch-server", modName + ".dll"));
				((ICoreServerAPI)api).Server.ShutDown();
				throw ex;
			}
			Exception e = new Exception(Lang.Get("versionmismatch-client", modName + ".dll"));
			((ICoreClientAPI)api).Event.EnqueueMainThreadTask(delegate
			{
				throw e;
			}, "killgame");
		}
	}
}
