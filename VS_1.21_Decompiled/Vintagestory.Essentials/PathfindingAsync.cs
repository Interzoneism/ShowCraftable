using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.Essentials;

public class PathfindingAsync : ModSystem, IAsyncServerSystem
{
	protected ICoreServerAPI api;

	protected AStar astar_offthread;

	protected AStar astar_mainthread;

	protected bool isShuttingDown;

	public ConcurrentQueue<PathfinderTask> PathfinderTasks = new ConcurrentQueue<PathfinderTask>();

	protected readonly Stopwatch totalTime = new Stopwatch();

	protected long lastTickTimeMs;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		this.api = api;
		astar_offthread = new AStar(api);
		astar_mainthread = new AStar(api);
		api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, delegate
		{
			isShuttingDown = true;
		});
		api.Event.RegisterGameTickListener(OnMainThreadTick, 20);
		api.Server.AddServerThread("ai-pathfinding", this);
	}

	public int OffThreadInterval()
	{
		return 5;
	}

	protected void OnMainThreadTick(float dt)
	{
		int count = PathfinderTasks.Count;
		if (count <= 1)
		{
			return;
		}
		api.World.FrameProfiler.Enter("ai-pathfinding-overflow " + count);
		int num = 1000;
		PathfinderTask pathfinderTask;
		while ((pathfinderTask = Next()) != null && num-- > 0)
		{
			pathfinderTask.waypoints = astar_mainthread.FindPathAsWaypoints(pathfinderTask.startBlockPos, pathfinderTask.targetBlockPos, pathfinderTask.maxFallHeight, pathfinderTask.stepHeight, pathfinderTask.collisionBox, pathfinderTask.searchDepth, pathfinderTask.mhdistanceTolerance);
			pathfinderTask.Finished = true;
			if (isShuttingDown)
			{
				break;
			}
			if (api.World.FrameProfiler.Enabled)
			{
				api.World.FrameProfiler.Mark("path d:" + pathfinderTask.searchDepth + " r:" + ((pathfinderTask.waypoints == null) ? "fail" : pathfinderTask.waypoints.Count.ToString()) + " s:" + pathfinderTask.startBlockPos?.ToString() + " e:" + pathfinderTask.targetBlockPos?.ToString() + " w:" + pathfinderTask.collisionBox.Width);
			}
		}
		api.World.FrameProfiler.Leave();
	}

	public void OnSeparateThreadTick()
	{
		ProcessQueue(astar_offthread, 100);
	}

	public void ProcessQueue(AStar astar, int maxCount)
	{
		PathfinderTask pathfinderTask;
		while ((pathfinderTask = Next()) != null && maxCount-- > 0)
		{
			try
			{
				pathfinderTask.waypoints = astar.FindPathAsWaypoints(pathfinderTask.startBlockPos, pathfinderTask.targetBlockPos, pathfinderTask.maxFallHeight, pathfinderTask.stepHeight, pathfinderTask.collisionBox, pathfinderTask.searchDepth, pathfinderTask.mhdistanceTolerance, pathfinderTask.CreatureType);
			}
			catch (Exception ex)
			{
				pathfinderTask.waypoints = null;
				api.World.Logger.Error("Exception thrown during pathfinding. Will ignore. Exception: {0}", ex.ToString());
			}
			pathfinderTask.Finished = true;
			if (isShuttingDown)
			{
				break;
			}
		}
	}

	protected PathfinderTask Next()
	{
		if (!PathfinderTasks.TryDequeue(out var result))
		{
			return null;
		}
		return result;
	}

	public void EnqueuePathfinderTask(PathfinderTask task)
	{
		PathfinderTasks.Enqueue(task);
	}

	public override void Dispose()
	{
		astar_mainthread?.Dispose();
		astar_mainthread = null;
	}

	public void ThreadDispose()
	{
		astar_offthread.Dispose();
		astar_offthread = null;
	}
}
