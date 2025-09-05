using System;

namespace Vintagestory.API.Client;

public abstract class UBORef : IDisposable
{
	public int Handle;

	public bool Disposed { get; protected set; }

	public int Size { get; set; }

	public abstract void Bind();

	public abstract void Unbind();

	public virtual void Dispose()
	{
		Disposed = true;
	}

	public abstract void Update<T>(T data) where T : struct;

	public abstract void Update<T>(T data, int offset, int size) where T : struct;

	public abstract void Update(object data, int offset, int size);
}
