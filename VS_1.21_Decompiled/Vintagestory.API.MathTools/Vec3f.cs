using System;
using System.IO;
using Newtonsoft.Json;
using ProtoBuf;
using Vintagestory.API.Client;

namespace Vintagestory.API.MathTools;

[JsonObject(/*Could not decode attribute arguments.*/)]
[ProtoContract]
public class Vec3f : IVec3, IEquatable<Vec3f>
{
	[JsonProperty]
	[ProtoMember(1)]
	public float X;

	[JsonProperty]
	[ProtoMember(2)]
	public float Y;

	[JsonProperty]
	[ProtoMember(3)]
	public float Z;

	public static Vec3f Zero => new Vec3f();

	public static Vec3f Half => new Vec3f(0.5f, 0.5f, 0.5f);

	public static Vec3f One => new Vec3f(1f, 1f, 1f);

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

	public bool IsZero
	{
		get
		{
			if (X == 0f && Y == 0f)
			{
				return Z == 0f;
			}
			return false;
		}
	}

	public float this[int index]
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

	int IVec3.XAsInt => (int)X;

	int IVec3.YAsInt => (int)Y;

	int IVec3.ZAsInt => (int)Z;

	double IVec3.XAsDouble => X;

	double IVec3.YAsDouble => Y;

	double IVec3.ZAsDouble => Z;

	float IVec3.XAsFloat => X;

	float IVec3.YAsFloat => Y;

	float IVec3.ZAsFloat => Z;

	public Vec3i AsVec3i => new Vec3i((int)X, (int)Y, (int)Z);

	public Vec3f()
	{
	}

	public static implicit operator FastVec3f(Vec3f a)
	{
		return new FastVec3f(a);
	}

	public Vec3f(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public Vec3f(Vec4f vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	public Vec3f(float[] values)
	{
		X = values[0];
		Y = values[1];
		Z = values[2];
	}

	public Vec3f(Vec3i vec3i)
	{
		X = vec3i.X;
		Y = vec3i.Y;
		Z = vec3i.Z;
	}

	public Vec3f(float xyz)
	{
		X = xyz;
		Y = xyz;
		Z = xyz;
	}

	public float Length()
	{
		return GameMath.RootSumOfSquares(X, Y, Z);
	}

	public void Negate()
	{
		X = 0f - X;
		Y = 0f - Y;
		Z = 0f - Z;
	}

	public Vec3f RotatedCopy(float yaw)
	{
		Matrixf matrixf = new Matrixf();
		matrixf.RotateYDeg(yaw);
		return matrixf.TransformVector(new Vec4f(X, Y, Z, 0f)).XYZ;
	}

	public float Dot(Vec3f a)
	{
		return X * a.X + Y * a.Y + Z * a.Z;
	}

	public float Dot(FastVec3f a)
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

	public Vec3f Cross(Vec3f vec)
	{
		return new Vec3f
		{
			X = Y * vec.Z - Z * vec.Y,
			Y = Z * vec.X - X * vec.Z,
			Z = X * vec.Y - Y * vec.X
		};
	}

	public double[] ToDoubleArray()
	{
		return new double[3] { X, Y, Z };
	}

	public void Cross(Vec3f a, Vec3f b)
	{
		X = a.Y * b.Z - a.Z * b.Y;
		Y = a.Z * b.X - a.X * b.Z;
		Z = a.X * b.Y - a.Y * b.X;
	}

	public void Cross(Vec3f a, Vec4f b)
	{
		X = a.Y * b.Z - a.Z * b.Y;
		Y = a.Z * b.X - a.X * b.Z;
		Z = a.X * b.Y - a.Y * b.X;
	}

	public Vec3f Add(float x, float y, float z)
	{
		X += x;
		Y += y;
		Z += z;
		return this;
	}

	public Vec3f Add(Vec3f vec)
	{
		X += vec.X;
		Y += vec.Y;
		Z += vec.Z;
		return this;
	}

	public Vec3f Add(Vec3d vec)
	{
		X += (float)vec.X;
		Y += (float)vec.Y;
		Z += (float)vec.Z;
		return this;
	}

	public Vec3f Sub(Vec3f vec)
	{
		X -= vec.X;
		Y -= vec.Y;
		Z -= vec.Z;
		return this;
	}

	public Vec3f Sub(Vec3d vec)
	{
		X -= (float)vec.X;
		Y -= (float)vec.Y;
		Z -= (float)vec.Z;
		return this;
	}

	public Vec3f Sub(Vec3i vec)
	{
		X -= vec.X;
		Y -= vec.Y;
		Z -= vec.Z;
		return this;
	}

	public Vec3f Mul(float multiplier)
	{
		X *= multiplier;
		Y *= multiplier;
		Z *= multiplier;
		return this;
	}

	public Vec3f Clone()
	{
		return new Vec3f(X, Y, Z);
	}

	public Vec3f Normalize()
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

	public double DistanceSq(double x, double y, double z)
	{
		return ((double)X - x) * ((double)X - x) + ((double)Y - y) * ((double)Y - y) + ((double)Z - z) * ((double)Z - z);
	}

	public float DistanceTo(Vec3d vec)
	{
		return (float)Math.Sqrt(((double)X - vec.X) * ((double)X - vec.X) + ((double)Y - vec.Y) * ((double)Y - vec.Y) + ((double)Z - vec.Z) * ((double)Z - vec.Z));
	}

	public float DistanceTo(Vec3f vec)
	{
		return (float)Math.Sqrt((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z));
	}

	public Vec3f AddCopy(float x, float y, float z)
	{
		return new Vec3f(X + x, Y + y, Z + z);
	}

	public Vec3f AddCopy(Vec3f vec)
	{
		return new Vec3f(X + vec.X, Y + vec.Y, Z + vec.Z);
	}

	public void ReduceBy(float val)
	{
		X = ((X > 0f) ? Math.Max(0f, X - val) : Math.Min(0f, X + val));
		Y = ((Y > 0f) ? Math.Max(0f, Y - val) : Math.Min(0f, Y + val));
		Z = ((Z > 0f) ? Math.Max(0f, Z - val) : Math.Min(0f, Z + val));
	}

	public Vec3f NormalizedCopy()
	{
		float num = Length();
		return new Vec3f(X / num, Y / num, Z / num);
	}

	public Vec3d ToVec3d()
	{
		return new Vec3d(X, Y, Z);
	}

	public static Vec3f operator -(Vec3f left, Vec3f right)
	{
		return new Vec3f(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
	}

	public static Vec3f operator +(Vec3f left, Vec3f right)
	{
		return new Vec3f(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
	}

	public static Vec3f operator -(Vec3f left, float right)
	{
		return new Vec3f(left.X - right, left.Y - right, left.Z - right);
	}

	public static Vec3f operator -(float left, Vec3f right)
	{
		return new Vec3f(left - right.X, left - right.Y, left - right.Z);
	}

	public static Vec3f operator +(Vec3f left, float right)
	{
		return new Vec3f(left.X + right, left.Y + right, left.Z + right);
	}

	public static Vec3f operator *(Vec3f left, float right)
	{
		return new Vec3f(left.X * right, left.Y * right, left.Z * right);
	}

	public static Vec3f operator *(float left, Vec3f right)
	{
		return new Vec3f(left * right.X, left * right.Y, left * right.Z);
	}

	public static float operator *(Vec3f left, Vec3f right)
	{
		return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
	}

	public static Vec3f operator /(Vec3f left, float right)
	{
		return new Vec3f(left.X / right, left.Y / right, left.Z / right);
	}

	public static bool operator ==(Vec3f left, Vec3f right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(Vec3f left, Vec3f right)
	{
		return !(left == right);
	}

	public Vec3f Set(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
		return this;
	}

	public Vec3f Set(Vec3d vec)
	{
		X = (float)vec.X;
		Y = (float)vec.Y;
		Z = (float)vec.Z;
		return this;
	}

	public Vec3f Set(float[] vec)
	{
		X = vec[0];
		Y = vec[1];
		Z = vec[2];
		return this;
	}

	public Vec3f Set(Vec3f vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
		return this;
	}

	public override string ToString()
	{
		return "x=" + X + ", y=" + Y + ", z=" + Z;
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(X);
		writer.Write(Y);
		writer.Write(Z);
	}

	public static Vec3f CreateFromBytes(BinaryReader reader)
	{
		return new Vec3f(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
	}

	public Vec4f ToVec4f(float w)
	{
		return new Vec4f(X, Y, Z, w);
	}

	public bool Equals(Vec3f other, double epsilon)
	{
		if ((double)Math.Abs(X - other.X) < epsilon && (double)Math.Abs(Y - other.Y) < epsilon)
		{
			return (double)Math.Abs(Z - other.Z) < epsilon;
		}
		return false;
	}

	public bool Equals(Vec3f other)
	{
		if (other != null && X == other.X && Y == other.Y)
		{
			return Z == other.Z;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is Vec3f vec3f)
		{
			if (vec3f != null && X == vec3f.X && Y == vec3f.Y)
			{
				return Z == vec3f.Z;
			}
			return false;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((391 + X.GetHashCode()) * 23 + Y.GetHashCode()) * 23 + Z.GetHashCode();
	}
}
