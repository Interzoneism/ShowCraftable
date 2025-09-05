using System;
using System.Diagnostics;

namespace Vintagestory.API.Common;

public abstract class LoggerBase : ILogger
{
	private static readonly object[] _emptyArgs;

	public static string SourcePath;

	public bool TraceLog { get; set; }

	public event LogEntryDelegate EntryAdded;

	static LoggerBase()
	{
		_emptyArgs = Array.Empty<object>();
		try
		{
			throw new DummyLoggerException("Exception for the logger to load some exception related info");
		}
		catch (DummyLoggerException e)
		{
			SourcePath = new StackTrace(e, fNeedFileInfo: true).GetFrame(0).GetFileName().Split("VintagestoryApi")[0];
		}
	}

	public void ClearWatchers()
	{
		this.EntryAdded = null;
	}

	protected abstract void LogImpl(EnumLogType logType, string format, params object[] args);

	public void Log(EnumLogType logType, string format, params object[] args)
	{
		LogImpl(logType, format, args);
		this.EntryAdded?.Invoke(logType, format, args);
	}

	public void Log(EnumLogType logType, string message)
	{
		Log(logType, message, _emptyArgs);
	}

	public void LogException(EnumLogType logType, Exception e)
	{
		Log(logType, "Exception: {0}\n{1}{2}", e.Message, (e.InnerException == null) ? "" : (" ---> " + e.InnerException?.ToString() + "\n   --- End of inner exception stack trace ---\n"), CleanStackTrace(e.StackTrace));
	}

	public void Chat(string format, params object[] args)
	{
		Log(EnumLogType.Chat, format, args);
	}

	public void Chat(string message)
	{
		Log(EnumLogType.Chat, message, _emptyArgs);
	}

	public void Event(string format, params object[] args)
	{
		Log(EnumLogType.Event, format, args);
	}

	public void Event(string message)
	{
		Log(EnumLogType.Event, message, _emptyArgs);
	}

	public void StoryEvent(string format, params object[] args)
	{
		Log(EnumLogType.StoryEvent, format, args);
	}

	public void StoryEvent(string message)
	{
		Log(EnumLogType.StoryEvent, message, _emptyArgs);
	}

	public void Build(string format, params object[] args)
	{
		Log(EnumLogType.Build, format, args);
	}

	public void Build(string message)
	{
		Log(EnumLogType.Build, message, _emptyArgs);
	}

	public void VerboseDebug(string format, params object[] args)
	{
		Log(EnumLogType.VerboseDebug, format, args);
	}

	public void VerboseDebug(string message)
	{
		Log(EnumLogType.VerboseDebug, message, _emptyArgs);
	}

	public void Debug(string format, params object[] args)
	{
		Log(EnumLogType.Debug, format, args);
	}

	public void Debug(string message)
	{
		Log(EnumLogType.Debug, message, _emptyArgs);
	}

	public void Notification(string format, params object[] args)
	{
		Log(EnumLogType.Notification, format, args);
	}

	public void Notification(string message)
	{
		Log(EnumLogType.Notification, message, _emptyArgs);
	}

	public void Warning(string format, params object[] args)
	{
		Log(EnumLogType.Warning, format, args);
	}

	public void Warning(string message)
	{
		Log(EnumLogType.Warning, message, _emptyArgs);
	}

	public void Warning(Exception e)
	{
		LogException(EnumLogType.Warning, e);
	}

	public void Error(string format, params object[] args)
	{
		try
		{
			Log(EnumLogType.Error, format, args);
		}
		catch (Exception e)
		{
			Log(EnumLogType.Error, "The logger itself threw an exception");
			Error(e);
		}
	}

	public void Error(string message)
	{
		try
		{
			Log(EnumLogType.Error, message, _emptyArgs);
		}
		catch (Exception e)
		{
			Log(EnumLogType.Error, "The logger itself threw an exception");
			Error(e);
		}
	}

	public void Error(Exception e)
	{
		try
		{
			LogException(EnumLogType.Error, e);
		}
		catch (Exception e2)
		{
			Log(EnumLogType.Error, "The logger itself threw an exception");
			Error(e2);
		}
	}

	public static string CleanStackTrace(string stackTrace)
	{
		if (stackTrace == null || stackTrace.Length < 150)
		{
			stackTrace += RemoveThreeLines(Environment.StackTrace);
		}
		return stackTrace.Replace(SourcePath, "");
	}

	private static string RemoveThreeLines(string s)
	{
		int num;
		if ((num = s.IndexOf('\n')) > 0)
		{
			s = s.Substring(num + 1);
		}
		if ((num = s.IndexOf('\n')) > 0)
		{
			s = s.Substring(num + 1);
		}
		if ((num = s.IndexOf('\n')) > 0)
		{
			s = s.Substring(num + 1);
		}
		return s;
	}

	public void Fatal(string format, params object[] args)
	{
		Log(EnumLogType.Fatal, format, args);
	}

	public void Fatal(string message)
	{
		Log(EnumLogType.Fatal, message, _emptyArgs);
	}

	public void Fatal(Exception e)
	{
		LogException(EnumLogType.Error, e);
	}

	public void Audit(string format, params object[] args)
	{
		Log(EnumLogType.Audit, format, args);
	}

	public void Audit(string message)
	{
		Log(EnumLogType.Audit, message, _emptyArgs);
	}

	public void Worldgen(string format, params object[] args)
	{
		Log(EnumLogType.Worldgen, format, args);
	}

	public void Worldgen(Exception e)
	{
		LogException(EnumLogType.Worldgen, e);
	}

	public void Worldgen(string message)
	{
		Log(EnumLogType.Worldgen, message, _emptyArgs);
	}
}
