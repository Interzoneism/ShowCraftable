using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

public class MeshData
{
	public delegate bool MeshDataFilterDelegate(int faceIndex);

	public static MeshDataRecycler Recycler;

	public const int StandardVerticesPerFace = 4;

	public const int StandardIndicesPerFace = 6;

	public const int BaseSizeInBytes = 34;

	public int[] TextureIds = Array.Empty<int>();

	public float[] xyz;

	public int[] Flags;

	public bool HasAnyWindModeSet;

	public int[] Normals;

	public float[] Uv;

	public byte[] Rgba;

	public int[] Indices;

	public byte[] TextureIndices;

	public CustomMeshDataPartFloat CustomFloats;

	public CustomMeshDataPartInt CustomInts;

	public CustomMeshDataPartShort CustomShorts;

	public CustomMeshDataPartByte CustomBytes;

	public bool XyzInstanced;

	public bool UvInstanced;

	public bool RgbaInstanced;

	public bool Rgba2Instanced;

	public bool IndicesInstanced;

	public bool FlagsInstanced;

	public bool XyzStatic = true;

	public bool UvStatic = true;

	public bool RgbaStatic = true;

	public bool Rgba2Static = true;

	public bool IndicesStatic = true;

	public bool FlagsStatic = true;

	public int XyzOffset;

	public int UvOffset;

	public int RgbaOffset;

	public int Rgba2Offset;

	public int FlagsOffset;

	public int NormalsOffset;

	public int IndicesOffset;

	public EnumDrawMode mode;

	public int NormalsCount;

	public int VerticesCount;

	public int IndicesCount;

	public int VerticesMax;

	public int IndicesMax;

	public byte[] XyzFaces;

	public int XyzFacesCount;

	public int TextureIndicesCount;

	public int IndicesPerFace = 6;

	public int VerticesPerFace = 4;

	public byte[] ClimateColorMapIds;

	public byte[] SeasonColorMapIds;

	public bool[] FrostableBits;

	public short[] RenderPassesAndExtraBits;

	public int ColorMapIdsCount;

	public int RenderPassCount;

	public bool Recyclable;

	public long RecyclingTime;

	public const int XyzSize = 12;

	public const int NormalSize = 4;

	public const int RgbaSize = 4;

	public const int UvSize = 8;

	public const int IndexSize = 4;

	public const int FlagsSize = 4;

	[Obsolete("Use RenderPassesAndExtraBits instead")]
	public short[] RenderPasses => RenderPassesAndExtraBits;

	public int XyzCount => VerticesCount * 3;

	public int RgbaCount => VerticesCount * 4;

	public int Rgba2Count => VerticesCount * 4;

	public int FlagsCount => VerticesCount;

	public int UvCount => VerticesCount * 2;

	public int GetVerticesCount()
	{
		return VerticesCount;
	}

	public void SetVerticesCount(int value)
	{
		VerticesCount = value;
	}

	public int GetIndicesCount()
	{
		return IndicesCount;
	}

	public void SetIndicesCount(int value)
	{
		IndicesCount = value;
	}

	public float[] GetXyz()
	{
		return xyz;
	}

	public void SetXyz(float[] p)
	{
		xyz = p;
	}

	public byte[] GetRgba()
	{
		return Rgba;
	}

	public void SetRgba(byte[] p)
	{
		Rgba = p;
	}

	public float[] GetUv()
	{
		return Uv;
	}

	public void SetUv(float[] p)
	{
		Uv = p;
	}

	public int[] GetIndices()
	{
		return Indices;
	}

	public void SetIndices(int[] p)
	{
		Indices = p;
	}

	public EnumDrawMode GetMode()
	{
		return mode;
	}

	public void SetMode(EnumDrawMode p)
	{
		mode = p;
	}

	public MeshData Translate(Vec3f offset)
	{
		Translate(offset.X, offset.Y, offset.Z);
		return this;
	}

	public MeshData Translate(float x, float y, float z)
	{
		for (int i = 0; i < VerticesCount; i++)
		{
			xyz[i * 3] += x;
			xyz[i * 3 + 1] += y;
			xyz[i * 3 + 2] += z;
		}
		return this;
	}

	public MeshData Rotate(Vec3f origin, float radX, float radY, float radZ)
	{
		Span<float> matrix = stackalloc float[16];
		Mat4f.RotateXYZ(matrix, radX, radY, radZ);
		return MatrixTransform(matrix, new float[4], origin);
	}

	public MeshData Scale(Vec3f origin, float scaleX, float scaleY, float scaleZ)
	{
		for (int i = 0; i < VerticesCount; i++)
		{
			int num = i * 3;
			float num2 = xyz[num] - origin.X;
			float num3 = xyz[num + 1] - origin.Y;
			float num4 = xyz[num + 2] - origin.Z;
			xyz[num] = origin.X + scaleX * num2;
			xyz[num + 1] = origin.Y + scaleY * num3;
			xyz[num + 2] = origin.Z + scaleZ * num4;
		}
		return this;
	}

	public MeshData ModelTransform(ModelTransform transform)
	{
		float[] array = Mat4f.Create();
		float x = transform.Translation.X + transform.Origin.X;
		float y = transform.Translation.Y + transform.Origin.Y;
		float z = transform.Translation.Z + transform.Origin.Z;
		Mat4f.Translate(array, array, x, y, z);
		Mat4f.RotateX(array, array, transform.Rotation.X * ((float)Math.PI / 180f));
		Mat4f.RotateY(array, array, transform.Rotation.Y * ((float)Math.PI / 180f));
		Mat4f.RotateZ(array, array, transform.Rotation.Z * ((float)Math.PI / 180f));
		Mat4f.Scale(array, array, transform.ScaleXYZ.X, transform.ScaleXYZ.Y, transform.ScaleXYZ.Z);
		Mat4f.Translate(array, array, 0f - transform.Origin.X, 0f - transform.Origin.Y, 0f - transform.Origin.Z);
		MatrixTransform(array);
		return this;
	}

	public MeshData MatrixTransform(float[] matrix)
	{
		return MatrixTransform(matrix, new float[4]);
	}

	public MeshData MatrixTransform(float[] matrix, float[] vec, Vec3f origin = null)
	{
		return MatrixTransform((Span<float>)matrix, vec, origin);
	}

	public MeshData MatrixTransform(Span<float> matrix, float[] vec, Vec3f origin = null)
	{
		float[] array = xyz;
		if (origin == null)
		{
			for (int i = 0; i < VerticesCount; i++)
			{
				Mat4f.MulWithVec3_Position(matrix, array, array, i * 3);
			}
		}
		else
		{
			for (int j = 0; j < VerticesCount; j++)
			{
				Mat4f.MulWithVec3_Position_WithOrigin(matrix, array, array, j * 3, origin);
			}
		}
		if (Normals != null)
		{
			int[] normals = Normals;
			for (int k = 0; k < VerticesCount; k++)
			{
				NormalUtil.FromPackedNormal(normals[k], ref vec);
				Mat4f.MulWithVec4(matrix, vec, vec);
				normals[k] = NormalUtil.PackNormal(vec);
			}
		}
		if (XyzFaces != null)
		{
			byte[] xyzFaces = XyzFaces;
			for (int l = 0; l < xyzFaces.Length; l++)
			{
				byte b = xyzFaces[l];
				if (b != 0)
				{
					Vec3f normalf = BlockFacing.ALLFACES[b - 1].Normalf;
					xyzFaces[l] = Mat4f.MulWithVec3_BlockFacing(matrix, normalf).MeshDataIndex;
				}
			}
		}
		if (Flags != null)
		{
			int[] flags = Flags;
			for (int m = 0; m < flags.Length; m++)
			{
				VertexFlags.UnpackNormal(flags[m], vec);
				Mat4f.MulWithVec3(matrix, vec, vec);
				float num = GameMath.RootSumOfSquares(vec[0], vec[1], vec[2]);
				flags[m] = (flags[m] & -33546241) | VertexFlags.PackNormal(vec[0] / num, vec[1] / num, vec[2] / num);
			}
		}
		return this;
	}

	public MeshData MatrixTransform(double[] matrix)
	{
		if (Mat4d.IsTranslationOnly(matrix))
		{
			Translate((float)matrix[12], (float)matrix[13], (float)matrix[14]);
			return this;
		}
		double[] toFill = new double[4];
		float[] array = xyz;
		int[] normals = Normals;
		for (int i = 0; i < VerticesCount; i++)
		{
			float num = array[i * 3];
			float num2 = array[i * 3 + 1];
			float num3 = array[i * 3 + 2];
			array[i * 3] = (float)(matrix[0] * (double)num + matrix[4] * (double)num2 + matrix[8] * (double)num3 + matrix[12]);
			array[i * 3 + 1] = (float)(matrix[1] * (double)num + matrix[5] * (double)num2 + matrix[9] * (double)num3 + matrix[13]);
			array[i * 3 + 2] = (float)(matrix[2] * (double)num + matrix[6] * (double)num2 + matrix[10] * (double)num3 + matrix[14]);
			if (normals != null)
			{
				NormalUtil.FromPackedNormal(normals[i], ref toFill);
				double[] normal = Mat4d.MulWithVec4(matrix, toFill);
				normals[i] = NormalUtil.PackNormal(normal);
			}
		}
		if (XyzFaces != null)
		{
			byte[] xyzFaces = XyzFaces;
			for (int j = 0; j < xyzFaces.Length; j++)
			{
				byte b = xyzFaces[j];
				if (b != 0)
				{
					Vec3d normald = BlockFacing.ALLFACES[b - 1].Normald;
					double x = matrix[0] * normald.X + matrix[4] * normald.Y + matrix[8] * normald.Z;
					double y = matrix[1] * normald.X + matrix[5] * normald.Y + matrix[9] * normald.Z;
					double z = matrix[2] * normald.X + matrix[6] * normald.Y + matrix[10] * normald.Z;
					BlockFacing blockFacing = BlockFacing.FromVector(x, y, z);
					xyzFaces[j] = blockFacing.MeshDataIndex;
				}
			}
		}
		if (Flags != null)
		{
			int[] flags = Flags;
			for (int k = 0; k < flags.Length; k++)
			{
				VertexFlags.UnpackNormal(flags[k], toFill);
				double x2 = matrix[0] * toFill[0] + matrix[4] * toFill[1] + matrix[8] * toFill[2];
				double y2 = matrix[1] * toFill[0] + matrix[5] * toFill[1] + matrix[9] * toFill[2];
				double z2 = matrix[2] * toFill[0] + matrix[6] * toFill[1] + matrix[10] * toFill[2];
				flags[k] = (flags[k] & -33546241) | VertexFlags.PackNormal(x2, y2, z2);
			}
		}
		return this;
	}

	public MeshData(bool initialiseArrays = true)
	{
		if (initialiseArrays)
		{
			XyzFaces = Array.Empty<byte>();
			ClimateColorMapIds = Array.Empty<byte>();
			SeasonColorMapIds = Array.Empty<byte>();
			RenderPassesAndExtraBits = Array.Empty<short>();
		}
	}

	public MeshData(int capacityVertices, int capacityIndices, bool withNormals = false, bool withUv = true, bool withRgba = true, bool withFlags = true)
	{
		XyzFaces = Array.Empty<byte>();
		ClimateColorMapIds = Array.Empty<byte>();
		SeasonColorMapIds = Array.Empty<byte>();
		RenderPassesAndExtraBits = Array.Empty<short>();
		xyz = new float[capacityVertices * 3];
		if (withNormals)
		{
			Normals = new int[capacityVertices];
		}
		if (withUv)
		{
			Uv = new float[capacityVertices * 2];
			TextureIndices = new byte[capacityVertices / VerticesPerFace];
		}
		if (withRgba)
		{
			Rgba = new byte[capacityVertices * 4];
		}
		if (withFlags)
		{
			Flags = new int[capacityVertices];
		}
		Indices = new int[capacityIndices];
		IndicesMax = capacityIndices;
		VerticesMax = capacityVertices;
	}

	public MeshData(int capacity)
	{
		xyz = new float[capacity * 3];
		Uv = new float[capacity * 2];
		Rgba = new byte[capacity * 4];
		Flags = new int[capacity];
		VerticesMax = capacity;
		int num = capacity * 6 / 4;
		Indices = new int[num];
		IndicesMax = num;
	}

	public MeshData WithColorMaps()
	{
		SeasonColorMapIds = new byte[VerticesMax / 4];
		ClimateColorMapIds = new byte[VerticesMax / 4];
		return this;
	}

	public MeshData WithXyzFaces()
	{
		XyzFaces = new byte[VerticesMax / 4];
		return this;
	}

	public MeshData WithRenderpasses()
	{
		RenderPassesAndExtraBits = new short[VerticesMax / 4];
		return this;
	}

	public MeshData WithNormals()
	{
		Normals = new int[VerticesMax];
		return this;
	}

	public void AddMeshData(MeshData data, EnumChunkRenderPass filterByRenderPass)
	{
		int renderPassInt = (int)filterByRenderPass;
		AddMeshData(data, (int i) => data.RenderPassesAndExtraBits[i] != renderPassInt && (data.RenderPassesAndExtraBits[i] != -1 || filterByRenderPass != EnumChunkRenderPass.Opaque));
	}

	public void AddMeshData(MeshData data, MeshDataFilterDelegate dele = null)
	{
		int num = 0;
		int verticesPerFace = VerticesPerFace;
		int indicesPerFace = IndicesPerFace;
		float[] array = xyz;
		int[] normals = Normals;
		float[] uv = Uv;
		byte[] rgba = Rgba;
		int[] flags = Flags;
		float[] array2 = data.xyz;
		int[] normals2 = data.Normals;
		float[] uv2 = data.Uv;
		byte[] rgba2 = data.Rgba;
		int[] flags2 = data.Flags;
		int[] indices = data.Indices;
		int num2 = ((data.CustomInts != null) ? ((data.CustomInts.InterleaveStride == 0) ? data.CustomInts.InterleaveSizes[0] : data.CustomInts.InterleaveStride) : 0);
		int num3 = ((data.CustomFloats != null) ? ((data.CustomFloats.InterleaveStride == 0) ? data.CustomFloats.InterleaveSizes[0] : data.CustomFloats.InterleaveStride) : 0);
		int num4 = ((data.CustomShorts != null) ? ((data.CustomShorts.InterleaveStride == 0) ? data.CustomShorts.InterleaveSizes[0] : data.CustomShorts.InterleaveStride) : 0);
		int num5 = ((data.CustomBytes != null) ? ((data.CustomBytes.InterleaveStride == 0) ? data.CustomBytes.InterleaveSizes[0] : data.CustomBytes.InterleaveStride) : 0);
		int num6 = data.VerticesCount / verticesPerFace;
		for (int i = 0; i < num6; i++)
		{
			if (dele != null && !dele(i))
			{
				num += indicesPerFace;
				continue;
			}
			int verticesCount = VerticesCount;
			if (uv != null)
			{
				AddTextureId(data.TextureIds[data.TextureIndices[i]]);
			}
			for (int j = 0; j < verticesPerFace; j++)
			{
				int num7 = i * verticesPerFace + j;
				if (VerticesCount >= VerticesMax)
				{
					GrowVertexBuffer();
					GrowNormalsBuffer();
					array = xyz;
					normals = Normals;
					uv = Uv;
					rgba = Rgba;
					flags = Flags;
				}
				int xyzCount = XyzCount;
				array[xyzCount] = array2[num7 * 3];
				array[xyzCount + 1] = array2[num7 * 3 + 1];
				array[xyzCount + 2] = array2[num7 * 3 + 2];
				if (normals != null)
				{
					normals[VerticesCount] = normals2[num7];
				}
				if (uv != null)
				{
					int uvCount = UvCount;
					uv[uvCount] = uv2[num7 * 2];
					uv[uvCount + 1] = uv2[num7 * 2 + 1];
				}
				if (rgba != null)
				{
					int rgbaCount = RgbaCount;
					rgba[rgbaCount] = rgba2[num7 * 4];
					rgba[rgbaCount + 1] = rgba2[num7 * 4 + 1];
					rgba[rgbaCount + 2] = rgba2[num7 * 4 + 2];
					rgba[rgbaCount + 3] = rgba2[num7 * 4 + 3];
				}
				if (flags != null)
				{
					flags[FlagsCount] = flags2[num7];
				}
				if (CustomInts != null && data.CustomInts != null)
				{
					CustomMeshDataPartInt customInts = CustomInts;
					CustomMeshDataPartInt customInts2 = data.CustomInts;
					for (int k = 0; k < num2; k++)
					{
						customInts.Add(customInts2.Values[num7 / num2 + k]);
					}
				}
				if (CustomFloats != null && data.CustomFloats != null)
				{
					CustomMeshDataPartFloat customFloats = CustomFloats;
					CustomMeshDataPartFloat customFloats2 = data.CustomFloats;
					for (int l = 0; l < num3; l++)
					{
						customFloats.Add(customFloats2.Values[num7 / num3 + l]);
					}
				}
				if (CustomShorts != null && data.CustomShorts != null)
				{
					CustomMeshDataPartShort customShorts = CustomShorts;
					CustomMeshDataPartShort customShorts2 = data.CustomShorts;
					for (int m = 0; m < num4; m++)
					{
						customShorts.Add(customShorts2.Values[num7 / num4 + m]);
					}
				}
				if (CustomBytes != null && data.CustomBytes != null)
				{
					CustomMeshDataPartByte customBytes = CustomBytes;
					CustomMeshDataPartByte customBytes2 = data.CustomBytes;
					for (int n = 0; n < num5; n++)
					{
						customBytes.Add(customBytes2.Values[num7 / num5 + n]);
					}
				}
				VerticesCount++;
			}
			for (int num8 = 0; num8 < indicesPerFace; num8++)
			{
				int num9 = i * indicesPerFace + num8;
				AddIndex(verticesCount - (i - num / indicesPerFace) * verticesPerFace + indices[num9] - 2 * num / 3);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte getTextureIndex(int textureId)
	{
		for (byte b = 0; b < TextureIds.Length; b++)
		{
			if (TextureIds[b] == textureId)
			{
				return b;
			}
		}
		TextureIds = TextureIds.Append(textureId);
		return (byte)(TextureIds.Length - 1);
	}

	public void AddMeshData(MeshData sourceMesh)
	{
		float[] array = xyz;
		int[] normals = Normals;
		float[] uv = Uv;
		byte[] rgba = Rgba;
		int[] flags = Flags;
		float[] array2 = sourceMesh.xyz;
		int[] normals2 = sourceMesh.Normals;
		float[] uv2 = sourceMesh.Uv;
		byte[] rgba2 = sourceMesh.Rgba;
		int[] flags2 = sourceMesh.Flags;
		for (int i = 0; i < sourceMesh.VerticesCount; i++)
		{
			if (VerticesCount >= VerticesMax)
			{
				GrowVertexBuffer();
				GrowNormalsBuffer();
				array = xyz;
				normals = Normals;
				uv = Uv;
				rgba = Rgba;
				flags = Flags;
			}
			int xyzCount = XyzCount;
			array[xyzCount] = array2[i * 3];
			array[xyzCount + 1] = array2[i * 3 + 1];
			array[xyzCount + 2] = array2[i * 3 + 2];
			if (normals != null)
			{
				normals[VerticesCount] = normals2[i];
			}
			if (uv != null)
			{
				int uvCount = UvCount;
				uv[uvCount] = uv2[i * 2];
				uv[uvCount + 1] = uv2[i * 2 + 1];
			}
			if (rgba != null)
			{
				int rgbaCount = RgbaCount;
				rgba[rgbaCount] = rgba2[i * 4];
				rgba[rgbaCount + 1] = rgba2[i * 4 + 1];
				rgba[rgbaCount + 2] = rgba2[i * 4 + 2];
				rgba[rgbaCount + 3] = rgba2[i * 4 + 3];
			}
			if (flags != null && flags2 != null)
			{
				flags[VerticesCount] = flags2[i];
			}
			VerticesCount++;
		}
		addMeshDataEtc(sourceMesh);
	}

	public void AddMeshData(MeshData sourceMesh, float xOffset, float yOffset, float zOffset)
	{
		float[] array = xyz;
		int[] normals = Normals;
		float[] uv = Uv;
		byte[] rgba = Rgba;
		int[] flags = Flags;
		float[] array2 = sourceMesh.xyz;
		int[] normals2 = sourceMesh.Normals;
		float[] uv2 = sourceMesh.Uv;
		byte[] rgba2 = sourceMesh.Rgba;
		int[] flags2 = sourceMesh.Flags;
		for (int i = 0; i < sourceMesh.VerticesCount; i++)
		{
			if (VerticesCount >= VerticesMax)
			{
				GrowVertexBuffer();
				GrowNormalsBuffer();
				array = xyz;
				normals = Normals;
				uv = Uv;
				rgba = Rgba;
				flags = Flags;
			}
			int xyzCount = XyzCount;
			array[xyzCount] = array2[i * 3] + xOffset;
			array[xyzCount + 1] = array2[i * 3 + 1] + yOffset;
			array[xyzCount + 2] = array2[i * 3 + 2] + zOffset;
			if (normals != null)
			{
				normals[VerticesCount] = normals2[i];
			}
			if (uv != null)
			{
				int uvCount = UvCount;
				uv[uvCount] = uv2[i * 2];
				uv[uvCount + 1] = uv2[i * 2 + 1];
			}
			if (rgba != null)
			{
				int rgbaCount = RgbaCount;
				rgba[rgbaCount] = rgba2[i * 4];
				rgba[rgbaCount + 1] = rgba2[i * 4 + 1];
				rgba[rgbaCount + 2] = rgba2[i * 4 + 2];
				rgba[rgbaCount + 3] = rgba2[i * 4 + 3];
			}
			if (flags != null && flags2 != null)
			{
				flags[VerticesCount] = flags2[i];
			}
			VerticesCount++;
		}
		addMeshDataEtc(sourceMesh);
	}

	private void addMeshDataEtc(MeshData sourceMesh)
	{
		int xyzFacesCount = sourceMesh.XyzFacesCount;
		byte[] xyzFaces = sourceMesh.XyzFaces;
		for (int i = 0; i < xyzFacesCount; i++)
		{
			AddXyzFace(xyzFaces[i]);
		}
		int textureIndicesCount = sourceMesh.TextureIndicesCount;
		byte[] textureIndices = sourceMesh.TextureIndices;
		int[] textureIds = sourceMesh.TextureIds;
		for (int j = 0; j < textureIndicesCount; j++)
		{
			AddTextureId(textureIds[textureIndices[j]]);
		}
		int num = ((IndicesCount > 0) ? ((mode == EnumDrawMode.Triangles) ? (Indices[IndicesCount - 1] + 1) : (Indices[IndicesCount - 2] + 1)) : 0);
		int indicesCount = sourceMesh.IndicesCount;
		int[] indices = sourceMesh.Indices;
		for (int k = 0; k < indicesCount; k++)
		{
			AddIndex(num + indices[k]);
		}
		int colorMapIdsCount = sourceMesh.ColorMapIdsCount;
		for (int l = 0; l < colorMapIdsCount; l++)
		{
			AddColorMapIndex(sourceMesh.ClimateColorMapIds[l], sourceMesh.SeasonColorMapIds[l]);
		}
		int renderPassCount = sourceMesh.RenderPassCount;
		short[] renderPassesAndExtraBits = sourceMesh.RenderPassesAndExtraBits;
		for (int m = 0; m < renderPassCount; m++)
		{
			AddRenderPass(renderPassesAndExtraBits[m]);
		}
		if (CustomInts != null)
		{
			if (sourceMesh.CustomInts != null)
			{
				int count = sourceMesh.CustomInts.Count;
				int[] values = sourceMesh.CustomInts.Values;
				CustomMeshDataPartInt customInts = CustomInts;
				for (int n = 0; n < count; n++)
				{
					customInts.Add(values[n]);
				}
			}
			else if (CustomInts.Values.Length < VerticesCount)
			{
				Array.Resize(ref CustomInts.Values, VerticesCount);
			}
		}
		if (CustomFloats != null)
		{
			if (sourceMesh.CustomFloats != null)
			{
				int count2 = sourceMesh.CustomFloats.Count;
				float[] values2 = sourceMesh.CustomFloats.Values;
				CustomMeshDataPartFloat customFloats = CustomFloats;
				for (int num2 = 0; num2 < count2; num2++)
				{
					customFloats.Add(values2[num2]);
				}
			}
			else if (CustomFloats.Values.Length < VerticesCount)
			{
				Array.Resize(ref CustomFloats.Values, VerticesCount);
			}
		}
		if (CustomShorts != null)
		{
			if (sourceMesh.CustomShorts != null)
			{
				int count3 = sourceMesh.CustomShorts.Count;
				short[] values3 = sourceMesh.CustomShorts.Values;
				CustomMeshDataPartShort customShorts = CustomShorts;
				for (int num3 = 0; num3 < count3; num3++)
				{
					customShorts.Add(values3[num3]);
				}
			}
			else if (CustomShorts.Values.Length < VerticesCount)
			{
				Array.Resize(ref CustomShorts.Values, VerticesCount);
			}
		}
		if (CustomBytes == null)
		{
			return;
		}
		if (sourceMesh.CustomBytes != null)
		{
			int count4 = sourceMesh.CustomBytes.Count;
			byte[] values4 = sourceMesh.CustomBytes.Values;
			CustomMeshDataPartByte customBytes = CustomBytes;
			for (int num4 = 0; num4 < count4; num4++)
			{
				customBytes.Add(values4[num4]);
			}
		}
		else if (CustomBytes.Values.Length < VerticesCount)
		{
			Array.Resize(ref CustomBytes.Values, VerticesCount);
		}
	}

	public void RemoveIndex()
	{
		if (IndicesCount > 0)
		{
			IndicesCount--;
		}
	}

	public void RemoveVertex()
	{
		if (VerticesCount > 0)
		{
			VerticesCount--;
		}
	}

	public void RemoveVertices(int count)
	{
		VerticesCount = Math.Max(0, VerticesCount - count);
	}

	public unsafe void AddVertexSkipTex(float x, float y, float z, int color = -1)
	{
		int verticesCount = VerticesCount;
		if (verticesCount >= VerticesMax)
		{
			GrowVertexBuffer();
		}
		float[] array = xyz;
		int num = verticesCount * 3;
		array[num] = x;
		array[num + 1] = y;
		array[num + 2] = z;
		fixed (byte* rgba = Rgba)
		{
			int* ptr = (int*)rgba;
			ptr[verticesCount] = color;
		}
		VerticesCount = verticesCount + 1;
	}

	public void AddVertex(float x, float y, float z, float u, float v)
	{
		int verticesCount = VerticesCount;
		if (verticesCount >= VerticesMax)
		{
			GrowVertexBuffer();
		}
		float[] array = xyz;
		float[] uv = Uv;
		int num = verticesCount * 3;
		array[num] = x;
		array[num + 1] = y;
		array[num + 2] = z;
		int num2 = verticesCount * 2;
		uv[num2] = u;
		uv[num2 + 1] = v;
		VerticesCount = verticesCount + 1;
	}

	public void AddVertex(float x, float y, float z, float u, float v, int color)
	{
		AddWithFlagsVertex(x, y, z, u, v, color, 0);
	}

	public void AddVertex(float x, float y, float z, float u, float v, byte[] color)
	{
		int verticesCount = VerticesCount;
		if (verticesCount >= VerticesMax)
		{
			GrowVertexBuffer();
		}
		float[] array = xyz;
		float[] uv = Uv;
		byte[] rgba = Rgba;
		int num = verticesCount * 3;
		array[num] = x;
		array[num + 1] = y;
		array[num + 2] = z;
		int num2 = verticesCount * 2;
		uv[num2] = u;
		uv[num2 + 1] = v;
		int num3 = verticesCount * 4;
		rgba[num3] = color[0];
		rgba[num3 + 1] = color[1];
		rgba[num3 + 2] = color[2];
		rgba[num3 + 3] = color[3];
		VerticesCount = verticesCount + 1;
	}

	public unsafe void AddWithFlagsVertex(float x, float y, float z, float u, float v, int color, int flags)
	{
		int verticesCount = VerticesCount;
		if (verticesCount >= VerticesMax)
		{
			GrowVertexBuffer();
		}
		float[] array = xyz;
		float[] uv = Uv;
		int num = verticesCount * 3;
		array[num] = x;
		array[num + 1] = y;
		array[num + 2] = z;
		int num2 = verticesCount * 2;
		uv[num2] = u;
		uv[num2 + 1] = v;
		if (Flags != null)
		{
			Flags[verticesCount] = flags;
		}
		fixed (byte* rgba = Rgba)
		{
			int* ptr = (int*)rgba;
			ptr[verticesCount] = color;
		}
		VerticesCount = verticesCount + 1;
	}

	public unsafe void AddVertexWithFlags(float x, float y, float z, float u, float v, int color, int flags)
	{
		AddVertexWithFlagsSkipColor(x, y, z, u, v, flags);
		fixed (byte* rgba = Rgba)
		{
			int* ptr = (int*)rgba;
			ptr[VerticesCount - 1] = color;
		}
	}

	public void AddVertexWithFlagsSkipColor(float x, float y, float z, float u, float v, int flags)
	{
		int verticesCount = VerticesCount;
		if (verticesCount >= VerticesMax)
		{
			GrowVertexBuffer();
		}
		float[] array = xyz;
		float[] uv = Uv;
		int num = verticesCount * 3;
		array[num] = x;
		array[num + 1] = y;
		array[num + 2] = z;
		int num2 = verticesCount * 2;
		uv[num2] = u;
		uv[num2 + 1] = v;
		if (Flags != null)
		{
			Flags[verticesCount] = flags;
		}
		VerticesCount = verticesCount + 1;
	}

	public void SetVertexFlags(int flag)
	{
		if (Flags != null)
		{
			int verticesCount = VerticesCount;
			for (int i = 0; i < verticesCount; i++)
			{
				Flags[i] |= flag;
			}
		}
	}

	public void AddNormal(float normalizedX, float normalizedY, float normalizedZ)
	{
		if (NormalsCount >= Normals.Length)
		{
			GrowNormalsBuffer();
		}
		Normals[NormalsCount++] = NormalUtil.PackNormal(normalizedX, normalizedY, normalizedZ);
	}

	public void AddNormal(BlockFacing facing)
	{
		if (NormalsCount >= Normals.Length)
		{
			GrowNormalsBuffer();
		}
		Normals[NormalsCount++] = facing.NormalPacked;
	}

	public void AddColorMapIndex(byte climateMapIndex, byte seasonMapIndex)
	{
		if (ColorMapIdsCount >= SeasonColorMapIds.Length)
		{
			Array.Resize(ref SeasonColorMapIds, SeasonColorMapIds.Length + 32);
			Array.Resize(ref ClimateColorMapIds, ClimateColorMapIds.Length + 32);
		}
		ClimateColorMapIds[ColorMapIdsCount] = climateMapIndex;
		SeasonColorMapIds[ColorMapIdsCount++] = seasonMapIndex;
	}

	public void AddColorMapIndex(byte climateMapIndex, byte seasonMapIndex, bool frostableBit)
	{
		if (FrostableBits == null)
		{
			FrostableBits = new bool[ClimateColorMapIds.Length];
		}
		if (ColorMapIdsCount >= SeasonColorMapIds.Length)
		{
			Array.Resize(ref SeasonColorMapIds, SeasonColorMapIds.Length + 32);
			Array.Resize(ref ClimateColorMapIds, ClimateColorMapIds.Length + 32);
			Array.Resize(ref FrostableBits, FrostableBits.Length + 32);
		}
		FrostableBits[ColorMapIdsCount] = frostableBit;
		ClimateColorMapIds[ColorMapIdsCount] = climateMapIndex;
		SeasonColorMapIds[ColorMapIdsCount++] = seasonMapIndex;
	}

	public void AddRenderPass(short renderPass)
	{
		if (RenderPassCount >= RenderPassesAndExtraBits.Length)
		{
			Array.Resize(ref RenderPassesAndExtraBits, RenderPassesAndExtraBits.Length + 32);
		}
		RenderPassesAndExtraBits[RenderPassCount++] = renderPass;
	}

	public void AddXyzFace(byte faceIndex)
	{
		if (XyzFacesCount >= XyzFaces.Length)
		{
			Array.Resize(ref XyzFaces, XyzFaces.Length + 32);
		}
		XyzFaces[XyzFacesCount++] = faceIndex;
	}

	public void AddTextureId(int textureId)
	{
		if (TextureIndicesCount >= TextureIndices.Length)
		{
			Array.Resize(ref TextureIndices, TextureIndices.Length + 4);
		}
		TextureIndices[TextureIndicesCount++] = getTextureIndex(textureId);
	}

	public void AddIndex(int index)
	{
		if (IndicesCount >= IndicesMax)
		{
			GrowIndexBuffer();
		}
		Indices[IndicesCount++] = index;
	}

	public void AddIndices(int i1, int i2, int i3, int i4, int i5, int i6)
	{
		AddIndices(allowSSBOs: false, i1, i2, i3, i4, i5, i6);
	}

	public void AddIndices(bool allowSSBOs, int i1, int i2, int i3, int i4, int i5, int i6)
	{
		int indicesCount = IndicesCount;
		if (indicesCount + 6 > IndicesMax)
		{
			GrowIndexBuffer(6);
		}
		int[] indices = Indices;
		indices[indicesCount++] = i1;
		indices[indicesCount++] = i2;
		indices[indicesCount++] = i3;
		indices[indicesCount++] = i4;
		indices[indicesCount++] = i5;
		indices[indicesCount++] = i6;
		IndicesCount = indicesCount;
	}

	public void AddQuadIndices(int i)
	{
		int indicesCount = IndicesCount;
		if (indicesCount + 6 > IndicesMax)
		{
			GrowIndexBuffer(6);
		}
		int[] indices = Indices;
		indices[indicesCount++] = i;
		indices[indicesCount++] = i + 1;
		indices[indicesCount++] = i + 2;
		indices[indicesCount++] = i;
		indices[indicesCount++] = i + 2;
		indices[indicesCount++] = i + 3;
		IndicesCount = indicesCount;
	}

	public void AddIndices(int[] indices)
	{
		int num = indices.Length;
		int indicesCount = IndicesCount;
		if (indicesCount + num > IndicesMax)
		{
			GrowIndexBuffer(num);
		}
		int[] indices2 = Indices;
		for (int i = 0; i < num; i++)
		{
			indices2[indicesCount++] = indices[i];
		}
		IndicesCount = indicesCount;
	}

	public void GrowIndexBuffer()
	{
		int num = IndicesCount;
		int[] array = new int[IndicesMax = num * 2];
		int[] indices = Indices;
		while (--num >= 0)
		{
			array[num] = indices[num];
		}
		Indices = array;
	}

	public void GrowIndexBuffer(int byAtLeastQuantity)
	{
		int indicesMax = Math.Max(IndicesCount * 2, IndicesCount + byAtLeastQuantity);
		int[] array = new int[IndicesMax = indicesMax];
		int[] indices = Indices;
		int num = IndicesCount;
		while (--num >= 0)
		{
			array[num] = indices[num];
		}
		Indices = array;
	}

	public void GrowNormalsBuffer()
	{
		if (Normals != null)
		{
			int num = Normals.Length;
			int[] array = new int[num * 2];
			int[] normals = Normals;
			while (--num >= 0)
			{
				array[num] = normals[num];
			}
			Normals = array;
		}
	}

	public void GrowVertexBuffer()
	{
		if (xyz != null)
		{
			float[] array = new float[XyzCount * 2];
			float[] array2 = xyz;
			int num = array2.Length;
			while (--num >= 0)
			{
				array[num] = array2[num];
			}
			xyz = array;
		}
		if (Uv != null)
		{
			float[] array3 = new float[UvCount * 2];
			float[] uv = Uv;
			int num2 = uv.Length;
			while (--num2 >= 0)
			{
				array3[num2] = uv[num2];
			}
			Uv = array3;
		}
		if (Rgba != null)
		{
			byte[] array4 = new byte[RgbaCount * 2];
			byte[] rgba = Rgba;
			int num3 = rgba.Length;
			while (--num3 >= 0)
			{
				array4[num3] = rgba[num3];
			}
			Rgba = array4;
		}
		if (Flags != null)
		{
			int[] array5 = new int[FlagsCount * 2];
			int[] flags = Flags;
			int num4 = flags.Length;
			while (--num4 >= 0)
			{
				array5[num4] = flags[num4];
			}
			Flags = array5;
		}
		VerticesMax *= 2;
	}

	public void CompactBuffers()
	{
		if (xyz != null)
		{
			int xyzCount = XyzCount;
			float[] destinationArray = new float[xyzCount + 1];
			Array.Copy(xyz, 0, destinationArray, 0, xyzCount);
			xyz = destinationArray;
		}
		if (Uv != null)
		{
			int uvCount = UvCount;
			float[] array = new float[uvCount + 1];
			Array.Copy(Uv, 0, array, 0, uvCount);
			Uv = array;
		}
		if (Rgba != null)
		{
			int rgbaCount = RgbaCount;
			byte[] array2 = new byte[rgbaCount + 1];
			Array.Copy(Rgba, 0, array2, 0, rgbaCount);
			Rgba = array2;
		}
		if (Flags != null)
		{
			int flagsCount = FlagsCount;
			int[] array3 = new int[flagsCount + 1];
			Array.Copy(Flags, 0, array3, 0, flagsCount);
			Flags = array3;
		}
		VerticesMax = VerticesCount;
	}

	public MeshData Clone()
	{
		MeshData meshData = CloneBasicData();
		CloneExtraData(meshData);
		return meshData;
	}

	private MeshData CloneBasicData()
	{
		MeshData meshData = new MeshData(initialiseArrays: false);
		meshData.VerticesPerFace = VerticesPerFace;
		meshData.IndicesPerFace = IndicesPerFace;
		meshData.SetVerticesCount(VerticesCount);
		meshData.xyz = xyz.FastCopy(XyzCount);
		if (Uv != null)
		{
			meshData.Uv = Uv.FastCopy(UvCount);
		}
		if (Rgba != null)
		{
			meshData.Rgba = Rgba.FastCopy(RgbaCount);
		}
		if (Flags != null)
		{
			meshData.Flags = Flags.FastCopy(FlagsCount);
		}
		meshData.Indices = Indices.FastCopy(IndicesCount);
		meshData.SetIndicesCount(IndicesCount);
		meshData.VerticesMax = VerticesCount;
		meshData.IndicesMax = meshData.Indices.Length;
		return meshData;
	}

	private void CopyBasicData(MeshData dest)
	{
		dest.SetVerticesCount(VerticesCount);
		dest.SetIndicesCount(IndicesCount);
		Array.Copy(xyz, dest.xyz, XyzCount);
		Array.Copy(Uv, dest.Uv, UvCount);
		Array.Copy(Rgba, dest.Rgba, RgbaCount);
		Array.Copy(Flags, dest.Flags, FlagsCount);
		Array.Copy(Indices, dest.Indices, IndicesCount);
	}

	public void DisposeBasicData()
	{
		xyz = null;
		Uv = null;
		Rgba = null;
		Flags = null;
		Indices = null;
	}

	private void CloneExtraData(MeshData dest)
	{
		if (Normals != null)
		{
			dest.Normals = Normals.FastCopy(NormalsCount);
		}
		if (XyzFaces != null)
		{
			dest.XyzFaces = XyzFaces.FastCopy(XyzFacesCount);
			dest.XyzFacesCount = XyzFacesCount;
		}
		if (TextureIndices != null)
		{
			dest.TextureIndices = TextureIndices.FastCopy(TextureIndicesCount);
			dest.TextureIndicesCount = TextureIndicesCount;
			dest.TextureIds = (int[])TextureIds.Clone();
		}
		if (ClimateColorMapIds != null)
		{
			dest.ClimateColorMapIds = ClimateColorMapIds.FastCopy(ColorMapIdsCount);
			dest.ColorMapIdsCount = ColorMapIdsCount;
		}
		if (SeasonColorMapIds != null)
		{
			dest.SeasonColorMapIds = SeasonColorMapIds.FastCopy(ColorMapIdsCount);
			dest.ColorMapIdsCount = ColorMapIdsCount;
		}
		if (RenderPassesAndExtraBits != null)
		{
			dest.RenderPassesAndExtraBits = RenderPassesAndExtraBits.FastCopy(RenderPassCount);
			dest.RenderPassCount = RenderPassCount;
		}
		if (CustomFloats != null)
		{
			dest.CustomFloats = CustomFloats.Clone();
		}
		if (CustomShorts != null)
		{
			dest.CustomShorts = CustomShorts.Clone();
		}
		if (CustomBytes != null)
		{
			dest.CustomBytes = CustomBytes.Clone();
		}
		if (CustomInts != null)
		{
			dest.CustomInts = CustomInts.Clone();
		}
	}

	private void DisposeExtraData()
	{
		Normals = null;
		NormalsCount = 0;
		XyzFaces = null;
		XyzFacesCount = 0;
		TextureIndices = null;
		TextureIndicesCount = 0;
		TextureIds = null;
		ClimateColorMapIds = null;
		SeasonColorMapIds = null;
		ColorMapIdsCount = 0;
		RenderPassesAndExtraBits = null;
		RenderPassCount = 0;
		CustomFloats = null;
		CustomShorts = null;
		CustomBytes = null;
		CustomInts = null;
	}

	public MeshData CloneUsingRecycler()
	{
		if (VerticesCount * 34 < 4096 || Uv == null || Rgba == null || Flags == null || VerticesPerFace != 4 || IndicesPerFace != 6)
		{
			return Clone();
		}
		int num = Math.Max(VerticesCount, (IndicesCount + 6 - 1) / 6 * 4);
		if ((float)num > (float)VerticesCount * 1.05f || (float)(num * 6 / 4) > (float)IndicesCount * 1.2f)
		{
			return Clone();
		}
		MeshData orCreateMesh = Recycler.GetOrCreateMesh(num);
		CopyBasicData(orCreateMesh);
		CloneExtraData(orCreateMesh);
		return orCreateMesh;
	}

	public void Dispose()
	{
		DisposeExtraData();
		if (Recyclable)
		{
			Recyclable = false;
			Recycler.Recycle(this);
		}
		else
		{
			DisposeBasicData();
		}
	}

	public MeshData EmptyClone()
	{
		MeshData meshData = new MeshData(initialiseArrays: false);
		meshData.VerticesPerFace = VerticesPerFace;
		meshData.IndicesPerFace = IndicesPerFace;
		meshData.xyz = new float[XyzCount];
		if (Normals != null)
		{
			meshData.Normals = new int[Normals.Length];
		}
		if (XyzFaces != null)
		{
			meshData.XyzFaces = new byte[XyzFaces.Length];
		}
		if (TextureIndices != null)
		{
			meshData.TextureIndices = new byte[TextureIndices.Length];
		}
		if (ClimateColorMapIds != null)
		{
			meshData.ClimateColorMapIds = new byte[ClimateColorMapIds.Length];
		}
		if (SeasonColorMapIds != null)
		{
			meshData.SeasonColorMapIds = new byte[SeasonColorMapIds.Length];
		}
		if (RenderPassesAndExtraBits != null)
		{
			meshData.RenderPassesAndExtraBits = new short[RenderPassesAndExtraBits.Length];
		}
		if (Uv != null)
		{
			meshData.Uv = new float[UvCount];
		}
		if (Rgba != null)
		{
			meshData.Rgba = new byte[RgbaCount];
		}
		if (Flags != null)
		{
			meshData.Flags = new int[FlagsCount];
		}
		meshData.Indices = new int[GetIndicesCount()];
		if (CustomFloats != null)
		{
			meshData.CustomFloats = CustomFloats.EmptyClone();
		}
		if (CustomShorts != null)
		{
			meshData.CustomShorts = CustomShorts.EmptyClone();
		}
		if (CustomBytes != null)
		{
			meshData.CustomBytes = CustomBytes.EmptyClone();
		}
		if (CustomInts != null)
		{
			meshData.CustomInts = CustomInts.EmptyClone();
		}
		meshData.VerticesMax = XyzCount / 3;
		meshData.IndicesMax = meshData.Indices.Length;
		return meshData;
	}

	public MeshData Clear()
	{
		IndicesCount = 0;
		VerticesCount = 0;
		ColorMapIdsCount = 0;
		RenderPassCount = 0;
		XyzFacesCount = 0;
		NormalsCount = 0;
		TextureIndicesCount = 0;
		if (CustomBytes != null)
		{
			CustomBytes.Count = 0;
		}
		if (CustomFloats != null)
		{
			CustomFloats.Count = 0;
		}
		if (CustomShorts != null)
		{
			CustomShorts.Count = 0;
		}
		if (CustomInts != null)
		{
			CustomInts.Count = 0;
		}
		TextureIds = Array.Empty<int>();
		return this;
	}

	public int SizeInBytes()
	{
		return ((xyz != null) ? (xyz.Length * 4) : 0) + ((Indices != null) ? (Indices.Length * 4) : 0) + ((Rgba != null) ? Rgba.Length : 0) + ((ClimateColorMapIds != null) ? ClimateColorMapIds.Length : 0) + ((SeasonColorMapIds != null) ? SeasonColorMapIds.Length : 0) + ((XyzFaces != null) ? XyzFaces.Length : 0) + ((RenderPassesAndExtraBits != null) ? (RenderPassesAndExtraBits.Length * 2) : 0) + ((Normals != null) ? (Normals.Length * 4) : 0) + ((Flags != null) ? (Flags.Length * 4) : 0) + ((Uv != null) ? (Uv.Length * 4) : 0) + ((CustomBytes?.Values != null) ? CustomBytes.Values.Length : 0) + ((CustomFloats?.Values != null) ? (CustomFloats.Values.Length * 4) : 0) + ((CustomShorts?.Values != null) ? (CustomShorts.Values.Length * 2) : 0) + ((CustomInts?.Values != null) ? (CustomInts.Values.Length * 4) : 0);
	}

	public MeshData WithTexPos(TextureAtlasPosition texPos)
	{
		MeshData meshData = Clone();
		meshData.SetTexPos(texPos);
		return meshData;
	}

	public void SetTexPos(TextureAtlasPosition texPos)
	{
		float num = texPos.x2 - texPos.x1;
		float num2 = texPos.y2 - texPos.y1;
		for (int i = 0; i < Uv.Length; i++)
		{
			Uv[i] = ((i % 2 == 0) ? (Uv[i] * num + texPos.x1) : (Uv[i] * num2 + texPos.y1));
		}
		byte textureIndex = getTextureIndex(texPos.atlasTextureId);
		for (int j = 0; j < TextureIndices.Length; j++)
		{
			TextureIndices[j] = textureIndex;
		}
	}

	public MeshData[] SplitByTextureId()
	{
		MeshData[] array = new MeshData[TextureIds.Length];
		if (array.Length == 1)
		{
			array[0] = this;
		}
		else
		{
			int i;
			for (i = 0; i < array.Length; i++)
			{
				MeshData obj = (array[i] = EmptyClone());
				obj.AddMeshData(this, (int faceindex) => TextureIndices[faceindex] == i);
				obj.CompactBuffers();
			}
		}
		return array;
	}
}
