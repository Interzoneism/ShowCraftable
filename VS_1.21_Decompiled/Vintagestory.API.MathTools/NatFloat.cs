using System;
using System.IO;

namespace Vintagestory.API.MathTools;

[DocumentAsJson]
public class NatFloat
{
	[DocumentAsJson]
	public float offset;

	[DocumentAsJson]
	public float avg;

	[DocumentAsJson]
	public float var;

	[DocumentAsJson]
	public EnumDistribution dist;

	[ThreadStatic]
	private static Random threadsafeRand;

	public static NatFloat Zero => new NatFloat(0f, 0f, EnumDistribution.UNIFORM);

	public static NatFloat One => new NatFloat(1f, 0f, EnumDistribution.UNIFORM);

	public NatFloat(float averagevalue, float variance, EnumDistribution distribution)
	{
		avg = averagevalue;
		var = variance;
		dist = distribution;
	}

	public static NatFloat createInvexp(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.INVEXP);
	}

	public static NatFloat createStrongInvexp(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.STRONGINVEXP);
	}

	public static NatFloat createStrongerInvexp(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.STRONGERINVEXP);
	}

	public static NatFloat createUniform(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.UNIFORM);
	}

	public static NatFloat createGauss(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.GAUSSIAN);
	}

	public static NatFloat createNarrowGauss(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.NARROWGAUSSIAN);
	}

	public static NatFloat createInvGauss(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.INVERSEGAUSSIAN);
	}

	public static NatFloat createTri(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.TRIANGLE);
	}

	public static NatFloat createDirac(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.DIRAC);
	}

	public static NatFloat create(EnumDistribution distribution, float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, distribution);
	}

	public NatFloat copyWithOffset(float value)
	{
		NatFloat natFloat = new NatFloat(value, value, dist);
		natFloat.offset += value;
		return natFloat;
	}

	public NatFloat addOffset(float value)
	{
		offset += value;
		return this;
	}

	public NatFloat setOffset(float offset)
	{
		this.offset = offset;
		return this;
	}

	public float nextFloat()
	{
		return nextFloat(1f, threadsafeRand ?? (threadsafeRand = new Random()));
	}

	public float nextFloat(float multiplier)
	{
		return nextFloat(multiplier, threadsafeRand ?? (threadsafeRand = new Random()));
	}

	public float nextFloat(float multiplier, Random rand)
	{
		switch (dist)
		{
		case EnumDistribution.UNIFORM:
		{
			float num = (float)rand.NextDouble() - 0.5f;
			return offset + multiplier * (avg + num * 2f * var);
		}
		case EnumDistribution.GAUSSIAN:
		{
			float num = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 3f;
			num -= 0.5f;
			return offset + multiplier * (avg + num * 2f * var);
		}
		case EnumDistribution.NARROWGAUSSIAN:
		{
			float num = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 6f;
			num -= 0.5f;
			return offset + multiplier * (avg + num * 2f * var);
		}
		case EnumDistribution.VERYNARROWGAUSSIAN:
		{
			float num = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 12f;
			num -= 0.5f;
			return offset + multiplier * (avg + num * 2f * var);
		}
		case EnumDistribution.INVEXP:
		{
			float num = (float)(rand.NextDouble() * rand.NextDouble());
			return offset + multiplier * (avg + num * var);
		}
		case EnumDistribution.STRONGINVEXP:
		{
			float num = (float)(rand.NextDouble() * rand.NextDouble() * rand.NextDouble());
			return offset + multiplier * (avg + num * var);
		}
		case EnumDistribution.STRONGERINVEXP:
		{
			float num = (float)(rand.NextDouble() * rand.NextDouble() * rand.NextDouble() * rand.NextDouble());
			return offset + multiplier * (avg + num * var);
		}
		case EnumDistribution.INVERSEGAUSSIAN:
		{
			float num = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 3f;
			num = ((!(num > 0.5f)) ? (num + 0.5f) : (num - 0.5f));
			num -= 0.5f;
			return offset + multiplier * (avg + 2f * num * var);
		}
		case EnumDistribution.NARROWINVERSEGAUSSIAN:
		{
			float num = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 6f;
			num = ((!(num > 0.5f)) ? (num + 0.5f) : (num - 0.5f));
			num -= 0.5f;
			return offset + multiplier * (avg + 2f * num * var);
		}
		case EnumDistribution.DIRAC:
		{
			float num = (float)rand.NextDouble() - 0.5f;
			float result = offset + multiplier * (avg + num * 2f * var);
			avg = 0f;
			var = 0f;
			return result;
		}
		case EnumDistribution.TRIANGLE:
		{
			float num = (float)(rand.NextDouble() + rand.NextDouble()) / 2f;
			num -= 0.5f;
			return offset + multiplier * (avg + num * 2f * var);
		}
		default:
			return 0f;
		}
	}

	public float nextFloat(float multiplier, IRandom rand)
	{
		switch (dist)
		{
		case EnumDistribution.UNIFORM:
		{
			float num = rand.NextFloat() - 0.5f;
			return offset + multiplier * (avg + num * 2f * var);
		}
		case EnumDistribution.GAUSSIAN:
		{
			float num = (rand.NextFloat() + rand.NextFloat() + rand.NextFloat()) / 3f;
			num -= 0.5f;
			return offset + multiplier * (avg + num * 2f * var);
		}
		case EnumDistribution.NARROWGAUSSIAN:
		{
			float num = (rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat()) / 6f;
			num -= 0.5f;
			return offset + multiplier * (avg + num * 2f * var);
		}
		case EnumDistribution.INVEXP:
		{
			float num = rand.NextFloat() * rand.NextFloat();
			return offset + multiplier * (avg + num * var);
		}
		case EnumDistribution.STRONGINVEXP:
		{
			float num = rand.NextFloat() * rand.NextFloat() * rand.NextFloat();
			return offset + multiplier * (avg + num * var);
		}
		case EnumDistribution.STRONGERINVEXP:
		{
			float num = rand.NextFloat() * rand.NextFloat() * rand.NextFloat() * rand.NextFloat();
			return offset + multiplier * (avg + num * var);
		}
		case EnumDistribution.INVERSEGAUSSIAN:
		{
			float num = (rand.NextFloat() + rand.NextFloat() + rand.NextFloat()) / 3f;
			num = ((!(num > 0.5f)) ? (num + 0.5f) : (num - 0.5f));
			num -= 0.5f;
			return offset + multiplier * (avg + 2f * num * var);
		}
		case EnumDistribution.NARROWINVERSEGAUSSIAN:
		{
			float num = (rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat()) / 6f;
			num = ((!(num > 0.5f)) ? (num + 0.5f) : (num - 0.5f));
			num -= 0.5f;
			return offset + multiplier * (avg + 2f * num * var);
		}
		case EnumDistribution.DIRAC:
		{
			float num = rand.NextFloat() - 0.5f;
			float result = offset + multiplier * (avg + num * 2f * var);
			avg = 0f;
			var = 0f;
			return result;
		}
		case EnumDistribution.TRIANGLE:
		{
			float num = (rand.NextFloat() + rand.NextFloat()) / 2f;
			num -= 0.5f;
			return offset + multiplier * (avg + num * 2f * var);
		}
		default:
			return 0f;
		}
	}

	public float ClampToRange(float value)
	{
		float val = avg - var;
		float val2 = avg + var;
		EnumDistribution enumDistribution = dist;
		if ((uint)(enumDistribution - 6) <= 2u)
		{
			val = avg;
		}
		return GameMath.Clamp(value, Math.Min(val, val2), Math.Max(val, val2));
	}

	public static NatFloat createFromBytes(BinaryReader reader)
	{
		NatFloat zero = Zero;
		zero.FromBytes(reader);
		return zero;
	}

	public NatFloat Clone()
	{
		return (NatFloat)MemberwiseClone();
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(offset);
		writer.Write(avg);
		writer.Write(var);
		writer.Write((byte)dist);
	}

	public void FromBytes(BinaryReader reader)
	{
		offset = reader.ReadSingle();
		avg = reader.ReadSingle();
		var = reader.ReadSingle();
		dist = (EnumDistribution)reader.ReadByte();
	}
}
