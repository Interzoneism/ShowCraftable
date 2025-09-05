using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Server;

public interface IServerAPI
{
	string ServerIp { get; }

	IServerPlayer[] Players { get; }

	IServerConfig Config { get; }

	EnumServerRunPhase CurrentRunPhase { get; }

	bool IsDedicated { get; }

	bool IsShuttingDown { get; }

	bool ReducedServerThreads { get; }

	long TotalReceivedBytes { get; }

	long TotalSentBytes { get; }

	int ServerUptimeSeconds { get; }

	long ServerUptimeMilliseconds { get; }

	int TotalWorldPlayTime { get; }

	ILogger Logger { get; }

	void MarkConfigDirty();

	void ShutDown();

	void AddServerThread(string threadname, IAsyncServerSystem system);

	bool PauseThread(string threadname, int waitTimeoutMs = 5000);

	void ResumeThread(string threadname);

	void LogChat(string message, params object[] args);

	void LogBuild(string message, params object[] args);

	void LogVerboseDebug(string message, params object[] args);

	void LogDebug(string message, params object[] args);

	void LogNotification(string message, params object[] args);

	void LogWarning(string message, params object[] args);

	void LogError(string message, params object[] args);

	void LogFatal(string message, params object[] args);

	void LogEvent(string message, params object[] args);

	int LoadMiniDimension(IMiniDimension blocks);

	int SetMiniDimension(IMiniDimension miniDimension, int subId);

	IMiniDimension GetMiniDimension(int subId);

	void AddPhysicsTickable(IPhysicsTickable entityBehavior);

	void RemovePhysicsTickable(IPhysicsTickable entityBehavior);
}
