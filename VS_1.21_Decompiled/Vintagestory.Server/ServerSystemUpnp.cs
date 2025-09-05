using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mono.Nat;
using Open.Nat;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

public class ServerSystemUpnp : ServerSystem
{
	private NatDevice natDevice;

	private IPAddress ipaddr;

	private INatDevice monoNatDevice;

	private Mapping mapping;

	private Mapping mappingUdp;

	private Mapping monoNatMapping;

	private Mapping monoNatMappingUdp;

	private bool wasOn;

	private long renewListenerId;

	public ServerSystemUpnp(ServerMain server)
		: base(server)
	{
		server.api.ChatCommands.Create("upnp").WithDescription("Runtime only setting. When turned on, the server will attempt to set up port forwarding through PMP or UPnP. When turned off, the port forward will be deleted again.").WithArgs(server.api.ChatCommands.Parsers.OptionalBool("on_off"))
			.RequiresPrivilege(Privilege.controlserver)
			.HandleWith(OnCmdToggleUpnp);
	}

	private TextCommandResult OnCmdToggleUpnp(TextCommandCallingArgs args)
	{
		bool flag = (bool)args[0];
		if (flag)
		{
			Initiate();
		}
		else
		{
			Dispose();
		}
		wasOn = flag;
		server.Config.RuntimeUpnp = wasOn;
		return TextCommandResult.Success("Upnp mode now " + (flag ? "on" : "off"));
	}

	public override void OnBeginRunGame()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected O, but got Unknown
		mapping = new Mapping((Protocol)0, server.CurrentPort, server.CurrentPort, 600, "Vintage Story TCP");
		mappingUdp = new Mapping((Protocol)1, server.CurrentPort, server.CurrentPort, 600, "Vintage Story UDP");
		monoNatMapping = new Mapping((Protocol)0, server.CurrentPort, server.CurrentPort, 600, "Vintage Story TCP");
		monoNatMappingUdp = new Mapping((Protocol)1, server.CurrentPort, server.CurrentPort, 600, "Vintage Story UDP");
		wasOn = server.Config.Upnp;
		if (wasOn && server.IsDedicatedServer)
		{
			Initiate();
		}
		server.Config.onUpnpChanged += onUpnpChanged;
	}

	private void onUpnpChanged()
	{
		if (wasOn && !server.Config.Upnp)
		{
			Dispose();
		}
		if (!wasOn && server.Config.Upnp)
		{
			Initiate();
		}
		wasOn = server.Config.Upnp;
	}

	public void Initiate()
	{
		string message;
		ServerMain.Logger.Event(message = "Begin searching for PMP and UPnP devices...");
		server.SendMessageToGroup(GlobalConstants.ServerInfoChatGroup, message, EnumChatType.Notification);
		findPmpDeviceAsync();
	}

	private async void findUpnpDeviceAsync()
	{
		CancellationTokenSource cts = new CancellationTokenSource(5000);
		try
		{
			onFoundNatDevice(await new NatDiscoverer().DiscoverDeviceAsync((PortMapper)2, cts), "UPnP");
		}
		catch (Exception)
		{
			findUpnpDeviceWithMonoNat();
		}
		cts.Dispose();
	}

	private async void findPmpDeviceAsync()
	{
		CancellationTokenSource cts = new CancellationTokenSource(5000);
		try
		{
			onFoundNatDevice(await new NatDiscoverer().DiscoverDeviceAsync((PortMapper)1, cts), "PMP");
		}
		catch (Exception)
		{
			findUpnpDeviceAsync();
		}
		cts.Dispose();
	}

	private void findUpnpDeviceWithMonoNat()
	{
		if (server.RunPhase != EnumServerRunPhase.Shutdown)
		{
			string message = $"No upnp or pmp device found after 5 seconds. Trying another method...";
			ServerMain.Logger.Event(message);
			server.SendMessageToGroup(GlobalConstants.ServerInfoChatGroup, message, EnumChatType.Notification);
			NatUtility.DeviceFound += MonoNatDeviceFound;
			NatUtility.StartDiscovery(Array.Empty<NatProtocol>());
			server.RegisterCallback(After5s, 5000);
		}
	}

	private void After5s(float dt)
	{
		if (monoNatDevice == null)
		{
			NatUtility.StopDiscovery();
			string message = "No upnp or pmp device found using either method. Giving up, sorry.";
			ServerMain.Logger.Event(message);
			server.SendMessageToGroup(GlobalConstants.ServerInfoChatGroup, message, EnumChatType.Notification, null, "nonatdevice");
			server.EventManager.TriggerUpnpComplete(success: false);
		}
		NatUtility.DeviceFound -= MonoNatDeviceFound;
	}

	private void CreateRenewCallback()
	{
		if (renewListenerId == 0L)
		{
			renewListenerId = server.RegisterCallback(RenewMapping, 540000);
		}
	}

	private void RenewMapping(float delta)
	{
		Task.Run(async delegate
		{
			_ = 2;
			try
			{
				if (monoNatDevice != null)
				{
					NatDeviceExtensions.CreatePortMap(monoNatDevice, monoNatMapping);
					NatDeviceExtensions.CreatePortMap(monoNatDevice, monoNatMappingUdp);
					ipaddr = NatDeviceExtensions.GetExternalIP(monoNatDevice);
				}
				if (natDevice != null)
				{
					await natDevice.CreatePortMapAsync(mapping);
					await natDevice.CreatePortMapAsync(mappingUdp);
					ipaddr = await natDevice.GetExternalIPAsync();
				}
			}
			catch (Exception e)
			{
				ServerMain.Logger.Warning("Failed to renew UnPn Port mapping, removing UPnP");
				ServerMain.Logger.Warning(e);
				Dispose();
			}
		});
	}

	private void MonoNatDeviceFound(object sender, DeviceEventArgs e)
	{
		try
		{
			monoNatDevice = e.Device;
			NatDeviceExtensions.CreatePortMap(monoNatDevice, monoNatMapping);
			NatDeviceExtensions.CreatePortMap(monoNatDevice, monoNatMappingUdp);
			CreateRenewCallback();
			ipaddr = NatDeviceExtensions.GetExternalIP(monoNatDevice);
			SendNatMessage();
		}
		catch (Exception e2)
		{
			ServerMain.Logger.Error("mono port map threw an exception:");
			ServerMain.Logger.Error(e2);
			monoNatDevice = null;
			ipaddr = null;
		}
	}

	private async void onFoundNatDevice(NatDevice device, string type)
	{
		if (natDevice != null)
		{
			return;
		}
		try
		{
			natDevice = device;
			ipaddr = await natDevice.GetExternalIPAsync();
			await natDevice.CreatePortMapAsync(mapping);
			await natDevice.CreatePortMapAsync(mappingUdp);
			CreateRenewCallback();
			SendNatMessage();
		}
		catch (Exception)
		{
			natDevice = null;
			if (type == "PMP")
			{
				findUpnpDeviceAsync();
			}
			if (type == "UPnP")
			{
				findUpnpDeviceWithMonoNat();
			}
		}
	}

	private void SendNatMessage()
	{
		if (NetUtil.IsPrivateIp(ipaddr.ToString()))
		{
			string message = $"Device with external ip {ipaddr.ToString()} found, but this is a private ip! Might not be accessible. Created mapping for port {mapping.PublicPort} anyway.";
			ServerMain.Logger.Event(message);
			server.SendMessageToGroup(GlobalConstants.ServerInfoChatGroup, message, EnumChatType.Notification, null, "foundnatdeviceprivip:" + ipaddr);
		}
		else
		{
			string message2 = $"Device with external ip {ipaddr.ToString()} found. Created mapping for port {mapping.PublicPort}!";
			ServerMain.Logger.Event(message2);
			server.SendMessageToGroup(GlobalConstants.ServerInfoChatGroup, message2, EnumChatType.Notification, null, "foundnatdevice:" + ipaddr);
		}
		server.EventManager.TriggerUpnpComplete(success: true);
	}

	public override void Dispose()
	{
		if (natDevice != null)
		{
			ServerMain.Logger.Event("Deleting port map on device with external ip {0}", ipaddr.ToString());
			Task.Run(async delegate
			{
				await natDevice.DeletePortMapAsync(mapping);
				await natDevice.DeletePortMapAsync(mappingUdp);
			});
		}
		if (monoNatDevice != null)
		{
			ServerMain.Logger.Event("Deleting port map on device with external ip {0}", ipaddr.ToString());
			NatDeviceExtensions.DeletePortMap(monoNatDevice, monoNatMapping);
			NatDeviceExtensions.DeletePortMap(monoNatDevice, monoNatMappingUdp);
		}
		server.UnregisterCallback(renewListenerId);
		renewListenerId = 0L;
		natDevice = null;
		monoNatDevice = null;
	}
}
