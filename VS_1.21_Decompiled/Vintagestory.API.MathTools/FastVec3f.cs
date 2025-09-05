using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Vintagestory.API.MathTools;

public struct FastVec3f
{
	public float X;

	public float Y;

	public float Z;

	public float R
	{
		get
		{
			return X;
		}
		set
		{
			X = value;
		}
	}

	public float G
	{
		get
		{
			return Y;
		}
		set
		{
			Y = value;
		}
	}

	public float B
	{
		get
		{
			return Z;
		}
		set
		{
			Z = value;
		}
	}

	public float this[int index]
	{
		get
		{
			return (float)((2 - index) / 2) * X + (float)(index % 2) * Y + (float)(index / 2) * Z;
		}
		set
		{
			switch (index)
			{
			case 0:
				X = value;
				break;
			case 1:
				Y = value;
				break;
			default:
				Z = value;
				break;
			}
		}
	}

	public FastVec3f(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public FastVec3f(Vec4f vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	public FastVec3f(float[] values)
	{
		X = values[0];
		Y = values[1];
		Z = values[2];
	}

	public FastVec3f(Vec3i vec3i)
	{
		X = vec3i.X;
		Y = vec3i.Y;
		Z = vec3i.Z;
	}

	public FastVec3f(Vec3f vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	public float Length()
	{
		return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
	}

	public void Negate()
	{
		X = 0f - X;
		Y = 0f - Y;
		Z = 0f - Z;
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		if (obj is FastVec3f fastVec3f && fastVec3f.X == X && fastVec3f.Y == Y && fastVec3f.Z == Z)
		{
			return true;
		}
		return false;
	}

	public float Dot(Vec3f a)
	{
		return X * a.X + Y * a.Y + Z * a.Z;
	}

	public float Dot(Vec3d a)
	{
		return (float)((double)X * a.X + (double)Y * a.Y + (double)Z * a.Z);
	}

	public double Dot(float[] pos)
	{
		return X * pos[0] + Y * pos[1] + Z * pos[2];
	}

	public double Dot(double[] pos)
	{
		return (float)((double)X * pos[0] + (double)Y * pos[1] + (double)Z * pos[2]);
	}

	public double[] ToDoubleArray()
	{
		return new double[3] { X, Y, Z };
	}

	public FastVec3f Add(float x, float y, float z)
	{
		X += x;
		Y += y;
		Z += z;
		return this;
	}

	public FastVec3f Mul(float multiplier)
	{
		X *= multiplier;
		Y *= multiplier;
		Z *= multiplier;
		return this;
	}

	public FastVec3f Clone()
	{
		return (FastVec3f)MemberwiseClone();
	}

	public FastVec3f Normalize()
	{
		float num = Length();
		if (num > 0f)
		{
			X /= num;
			Y /= num;
			Z /= num;
		}
		return this;
	}

	public float Distance(FastVec3f vec)
	{
		return (float)Math.Sqrt((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z));
	}

	public double DistanceSq(double x, double y, double z)
	{
		return ((double)X - x) * ((double)X - x) + ((double)Y - y) * ((double)Y - y) + ((double)Z - z) * ((double)Z - z);
	}

	public float Distance(Vec3d vec)
	{
		return (float)Math.Sqrt(((double)X - vec.X) * ((double)X - vec.X) + ((double)Y - vec.Y) * ((double)Y - vec.Y) + ((double)Z - vec.Z) * ((double)Z - vec.Z));
	}

	public FastVec3f AddCopy(float x, float y, float z)
	{
		return new FastVec3f(X + x, Y + y, Z + z);
	}

	public FastVec3f AddCopy(FastVec3f vec)
	{
		return new FastVec3f(X + vec.X, Y + vec.Y, Z + vec.Z);
	}

	public void ReduceBy(float val)
	{
		X = ((X > 0f) ? Math.Max(0f, X - val) : Math.Min(0f, X + val));
		Y = ((Y > 0f) ? Math.Max(0f, Y - val) : Math.Min(0f, Y + val));
		Z = ((Z > 0f) ? Math.Max(0f, Z - val) : Math.Min(0f, Z + val));
	}

	public FastVec3f NormalizedCopy()
	{
		float num = Length();
		return new FastVec3f(X / num, Y / num, Z / num);
	}

	public Vec3d ToVec3d()
	{
		return new Vec3d(X, Y, Z);
	}

	public FastVec3f Set(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
		return this;
	}

	public FastVec3f Set(Vec3d vec)
	{
		X = (float)vec.X;
		Y = (float)vec.Y;
		Z = (float)vec.Z;
		return this;
	}

	public FastVec3f Set(float[] vec)
	{
		X = vec[0];
		Y = vec[1];
		Z = vec[2];
		return this;
	}

	public void Set(FastVec3f vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	public override string ToString()
	{
		return "x=" + X + ", y=" + Y + ", z=" + Z;
	}

	public void Write(BinaryWriter writer)
	{
		writer.Write(X);
		writer.Write(Y);
		writer.Write(Z);
	}

	public static FastVec3f CreateFromBytes(BinaryReader reader)
	{
		return new FastVec3f(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
	}

	public static FastVec3f operator +(FastVec3f left, float right)
	{
		return new FastVec3f(left.X + right, left.Y + right, left.Z + right);
	}

	public static FastVec3f operator -(FastVec3f left, float right)
	{
		return new FastVec3f(left.X - right, left.Y - right, left.Z - right);
	}

	public static FastVec3f operator *(FastVec3f left, float right)
	{
		return new FastVec3f(left.X * right, left.Y * right, left.Z * right);
	}
}
