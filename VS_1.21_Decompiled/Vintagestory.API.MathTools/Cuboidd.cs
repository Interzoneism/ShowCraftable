using System;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.MathTools;

public class Cuboidd : ICuboid<double, Cuboidd>, IEquatable<Cuboidd>
{
	private const double epsilon = 1.6E-05;

	public double X1;

	public double Y1;

	public double Z1;

	public double X2;

	public double Y2;

	public double Z2;

	public double Width => MaxX - MinX;

	public double Height => MaxY - MinY;

	public double Length => MaxZ - MinZ;

	public double MinX => Math.Min(X1, X2);

	public double MinY => Math.Min(Y1, Y2);

	public double MinZ => Math.Min(Z1, Z2);

	public double MaxX => Math.Max(X1, X2);

	public double MaxY => Math.Max(Y1, Y2);

	public double MaxZ => Math.Max(Z1, Z2);

	public Vec3d Start => new Vec3d(MinX, MinY, MinZ);

	public Vec3d End => new Vec3d(MaxX, MaxY, MaxZ);

	public Cuboidd()
	{
	}

	public Cuboidd(double x1, double y1, double z1, double x2, double y2, double z2)
	{
		Set(x1, y1, z1, x2, y2, z2);
	}

	public Cuboidd(Vec3d start, Vec3d end)
	{
		X1 = start.X;
		Y1 = start.Y;
		Z1 = start.Z;
		X2 = end.X;
		Y2 = end.Y;
		Z2 = end.Z;
	}

	public Cuboidd Set(double x1, double y1, double z1, double x2, double y2, double z2)
	{
		X1 = x1;
		Y1 = y1;
		Z1 = z1;
		X2 = x2;
		Y2 = y2;
		Z2 = z2;
		return this;
	}

	public Cuboidd Set(IVec3 min, IVec3 max)
	{
		Set(min.XAsDouble, min.YAsDouble, min.ZAsDouble, max.XAsDouble, max.YAsDouble, max.ZAsDouble);
		return this;
	}

	public Cuboidd Set(Cuboidf selectionBox)
	{
		X1 = selectionBox.X1;
		Y1 = selectionBox.Y1;
		Z1 = selectionBox.Z1;
		X2 = selectionBox.X2;
		Y2 = selectionBox.Y2;
		Z2 = selectionBox.Z2;
		return this;
	}

	public void Set(Cuboidd other)
	{
		X1 = other.X1;
		Y1 = other.Y1;
		Z1 = other.Z1;
		X2 = other.X2;
		Y2 = other.Y2;
		Z2 = other.Z2;
	}

	public Cuboidd SetAndTranslate(Cuboidf selectionBox, Vec3d vec)
	{
		X1 = (double)selectionBox.X1 + vec.X;
		Y1 = (double)selectionBox.Y1 + vec.Y;
		Z1 = (double)selectionBox.Z1 + vec.Z;
		X2 = (double)selectionBox.X2 + vec.X;
		Y2 = (double)selectionBox.Y2 + vec.Y;
		Z2 = (double)selectionBox.Z2 + vec.Z;
		return this;
	}

	public Cuboidd SetAndTranslate(Cuboidf selectionBox, double dX, double dY, double dZ)
	{
		X1 = (double)selectionBox.X1 + dX;
		Y1 = (double)selectionBox.Y1 + dY;
		Z1 = (double)selectionBox.Z1 + dZ;
		X2 = (double)selectionBox.X2 + dX;
		Y2 = (double)selectionBox.Y2 + dY;
		Z2 = (double)selectionBox.Z2 + dZ;
		return this;
	}

	public void RemoveRoundingErrors()
	{
		double num = X1 * 16.0;
		double num2 = Z1 * 16.0;
		double num3 = X2 * 16.0;
		double num4 = Z2 * 16.0;
		if (Math.Ceiling(num) - num < 1.6E-05)
		{
			X1 = Math.Ceiling(num) / 16.0;
		}
		if (Math.Ceiling(num2) - num2 < 1.6E-05)
		{
			Z1 = Math.Ceiling(num2) / 16.0;
		}
		if (num3 - Math.Floor(num3) < 1.6E-05)
		{
			X2 = Math.Floor(num3) / 16.0;
		}
		if (num4 - Math.Floor(num4) < 1.6E-05)
		{
			Z2 = Math.Floor(num4) / 16.0;
		}
	}

	public Cuboidd Translate(IVec3 vec)
	{
		Translate(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble);
		return this;
	}

	public Cuboidd Translate(double posX, double posY, double posZ)
	{
		X1 += posX;
		Y1 += posY;
		Z1 += posZ;
		X2 += posX;
		Y2 += posY;
		Z2 += posZ;
		return this;
	}

	public Cuboidd GrowBy(double dx, double dy, double dz)
	{
		X1 -= dx;
		Y1 -= dy;
		Z1 -= dz;
		X2 += dx;
		Y2 += dy;
		Z2 += dz;
		return this;
	}

	public bool ContainsOrTouches(double x, double y, double z)
	{
		if (x >= X1 && x <= X2 && y >= Y1 && y <= Y2 && z >= Z1)
		{
			return z <= Z2;
		}
		return false;
	}

	public bool Contains(double x, double y, double z)
	{
		if (x > X1 && x < X2 && y > Y1 && y < Y2 && z > Z1)
		{
			return z < Z2;
		}
		return false;
	}

	public bool ContainsOrTouches(IVec3 vec)
	{
		return ContainsOrTouches(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble);
	}

	public Cuboidd GrowToInclude(int x, int y, int z)
	{
		X1 = Math.Min(X1, x);
		Y1 = Math.Min(Y1, y);
		Z1 = Math.Min(Z1, z);
		X2 = Math.Max(X2, x + 1);
		Y2 = Math.Max(Y2, y + 1);
		Z2 = Math.Max(Z2, z + 1);
		return this;
	}

	public Cuboidd GrowToInclude(IVec3 vec)
	{
		GrowToInclude(vec.XAsInt, vec.YAsInt, vec.ZAsInt);
		return this;
	}

	public double ShortestDistanceFrom(double x, double y, double z)
	{
		double num = x - GameMath.Clamp(x, X1, X2);
		double num2 = y - GameMath.Clamp(y, Y1, Y2);
		double num3 = z - GameMath.Clamp(z, Z1, Z2);
		return Math.Sqrt(num * num + num2 * num2 + num3 * num3);
	}

	public Cuboidi ToCuboidi()
	{
		return new Cuboidi((int)X1, (int)Y1, (int)Z1, (int)X2, (int)Y2, (int)Z2);
	}

	public double ShortestVerticalDistanceFrom(double y)
	{
		return y - GameMath.Clamp(y, Y1, Y2);
	}

	public double ShortestVerticalDistanceFrom(Cuboidd cuboid)
	{
		double val = cuboid.Y1 - GameMath.Clamp(cuboid.Y1, Y1, Y2);
		double val2 = cuboid.Y2 - GameMath.Clamp(cuboid.Y2, Y1, Y2);
		return Math.Min(val, val2);
	}

	public double ShortestVerticalDistanceFrom(Cuboidf cuboid, EntityPos offset)
	{
		double num = offset.Y + (double)cuboid.Y1;
		double num2 = offset.Y + (double)cuboid.Y2;
		double val = num - GameMath.Clamp(num, Y1, Y2);
		if (num <= Y1 && num2 >= Y2)
		{
			val = 0.0;
		}
		double val2 = num2 - GameMath.Clamp(num2, Y1, Y2);
		return Math.Min(val, val2);
	}

	public double ShortestDistanceFrom(Cuboidd cuboid)
	{
		double num = cuboid.X1 - GameMath.Clamp(cuboid.X1, X1, X2);
		double num2 = cuboid.Y1 - GameMath.Clamp(cuboid.Y1, Y1, Y2);
		double num3 = cuboid.Z1 - GameMath.Clamp(cuboid.Z1, Z1, Z2);
		double num4 = cuboid.X2 - GameMath.Clamp(cuboid.X2, X1, X2);
		double num5 = cuboid.Y2 - GameMath.Clamp(cuboid.Y2, Y1, Y2);
		double num6 = cuboid.Z2 - GameMath.Clamp(cuboid.Z2, Z1, Z2);
		return Math.Sqrt(Math.Min(num * num, num4 * num4) + Math.Min(num2 * num2, num5 * num5) + Math.Min(num3 * num3, num6 * num6));
	}

	public double ShortestDistanceFrom(Cuboidf cuboid, BlockPos offset)
	{
		double num = (float)offset.X + cuboid.X1;
		double num2 = (float)offset.Y + cuboid.Y1;
		double num3 = (float)offset.Z + cuboid.Z1;
		double num4 = (float)offset.X + cuboid.X2;
		double num5 = (float)offset.Y + cuboid.Y2;
		double num6 = (float)offset.Z + cuboid.Z2;
		double num7 = num - GameMath.Clamp(num, X1, X2);
		double num8 = num2 - GameMath.Clamp(num2, Y1, Y2);
		double num9 = num3 - GameMath.Clamp(num3, Z1, Z2);
		if (num <= X1 && num4 >= X2)
		{
			num7 = 0.0;
		}
		if (num2 <= Y1 && num5 >= Y2)
		{
			num8 = 0.0;
		}
		if (num3 <= Z1 && num6 >= Z2)
		{
			num9 = 0.0;
		}
		double num10 = num4 - GameMath.Clamp(num4, X1, X2);
		double num11 = num5 - GameMath.Clamp(num5, Y1, Y2);
		double num12 = num6 - GameMath.Clamp(num6, Z1, Z2);
		return Math.Sqrt(Math.Min(num7 * num7, num10 * num10) + Math.Min(num8 * num8, num11 * num11) + Math.Min(num9 * num9, num12 * num12));
	}

	public double ShortestHorizontalDistanceFrom(Cuboidf cuboid, BlockPos offset)
	{
		double num = (double)((float)offset.X + cuboid.X1) - GameMath.Clamp((float)offset.X + cuboid.X1, X1, X2);
		double num2 = (double)((float)offset.Z + cuboid.Z1) - GameMath.Clamp((float)offset.Z + cuboid.Z1, Z1, Z2);
		double num3 = (double)((float)offset.X + cuboid.X2) - GameMath.Clamp((float)offset.X + cuboid.X2, X1, X2);
		double num4 = (double)((float)offset.Z + cuboid.Z2) - GameMath.Clamp((float)offset.Z + cuboid.Z2, Z1, Z2);
		return Math.Sqrt(Math.Min(num * num, num3 * num3) + Math.Min(num2 * num2, num4 * num4));
	}

	public double ShortestHorizontalDistanceFrom(double x, double z)
	{
		double num = x - GameMath.Clamp(x, X1, X2);
		double num2 = z - GameMath.Clamp(z, Z1, Z2);
		return Math.Sqrt(num * num + num2 * num2);
	}

	public double ShortestDistanceFrom(IVec3 vec)
	{
		return ShortestDistanceFrom(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble);
	}

	public double pushOutX(Cuboidd from, double motx, ref EnumPushDirection direction)
	{
		direction = EnumPushDirection.None;
		if (from.Z2 > Z1 && from.Z1 < Z2 && from.Y2 > Y1 && from.Y1 < Y2)
		{
			if (motx > 0.0 && from.X2 <= X1 && X1 - from.X2 < motx)
			{
				direction = EnumPushDirection.Positive;
				motx = X1 - from.X2;
			}
			else if (motx < 0.0 && from.X1 >= X2 && X2 - from.X1 > motx)
			{
				direction = EnumPushDirection.Negative;
				motx = X2 - from.X1;
			}
		}
		return motx;
	}

	public double pushOutY(Cuboidd from, double moty, ref EnumPushDirection direction)
	{
		direction = EnumPushDirection.None;
		if (from.X2 > X1 && from.X1 < X2 && from.Z2 > Z1 && from.Z1 < Z2)
		{
			if (moty > 0.0 && from.Y2 <= Y1 && Y1 - from.Y2 < moty)
			{
				direction = EnumPushDirection.Positive;
				moty = Y1 - from.Y2;
			}
			else if (moty < 0.0 && from.Y1 >= Y2 && Y2 - from.Y1 > moty)
			{
				direction = EnumPushDirection.Negative;
				moty = Y2 - from.Y1;
			}
		}
		return moty;
	}

	public double pushOutZ(Cuboidd from, double motz, ref EnumPushDirection direction)
	{
		direction = EnumPushDirection.None;
		if (from.X2 > X1 && from.X1 < X2 && from.Y2 > Y1 && from.Y1 < Y2)
		{
			if (motz > 0.0 && from.Z2 <= Z1 && Z1 - from.Z2 < motz)
			{
				direction = EnumPushDirection.Positive;
				motz = Z1 - from.Z2;
			}
			else if (motz < 0.0 && from.Z1 >= Z2 && Z2 - from.Z1 > motz)
			{
				direction = EnumPushDirection.Negative;
				motz = Z2 - from.Z1;
			}
		}
		return motz;
	}

	public Cuboidd RotatedCopy(double degX, double degY, double degZ, Vec3d origin)
	{
		double rad = degX * 0.01745329238474369;
		double rad2 = degY * 0.01745329238474369;
		double rad3 = degZ * 0.01745329238474369;
		double[] array = Mat4d.Create();
		Mat4d.RotateX(array, array, rad);
		Mat4d.RotateY(array, array, rad2);
		Mat4d.RotateZ(array, array, rad3);
		(new double[4])[3] = 1.0;
		double[] vec = new double[4]
		{
			X1 - origin.X,
			Y1 - origin.Y,
			Z1 - origin.Z,
			1.0
		};
		double[] vec2 = new double[4]
		{
			X2 - origin.X,
			Y2 - origin.Y,
			Z2 - origin.Z,
			1.0
		};
		vec = Mat4d.MulWithVec4(array, vec);
		vec2 = Mat4d.MulWithVec4(array, vec2);
		if (vec2[0] < vec[0])
		{
			double num = vec2[0];
			vec2[0] = vec[0];
			vec[0] = num;
		}
		if (vec2[1] < vec[1])
		{
			double num = vec2[1];
			vec2[1] = vec[1];
			vec[1] = num;
		}
		if (vec2[2] < vec[2])
		{
			double num = vec2[2];
			vec2[2] = vec[2];
			vec[2] = num;
		}
		return new Cuboidd(vec[0] + origin.X, vec[1] + origin.Y, vec[2] + origin.Z, vec2[0] + origin.X, vec2[1] + origin.Y, vec2[2] + origin.Z);
	}

	public Cuboidd RotatedCopy(IVec3 vec, Vec3d origin)
	{
		return RotatedCopy(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble, origin);
	}

	public Cuboidd Offset(double dx, double dy, double dz)
	{
		X1 += dx;
		Y1 += dy;
		Z1 += dz;
		X2 += dx;
		Y2 += dy;
		Z2 += dz;
		return this;
	}

	public Cuboidd OffsetCopy(double x, double y, double z)
	{
		return new Cuboidd(X1 + x, Y1 + y, Z1 + z, X2 + x, Y2 + y, Z2 + z);
	}

	public Cuboidd OffsetCopy(IVec3 vec)
	{
		return OffsetCopy(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble);
	}

	public bool Intersects(Cuboidd other)
	{
		if (X2 > other.X1 && X1 < other.X2 && Y2 > other.Y1 && Y1 < other.Y2 && Z2 > other.Z1 && Z1 < other.Z2)
		{
			return true;
		}
		return false;
	}

	public bool Intersects(Cuboidf other)
	{
		if (X2 > (double)other.X1 && X1 < (double)other.X2 && Y2 > (double)other.Y1 && Y1 < (double)other.Y2 && Z2 > (double)other.Z1 && Z1 < (double)other.Z2)
		{
			return true;
		}
		return false;
	}

	public bool Intersects(Cuboidf other, Vec3d offset)
	{
		if (X2 > (double)other.X1 + offset.X && X1 < (double)other.X2 + offset.X && Z2 > (double)other.Z1 + offset.Z && Z1 < (double)other.Z2 + offset.Z && Y2 > (double)other.Y1 + offset.Y && Y1 < Math.Round((double)other.Y2 + offset.Y, 5))
		{
			return true;
		}
		return false;
	}

	public bool Intersects(Cuboidf other, double offsetx, double offsety, double offsetz)
	{
		if (X2 > (double)other.X1 + offsetx && X1 < (double)other.X2 + offsetx && Z2 > (double)other.Z1 + offsetz && Z1 < (double)other.Z2 + offsetz && Y2 > (double)other.Y1 + offsety && Y1 < Math.Round((double)other.Y2 + offsety, 5))
		{
			return true;
		}
		return false;
	}

	public bool IntersectsOrTouches(Cuboidd other)
	{
		if (X2 >= other.X1 && X1 <= other.X2 && Y2 >= other.Y1 && Y1 <= other.Y2 && Z2 >= other.Z1 && Z1 <= other.Z2)
		{
			return true;
		}
		return false;
	}

	public bool IntersectsOrTouches(Cuboidf other, Vec3d offset)
	{
		if (X2 >= (double)other.X1 + offset.X && X1 <= (double)other.X2 + offset.X && Z2 >= (double)other.Z1 + offset.Z && Z1 <= (double)other.Z2 + offset.Z && Y2 >= (double)other.Y1 + offset.Y && Y1 <= Math.Round((double)other.Y2 + offset.Y, 5))
		{
			return true;
		}
		return false;
	}

	public bool IntersectsOrTouches(Cuboidf other, double offsetX, double offsetY, double offsetZ)
	{
		return !(X2 < (double)other.X1 + offsetX) && !(X1 > (double)other.X2 + offsetX) && !(Y2 < (double)other.Y1 + offsetY) && !(Y1 > (double)other.Y2 + offsetY) && !(Z2 < (double)other.Z1 + offsetZ) && !(Z1 > (double)other.Z2 + offsetZ);
	}

	public Cuboidf ToFloat()
	{
		return new Cuboidf((float)X1, (float)Y1, (float)Z1, (float)X2, (float)Y2, (float)Z2);
	}

	public Cuboidd Clone()
	{
		return (Cuboidd)MemberwiseClone();
	}

	public bool Equals(Cuboidd other)
	{
		if (other.X1 == X1 && other.Y1 == Y1 && other.Z1 == Z1 && other.X2 == X2 && other.Y2 == Y2)
		{
			return other.Z2 == Z2;
		}
		return false;
	}
}
