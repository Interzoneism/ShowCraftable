using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public static class MeshUtil
{
	public static void SetWindFlag(this MeshData sourceMesh, float waveFlagMinY = 0.5625f, int flag = 67108864)
	{
		int verticesCount = sourceMesh.VerticesCount;
		float[] xyz = sourceMesh.xyz;
		int[] flags = sourceMesh.Flags;
		for (int i = 0; i < verticesCount; i++)
		{
			if (xyz[i * 3 + 1] > waveFlagMinY)
			{
				flags[i] |= flag;
			}
			else
			{
				flags[i] &= -503316481;
			}
		}
	}

	public static void ClearWindFlags(this MeshData sourceMesh)
	{
		int verticesCount = sourceMesh.VerticesCount;
		int[] flags = sourceMesh.Flags;
		for (int i = 0; i < verticesCount; i++)
		{
			flags[i] &= -503316481;
		}
	}

	public static void ToggleWindModeSetWindData(this MeshData sourceMesh, int leavesNoShearTileSide, bool enableWind, int groundOffsetTop)
	{
		int num = 33554431;
		int verticesCount = sourceMesh.VerticesCount;
		int[] flags = sourceMesh.Flags;
		if (!enableWind)
		{
			for (int i = 0; i < verticesCount; i++)
			{
				flags[i] &= num;
			}
			return;
		}
		float[] xyz = sourceMesh.xyz;
		for (int j = 0; j < verticesCount; j++)
		{
			int num2 = (int)(sourceMesh.xyz[j * 3 + 1] - 1.5f) >> 1;
			if (leavesNoShearTileSide != 0)
			{
				int num3 = (int)(xyz[j * 3] - 1.5f) >> 1;
				int num4 = (int)(xyz[j * 3 + 2] - 1.5f) >> 1;
				int num5 = (1 << 4 - num2) | (4 + num4 * 3) | (2 - num3 * 6);
				if ((leavesNoShearTileSide & num5) != 0)
				{
					VertexFlags.ReplaceWindData(ref flags[j], 0);
					continue;
				}
			}
			int windData = ((groundOffsetTop == 8) ? 7 : (groundOffsetTop + num2));
			VertexFlags.ReplaceWindData(ref flags[j], windData);
		}
	}
}
