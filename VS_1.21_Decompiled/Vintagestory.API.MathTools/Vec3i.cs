using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using ProtoBuf;

namespace Vintagestory.API.MathTools;

[ProtoContract]
[JsonObject(/*Could not decode attribute arguments.*/)]
public class Vec3i : IEquatable<Vec3i>, IVec3
{
	[ProtoMember(1)]
	[JsonProperty]
	public int X;

	[ProtoMember(2)]
	[JsonProperty]
	public int Y;

	[ProtoMember(3)]
	[JsonProperty]
	public int Z;

	public static readonly Vec3i[] DirectAndIndirectNeighbours;

	public bool IsZero
	{
		get
		{
			if (X == 0 && Y == 0)
			{
				return Z == 0;
			}
			return false;
		}
	}

	public int this[int index]
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

	public BlockPos AsBlockPos => new BlockPos(X, Y, Z);

	int IVec3.XAsInt => X;

	int IVec3.YAsInt => Y;

	int IVec3.ZAsInt => Z;

	double IVec3.XAsDouble => X;

	double IVec3.YAsDouble => Y;

	double IVec3.ZAsDouble => Z;

	float IVec3.XAsFloat => X;

	float IVec3.YAsFloat => Y;

	float IVec3.ZAsFloat => Z;

	public static Vec3i Zero => new Vec3i(0, 0, 0);

	public Vec2i XZ => new Vec2i(X, Z);

	public Vec3i AsVec3i => new Vec3i(X, Y, Z);

	static Vec3i()
	{
		List<Vec3i> list = new List<Vec3i>();
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					if (i != 0 || j != 0 || k != 0)
					{
						list.Add(new Vec3i(i, j, k));
					}
				}
			}
		}
		DirectAndIndirectNeighbours = list.ToArray();
	}

	public Vec3i()
	{
	}

	public Vec3i(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public Vec3i(FastVec3i vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	public Vec3i(BlockPos pos)
	{
		X = pos.X;
		Y = pos.Y;
		Z = pos.Z;
	}

	public Vec3i Add(int x, int y, int z)
	{
		X += x;
		Y += y;
		Z += z;
		return this;
	}

	public Vec3i AddCopy(int x, int y, int z)
	{
		return new Vec3i(X + x, Y + y, Z + z);
	}

	public Vec3i Add(int x, int y, int z, Vec3i intoVec)
	{
		intoVec.X = X + x;
		intoVec.Y = Y + y;
		intoVec.Z = Z + z;
		return this;
	}

	public Vec3i Add(BlockFacing towardsFace, int length = 1)
	{
		X += towardsFace.Normali.X * length;
		Y += towardsFace.Normali.Y * length;
		Z += towardsFace.Normali.Z * length;
		return this;
	}

	public int ManhattenDistanceTo(Vec3i vec)
	{
		return Math.Abs(X - vec.X) + Math.Abs(Y - vec.Y) + Math.Abs(Z - vec.Z);
	}

	public long SquareDistanceTo(Vec3i vec)
	{
		long num = X - vec.X;
		long num2 = Y - vec.Y;
		long num3 = Z - vec.Z;
		return num * num + num2 * num2 + num3 * num3;
	}

	public long SquareDistanceTo(int x, int y, int z)
	{
		long num = X - x;
		long num2 = Y - y;
		long num3 = Z - z;
		return num * num + num2 * num2 + num3 * num3;
	}

	public double DistanceTo(Vec3i vec)
	{
		long num = X - vec.X;
		long num2 = Y - vec.Y;
		long num3 = Z - vec.Z;
		return Math.Sqrt(num * num + num2 * num2 + num3 * num3);
	}

	public void Reduce(int val = 1)
	{
		X = ((X > 0) ? Math.Max(0, X - val) : Math.Min(0, X + val));
		Y = ((Y > 0) ? Math.Max(0, Y - val) : Math.Min(0, Y + val));
		Z = ((Z > 0) ? Math.Max(0, Z - val) : Math.Min(0, Z + val));
	}

	public void ReduceX(int val = 1)
	{
		X = ((X > 0) ? Math.Max(0, X - val) : Math.Min(0, X + val));
	}

	public void ReduceY(int val = 1)
	{
		Y = ((Y > 0) ? Math.Max(0, Y - val) : Math.Min(0, Y + val));
	}

	public void ReduceZ(int val = 1)
	{
		Z = ((Z > 0) ? Math.Max(0, Z - val) : Math.Min(0, Z + val));
	}

	public Vec3i Set(int positionX, int positionY, int positionZ)
	{
		X = positionX;
		Y = positionY;
		Z = positionZ;
		return this;
	}

	public Vec3i Set(Vec3i fromPos)
	{
		X = fromPos.X;
		Y = fromPos.Y;
		Z = fromPos.Z;
		return this;
	}

	internal void Offset(BlockFacing face)
	{
		X += face.Normali.X;
		Y += face.Normali.Y;
		Z += face.Normali.Z;
	}

	public Vec3i Clone()
	{
		return (Vec3i)MemberwiseClone();
	}

	public override bool Equals(object obj)
	{
		if (obj is Vec3i)
		{
			Vec3i vec3i = (Vec3i)obj;
			if (X == vec3i.X && Y == vec3i.Y)
			{
				return Z == vec3i.Z;
			}
			return false;
		}
		return false;
	}

	public bool Equals(Vec3i other)
	{
		if (other != null && X == other.X && Y == other.Y)
		{
			return Z == other.Z;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((391 + X) * 23 + Y) * 23 + Z;
	}

	public override string ToString()
	{
		return "X=" + X + ",Y=" + Y + ",Z=" + Z;
	}

	internal float[] ToFloats()
	{
		return new float[3] { X, Y, Z };
	}

	public Vec3i AddCopy(BlockFacing facing)
	{
		return new Vec3i(X + facing.Normali.X, Y + facing.Normali.Y, Z + facing.Normali.Z);
	}

	public BlockPos ToBlockPos()
	{
		return new BlockPos
		{
			X = X,
			Y = Y,
			Z = Z
		};
	}

	public bool Equals(int x, int y, int z)
	{
		if (X == x && Y == y)
		{
			return Z == z;
		}
		return false;
	}

	public static Vec3i operator *(Vec3i left, int right)
	{
		return new Vec3i(left.X * right, left.Y * right, left.Z * right);
	}

	public static Vec3i operator *(int left, Vec3i right)
	{
		return new Vec3i(left * right.X, left * right.Y, left * right.Z);
	}

	public static Vec3i operator /(Vec3i left, int right)
	{
		return new Vec3i(left.X / right, left.Y / right, left.Z / right);
	}

	public static Vec3i operator +(Vec3i left, Vec3i right)
	{
		return new Vec3i(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
	}

	public static Vec3i operator -(Vec3i left, Vec3i right)
	{
		return new Vec3i(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
	}

	public static Vec3i operator -(Vec3i vec)
	{
		return new Vec3i(-vec.X, -vec.Y, -vec.Z);
	}

	public static bool operator ==(Vec3i left, Vec3i right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(Vec3i left, Vec3i right)
	{
		return !(left == right);
	}
}
