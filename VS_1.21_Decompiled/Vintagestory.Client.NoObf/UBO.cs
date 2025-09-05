using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class UBO : UBORef
{
	public override void Bind()
	{
		GL.BindBuffer((BufferTarget)35345, Handle);
		GL.BindBufferBase((BufferRangeTarget)35345, 0, Handle);
	}

	public override void Dispose()
	{
		base.Dispose();
		GL.DeleteBuffers(1, ref Handle);
	}

	public override void Unbind()
	{
		GL.BindBuffer((BufferTarget)35345, 0);
	}

	public override void Update<T>(T data)
	{
		if (Unsafe.SizeOf<T>() != base.Size)
		{
			throw new ArgumentException("Supplied struct must be of byte size " + base.Size + " but has size " + Unsafe.SizeOf<T>());
		}
		Bind();
		using (GCHandleProvider gCHandleProvider = new GCHandleProvider(data))
		{
			GL.BufferData((BufferTarget)35345, base.Size, (IntPtr)gCHandleProvider.Pointer, (BufferUsageHint)35048);
		}
		Unbind();
	}

	public override void Update<T>(T data, int offset, int size)
	{
		if (Unsafe.SizeOf<T>() != base.Size)
		{
			throw new ArgumentException("Supplied struct must be of byte size " + base.Size + " but has size " + Unsafe.SizeOf<T>());
		}
		Bind();
		using (GCHandleProvider gCHandleProvider = new GCHandleProvider(data))
		{
			GL.BufferSubData((BufferTarget)35345, (IntPtr)offset, size, (IntPtr)gCHandleProvider.Pointer);
		}
		Unbind();
	}

	public override void Update(object data, int offset, int size)
	{
		Bind();
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		nint num = gCHandle.AddrOfPinnedObject();
		GL.BufferSubData((BufferTarget)35345, (IntPtr)offset, size, (IntPtr)num);
		gCHandle.Free();
		Unbind();
	}
}
