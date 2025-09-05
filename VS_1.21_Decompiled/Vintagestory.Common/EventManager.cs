using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public abstract class EventManager
{
	private long listenerId;

	private long callBackId;

	internal List<GameTickListener> GameTickListenersEntity = new List<GameTickListener>();

	internal ConcurrentDictionary<long, DelayedCallback> DelayedCallbacksEntity = new ConcurrentDictionary<long, DelayedCallback>();

	internal List<GameTickListenerBlock> GameTickListenersBlock = new List<GameTickListenerBlock>();

	internal List<DelayedCallbackBlock> DelayedCallbacksBlock = new List<DelayedCallbackBlock>();

	internal ConcurrentDictionary<long, int> GameTickListenersEntityIndices = new ConcurrentDictionary<long, int>();

	internal ConcurrentDictionary<long, int> GameTickListenersBlockIndices = new ConcurrentDictionary<long, int>();

	internal Dictionary<BlockPos, DelayedCallbackBlock> SingleDelayedCallbacksBlock = new Dictionary<BlockPos, DelayedCallbackBlock>();

	private List<DelayedCallback> deletable = new List<DelayedCallback>();

	protected Thread serverThread;

	public abstract ILogger Logger { get; }

	public abstract string CommandPrefix { get; }

	public abstract long InWorldEllapsedMs { get; }

	public event OnGetClimateDelegate OnGetClimate;

	public event OnGetWindSpeedDelegate OnGetWindSpeed;

	public abstract bool HasPrivilege(string playerUid, string privilegecode);

	public virtual void TriggerOnGetClimate(ref ClimateCondition climate, BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.WorldGenValues, double totalDays = 0.0)
	{
		if (this.OnGetClimate != null)
		{
			Delegate[] invocationList = this.OnGetClimate.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((OnGetClimateDelegate)invocationList[i])(ref climate, pos, mode, totalDays);
			}
		}
	}

	public virtual void TriggerOnGetWindSpeed(Vec3d pos, ref Vec3d windSpeed)
	{
		if (this.OnGetWindSpeed != null)
		{
			Delegate[] invocationList = this.OnGetWindSpeed.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				((OnGetWindSpeedDelegate)invocationList[i])(pos, ref windSpeed);
			}
		}
	}

	public virtual void TriggerGameTick(long ellapsedMilliseconds, IWorldAccessor world)
	{
		FrameProfilerUtil frameProfiler = world.FrameProfiler;
		List<GameTickListener> gameTickListenersEntity = GameTickListenersEntity;
		if (frameProfiler.Enabled)
		{
			world.FrameProfiler.Enter("tick-entitylisteners (mainly BlockEntities)");
			for (int i = 0; i < gameTickListenersEntity.Count; i++)
			{
				GameTickListener gameTickListener = gameTickListenersEntity[i];
				if (gameTickListener != null && ellapsedMilliseconds - gameTickListener.LastUpdateMilliseconds > gameTickListener.Millisecondinterval)
				{
					gameTickListener.OnTriggered(ellapsedMilliseconds);
					frameProfiler.Mark("gmle", gameTickListener.Origin().GetType());
				}
			}
			world.FrameProfiler.Leave();
		}
		else
		{
			for (int j = 0; j < gameTickListenersEntity.Count; j++)
			{
				GameTickListener gameTickListener2 = gameTickListenersEntity[j];
				if (gameTickListener2 != null && ellapsedMilliseconds - gameTickListener2.LastUpdateMilliseconds > gameTickListener2.Millisecondinterval)
				{
					gameTickListener2.OnTriggered(ellapsedMilliseconds);
				}
			}
		}
		frameProfiler.Mark("tick-gtentity");
		List<GameTickListenerBlock> gameTickListenersBlock = GameTickListenersBlock;
		for (int k = 0; k < gameTickListenersBlock.Count; k++)
		{
			GameTickListenerBlock gameTickListenerBlock = gameTickListenersBlock[k];
			if (gameTickListenerBlock != null && ellapsedMilliseconds - gameTickListenerBlock.LastUpdateMilliseconds > gameTickListenerBlock.Millisecondinterval)
			{
				gameTickListenerBlock.Handler(world, gameTickListenerBlock.Pos, (float)(ellapsedMilliseconds - gameTickListenerBlock.LastUpdateMilliseconds) / 1000f);
				gameTickListenerBlock.LastUpdateMilliseconds = ellapsedMilliseconds;
			}
		}
		frameProfiler.Mark("tick-gtblock");
		deletable.Clear();
		foreach (KeyValuePair<long, DelayedCallback> item in DelayedCallbacksEntity)
		{
			if (ellapsedMilliseconds - item.Value.CallAtEllapsedMilliseconds >= 0)
			{
				DelayedCallback value = item.Value;
				value.Handler((float)(ellapsedMilliseconds - value.CallAtEllapsedMilliseconds) / 1000f);
				deletable.Add(value);
			}
		}
		frameProfiler.Mark("tick-dcentity");
		foreach (DelayedCallback item2 in deletable)
		{
			DelayedCallbacksEntity.TryRemove(item2.ListenerId, out var _);
		}
		List<DelayedCallbackBlock> delayedCallbacksBlock = DelayedCallbacksBlock;
		for (int l = 0; l < delayedCallbacksBlock.Count; l++)
		{
			DelayedCallbackBlock delayedCallbackBlock = delayedCallbacksBlock[l];
			if (ellapsedMilliseconds - delayedCallbackBlock.CallAtEllapsedMilliseconds >= 0)
			{
				delayedCallbacksBlock.RemoveAt(l);
				l--;
				delayedCallbackBlock.Handler(world, delayedCallbackBlock.Pos, (float)(ellapsedMilliseconds - delayedCallbackBlock.CallAtEllapsedMilliseconds) / 1000f);
			}
		}
		Dictionary<BlockPos, DelayedCallbackBlock> singleDelayedCallbacksBlock = SingleDelayedCallbacksBlock;
		if (singleDelayedCallbacksBlock.Count > 0)
		{
			foreach (BlockPos item3 in new List<BlockPos>(singleDelayedCallbacksBlock.Keys))
			{
				DelayedCallbackBlock delayedCallbackBlock2 = singleDelayedCallbacksBlock[item3];
				if (ellapsedMilliseconds - delayedCallbackBlock2.CallAtEllapsedMilliseconds >= 0)
				{
					singleDelayedCallbacksBlock.Remove(item3);
					delayedCallbackBlock2.Handler(world, delayedCallbackBlock2.Pos, (float)(ellapsedMilliseconds - delayedCallbackBlock2.CallAtEllapsedMilliseconds) / 1000f);
				}
			}
		}
		frameProfiler.Mark("tick-dcblock");
	}

	public virtual void TriggerGameTickDebug(long ellapsedMilliseconds, IWorldAccessor world)
	{
		List<GameTickListener> gameTickListenersEntity = GameTickListenersEntity;
		for (int i = 0; i < gameTickListenersEntity.Count; i++)
		{
			GameTickListener gameTickListener = gameTickListenersEntity[i];
			if (gameTickListener != null && ellapsedMilliseconds - gameTickListener.LastUpdateMilliseconds > gameTickListener.Millisecondinterval)
			{
				gameTickListener.OnTriggered(ellapsedMilliseconds);
				world.FrameProfiler.Mark("gmle", gameTickListener.Origin().GetType());
			}
		}
		List<GameTickListenerBlock> gameTickListenersBlock = GameTickListenersBlock;
		for (int j = 0; j < gameTickListenersBlock.Count; j++)
		{
			GameTickListenerBlock gameTickListenerBlock = gameTickListenersBlock[j];
			if (gameTickListenerBlock != null && ellapsedMilliseconds - gameTickListenerBlock.LastUpdateMilliseconds > gameTickListenerBlock.Millisecondinterval)
			{
				gameTickListenerBlock.Handler(world, gameTickListenerBlock.Pos, (float)(ellapsedMilliseconds - gameTickListenerBlock.LastUpdateMilliseconds) / 1000f);
				gameTickListenerBlock.LastUpdateMilliseconds = ellapsedMilliseconds;
				world.FrameProfiler.Mark("gmlb", gameTickListenerBlock.Handler.Target.GetType());
			}
		}
		deletable.Clear();
		foreach (KeyValuePair<long, DelayedCallback> item in DelayedCallbacksEntity)
		{
			if (ellapsedMilliseconds - item.Value.CallAtEllapsedMilliseconds >= 0)
			{
				DelayedCallback value = item.Value;
				value.Handler((float)(ellapsedMilliseconds - value.CallAtEllapsedMilliseconds) / 1000f);
				deletable.Add(value);
				world.FrameProfiler.Mark("dce", value.Handler.Target.GetType());
			}
		}
		foreach (DelayedCallback item2 in deletable)
		{
			DelayedCallbacksEntity.TryRemove(item2.ListenerId, out var _);
		}
		List<DelayedCallbackBlock> delayedCallbacksBlock = DelayedCallbacksBlock;
		for (int k = 0; k < delayedCallbacksBlock.Count; k++)
		{
			DelayedCallbackBlock delayedCallbackBlock = delayedCallbacksBlock[k];
			if (ellapsedMilliseconds - delayedCallbackBlock.CallAtEllapsedMilliseconds >= 0)
			{
				delayedCallbacksBlock.RemoveAt(k);
				k--;
				delayedCallbackBlock.Handler(world, delayedCallbackBlock.Pos, (float)(ellapsedMilliseconds - delayedCallbackBlock.CallAtEllapsedMilliseconds) / 1000f);
				world.FrameProfiler.Mark("dcb", delayedCallbackBlock.Handler.Target.GetType());
			}
		}
		Dictionary<BlockPos, DelayedCallbackBlock> singleDelayedCallbacksBlock = SingleDelayedCallbacksBlock;
		if (singleDelayedCallbacksBlock.Count <= 0)
		{
			return;
		}
		foreach (BlockPos item3 in new List<BlockPos>(singleDelayedCallbacksBlock.Keys))
		{
			DelayedCallbackBlock delayedCallbackBlock2 = singleDelayedCallbacksBlock[item3];
			if (ellapsedMilliseconds - delayedCallbackBlock2.CallAtEllapsedMilliseconds >= 0)
			{
				singleDelayedCallbacksBlock.Remove(item3);
				delayedCallbackBlock2.Handler(world, delayedCallbackBlock2.Pos, (float)(ellapsedMilliseconds - delayedCallbackBlock2.CallAtEllapsedMilliseconds) / 1000f);
				world.FrameProfiler.Mark("sdcb", delayedCallbackBlock2.Handler.Target.GetType());
			}
		}
	}

	public virtual long AddGameTickListener(Action<float> handler, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		return AddGameTickListener(handler, null, millisecondInterval, initialDelayOffsetMs);
	}

	public virtual long AddGameTickListener(Action<float> handler, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		long num = ++listenerId;
		GameTickListener gameTickListener = new GameTickListener
		{
			Handler = handler,
			ErrorHandler = errorHandler,
			Millisecondinterval = millisecondInterval,
			ListenerId = num,
			LastUpdateMilliseconds = InWorldEllapsedMs + initialDelayOffsetMs
		};
		List<GameTickListener> gameTickListenersEntity = GameTickListenersEntity;
		for (int i = 0; i < gameTickListenersEntity.Count; i++)
		{
			if (gameTickListenersEntity[i] == null)
			{
				gameTickListenersEntity[i] = gameTickListener;
				GameTickListenersEntityIndices[num] = i;
				if (gameTickListenersEntity[GameTickListenersEntityIndices[num]] != gameTickListener)
				{
					throw new InvalidOperationException("Failed to add listener properly");
				}
				return num;
			}
		}
		gameTickListenersEntity.Add(gameTickListener);
		GameTickListenersEntityIndices[num] = gameTickListenersEntity.Count - 1;
		if (gameTickListenersEntity[GameTickListenersEntityIndices[num]] != gameTickListener)
		{
			throw new InvalidOperationException("Failed to add listener properly");
		}
		return num;
	}

	public long AddGameTickListener(Action<IWorldAccessor, BlockPos, float> handler, BlockPos pos, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		long num = ++listenerId;
		GameTickListenerBlock gameTickListenerBlock = new GameTickListenerBlock
		{
			Handler = handler,
			Millisecondinterval = millisecondInterval,
			ListenerId = num,
			LastUpdateMilliseconds = InWorldEllapsedMs + initialDelayOffsetMs,
			Pos = pos.Copy()
		};
		List<GameTickListenerBlock> gameTickListenersBlock = GameTickListenersBlock;
		for (int i = 0; i < gameTickListenersBlock.Count; i++)
		{
			if (gameTickListenersBlock[i] == null)
			{
				gameTickListenersBlock[i] = gameTickListenerBlock;
				GameTickListenersBlockIndices[num] = i;
				return num;
			}
		}
		gameTickListenersBlock.Add(gameTickListenerBlock);
		GameTickListenersBlockIndices[num] = gameTickListenersBlock.Count - 1;
		return num;
	}

	public virtual long AddDelayedCallback(Action<float> handler, long callAfterEllapsedMS)
	{
		long num = Interlocked.Increment(ref callBackId);
		DelayedCallback value = new DelayedCallback
		{
			CallAtEllapsedMilliseconds = InWorldEllapsedMs + callAfterEllapsedMS,
			Handler = handler,
			ListenerId = num
		};
		DelayedCallbacksEntity[num] = value;
		return num;
	}

	public virtual long AddDelayedCallback(Action<IWorldAccessor, BlockPos, float> handler, BlockPos pos, long callAfterEllapsedMS)
	{
		long result = Interlocked.Increment(ref callBackId);
		DelayedCallbacksBlock.Add(new DelayedCallbackBlock
		{
			CallAtEllapsedMilliseconds = InWorldEllapsedMs + callAfterEllapsedMS,
			Handler = handler,
			ListenerId = result,
			Pos = pos.Copy()
		});
		return result;
	}

	internal virtual long AddSingleDelayedCallback(Action<IWorldAccessor, BlockPos, float> handler, BlockPos pos, long callAfterEllapsedMs)
	{
		BlockPos blockPos = pos.Copy();
		long result = Interlocked.Increment(ref callBackId);
		SingleDelayedCallbacksBlock[blockPos] = new DelayedCallbackBlock
		{
			CallAtEllapsedMilliseconds = InWorldEllapsedMs + callAfterEllapsedMs,
			Handler = handler,
			ListenerId = result,
			Pos = blockPos
		};
		return result;
	}

	public void RemoveDelayedCallback(long callbackId)
	{
		if (callbackId == 0L || DelayedCallbacksEntity.TryRemove(callbackId, out var _))
		{
			return;
		}
		foreach (DelayedCallbackBlock item in DelayedCallbacksBlock)
		{
			if (item.ListenerId == callbackId)
			{
				DelayedCallbacksBlock.Remove(item);
				return;
			}
		}
		foreach (KeyValuePair<BlockPos, DelayedCallbackBlock> item2 in SingleDelayedCallbacksBlock)
		{
			if (item2.Value.ListenerId == callbackId)
			{
				SingleDelayedCallbacksBlock.Remove(item2.Key);
				break;
			}
		}
	}

	public void RemoveGameTickListener(long listenerId)
	{
		if (listenerId == 0L)
		{
			return;
		}
		int value2;
		if (GameTickListenersEntityIndices.TryRemove(listenerId, out var value))
		{
			GameTickListener gameTickListener = GameTickListenersEntity[value];
			if (gameTickListener != null && gameTickListener.ListenerId == listenerId)
			{
				GameTickListenersEntity[value] = null;
				return;
			}
		}
		else if (GameTickListenersBlockIndices.TryRemove(listenerId, out value2))
		{
			GameTickListenerBlock gameTickListenerBlock = GameTickListenersBlock[value2];
			if (gameTickListenerBlock != null && gameTickListenerBlock.ListenerId == listenerId)
			{
				GameTickListenersBlock[value2] = null;
				return;
			}
		}
		List<GameTickListener> gameTickListenersEntity = GameTickListenersEntity;
		for (int i = 0; i < gameTickListenersEntity.Count; i++)
		{
			GameTickListener gameTickListener2 = gameTickListenersEntity[i];
			if (gameTickListener2 != null && gameTickListener2.ListenerId == listenerId)
			{
				gameTickListenersEntity[i] = null;
				return;
			}
		}
		List<GameTickListenerBlock> gameTickListenersBlock = GameTickListenersBlock;
		for (int j = 0; j < gameTickListenersBlock.Count; j++)
		{
			GameTickListenerBlock gameTickListenerBlock2 = gameTickListenersBlock[j];
			if (gameTickListenerBlock2 != null && gameTickListenerBlock2.ListenerId == listenerId)
			{
				gameTickListenersBlock[j] = null;
				break;
			}
		}
	}
}
