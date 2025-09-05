using System;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class ModelIcosahedronUtil
{
	public static uint white = uint.MaxValue;

	public static double X = 0.525731086730957;

	public static double Z = 0.8506507873535156;

	public static double[][] vdata = new double[12][]
	{
		new double[3]
		{
			0.0 - X,
			0.0,
			Z
		},
		new double[3] { X, 0.0, Z },
		new double[3]
		{
			0.0 - X,
			0.0,
			0.0 - Z
		},
		new double[3]
		{
			X,
			0.0,
			0.0 - Z
		},
		new double[3] { 0.0, Z, X },
		new double[3]
		{
			0.0,
			Z,
			0.0 - X
		},
		new double[3]
		{
			0.0,
			0.0 - Z,
			X
		},
		new double[3]
		{
			0.0,
			0.0 - Z,
			0.0 - X
		},
		new double[3] { Z, X, 0.0 },
		new double[3]
		{
			0.0 - Z,
			X,
			0.0
		},
		new double[3]
		{
			Z,
			0.0 - X,
			0.0
		},
		new double[3]
		{
			0.0 - Z,
			0.0 - X,
			0.0
		}
	};

	public static int[][] tindx = new int[20][]
	{
		new int[3] { 0, 4, 1 },
		new int[3] { 0, 9, 4 },
		new int[3] { 9, 5, 4 },
		new int[3] { 4, 5, 8 },
		new int[3] { 4, 8, 1 },
		new int[3] { 8, 10, 1 },
		new int[3] { 8, 3, 10 },
		new int[3] { 5, 3, 8 },
		new int[3] { 5, 2, 3 },
		new int[3] { 2, 7, 3 },
		new int[3] { 7, 10, 3 },
		new int[3] { 7, 6, 10 },
		new int[3] { 7, 11, 6 },
		new int[3] { 11, 0, 6 },
		new int[3] { 0, 1, 6 },
		new int[3] { 6, 1, 10 },
		new int[3] { 9, 0, 11 },
		new int[3] { 9, 11, 2 },
		new int[3] { 9, 2, 5 },
		new int[3] { 7, 2, 11 }
	};

	public static MeshData genIcosahedron(int depth, float radius)
	{
		MeshData meshData = new MeshData(10, 10);
		int index = 0;
		for (int i = 0; i < tindx.Length; i++)
		{
			subdivide(meshData, ref index, vdata[tindx[i][0]], vdata[tindx[i][1]], vdata[tindx[i][2]], depth, radius);
		}
		return meshData;
	}

	private static void subdivide(MeshData modeldata, ref int index, double[] vA0, double[] vB1, double[] vC2, int depth, float radius)
	{
		double[] array = new double[3];
		double[] array2 = new double[3];
		double[] array3 = new double[3];
		if (depth == 0)
		{
			addTriangle(modeldata, ref index, vA0, vB1, vC2, radius);
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			array[i] = (vA0[i] + vB1[i]) / 2.0;
			array2[i] = (vB1[i] + vC2[i]) / 2.0;
			array3[i] = (vC2[i] + vA0[i]) / 2.0;
		}
		double num = mod(array);
		double num2 = mod(array2);
		double num3 = mod(array3);
		for (int i = 0; i < 3; i++)
		{
			array[i] /= num;
			array2[i] /= num2;
			array3[i] /= num3;
		}
		subdivide(modeldata, ref index, vA0, array, array3, depth - 1, radius);
		subdivide(modeldata, ref index, vB1, array2, array, depth - 1, radius);
		subdivide(modeldata, ref index, vC2, array3, array2, depth - 1, radius);
		subdivide(modeldata, ref index, array, array2, array3, depth - 1, radius);
	}

	public static double mod(double[] v)
	{
		return Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
	}

	private static double[] calcTextureMap(double[] vtx)
	{
		double[] array = new double[3];
		array[0] = Math.Sqrt(vtx[0] * vtx[0] + vtx[1] * vtx[1] + vtx[2] * vtx[2]);
		array[1] = Math.Acos(vtx[2] / array[0]);
		array[2] = Math.Atan2(vtx[1], vtx[0]);
		array[1] += Math.PI;
		array[1] /= Math.PI * 2.0;
		array[2] += Math.PI;
		array[2] /= Math.PI * 2.0;
		return array;
	}

	private static void addTriangle(MeshData modeldata, ref int index, double[] v1, double[] v2, double[] v3, float radius)
	{
		double[] array = calcTextureMap(v1);
		modeldata.AddVertex((float)((double)radius * v1[0]), (float)((double)radius * v1[1]), (float)((double)radius * v1[2]), (float)array[1], (float)array[2], (int)white);
		modeldata.AddIndex(index++);
		array = calcTextureMap(v2);
		modeldata.AddVertex((float)((double)radius * v2[0]), (float)((double)radius * v2[1]), (float)((double)radius * v2[2]), (float)array[1], (float)array[2], (int)white);
		modeldata.AddIndex(index++);
		array = calcTextureMap(v3);
		modeldata.AddVertex((float)((double)radius * v3[0]), (float)((double)radius * v3[1]), (float)((double)radius * v3[2]), (float)array[1], (float)array[2], (int)white);
		modeldata.AddIndex(index++);
	}
}
