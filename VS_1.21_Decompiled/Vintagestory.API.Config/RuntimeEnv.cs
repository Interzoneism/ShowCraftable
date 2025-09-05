using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Vintagestory.API.Util;

namespace Vintagestory.API.Config;

public static class RuntimeEnv
{
	public static bool DebugTextureDispose;

	public static bool DebugVAODispose;

	public static bool DebugSoundDispose;

	public static bool DebugOutOfRangeBlockAccess;

	public static bool DebugThreadPool;

	public static int MainThreadId;

	public static int ServerMainThreadId;

	public static float GUIScale;

	public static readonly OS OS;

	public static readonly string EnvSearchPathName;

	public static readonly bool IsWaylandSession;

	public static readonly bool IsDevEnvironment;

	static RuntimeEnv()
	{
		DebugTextureDispose = false;
		DebugVAODispose = false;
		DebugSoundDispose = false;
		DebugOutOfRangeBlockAccess = false;
		DebugThreadPool = false;
		IsDevEnvironment = !Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets"));
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			OS = OS.Windows;
			EnvSearchPathName = "PATH";
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			OS = OS.Linux;
			EnvSearchPathName = "LD_LIBRARY_PATH";
			string environmentVariable = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
			string environmentVariable2 = Environment.GetEnvironmentVariable("OPENTK_4_USE_WAYLAND");
			if (environmentVariable == "wayland" && environmentVariable2 == null)
			{
				Environment.SetEnvironmentVariable("OPENTK_4_USE_WAYLAND", "0");
				IsWaylandSession = false;
			}
			else
			{
				IsWaylandSession = environmentVariable == "wayland" && environmentVariable2 != "0";
			}
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			OS = OS.Mac;
			EnvSearchPathName = "DYLD_FRAMEWORK_PATH";
		}
	}

	public static string GetLocalIpAddress()
	{
		try
		{
			NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface networkInterface in allNetworkInterfaces)
			{
				if (networkInterface.OperationalStatus != OperationalStatus.Up)
				{
					continue;
				}
				IPInterfaceProperties iPProperties = networkInterface.GetIPProperties();
				if (iPProperties.GatewayAddresses.Count == 0)
				{
					continue;
				}
				foreach (UnicastIPAddressInformation unicastAddress in iPProperties.UnicastAddresses)
				{
					if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(unicastAddress.Address))
					{
						return unicastAddress.Address.ToString();
					}
				}
			}
			return "Unknown ip";
		}
		catch (Exception)
		{
			try
			{
				return Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault((IPAddress ip) => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();
			}
			catch (Exception)
			{
				return "Unknown ip";
			}
		}
	}

	public static string GetOsString()
	{
		switch (OS)
		{
		case OS.Windows:
			return $"Windows {Environment.OSVersion.Version}";
		case OS.Mac:
			return $"Mac {Environment.OSVersion.Version}";
		case OS.Linux:
			try
			{
				if (File.Exists("/etc/os-release"))
				{
					string value = File.ReadAllLines("/etc/os-release").FirstOrDefault((string line) => line.StartsWithOrdinal("PRETTY_NAME="))?.Split('=').ElementAt(1).Trim('"');
					return $"Linux ({value}) [Kernel {Environment.OSVersion.Version}]";
				}
			}
			catch (Exception)
			{
			}
			return $"Linux (Unknown) [Kernel {Environment.OSVersion.Version}]";
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
