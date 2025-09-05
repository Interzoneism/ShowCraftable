using System.Collections.Generic;

namespace Vintagestory.Server;

public class ServerSystemNotifyPing : ServerSystem
{
	private Timer pingtimer = new Timer
	{
		Interval = 1.0,
		MaxDeltaTime = 5.0
	};

	public ServerSystemNotifyPing(ServerMain server)
		: base(server)
	{
		server.RegisterGameTickListener(OnEveryFewSeconds, 5000);
		server.PacketHandlers[2] = HandlePingReply;
		server.PacketHandlingOnConnectingAllowed[2] = true;
	}

	public override void OnServerTick(float dt)
	{
		pingtimer.Update(PingTimerTick);
	}

	private void OnEveryFewSeconds(float t1)
	{
		server.BroadcastPlayerPings();
	}

	private void HandlePingReply(Packet_Client packet, ConnectedClient client)
	{
		client.Ping.OnReceive(server.totalUnpausedTime.ElapsedMilliseconds);
		client.LastPing = (float)client.Ping.RoundtripTimeTotalMilliseconds() / 1000f;
	}

	private void PingTimerTick()
	{
		if (server.exit.GetExit())
		{
			return;
		}
		long elapsedMilliseconds = server.totalUnpausedTime.ElapsedMilliseconds;
		List<int> list = new List<int>();
		foreach (var (num2, connectedClient2) in server.Clients)
		{
			if (!connectedClient2.Ping.DidReplyOnLastPing)
			{
				if (connectedClient2.Ping.DidTimeout(elapsedMilliseconds) && !connectedClient2.IsSinglePlayerClient)
				{
					long num3 = (elapsedMilliseconds - connectedClient2.Ping.TimeSendMilliSeconds) / 1000;
					ServerMain.Logger.Notification(num3 + "s ping timeout for " + connectedClient2.PlayerName + ". Kicking player...");
					list.Add(num2);
				}
			}
			else
			{
				server.SendPacket(num2, ServerPackets.Ping());
				connectedClient2.Ping.OnSend(elapsedMilliseconds);
			}
			if (!connectedClient2.FallBackToTcp && !connectedClient2.IsSinglePlayerClient && connectedClient2.Ping.DidUdpTimeout(elapsedMilliseconds))
			{
				connectedClient2.FallBackToTcp = true;
				Packet_Server packet = new Packet_Server
				{
					Id = 78
				};
				server.SendPacket(num2, packet);
				float value = (float)(elapsedMilliseconds - connectedClient2.Ping.TimeReceivedUdp) / 1000f;
				ServerMain.Logger.Debug($"UDP: Server did not receive any UDP packets from Client {connectedClient2.Id} for {value}s, telling the client to send positions over TCP, server switches to TCP too.");
			}
		}
		foreach (int item in list)
		{
			if (server.Clients.TryGetValue(item, out var value2))
			{
				server.DisconnectPlayer(value2);
			}
		}
	}
}
