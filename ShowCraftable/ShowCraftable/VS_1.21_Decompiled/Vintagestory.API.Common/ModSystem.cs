using Vintagestory.API.Client;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public abstract class ModSystem
{
	public Mod Mod { get; internal set; }

	public virtual bool ShouldLoad(ICoreAPI api)
	{
		return ShouldLoad(api.Side);
	}

	public virtual bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public virtual double ExecuteOrder()
	{
		return 0.1;
	}

	public virtual void StartPre(ICoreAPI api)
	{
	}

	public virtual void Start(ICoreAPI api)
	{
	}

	public virtual void AssetsLoaded(ICoreAPI api)
	{
	}

	public virtual void AssetsFinalize(ICoreAPI api)
	{
	}

	public virtual void StartClientSide(ICoreClientAPI api)
	{
	}

	public virtual void StartServerSide(ICoreServerAPI api)
	{
	}

	public virtual void Dispose()
	{
	}
}
