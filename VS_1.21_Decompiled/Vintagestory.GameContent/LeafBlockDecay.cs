using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class LeafBlockDecay : ModSystem
{
	private class CheckDecayThread : IAsyncServerSystem
	{
		public static int leafDecayCheckTickInterval = 10;

		private HashSet<BlockPos> checkDecay;

		private HashSet<BlockPos> performDecay;

		private ICoreServerAPI sapi;

		public bool Stopping { get; set; }

		public CheckDecayThread(ICoreServerAPI sapi)
		{
			this.sapi = sapi;
		}

		public void Start(HashSet<BlockPos> checkDecay, HashSet<BlockPos> performDecay)
		{
			this.checkDecay = checkDecay;
			this.performDecay = performDecay;
			sapi.Server.AddServerThread("CheckLeafDecay", this);
		}

		public int OffThreadInterval()
		{
			return leafDecayCheckTickInterval;
		}

		public void OnSeparateThreadTick()
		{
			for (int i = 0; i < 100; i++)
			{
				if (checkDecay.Count == 0)
				{
					break;
				}
				BlockPos blockPos = null;
				lock (checkDecayLock)
				{
					blockPos = checkDecay.First();
					checkDecay.Remove(blockPos);
				}
				if (shouldDecay(blockPos))
				{
					lock (performDecayLock)
					{
						performDecay.Add(blockPos);
					}
				}
			}
		}

		public void ThreadDispose()
		{
			lock (checkDecayLock)
			{
				checkDecay.Clear();
			}
			lock (performDecayLock)
			{
				performDecay.Clear();
			}
		}

		private bool shouldDecay(BlockPos startPos)
		{
			Queue<Vec4i> queue = new Queue<Vec4i>();
			HashSet<BlockPos> hashSet = new HashSet<BlockPos>();
			IBlockAccessor blockAccessor = sapi.World.BlockAccessor;
			Block block = blockAccessor.GetBlock(startPos);
			if (canDecay(block))
			{
				queue.Enqueue(new Vec4i(startPos, 2));
				hashSet.Add(startPos);
				while (queue.Count > 0)
				{
					if (hashSet.Count > 600)
					{
						return false;
					}
					Vec4i vec4i = queue.Dequeue();
					for (int i = 0; i < Vec3i.DirectAndIndirectNeighbours.Length; i++)
					{
						Vec3i vec3i = Vec3i.DirectAndIndirectNeighbours[i];
						BlockPos blockPos = new BlockPos(vec4i.X + vec3i.X, vec4i.Y + vec3i.Y, vec4i.Z + vec3i.Z);
						if (hashSet.Contains(blockPos))
						{
							continue;
						}
						hashSet.Add(blockPos);
						block = blockAccessor.GetBlock(blockPos);
						if (preventsDecay(block))
						{
							return false;
						}
						int value = blockPos.X - startPos.X;
						int value2 = blockPos.Y - startPos.Y;
						int value3 = blockPos.Z - startPos.Z;
						if (Math.Abs(value) > 4 || Math.Abs(value2) > 4 || Math.Abs(value3) > 4)
						{
							if (block.Id != 0)
							{
								return false;
							}
						}
						else if (canDecay(block))
						{
							queue.Enqueue(new Vec4i(blockPos, 0));
						}
					}
				}
				return true;
			}
			return false;
		}
	}

	private ICoreServerAPI sapi;

	private HashSet<BlockPos> checkDecayQueue = new HashSet<BlockPos>();

	public static object checkDecayLock = new object();

	private HashSet<BlockPos> performDecayQueue = new HashSet<BlockPos>();

	public static object performDecayLock = new object();

	private CheckDecayThread checkDecayThread;

	public static int leafRemovalInterval = 1000;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.SaveGameLoaded += onSaveGameLoaded;
		api.Event.GameWorldSave += onGameGettingSaved;
		api.Event.RegisterEventBusListener(onLeafDecayEventReceived, 0.5, "testForDecay");
		api.Event.RegisterGameTickListener(processReadyToDecayQueue, leafRemovalInterval);
		sapi.ChatCommands.GetOrCreate("debug").BeginSubCommand("leafdecaydebug").WithDescription("Shows leaf decay stats")
			.RequiresPrivilege(Privilege.controlserver)
			.HandleWith((TextCommandCallingArgs _) => TextCommandResult.Success("Queue sizes: pdq: " + performDecayQueue.Count + " / cdq: " + checkDecayQueue.Count))
			.EndSubCommand();
	}

	private void processReadyToDecayQueue(float dt)
	{
		if (performDecayQueue.Count != 0)
		{
			BlockPos blockPos = null;
			lock (performDecayLock)
			{
				blockPos = performDecayQueue.First();
				performDecayQueue.Remove(blockPos);
			}
			doDecay(blockPos);
		}
	}

	private void onLeafDecayEventReceived(string eventName, ref EnumHandling handling, IAttribute data)
	{
		if (checkDecayThread != null)
		{
			TreeAttribute treeAttribute = data as TreeAttribute;
			BlockPos pos = new BlockPos(treeAttribute.GetInt("x"), treeAttribute.GetInt("y"), treeAttribute.GetInt("z"));
			queueNeighborsForCheckDecay(pos);
		}
	}

	private void queueNeighborsForCheckDecay(BlockPos pos)
	{
		lock (checkDecayLock)
		{
			for (int i = 0; i < Vec3i.DirectAndIndirectNeighbours.Length; i++)
			{
				Vec3i vec3i = Vec3i.DirectAndIndirectNeighbours[i];
				Block blockRaw = sapi.World.BlockAccessor.GetBlockRaw(pos.X + vec3i.X, pos.InternalY + vec3i.Y, pos.Z + vec3i.Z);
				if (blockRaw.Id != 0 && canDecay(blockRaw))
				{
					checkDecayQueue.Add(pos.AddCopy(vec3i));
				}
			}
		}
	}

	private void doDecay(BlockPos pos)
	{
		if (canDecay(sapi.World.BlockAccessor.GetBlock(pos)))
		{
			sapi.World.BlockAccessor.SetBlock(0, pos);
			for (int i = 0; i < 6; i++)
			{
				sapi.World.BlockAccessor.MarkBlockDirty(pos.AddCopy(BlockFacing.ALLFACES[i]));
			}
			queueNeighborsForCheckDecay(pos);
		}
	}

	private void onGameGettingSaved()
	{
		using FastMemoryStream ms = new FastMemoryStream();
		lock (checkDecayLock)
		{
			sapi.WorldManager.SaveGame.StoreData("checkDecayQueue", SerializerUtil.Serialize(checkDecayQueue, ms));
			sapi.WorldManager.SaveGame.StoreData("performDecayQueue", SerializerUtil.Serialize(performDecayQueue, ms));
		}
	}

	private void onSaveGameLoaded()
	{
		sapi.Logger.Debug("Loading leaf block decay system");
		checkDecayQueue = deserializeQueue("checkDecayQueue");
		performDecayQueue = deserializeQueue("performDecayQueue");
		checkDecayThread = new CheckDecayThread(sapi);
		checkDecayThread.Start(checkDecayQueue, performDecayQueue);
		sapi.Logger.Debug("Finished loading leaf block decay system");
	}

	private HashSet<BlockPos> deserializeQueue(string name)
	{
		try
		{
			byte[] data = sapi.WorldManager.SaveGame.GetData(name);
			if (data != null)
			{
				return SerializerUtil.Deserialize<HashSet<BlockPos>>(data);
			}
		}
		catch (Exception e)
		{
			sapi.World.Logger.Error("Failed loading LeafBlockDecay.{0}. Resetting. Exception:", name);
			sapi.World.Logger.Error(e);
		}
		return new HashSet<BlockPos>();
	}

	public static bool canDecay(Block block)
	{
		if (block.BlockMaterial == EnumBlockMaterial.Leaves)
		{
			return block.Attributes?.IsTrue("canDecay") ?? false;
		}
		return false;
	}

	public static bool preventsDecay(Block block)
	{
		if (block.Id != 0)
		{
			return block.Attributes?.IsTrue("preventsDecay") ?? false;
		}
		return false;
	}
}
