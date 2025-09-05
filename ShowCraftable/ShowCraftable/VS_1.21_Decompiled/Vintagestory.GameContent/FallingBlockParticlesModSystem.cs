using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class FallingBlockParticlesModSystem : ModSystem
{
	private static SimpleParticleProperties dustParticles;

	private static SimpleParticleProperties bitsParticles;

	private ICoreClientAPI capi;

	private HashSet<EntityBlockFalling> fallingBlocks = new HashSet<EntityBlockFalling>();

	private ConcurrentQueue<EntityBlockFalling> toRegister = new ConcurrentQueue<EntityBlockFalling>();

	private ConcurrentQueue<EntityBlockFalling> toRemove = new ConcurrentQueue<EntityBlockFalling>();

	public int ActiveFallingBlocks => fallingBlocks.Count;

	static FallingBlockParticlesModSystem()
	{
		dustParticles = new SimpleParticleProperties(1f, 3f, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1f, 1f, 0.3f, 0.3f, EnumParticleModel.Quad);
		dustParticles.AddQuantity = 5f;
		dustParticles.MinVelocity.Set(-0.05f, -0.4f, -0.05f);
		dustParticles.AddVelocity.Set(0.1f, 0.2f, 0.1f);
		dustParticles.WithTerrainCollision = true;
		dustParticles.ParticleModel = EnumParticleModel.Quad;
		dustParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f);
		dustParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, 3f);
		dustParticles.GravityEffect = 0f;
		dustParticles.MaxSize = 1.3f;
		dustParticles.LifeLength = 3f;
		dustParticles.SelfPropelled = true;
		dustParticles.AddPos.Set(1.4, 1.4, 1.4);
		bitsParticles = new SimpleParticleProperties(1f, 3f, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1f, 1f, 0.1f, 0.3f, EnumParticleModel.Quad);
		bitsParticles.AddPos.Set(1.4, 1.4, 1.4);
		bitsParticles.AddQuantity = 20f;
		bitsParticles.MinVelocity.Set(-0.25f, 0f, -0.25f);
		bitsParticles.AddVelocity.Set(0.5f, 1f, 0.5f);
		bitsParticles.WithTerrainCollision = true;
		bitsParticles.ParticleModel = EnumParticleModel.Cube;
		bitsParticles.LifeLength = 1.5f;
		bitsParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f);
		bitsParticles.GravityEffect = 2.5f;
		bitsParticles.MinSize = 0.5f;
		bitsParticles.MaxSize = 1.5f;
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.RegisterAsyncParticleSpawner(asyncParticleSpawn);
	}

	public void Register(EntityBlockFalling entity)
	{
		toRegister.Enqueue(entity);
	}

	public void Unregister(EntityBlockFalling entity)
	{
		toRemove.Enqueue(entity);
	}

	private bool asyncParticleSpawn(float dt, IAsyncParticleManager manager)
	{
		int num = manager.ParticlesAlive(EnumParticleModel.Quad);
		float num2 = Math.Max(0.05f, (float)Math.Pow(0.949999988079071, (float)num / 200f));
		foreach (EntityBlockFalling fallingBlock in fallingBlocks)
		{
			float num3 = 0f;
			if (fallingBlock.nowImpacted)
			{
				if (capi.World.BlockAccessor.GetBlock(fallingBlock.Pos.AsBlockPos, 2).Id == 0)
				{
					num3 = 20f;
				}
				fallingBlock.nowImpacted = false;
			}
			if (fallingBlock.Block.Id != 0)
			{
				dustParticles.Color = fallingBlock.stackForParticleColor.Collectible.GetRandomColor(capi, fallingBlock.stackForParticleColor);
				dustParticles.Color &= 16777215;
				dustParticles.Color |= -1778384896;
				dustParticles.MinPos.Set(fallingBlock.Pos.X - 0.2 - 0.5, fallingBlock.Pos.Y, fallingBlock.Pos.Z - 0.2 - 0.5);
				dustParticles.MinSize = 1f;
				float num4 = num3 / 20f;
				dustParticles.AddPos.Y = fallingBlock.maxSpawnHeightForParticles;
				dustParticles.MinVelocity.Set(-0.2f * num4, 1f * (float)fallingBlock.Pos.Motion.Y, -0.2f * num4);
				dustParticles.AddVelocity.Set(0.4f * num4, 0.2f * (float)fallingBlock.Pos.Motion.Y + (0f - num4), 0.4f * num4);
				dustParticles.MinQuantity = num3 * fallingBlock.dustIntensity * num2 / 2f;
				dustParticles.AddQuantity = (6f * Math.Abs((float)fallingBlock.Pos.Motion.Y) + num3) * fallingBlock.dustIntensity * num2 / 2f;
				manager.Spawn(dustParticles);
			}
			bitsParticles.MinPos.Set(fallingBlock.Pos.X - 0.2 - 0.5, fallingBlock.Pos.Y - 0.5, fallingBlock.Pos.Z - 0.2 - 0.5);
			bitsParticles.MinVelocity.Set(-2f, 30f * (float)fallingBlock.Pos.Motion.Y, -2f);
			bitsParticles.AddVelocity.Set(4f, 0.2f * (float)fallingBlock.Pos.Motion.Y, 4f);
			bitsParticles.MinQuantity = num2;
			bitsParticles.AddQuantity = 6f * Math.Abs((float)fallingBlock.Pos.Motion.Y) * num2;
			bitsParticles.Color = dustParticles.Color;
			bitsParticles.AddPos.Y = fallingBlock.maxSpawnHeightForParticles;
			dustParticles.Color = fallingBlock.Block.GetRandomColor(capi, fallingBlock.stackForParticleColor);
			capi.World.SpawnParticles(bitsParticles);
		}
		int count = toRegister.Count;
		while (count-- > 0)
		{
			if (toRegister.TryDequeue(out var result))
			{
				fallingBlocks.Add(result);
			}
		}
		count = toRemove.Count;
		while (count-- > 0)
		{
			if (toRemove.TryDequeue(out var result2))
			{
				fallingBlocks.Remove(result2);
			}
		}
		return true;
	}
}
