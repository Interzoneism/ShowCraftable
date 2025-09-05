using System;
using System.IO;

namespace Vintagestory.API.MathTools;

public struct FastVec3d
{
	public double X;

	public double Y;

	public double Z;

	public double this[int index]
	{
		get
		{
			return index switch
			{
				1 => Y, 
				0 => X, 
				_ => Z, 
			};
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

	public FastVec3d(double x, double y, double z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public FastVec3d(Vec4d vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	public FastVec3d(double[] values)
	{
		X = values[0];
		Y = values[1];
		Z = values[2];
	}

	public FastVec3d(Vec3i vec3i)
	{
		X = vec3i.X;
		Y = vec3i.Y;
		Z = vec3i.Z;
	}

	public FastVec3d(BlockPos pos)
	{
		X = pos.X;
		Y = pos.Y;
		Z = pos.Z;
	}

	public double Length()
	{
		return Math.Sqrt(X * X + Y * Y + Z * Z);
	}

	public void Negate()
	{
		X = 0.0 - X;
		Y = 0.0 - Y;
		Z = 0.0 - Z;
	}

	public double Dot(Vec3f a)
	{
		return X * (double)a.X + Y * (double)a.Y + Z * (double)a.Z;
	}

	public double Dot(Vec3d a)
	{
		return X * a.X + Y * a.Y + Z * a.Z;
	}

	public double Dot(float[] pos)
	{
		return X * (double)pos[0] + Y * (double)pos[1] + Z * (double)pos[2];
	}

	public double Dot(double[] pos)
	{
		return X * pos[0] + Y * pos[1] + Z * pos[2];
	}

	public double[] ToDoubleArray()
	{
		return new double[3] { X, Y, Z };
	}

	public FastVec3d Add(double x, double y, double z)
	{
		X += x;
		Y += y;
		Z += z;
		return this;
	}

	public FastVec3d Add(double d)
	{
		X += d;
		Y += d;
		Z += d;
		return this;
	}

	public FastVec3d Add(Vec3i vec)
	{
		X += vec.X;
		Y += vec.Y;
		Z += vec.Z;
		return this;
	}

	public FastVec3d Add(BlockPos pos)
	{
		X += pos.X;
		Y += pos.Y;
		Z += pos.Z;
		return this;
	}

	public FastVec3d Mul(double multiplier)
	{
		X *= multiplier;
		Y *= multiplier;
		Z *= multiplier;
		return this;
	}

	public FastVec3d Clone()
	{
		return (FastVec3d)MemberwiseClone();
	}

	public FastVec3d Normalize()
	{
		double num = Length();
		if (num > 0.0)
		{
			X /= num;
			Y /= num;
			Z /= num;
		}
		return this;
	}

	public double Distance(FastVec3d vec)
	{
		return Math.Sqrt((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z));
	}

	public double DistanceSq(double x, double y, double z)
	{
		return (X - x) * (X - x) + (Y - y) * (Y - y) + (Z - z) * (Z - z);
	}

	public double Distance(Vec3d vec)
	{
		return Math.Sqrt((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z));
	}

	public FastVec3d AddCopy(double x, double y, double z)
	{
		return new FastVec3d(X + x, Y + y, Z + z);
	}

	public FastVec3d AddCopy(FastVec3d vec)
	{
		return new FastVec3d(X + vec.X, Y + vec.Y, Z + vec.Z);
	}

	public void ReduceBy(double val)
	{
		X = ((X > 0.0) ? Math.Max(0.0, X - val) : Math.Min(0.0, X + val));
		Y = ((Y > 0.0) ? Math.Max(0.0, Y - val) : Math.Min(0.0, Y + val));
		Z = ((Z > 0.0) ? Math.Max(0.0, Z - val) : Math.Min(0.0, Z + val));
	}

	public FastVec3d NormalizedCopy()
	{
		double num = Length();
		return new FastVec3d(X / num, Y / num, Z / num);
	}

	public Vec3d ToVec3d()
	{
		return new Vec3d(X, Y, Z);
	}

	public FastVec3d Set(double x, double y, double z)
	{
		X = x;
		Y = y;
		Z = z;
		return this;
	}

	public FastVec3d Set(Vec3d vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
		return this;
	}

	public FastVec3d Set(double[] vec)
	{
		X = vec[0];
		Y = vec[1];
		Z = vec[2];
		return this;
	}

	public void Set(FastVec3d vec)
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

	public static FastVec3d CreateFromBytes(BinaryReader reader)
	{
		return new FastVec3d(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
	}
}
