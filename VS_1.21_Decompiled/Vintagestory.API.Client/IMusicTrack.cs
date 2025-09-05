using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface IMusicTrack
{
	string Name { get; }

	bool IsActive { get; }

	float Priority { get; }

	float StartPriority { get; }

	string PositionString { get; }

	void BeginSort();

	void Initialize(IAssetManager assetManager, ICoreClientAPI capi, IMusicEngine musicEngine);

	bool ShouldPlay(TrackedPlayerProperties props, ClimateCondition conds, BlockPos pos);

	void BeginPlay(TrackedPlayerProperties props);

	bool ContinuePlay(float dt, TrackedPlayerProperties props);

	void UpdateVolume();

	void FadeOut(float seconds, Action onFadedOut = null);

	void FastForward(float seconds);
}
