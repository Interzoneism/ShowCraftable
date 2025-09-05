using System;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public interface IMusicEngine
{
	IMusicTrack CurrentTrack { get; }

	IMusicTrack LastPlayedTrack { get; }

	long MillisecondsSinceLastTrack { get; }

	void LoadTrack(AssetLocation location, Action<ILoadedSound> onLoaded, float volume = 1f, float pitch = 1f);

	void StopTrack(IMusicTrack musicTrack);
}
