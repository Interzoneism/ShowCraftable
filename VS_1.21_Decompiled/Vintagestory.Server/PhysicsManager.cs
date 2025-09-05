using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;
using Vintagestory.Server.Systems;

namespace Vintagestory.Server;

public class PhysicsManager : LoadBalancedTask
{
	private class PhysicsOffthreadTasks
	{
		private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

		private readonly PhysicsManager physicsManager;

		private bool idle;

		private bool busy;

		internal FastMemoryStream buffer = new FastMemoryStream();

		public PhysicsOffthreadTasks(PhysicsManager manager)
		{
			physicsManager = manager;
		}

		internal void Start()
		{
			physicsManager.server.EventManager.TriggerPhysicsThreadStart();
			while (!physicsManager.ShouldExit())
			{
				if (queue.IsEmpty)
				{
					try
					{
						idle = true;
						lock (this)
						{
							Monitor.Wait(this, 10);
						}
						idle = false;
					}
					catch (ThreadInterruptedException)
					{
					}
				}
				Action result;
				while (queue.TryDequeue(out result))
				{
					busy = true;
					if (physicsManager.ShouldExit())
					{
						break;
					}
					try
					{
						result();
					}
					catch (Exception e)
					{
						ServerMain.Logger.Error(e);
					}
				}
				busy = false;
			}
		}

		internal void QueueAsyncTask(Action a)
		{
			if (physicsManager.server.ReducedServerThreads)
			{
				a();
				return;
			}
			queue.Enqueue(a);
			if (!idle)
			{
				return;
			}
			lock (this)
			{
				Monitor.Pulse(this);
			}
		}

		internal bool Busy()
		{
			if (!busy)
			{
				return !queue.IsEmpty;
			}
			return true;
		}
	}

	private const int HiResTrackingRange = 2500;

	public const float AttributesToClientsInterval = 0.2f;

	private const int firstTick = 1;

	public readonly ConcurrentQueue<IPhysicsTickable> toAdd = new ConcurrentQueue<IPhysicsTickable>();

	public readonly ConcurrentQueue<IPhysicsTickable> toRemove = new ConcurrentQueue<IPhysicsTickable>();

	private const float tickInterval = 1f / 30f;

	private readonly ICoreServerAPI sapi;

	private readonly ServerUdpNetwork udpNetwork;

	public readonly IServerNetworkChannel AnimationsAndTagsChannel;

	private readonly ServerMain server;

	private readonly LoadBalancer loadBalancer;

	private int maxPhysicsThreads;

	private int ticksToDo;

	private readonly long listener;

	private float physicsTickAccum;

	private float attrUpdateAccum;

	private readonly List<IPhysicsTickable> tickables = new List<IPhysicsTickable>();

	private ServerSystemEntitySimulation es;

	private List<Packet_EntityAttributes> cliententitiesFullUpdate = new List<Packet_EntityAttributes>();

	private List<Packet_EntityAttributeUpdate> cliententitiesPartialUpdate = new List<Packet_EntityAttributeUpdate>();

	private List<Packet_EntityAttributes> cliententitiesDebugUpdate = new List<Packet_EntityAttributes>();

	private Dictionary<long, Packet_EntityAttributes>[] entitiesFullUpdate;

	private Dictionary<long, Packet_EntityAttributeUpdate>[] entitiesPartialUpdate;

	private Dictionary<long, Packet_EntityAttributes>[] entitiesDebugUpdate;

	private Dictionary<long, Packet_EntityPosition>[] entitiesPositionPackets;

	private Dictionary<long, AnimationPacket>[] entitiesAnimPackets;

	private ConcurrentDictionary<long, EntityTagPacket> entitiesTagPackets;

	private FastMemoryStream[] entitiesUpdateReusableBuffers;

	private double[] positions = new double[3];

	private readonly EntityDespawnData outofRangeDespawnData = new EntityDespawnData
	{
		Reason = EnumDespawnReason.OutOfRange
	};

	private List<long> alreadyTracked = new List<long>();

	private List<Entity> newlyTracked = new List<Entity>();

	private List<ConnectedClient> ClientList;

	private ConcurrentBag<Entity> stateChanges = new ConcurrentBag<Entity>();

	private PhysicsOffthreadTasks offthreadProcess;

	private int currentTick;

	private static float rateModifier = 1f;

	private float deltaT;

	public PhysicsManager(ICoreServerAPI sapi, ServerUdpNetwork udpNetwork)
	{
		this.sapi = sapi;
		this.udpNetwork = udpNetwork;
		AnimationsAndTagsChannel = sapi.Network.RegisterChannel("EntityAnims");
		AnimationsAndTagsChannel.RegisterMessageType<AnimationPacket>().RegisterMessageType<BulkAnimationPacket>().RegisterMessageType<EntityTagPacket>();
		maxPhysicsThreads = Math.Clamp(MagicNum.MaxPhysicsThreads, 1, 8);
		if (sapi.Server.ReducedServerThreads)
		{
			maxPhysicsThreads = 1;
		}
		entitiesFullUpdate = new Dictionary<long, Packet_EntityAttributes>[maxPhysicsThreads];
		entitiesPartialUpdate = new Dictionary<long, Packet_EntityAttributeUpdate>[maxPhysicsThreads];
		entitiesDebugUpdate = new Dictionary<long, Packet_EntityAttributes>[maxPhysicsThreads];
		entitiesPositionPackets = new Dictionary<long, Packet_EntityPosition>[maxPhysicsThreads];
		entitiesAnimPackets = new Dictionary<long, AnimationPacket>[maxPhysicsThreads];
		entitiesTagPackets = new ConcurrentDictionary<long, EntityTagPacket>();
		entitiesUpdateReusableBuffers = new FastMemoryStream[maxPhysicsThreads];
		for (int i = 0; i < entitiesFullUpdate.Length; i++)
		{
			entitiesFullUpdate[i] = new Dictionary<long, Packet_EntityAttributes>();
		}
		for (int j = 0; j < entitiesPartialUpdate.Length; j++)
		{
			entitiesPartialUpdate[j] = new Dictionary<long, Packet_EntityAttributeUpdate>();
		}
		for (int k = 0; k < entitiesDebugUpdate.Length; k++)
		{
			entitiesDebugUpdate[k] = new Dictionary<long, Packet_EntityAttributes>();
		}
		for (int l = 0; l < entitiesPositionPackets.Length; l++)
		{
			entitiesPositionPackets[l] = new Dictionary<long, Packet_EntityPosition>();
		}
		for (int m = 0; m < entitiesAnimPackets.Length; m++)
		{
			entitiesAnimPackets[m] = new Dictionary<long, AnimationPacket>();
		}
		for (int n = 0; n < entitiesUpdateReusableBuffers.Length; n++)
		{
			entitiesUpdateReusableBuffers[n] = new FastMemoryStream();
		}
		server = sapi.World as ServerMain;
		loadBalancer = new LoadBalancer(this, ServerMain.Logger);
		loadBalancer.CreateDedicatedThreads(maxPhysicsThreads, "physicsManager", server.Serverthreads);
		offthreadProcess = new PhysicsOffthreadTasks(this);
		if (!server.ReducedServerThreads)
		{
			Thread thread = TyronThreadPool.CreateDedicatedThread(offthreadProcess.Start, "physicsManagerHelper");
			thread.IsBackground = true;
			thread.Priority = Thread.CurrentThread.Priority;
			server.Serverthreads.Add(thread);
		}
		listener = server.RegisterGameTickListener(ServerTick, 1);
		rateModifier = 1f;
		PhysicsBehaviorBase.InitServerMT(sapi);
	}

	public void Init()
	{
		es = server.Systems.First((ServerSystem s) => s is ServerSystemEntitySimulation) as ServerSystemEntitySimulation;
		es.physicsManager = this;
	}

	public void ForceClientUpdateTick(ConnectedClient client)
	{
		attrUpdateAccum = 0.2f;
	}

	public void ServerTick(float dt)
	{
		ServerMain.FrameProfiler.Enter("physicsmanager-servertick");
		IPhysicsTickable result;
		while (!toAdd.IsEmpty && toAdd.TryDequeue(out result))
		{
			if (result != null && result.Entity != null)
			{
				if (result.Entity.ServerBehaviorsThreadsafe == null)
				{
					ServerMain.Logger.Warning("An entity " + result.Entity.Code.ToShortString() + " failed to complete initialisation, will not be physics ticked.");
				}
				else
				{
					tickables.Add(result);
				}
			}
		}
		IPhysicsTickable result2;
		while (!toRemove.IsEmpty && toRemove.TryDequeue(out result2))
		{
			if (result2 != null)
			{
				tickables.Remove(result2);
			}
		}
		physicsTickAccum += dt;
		deltaT = dt;
		if (physicsTickAccum > 0.4f)
		{
			int num = (int)((physicsTickAccum - 0.4f) / (1f / 30f));
			if (ServerMain.FrameProfiler.Enabled)
			{
				ServerMain.Logger.Warning("Over 400ms tick. Skipping {0} physics ticks.", num);
			}
			physicsTickAccum %= 0.4f;
		}
		ServerMain.FrameProfiler.Mark("physicsmanager-preparation");
		ticksToDo = (int)(physicsTickAccum / (1f / 30f));
		physicsTickAccum -= (float)ticksToDo * (1f / 30f);
		long num2 = Environment.TickCount + 1000;
		while (offthreadProcess.Busy() && Environment.TickCount < num2)
		{
			Thread.Sleep(0);
		}
		ServerMain.FrameProfiler.Mark("physicsmanager-waitingOnPreviousTick");
		BuildClientList(server.Clients.Values);
		attrUpdateAccum += dt;
		if (attrUpdateAccum >= 0.2f)
		{
			attrUpdateAccum = 0f;
		}
		ServerMain.FrameProfiler.Mark("physicsmanager-buildclientlist");
		int num3 = 1;
		if (ticksToDo > 0)
		{
			stateChanges.Clear();
			if (tickables.Count > 800 && maxPhysicsThreads > 1)
			{
				num3 = maxPhysicsThreads;
				loadBalancer.SynchroniseWorkToMainThread(this);
				loadBalancer.AwaitCompletionOnAllThreads(num3);
				ServerMain.FrameProfiler.Mark("physicsmanager-waitingForSlowestThread");
				if (attrUpdateAccum == 0f)
				{
					GatherUpdatePacketsFromAllThreads();
					ServerMain.FrameProfiler.Mark("physicsmanager-mergeThreadPackets");
				}
			}
			else
			{
				DoWork(0);
			}
			foreach (Entity stateChange in stateChanges)
			{
				ActiveStateChanged(stateChange);
			}
			stateChanges.Clear();
			currentTick += ticksToDo;
			float dt2 = (float)ticksToDo * (1f / 30f) * rateModifier;
			foreach (IPhysicsTickable tickable in tickables)
			{
				try
				{
					tickable.AfterPhysicsTick(dt2);
				}
				catch (Exception e)
				{
					ServerMain.Logger.Error(e);
				}
			}
			ServerMain.FrameProfiler.Mark("physicsmanager-afterphysicstick");
		}
		SendPositionsForNonTickableEntities(attrUpdateAccum == 0f);
		ServerMain.FrameProfiler.Mark("physicsmanager-nontickables");
		if (attrUpdateAccum == 0f)
		{
			foreach (ConnectedClient client in ClientList)
			{
				UpdateTrackedEntityLists(client, num3);
			}
			ServerMain.FrameProfiler.Mark("physicsmanager-updatetrackedentitylists");
			SendTrackedEntitiesStateChanges();
			offthreadProcess.QueueAsyncTask(delegate
			{
				SendAttributesViaTCP(ClientList);
			});
		}
		Entity[] spawnsToSend = null;
		int num4 = 0;
		lock (server.EntitySpawnSendQueue)
		{
			List<Entity> entitySpawnSendQueue = server.EntitySpawnSendQueue;
			num4 = entitySpawnSendQueue.Count;
			if (num4 > 0)
			{
				spawnsToSend = new Entity[num4];
				for (int num5 = 0; num5 < spawnsToSend.Length; num5++)
				{
					Entity entity = entitySpawnSendQueue[num5];
					if (entity.Alive)
					{
						spawnsToSend[num5] = entity;
					}
					else
					{
						num4--;
					}
				}
				entitySpawnSendQueue.Clear();
			}
		}
		ServerMain.FrameProfiler.Mark("physicsmanager-sendspawnlockwaiting");
		if (num4 > 0)
		{
			if (PrepareEntitySpawns(spawnsToSend, ClientList) > 0)
			{
				Dictionary<long, Packet_EntityPosition> entityPositionPackets = entitiesPositionPackets[0];
				entityPositionPackets.Clear();
				DoFirstPhysicsTicks(spawnsToSend, entityPositionPackets);
				offthreadProcess.QueueAsyncTask(delegate
				{
					SendEntitySpawns(spawnsToSend, ClientList, entityPositionPackets);
				});
			}
			ServerMain.FrameProfiler.Mark("physicsmanager-sendspawns");
		}
		ServerMain.FrameProfiler.Leave();
	}

	private void BuildClientList(ICollection<ConnectedClient> values)
	{
		List<ConnectedClient> list = (ClientList = new List<ConnectedClient>());
		foreach (ConnectedClient value in values)
		{
			if ((value.State == EnumClientState.Connected || value.State == EnumClientState.Playing) && value.Entityplayer != null)
			{
				list.Add(value);
			}
		}
		if (positions.Length < list.Count * 3)
		{
			positions = new double[list.Count * 3];
		}
		double[] array = positions;
		int num = 0;
		foreach (ConnectedClient item in list)
		{
			EntityPos position = item.Position;
			array[num] = position.X;
			array[num + 1] = position.Y;
			array[num + 2] = position.Z;
			num += 3;
			if (item.threadedTrackedEntities == null)
			{
				List<Entity>[] array2 = (item.threadedTrackedEntities = new List<Entity>[maxPhysicsThreads]);
				for (int i = 0; i < maxPhysicsThreads; i++)
				{
					array2[i] = new List<Entity>();
				}
			}
			else
			{
				List<Entity>[] threadedTrackedEntities = item.threadedTrackedEntities;
				for (int j = 0; j < maxPhysicsThreads; j++)
				{
					threadedTrackedEntities[j].Clear();
				}
			}
		}
	}

	public void UpdateTrackedEntitiesStates(ConnectedClient client)
	{
		List<ConnectedClient> clients = new List<ConnectedClient> { client };
		EntityPos position = client.Position;
		positions[0] = position.X;
		positions[1] = position.Y;
		positions[2] = position.Z;
		foreach (Entity value in server.LoadedEntities.Values)
		{
			if (UpdateTrackedEntityState(value, clients, 0))
			{
				ActiveStateChanged(value);
			}
		}
		UpdateTrackedEntityLists(client, 1);
	}

	private bool UpdateTrackedEntityState(Entity entity, List<ConnectedClient> clients, int zeroBasedThreadNum)
	{
		EntityPos serverPos = entity.ServerPos;
		double x = serverPos.X;
		double y = serverPos.Y;
		double z = serverPos.Z;
		double num = double.MaxValue;
		double num2 = entity.SimulationRange * entity.SimulationRange;
		double num3 = Math.Max(es.trackingRangeSq, num2);
		long entityId = entity.EntityId;
		long inChunkIndex3d = entity.InChunkIndex3d;
		bool allowOutsideLoadedRange = entity.AllowOutsideLoadedRange;
		double[] array = positions;
		int num4 = 0;
		foreach (ConnectedClient client in clients)
		{
			double num5 = x - array[num4];
			double num6 = y - array[num4 + 1];
			double num7 = z - array[num4 + 2];
			num4 += 3;
			double num8 = num5 * num5 + num7 * num7 + num6 * num6;
			if (num8 < num)
			{
				num = num8;
			}
			if (num8 < num3 && (client.DidSendChunk(inChunkIndex3d) || entityId == client.Player.Entity.EntityId || allowOutsideLoadedRange))
			{
				client.threadedTrackedEntities[zeroBasedThreadNum].Add(entity);
			}
		}
		if (num < num3)
		{
			entity.IsTracked = (byte)((num >= 2500.0) ? 1 : 2);
		}
		else
		{
			entity.IsTracked = 0;
			if (!(entity is EntityPlayer))
			{
				CompletePositionUpdate(entity);
			}
		}
		entity.NearestPlayerDistance = (float)Math.Sqrt(num);
		if (!entity.AlwaysActive)
		{
			bool flag = num < num2;
			if (flag != (entity.State == EnumEntityState.Active))
			{
				entity.State = ((!flag) ? EnumEntityState.Inactive : EnumEntityState.Active);
				return true;
			}
		}
		return false;
	}

	private void ActiveStateChanged(Entity entity)
	{
		entity.OnStateChanged(entity.State ^ EnumEntityState.Inactive);
	}

	private void CompletePositionUpdate(Entity entity)
	{
		entity.PreviousServerPos.SetFrom(entity.ServerPos);
		entity.IsTeleport = false;
	}

	private void UpdateTrackedEntityLists(ConnectedClient client, int threadCount)
	{
		List<Entity>[] threadedTrackedEntities = client.threadedTrackedEntities;
		List<Entity> list = threadedTrackedEntities[0];
		HashSet<long> trackedEntities = client.TrackedEntities;
		List<long> list2 = alreadyTracked;
		list2.EnsureCapacity(trackedEntities.Count);
		List<Entity> list3 = newlyTracked;
		foreach (Entity item in list)
		{
			long entityId;
			if (trackedEntities.Remove(entityId = item.EntityId))
			{
				list2.Add(entityId);
			}
			else
			{
				list3.Add(item);
			}
		}
		for (int i = 1; i < threadCount; i++)
		{
			foreach (Entity item2 in threadedTrackedEntities[i])
			{
				long entityId;
				if (trackedEntities.Remove(entityId = item2.EntityId))
				{
					list2.Add(entityId);
				}
				else
				{
					list3.Add(item2);
				}
				list.Add(item2);
			}
			threadedTrackedEntities[i].Clear();
		}
		foreach (long item3 in trackedEntities)
		{
			client.entitiesNowOutOfRange.Add(new EntityDespawn
			{
				ForClientId = client.Id,
				DespawnData = outofRangeDespawnData,
				EntityId = item3
			});
		}
		trackedEntities.Clear();
		trackedEntities.AddRange(list2);
		list2.Clear();
		foreach (Entity item4 in list3)
		{
			if (trackedEntities.Count >= MagicNum.TrackedEntitiesPerClient)
			{
				break;
			}
			trackedEntities.Add(item4.EntityId);
			client.entitiesNowInRange.Add(new EntityInRange
			{
				ForClientId = client.Id,
				Entity = item4
			});
		}
		list3.Clear();
	}

	public void SendTrackedEntitiesStateChanges()
	{
		List<AnimationPacket> list = new List<AnimationPacket>();
		FastMemoryStream fastMemoryStream = null;
		try
		{
			foreach (ConnectedClient client in ClientList)
			{
				if (client.entitiesNowInRange.Count > 0)
				{
					list.Clear();
					if (fastMemoryStream == null)
					{
						fastMemoryStream = new FastMemoryStream();
					}
					foreach (EntityInRange item in client.entitiesNowInRange)
					{
						Entity entity = item.Entity;
						if (entity is EntityPlayer entityPlayer)
						{
							server.PlayersByUid.TryGetValue(entityPlayer.PlayerUID, out var value);
							if (value != null)
							{
								server.SendPacket(item.ForClientId, ((ServerWorldPlayerData)value.WorldData).ToPacketForOtherPlayers(value));
							}
						}
						fastMemoryStream.Reset();
						BinaryWriter writer = new BinaryWriter(fastMemoryStream);
						server.SendPacket(item.ForClientId, ServerPackets.GetFullEntityPacket(entity, fastMemoryStream, writer));
						if (entity.AnimManager != null)
						{
							list.Add(new AnimationPacket(entity));
						}
					}
					BulkAnimationPacket message = new BulkAnimationPacket
					{
						Packets = list.ToArray()
					};
					AnimationsAndTagsChannel.SendPacket(message, client.Player);
					client.entitiesNowInRange.Clear();
				}
				if (client.entitiesNowOutOfRange.Count > 0)
				{
					server.SendPacket(client.Id, ServerPackets.GetEntityDespawnPacket(client.entitiesNowOutOfRange));
					client.entitiesNowOutOfRange.Clear();
				}
			}
		}
		finally
		{
			fastMemoryStream?.Dispose();
		}
		ServerMain.FrameProfiler.Mark("physicsmanager-sendstatechanged");
	}

	public void GatherUpdatePacketsFromAllThreads()
	{
		Dictionary<long, Packet_EntityAttributes> dict = entitiesFullUpdate[0];
		Dictionary<long, Packet_EntityAttributeUpdate> dict2 = entitiesPartialUpdate[0];
		Dictionary<long, Packet_EntityAttributes> dict3 = entitiesDebugUpdate[0];
		for (int i = 1; i < maxPhysicsThreads; i++)
		{
			dict.AddRange(entitiesFullUpdate[i]);
			dict2.AddRange(entitiesPartialUpdate[i]);
			dict3.AddRange(entitiesDebugUpdate[i]);
			entitiesFullUpdate[i].Clear();
			entitiesPartialUpdate[i].Clear();
			entitiesDebugUpdate[i].Clear();
		}
	}

	public void SendAttributesViaTCP(List<ConnectedClient> clientList)
	{
		Dictionary<long, Packet_EntityAttributes> dictionary = entitiesFullUpdate[0];
		Dictionary<long, Packet_EntityAttributeUpdate> dictionary2 = entitiesPartialUpdate[0];
		Dictionary<long, Packet_EntityAttributes> dictionary3 = entitiesDebugUpdate[0];
		List<Packet_EntityAttributes> list = cliententitiesFullUpdate;
		List<Packet_EntityAttributeUpdate> list2 = cliententitiesPartialUpdate;
		List<Packet_EntityAttributes> list3 = cliententitiesDebugUpdate;
		bool entityDebugMode = server.Config.EntityDebugMode;
		foreach (ConnectedClient client in clientList)
		{
			list.Clear();
			list2.Clear();
			list3.Clear();
			try
			{
				try
				{
					foreach (Entity item in client.threadedTrackedEntities[0])
					{
						long entityId = item.EntityId;
						if (dictionary.TryGetValue(entityId, out var value))
						{
							list.Add(value);
						}
						if (dictionary2.TryGetValue(entityId, out var value2))
						{
							list2.Add(value2);
						}
						if (entityDebugMode && dictionary3.TryGetValue(entityId, out var value3))
						{
							list3.Add(value3);
						}
					}
				}
				catch (InvalidOperationException)
				{
				}
				if (list.Count > 0 || list2.Count > 0)
				{
					server.SendPacket(client.Id, ServerPackets.GetBulkEntityAttributesPacket(list, list2));
				}
				if (list3.Count > 0)
				{
					server.SendPacket(client.Id, ServerPackets.GetBulkEntityDebugAttributesPacket(list3));
				}
			}
			finally
			{
				list.Clear();
				list2.Clear();
				list3.Clear();
				client.threadedTrackedEntities[0].Clear();
			}
		}
		dictionary.Clear();
		dictionary2.Clear();
		dictionary3.Clear();
	}

	public void BuildPositionPacket(Entity entity, bool forceUpdate, Dictionary<long, Packet_EntityPosition> entityPositionPackets, Dictionary<long, AnimationPacket> entityAnimPackets)
	{
		if (entity is EntityPlayer)
		{
			return;
		}
		EntityAgent entityAgent = entity as EntityAgent;
		if (entity.AnimManager != null && (entity.AnimManager.AnimationsDirty || entity.IsTeleport))
		{
			entityAnimPackets[entity.EntityId] = new AnimationPacket(entity);
			entity.AnimManager.AnimationsDirty = false;
		}
		if (forceUpdate || !entity.ServerPos.BasicallySameAs(entity.PreviousServerPos) || (entityAgent != null && entityAgent.Controls.Dirty) || entity.tagsDirty)
		{
			int intAndIncrement = entity.Attributes.GetIntAndIncrement("tick");
			entityPositionPackets[entity.EntityId] = ServerPackets.getEntityPositionPacket(entity.ServerPos, entity, intAndIncrement);
			if (entityAgent != null)
			{
				entityAgent.Controls.Dirty = false;
			}
			entity.tagsDirty = false;
		}
		CompletePositionUpdate(entity);
	}

	private EntityTagPacket BuildAttributesPackets(Entity entity, FastMemoryStream ms, bool debugMode, Dictionary<long, Packet_EntityAttributes> eFullUpdate, Dictionary<long, Packet_EntityAttributeUpdate> ePartialUpdate, Dictionary<long, Packet_EntityAttributes> eDebugUpdate)
	{
		EntityTagPacket result = null;
		SyncedTreeAttribute watchedAttributes = entity.WatchedAttributes;
		if (watchedAttributes.AllDirty)
		{
			ms.Reset();
			eFullUpdate[entity.EntityId] = ServerPackets.GetEntityPacket(ms, entity);
			entity.tagsDirty = false;
		}
		else
		{
			if (watchedAttributes.PartialDirty)
			{
				ms.Reset();
				ePartialUpdate[entity.EntityId] = ServerPackets.GetEntityPartialAttributePacket(ms, entity);
			}
			if (entity.tagsDirty)
			{
				result = ServerPackets.GetEntityTagPacket(entity);
				entity.tagsDirty = false;
			}
		}
		if (debugMode && (entity.DebugAttributes.AllDirty || entity.DebugAttributes.PartialDirty))
		{
			ms.Reset();
			eDebugUpdate[entity.EntityId] = ServerPackets.GetEntityDebugAttributePacket(ms, entity);
		}
		watchedAttributes.MarkClean();
		return result;
	}

	public void SendPositionsAndAnimations(Dictionary<long, Packet_EntityPosition> entityPositionPackets, Dictionary<long, AnimationPacket> entityAnimPackets, int zeroBasedThreadNum, bool stateUpdateTick)
	{
		List<Packet_EntityPosition> list = new List<Packet_EntityPosition>();
		List<AnimationPacket> list2 = new List<AnimationPacket>();
		List<EntityTagPacket> list3 = new List<EntityTagPacket>();
		ConcurrentDictionary<long, EntityTagPacket> concurrentDictionary = entitiesTagPackets;
		foreach (ConnectedClient client in ClientList)
		{
			list.Clear();
			list2.Clear();
			list3.Clear();
			if (stateUpdateTick)
			{
				List<Entity> list4 = client.threadedTrackedEntities[zeroBasedThreadNum];
				foreach (Entity item in list4)
				{
					long entityId = item.EntityId;
					if (entityPositionPackets.TryGetValue(entityId, out var value))
					{
						list.Add(value);
					}
					if (entityAnimPackets.TryGetValue(entityId, out var value2))
					{
						list2.Add(value2);
					}
					if (concurrentDictionary.TryGetValue(entityId, out var value3))
					{
						list3.Add(value3);
					}
				}
				if (entityAnimPackets.TryGetValue(client.Entityplayer.EntityId, out var value4) && !list4.Contains(client.Entityplayer))
				{
					list2.Add(value4);
				}
			}
			else
			{
				foreach (long trackedEntity in client.TrackedEntities)
				{
					if (entityPositionPackets.TryGetValue(trackedEntity, out var value5))
					{
						list.Add(value5);
					}
					if (entityAnimPackets.TryGetValue(trackedEntity, out var value6))
					{
						list2.Add(value6);
					}
					if (concurrentDictionary.TryGetValue(trackedEntity, out var value7))
					{
						list3.Add(value7);
					}
				}
			}
			int count = list.Count;
			if (count > 8 && !client.IsSinglePlayerClient && !client.FallBackToTcp)
			{
				for (int i = 0; i < count; i += 8)
				{
					Packet_EntityPosition[] array = new Packet_EntityPosition[Math.Min(8, count - i)];
					for (int j = 0; j < array.Length; j++)
					{
						array[j] = list[i + j];
					}
					Packet_BulkEntityPosition packet_BulkEntityPosition = new Packet_BulkEntityPosition();
					packet_BulkEntityPosition.SetEntityPositions(array);
					udpNetwork.SendPacket_Threadsafe(client, packet_BulkEntityPosition);
				}
			}
			else if (count > 0)
			{
				Packet_BulkEntityPosition packet_BulkEntityPosition2 = new Packet_BulkEntityPosition();
				packet_BulkEntityPosition2.SetEntityPositions(list.ToArray());
				udpNetwork.SendPacket_Threadsafe(client, packet_BulkEntityPosition2);
			}
			if (list2.Count > 0)
			{
				BulkAnimationPacket message = new BulkAnimationPacket
				{
					Packets = list2.ToArray()
				};
				AnimationsAndTagsChannel.SendPacket(message, client.Player);
			}
			if (list3.Count <= 0)
			{
				continue;
			}
			foreach (EntityTagPacket item2 in list3)
			{
				AnimationsAndTagsChannel.SendPacket(item2, client.Player);
			}
		}
	}

	private int PrepareEntitySpawns(Entity[] spawnsToSend, List<ConnectedClient> clientList)
	{
		int num = MagicNum.DefaultEntityTrackingRange * MagicNum.ServerChunkSize * MagicNum.DefaultEntityTrackingRange * MagicNum.ServerChunkSize;
		double[] array = positions;
		int count = clientList.Count;
		foreach (ConnectedClient client in clientList)
		{
			client.EntitySpawnsToSend.Clear();
		}
		int num2 = 0;
		for (int i = 0; i < spawnsToSend.Length; i++)
		{
			Entity entity = spawnsToSend[i];
			if (entity == null)
			{
				continue;
			}
			EntityPos serverPos = entity.ServerPos;
			long entityId = entity.EntityId;
			bool flag = false;
			for (int j = 0; j < array.Length && j < count; j += 3)
			{
				if (serverPos.InRangeOf(array[j], array[j + 1], array[j + 2], num))
				{
					ConnectedClient connectedClient = clientList[j / 3];
					connectedClient.TrackedEntities.Add(entityId);
					connectedClient.EntitySpawnsToSend.Add(entity);
					flag = true;
				}
			}
			if (flag)
			{
				num2++;
			}
			else
			{
				spawnsToSend[i] = null;
			}
		}
		return num2;
	}

	public void SendEntitySpawns(Entity[] spawnsToSend, List<ConnectedClient> clientList, Dictionary<long, Packet_EntityPosition> entityPositionPackets)
	{
		FastMemoryStream buffer = offthreadProcess.buffer;
		buffer.Reset();
		try
		{
			foreach (ConnectedClient client in clientList)
			{
				if (client.EntitySpawnsToSend.Count <= 0)
				{
					continue;
				}
				server.SendPacket(client.Id, ServerPackets.GetEntitySpawnPacket(client.EntitySpawnsToSend, buffer));
				foreach (Entity item in client.EntitySpawnsToSend)
				{
					if (entityPositionPackets.TryGetValue(item.EntityId, out var value))
					{
						Packet_Server packet = new Packet_Server
						{
							Id = 80,
							EntityPosition = value
						};
						server.SendPacket(client.Id, packet);
						item.ServerPos.SetFromPacket(value, item);
						item.Attributes.SetInt("tick", 2);
					}
				}
			}
			foreach (Entity entity in spawnsToSend)
			{
				if (entity != null)
				{
					entity.packet = null;
				}
			}
		}
		catch (Exception e)
		{
			ServerMain.Logger.Error(e);
		}
		entityPositionPackets.Clear();
	}

	private void DoFirstPhysicsTicks(Entity[] spawnsToSend, Dictionary<long, Packet_EntityPosition> entityPositionPackets)
	{
		float adjustedRate = 1f / 30f * rateModifier;
		EntityPos entityPos = new EntityPos();
		foreach (Entity entity in spawnsToSend)
		{
			if (entity != null && !(entity is EntityPlayer))
			{
				entityPos.SetFrom(entity.ServerPos);
				entityPositionPackets[entity.EntityId] = DoFirstPhysicsTick(entity, adjustedRate);
				entity.ServerPos.SetFrom(entityPos);
			}
		}
	}

	private static Packet_EntityPosition DoFirstPhysicsTick(Entity entity, float adjustedRate)
	{
		foreach (EntityBehavior behavior in entity.SidedProperties.Behaviors)
		{
			if (behavior is IPhysicsTickable physicsTickable)
			{
				physicsTickable.Ticking = true;
				physicsTickable.OnPhysicsTick(adjustedRate);
				physicsTickable.OnPhysicsTick(adjustedRate);
				physicsTickable.AfterPhysicsTick(adjustedRate);
				break;
			}
		}
		return ServerPackets.getEntityPositionPacket(entity.ServerPos, entity, 1);
	}

	internal void SendPrioritySpawn(Entity entity, ICollection<ConnectedClient> clientList)
	{
		Packet_Server entitySpawnPacket = ServerPackets.GetEntitySpawnPacket(new List<Entity>(1) { entity });
		Packet_EntityPosition entityPosition = DoFirstPhysicsTick(entity, 1f / 15f);
		Packet_Server packet = new Packet_Server
		{
			Id = 80,
			EntityPosition = entityPosition
		};
		entity.Attributes.SetInt("tick", 2);
		int squareDistance = MagicNum.DefaultEntityTrackingRange * MagicNum.ServerChunkSize * MagicNum.DefaultEntityTrackingRange * MagicNum.ServerChunkSize;
		EntityPos serverPos = entity.ServerPos;
		foreach (ConnectedClient client in clientList)
		{
			if ((client.State == EnumClientState.Connected || client.State == EnumClientState.Playing) && client.Entityplayer != null)
			{
				EntityPos serverPos2 = client.Entityplayer.ServerPos;
				if (serverPos.InRangeOf(serverPos2, squareDistance))
				{
					client.TrackedEntities.Add(entity.EntityId);
					server.SendPacket(client.Id, entitySpawnPacket);
					server.SendPacket(client.Id, packet);
				}
			}
		}
	}

	public void DoWork(int threadNumber)
	{
		float dt = 1f / 30f * rateModifier;
		List<IPhysicsTickable> list = tickables;
		int count = list.Count;
		int num = maxPhysicsThreads;
		if (threadNumber == 0)
		{
			threadNumber = 1;
			num = 1;
		}
		int num2 = 480;
		int num3 = num2 + (count - num2) * (threadNumber - 1) / num;
		int num4 = num2 + (count - num2) * threadNumber / num;
		if (threadNumber == 1)
		{
			num3 = 0;
		}
		Dictionary<long, Packet_EntityPosition> dictionary = entitiesPositionPackets[threadNumber - 1];
		Dictionary<long, AnimationPacket> dictionary2 = entitiesAnimPackets[threadNumber - 1];
		dictionary.Clear();
		dictionary2.Clear();
		if (attrUpdateAccum == 0f)
		{
			List<ConnectedClient> clientList = ClientList;
			for (int i = num3; i < num4; i++)
			{
				IPhysicsTickable physicsTickable = list[i];
				if (UpdateTrackedEntityState(physicsTickable.Entity, clientList, threadNumber - 1))
				{
					if (threadNumber == 1)
					{
						ActiveStateChanged(physicsTickable.Entity);
					}
					else
					{
						stateChanges.Add(physicsTickable.Entity);
					}
				}
			}
		}
		FrameProfilerUtil frameProfilerUtil = null;
		if (threadNumber == 1)
		{
			frameProfilerUtil = ServerMain.FrameProfiler;
			if (frameProfilerUtil == null)
			{
				throw new Exception("FrameProfiler on main thread was null - this should be impossible!");
			}
			if (!frameProfilerUtil.Enabled)
			{
				frameProfilerUtil = null;
			}
		}
		if (num == 1)
		{
			frameProfilerUtil?.Enter("entityphysics-mainthread (" + count + " entities, single-threaded) (" + ticksToDo + " physics ticks to do)");
		}
		else
		{
			frameProfilerUtil?.Enter("entityphysics-mainthread (" + count + " entities across " + num + " threads) (" + ticksToDo + " physics ticks to do)");
		}
		FastMemoryStream fastMemoryStream = entitiesUpdateReusableBuffers[threadNumber - 1];
		fastMemoryStream.Reset();
		bool entityDebugMode = server.Config.EntityDebugMode;
		Dictionary<long, Packet_EntityAttributes> dictionary3 = null;
		Dictionary<long, Packet_EntityAttributeUpdate> dictionary4 = null;
		Dictionary<long, Packet_EntityAttributes> dictionary5 = null;
		if (attrUpdateAccum == 0f)
		{
			dictionary3 = entitiesFullUpdate[threadNumber - 1];
			dictionary4 = entitiesPartialUpdate[threadNumber - 1];
			dictionary5 = entitiesDebugUpdate[threadNumber - 1];
			dictionary3.Clear();
			dictionary4.Clear();
			dictionary5.Clear();
		}
		try
		{
			int num5 = -1;
			float deltaTime = deltaT;
			bool flag = attrUpdateAccum == 0f;
			while (++num5 < ticksToDo)
			{
				int num6 = (ticksToDo - num5 + 1) % 2;
				num5 += num6;
				bool flag2 = num5 == ticksToDo - 1;
				bool flag3 = num5 == num6;
				bool forceUpdate = (num5 + currentTick) % 30 <= num6;
				for (int j = num3; j < num4; j++)
				{
					IPhysicsTickable physicsTickable2 = list[j];
					Entity entity = physicsTickable2.Entity;
					if (flag3)
					{
						EntityBehavior[] serverBehaviorsThreadsafe = entity.ServerBehaviorsThreadsafe;
						for (int k = 0; k < serverBehaviorsThreadsafe.Length; k++)
						{
							serverBehaviorsThreadsafe[k].OnGameTick(deltaTime);
							frameProfilerUtil?.Mark(serverBehaviorsThreadsafe[k].ProfilerName);
						}
					}
					if (entity.IsTracked == 0)
					{
						if (flag2)
						{
							entity.PositionTicked = true;
						}
						continue;
					}
					if (!((physicsTickable2 as PhysicsBehaviorBase)?.mountableSupplier?.Controller is EntityPlayer { Alive: not false }))
					{
						physicsTickable2.OnPhysicsTick(dt);
						if (frameProfilerUtil == null)
						{
							if (num6 != 0)
							{
								physicsTickable2.OnPhysicsTick(dt);
							}
							BuildPositionPacket(entity, forceUpdate, dictionary, dictionary2);
						}
						else
						{
							frameProfilerUtil.Mark("physicstick-oneentity");
							if (num6 != 0)
							{
								physicsTickable2.OnPhysicsTick(dt);
								frameProfilerUtil.Mark("physicstick-oneentity");
							}
							BuildPositionPacket(entity, forceUpdate, dictionary, dictionary2);
							frameProfilerUtil.Mark("physicstick-buildpospacket");
						}
					}
					if (!flag2)
					{
						continue;
					}
					if (flag)
					{
						EntityTagPacket entityTagPacket = BuildAttributesPackets(entity, fastMemoryStream, entityDebugMode, dictionary3, dictionary4, dictionary5);
						if (entityTagPacket != null)
						{
							entitiesTagPackets[entity.EntityId] = entityTagPacket;
						}
						frameProfilerUtil?.Mark("physicstick-buildattrpacket");
					}
					entity.PositionTicked = true;
				}
				SendPositionsAndAnimations(dictionary, dictionary2, threadNumber - 1, attrUpdateAccum == 0f);
				frameProfilerUtil?.Mark("physicsmanager-udp");
			}
		}
		catch (Exception e)
		{
			ServerMain.Logger.Error("Error while enumerating tickables. Tickables total count is " + list.Count);
			ServerMain.Logger.Error(e);
		}
		dictionary.Clear();
		dictionary2.Clear();
		frameProfilerUtil?.Leave();
	}

	private void SendPositionsForNonTickableEntities(bool doBuildAttributes)
	{
		Dictionary<long, Packet_EntityPosition> dictionary = entitiesPositionPackets[0];
		Dictionary<long, AnimationPacket> dictionary2 = entitiesAnimPackets[0];
		IDictionary<long, EntityTagPacket> dictionary3 = entitiesTagPackets;
		dictionary.Clear();
		dictionary2.Clear();
		dictionary3.Clear();
		bool forceUpdate = currentTick % 30 < ticksToDo;
		Dictionary<long, Packet_EntityAttributes> eFullUpdate = null;
		Dictionary<long, Packet_EntityAttributeUpdate> ePartialUpdate = null;
		Dictionary<long, Packet_EntityAttributes> eDebugUpdate = null;
		bool entityDebugMode = server.Config.EntityDebugMode;
		FastMemoryStream ms = null;
		if (doBuildAttributes)
		{
			eFullUpdate = entitiesFullUpdate[0];
			ePartialUpdate = entitiesPartialUpdate[0];
			eDebugUpdate = entitiesDebugUpdate[0];
			ms = new FastMemoryStream(64);
		}
		float deltaTime = deltaT;
		FrameProfilerUtil frameProfiler = ServerMain.FrameProfiler;
		foreach (Entity value in server.LoadedEntities.Values)
		{
			if (value.PositionTicked)
			{
				value.PositionTicked = false;
				continue;
			}
			EntityBehavior[] serverBehaviorsThreadsafe = value.ServerBehaviorsThreadsafe;
			foreach (EntityBehavior entityBehavior in serverBehaviorsThreadsafe)
			{
				entityBehavior.OnGameTick(deltaTime);
				frameProfiler.Mark(entityBehavior.ProfilerName);
			}
			if (doBuildAttributes && UpdateTrackedEntityState(value, ClientList, 0))
			{
				ActiveStateChanged(value);
			}
			if (value.IsTracked == 0)
			{
				continue;
			}
			if (ticksToDo > 0)
			{
				BuildPositionPacket(value, forceUpdate, dictionary, dictionary2);
			}
			if (doBuildAttributes)
			{
				EntityTagPacket entityTagPacket = BuildAttributesPackets(value, ms, entityDebugMode, eFullUpdate, ePartialUpdate, eDebugUpdate);
				if (entityTagPacket != null)
				{
					entitiesTagPackets[value.EntityId] = entityTagPacket;
				}
				frameProfiler.Mark("physicstick-buildattrpacket");
			}
		}
		if (dictionary.Count + dictionary2.Count + dictionary3.Count > 0)
		{
			SendPositionsAndAnimations(dictionary, dictionary2, 0, doBuildAttributes);
		}
		dictionary.Clear();
		dictionary2.Clear();
		dictionary3.Clear();
	}

	public bool ShouldExit()
	{
		if (!server.stopped)
		{
			return server.exit.exit;
		}
		return true;
	}

	public void HandleException(Exception e)
	{
		ServerMain.Logger.Error("Error thrown while ticking physics:\n{0}\n{1}", e.Message, e.StackTrace);
	}

	public void StartWorkerThread(int threadNum)
	{
		try
		{
			while (tickables.Count < 120)
			{
				if (ShouldExit())
				{
					return;
				}
				Thread.Sleep(15);
			}
		}
		catch (Exception)
		{
		}
		server.EventManager.TriggerPhysicsThreadStart();
		loadBalancer.WorkerThreadLoop(this, threadNum);
	}

	public void Dispose()
	{
		server?.UnregisterGameTickListener(listener);
		tickables.Clear();
	}
}
