using System;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class LoadedTexture : IDisposable
{
	public int TextureId;

	public int Width;

	public int Height;

	protected bool disposed;

	protected string trace;

	protected ICoreClientAPI capi;

	public bool Disposed => disposed;

	public bool IgnoreUndisposed { get; set; }

	public LoadedTexture(ICoreClientAPI capi)
	{
		this.capi = capi;
		if (RuntimeEnv.DebugTextureDispose)
		{
			trace = Environment.StackTrace;
		}
	}

	public LoadedTexture(ICoreClientAPI capi, int textureId, int width, int height)
	{
		this.capi = capi;
		TextureId = textureId;
		Width = width;
		Height = height;
		if (RuntimeEnv.DebugTextureDispose)
		{
			trace = Environment.StackTrace;
		}
	}

	public virtual void Dispose()
	{
		disposed = true;
		if (TextureId != 0)
		{
			capi.Gui.DeleteTexture(TextureId);
			TextureId = 0;
		}
	}

	~LoadedTexture()
	{
		if (IgnoreUndisposed || (TextureId == 0 || disposed))
		{
			return;
		}
		ICoreClientAPI coreClientAPI = capi;
		if (coreClientAPI == null || !coreClientAPI.IsShuttingDown)
		{
			if (trace == null)
			{
				capi?.Logger.Debug("Texture with texture id {0} is leaking memory, missing call to Dispose. Set env var TEXTURE_DEBUG_DISPOSE to get allocation trace.", TextureId);
			}
			else
			{
				capi?.Logger.Debug("Texture with texture id {0} is leaking memory, missing call to Dispose. Allocated at {1}.", TextureId, trace);
			}
		}
	}
}
