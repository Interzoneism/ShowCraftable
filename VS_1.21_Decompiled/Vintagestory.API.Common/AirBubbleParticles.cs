using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class AirBubbleParticles : IParticlePropertiesProvider
{
	private Random rand = new Random();

	public Vec3d BasePos = new Vec3d();

	public Vec3f AddVelocity = new Vec3f();

	public float Range = 0.25f;

	public float quantity = 30f;

	public float horVelocityMul = 1f;

	public bool Async => false;

	public IParticlePropertiesProvider[] SecondaryParticles => null;

	public IParticlePropertiesProvider[] DeathParticles => null;

	public float Bounciness { get; set; }

	public bool DieInAir => false;

	public bool DieInLiquid => false;

	public bool SwimOnLiquid { get; set; } = true;

	public int VertexFlags => 0;

	public float GravityEffect => 0.1f;

	public bool TerrainCollision => true;

	public float LifeLength { get; set; } = 0.25f;

	public EvolvingNatFloat OpacityEvolve => null;

	public EvolvingNatFloat RedEvolve => null;

	public EvolvingNatFloat GreenEvolve => null;

	public EvolvingNatFloat BlueEvolve => null;

	public bool RandomVelocityChange { get; set; }

	public Vec3d Pos => new Vec3d(BasePos.X + rand.NextDouble() * (double)Range - (double)(Range / 2f), BasePos.Y + 0.1 + rand.NextDouble() * 0.2, BasePos.Z + rand.NextDouble() * (double)Range - (double)(Range / 2f));

	public float Quantity => quantity;

	public int LightEmission => 0;

	public float Size => (float)rand.NextDouble() * 0.2f + 0.2f;

	public EvolvingNatFloat SizeEvolve => new EvolvingNatFloat(EnumTransformFunction.LINEAR, 0.25f);

	public EvolvingNatFloat[] VelocityEvolve => null;

	public EnumParticleModel ParticleModel => EnumParticleModel.Cube;

	public bool SelfPropelled => false;

	public float SecondarySpawnInterval => 0f;

	public bool DieOnRainHeightmap => false;

	public Vec3f ParentVelocity => null;

	float IParticlePropertiesProvider.ParentVelocityWeight => 0f;

	public void Init(ICoreAPI api)
	{
	}

	public int GetRgbaColor(ICoreClientAPI capi)
	{
		return ColorUtil.HsvToRgba(110, 20 + rand.Next(20), 220 + rand.Next(30), 120 + rand.Next(50));
	}

	public Vec3f GetVelocity(Vec3d pos)
	{
		return new Vec3f(horVelocityMul * ((float)rand.NextDouble() - 0.5f + AddVelocity.X), 0.1f * (float)rand.NextDouble() + 0.4f + AddVelocity.Y, horVelocityMul * ((float)rand.NextDouble() - 0.5f + AddVelocity.Z));
	}

	public void ToBytes(BinaryWriter writer)
	{
	}

	public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
	}

	public void BeginParticle()
	{
	}

	public void PrepareForSecondarySpawn(ParticleBase particleInstance)
	{
	}
}
