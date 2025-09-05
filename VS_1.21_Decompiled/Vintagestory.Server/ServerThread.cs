using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace Vintagestory.Server;

public class ServerThread
{
	internal static bool shouldExit = false;

	public volatile bool ShouldPause;

	internal string threadName;

	internal bool paused;

	private bool alive;

	public ServerSystem[] serversystems;

	internal ServerMain server;

	private Stopwatch totalPassedTime = new Stopwatch();

	public static int SleepMs = 1;

	private readonly CancellationToken _token;

	public bool Alive => alive;

	public ServerThread(ServerMain server, string threadName, CancellationToken cancellationToken)
	{
		this.server = server;
		this.threadName = threadName;
		alive = true;
		_token = cancellationToken;
	}

	public void Process()
	{
		ServerMain.FrameProfiler = new FrameProfilerUtil("[Thread " + threadName + "] ");
		totalPassedTime.Start();
		try
		{
			while (!_token.IsCancellationRequested)
			{
				bool flag = server.Suspended || ShouldPause;
				bool flag2 = false;
				if (!flag)
				{
					ServerMain.FrameProfiler.Begin(null);
					flag2 = Update();
					ServerMain.FrameProfiler.Mark("update");
				}
				UpdatePausedStatus(flag);
				if (!flag2)
				{
					Thread.Sleep(SleepMs);
					ServerMain.FrameProfiler.Mark("sleep");
				}
				if (shouldExit)
				{
					ShutDown();
					break;
				}
				if (!flag)
				{
					ServerMain.FrameProfiler.OffThreadEnd();
				}
			}
		}
		catch (TaskCanceledException)
		{
		}
		catch (Exception e)
		{
			ServerMain.Logger.Fatal("Caught unhandled exception in thread '{0}'. Shutting down server.", threadName);
			ServerMain.Logger.Fatal(e);
			server.EnqueueMainThreadTask(delegate
			{
				server.Stop("Exception during Process");
			});
		}
		alive = false;
	}

	protected virtual void UpdatePausedStatus(bool newpaused)
	{
		paused = newpaused;
	}

	public void ShutDown()
	{
		for (int i = 0; i < serversystems.Length; i++)
		{
			serversystems[i].OnSeperateThreadShutDown();
		}
		alive = false;
	}

	public bool Update()
	{
		long elapsedMilliseconds = totalPassedTime.ElapsedMilliseconds;
		bool flag = false;
		for (int i = 0; i < serversystems.Length; i++)
		{
			ServerSystem serverSystem = serversystems[i];
			int updateInterval = serverSystem.GetUpdateInterval();
			flag = flag || updateInterval < 0;
			if (elapsedMilliseconds - serverSystem.millisecondsSinceStartSeperateThread > updateInterval)
			{
				serverSystem.millisecondsSinceStartSeperateThread = elapsedMilliseconds;
				serverSystem.OnSeparateThreadTick();
				ServerMain.FrameProfiler.Mark(serverSystem.FrameprofilerName);
			}
		}
		return flag;
	}

	public virtual void OnBeginInitialization()
	{
		for (int i = 0; i < serversystems.Length; i++)
		{
			serversystems[i].OnBeginInitialization();
		}
	}

	public virtual void OnBeginConfiguration()
	{
		for (int i = 0; i < serversystems.Length; i++)
		{
			serversystems[i].OnBeginConfiguration();
		}
	}

	public virtual void OnPrepareAssets()
	{
		for (int i = 0; i < serversystems.Length; i++)
		{
			serversystems[i].OnLoadAssets();
		}
	}

	public virtual void OnBeginLoadGamePre()
	{
		for (int i = 0; i < serversystems.Length; i++)
		{
			serversystems[i].OnBeginModsAndConfigReady();
		}
	}

	public virtual void OnBeginLoadGame(SaveGame savegame)
	{
		for (int i = 0; i < serversystems.Length; i++)
		{
			serversystems[i].OnBeginGameReady(savegame);
		}
	}

	public virtual void OnBeginRunGame()
	{
		for (int i = 0; i < serversystems.Length; i++)
		{
			serversystems[i].OnBeginRunGame();
		}
	}

	public virtual void OnBeginShutdown()
	{
		for (int i = 0; i < serversystems.Length; i++)
		{
			serversystems[i].OnBeginShutdown();
		}
	}
}
