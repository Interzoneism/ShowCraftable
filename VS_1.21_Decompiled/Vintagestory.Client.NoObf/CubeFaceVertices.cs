using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class CubeFaceVertices
{
	public static Vec3iAndFacingFlags[][] blockFaceVerticesCentered;

	public static FastVec3f[][] blockFaceVertices;

	public static Vec3iAndFacingFlags[][] blockFaceVerticesCenteredDiv2;

	public static FastVec3f[][] blockFaceVerticesDiv2;

	static CubeFaceVertices()
	{
		Vec3iAndFacingFlags.Initialize(34);
		Init(0.5f, 0.5f, out blockFaceVerticesCentered, out blockFaceVertices);
		Init(1f, 0.5f, out blockFaceVerticesCenteredDiv2, out blockFaceVerticesDiv2);
	}

	public static void Init(float horMul, float vertMul, out Vec3iAndFacingFlags[][] bfVerticesCentered, out FastVec3f[][] bfVertices)
	{
		bfVerticesCentered = new Vec3iAndFacingFlags[6][];
		for (int i = 0; i < 6; i++)
		{
			bfVerticesCentered[i] = new Vec3iAndFacingFlags[9];
		}
		int flag = BlockFacing.NORTH.Flag;
		int flag2 = BlockFacing.EAST.Flag;
		int flag3 = BlockFacing.SOUTH.Flag;
		int flag4 = BlockFacing.WEST.Flag;
		int flag5 = BlockFacing.UP.Flag;
		int flag6 = BlockFacing.DOWN.Flag;
		int num = flag2 | flag3;
		int num2 = flag3 | flag4;
		int num3 = flag | flag2;
		int num4 = flag | flag4;
		int num5 = flag3 | flag6;
		int num6 = flag | flag6;
		int num7 = flag3 | flag5;
		int num8 = flag | flag5;
		int num9 = flag4 | flag6;
		int num10 = flag2 | flag6;
		int num11 = flag4 | flag5;
		int num12 = flag2 | flag5;
		bfVerticesCentered[4][8] = new Vec3iAndFacingFlags(0, 1, 0, flag6, flag5);
		bfVerticesCentered[4][0] = new Vec3iAndFacingFlags(0, 1, 1, flag3, flag, num3, num4);
		bfVerticesCentered[4][1] = new Vec3iAndFacingFlags(0, 1, -1, flag, flag3, num, num2);
		bfVerticesCentered[4][2] = new Vec3iAndFacingFlags(1, 1, 0, flag2, flag4, num2, num4);
		bfVerticesCentered[4][3] = new Vec3iAndFacingFlags(-1, 1, 0, flag4, flag2, num, num3);
		bfVerticesCentered[4][4] = new Vec3iAndFacingFlags(1, 1, 1, num, num4);
		bfVerticesCentered[4][5] = new Vec3iAndFacingFlags(-1, 1, 1, num2, num3);
		bfVerticesCentered[4][6] = new Vec3iAndFacingFlags(1, 1, -1, num3, num2);
		bfVerticesCentered[4][7] = new Vec3iAndFacingFlags(-1, 1, -1, num4, num);
		bfVerticesCentered[1][8] = new Vec3iAndFacingFlags(1, 0, 0, flag4, flag2);
		bfVerticesCentered[1][0] = new Vec3iAndFacingFlags(1, 1, 0, flag5, flag6, num6, num5);
		bfVerticesCentered[1][1] = new Vec3iAndFacingFlags(1, -1, 0, flag6, flag5, num8, num7);
		bfVerticesCentered[1][2] = new Vec3iAndFacingFlags(1, 0, 1, flag3, flag, num8, num6);
		bfVerticesCentered[1][3] = new Vec3iAndFacingFlags(1, 0, -1, flag, flag3, num7, num5);
		bfVerticesCentered[1][4] = new Vec3iAndFacingFlags(1, 1, 1, num7, num6);
		bfVerticesCentered[1][5] = new Vec3iAndFacingFlags(1, 1, -1, num8, num5);
		bfVerticesCentered[1][6] = new Vec3iAndFacingFlags(1, -1, 1, num5, num8);
		bfVerticesCentered[1][7] = new Vec3iAndFacingFlags(1, -1, -1, num6, num7);
		bfVerticesCentered[5][8] = new Vec3iAndFacingFlags(0, -1, 0, flag5, flag6);
		bfVerticesCentered[5][0] = new Vec3iAndFacingFlags(0, -1, -1, flag, flag3, num, num2);
		bfVerticesCentered[5][1] = new Vec3iAndFacingFlags(0, -1, 1, flag3, flag, num3, num4);
		bfVerticesCentered[5][2] = new Vec3iAndFacingFlags(1, -1, 0, flag2, flag4, num4, num2);
		bfVerticesCentered[5][3] = new Vec3iAndFacingFlags(-1, -1, 0, flag4, flag2, num3, num);
		bfVerticesCentered[5][4] = new Vec3iAndFacingFlags(1, -1, -1, num3, num2);
		bfVerticesCentered[5][5] = new Vec3iAndFacingFlags(-1, -1, -1, num4, num);
		bfVerticesCentered[5][6] = new Vec3iAndFacingFlags(1, -1, 1, num, num4);
		bfVerticesCentered[5][7] = new Vec3iAndFacingFlags(-1, -1, 1, num2, num3);
		bfVerticesCentered[3][8] = new Vec3iAndFacingFlags(-1, 0, 0, flag2, flag4);
		bfVerticesCentered[3][0] = new Vec3iAndFacingFlags(-1, 1, 0, flag5, flag6, num5, num6);
		bfVerticesCentered[3][1] = new Vec3iAndFacingFlags(-1, -1, 0, flag6, flag5, num7, num8);
		bfVerticesCentered[3][2] = new Vec3iAndFacingFlags(-1, 0, -1, flag, flag3, num7, num5);
		bfVerticesCentered[3][3] = new Vec3iAndFacingFlags(-1, 0, 1, flag3, flag, num8, num6);
		bfVerticesCentered[3][4] = new Vec3iAndFacingFlags(-1, 1, -1, num8, num5);
		bfVerticesCentered[3][5] = new Vec3iAndFacingFlags(-1, 1, 1, num7, num6);
		bfVerticesCentered[3][6] = new Vec3iAndFacingFlags(-1, -1, -1, num6, num7);
		bfVerticesCentered[3][7] = new Vec3iAndFacingFlags(-1, -1, 1, num5, num8);
		bfVerticesCentered[2][8] = new Vec3iAndFacingFlags(0, 0, 1, flag, flag3);
		bfVerticesCentered[2][0] = new Vec3iAndFacingFlags(0, 1, 1, flag5, flag6, num10, num9);
		bfVerticesCentered[2][1] = new Vec3iAndFacingFlags(0, -1, 1, flag6, flag5, num12, num11);
		bfVerticesCentered[2][2] = new Vec3iAndFacingFlags(-1, 0, 1, flag4, flag2, num12, num10);
		bfVerticesCentered[2][3] = new Vec3iAndFacingFlags(1, 0, 1, flag2, flag4, num11, num9);
		bfVerticesCentered[2][4] = new Vec3iAndFacingFlags(-1, 1, 1, num11, num10);
		bfVerticesCentered[2][5] = new Vec3iAndFacingFlags(1, 1, 1, num12, num9);
		bfVerticesCentered[2][6] = new Vec3iAndFacingFlags(-1, -1, 1, num9, num12);
		bfVerticesCentered[2][7] = new Vec3iAndFacingFlags(1, -1, 1, num10, num11);
		bfVerticesCentered[0][8] = new Vec3iAndFacingFlags(0, 0, -1, flag3, flag);
		bfVerticesCentered[0][0] = new Vec3iAndFacingFlags(0, 1, -1, flag5, flag6, num9, num10);
		bfVerticesCentered[0][1] = new Vec3iAndFacingFlags(0, -1, -1, flag6, flag5, num11, num12);
		bfVerticesCentered[0][2] = new Vec3iAndFacingFlags(1, 0, -1, flag2, flag4, num11, num9);
		bfVerticesCentered[0][3] = new Vec3iAndFacingFlags(-1, 0, -1, flag4, flag2, num12, num10);
		bfVerticesCentered[0][4] = new Vec3iAndFacingFlags(1, 1, -1, num12, num9);
		bfVerticesCentered[0][5] = new Vec3iAndFacingFlags(-1, 1, -1, num11, num10);
		bfVerticesCentered[0][6] = new Vec3iAndFacingFlags(1, -1, -1, num10, num11);
		bfVerticesCentered[0][7] = new Vec3iAndFacingFlags(-1, -1, -1, num9, num12);
		bfVertices = new FastVec3f[6][];
		for (int j = 0; j < bfVerticesCentered.Length; j++)
		{
			bfVertices[j] = new FastVec3f[bfVerticesCentered[j].Length];
			for (int k = 0; k < bfVerticesCentered[j].Length; k++)
			{
				Vec3iAndFacingFlags vec3iAndFacingFlags = bfVerticesCentered[j][k];
				bfVertices[j][k] = new FastVec3f((float)(vec3iAndFacingFlags.X + 1) * horMul, (float)(vec3iAndFacingFlags.Y + 1) * vertMul, (float)(vec3iAndFacingFlags.Z + 1) * horMul);
			}
		}
	}
}
