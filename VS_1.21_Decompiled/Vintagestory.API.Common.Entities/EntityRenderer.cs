using Vintagestory.API.Client;

namespace Vintagestory.API.Common.Entities;

public abstract class EntityRenderer
{
	public Entity entity;

	public ICoreClientAPI capi;

	public EntityRenderer(Entity entity, ICoreClientAPI api)
	{
		this.entity = entity;
		capi = api;
	}

	public virtual void OnEntityLoaded()
	{
	}

	public virtual void DoRender3DOpaque(float dt, bool isShadowPass)
	{
	}

	public virtual void DoRender3DOpaqueBatched(float dt, bool isShadowPass)
	{
	}

	public virtual void DoRender3DAfterOIT(float dt, bool isShadowPass)
	{
	}

	public virtual void DoRender2D(float dt)
	{
	}

	public virtual void RenderToGui(float dt, double posX, double posY, double posZ, float yawDelta, float size)
	{
	}

	public virtual void BeforeRender(float dt)
	{
	}

	public abstract void Dispose();

	public virtual void DoRender3DOIT(float dt)
	{
	}

	public virtual void DoRender3DOITBatched(float dt)
	{
	}
}
