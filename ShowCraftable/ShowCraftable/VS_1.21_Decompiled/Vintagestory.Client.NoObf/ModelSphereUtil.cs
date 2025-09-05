using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ModelSphereUtil
{
	private static float GetPi()
	{
		return 3141592f / 1000000f;
	}

	public static MeshData GetSphereModelData(float radius, float height, int segments, int rings)
	{
		int num = 0;
		float[] array = new float[rings * segments * 3];
		float[] array2 = new float[rings * segments * 2];
		byte[] array3 = new byte[rings * segments * 4];
		for (int i = 0; i < rings; i++)
		{
			float num2 = i;
			float value = num2 / (float)(rings - 1) * GetPi();
			for (int j = 0; j < segments; j++)
			{
				float num3 = j;
				float value2 = num3 / (float)(segments - 1) * 2f * GetPi();
				float num4 = radius * GameMath.Sin(value) * GameMath.Cos(value2);
				float num5 = height * GameMath.Cos(value);
				float num6 = radius * GameMath.Sin(value) * GameMath.Sin(value2);
				float num7 = num3 / (float)(segments - 1);
				float num8 = num2 / (float)(rings - 1);
				array[num * 3] = num4;
				array[num * 3 + 1] = num5;
				array[num * 3 + 2] = num6;
				array2[num * 2] = num7;
				array2[num * 2 + 1] = num8;
				array3[num * 4] = byte.MaxValue;
				array3[num * 4 + 1] = byte.MaxValue;
				array3[num * 4 + 2] = byte.MaxValue;
				array3[num * 4 + 3] = byte.MaxValue;
				num++;
			}
		}
		MeshData meshData = new MeshData();
		meshData.SetVerticesCount(segments * rings);
		meshData.SetIndicesCount(segments * rings * 6);
		meshData.SetXyz(array);
		meshData.SetUv(array2);
		meshData.SetRgba(array3);
		meshData.SetIndices(CalculateElements(radius, height, segments, rings));
		return meshData;
	}

	public static int[] CalculateElements(float radius, float height, int segments, int rings)
	{
		int num = 0;
		int[] array = new int[segments * rings * 6];
		for (int i = 0; i < rings - 1; i++)
		{
			for (int j = 0; j < segments - 1; j++)
			{
				array[num++] = i * segments + j;
				array[num++] = (i + 1) * segments + j;
				array[num++] = (i + 1) * segments + j + 1;
				array[num++] = (i + 1) * segments + j + 1;
				array[num++] = i * segments + j + 1;
				array[num++] = i * segments + j;
			}
		}
		return array;
	}
}
