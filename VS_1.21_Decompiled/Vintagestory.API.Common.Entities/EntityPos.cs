using System;
using System.IO;
using System.Runtime.CompilerServices;
using ProtoBuf;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

[ProtoContract]
public class EntityPos
{
	[ProtoMember(1)]
	protected double x;

	[ProtoMember(2)]
	protected double y;

	[ProtoMember(3)]
	protected double z;

	[ProtoMember(4)]
	public int Dimension;

	[ProtoMember(5)]
	protected float roll;

	[ProtoMember(6)]
	protected float yaw;

	[ProtoMember(7)]
	protected float pitch;

	[ProtoMember(8)]
	protected int stance;

	[ProtoMember(9)]
	public float HeadYaw;

	[ProtoMember(10)]
	public float HeadPitch;

	[ProtoMember(11)]
	public Vec3d Motion = new Vec3d();

	public virtual double X
	{
		get
		{
			return x;
		}
		set
		{
			x = value;
		}
	}

	public virtual double Y
	{
		get
		{
			return y;
		}
		set
		{
			y = value;
		}
	}

	public virtual double InternalY => y + (double)(Dimension * 32768);

	public virtual double Z
	{
		get
		{
			return z;
		}
		set
		{
			z = value;
		}
	}

	public virtual int DimensionYAdjustment => Dimension * 32768;

	public virtual float Roll
	{
		get
		{
			return roll;
		}
		set
		{
			roll = value;
		}
	}

	public virtual float Yaw
	{
		get
		{
			return yaw;
		}
		set
		{
			yaw = value;
		}
	}

	public virtual float Pitch
	{
		get
		{
			return pitch;
		}
		set
		{
			pitch = value;
		}
	}

	public BlockPos AsBlockPos => new BlockPos((int)x, (int)y, (int)z, Dimension);

	public Vec3i XYZInt => new Vec3i((int)x, (int)InternalY, (int)z);

	public Vec3d XYZ => new Vec3d(x, InternalY, z);

	public Vec3f XYZFloat => new Vec3f((float)x, (float)InternalY, (float)z);

	internal int XInt => (int)x;

	internal int YInt => (int)y;

	internal int ZInt => (int)z;

	public void SetPosWithDimension(Vec3d pos)
	{
		X = pos.X;
		y = pos.Y % 32768.0;
		z = pos.Z;
		Dimension = (int)pos.Y / 32768;
	}

	public void SetPos(Vec3d pos)
	{
		X = pos.X;
		y = pos.Y;
		z = pos.Z;
	}

	public EntityPos()
	{
	}

	public EntityPos(double x, double y, double z, float heading = 0f, float pitch = 0f, float roll = 0f)
	{
		this.x = x;
		this.y = y;
		this.z = z;
		yaw = heading;
		this.pitch = pitch;
		this.roll = roll;
	}

	public EntityPos Add(double x, double y, double z)
	{
		X += x;
		this.y += y;
		this.z += z;
		return this;
	}

	public EntityPos Add(Vec3f vec)
	{
		X += vec.X;
		y += vec.Y;
		z += vec.Z;
		return this;
	}

	public EntityPos SetPos(int x, int y, int z)
	{
		X = x;
		this.y = y;
		this.z = z;
		return this;
	}

	public EntityPos SetPos(BlockPos pos)
	{
		X = pos.X;
		y = pos.Y;
		z = pos.Z;
		return this;
	}

	public EntityPos SetPos(double x, double y, double z)
	{
		X = x;
		this.y = y;
		this.z = z;
		return this;
	}

	public EntityPos SetPos(EntityPos pos)
	{
		X = pos.x;
		y = pos.y;
		z = pos.z;
		return this;
	}

	public EntityPos SetAngles(EntityPos pos)
	{
		Roll = pos.roll;
		yaw = pos.yaw;
		pitch = pos.pitch;
		HeadPitch = pos.HeadPitch;
		HeadYaw = pos.HeadYaw;
		return this;
	}

	public EntityPos SetAngles(float roll, float yaw, float pitch)
	{
		Roll = roll;
		this.yaw = yaw;
		this.pitch = pitch;
		return this;
	}

	public EntityPos SetYaw(float yaw)
	{
		Yaw = yaw;
		return this;
	}

	public bool InRangeOf(EntityPos position, int squareDistance)
	{
		double num = x - position.x;
		double num2 = InternalY - position.InternalY;
		double num3 = z - position.z;
		return num * num + num2 * num2 + num3 * num3 <= (double)squareDistance;
	}

	public bool InRangeOf(int x, int y, int z, float squareDistance)
	{
		double num = this.x - (double)x;
		double num2 = InternalY - (double)y;
		double num3 = this.z - (double)z;
		return num * num + num2 * num2 + num3 * num3 <= (double)squareDistance;
	}

	public bool InHorizontalRangeOf(int x, int z, float squareDistance)
	{
		double num = this.x - (double)x;
		double num2 = this.z - (double)z;
		return num * num + num2 * num2 <= (double)squareDistance;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool InRangeOf(double x, double y, double z, float squareDistance)
	{
		double num = this.x - x;
		double num2 = InternalY - y;
		double num3 = this.z - z;
		return num * num + num2 * num2 + num3 * num3 <= (double)squareDistance;
	}

	public bool InRangeOf(BlockPos pos, float squareDistance)
	{
		double num = x - (double)pos.X;
		double num2 = InternalY - (double)pos.InternalY;
		double num3 = z - (double)pos.Z;
		return num * num + num2 * num2 + num3 * num3 <= (double)squareDistance;
	}

	public bool InRangeOf(Vec3f pos, float squareDistance)
	{
		double num = x - (double)pos.X;
		double num2 = InternalY - (double)pos.Y;
		double num3 = z - (double)pos.Z;
		return num * num + num2 * num2 + num3 * num3 <= (double)squareDistance;
	}

	public bool InRangeOf(Vec3d position, float horRangeSq, float vertRange)
	{
		double num = x - position.X;
		double num2 = z - position.Z;
		if (num * num + num2 * num2 > (double)horRangeSq)
		{
			return false;
		}
		return Math.Abs(InternalY - position.Y) <= (double)vertRange;
	}

	public float SquareDistanceTo(float x, float y, float z)
	{
		double num = this.x - (double)x;
		double num2 = InternalY - (double)y;
		double num3 = this.z - (double)z;
		return (float)(num * num + num2 * num2 + num3 * num3);
	}

	public float SquareDistanceTo(double x, double y, double z)
	{
		double num = this.x - x;
		double num2 = InternalY - y;
		double num3 = this.z - z;
		return (float)(num * num + num2 * num2 + num3 * num3);
	}

	public double SquareDistanceTo(Vec3d pos)
	{
		double num = x - pos.X;
		double num2 = InternalY - pos.Y;
		double num3 = z - pos.Z;
		return num * num + num2 * num2 + num3 * num3;
	}

	public double SquareHorDistanceTo(Vec3d pos)
	{
		double num = x - pos.X;
		double num2 = z - pos.Z;
		return num * num + num2 * num2;
	}

	public double DistanceTo(Vec3d pos)
	{
		double num = x - pos.X;
		double num2 = InternalY - pos.Y;
		double num3 = z - pos.Z;
		return GameMath.Sqrt(num * num + num2 * num2 + num3 * num3);
	}

	public double DistanceTo(EntityPos pos)
	{
		double num = x - pos.x;
		double num2 = InternalY - pos.InternalY;
		double num3 = z - pos.z;
		return GameMath.Sqrt(num * num + num2 * num2 + num3 * num3);
	}

	public double HorDistanceTo(Vec3d pos)
	{
		double num = x - pos.X;
		double num2 = z - pos.Z;
		return GameMath.Sqrt(num * num + num2 * num2);
	}

	public double HorDistanceTo(double x, double z)
	{
		double num = this.x - x;
		double num2 = this.z - z;
		return GameMath.Sqrt(num * num + num2 * num2);
	}

	public double HorDistanceTo(EntityPos pos)
	{
		double num = x - pos.x;
		double num2 = z - pos.z;
		return GameMath.Sqrt(num * num + num2 * num2);
	}

	public float SquareDistanceTo(EntityPos pos)
	{
		double num = x - pos.x;
		double num2 = InternalY - pos.InternalY;
		double num3 = z - pos.z;
		return (float)(num * num + num2 * num2 + num3 * num3);
	}

	public EntityPos Copy()
	{
		return new EntityPos
		{
			X = x,
			y = y,
			z = z,
			yaw = yaw,
			pitch = pitch,
			roll = roll,
			HeadYaw = HeadYaw,
			HeadPitch = HeadPitch,
			Motion = new Vec3d(Motion.X, Motion.Y, Motion.Z),
			Dimension = Dimension
		};
	}

	public Vec3f GetViewVector()
	{
		return GetViewVector(pitch, yaw);
	}

	public static Vec3f GetViewVector(float pitch, float yaw)
	{
		float num = GameMath.Cos(pitch);
		float num2 = GameMath.Sin(pitch);
		float num3 = GameMath.Cos(yaw);
		float num4 = GameMath.Sin(yaw);
		return new Vec3f((0f - num) * num4, num2, (0f - num) * num3);
	}

	public EntityPos AheadCopy(double offset)
	{
		float num = GameMath.Cos(pitch);
		float num2 = GameMath.Sin(pitch);
		float num3 = GameMath.Cos(yaw);
		float num4 = GameMath.Sin(yaw);
		return new EntityPos(x - (double)(num * num4) * offset, y + (double)num2 * offset, z - (double)(num * num3) * offset, yaw, pitch, roll)
		{
			Dimension = Dimension
		};
	}

	public EntityPos HorizontalAheadCopy(double offset)
	{
		float num = GameMath.Cos(yaw);
		float num2 = GameMath.Sin(yaw);
		return new EntityPos(x + (double)num2 * offset, y, z + (double)num * offset, yaw, pitch, roll)
		{
			Dimension = Dimension
		};
	}

	public EntityPos BehindCopy(double offset)
	{
		float num = GameMath.Cos(0f - yaw);
		float num2 = GameMath.Sin(0f - yaw);
		return new EntityPos(x + (double)num2 * offset, y, z + (double)num * offset, yaw, pitch, roll)
		{
			Dimension = Dimension
		};
	}

	public bool BasicallySameAs(EntityPos pos, double epsilon = 0.0001)
	{
		double num = epsilon * epsilon;
		if (GameMath.SumOfSquares(x - pos.x, y - pos.y, z - pos.z) >= num)
		{
			return false;
		}
		if (GameMath.Square(roll - pos.roll) < num && GameMath.Square(yaw - pos.yaw) < num && GameMath.Square(pitch - pos.pitch) < num)
		{
			return GameMath.SumOfSquares(Motion.X - pos.Motion.X, Motion.Y - pos.Motion.Y, Motion.Z - pos.Motion.Z) < num;
		}
		return false;
	}

	public bool BasicallySameAsIgnoreMotion(EntityPos pos, double epsilon = 0.0001)
	{
		double num = epsilon * epsilon;
		if (GameMath.Square(x - pos.x) >= num || GameMath.Square(y - pos.y) >= num || GameMath.Square(z - pos.z) >= num)
		{
			return false;
		}
		if (GameMath.Square(roll - pos.roll) < num && GameMath.Square(yaw - pos.yaw) < num)
		{
			return GameMath.Square(pitch - pos.pitch) < num;
		}
		return false;
	}

	public bool BasicallySameAsIgnoreAngles(EntityPos pos, double epsilon = 0.0001)
	{
		double num = epsilon * epsilon;
		if (GameMath.SumOfSquares(x - pos.x, y - pos.y, z - pos.z) < num)
		{
			return GameMath.SumOfSquares(Motion.X - pos.Motion.X, Motion.Y - pos.Motion.Y, Motion.Z - pos.Motion.Z) < num;
		}
		return false;
	}

	public EntityPos SetFrom(EntityPos pos)
	{
		X = pos.x;
		y = pos.y;
		z = pos.z;
		Dimension = pos.Dimension;
		roll = pos.roll;
		yaw = pos.yaw;
		pitch = pos.pitch;
		Motion.Set(pos.Motion);
		HeadYaw = pos.HeadYaw;
		HeadPitch = pos.HeadPitch;
		return this;
	}

	public EntityPos SetFrom(Vec3d pos)
	{
		X = pos.X;
		y = pos.Y;
		z = pos.Z;
		return this;
	}

	public override string ToString()
	{
		return "XYZ: " + X + "/" + Y + "/" + Z + ", YPR " + Yaw + "/" + Pitch + "/" + Roll + ", Dim " + Dimension;
	}

	public string OnlyPosToString()
	{
		return X.ToString("#.##", GlobalConstants.DefaultCultureInfo) + ", " + Y.ToString("#.##", GlobalConstants.DefaultCultureInfo) + ", " + Z.ToString("#.##", GlobalConstants.DefaultCultureInfo);
	}

	public string OnlyAnglesToString()
	{
		return roll.ToString("#.##", GlobalConstants.DefaultCultureInfo) + ", " + yaw.ToString("#.##", GlobalConstants.DefaultCultureInfo) + pitch.ToString("#.##", GlobalConstants.DefaultCultureInfo);
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(x);
		writer.Write(InternalY);
		writer.Write(z);
		writer.Write(roll);
		writer.Write(yaw);
		writer.Write(pitch);
		writer.Write(stance);
		writer.Write(Motion.X);
		writer.Write(Motion.Y);
		writer.Write(Motion.Z);
	}

	public void FromBytes(BinaryReader reader)
	{
		x = reader.ReadDouble();
		y = reader.ReadDouble();
		Dimension = (int)y / 32768;
		y -= Dimension * 32768;
		z = reader.ReadDouble();
		roll = reader.ReadSingle();
		yaw = reader.ReadSingle();
		pitch = reader.ReadSingle();
		stance = reader.ReadInt32();
		Motion.X = reader.ReadDouble();
		Motion.Y = reader.ReadDouble();
		Motion.Z = reader.ReadDouble();
	}

	public bool AnyNaN()
	{
		if (double.IsNaN(x + y + z))
		{
			return true;
		}
		if (float.IsNaN(roll + yaw + pitch))
		{
			return true;
		}
		if (double.IsNaN(Motion.X + Motion.Y + Motion.Z))
		{
			return true;
		}
		if (Math.Abs(x) + Math.Abs(y) + Math.Abs(z) > 268435456.0)
		{
			return true;
		}
		return false;
	}
}
