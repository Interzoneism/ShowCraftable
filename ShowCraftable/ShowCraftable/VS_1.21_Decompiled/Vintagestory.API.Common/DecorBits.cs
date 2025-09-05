using System.Runtime.CompilerServices;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public struct DecorBits
{
	private int faceAndSubposition;

	private const int Index3dMask = 32767;

	private const int mask3Bits = 224;

	private const int mask5Bits = 31;

	public const int maskRotationData = 7;

	public int Face => faceAndSubposition % 6;

	public int SubPosition => (faceAndSubposition / 6) & 0xFFF;

	public int Rotation
	{
		get
		{
			return faceAndSubposition / 6 >> 12;
		}
		set
		{
			int num = SubPosition + (value << 12);
			faceAndSubposition = faceAndSubposition % 6 + num * 6;
		}
	}

	public static implicit operator int(DecorBits a)
	{
		return a.faceAndSubposition;
	}

	public DecorBits(int value)
	{
		faceAndSubposition = value;
	}

	public DecorBits(BlockFacing face)
	{
		faceAndSubposition = face.Index;
	}

	public DecorBits(BlockFacing face, int vx, int vy, int vz)
	{
		int num = 0;
		switch (face.Index)
		{
		case 0:
			num = 15 - vx + vy * 16;
			break;
		case 1:
			num = 15 - vz + vy * 16;
			break;
		case 2:
			num = vx + vy * 16;
			break;
		case 3:
			num = vz + vy * 16;
			break;
		case 4:
			num = vx + vz * 16;
			break;
		case 5:
			num = vx + (15 - vz) * 16;
			break;
		}
		faceAndSubposition = face.Index + 6 * (1 + num);
	}

	public static int FaceAndSubpositionToIndex(int faceAndSubposition)
	{
		int num = faceAndSubposition / 6;
		int num2 = (num >> 12) & 7;
		num &= 0xFFF;
		int num3 = faceAndSubposition % 6;
		if (num < 256)
		{
			faceAndSubposition = num3 + (num & 0x1F) * 6;
			num &= 0xE0;
		}
		else
		{
			faceAndSubposition = num3 + 192;
			num = 224;
		}
		return (faceAndSubposition << 24) + (num + num2 << 16);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FaceAndSubpositionFromIndex(int packedIndex)
	{
		int num = packedIndex >> 16;
		return ((packedIndex >> 24) & 0xFF) + ((num & 0xE0) + ((num & 7) << 12)) * 6;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FaceToIndex(BlockFacing face)
	{
		return face.Index << 24;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FaceFromIndex(int packedIndex)
	{
		return ((packedIndex >> 24) & 0xFF) % 6;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Index3dFromIndex(int packedIndex)
	{
		return packedIndex & 0x7FFF;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int SubpositionFromIndex(int packedIndex)
	{
		return ((packedIndex >> 24) & 0xFF) / 6 + ((packedIndex >> 16) & 0xE0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int RotationFromIndex(int packedIndex)
	{
		return (packedIndex >> 16) & 7;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BlockFacing FacingFromIndex(int packedIndex)
	{
		return BlockFacing.ALLFACES[FaceFromIndex(packedIndex)];
	}
}
