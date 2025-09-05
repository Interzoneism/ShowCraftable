using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Vintagestory.API.Config;

namespace Vintagestory.API.Util;

public static class NetUtil
{
	public static void OpenUrlInBrowser(string url)
	{
		try
		{
			Process.Start("start \"" + url + "\"");
		}
		catch
		{
			if (RuntimeEnv.OS == OS.Windows)
			{
				url = url.Replace("&", "^&");
				if (Uri.TryCreate(url, UriKind.Absolute, out Uri result) && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps))
				{
					Process.Start(new ProcessStartInfo(url)
					{
						UseShellExecute = true
					});
				}
				else
				{
					Process.Start("explorer.exe", url);
				}
			}
			else if (RuntimeEnv.OS == OS.Linux)
			{
				url = "\"" + url + "\"";
				Process.Start("xdg-open", url);
			}
			else
			{
				if (RuntimeEnv.OS != OS.Mac)
				{
					throw;
				}
				url = "\"" + url + "\"";
				Process.Start("open", url);
			}
		}
	}

	public static bool IsPrivateIp(string ip)
	{
		string[] array = ip.Split('.');
		if (array.Length < 2)
		{
			return false;
		}
		int.TryParse(array[1], out var result);
		if (!(array[0] == "10") && (!(array[0] == "172") || result < 16 || result > 31))
		{
			if (array[0] == "192")
			{
				return array[1] == "168";
			}
			return false;
		}
		return true;
	}

	public static UriInfo getUriInfo(string uri, out string error)
	{
		bool flag = false;
		string password = null;
		if (uri.Contains("@"))
		{
			string[] array = uri.Split('@');
			password = array[0];
			uri = array[1];
		}
		if (IPAddress.TryParse(uri, out IPAddress address))
		{
			_ = address.AddressFamily;
			flag = address.AddressFamily == AddressFamily.InterNetworkV6;
		}
		string hostname = uri;
		int result = 0;
		int? port = null;
		if (!flag && uri.Contains(":"))
		{
			string[] array2 = uri.Split(':');
			hostname = array2[0];
			if (int.TryParse(array2[1], out result))
			{
				port = result;
			}
			else
			{
				error = Lang.Get("Invalid ipv6 address or invalid port number");
			}
		}
		if (flag && uri.Contains("]:"))
		{
			string[] array3 = uri.Split(new string[1] { "]:" }, StringSplitOptions.None);
			hostname = address.ToString();
			if (int.TryParse(array3[1], out result))
			{
				port = result;
			}
			else
			{
				error = Lang.Get("Invalid port number");
			}
		}
		error = null;
		return new UriInfo
		{
			Hostname = hostname,
			Password = password,
			Port = port
		};
	}
}
