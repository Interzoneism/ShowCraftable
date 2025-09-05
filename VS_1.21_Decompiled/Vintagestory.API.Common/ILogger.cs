using System;

namespace Vintagestory.API.Common;

public interface ILogger
{
	bool TraceLog { get; set; }

	event LogEntryDelegate EntryAdded;

	void ClearWatchers();

	void Log(EnumLogType logType, string format, params object[] args);

	void Log(EnumLogType logType, string message);

	void LogException(EnumLogType logType, Exception e);

	void Chat(string format, params object[] args);

	void Chat(string message);

	void Event(string format, params object[] args);

	void Event(string message);

	void StoryEvent(string format, params object[] args);

	void StoryEvent(string message);

	void Build(string format, params object[] args);

	void Build(string message);

	void VerboseDebug(string format, params object[] args);

	void VerboseDebug(string message);

	void Debug(string format, params object[] args);

	void Debug(string message);

	void Notification(string format, params object[] args);

	void Notification(string message);

	void Warning(string format, params object[] args);

	void Warning(string message);

	void Warning(Exception e);

	void Error(string format, params object[] args);

	void Error(string message);

	void Error(Exception e);

	void Fatal(string format, params object[] args);

	void Fatal(string message);

	void Fatal(Exception e);

	void Audit(string format, params object[] args);

	void Audit(string message);
}
