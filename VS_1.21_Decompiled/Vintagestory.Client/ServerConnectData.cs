using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using DnsClient;
using DnsClient.Protocol;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class ServerConnectData
{
	public string HostRaw;

	public string Host;

	public int Port;

	public string ServerPassword;

	public bool IsServePasswordProtected;

	public string ErrorMessage;

	public int PositionInQueue;

	public bool Connected;

	public string PlayerUID => ClientSettings.PlayerUID;

	public string PlayerName => ClientSettings.PlayerName;

	public string MpToken => ClientSettings.MpToken;

	public static ServerConnectData FromHost(string host)
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Expected O, but got Unknown
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		ServerConnectData serverConnectData = new ServerConnectData();
		serverConnectData.HostRaw = host;
		if (host.StartsWithOrdinal("vh-"))
		{
			host = ResolveConnectionString(host);
			if (host.Length == 0 || host == ":")
			{
				throw new Exception("Invalid Vintagehosting address '" + serverConnectData.HostRaw + "' - no such server exists. Please double check that you entered the correct address");
			}
		}
		string error;
		UriInfo uriInfo = NetUtil.getUriInfo(host, out error);
		ScreenManager.Platform.Logger.Notification("Connecting to " + uriInfo.Hostname + "...");
		if (!uriInfo.Port.HasValue && !NetUtil.IsPrivateIp(uriInfo.Hostname))
		{
			try
			{
				LookupClient val = new LookupClient(new LookupClientOptions
				{
					UseCache = true,
					Timeout = new TimeSpan(0, 0, 4),
					Retries = 2
				});
				if (val.NameServers.Count == 0)
				{
					throw new Exception("No name servers found - Please make sure you are connected to the internet.");
				}
				IDnsQueryResponse val2 = val.Query("_vintagestory._tcp." + host, (QueryType)33, (QueryClass)1);
				if (!val2.HasError)
				{
					SrvRecord val3 = val2.Answers.OfType<SrvRecord>().FirstOrDefault();
					if (val3 != null)
					{
						uriInfo.Port = val3.Port;
						DnsString target = val3.Target;
						if (((target != null) ? target.Value : null) != null)
						{
							uriInfo.Hostname = val3.Target.Value;
						}
						ScreenManager.Platform.Logger.Notification("SRV record found - port " + val3.Port + ", target " + val3.Target.Value);
					}
					else
					{
						ScreenManager.Platform.Logger.Notification("No SRV record found, will connect to supplied hostname");
					}
				}
				else
				{
					ScreenManager.Platform.Logger.Error("Unable to read srv record, will connect to supplied hostname. Error: " + val2.ErrorMessage + "\r\n" + (object)val2.Header);
				}
			}
			catch (Exception e)
			{
				ScreenManager.Platform.Logger.Error("Exception thrown during SRV record lookup on {0}. Will ignore SRV record.", host);
				ScreenManager.Platform.Logger.Error(e);
			}
		}
		serverConnectData.ErrorMessage = error;
		serverConnectData.Port = ((!uriInfo.Port.HasValue) ? 42420 : uriInfo.Port.Value);
		serverConnectData.Host = uriInfo.Hostname;
		serverConnectData.ServerPassword = uriInfo.Password;
		serverConnectData.IsServePasswordProtected = uriInfo.Password != null;
		return serverConnectData;
	}

	private static string ResolveConnectionString(string host)
	{
		FormUrlEncodedContent postData = new FormUrlEncodedContent(new KeyValuePair<string, string>[1]
		{
			new KeyValuePair<string, string>("host", host)
		});
		Uri uri = new Uri("https://auth3.vintagestory.at/resolveserverhost");
		ServerHostResolveResp serverHostResolveResp = JsonUtil.FromString<ServerHostResolveResp>(VSWebClient.Inst.Post(uri, postData));
		if (serverHostResolveResp.Host == null || serverHostResolveResp.Host.Length == 0)
		{
			throw new ArgumentException("Sorry, no such vintagehosting server known");
		}
		if (serverHostResolveResp.Status == "expired")
		{
			throw new ArgumentException("Sorry, this vintagehosting server is expired, the owner needs to purchase more server time.");
		}
		return serverHostResolveResp.Host;
	}
}
