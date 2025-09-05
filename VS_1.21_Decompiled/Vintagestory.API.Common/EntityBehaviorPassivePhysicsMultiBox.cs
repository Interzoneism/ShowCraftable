using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

namespace Vintagestory.API.Common;

public class EntityBehaviorPassivePhysicsMultiBox : EntityBehaviorPassivePhysics, IRenderer, IDisposable
{
	protected Cuboidf[] OrigCollisionBoxes;

	protected Cuboidf[] CollisionBoxes;

	private WireframeCube entityWf;

	[ThreadStatic]
	protected internal static MultiCollisionTester mcollisionTester;

	private Matrixf mat = new Matrixf();

	private Vec3d tmpPos = new Vec3d();

	private float pushVelocityMul = 1f;

	public double RenderOrder => 0.5;

	public int RenderRange => 99;

	public EntityBehaviorPassivePhysicsMultiBox(Entity entity)
		: base(entity)
	{
		if (mcollisionTester == null)
		{
			mcollisionTester = new MultiCollisionTester();
		}
	}

	public static void InitServer(ICoreServerAPI sapi)
	{
		mcollisionTester = new MultiCollisionTester();
		sapi.Event.PhysicsThreadStart += delegate
		{
			mcollisionTester = new MultiCollisionTester();
		};
	}

	public void Dispose()
	{
		entityWf?.Dispose();
		capi?.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		if (entity.Api is ICoreClientAPI coreClientAPI)
		{
			coreClientAPI.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "PassivePhysicsMultiBoxWf");
			entityWf = WireframeCube.CreateCenterOriginCube(coreClientAPI, -1);
		}
		base.Initialize(properties, attributes);
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		AdjustCollisionBoxesToYaw(1f, push: false, entity.SidedPos.Yaw);
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		Dispose();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (capi.Render.WireframeDebugRender.Entity)
		{
			if (capi.World.Player.Entity.MountedOn != entity)
			{
				AdjustCollisionBoxesToYaw(deltaTime * 60f, push: false, entity.SidedPos.Yaw);
			}
			Cuboidf[] collisionBoxes = CollisionBoxes;
			foreach (Cuboidf cuboidf in collisionBoxes)
			{
				float num = cuboidf.XSize / 2f;
				float num2 = cuboidf.YSize / 2f;
				float num3 = cuboidf.ZSize / 2f;
				double posx = entity.Pos.X + (double)cuboidf.X1 + (double)num;
				double posy = entity.Pos.Y + (double)cuboidf.Y1 + (double)num2;
				double posz = entity.Pos.Z + (double)cuboidf.Z1 + (double)num3;
				entityWf.Render(capi, posx, posy, posz, num, num2, num3, 1f, new Vec4f(1f, 0f, 1f, 1f));
			}
		}
	}

	public override void SetProperties(JsonObject attributes)
	{
		base.SetProperties(attributes);
		CollisionBoxes = attributes["collisionBoxes"].AsArray<Cuboidf>();
		OrigCollisionBoxes = attributes["collisionBoxes"].AsArray<Cuboidf>();
	}

	protected override void applyCollision(EntityPos pos, float dtFactor)
	{
		AdjustCollisionBoxesToYaw(dtFactor, push: true, entity.SidedPos.Yaw);
		mcollisionTester.ApplyTerrainCollision(CollisionBoxes, CollisionBoxes.Length, entity, pos, dtFactor, ref newPos, 0f, CollisionYExtra);
	}

	public bool AdjustCollisionBoxesToYaw(float dtFac, bool push, float newYaw)
	{
		adjustBoxesToYaw(newYaw);
		if (push)
		{
			tmpPos.Set(entity.SidedPos.X, entity.SidedPos.Y, entity.SidedPos.Z);
			Cuboidd collidingCollisionBox = mcollisionTester.GetCollidingCollisionBox(entity.World.BlockAccessor, CollisionBoxes, CollisionBoxes.Length, tmpPos, alsoCheckTouch: false);
			if (collidingCollisionBox != null)
			{
				if (PushoutOfCollisionbox(dtFac / 60f, collidingCollisionBox))
				{
					return true;
				}
				return false;
			}
		}
		return true;
	}

	private void adjustBoxesToYaw(float newYaw)
	{
		for (int i = 0; i < OrigCollisionBoxes.Length; i++)
		{
			Cuboidf cuboidf = OrigCollisionBoxes[i];
			float midX = cuboidf.MidX;
			float midY = cuboidf.MidY;
			float midZ = cuboidf.MidZ;
			mat.Identity();
			mat.RotateY(newYaw + (float)Math.PI);
			Vec4d vec4d = mat.TransformVector(new Vec4d(midX, midY, midZ, 1.0));
			Cuboidf cuboidf2 = CollisionBoxes[i];
			double value = vec4d.X - (double)cuboidf2.MidX;
			double value2 = vec4d.Z - (double)cuboidf2.MidZ;
			if (Math.Abs(value) > 0.01 || Math.Abs(value2) > 0.01)
			{
				float num = cuboidf.Width / 2f;
				float num2 = cuboidf.Height / 2f;
				float num3 = cuboidf.Length / 2f;
				cuboidf2.Set((float)vec4d.X - num, (float)vec4d.Y - num2, (float)vec4d.Z - num3, (float)vec4d.X + num, (float)vec4d.Y + num2, (float)vec4d.Z + num3);
			}
		}
	}

	private bool PushoutOfCollisionbox(float dt, Cuboidd collBox)
	{
		double x = entity.SidedPos.X;
		double y = entity.SidedPos.Y;
		double z = entity.SidedPos.Z;
		IBlockAccessor blockAccessor = entity.World.BlockAccessor;
		Vec3i vec3i = null;
		double num = 99.0;
		for (int i = 0; i < Cardinal.ALL.Length; i++)
		{
			if (num <= 0.25)
			{
				break;
			}
			Cardinal cardinal = Cardinal.ALL[i];
			for (int j = 1; j <= 4; j++)
			{
				float num2 = (float)j / 4f;
				if (mcollisionTester.GetCollidingCollisionBox(blockAccessor, CollisionBoxes, CollisionBoxes.Length, tmpPos.Set(x + (double)((float)cardinal.Normali.X * num2), y, z + (double)((float)cardinal.Normali.Z * num2)), alsoCheckTouch: false) == null && (double)num2 < num)
				{
					num = num2 + (cardinal.IsDiagnoal ? 0.1f : 0f);
					vec3i = cardinal.Normali;
					break;
				}
			}
		}
		if (vec3i == null)
		{
			return false;
		}
		dt = Math.Min(dt, 0.1f);
		float num3 = ((float)entity.World.Rand.NextDouble() - 0.5f) / 600f;
		float num4 = ((float)entity.World.Rand.NextDouble() - 0.5f) / 600f;
		entity.SidedPos.X += (float)vec3i.X * dt * 1.5f;
		entity.SidedPos.Z += (float)vec3i.Z * dt * 1.5f;
		entity.SidedPos.Motion.X = pushVelocityMul * (float)vec3i.X * dt + num3;
		entity.SidedPos.Motion.Z = pushVelocityMul * (float)vec3i.Z * dt + num4;
		return true;
	}
}
