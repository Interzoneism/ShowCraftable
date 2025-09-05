using System;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using ProtoBuf;
using Vintagestory.API.Common;

namespace Vintagestory.API.MathTools;

[ProtoContract]
[JsonObject(/*Could not decode attribute arguments.*/)]
public class BlockPos : IEquatable<BlockPos>, IVec3
{
	[ProtoMember(1)]
	[JsonProperty]
	public int X;

	[ProtoMember(3)]
	[JsonProperty]
	public int Z;

	public int Y;

	public int dimension;

	public const int DimensionBoundary = 32768;

	[ProtoMember(2)]
	[JsonProperty]
	public int InternalY
	{
		get
		{
			return Y + dimension * 32768;
		}
		set
		{
			Y = value % 32768;
			dimension = value / 32768;
		}
	}

	public int this[int i]
	{
		get
		{
			return i switch
			{
				2 => Z, 
				1 => Y, 
				0 => X, 
				_ => dimension, 
			};
		}
		set
		{
			switch (i)
			{
			case 0:
				X = value;
				break;
			case 1:
				Y = value;
				break;
			case 2:
				Z = value;
				break;
			default:
				dimension = value;
				break;
			}
		}
	}

	int IVec3.XAsInt => X;

	int IVec3.YAsInt => Y;

	int IVec3.ZAsInt => Z;

	double IVec3.XAsDouble => X;

	double IVec3.YAsDouble => Y;

	double IVec3.ZAsDouble => Z;

	float IVec3.XAsFloat => X;

	float IVec3.YAsFloat => Y;

	float IVec3.ZAsFloat => Z;

	[JsonIgnore]
	public Vec3i AsVec3i => new Vec3i(X, Y, Z);

	[Obsolete("Not dimension-aware. Use new BlockPos(dimensionId) where possible")]
	public BlockPos()
	{
	}

	public BlockPos(int dim)
	{
		dimension = dim;
	}

	public BlockPos(int x, int y, int z)
	{
		X = x;
		Y = y % 32768;
		Z = z;
		dimension = y / 32768;
	}

	public BlockPos(int x, int y, int z, int dim)
	{
		X = x;
		Y = y;
		Z = z;
		dimension = dim;
	}

	[Obsolete("Not dimension-aware. Use overload with a dimension parameter instead")]
	public BlockPos(Vec3i vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	public BlockPos(Vec3i vec, int dim)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
		dimension = dim;
	}

	public BlockPos(Vec4i vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	public BlockPos Up(int dy = 1)
	{
		Y += dy;
		return this;
	}

	public BlockPos Down(int dy = 1)
	{
		Y -= dy;
		return this;
	}

	public BlockPos Set(Vec3d origin)
	{
		X = (int)origin.X;
		Y = (int)origin.Y;
		Z = (int)origin.Z;
		return this;
	}

	public BlockPos Set(Vec3i pos)
	{
		X = pos.X;
		Y = pos.Y;
		Z = pos.Z;
		return this;
	}

	public BlockPos Set(FastVec3i pos)
	{
		X = pos.X;
		Y = pos.Y;
		Z = pos.Z;
		return this;
	}

	public BlockPos SetAndCorrectDimension(Vec3d origin)
	{
		X = (int)origin.X;
		Y = (int)origin.Y % 32768;
		Z = (int)origin.Z;
		dimension = (int)origin.Y / 32768;
		return this;
	}

	public BlockPos SetAndCorrectDimension(int x, int y, int z)
	{
		X = x;
		Y = y % 32768;
		Z = z;
		dimension = y / 32768;
		return this;
	}

	public BlockPos Set(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
		return this;
	}

	public BlockPos Set(float x, float y, float z)
	{
		X = (int)x;
		Y = (int)y;
		Z = (int)z;
		return this;
	}

	public BlockPos Set(BlockPos blockPos)
	{
		X = blockPos.X;
		Y = blockPos.Y;
		Z = blockPos.Z;
		return this;
	}

	public BlockPos Set(BlockPos blockPos, int dim)
	{
		X = blockPos.X;
		Y = blockPos.Y;
		Z = blockPos.Z;
		dimension = dim;
		return this;
	}

	public BlockPos SetDimension(int dim)
	{
		dimension = dim;
		return this;
	}

	public bool SetAndEquals(int x, int y, int z)
	{
		if (X == x && Z == z && Y == y)
		{
			return true;
		}
		X = x;
		Y = y;
		Z = z;
		return false;
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(X);
		writer.Write(Y);
		writer.Write(Z);
		writer.Write(dimension);
	}

	public Vec3i ToLocalPosition(ICoreAPI api)
	{
		return new Vec3i(X - api.World.DefaultSpawnPosition.XInt, Y, Z - api.World.DefaultSpawnPosition.ZInt);
	}

	public BlockPos West()
	{
		X--;
		return this;
	}

	public static BlockPos CreateFromBytes(BinaryReader reader)
	{
		return new BlockPos(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
	}

	public BlockPos North()
	{
		Z--;
		return this;
	}

	public BlockPos East()
	{
		X++;
		return this;
	}

	public BlockPos South()
	{
		Z++;
		return this;
	}

	public BlockFacing FacingFrom(BlockPos other)
	{
		int num = other.X - X;
		int num2 = other.Y - Y;
		int num3 = other.Z - Z;
		if (num * num >= num3 * num3)
		{
			if (num * num >= num2 * num2)
			{
				if (num <= 0)
				{
					return BlockFacing.EAST;
				}
				return BlockFacing.WEST;
			}
		}
		else if (num3 * num3 >= num2 * num2)
		{
			if (num3 <= 0)
			{
				return BlockFacing.SOUTH;
			}
			return BlockFacing.NORTH;
		}
		if (num2 <= 0)
		{
			return BlockFacing.UP;
		}
		return BlockFacing.DOWN;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos WestCopy(int length = 1)
	{
		return new BlockPos(X - length, Y, Z, dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos SouthCopy(int length = 1)
	{
		return new BlockPos(X, Y, Z + length, dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos EastCopy(int length = 1)
	{
		return new BlockPos(X + length, Y, Z, dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos NorthCopy(int length = 1)
	{
		return new BlockPos(X, Y, Z - length, dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos DownCopy(int length = 1)
	{
		return new BlockPos(X, Y - length, Z, dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos UpCopy(int length = 1)
	{
		return new BlockPos(X, Y + length, Z, dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual BlockPos Copy()
	{
		return new BlockPos(X, Y, Z, dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual BlockPos CopyAndCorrectDimension()
	{
		return new BlockPos(X, Y % 32768, Z, dimension + Y / 32768);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos Add(float dx, float dy, float dz)
	{
		X += (int)dx;
		Y += (int)dy;
		Z += (int)dz;
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos Add(int dx, int dy, int dz)
	{
		X += dx;
		Y += dy;
		Z += dz;
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos Add(Vec3i vector)
	{
		X += vector.X;
		Y += vector.Y;
		Z += vector.Z;
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos Add(FastVec3i vector)
	{
		X += vector.X;
		Y += vector.Y;
		Z += vector.Z;
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos Add(BlockPos pos)
	{
		X += pos.X;
		Y += pos.Y;
		Z += pos.Z;
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos Add(BlockFacing facing, int length = 1)
	{
		Vec3i normali = facing.Normali;
		X += normali.X * length;
		Y += normali.Y * length;
		Z += normali.Z * length;
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos Offset(BlockFacing facing)
	{
		Vec3i normali = facing.Normali;
		X += normali.X;
		Y += normali.Y;
		Z += normali.Z;
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos AddCopy(float dx, float dy, float dz)
	{
		return new BlockPos((int)((float)X + dx), (int)((float)Y + dy), (int)((float)Z + dz), dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos AddCopy(int dx, int dy, int dz)
	{
		return new BlockPos(X + dx, Y + dy, Z + dz, dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos AddCopy(int xyz)
	{
		return new BlockPos(X + xyz, Y + xyz, Z + xyz, dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos AddCopy(Vec3i vector)
	{
		return new BlockPos(X + vector.X, Y + vector.Y, Z + vector.Z, dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos AddCopy(BlockFacing facing)
	{
		return new BlockPos(X + facing.Normali.X, Y + facing.Normali.Y, Z + facing.Normali.Z, dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos AddCopy(BlockFacing facing, int length)
	{
		return new BlockPos(X + facing.Normali.X * length, Y + facing.Normali.Y * length, Z + facing.Normali.Z * length, dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos Sub(BlockPos pos)
	{
		X -= pos.X;
		Y -= pos.Y;
		Z -= pos.Z;
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos Sub(int x, int y, int z)
	{
		X -= x;
		Y -= y;
		Z -= z;
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos SubCopy(BlockPos pos)
	{
		return new BlockPos(X - pos.X, InternalY - pos.InternalY, Z - pos.Z, 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos SubCopy(int x, int y, int z)
	{
		return new BlockPos(X - x, Y - y, Z - z, dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BlockPos DivCopy(int factor)
	{
		return new BlockPos(X / factor, Y / factor, Z / factor, dimension);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void IterateHorizontalOffsets(int i)
	{
		Z += ((i + 1) & 2) - 1;
		X += ((i % 3) & 1) - i / 2;
	}

	public float DistanceTo(BlockPos pos)
	{
		if (pos.dimension != dimension)
		{
			return float.MaxValue;
		}
		double num = pos.X - X;
		double num2 = pos.Y - Y;
		double num3 = pos.Z - Z;
		return GameMath.Sqrt(num * num + num2 * num2 + num3 * num3);
	}

	public float DistanceTo(double x, double y, double z)
	{
		double num = x - (double)X;
		double num2 = y - (double)Y;
		double num3 = z - (double)Z;
		return GameMath.Sqrt(num * num + num2 * num2 + num3 * num3);
	}

	public float DistanceSqTo(double x, double y, double z)
	{
		double num = x - (double)X;
		double num2 = y - (double)InternalY;
		double num3 = z - (double)Z;
		return (float)(num * num + num2 * num2 + num3 * num3);
	}

	public double DistanceSqToNearerEdge(double x, double y, double z)
	{
		double num = x - (double)X;
		double num2 = y - (double)Y - 0.75;
		double num3 = z - (double)Z;
		if (num > 0.0)
		{
			num = ((num <= 1.0) ? 0.0 : (num - 1.0));
		}
		if (num3 > 0.0)
		{
			num3 = ((num3 <= 1.0) ? 0.0 : (num3 - 1.0));
		}
		return num * num + num2 * num2 + num3 * num3;
	}

	public float HorDistanceSqTo(double x, double z)
	{
		double num = x - (double)X;
		double num2 = z - (double)Z;
		return (float)(num * num + num2 * num2);
	}

	public int HorizontalManhattenDistance(BlockPos pos)
	{
		if (pos.dimension != dimension)
		{
			return int.MaxValue;
		}
		return Math.Abs(X - pos.X) + Math.Abs(Z - pos.Z);
	}

	public int ManhattenDistance(BlockPos pos)
	{
		if (pos.dimension != dimension)
		{
			return int.MaxValue;
		}
		return Math.Abs(X - pos.X) + Math.Abs(Y - pos.Y) + Math.Abs(Z - pos.Z);
	}

	public int ManhattenDistance(int x, int y, int z)
	{
		return Math.Abs(X - x) + Math.Abs(Y - y) + Math.Abs(Z - z);
	}

	public bool InRangeHorizontally(int x, int z, int range)
	{
		if (Math.Abs(X - x) <= range)
		{
			return Math.Abs(Z - z) <= range;
		}
		return false;
	}

	public Vec3d ToVec3d()
	{
		return new Vec3d(X, InternalY, Z);
	}

	public Vec3i ToVec3i()
	{
		return new Vec3i(X, InternalY, Z);
	}

	public Vec3f ToVec3f()
	{
		return new Vec3f(X, InternalY, Z);
	}

	public override string ToString()
	{
		return X + ", " + Y + ", " + Z + ((dimension > 0) ? (" : " + dimension) : "");
	}

	public override bool Equals(object obj)
	{
		if (obj is BlockPos blockPos && X == blockPos.X && Y == blockPos.Y && Z == blockPos.Z)
		{
			return dimension == blockPos.dimension;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((391 + X) * 23 + Y) * 23 + Z + dimension * 269023;
	}

	public bool Equals(BlockPos other)
	{
		if (other != null && X == other.X && Y == other.Y && Z == other.Z)
		{
			return dimension == other.dimension;
		}
		return false;
	}

	public bool Equals(int x, int y, int z)
	{
		if (X == x && Y == y)
		{
			return Z == z;
		}
		return false;
	}

	public static BlockPos operator +(BlockPos left, BlockPos right)
	{
		return new BlockPos(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.dimension);
	}

	public static BlockPos operator -(BlockPos left, BlockPos right)
	{
		return new BlockPos(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.dimension);
	}

	public static BlockPos operator +(BlockPos left, int right)
	{
		return new BlockPos(left.X + right, left.Y + right, left.Z + right, left.dimension);
	}

	public static BlockPos operator -(BlockPos left, int right)
	{
		return new BlockPos(left.X - right, left.Y - right, left.Z - right, left.dimension);
	}

	public static BlockPos operator *(BlockPos left, int right)
	{
		return new BlockPos(left.X * right, left.Y * right, left.Z * right, left.dimension);
	}

	public static BlockPos operator *(int left, BlockPos right)
	{
		return new BlockPos(left * right.X, left * right.Y, left * right.Z, right.dimension);
	}

	public static BlockPos operator /(BlockPos left, int right)
	{
		return new BlockPos(left.X / right, left.Y / right, left.Z / right, left.dimension);
	}

	public static bool operator ==(BlockPos left, BlockPos right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(BlockPos left, BlockPos right)
	{
		return !(left == right);
	}

	public static void Walk(BlockPos startPos, BlockPos untilPos, Vec3i mapSizeForClamp, Action<int, int, int> onpos)
	{
		int num = GameMath.Clamp(Math.Min(startPos.X, untilPos.X), 0, mapSizeForClamp.X);
		int num2 = GameMath.Clamp(Math.Min(startPos.Y, untilPos.Y), 0, mapSizeForClamp.Y);
		int num3 = GameMath.Clamp(Math.Min(startPos.Z, untilPos.Z), 0, mapSizeForClamp.Z);
		int num4 = GameMath.Clamp(Math.Max(startPos.X, untilPos.X), 0, mapSizeForClamp.X);
		int num5 = GameMath.Clamp(Math.Max(startPos.Y, untilPos.Y), 0, mapSizeForClamp.Y);
		int num6 = GameMath.Clamp(Math.Max(startPos.Z, untilPos.Z), 0, mapSizeForClamp.Z);
		for (int i = num; i < num4; i++)
		{
			for (int j = num2; j < num5; j++)
			{
				for (int k = num3; k < num6; k++)
				{
					onpos(i, j, k);
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ToColumnIndex3d()
	{
		int num = X % 32;
		int num2 = Z % 32;
		return (Y * 32 + num2) * 32 + num;
	}

	public void SetFromColumnIndex3d(int index3d, int cx, int cz)
	{
		X = cx * 32 + index3d % 32;
		Z = cz * 32 + index3d / 32 % 32;
		Y = index3d / 1024;
	}

	public int ToSchematicIndex()
	{
		return (Y << 20) + (Z << 10) + X;
	}

	public void SetFromSchematicIndex(int index3d)
	{
		X = index3d & 0x3FF;
		Z = (index3d >> 10) & 0x3FF;
		Y = (index3d >> 20) & 0x3FF;
	}
}
