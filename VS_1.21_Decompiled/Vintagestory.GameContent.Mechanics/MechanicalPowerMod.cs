using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent.Mechanics;

public class MechanicalPowerMod : ModSystem, IRenderer, IDisposable
{
	public MechNetworkRenderer Renderer;

	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	private IClientNetworkChannel clientNwChannel;

	private IServerNetworkChannel serverNwChannel;

	public ICoreAPI Api;

	private MechPowerData data = new MechPowerData();

	private bool allNetworksFullyLoaded = true;

	private List<MechanicalNetwork> nowFullyLoaded = new List<MechanicalNetwork>();

	public double RenderOrder => 0.0;

	public int RenderRange => 9999;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		Api = api;
		if (api.World is IClientWorldAccessor)
		{
			(api as ICoreClientAPI).Event.RegisterRenderer(this, EnumRenderStage.Before, "mechanicalpowertick");
			clientNwChannel = ((ICoreClientAPI)api).Network.RegisterChannel("vsmechnetwork").RegisterMessageType(typeof(MechNetworkPacket)).RegisterMessageType(typeof(NetworkRemovedPacket))
				.RegisterMessageType(typeof(MechClientRequestPacket))
				.SetMessageHandler<MechNetworkPacket>(OnPacket)
				.SetMessageHandler<NetworkRemovedPacket>(OnNetworkRemovePacket);
		}
		else
		{
			api.World.RegisterGameTickListener(OnServerGameTick, 20);
			serverNwChannel = ((ICoreServerAPI)api).Network.RegisterChannel("vsmechnetwork").RegisterMessageType(typeof(MechNetworkPacket)).RegisterMessageType(typeof(NetworkRemovedPacket))
				.RegisterMessageType(typeof(MechClientRequestPacket))
				.SetMessageHandler<MechClientRequestPacket>(OnClientRequestPacket);
		}
	}

	public long getTickNumber()
	{
		return data.tickNumber;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		capi = api;
		api.Event.BlockTexturesLoaded += onLoaded;
		api.Event.LeaveWorld += delegate
		{
			Renderer?.Dispose();
		};
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		base.StartServerSide(api);
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.ChunkDirty += Event_ChunkDirty;
	}

	protected void OnServerGameTick(float dt)
	{
		data.tickNumber++;
		foreach (MechanicalNetwork item in data.networksById.Values.ToList())
		{
			if (item.fullyLoaded && item.nodes.Count > 0)
			{
				item.ServerTick(dt, data.tickNumber);
			}
		}
	}

	protected void OnPacket(MechNetworkPacket networkMessage)
	{
		bool isNew = !data.networksById.ContainsKey(networkMessage.networkId);
		GetOrCreateNetwork(networkMessage.networkId).UpdateFromPacket(networkMessage, isNew);
	}

	protected void OnNetworkRemovePacket(NetworkRemovedPacket networkMessage)
	{
		data.networksById.Remove(networkMessage.networkId);
	}

	protected void OnClientRequestPacket(IServerPlayer player, MechClientRequestPacket networkMessage)
	{
		if (data.networksById.TryGetValue(networkMessage.networkId, out var value))
		{
			value.SendBlocksUpdateToClient(player);
		}
	}

	public void broadcastNetwork(MechNetworkPacket packet)
	{
		serverNwChannel.BroadcastPacket(packet);
	}

	private void Event_GameWorldSave()
	{
	}

	private void Event_SaveGameLoaded()
	{
		data = new MechPowerData();
	}

	private void onLoaded()
	{
		Renderer = new MechNetworkRenderer(capi, this);
	}

	internal void OnNodeRemoved(IMechanicalPowerDevice device)
	{
		if (device.Network != null)
		{
			RebuildNetwork(device.Network, device);
		}
	}

	public void RebuildNetwork(MechanicalNetwork network, IMechanicalPowerDevice nowRemovedNode = null)
	{
		network.Valid = false;
		if (Api.Side == EnumAppSide.Server)
		{
			DeleteNetwork(network);
		}
		if (network.nodes.Values.Count == 0)
		{
			return;
		}
		IMechanicalPowerNode[] array = network.nodes.Values.ToArray();
		IMechanicalPowerNode[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].LeaveNetwork();
		}
		array2 = array;
		foreach (IMechanicalPowerNode mechanicalPowerNode in array2)
		{
			if (!(mechanicalPowerNode is IMechanicalPowerDevice))
			{
				continue;
			}
			IMechanicalPowerDevice mechanicalPowerDevice = Api.World.BlockAccessor.GetBlockEntity((mechanicalPowerNode as IMechanicalPowerDevice).Position)?.GetBehavior<BEBehaviorMPBase>();
			if (mechanicalPowerDevice == null)
			{
				continue;
			}
			BlockFacing propagationDirection = mechanicalPowerDevice.GetPropagationDirection();
			if (mechanicalPowerDevice.OutFacingForNetworkDiscovery != null && (nowRemovedNode == null || mechanicalPowerDevice.Position != nowRemovedNode.Position))
			{
				MechanicalNetwork mechanicalNetwork = mechanicalPowerDevice.CreateJoinAndDiscoverNetwork(mechanicalPowerDevice.OutFacingForNetworkDiscovery);
				bool flag = mechanicalPowerDevice.GetPropagationDirection() == propagationDirection.Opposite;
				mechanicalNetwork.Speed = (flag ? (0f - network.Speed) : network.Speed);
				mechanicalNetwork.AngleRad = network.AngleRad;
				mechanicalNetwork.TotalAvailableTorque = (flag ? (0f - network.TotalAvailableTorque) : network.TotalAvailableTorque);
				mechanicalNetwork.NetworkResistance = network.NetworkResistance;
				if (Api.Side == EnumAppSide.Server)
				{
					mechanicalNetwork.broadcastData();
				}
			}
		}
	}

	public void RemoveDeviceForRender(IMechanicalPowerRenderable device)
	{
		Renderer?.RemoveDevice(device);
	}

	public void AddDeviceForRender(IMechanicalPowerRenderable device)
	{
		Renderer?.AddDevice(device);
	}

	public override void Dispose()
	{
		base.Dispose();
		Renderer?.Dispose();
	}

	public MechanicalNetwork GetOrCreateNetwork(long networkId)
	{
		if (!data.networksById.TryGetValue(networkId, out var value))
		{
			value = (data.networksById[networkId] = new MechanicalNetwork(this, networkId));
		}
		testFullyLoaded(value);
		return value;
	}

	public void testFullyLoaded(MechanicalNetwork mw)
	{
		if (Api.Side == EnumAppSide.Server && !mw.fullyLoaded)
		{
			mw.fullyLoaded = mw.testFullyLoaded(Api);
			allNetworksFullyLoaded &= mw.fullyLoaded;
		}
	}

	private void Event_ChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
	{
		if (allNetworksFullyLoaded || reason == EnumChunkDirtyReason.MarkedDirty)
		{
			return;
		}
		allNetworksFullyLoaded = true;
		nowFullyLoaded.Clear();
		foreach (MechanicalNetwork value in data.networksById.Values)
		{
			if (value.fullyLoaded)
			{
				continue;
			}
			allNetworksFullyLoaded = false;
			if (value.inChunks.ContainsKey(chunkCoord))
			{
				testFullyLoaded(value);
				if (value.fullyLoaded)
				{
					nowFullyLoaded.Add(value);
				}
			}
		}
		for (int i = 0; i < nowFullyLoaded.Count; i++)
		{
			RebuildNetwork(nowFullyLoaded[i]);
		}
	}

	public MechanicalNetwork CreateNetwork(IMechanicalPowerDevice powerProducerNode)
	{
		MechanicalNetwork mechanicalNetwork = new MechanicalNetwork(this, data.nextNetworkId);
		mechanicalNetwork.fullyLoaded = true;
		data.networksById[data.nextNetworkId] = mechanicalNetwork;
		data.nextNetworkId++;
		return mechanicalNetwork;
	}

	public void DeleteNetwork(MechanicalNetwork network)
	{
		data.networksById.Remove(network.networkId);
		serverNwChannel.BroadcastPacket(new NetworkRemovedPacket
		{
			networkId = network.networkId
		});
	}

	public void SendNetworkBlocksUpdateRequestToServer(long networkId)
	{
		clientNwChannel.SendPacket(new MechClientRequestPacket
		{
			networkId = networkId
		});
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (capi.IsGamePaused)
		{
			return;
		}
		foreach (MechanicalNetwork value in data.networksById.Values)
		{
			value.ClientTick(deltaTime);
		}
	}
}
