using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public abstract class BlockEntity
{
	protected List<long> TickHandlers;

	protected List<long> CallbackHandlers;

	public ICoreAPI Api;

	public BlockPos Pos;

	public List<BlockEntityBehavior> Behaviors = new List<BlockEntityBehavior>();

	public ItemStack stackForWorldgen;

	private ITreeAttribute missingBlockTree;

	public Block Block { get; set; }

	public BlockEntity()
	{
	}

	public T GetBehavior<T>() where T : class
	{
		for (int i = 0; i < Behaviors.Count; i++)
		{
			if (Behaviors[i] is T)
			{
				return Behaviors[i] as T;
			}
		}
		return null;
	}

	public virtual void Initialize(ICoreAPI api)
	{
		Api = api;
		FrameProfilerUtil frameProfiler = api.World.FrameProfiler;
		if (frameProfiler != null && frameProfiler.Enabled)
		{
			foreach (BlockEntityBehavior behavior in Behaviors)
			{
				behavior.Initialize(api, behavior.properties);
				api.World.FrameProfiler.Mark("initbebehavior-", behavior.GetType());
			}
		}
		else
		{
			foreach (BlockEntityBehavior behavior2 in Behaviors)
			{
				behavior2.Initialize(api, behavior2.properties);
			}
		}
		if (stackForWorldgen != null)
		{
			try
			{
				OnBlockPlaced(stackForWorldgen);
			}
			catch (Exception e)
			{
				api.Logger.Error(e);
			}
			stackForWorldgen = null;
		}
	}

	public virtual void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		Block = block;
		BlockEntityBehaviorType[] blockEntityBehaviors = block.BlockEntityBehaviors;
		foreach (BlockEntityBehaviorType blockEntityBehaviorType in blockEntityBehaviors)
		{
			if (worldForResolve.ClassRegistry.GetBlockEntityBehaviorClass(blockEntityBehaviorType.Name) == null)
			{
				worldForResolve.Logger.Warning(Lang.Get("Block entity behavior {0} for block {1} not found", blockEntityBehaviorType.Name, block.Code));
				continue;
			}
			if (blockEntityBehaviorType.properties == null)
			{
				blockEntityBehaviorType.properties = new JsonObject((JToken)new JObject());
			}
			BlockEntityBehavior blockEntityBehavior = worldForResolve.ClassRegistry.CreateBlockEntityBehavior(this, blockEntityBehaviorType.Name);
			blockEntityBehavior.properties = blockEntityBehaviorType.properties;
			Behaviors.Add(blockEntityBehavior);
		}
	}

	public virtual long RegisterGameTickListener(Action<float> onGameTick, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		if (Dimensions.ShouldNotTick(Pos, Api))
		{
			return 0L;
		}
		long num = Api.Event.RegisterGameTickListener(onGameTick, TickingExceptionHandler, millisecondInterval, initialDelayOffsetMs);
		if (TickHandlers == null)
		{
			TickHandlers = new List<long>(1);
		}
		TickHandlers.Add(num);
		return num;
	}

	public virtual void UnregisterGameTickListener(long listenerId)
	{
		Api.Event.UnregisterGameTickListener(listenerId);
		TickHandlers?.Remove(listenerId);
	}

	public virtual void UnregisterAllTickListeners()
	{
		if (TickHandlers == null)
		{
			return;
		}
		foreach (long tickHandler in TickHandlers)
		{
			Api.Event.UnregisterGameTickListener(tickHandler);
		}
	}

	public virtual long RegisterDelayedCallback(Action<float> OnDelayedCallbackTick, int millisecondInterval)
	{
		long num = Api.Event.RegisterCallback(OnDelayedCallbackTick, millisecondInterval);
		if (CallbackHandlers == null)
		{
			CallbackHandlers = new List<long>();
		}
		CallbackHandlers.Add(num);
		return num;
	}

	public virtual void UnregisterDelayedCallback(long listenerId)
	{
		Api.Event.UnregisterCallback(listenerId);
		CallbackHandlers?.Remove(listenerId);
	}

	public virtual void TickingExceptionHandler(Exception e)
	{
		if (Api == null)
		{
			throw new Exception("Api was null while ticking a BlockEntity: " + GetType().FullName);
		}
		Api.Logger.Error("At position " + Pos?.ToString() + " for block " + (Block?.Code.ToShortString() ?? "(missing)") + " a " + GetType().Name + " threw an error when ticked:");
		Api.Logger.Error(e);
	}

	public virtual void OnBlockRemoved()
	{
		UnregisterAllTickListeners();
		if (CallbackHandlers != null)
		{
			foreach (long callbackHandler in CallbackHandlers)
			{
				Api.Event.UnregisterCallback(callbackHandler);
			}
		}
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			behavior.OnBlockRemoved();
		}
	}

	public virtual void OnExchanged(Block block)
	{
		if (block != Block)
		{
			MarkDirty(redrawOnClient: true);
		}
		Block = block;
	}

	public virtual void OnBlockBroken(IPlayer byPlayer = null)
	{
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			behavior.OnBlockBroken(byPlayer);
		}
	}

	public virtual void HistoryStateRestore()
	{
	}

	public virtual void OnBlockUnloaded()
	{
		try
		{
			if (Api != null)
			{
				UnregisterAllTickListeners();
				if (CallbackHandlers != null)
				{
					foreach (long callbackHandler in CallbackHandlers)
					{
						Api.Event.UnregisterCallback(callbackHandler);
					}
				}
			}
			foreach (BlockEntityBehavior behavior in Behaviors)
			{
				behavior.OnBlockUnloaded();
			}
		}
		catch (Exception)
		{
			Api.Logger.Error("At position " + Pos?.ToString() + " for block " + (Block?.Code.ToShortString() ?? "(missing)") + " a " + GetType().Name + " threw an error when unloaded");
			throw;
		}
	}

	public virtual void OnBlockPlaced(ItemStack byItemStack = null)
	{
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			behavior.OnBlockPlaced(byItemStack);
		}
	}

	public virtual void ToTreeAttributes(ITreeAttribute tree)
	{
		ICoreAPI api = Api;
		if ((api == null || api.Side != EnumAppSide.Client) && Block.IsMissing)
		{
			foreach (KeyValuePair<string, IAttribute> item in missingBlockTree)
			{
				tree[item.Key] = item.Value;
			}
			return;
		}
		tree.SetInt("posx", Pos.X);
		tree.SetInt("posy", Pos.InternalY);
		tree.SetInt("posz", Pos.Z);
		if (Block != null)
		{
			tree.SetString("blockCode", Block.Code.ToShortString());
		}
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			behavior.ToTreeAttributes(tree);
		}
	}

	public virtual void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		Pos = new BlockPos(tree.GetInt("posx"), tree.GetInt("posy"), tree.GetInt("posz"));
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			behavior.FromTreeAttributes(tree, worldAccessForResolve);
		}
		if (worldAccessForResolve.Side == EnumAppSide.Server && Block.IsMissing)
		{
			missingBlockTree = tree;
		}
	}

	public virtual void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
	{
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			behavior.OnReceivedClientPacket(fromPlayer, packetid, data);
		}
	}

	public virtual void OnReceivedServerPacket(int packetid, byte[] data)
	{
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			behavior.OnReceivedServerPacket(packetid, data);
		}
	}

	public virtual void MarkDirty(bool redrawOnClient = false, IPlayer skipPlayer = null)
	{
		if (Api != null)
		{
			Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
			if (redrawOnClient)
			{
				Api.World.BlockAccessor.MarkBlockDirty(Pos, skipPlayer);
			}
		}
	}

	public virtual void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			behavior.GetBlockInfo(forPlayer, dsc);
		}
	}

	public virtual void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			behavior.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
		}
	}

	[Obsolete("Use the variant with resolveImports parameter")]
	public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
	{
		OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports: true);
	}

	public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			behavior.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);
		}
	}

	public virtual bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		bool flag = false;
		for (int i = 0; i < Behaviors.Count; i++)
		{
			flag |= Behaviors[i].OnTesselation(mesher, tessThreadTesselator);
		}
		return flag;
	}

	public virtual void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
	{
		Pos = pos.Copy();
		for (int i = 0; i < Behaviors.Count; i++)
		{
			Behaviors[i].OnPlacementBySchematic(api, blockAccessor, pos, replaceBlocks, centerrockblockid, layerBlock, resolveImports);
		}
	}
}
