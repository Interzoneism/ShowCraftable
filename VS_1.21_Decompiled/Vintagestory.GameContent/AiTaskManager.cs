using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public sealed class AiTaskManager(Entity entity)
{
	public const int ActiveTasksSlotsNumber = 8;

	private readonly Entity entity = entity;

	private readonly List<IAiTask> tasks = new List<IAiTask>();

	private readonly IAiTask?[] activeTasksBySlot = new IAiTask[8];

	private bool wasRunAiTasks;

	public bool Shuffle { get; set; }

	public IAiTask?[] ActiveTasksBySlot => activeTasksBySlot;

	public List<IAiTask> AllTasks => tasks;

	public event Action<IAiTask>? OnTaskStarted;

	public event Action<IAiTask>? OnTaskStopped;

	public event ActionBoolReturn<IAiTask>? OnShouldExecuteTask;

	public void OnGameTick(float dt)
	{
		if (!AiRuntimeConfig.RunAiTasks)
		{
			if (wasRunAiTasks)
			{
				IAiTask[] array = activeTasksBySlot;
				for (int i = 0; i < array.Length; i++)
				{
					array[i]?.FinishExecute(cancelled: true);
				}
			}
			wasRunAiTasks = false;
		}
		else
		{
			wasRunAiTasks = true;
			if (Shuffle)
			{
				tasks.Shuffle(entity.World.Rand);
			}
			StartNewTasks();
			ProcessRunningTasks(dt);
			LogRunningTasks();
		}
	}

	public void AddTask(IAiTask task)
	{
		tasks.Add(task);
		task.ProfilerName = "task-startexecute-" + AiTaskRegistry.TaskCodes[task.GetType()];
	}

	public void RemoveTask(IAiTask task)
	{
		tasks.Remove(task);
	}

	public void AfterInitialize()
	{
		foreach (IAiTask task in tasks)
		{
			task.AfterInitialize();
		}
	}

	public void ExecuteTask(IAiTask task, int slot)
	{
		task.StartExecute();
		activeTasksBySlot[slot] = task;
		if (entity.World.FrameProfiler.Enabled)
		{
			entity.World.FrameProfiler.Mark("task-startexecute-", AiTaskRegistry.TaskCodes[task.GetType()]);
		}
	}

	public void ExecuteTask<TTask>() where TTask : IAiTask
	{
		foreach (TTask item in tasks.OfType<TTask>())
		{
			int slot = item.Slot;
			IAiTask aiTask = activeTasksBySlot[slot];
			if (aiTask != null)
			{
				aiTask.FinishExecute(cancelled: true);
				this.OnTaskStopped?.Invoke(aiTask);
			}
			activeTasksBySlot[slot] = item;
			item.StartExecute();
			this.OnTaskStarted?.Invoke(item);
			entity.World.FrameProfiler.Mark(item.ProfilerName);
		}
	}

	public TTask? GetTask<TTask>() where TTask : IAiTask
	{
		return (TTask)tasks.Find((IAiTask task) => task is TTask);
	}

	public IAiTask? GetTask(string id)
	{
		return tasks.Find((IAiTask task) => task.Id == id);
	}

	public IEnumerable<TTask> GetTasks<TTask>() where TTask : IAiTask
	{
		return tasks.OfType<TTask>();
	}

	public void StopTask(Type taskType)
	{
		IAiTask[] array = activeTasksBySlot;
		foreach (IAiTask aiTask in array)
		{
			if (aiTask?.GetType() == taskType)
			{
				aiTask.FinishExecute(cancelled: true);
				this.OnTaskStopped?.Invoke(aiTask);
				activeTasksBySlot[aiTask.Slot] = null;
			}
		}
		entity.World.FrameProfiler.Mark("finishexecute");
	}

	public void StopTask<TTask>() where TTask : IAiTask
	{
		foreach (TTask item in activeTasksBySlot.OfType<TTask>())
		{
			item.FinishExecute(cancelled: true);
			this.OnTaskStopped?.Invoke(item);
			activeTasksBySlot[item.Slot] = null;
		}
		entity.World.FrameProfiler.Mark("finishexecute");
	}

	public void StopTasks()
	{
		IAiTask[] array = activeTasksBySlot;
		foreach (IAiTask aiTask in array)
		{
			if (aiTask != null)
			{
				aiTask.FinishExecute(cancelled: true);
				this.OnTaskStopped?.Invoke(aiTask);
				activeTasksBySlot[aiTask.Slot] = null;
			}
		}
	}

	public bool IsTaskActive(string id)
	{
		IAiTask[] array = activeTasksBySlot;
		foreach (IAiTask aiTask in array)
		{
			if (aiTask != null && aiTask.Id == id)
			{
				return true;
			}
		}
		return false;
	}

	internal void Notify(string key, object data)
	{
		if (key == "starttask")
		{
			string taskId = (string)data;
			if (activeTasksBySlot.FirstOrDefault((IAiTask aiTask5) => aiTask5?.Id == taskId) != null)
			{
				return;
			}
			IAiTask task = GetTask(taskId);
			if (task != null)
			{
				IAiTask aiTask = activeTasksBySlot[task.Slot];
				if (aiTask != null)
				{
					aiTask.FinishExecute(cancelled: true);
					this.OnTaskStopped?.Invoke(aiTask);
				}
				activeTasksBySlot[task.Slot] = null;
				ExecuteTask(task, task.Slot);
			}
			return;
		}
		if (key == "stoptask")
		{
			string taskId2 = (string)data;
			IAiTask aiTask2 = activeTasksBySlot.FirstOrDefault((IAiTask aiTask5) => aiTask5?.Id == taskId2);
			if (aiTask2 != null)
			{
				aiTask2.FinishExecute(cancelled: true);
				this.OnTaskStopped?.Invoke(aiTask2);
				activeTasksBySlot[aiTask2.Slot] = null;
			}
			return;
		}
		for (int num = 0; num < tasks.Count; num++)
		{
			IAiTask aiTask3 = tasks[num];
			if (!aiTask3.Notify(key, data))
			{
				continue;
			}
			int slot = tasks[num].Slot;
			IAiTask aiTask4 = activeTasksBySlot[slot];
			if (aiTask4 == null || aiTask3.Priority > aiTask4.PriorityForCancel)
			{
				if (aiTask4 != null)
				{
					aiTask4.FinishExecute(cancelled: true);
					this.OnTaskStopped?.Invoke(aiTask4);
				}
				activeTasksBySlot[slot] = aiTask3;
				aiTask3.StartExecute();
				this.OnTaskStarted?.Invoke(aiTask3);
			}
		}
	}

	internal void OnStateChanged(EnumEntityState beforeState)
	{
		foreach (IAiTask task in tasks)
		{
			task.OnStateChanged(beforeState);
		}
	}

	internal void OnEntitySpawn()
	{
		foreach (IAiTask task in tasks)
		{
			task.OnEntitySpawn();
		}
	}

	internal void OnEntityLoaded()
	{
		foreach (IAiTask task in tasks)
		{
			task.OnEntityLoaded();
		}
	}

	internal void OnEntityDespawn(EntityDespawnData reason)
	{
		foreach (IAiTask task in tasks)
		{
			task.OnEntityDespawn(reason);
		}
	}

	internal void OnEntityHurt(DamageSource source, float damage)
	{
		foreach (IAiTask task in tasks)
		{
			task.OnEntityHurt(source, damage);
		}
	}

	private void StartNewTasks()
	{
		foreach (IAiTask task in tasks)
		{
			if (task.Priority < 0f)
			{
				continue;
			}
			int slot = task.Slot;
			IAiTask aiTask = activeTasksBySlot[slot];
			if ((aiTask == null || task.Priority > aiTask.PriorityForCancel) && task.ShouldExecute() && ShouldExecuteTask(task))
			{
				aiTask?.FinishExecute(cancelled: true);
				if (aiTask != null)
				{
					this.OnTaskStopped?.Invoke(aiTask);
				}
				activeTasksBySlot[slot] = task;
				task.StartExecute();
				this.OnTaskStarted?.Invoke(task);
			}
			if (entity.World.FrameProfiler.Enabled)
			{
				entity.World.FrameProfiler.Mark(task.ProfilerName);
			}
		}
	}

	private bool ShouldExecuteTask(IAiTask task)
	{
		if (this.OnShouldExecuteTask == null)
		{
			return true;
		}
		bool flag = true;
		Delegate[] invocationList = this.OnShouldExecuteTask.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			ActionBoolReturn<IAiTask> actionBoolReturn = (ActionBoolReturn<IAiTask>)invocationList[i];
			flag &= actionBoolReturn(task);
		}
		return flag;
	}

	private void ProcessRunningTasks(float dt)
	{
		for (int i = 0; i < activeTasksBySlot.Length; i++)
		{
			IAiTask aiTask = activeTasksBySlot[i];
			if (aiTask != null && aiTask.CanContinueExecute())
			{
				if (!aiTask.ContinueExecute(dt))
				{
					aiTask.FinishExecute(cancelled: false);
					this.OnTaskStopped?.Invoke(aiTask);
					activeTasksBySlot[i] = null;
				}
				if (entity.World.FrameProfiler.Enabled)
				{
					entity.World.FrameProfiler.Mark("task-continueexec-", AiTaskRegistry.TaskCodes[aiTask.GetType()]);
				}
			}
		}
	}

	private void LogRunningTasks()
	{
		if (!entity.World.EntityDebugMode)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		for (int i = 0; i < activeTasksBySlot.Length; i++)
		{
			IAiTask aiTask = activeTasksBySlot[i];
			if (aiTask != null)
			{
				if (num++ > 0)
				{
					stringBuilder.Append(", ");
				}
				AiTaskRegistry.TaskCodes.TryGetValue(aiTask.GetType(), out string value);
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 3, stringBuilder2);
				handler.AppendFormatted(value);
				handler.AppendLiteral("(p");
				handler.AppendFormatted(aiTask.Priority);
				handler.AppendLiteral(", pc ");
				handler.AppendFormatted(aiTask.PriorityForCancel);
				handler.AppendLiteral(")");
				stringBuilder2.Append(ref handler);
			}
		}
		entity.DebugAttributes.SetString("AI Tasks", (stringBuilder.Length > 0) ? stringBuilder.ToString() : "-");
	}
}
