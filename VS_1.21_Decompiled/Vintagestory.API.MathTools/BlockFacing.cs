using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.API.MathTools;

public class BlockFacing
{
	public const int NumberOfFaces = 6;

	public const int indexNORTH = 0;

	public const int indexEAST = 1;

	public const int indexSOUTH = 2;

	public const int indexWEST = 3;

	public const int indexUP = 4;

	public const int indexDOWN = 5;

	public static readonly byte HorizontalFlags = 15;

	public static readonly byte VerticalFlags = 48;

	public static readonly BlockFacing NORTH = new BlockFacing("north", 1, 0, 2, 1, new Vec3i(0, 0, -1), new Vec3f(0.5f, 0.5f, 0f), EnumAxis.Z, new Cuboidf(0f, 0f, 0f, 1f, 1f, 0f));

	public static readonly BlockFacing EAST = new BlockFacing("east", 2, 1, 3, 0, new Vec3i(1, 0, 0), new Vec3f(1f, 0.5f, 0.5f), EnumAxis.X, new Cuboidf(1f, 0f, 0f, 1f, 1f, 1f));

	public static readonly BlockFacing SOUTH = new BlockFacing("south", 4, 2, 0, 3, new Vec3i(0, 0, 1), new Vec3f(0.5f, 0.5f, 1f), EnumAxis.Z, new Cuboidf(0f, 0f, 1f, 1f, 1f, 1f));

	public static readonly BlockFacing WEST = new BlockFacing("west", 8, 3, 1, 2, new Vec3i(-1, 0, 0), new Vec3f(0f, 0.5f, 0.5f), EnumAxis.X, new Cuboidf(0f, 0f, 0f, 0f, 1f, 1f));

	public static readonly BlockFacing UP = new BlockFacing("up", 16, 4, 5, -1, new Vec3i(0, 1, 0), new Vec3f(0.5f, 1f, 0.5f), EnumAxis.Y, new Cuboidf(0f, 1f, 0f, 1f, 1f, 1f));

	public static readonly BlockFacing DOWN = new BlockFacing("down", 32, 5, 4, -1, new Vec3i(0, -1, 0), new Vec3f(0.5f, 0f, 0.5f), EnumAxis.Y, new Cuboidf(0f, 0f, 0f, 1f, 0f, 1f));

	public static readonly BlockFacing[] ALLFACES = new BlockFacing[6] { NORTH, EAST, SOUTH, WEST, UP, DOWN };

	public static readonly Vec3i[] ALLNORMALI = new Vec3i[6] { NORTH.normali, EAST.normali, SOUTH.normali, WEST.normali, UP.normali, DOWN.normali };

	public static readonly int[] AllVertexFlagsNormals = new int[6] { NORTH.normalPackedFlags, EAST.normalPackedFlags, SOUTH.normalPackedFlags, WEST.normalPackedFlags, UP.normalPackedFlags, DOWN.normalPackedFlags };

	public static readonly BlockFacing[] HORIZONTALS = new BlockFacing[4] { NORTH, EAST, SOUTH, WEST };

	public static readonly Vec3i[] HORIZONTAL_NORMALI = new Vec3i[4] { NORTH.normali, EAST.normali, SOUTH.normali, WEST.normali };

	public static readonly BlockFacing[] VERTICALS = new BlockFacing[2] { UP, DOWN };

	public static readonly BlockFacing[] HORIZONTALS_ANGLEORDER = new BlockFacing[4] { EAST, NORTH, WEST, SOUTH };

	private int index;

	private byte meshDataIndex;

	private int horizontalAngleIndex;

	private byte flag;

	private int oppositeIndex;

	private Vec3i normali;

	private Vec3f normalf;

	private Vec3d normald;

	private byte normalb;

	private int normalPacked;

	private int normalPackedFlags;

	private Vec3f planeCenter;

	private string code;

	private EnumAxis axis;

	private Cuboidf plane;

	public byte Flag => flag;

	public int Index => index;

	public byte MeshDataIndex => meshDataIndex;

	public int HorizontalAngleIndex => horizontalAngleIndex;

	public Vec3i Normali => normali;

	public Vec3f Normalf => normalf;

	public Vec3d Normald => normald;

	public Cuboidf Plane => plane;

	public byte NormalByte => normalb;

	public int NormalPacked => normalPacked;

	public int NormalPackedFlags => normalPackedFlags;

	public Vec3f PlaneCenter => planeCenter;

	public string Code => code;

	public bool IsHorizontal => index <= 3;

	public bool IsVertical => index >= 4;

	public bool IsAxisNS
	{
		get
		{
			if (index != 0)
			{
				return index == 2;
			}
			return true;
		}
	}

	public bool IsAxisWE
	{
		get
		{
			if (index != 1)
			{
				return index == 3;
			}
			return true;
		}
	}

	public EnumAxis Axis => axis;

	public BlockFacing Opposite => ALLFACES[oppositeIndex];

	public bool Negative
	{
		get
		{
			if (index != 0 && index != 3)
			{
				return index == 5;
			}
			return true;
		}
	}

	private BlockFacing(string code, byte flag, int index, int oppositeIndex, int horizontalAngleIndex, Vec3i facingVector, Vec3f planeCenter, EnumAxis axis, Cuboidf plane)
	{
		this.index = index;
		meshDataIndex = (byte)(index + 1);
		this.horizontalAngleIndex = horizontalAngleIndex;
		this.flag = flag;
		this.code = code;
		this.oppositeIndex = oppositeIndex;
		normali = facingVector;
		normalf = new Vec3f(facingVector.X, facingVector.Y, facingVector.Z);
		normald = new Vec3d(facingVector.X, facingVector.Y, facingVector.Z);
		this.plane = plane;
		normalPacked = NormalUtil.PackNormal(normalf.X, normalf.Y, normalf.Z);
		normalb = (byte)(((axis == EnumAxis.Z) ? 1u : 0u) | (((facingVector.Z < 0) ? 1u : 0u) << 1) | (((axis == EnumAxis.Y) ? 1u : 0u) << 2) | (((facingVector.Y < 0) ? 1u : 0u) << 3) | (((axis == EnumAxis.X) ? 1u : 0u) << 4) | (((facingVector.X < 0) ? 1u : 0u) << 5));
		normalPackedFlags = VertexFlags.PackNormal(normalf);
		this.planeCenter = planeCenter;
		this.axis = axis;
	}

	[Obsolete("Use Opposite property instead")]
	public BlockFacing GetOpposite()
	{
		return ALLFACES[oppositeIndex];
	}

	public BlockFacing GetCCW()
	{
		return HORIZONTALS_ANGLEORDER[(horizontalAngleIndex + 1) % 4];
	}

	public BlockFacing GetCW()
	{
		return HORIZONTALS_ANGLEORDER[GameMath.Mod(horizontalAngleIndex - 1, 4)];
	}

	public BlockFacing GetHorizontalRotated(int angle)
	{
		if (horizontalAngleIndex < 0)
		{
			return this;
		}
		int num = GameMath.Mod(angle / 90 + index, 4);
		return HORIZONTALS[num];
	}

	public BlockFacing FaceWhenRotatedBy(float radX, float radY, float radZ)
	{
		float[] array = Mat4f.Create();
		Mat4f.RotateX(array, array, radX);
		Mat4f.RotateY(array, array, radY);
		Mat4f.RotateZ(array, array, radZ);
		float[] vec = new float[4] { Normalf.X, Normalf.Y, Normalf.Z, 1f };
		vec = Mat4f.MulWithVec4(array, vec);
		float num = (float)Math.PI;
		BlockFacing result = null;
		for (int i = 0; i < ALLFACES.Length; i++)
		{
			BlockFacing blockFacing = ALLFACES[i];
			float num2 = (float)Math.Acos(blockFacing.Normalf.Dot(vec));
			if (num2 < num)
			{
				num = num2;
				result = blockFacing;
			}
		}
		return result;
	}

	public float GetFaceBrightness(float radX, float radY, float radZ, float[] BlockSideBrightnessByFacing)
	{
		float[] array = Mat4f.Create();
		Mat4f.RotateX(array, array, radX);
		Mat4f.RotateY(array, array, radY);
		Mat4f.RotateZ(array, array, radZ);
		FastVec3f a = Mat4f.MulWithVec3(array, Normalf.X, Normalf.Y, Normalf.Z);
		float num = 0f;
		for (int i = 0; i < ALLFACES.Length; i++)
		{
			BlockFacing blockFacing = ALLFACES[i];
			float num2 = (float)Math.Acos(blockFacing.Normalf.Dot(a));
			if (!(num2 >= (float)Math.PI / 2f))
			{
				num += (1f - num2 / ((float)Math.PI / 2f)) * BlockSideBrightnessByFacing[blockFacing.Index];
			}
		}
		return num;
	}

	public Vec2f ToAB(Vec3f pos)
	{
		return axis switch
		{
			EnumAxis.X => new Vec2f(pos.Z, pos.Y), 
			EnumAxis.Y => new Vec2f(pos.X, pos.Z), 
			EnumAxis.Z => new Vec2f(pos.X, pos.Y), 
			_ => null, 
		};
	}

	public void IterateThruFacingOffsets(BlockPos pos)
	{
		switch (index)
		{
		case 0:
			pos.Z--;
			break;
		case 1:
			pos.Z++;
			pos.X++;
			break;
		case 2:
			pos.X--;
			pos.Z++;
			break;
		case 3:
			pos.Z--;
			pos.X--;
			break;
		case 4:
			pos.X++;
			pos.Y++;
			break;
		case 5:
			pos.Y -= 2;
			break;
		}
	}

	public static void FinishIteratingAllFaces(BlockPos pos)
	{
		pos.Y++;
	}

	public float GetFaceBrightness(double[] matrix, float[] BlockSideBrightnessByFacing)
	{
		double[] vec = new double[4] { Normalf.X, Normalf.Y, Normalf.Z, 1.0 };
		matrix[12] = 0.0;
		matrix[13] = 0.0;
		matrix[14] = 0.0;
		vec = Mat4d.MulWithVec4(matrix, vec);
		float num = GameMath.Sqrt(vec[0] * vec[0] + vec[1] * vec[1] + vec[2] * vec[2]);
		vec[0] /= num;
		vec[1] /= num;
		vec[2] /= num;
		float num2 = 0f;
		for (int i = 0; i < ALLFACES.Length; i++)
		{
			BlockFacing blockFacing = ALLFACES[i];
			float num3 = (float)Math.Acos(blockFacing.Normalf.Dot(vec));
			if (!(num3 >= (float)Math.PI / 2f))
			{
				num2 += (1f - num3 / ((float)Math.PI / 2f)) * BlockSideBrightnessByFacing[blockFacing.Index];
			}
		}
		return num2;
	}

	public bool IsAdjacent(BlockFacing facing)
	{
		if (IsVertical)
		{
			return facing.IsHorizontal;
		}
		if ((!IsHorizontal || !facing.IsVertical) && (axis != EnumAxis.X || facing.axis != EnumAxis.Z))
		{
			if (axis == EnumAxis.Z)
			{
				return facing.axis == EnumAxis.X;
			}
			return false;
		}
		return true;
	}

	public override string ToString()
	{
		return code;
	}

	public static BlockFacing FromCode(string code)
	{
		code = code?.ToLowerInvariant();
		return code switch
		{
			"north" => NORTH, 
			"south" => SOUTH, 
			"east" => EAST, 
			"west" => WEST, 
			"up" => UP, 
			"down" => DOWN, 
			_ => null, 
		};
	}

	public static BlockFacing FromFirstLetter(char code)
	{
		return FromFirstLetter(code.ToString() ?? "");
	}

	public static BlockFacing FromFirstLetter(string code)
	{
		if (code.Length < 1)
		{
			return null;
		}
		return char.ToLowerInvariant(code[0]) switch
		{
			'n' => NORTH, 
			's' => SOUTH, 
			'e' => EAST, 
			'w' => WEST, 
			'u' => UP, 
			'd' => DOWN, 
			_ => null, 
		};
	}

	public static BlockFacing FromNormal(Vec3f vec)
	{
		float num = (float)Math.PI;
		BlockFacing result = null;
		for (int i = 0; i < ALLFACES.Length; i++)
		{
			BlockFacing blockFacing = ALLFACES[i];
			float num2 = (float)Math.Acos(blockFacing.Normalf.Dot(vec));
			if (num2 < num)
			{
				num = num2;
				result = blockFacing;
			}
		}
		return result;
	}

	public static BlockFacing FromNormal(Vec3i vec)
	{
		for (int i = 0; i < ALLFACES.Length; i++)
		{
			BlockFacing blockFacing = ALLFACES[i];
			if (blockFacing.normali.Equals(vec))
			{
				return blockFacing;
			}
		}
		return null;
	}

	public static BlockFacing FromVector(double x, double y, double z)
	{
		float num = (float)Math.PI;
		BlockFacing result = null;
		double num2 = GameMath.Sqrt(x * x + y * y + z * z);
		x /= num2;
		y /= num2;
		z /= num2;
		for (int i = 0; i < ALLFACES.Length; i++)
		{
			BlockFacing blockFacing = ALLFACES[i];
			float num3 = (float)Math.Acos((double)blockFacing.Normalf.X * x + (double)blockFacing.Normalf.Y * y + (double)blockFacing.Normalf.Z * z);
			if (num3 < num)
			{
				num = num3;
				result = blockFacing;
			}
		}
		return result;
	}

	public static BlockFacing FromVector(Vec3f vec)
	{
		return FromVector(vec.X, vec.Y, vec.Z);
	}

	public static BlockFacing FromVector(Vec3d vec)
	{
		return FromVector(vec.X, vec.Y, vec.Z);
	}

	public static BlockFacing FromFlag(int flag)
	{
		return flag switch
		{
			1 => NORTH, 
			4 => SOUTH, 
			2 => EAST, 
			8 => WEST, 
			16 => UP, 
			32 => DOWN, 
			_ => null, 
		};
	}

	public static BlockFacing HorizontalFromAngle(float radians)
	{
		int num = GameMath.Mod((int)Math.Round(radians * (180f / (float)Math.PI) / 90f), 4);
		return HORIZONTALS_ANGLEORDER[num];
	}

	public static BlockFacing HorizontalFromYaw(float radians)
	{
		int num = GameMath.Mod((int)Math.Round(radians * (180f / (float)Math.PI) / 90f) - 1, 4);
		return HORIZONTALS_ANGLEORDER[num];
	}

	public static bool FlagContains(byte flag, BlockFacing facing)
	{
		return (flag & facing.flag) > 0;
	}

	public static bool FlagContainsHorizontals(byte flag)
	{
		return (flag & HorizontalFlags) > 0;
	}
}
