using System;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Server;

public class ChunkLoadOptions
{
	public bool KeepLoaded;

	public Action OnLoaded;

	public ITreeAttribute ChunkGenParams = new TreeAttribute();
}
