using System;

namespace Vintagestory.API.Client;

public abstract class MeshRef : IDisposable
{
	public bool MultidrawByTextureId { get; protected set; }

	public abstract bool Initialized { get; }

	public bool Disposed { get; protected set; }

	public virtual void Dispose()
	{
		Disposed = true;
	}
}
