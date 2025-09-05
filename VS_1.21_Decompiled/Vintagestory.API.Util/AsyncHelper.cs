using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Vintagestory.API.Common;

namespace Vintagestory.API.Util;

public class AsyncHelper
{
	public class Multithreaded
	{
		protected volatile int activeThreads;

		protected void ResetThreading()
		{
			activeThreads = 0;
		}

		protected bool WorkerThreadsInProgress()
		{
			return activeThreads != 0;
		}

		protected void StartWorkerThread(Action task)
		{
			TyronThreadPool.QueueTask(delegate
			{
				OnWorkerThread(task);
			}, "asynchelper");
		}

		protected void OnWorkerThread(Action task)
		{
			Interlocked.Increment(ref activeThreads);
			try
			{
				task();
			}
			finally
			{
				Interlocked.Decrement(ref activeThreads);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool CanProceedOnThisThread(ref int started)
	{
		return Interlocked.CompareExchange(ref started, 1, 0) == 0;
	}
}
