using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public abstract class CollectibleParticleProperties : IParticlePropertiesProvider
{
	public Random rand = new Random();

	public ICoreAPI api;

	public bool Async => false;

	public float Bounciness { get; set; }

	public bool DieOnRainHeightmap { get; set; }

	public virtual bool RandomVelocityChange { get; set; }

	public virtual bool DieInLiquid => false;

	public virtual bool SwimOnLiquid => false;

	public virtual bool DieInAir => false;

	public abstract float Quantity { get; }

	public abstract Vec3d Pos { get; }

	public int LightEmission { get; set; }

	public abstract int VertexFlags { get; }

	public abstract EnumParticleModel ParticleModel { get; }

	public virtual bool SelfPropelled => false;

	public virtual bool TerrainCollision => true;

	public virtual float Size => 1f;

	public virtual float GravityEffect => 1f;

	public virtual float LifeLength => 1.5f;

	public virtual EvolvingNatFloat OpacityEvolve => null;

	public virtual EvolvingNatFloat RedEvolve => null;

	public virtual EvolvingNatFloat GreenEvolve => null;

	public virtual EvolvingNatFloat BlueEvolve => null;

	public virtual EvolvingNatFloat SizeEvolve => null;

	public virtual EvolvingNatFloat[] VelocityEvolve => null;

	public virtual IParticlePropertiesProvider[] SecondaryParticles => null;

	public IParticlePropertiesProvider[] DeathParticles => null;

	public virtual float SecondarySpawnInterval => 0f;

	public Vec3f ParentVelocity { get; set; }

	public float ParentVelocityWeight { get; set; }

	public abstract Vec3f GetVelocity(Vec3d pos);

	public abstract int GetRgbaColor(ICoreClientAPI capi);

	public virtual bool UseLighting()
	{
		return true;
	}

	public Vec3d RandomBlockPos(IBlockAccessor blockAccess, BlockPos pos, Block block, BlockFacing facing = null)
	{
		Cuboidf particleBreakBox = block.GetParticleBreakBox(blockAccess, pos, facing);
		if (facing == null)
		{
			return new Vec3d((double)((float)pos.X + particleBreakBox.X1 + 1f / 32f) + rand.NextDouble() * (double)(particleBreakBox.XSize - 0.0625f), (double)((float)pos.InternalY + particleBreakBox.Y1 + 1f / 32f) + rand.NextDouble() * (double)(particleBreakBox.YSize - 0.0625f), (double)((float)pos.Z + particleBreakBox.Z1 + 1f / 32f) + rand.NextDouble() * (double)(particleBreakBox.ZSize - 0.0625f));
		}
		bool flag = particleBreakBox != null;
		Vec3i normali = facing.Normali;
		Vec3d vec3d = new Vec3d((float)pos.X + 0.5f + (float)normali.X / 1.9f + ((!flag || facing.Axis != EnumAxis.X) ? 0f : ((normali.X > 0) ? (particleBreakBox.X2 - 1f) : particleBreakBox.X1)), (float)pos.InternalY + 0.5f + (float)normali.Y / 1.9f + ((!flag || facing.Axis != EnumAxis.Y) ? 0f : ((normali.Y > 0) ? (particleBreakBox.Y2 - 1f) : particleBreakBox.Y1)), (float)pos.Z + 0.5f + (float)normali.Z / 1.9f + ((!flag || facing.Axis != EnumAxis.Z) ? 0f : ((normali.Z > 0) ? (particleBreakBox.Z2 - 1f) : particleBreakBox.Z1)));
		vec3d.Add((rand.NextDouble() - 0.5) * (double)(1 - Math.Abs(normali.X)), (rand.NextDouble() - 0.5) * (double)(1 - Math.Abs(normali.Y)) - (double)((facing == BlockFacing.DOWN) ? 0.1f : 0f), (rand.NextDouble() - 0.5) * (double)(1 - Math.Abs(normali.Z)));
		return vec3d;
	}

	public virtual Block ColorByBlock()
	{
		return null;
	}

	public virtual void ToBytes(BinaryWriter writer)
	{
	}

	public virtual void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
	}

	public void BeginParticle()
	{
	}

	public virtual void PrepareForSecondarySpawn(ParticleBase particleInstance)
	{
	}

	public virtual void Init(ICoreAPI api)
	{
		this.api = api;
	}
}
