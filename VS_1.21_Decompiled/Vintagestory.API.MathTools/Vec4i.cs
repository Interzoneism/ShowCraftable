using System;
using ProtoBuf;

namespace Vintagestory.API.MathTools;

[ProtoContract]
public class Vec4i : IEquatable<Vec4i>
{
	[ProtoMember(1)]
	public int X;

	[ProtoMember(2)]
	public int Y;

	[ProtoMember(3)]
	public int Z;

	[ProtoMember(4)]
	public int W;

	public Vec4i()
	{
	}

	public Vec4i(BlockPos pos, int w)
	{
		X = pos.X;
		Y = pos.InternalY;
		Z = pos.Z;
		W = w;
	}

	public Vec4i(int x, int y, int z, int w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public bool Equals(Vec4i other)
	{
		if (other != null && other.X == X && other.Y == Y && other.Z == Z)
		{
			return other.W == W;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((17 * 23 + X.GetHashCode()) * 23 + Y.GetHashCode()) * 23 + Z.GetHashCode()) * 23 + W.GetHashCode();
	}

	public float HorDistanceSqTo(double x, double z)
	{
		double num = x - (double)X;
		double num2 = z - (double)Z;
		return (float)(num * num + num2 * num2);
	}
}
public class Vec4i<T>
{
	public int X;

	public int Y;

	public int Z;

	public T Value;

	public Vec4i()
	{
	}

	public Vec4i(int x, int y, int z, T Value)
	{
		X = x;
		Y = y;
		Z = z;
		this.Value = Value;
	}
}
