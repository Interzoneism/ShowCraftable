using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IParticlePropertiesProvider
{
	bool Async { get; }

	float ParentVelocityWeight { get; }

	bool DieInLiquid { get; }

	bool SwimOnLiquid { get; }

	float Bounciness { get; }

	bool DieInAir { get; }

	bool DieOnRainHeightmap { get; }

	float Quantity { get; }

	Vec3d Pos { get; }

	Vec3f ParentVelocity { get; }

	int LightEmission { get; }

	EvolvingNatFloat OpacityEvolve { get; }

	EvolvingNatFloat RedEvolve { get; }

	EvolvingNatFloat GreenEvolve { get; }

	EvolvingNatFloat BlueEvolve { get; }

	EnumParticleModel ParticleModel { get; }

	float Size { get; }

	EvolvingNatFloat SizeEvolve { get; }

	EvolvingNatFloat[] VelocityEvolve { get; }

	float GravityEffect { get; }

	float LifeLength { get; }

	int VertexFlags { get; }

	bool SelfPropelled { get; }

	bool TerrainCollision { get; }

	float SecondarySpawnInterval { get; }

	IParticlePropertiesProvider[] SecondaryParticles { get; }

	IParticlePropertiesProvider[] DeathParticles { get; }

	bool RandomVelocityChange { get; }

	void Init(ICoreAPI api);

	void BeginParticle();

	Vec3f GetVelocity(Vec3d pos);

	int GetRgbaColor(ICoreClientAPI capi);

	void ToBytes(BinaryWriter writer);

	void FromBytes(BinaryReader reader, IWorldAccessor resolver);

	void PrepareForSecondarySpawn(ParticleBase particleInstance);
}
