using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public sealed class ParticleGeneric : ParticleBase
{
	private static EvolvingNatFloat AccelerationX = EvolvingNatFloat.create(EnumTransformFunction.SINUS, (float)Math.PI * 2f);

	private static EvolvingNatFloat AccelerationZ = EvolvingNatFloat.create(EnumTransformFunction.COSINUS, 7.539823f);

	public Vec3f StartingVelocity = new Vec3f(0f, 0f, 0f);

	public Vec3f ParentVelocity;

	public float ParentVelocityWeight;

	public float SizeMultiplier = 1f;

	public float ParticleHeight;

	public EvolvingNatFloat SizeEvolve;

	public EvolvingNatFloat[] VelocityEvolve;

	public EvolvingNatFloat OpacityEvolve;

	public EvolvingNatFloat GreenEvolve;

	public EvolvingNatFloat RedEvolve;

	public EvolvingNatFloat BlueEvolve;

	public int LightEmission;

	public float GravityStrength;

	public bool TerrainCollision;

	public bool SelfPropelled;

	public bool DieInLiquid;

	public bool DieInAir;

	public bool DieOnRainHeightmap;

	public bool SwimOnLiquid;

	public bool RandomVelocityChange;

	public IParticlePropertiesProvider[] SecondaryParticles;

	public float[] SecondarySpawnTimers;

	public IParticlePropertiesProvider[] DeathParticles;

	private byte dirNormalizedX;

	private byte dirNormalizedY;

	private byte dirNormalizedZ;

	private float seq;

	private float dir = 1f;

	public ParticleGeneric()
	{
		SecondarySpawnTimers = new float[4];
	}

	public override void TickNow(float lifedt, float pdt, ICoreClientAPI api, ParticlePhysics physicsSim)
	{
		SecondsAlive += lifedt;
		if (SecondaryParticles != null)
		{
			for (int i = 0; i < SecondaryParticles.Length; i++)
			{
				SecondarySpawnTimers[i] += pdt;
				IParticlePropertiesProvider particlePropertiesProvider = SecondaryParticles[i];
				if (SecondarySpawnTimers[i] > particlePropertiesProvider.SecondarySpawnInterval)
				{
					SecondarySpawnTimers[i] = 0f;
					particlePropertiesProvider.PrepareForSecondarySpawn(this);
					api.World.SpawnParticles(particlePropertiesProvider);
				}
			}
		}
		if (TerrainCollision && SelfPropelled)
		{
			Velocity.X += (StartingVelocity.X - Velocity.X) * 0.02f;
			Velocity.Y += (StartingVelocity.Y - Velocity.Y) * 0.02f;
			Velocity.Z += (StartingVelocity.Z - Velocity.Z) * 0.02f;
		}
		Velocity.Y -= GravityStrength * pdt;
		float height = ParticleHeight * SizeMultiplier;
		physicsSim.HandleBoyancy(Position, Velocity, SwimOnLiquid, GravityStrength, pdt, height);
		if (VelocityEvolve != null)
		{
			float sequence = SecondsAlive / LifeLength;
			motion.Set(Velocity.X * VelocityEvolve[0].nextFloat(0f, sequence) * pdt, Velocity.Y * VelocityEvolve[1].nextFloat(0f, sequence) * pdt, Velocity.Z * VelocityEvolve[2].nextFloat(0f, sequence) * pdt);
		}
		else
		{
			motion.Set(Velocity.X * pdt, Velocity.Y * pdt, Velocity.Z * pdt);
		}
		if (ParentVelocity != null)
		{
			motion.Add(ParentVelocity.X * ParentVelocityWeight * pdt * tdragnow, ParentVelocity.Y * ParentVelocityWeight * pdt * tdragnow, ParentVelocity.Z * ParentVelocityWeight * pdt * tdragnow);
		}
		if (TerrainCollision)
		{
			updatePositionWithCollision(pdt, api, physicsSim, height);
		}
		else
		{
			Position.X += motion.X;
			Position.Y += motion.Y;
			Position.Z += motion.Z;
		}
		if (RandomVelocityChange)
		{
			if (seq > 0f)
			{
				Velocity.X += dir * AccelerationX.nextFloat(0f, seq) * pdt * 4f * SizeMultiplier;
				Velocity.Z += AccelerationZ.nextFloat(0f, seq) * pdt * 3f * SizeMultiplier;
				Velocity.Y += (dir * AccelerationX.nextFloat(0f, seq) * pdt * 10f * SizeMultiplier - dir * AccelerationZ.nextFloat(0f, seq) * pdt * 3f * SizeMultiplier) / 10f;
				seq += pdt / 3f;
				if (seq > 2f)
				{
					seq = 0f;
				}
				if (api.World.Rand.NextDouble() < 0.005)
				{
					seq = 0f;
				}
			}
			else
			{
				Velocity.X += (StartingVelocity.X - Velocity.X) * pdt;
				Velocity.Z += (StartingVelocity.Z - Velocity.Z) * pdt;
			}
			if (api.World.Rand.NextDouble() < 0.005)
			{
				seq = (float)api.World.Rand.NextDouble() * 0.5f;
				dir = api.World.Rand.Next(2) * 2 - 1;
			}
		}
		Alive = SecondsAlive < LifeLength && (!DieInAir || physicsSim.BlockAccess.GetBlockRaw((int)Position.X, (int)(Position.Y + 0.15000000596046448), (int)Position.Z, 2).IsLiquid());
		tickCount++;
		if (tickCount > 2)
		{
			lightrgbs = ((LightEmission == int.MaxValue) ? LightEmission : physicsSim.BlockAccess.GetLightRGBsAsInt((int)Position.X, (int)Position.Y, (int)Position.Z));
			if (LightEmission != 0)
			{
				lightrgbs = Math.Max(lightrgbs & 0xFF, LightEmission & 0xFF) | Math.Max(lightrgbs & 0xFF00, LightEmission & 0xFF00) | Math.Max(lightrgbs & 0xFF0000, LightEmission & 0xFF0000);
			}
			if (DieOnRainHeightmap)
			{
				float num = 1f - prevPosAdvance;
				double num2 = prevPosDeltaX * num * pdt * 8f;
				double num3 = prevPosDeltaY * num * pdt * 12f;
				double num4 = prevPosDeltaZ * num * pdt * 8f;
				Alive &= (double)physicsSim.BlockAccess.GetRainMapHeightAt((int)(Position.X + num2), (int)(Position.Z + num4)) - num3 < Position.Y;
			}
			Alive &= !DieInLiquid || !physicsSim.BlockAccess.GetBlockRaw((int)Position.X, (int)(Position.Y + 0.15000000596046448), (int)Position.Z, 2).IsLiquid();
			tickCount = 0;
			float num5 = Velocity.Length();
			dirNormalizedX = (byte)(Velocity.X / num5 * 128f);
			dirNormalizedY = (byte)(Velocity.Y / num5 * 128f);
			dirNormalizedZ = (byte)(Velocity.Z / num5 * 128f);
		}
		if (!Alive && DeathParticles != null)
		{
			for (int j = 0; j < DeathParticles.Length; j++)
			{
				IParticlePropertiesProvider particlePropertiesProvider2 = DeathParticles[j];
				particlePropertiesProvider2.PrepareForSecondarySpawn(this);
				api.World.SpawnParticles(particlePropertiesProvider2);
			}
		}
	}

	public override void UpdateBuffers(MeshData buffer, Vec3d cameraPos, ref int posPosition, ref int rgbaPosition, ref int flagPosition)
	{
		float sequence = SecondsAlive / LifeLength;
		float num = 1f - prevPosAdvance;
		buffer.CustomFloats.Values[posPosition++] = (float)(Position.X - (double)(prevPosDeltaX * num) - cameraPos.X);
		buffer.CustomFloats.Values[posPosition++] = (float)(Position.Y - (double)(prevPosDeltaY * num) - cameraPos.Y);
		buffer.CustomFloats.Values[posPosition++] = (float)(Position.Z - (double)(prevPosDeltaZ * num) - cameraPos.Z);
		buffer.CustomFloats.Values[posPosition++] = ((SizeEvolve != null) ? SizeEvolve.nextFloat(SizeMultiplier, sequence) : SizeMultiplier);
		byte b = ColorAlpha;
		if (OpacityEvolve != null)
		{
			b = (byte)GameMath.Clamp(OpacityEvolve.nextFloat((int)b, sequence), 0f, 255f);
		}
		buffer.CustomBytes.Values[rgbaPosition++] = dirNormalizedX;
		buffer.CustomBytes.Values[rgbaPosition++] = dirNormalizedY;
		buffer.CustomBytes.Values[rgbaPosition++] = dirNormalizedZ;
		rgbaPosition++;
		buffer.CustomBytes.Values[rgbaPosition++] = (byte)lightrgbs;
		buffer.CustomBytes.Values[rgbaPosition++] = (byte)(lightrgbs >> 8);
		buffer.CustomBytes.Values[rgbaPosition++] = (byte)(lightrgbs >> 16);
		buffer.CustomBytes.Values[rgbaPosition++] = (byte)(lightrgbs >> 24);
		buffer.CustomBytes.Values[rgbaPosition++] = (byte)((float)(int)ColorBlue + ((BlueEvolve == null) ? 0f : BlueEvolve.nextFloat((int)ColorBlue, sequence)));
		buffer.CustomBytes.Values[rgbaPosition++] = (byte)((float)(int)ColorGreen + ((GreenEvolve == null) ? 0f : GreenEvolve.nextFloat((int)ColorGreen, sequence)));
		buffer.CustomBytes.Values[rgbaPosition++] = (byte)((float)(int)ColorRed + ((RedEvolve == null) ? 0f : RedEvolve.nextFloat((int)ColorRed, sequence)));
		buffer.CustomBytes.Values[rgbaPosition++] = b;
		buffer.Flags[flagPosition++] = VertexFlags;
	}

	public void Spawned(ParticlePhysics physicsSim)
	{
		Alive = true;
		SecondsAlive = 0f;
		accum = physicsSim.PhysicsTickTime;
		lightrgbs = physicsSim.BlockAccess.GetLightRGBsAsInt((int)Position.X, (int)Position.Y, (int)Position.Z);
		if (SecondaryParticles != null)
		{
			for (int i = 0; i < SecondaryParticles.Length; i++)
			{
				SecondarySpawnTimers[i] = 0f;
			}
		}
	}
}
