using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

[DocumentAsJson]
[JsonObject(/*Could not decode attribute arguments.*/)]
public class AdvancedParticleProperties : IParticlePropertiesProvider
{
	[JsonProperty]
	public NatFloat[] HsvaColor = new NatFloat[4]
	{
		NatFloat.createUniform(128f, 128f),
		NatFloat.createUniform(128f, 128f),
		NatFloat.createUniform(128f, 128f),
		NatFloat.createUniform(255f, 0f)
	};

	[JsonProperty]
	public NatFloat[] PosOffset = new NatFloat[3]
	{
		NatFloat.createUniform(0f, 0f),
		NatFloat.createUniform(0f, 0f),
		NatFloat.createUniform(0f, 0f)
	};

	public Vec3d basePos = new Vec3d();

	public Vec3f baseVelocity = new Vec3f();

	public Block block;

	public int Color;

	private Vec3d tmpPos = new Vec3d();

	private Vec3f tmpVelo = new Vec3f();

	public bool Async => false;

	[JsonProperty]
	public bool RandomVelocityChange { get; set; }

	[JsonProperty]
	public bool DieOnRainHeightmap { get; set; }

	[JsonProperty]
	public AdvancedParticleProperties[] SecondaryParticles { get; set; }

	[JsonProperty]
	public AdvancedParticleProperties[] DeathParticles { get; set; }

	[JsonProperty]
	public NatFloat SecondarySpawnInterval { get; set; } = NatFloat.createUniform(0f, 0f);

	[JsonProperty]
	public float Bounciness { get; set; }

	[JsonProperty]
	public bool DieInAir { get; set; }

	[JsonProperty]
	public bool DieInLiquid { get; set; }

	[JsonProperty]
	public bool SwimOnLiquid { get; set; }

	[JsonProperty]
	public bool ColorByBlock { get; set; }

	[JsonProperty]
	public EvolvingNatFloat OpacityEvolve { get; set; }

	[JsonProperty]
	public EvolvingNatFloat RedEvolve { get; set; }

	[JsonProperty]
	public EvolvingNatFloat GreenEvolve { get; set; }

	[JsonProperty]
	public EvolvingNatFloat BlueEvolve { get; set; }

	[JsonProperty]
	public NatFloat GravityEffect { get; set; } = NatFloat.createUniform(1f, 0f);

	[JsonProperty]
	public NatFloat LifeLength { get; set; } = NatFloat.createUniform(1f, 0f);

	[JsonProperty]
	public NatFloat Quantity { get; set; } = NatFloat.createUniform(1f, 0f);

	[JsonProperty]
	public NatFloat Size { get; set; } = NatFloat.createUniform(1f, 0f);

	[JsonProperty]
	public EvolvingNatFloat SizeEvolve { get; set; } = EvolvingNatFloat.createIdentical(0f);

	[JsonProperty]
	public NatFloat[] Velocity { get; set; } = new NatFloat[3]
	{
		NatFloat.createUniform(0f, 0.5f),
		NatFloat.createUniform(0f, 0.5f),
		NatFloat.createUniform(0f, 0.5f)
	};

	[JsonProperty]
	public EvolvingNatFloat[] VelocityEvolve { get; set; }

	[JsonProperty]
	public EnumParticleModel ParticleModel { get; set; } = EnumParticleModel.Cube;

	[JsonProperty]
	public int VertexFlags { get; set; }

	[JsonProperty]
	public bool SelfPropelled { get; set; }

	[JsonProperty]
	public bool TerrainCollision { get; set; } = true;

	[JsonProperty]
	public float WindAffectednes { get; set; }

	public int LightEmission => 0;

	bool IParticlePropertiesProvider.DieInAir => DieInAir;

	bool IParticlePropertiesProvider.DieInLiquid => DieInLiquid;

	bool IParticlePropertiesProvider.SwimOnLiquid => SwimOnLiquid;

	public Vec3d Pos
	{
		get
		{
			tmpPos.Set(basePos.X + (double)PosOffset[0].nextFloat(), basePos.Y + (double)PosOffset[1].nextFloat(), basePos.Z + (double)PosOffset[2].nextFloat());
			return tmpPos;
		}
	}

	float IParticlePropertiesProvider.Quantity => Quantity.nextFloat();

	float IParticlePropertiesProvider.Size => Size.nextFloat();

	public Vec3f ParentVelocity { get; set; }

	public float WindAffectednesAtPos { get; set; }

	public float ParentVelocityWeight { get; set; }

	EnumParticleModel IParticlePropertiesProvider.ParticleModel => ParticleModel;

	bool IParticlePropertiesProvider.SelfPropelled => SelfPropelled;

	float IParticlePropertiesProvider.SecondarySpawnInterval => SecondarySpawnInterval.nextFloat();

	bool IParticlePropertiesProvider.TerrainCollision => TerrainCollision;

	float IParticlePropertiesProvider.GravityEffect => GravityEffect.nextFloat();

	float IParticlePropertiesProvider.LifeLength => LifeLength.nextFloat();

	IParticlePropertiesProvider[] IParticlePropertiesProvider.SecondaryParticles => SecondaryParticles;

	IParticlePropertiesProvider[] IParticlePropertiesProvider.DeathParticles => DeathParticles;

	public void Init(ICoreAPI api)
	{
	}

	public int GetRgbaColor(ICoreClientAPI capi)
	{
		if (HsvaColor == null)
		{
			return Color;
		}
		int num = ColorUtil.HsvToRgba((byte)GameMath.Clamp(HsvaColor[0].nextFloat(), 0f, 255f), (byte)GameMath.Clamp(HsvaColor[1].nextFloat(), 0f, 255f), (byte)GameMath.Clamp(HsvaColor[2].nextFloat(), 0f, 255f), (byte)GameMath.Clamp(HsvaColor[3].nextFloat(), 0f, 255f));
		int num2 = num & 0xFF;
		int num3 = (num >> 8) & 0xFF;
		int num4 = (num >> 16) & 0xFF;
		int num5 = (num >> 24) & 0xFF;
		return (num2 << 16) | (num3 << 8) | num4 | (num5 << 24);
	}

	public Vec3f GetVelocity(Vec3d pos)
	{
		tmpVelo.Set(baseVelocity.X + Velocity[0].nextFloat(), baseVelocity.Y + Velocity[1].nextFloat(), baseVelocity.Z + Velocity[2].nextFloat());
		return tmpVelo;
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(basePos.X);
		writer.Write(basePos.Y);
		writer.Write(basePos.Z);
		writer.Write(DieInAir);
		writer.Write(DieInLiquid);
		writer.Write(SwimOnLiquid);
		for (int i = 0; i < 4; i++)
		{
			HsvaColor[i].ToBytes(writer);
		}
		GravityEffect.ToBytes(writer);
		LifeLength.ToBytes(writer);
		for (int j = 0; j < 3; j++)
		{
			PosOffset[j].ToBytes(writer);
		}
		Quantity.ToBytes(writer);
		Size.ToBytes(writer);
		for (int k = 0; k < 3; k++)
		{
			Velocity[k].ToBytes(writer);
		}
		writer.Write((byte)ParticleModel);
		writer.Write(VertexFlags);
		writer.Write(OpacityEvolve == null);
		if (OpacityEvolve != null)
		{
			OpacityEvolve.ToBytes(writer);
		}
		writer.Write(RedEvolve == null);
		if (RedEvolve != null)
		{
			RedEvolve.ToBytes(writer);
		}
		writer.Write(GreenEvolve == null);
		if (GreenEvolve != null)
		{
			GreenEvolve.ToBytes(writer);
		}
		writer.Write(BlueEvolve == null);
		if (BlueEvolve != null)
		{
			BlueEvolve.ToBytes(writer);
		}
		SizeEvolve.ToBytes(writer);
		writer.Write(SelfPropelled);
		writer.Write(TerrainCollision);
		writer.Write(ColorByBlock);
		writer.Write(VelocityEvolve != null);
		if (VelocityEvolve != null)
		{
			for (int l = 0; l < 3; l++)
			{
				VelocityEvolve[l].ToBytes(writer);
			}
		}
		SecondarySpawnInterval.ToBytes(writer);
		if (SecondaryParticles == null)
		{
			writer.Write(0);
		}
		else
		{
			writer.Write(SecondaryParticles.Length);
			for (int m = 0; m < SecondaryParticles.Length; m++)
			{
				SecondaryParticles[m].ToBytes(writer);
			}
		}
		if (DeathParticles == null)
		{
			writer.Write(0);
		}
		else
		{
			writer.Write(DeathParticles.Length);
			for (int n = 0; n < DeathParticles.Length; n++)
			{
				DeathParticles[n].ToBytes(writer);
			}
		}
		writer.Write(WindAffectednes);
		writer.Write(Bounciness);
	}

	public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		basePos = new Vec3d(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
		DieInAir = reader.ReadBoolean();
		DieInLiquid = reader.ReadBoolean();
		SwimOnLiquid = reader.ReadBoolean();
		HsvaColor = new NatFloat[4]
		{
			NatFloat.createFromBytes(reader),
			NatFloat.createFromBytes(reader),
			NatFloat.createFromBytes(reader),
			NatFloat.createFromBytes(reader)
		};
		GravityEffect = NatFloat.createFromBytes(reader);
		LifeLength = NatFloat.createFromBytes(reader);
		PosOffset = new NatFloat[3]
		{
			NatFloat.createFromBytes(reader),
			NatFloat.createFromBytes(reader),
			NatFloat.createFromBytes(reader)
		};
		Quantity = NatFloat.createFromBytes(reader);
		Size = NatFloat.createFromBytes(reader);
		Velocity = new NatFloat[3]
		{
			NatFloat.createFromBytes(reader),
			NatFloat.createFromBytes(reader),
			NatFloat.createFromBytes(reader)
		};
		ParticleModel = (EnumParticleModel)reader.ReadByte();
		VertexFlags = reader.ReadInt32();
		if (!reader.ReadBoolean())
		{
			OpacityEvolve = EvolvingNatFloat.CreateFromBytes(reader);
		}
		if (!reader.ReadBoolean())
		{
			RedEvolve = EvolvingNatFloat.CreateFromBytes(reader);
		}
		if (!reader.ReadBoolean())
		{
			GreenEvolve = EvolvingNatFloat.CreateFromBytes(reader);
		}
		if (!reader.ReadBoolean())
		{
			BlueEvolve = EvolvingNatFloat.CreateFromBytes(reader);
		}
		SizeEvolve.FromBytes(reader);
		SelfPropelled = reader.ReadBoolean();
		TerrainCollision = reader.ReadBoolean();
		ColorByBlock = reader.ReadBoolean();
		if (reader.ReadBoolean())
		{
			VelocityEvolve = new EvolvingNatFloat[3]
			{
				EvolvingNatFloat.createIdentical(0f),
				EvolvingNatFloat.createIdentical(0f),
				EvolvingNatFloat.createIdentical(0f)
			};
			VelocityEvolve[0].FromBytes(reader);
			VelocityEvolve[1].FromBytes(reader);
			VelocityEvolve[2].FromBytes(reader);
		}
		SecondarySpawnInterval = NatFloat.createFromBytes(reader);
		int num = reader.ReadInt32();
		if (num > 0)
		{
			SecondaryParticles = new AdvancedParticleProperties[num];
			for (int i = 0; i < num; i++)
			{
				SecondaryParticles[i] = createFromBytes(reader, resolver);
			}
		}
		int num2 = reader.ReadInt32();
		if (num2 > 0)
		{
			DeathParticles = new AdvancedParticleProperties[num2];
			for (int j = 0; j < num2; j++)
			{
				DeathParticles[j] = createFromBytes(reader, resolver);
			}
		}
		WindAffectednes = reader.ReadSingle();
		Bounciness = reader.ReadSingle();
	}

	public static AdvancedParticleProperties createFromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		AdvancedParticleProperties advancedParticleProperties = new AdvancedParticleProperties();
		advancedParticleProperties.FromBytes(reader, resolver);
		return advancedParticleProperties;
	}

	public AdvancedParticleProperties Clone()
	{
		AdvancedParticleProperties advancedParticleProperties = new AdvancedParticleProperties();
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(memoryStream);
		ToBytes(writer);
		memoryStream.Position = 0L;
		advancedParticleProperties.FromBytes(new BinaryReader(memoryStream), null);
		return advancedParticleProperties;
	}

	public void BeginParticle()
	{
		if (WindAffectednes > 0f)
		{
			ParentVelocityWeight = WindAffectednesAtPos * WindAffectednes;
			ParentVelocity = GlobalConstants.CurrentWindSpeedClient;
		}
	}

	public void PrepareForSecondarySpawn(ParticleBase particleInstance)
	{
		Vec3d position = particleInstance.Position;
		basePos.X = position.X;
		basePos.Y = position.Y;
		basePos.Z = position.Z;
	}
}
