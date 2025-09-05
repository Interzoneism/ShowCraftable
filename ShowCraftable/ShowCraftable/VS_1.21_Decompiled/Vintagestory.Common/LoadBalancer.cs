using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;

namespace Vintagestory.Common;

internal class LoadBalancer
{
	private LoadBalancedTask caller;

	private Logger logger;

	private volatile int threadCompletionCounter;

	internal LoadBalancer(LoadBalancedTask caller, Logger logger)
	{
		this.caller = caller;
		this.logger = logger;
	}

	internal void CreateDedicatedThreads(int threadCount, string name, List<Thread> threadlist)
	{
		for (int i = 2; i <= threadCount; i++)
		{
			Thread item = CreateDedicatedWorkerThread(i, name, threadlist);
			threadlist?.Add(item);
		}
	}

	private Thread CreateDedicatedWorkerThread(int threadnum, string name, List<Thread> threadlist = null)
	{
		Thread thread = TyronThreadPool.CreateDedicatedThread(delegate
		{
			caller.StartWorkerThread(threadnum);
		}, name + threadnum);
		thread.IsBackground = false;
		thread.Priority = Thread.CurrentThread.Priority;
		return thread;
	}

	internal void SynchroniseWorkToMainThread(object source)
	{
		threadCompletionCounter = 1;
		try
		{
			if (!caller.ShouldExit())
			{
				lock (source)
				{
					Monitor.PulseAll(source);
				}
				caller.DoWork(1);
			}
		}
		catch (Exception e)
		{
			caller.HandleException(e);
		}
	}

	internal void SynchroniseWorkOnWorkerThread(object source, int workernum)
	{
		bool flag = false;
		try
		{
			lock (source)
			{
				flag = Monitor.Wait(source, 1600);
			}
		}
		catch (ThreadInterruptedException)
		{
			return;
		}
		try
		{
			if (flag)
			{
				caller.DoWork(workernum);
				Interlocked.Increment(ref threadCompletionCounter);
			}
		}
		catch (ThreadAbortException)
		{
			throw;
		}
		catch (Exception e)
		{
			caller.HandleException(e);
		}
	}

	internal void WorkerThreadLoop(object source, int workernum, int msToSleep = 1)
	{
		try
		{
			while (!caller.ShouldExit())
			{
				SynchroniseWorkOnWorkerThread(source, workernum);
			}
		}
		catch (ThreadAbortException)
		{
		}
		catch (Exception ex2)
		{
			logger.Fatal("Error thrown in worker thread management (this worker thread will now stop as a precaution)\n{0}", ex2.Message);
			logger.Fatal(ex2);
		}
	}

	internal void AwaitCompletionOnAllThreads(int threadsCount)
	{
		long num = Environment.TickCount + 1000;
		SpinWait spinWait = default(SpinWait);
		while (Interlocked.CompareExchange(ref threadCompletionCounter, 0, threadsCount) != threadsCount)
		{
			spinWait.SpinOnce();
			if (Environment.TickCount > num)
			{
				break;
			}
		}
		threadCompletionCounter = 0;
	}
}
