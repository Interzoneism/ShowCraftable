using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Vintagestory.API.Config;

namespace Vintagestory.API.Common;

public class TyronThreadPool
{
	public static TyronThreadPool Inst = new TyronThreadPool();

	public ILogger Logger;

	public ConcurrentDictionary<int, string> RunningTasks = new ConcurrentDictionary<int, string>();

	public ConcurrentDictionary<int, Thread> DedicatedThreads = new ConcurrentDictionary<int, Thread>();

	private int keyCounter;

	private int dedicatedCounter;

	public TyronThreadPool()
	{
		ThreadPool.SetMaxThreads(10, 1);
	}

	private int MarkStarted(string caller)
	{
		int num = keyCounter++;
		RunningTasks[num] = caller;
		return num;
	}

	private void MarkEnded(int key)
	{
		RunningTasks.TryRemove(key, out var _);
	}

	public string ListAllRunningTasks()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string value in RunningTasks.Values)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append(value);
		}
		if (stringBuilder.Length == 0)
		{
			stringBuilder.Append("[empty]");
		}
		stringBuilder.AppendLine();
		return "Current threadpool tasks: " + stringBuilder.ToString() + "\nThread pool thread count: " + ThreadPool.ThreadCount;
	}

	public string ListAllThreads()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Server threads (" + DedicatedThreads.Count + "):");
		List<Thread> list = new List<Thread>(DedicatedThreads.Count);
		foreach (KeyValuePair<int, Thread> dedicatedThread in DedicatedThreads)
		{
			int key = dedicatedThread.Key;
			while (key >= list.Count)
			{
				list.Add(null);
			}
			list[key] = dedicatedThread.Value;
		}
		foreach (Thread item in list)
		{
			if (item != null && item.ThreadState != System.Threading.ThreadState.Stopped)
			{
				stringBuilder.Append("tid" + item.ManagedThreadId + " ");
				stringBuilder.Append(item.Name);
				stringBuilder.Append(": ");
				stringBuilder.AppendLine(item.ThreadState.ToString());
			}
		}
		ProcessThreadCollection threads = Process.GetCurrentProcess().Threads;
		List<ProcessThread> list2 = new List<ProcessThread>();
		foreach (ProcessThread item2 in threads)
		{
			if (item2 != null)
			{
				list2.Add(item2);
			}
		}
		list2 = list2.OrderByDescending((ProcessThread t) => t.UserProcessorTime.Ticks).ToList();
		stringBuilder.AppendLine("\nAll process threads (" + list2.Count + "):");
		foreach (ProcessThread item3 in list2)
		{
			if (item3 != null)
			{
				stringBuilder.Append(item3.ThreadState.ToString() + " ");
				stringBuilder.Append("tid" + item3.Id + " ");
				if (RuntimeEnv.OS != OS.Mac)
				{
					stringBuilder.Append(item3.StartTime);
				}
				stringBuilder.Append(": P ");
				stringBuilder.Append(item3.CurrentPriority);
				stringBuilder.Append(": ");
				stringBuilder.Append(item3.ThreadState.ToString());
				stringBuilder.Append(": T ");
				stringBuilder.Append(item3.UserProcessorTime.ToString());
				stringBuilder.Append(": T Total ");
				stringBuilder.AppendLine(item3.TotalProcessorTime.ToString());
			}
		}
		return stringBuilder.ToString();
	}

	public static void QueueTask(Action callback, string caller)
	{
		int key = Inst.MarkStarted(caller);
		QueueTask(callback);
		Inst.MarkEnded(key);
	}

	public static void QueueLongDurationTask(Action callback, string caller)
	{
		int key = Inst.MarkStarted(caller);
		QueueLongDurationTask(callback);
		Inst.MarkEnded(key);
	}

	public static void QueueTask(Action callback)
	{
		if (RuntimeEnv.DebugThreadPool)
		{
			Inst.Logger.VerboseDebug("QueueTask." + Environment.StackTrace);
		}
		ThreadPool.QueueUserWorkItem(delegate
		{
			callback();
		});
	}

	public static void QueueLongDurationTask(Action callback)
	{
		if (RuntimeEnv.DebugThreadPool)
		{
			Inst.Logger.VerboseDebug("QueueTask." + Environment.StackTrace);
		}
		ThreadPool.QueueUserWorkItem(delegate
		{
			callback();
		});
	}

	public static Thread CreateDedicatedThread(ThreadStart starter, string name)
	{
		Thread thread = new Thread(starter);
		thread.IsBackground = true;
		thread.Name = name;
		Inst.DedicatedThreads[Inst.dedicatedCounter++] = thread;
		return thread;
	}

	public void Dispose()
	{
		RunningTasks.Clear();
		DedicatedThreads.Clear();
		keyCounter = 0;
		dedicatedCounter = 0;
	}
}
