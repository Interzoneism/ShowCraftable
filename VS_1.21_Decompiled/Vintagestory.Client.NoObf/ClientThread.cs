using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Vintagestory.Client.NoObf;

internal class ClientThread
{
	private string threadName;

	internal bool paused;

	private ClientSystem[] clientsystems;

	private Stopwatch lastFramePassedTime = new Stopwatch();

	private Stopwatch totalPassedTime = new Stopwatch();

	private ClientMain game;

	private readonly CancellationToken _token;

	public ClientThread(ClientMain game, string threadName, ClientSystem[] clientsystems, CancellationToken cancellationToken)
	{
		this.game = game;
		this.threadName = threadName;
		this.clientsystems = clientsystems;
		_token = cancellationToken;
	}

	public void Process()
	{
		totalPassedTime.Start();
		try
		{
			while (!_token.IsCancellationRequested)
			{
				if (!Update())
				{
					Thread.Sleep(5);
				}
				if (game.threadsShouldExit)
				{
					ShutDown();
					break;
				}
			}
		}
		catch (TaskCanceledException)
		{
		}
		catch (Exception ex2)
		{
			if (game.threadsShouldExit)
			{
				game.Logger.Notification("Client thread {0} threw an exception during exit. Likely unclean exit, which should not be a problem in most instance. Exception: '{1}'", threadName, ex2);
			}
			else
			{
				game.Logger.Fatal("Caught unhandled exception in thread '{0}'. Exiting game.", threadName);
				game.Logger.Fatal(ex2);
				game.Platform.XPlatInterface.ShowMessageBox("Client Thread Crash", "Whoops, a client game thread crashed, please check the client-main.log for more Information. I will now exit the game (and stop the server if in singleplayer). Sorry about that :(");
				game.KillNextFrame = true;
			}
		}
	}

	public void ShutDown()
	{
	}

	public bool Update()
	{
		float dt = (float)lastFramePassedTime.ElapsedTicks / (float)Stopwatch.Frequency;
		long elapsedMilliseconds = totalPassedTime.ElapsedMilliseconds;
		lastFramePassedTime.Restart();
		bool flag = false;
		for (int i = 0; i < clientsystems.Length; i++)
		{
			int num = clientsystems[i].SeperateThreadTickIntervalMs();
			flag = flag || num < 0;
			if (elapsedMilliseconds - clientsystems[i].threadMillisecondsSinceStart > num)
			{
				clientsystems[i].threadMillisecondsSinceStart = elapsedMilliseconds;
				clientsystems[i].OnSeperateThreadGameTick(dt);
			}
		}
		return flag;
	}
}
