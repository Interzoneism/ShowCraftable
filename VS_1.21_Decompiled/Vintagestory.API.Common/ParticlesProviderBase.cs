using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public abstract class ParticlesProviderBase : IParticlePropertiesProvider
{
	public bool Async => false;

	public float Bounciness { get; set; }

	public bool RandomVelocityChange { get; set; }

	public bool DieOnRainHeightmap { get; set; }

	public virtual bool DieInLiquid => false;

	public virtual bool SwimOnLiquid => false;

	public virtual bool DieInAir => false;

	public virtual float Quantity => 1f;

	public virtual Vec3d Pos => Vec3d.Zero;

	public virtual EvolvingNatFloat OpacityEvolve => null;

	public virtual EvolvingNatFloat RedEvolve => null;

	public virtual EvolvingNatFloat GreenEvolve => null;

	public virtual EvolvingNatFloat BlueEvolve => null;

	public virtual EnumParticleModel ParticleModel => EnumParticleModel.Quad;

	public virtual float Size => 1f;

	public virtual EvolvingNatFloat SizeEvolve => null;

	public virtual EvolvingNatFloat[] VelocityEvolve => null;

	public virtual float GravityEffect => 1f;

	public virtual float LifeLength => 1f;

	public virtual int VertexFlags => 0;

	public virtual bool SelfPropelled => false;

	public bool TerrainCollision => true;

	public virtual float SecondarySpawnInterval => 0f;

	public virtual IParticlePropertiesProvider[] SecondaryParticles => null;

	public IParticlePropertiesProvider[] DeathParticles => null;

	public Vec3f ParentVelocity { get; set; }

	public bool WindAffected { get; set; }

	public float ParentVelocityWeight { get; set; }

	public int LightEmission { get; set; }

	public virtual Vec3f GetVelocity(Vec3d pos)
	{
		return Vec3f.Zero;
	}

	public virtual int GetRgbaColor(ICoreClientAPI capi)
	{
		return -1;
	}

	public virtual void ToBytes(BinaryWriter writer)
	{
	}

	public virtual void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
	}

	public virtual void BeginParticle()
	{
	}

	public virtual void PrepareForSecondarySpawn(ParticleBase particleInstance)
	{
	}

	public virtual void Init(ICoreAPI api)
	{
	}
}
