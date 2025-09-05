using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common.Entities;

public abstract class PhysicsBehaviorBase : EntityBehavior
{
	protected ICoreClientAPI capi;

	protected ICoreServerAPI sapi;

	protected const float clientInterval = 1f / 15f;

	protected int previousVersion;

	public IMountable mountableSupplier;

	protected readonly EntityPos lPos = new EntityPos();

	protected Vec3d nPos;

	public float CollisionYExtra = 1f;

	[ThreadStatic]
	protected internal static CachingCollisionTester collisionTester;

	static PhysicsBehaviorBase()
	{
	}

	public PhysicsBehaviorBase(Entity entity)
		: base(entity)
	{
	}

	public static void InitServerMT(ICoreServerAPI sapi)
	{
		collisionTester = new CachingCollisionTester();
		sapi.Event.PhysicsThreadStart += delegate
		{
			collisionTester = new CachingCollisionTester();
		};
	}

	public void Init()
	{
		if (entity.Api is ICoreClientAPI coreClientAPI)
		{
			capi = coreClientAPI;
		}
		if (entity.Api is ICoreServerAPI coreServerAPI)
		{
			sapi = coreServerAPI;
		}
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		mountableSupplier = entity.GetInterface<IMountable>();
	}
}
