using System;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.Server;

internal class ServerSystemClientAwareness : ServerSystem
{
	private Dictionary<int, ClientStatistics> clients = new Dictionary<int, ClientStatistics>();

	public ServerSystemClientAwareness(ServerMain server)
		: base(server)
	{
		server.clientAwarenessEvents = new Dictionary<EnumClientAwarenessEvent, List<Action<ClientStatistics>>>();
		server.clientAwarenessEvents[EnumClientAwarenessEvent.ChunkTransition] = new List<Action<ClientStatistics>>();
	}

	public override int GetUpdateInterval()
	{
		return 100;
	}

	public override void OnServerTick(float dt)
	{
		foreach (ClientStatistics value in clients.Values)
		{
			EnumClientAwarenessEvent? enumClientAwarenessEvent = value.DetectChanges();
			if (!enumClientAwarenessEvent.HasValue)
			{
				continue;
			}
			foreach (Action<ClientStatistics> item in server.clientAwarenessEvents[enumClientAwarenessEvent.Value])
			{
				item(value);
			}
		}
	}

	public void TriggerEvent(EnumClientAwarenessEvent clientEvent, int clientId)
	{
		if (!clients.TryGetValue(clientId, out var value) || !server.clientAwarenessEvents.TryGetValue(clientEvent, out var value2))
		{
			return;
		}
		foreach (Action<ClientStatistics> item in value2)
		{
			item(value);
		}
	}

	public override void OnPlayerJoin(ServerPlayer player)
	{
		EntityPos serverPos = player.Entity.ServerPos;
		clients[player.ClientId] = new ClientStatistics
		{
			client = player.client,
			lastChunkX = (int)serverPos.X / 32,
			lastChunkY = (int)serverPos.Y / 32,
			lastChunkZ = (int)serverPos.Z / 32
		};
	}

	public override void OnPlayerDisconnect(ServerPlayer player)
	{
		clients.Remove(player.ClientId);
	}
}
