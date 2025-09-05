using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class JsonTesselator : IBlockTesselator
{
	public const int DisableRandomsFlag = 1024;

	public const long Darkness = 4934475L;

	public TerrainMesherHelper helper = new TerrainMesherHelper();

	private BlockPos pos = new BlockPos();

	private int[] jsonLightRGB = new int[25];

	public static float[] reusableIdentityMatrix = Mat4f.Create();

	private float[] floatpool = new float[3];

	private int[] windFlagsMask = new int[64];

	private int[] windFlagsSet = new int[64];

	private static int[] faceCoordLookup = new int[6] { 2, 0, 2, 0, 1, 1 };

	private float[] tmpCoords = new float[3];

	private static int[][] axesByFacingLookup = new int[6][]
	{
		new int[2] { 0, 1 },
		new int[2] { 1, 2 },
		new int[2] { 0, 1 },
		new int[2] { 1, 2 },
		new int[2] { 0, 2 },
		new int[2] { 0, 2 }
	};

	private static int[][] indexesByFacingLookup = new int[6][]
	{
		new int[4] { 3, 2, 1, 0 },
		new int[4] { 3, 1, 2, 0 },
		new int[4] { 2, 3, 0, 1 },
		new int[4] { 2, 0, 3, 1 },
		new int[4] { 3, 2, 1, 0 },
		new int[4] { 1, 0, 3, 2 }
	};

	[ThreadStatic]
	private static float[] reusableFloatArray;

	[ThreadStatic]
	private static int[] reusableIntArray;

	public JsonTesselator()
	{
		helper.tess = this;
	}

	public long SetUpLightRGBs(TCTCache vars)
	{
		int extIndex3d = vars.extIndex3d;
		int[] currentLightRGBByCorner = vars.CurrentLightRGBByCorner;
		int num = 0;
		long num2 = 0L;
		int[] array = jsonLightRGB;
		for (int i = 0; i < 6; i++)
		{
			num2 += vars.CalcBlockFaceLight(i, extIndex3d + TileSideEnum.MoveIndex[i]);
			array[num++] = currentLightRGBByCorner[0];
			array[num++] = currentLightRGBByCorner[1];
			array[num++] = currentLightRGBByCorner[2];
			array[num++] = currentLightRGBByCorner[3];
		}
		return num2 + (array[24] = vars.tct.currentChunkRgbsExt[extIndex3d]);
	}

	public void Tesselate(TCTCache vars)
	{
		int extIndex3d = vars.extIndex3d;
		long num = SetUpLightRGBs(vars);
		helper.vars = vars;
		pos.SetDimension(vars.dimension);
		pos.Set(vars.posX, vars.posY, vars.posZ);
		Dictionary<BlockPos, BlockEntity> blockEntitiesOfChunk = vars.blockEntitiesOfChunk;
		if (blockEntitiesOfChunk != null && blockEntitiesOfChunk.TryGetValue(pos, out var value))
		{
			try
			{
				if (value != null && value.OnTesselation(helper, vars.tct.offthreadTesselator))
				{
					return;
				}
			}
			catch (Exception e)
			{
				vars.tct.game.Logger.Error("Exception thrown during OnTesselation() of block entity {0}@{1}/{2}/{3}. Block will probably not be rendered as intended.", value, vars.posX, vars.posY, vars.posZ);
				vars.tct.game.Logger.Error(e);
			}
		}
		MeshData defaultBlockMesh = vars.shapes.GetDefaultBlockMesh(vars.block);
		bool flag = vars.block.DoNotRenderAtLod2;
		if (vars.block.Lod0Shape != null)
		{
			if (NotSurrounded(vars, extIndex3d))
			{
				doMesh(vars, vars.block.Lod0Mesh, 0);
			}
			else
			{
				flag = true;
			}
		}
		if (num == 4934475)
		{
			doMesh(vars, defaultBlockMesh, 0);
			return;
		}
		if (vars.block.Lod2Mesh == null)
		{
			doMesh(vars, defaultBlockMesh, (!flag) ? 1 : 2);
			return;
		}
		doMesh(vars, defaultBlockMesh, 2);
		doMesh(vars, vars.block.Lod2Mesh, 3);
	}

	public static bool NotSurrounded(TCTCache vars, int extIndex3d)
	{
		if (vars.block.FaceCullMode != EnumFaceCullMode.CollapseMaterial)
		{
			return true;
		}
		for (int i = 0; i < 5; i++)
		{
			Block block = vars.tct.currentChunkBlocksExt[extIndex3d + TileSideEnum.MoveIndex[i]];
			if (block.BlockMaterial != vars.block.BlockMaterial && !block.SideOpaque[TileSideEnum.GetOpposite(i)])
			{
				return true;
			}
		}
		return false;
	}

	public void doMesh(TCTCache vars, MeshData sourceMesh, int lodLevel)
	{
		if (sourceMesh.VerticesCount == 0)
		{
			return;
		}
		if (sourceMesh == null)
		{
			vars.block.DrawType = EnumDrawType.Cube;
			return;
		}
		MeshData[] array = (((lodLevel + 1) / 2 == 1) ? vars.shapes.altblockModelDatasLod1[vars.blockId] : ((lodLevel == 0) ? vars.shapes.altblockModelDatasLod0[vars.blockId] : vars.shapes.altblockModelDatasLod2[vars.blockId]));
		if (array != null)
		{
			int num = GameMath.MurmurHash3Mod(vars.posX, (vars.block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? vars.posY : 0, vars.posZ, array.Length);
			sourceMesh = array[num];
		}
		vars.block.OnJsonTesselation(ref sourceMesh, ref jsonLightRGB, pos, vars.tct.currentChunkBlocksExt, vars.extIndex3d);
		if (vars.preRotationMatrix != null)
		{
			AddJsonModelDataToMesh(sourceMesh, lodLevel, vars, helper, vars.preRotationMatrix);
		}
		else if (vars.block.RandomizeRotations || vars.block.RandomSizeAdjust != 0f)
		{
			float[] tfMatrix = (vars.block.RandomizeRotations ? TesselationMetaData.randomRotMatrices[GameMath.MurmurHash3Mod(-vars.posX, (vars.block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? vars.posY : 0, vars.posZ, TesselationMetaData.randomRotations.Length)] : reusableIdentityMatrix);
			AddJsonModelDataToMesh(sourceMesh, lodLevel, vars, helper, tfMatrix);
		}
		else
		{
			AddJsonModelDataToMesh(sourceMesh, lodLevel, vars, helper, null);
		}
	}

	public void AddJsonModelDataToMesh(MeshData sourceMesh, int lodlevel, TCTCache vars, IMeshPoolSupplier poolSupplier, float[] tfMatrix)
	{
		if (sourceMesh.VerticesCount > windFlagsMask.Length)
		{
			windFlagsMask = new int[sourceMesh.VerticesCount];
			windFlagsSet = new int[sourceMesh.VerticesCount];
		}
		for (int i = 0; i < sourceMesh.VerticesCount; i++)
		{
			windFlagsMask[i] = -1;
			windFlagsSet[i] = 0;
		}
		Block block = vars.tct.currentChunkFluidBlocksExt[vars.extIndex3d + vars.block.IceCheckOffset * 34 * 34];
		if (block.BlockId != 0)
		{
			AdjustWindWaveForFluids(sourceMesh, block);
		}
		float finalX = vars.finalX;
		float num = vars.finalY;
		float finalZ = vars.finalZ;
		int vertexFlags = vars.VertexFlags;
		int drawFaceFlags = vars.drawFaceFlags;
		bool frostable = vars.block.Frostable;
		byte b = 0;
		byte b2 = 0;
		byte b3 = 0;
		byte b4 = 0;
		float num2 = 1f;
		byte temperature = vars.ColorMapData.Temperature;
		byte rainfall = vars.ColorMapData.Rainfall;
		int num3 = -2;
		int num4 = -1;
		MeshData meshData = null;
		int leftTop = 0;
		int rightTop = 0;
		int leftBottom = 0;
		int rightBottom = 0;
		float[] array = tmpCoords;
		float[] array2 = sourceMesh.xyz;
		float[] uv = sourceMesh.Uv;
		int[] array3 = sourceMesh.Flags;
		int[] indices = sourceMesh.Indices;
		float[] array4 = null;
		float[] array5 = null;
		byte[] array6 = null;
		int[] array7 = null;
		CustomMeshDataPartInt customMeshDataPartInt = null;
		float[] array8 = sourceMesh.xyz;
		int[] array9 = sourceMesh.Flags;
		if (poolSupplier == null)
		{
			poolSupplier = vars.tct;
		}
		int verticesPerFace = sourceMesh.VerticesPerFace;
		int indicesPerFace = sourceMesh.IndicesPerFace;
		bool useSSBOs = vars.tct.game.api.renderapi.useSSBOs;
		if ((useSSBOs && verticesPerFace != 4) || indicesPerFace != 6)
		{
			string message = "Model " + vars.block.Code.ToShortString() + " does not have 4 vertices and 6 indices per face, will break chunk shader optimizations. Try disabling clientsetting: allowSSBOs.";
			vars.tct.game.Logger.Warning(message);
		}
		if (tfMatrix != null)
		{
			int num5 = sourceMesh.VerticesCount * 3;
			array8 = NewFloatArray(num5);
			float num6 = vars.block.RandomSizeAdjust;
			bool flag = false;
			if (num6 != 0f)
			{
				if (num6 < 0f)
				{
					num6 = 0f - num6;
				}
				else
				{
					flag = true;
				}
				int num7 = (int)num6;
				num6 = num6 % 1f * ((float)GameMath.MurmurHash3Mod(-vars.posX, 0, vars.posZ, 2000) / 1000f - 1f);
				num += (float)num7 * num6;
			}
			for (int j = 0; j < num5; j += 3)
			{
				if (num6 == 0f)
				{
					Mat4f.MulWithVec3_Position(tfMatrix, array2, array8, j);
				}
				else if (flag)
				{
					Mat4f.MulWithVec3_Position_AndScale(tfMatrix, array2, array8, j, 1f + num6);
				}
				else
				{
					Mat4f.MulWithVec3_Position_AndScaleXY(tfMatrix, array2, array8, j, 1f + num6);
				}
			}
			num5 = sourceMesh.FlagsCount;
			array9 = array3;
			if (array9 != null && tfMatrix != reusableIdentityMatrix)
			{
				array9 = NewIntArray(num5);
				for (int k = 0; k < num5; k++)
				{
					int num8 = array3[k];
					VertexFlags.UnpackNormal(num8, floatpool);
					Mat4f.MulWithVec3(tfMatrix, floatpool, floatpool);
					float num9 = GameMath.RootSumOfSquares(floatpool[0], floatpool[1], floatpool[2]);
					array9[k] = (num8 & -33546241) | VertexFlags.PackNormal(floatpool[0] / num9, floatpool[1] / num9, floatpool[2] / num9);
				}
			}
		}
		int xyzFacesCount = sourceMesh.XyzFacesCount;
		short[] renderPassesAndExtraBits = sourceMesh.RenderPassesAndExtraBits;
		bool flag2 = renderPassesAndExtraBits != null && renderPassesAndExtraBits.Length != 0;
		bool flag3 = tfMatrix == null;
		for (int l = 0; l < xyzFacesCount; l++)
		{
			int num10 = sourceMesh.TextureIds[sourceMesh.TextureIndices[l * verticesPerFace / sourceMesh.VerticesPerFace]];
			float num11 = finalX;
			float num12 = finalZ;
			bool flag4 = true;
			int num13;
			if (flag2)
			{
				num13 = sourceMesh.RenderPassesAndExtraBits[l];
				if (num13 >= 1024)
				{
					num13 &= 0x3FF;
					flag4 = false;
					num11 = (int)(finalX + 0.5f);
					num12 = (int)(finalZ + 0.5f);
				}
			}
			else
			{
				num13 = -1;
			}
			if (flag4 != flag3)
			{
				flag3 = flag4;
				array2 = (flag4 ? array8 : sourceMesh.xyz);
				array3 = (flag4 ? array9 : sourceMesh.Flags);
			}
			int num14 = sourceMesh.XyzFaces[l] - 1;
			if (num14 >= 0)
			{
				if (tfMatrix != null && flag4 && tfMatrix != reusableIdentityMatrix)
				{
					num14 = Mat4f.MulWithVec3_BlockFacing(tfMatrix, BlockFacing.ALLFACES[num14].Normalf).Index;
				}
				if (((1 << num14) & drawFaceFlags) == 0)
				{
					int num15 = l * 4 * 3 + faceCoordLookup[num14];
					float num16 = array2[num15];
					if (num14 == 1 || num14 == 2 || num14 == 4)
					{
						num16 = 1f - num16;
					}
					if (num16 <= 0.01f)
					{
						num16 = array2[num15 + 6];
						if (num14 == 1 || num14 == 2 || num14 == 4)
						{
							num16 = 1f - num16;
						}
						if (num16 <= 0.01f)
						{
							bool flag5 = true;
							for (int m = 0; m < 9; m++)
							{
								if (m == 3)
								{
									m += 3;
								}
								int num17 = l * 4 * 3 + m;
								if (num17 != num15 && num17 != num15 + 6)
								{
									num16 = array2[num17];
									if (num16 < -0.0001f || num16 > 1.0001f)
									{
										flag5 = false;
										break;
									}
								}
							}
							if (flag5)
							{
								continue;
							}
						}
					}
				}
			}
			bool flag6 = num13 == 4;
			bool flag7 = num13 == 5;
			if (num3 != num13 || num4 != num10)
			{
				meshData = poolSupplier.GetMeshPoolForPass(num10, (num13 >= 0) ? ((EnumChunkRenderPass)num13) : vars.RenderPass, lodlevel);
				array4 = meshData.xyz;
				array5 = meshData.Uv;
				array6 = meshData.Rgba;
				customMeshDataPartInt = meshData.CustomInts;
				array7 = meshData.Flags;
			}
			num3 = num13;
			num4 = num10;
			int value = ColorMapData.FromValues(sourceMesh.SeasonColorMapIds[l], sourceMesh.ClimateColorMapIds[l], temperature, rainfall, (sourceMesh.FrostableBits != null) ? sourceMesh.FrostableBits[l] : frostable, vars.block.ExtraColorBits);
			int[] array10 = null;
			float num18 = 0f;
			int[] array11 = jsonLightRGB;
			if (num14 < 0)
			{
				int num19 = array11[24];
				b = (byte)((float)(num19 & 0xFF) * num2);
				b2 = (byte)(num19 >> 8);
				b3 = (byte)(num19 >> 16);
				b4 = (byte)(num19 >> 24);
				num2 = 1f;
			}
			else
			{
				array10 = axesByFacingLookup[num14];
				int[] array12 = indexesByFacingLookup[num14];
				int num20 = num14 * 4;
				leftTop = array11[num20 + array12[0]];
				rightTop = array11[num20 + array12[1]];
				leftBottom = array11[num20 + array12[2]];
				rightBottom = array11[num20 + array12[3]];
				if (vars.textureVOffset != 0f && ((1 << num14) & vars.block.alternatingVOffsetFaces) == 0)
				{
					num18 = vars.textureVOffset / ((float)ClientSettings.MaxTextureAtlasHeight / 32f);
				}
			}
			int num21 = l * verticesPerFace;
			int verticesCount = meshData.VerticesCount;
			int num22 = verticesCount - l * verticesPerFace;
			int num23 = verticesCount * 3;
			int num24 = num21 * 3;
			int num25 = verticesCount * 2;
			int num26 = num21 * 2;
			int num27 = verticesCount * 4;
			int num28 = verticesPerFace;
			do
			{
				if (verticesCount >= meshData.VerticesMax)
				{
					meshData.VerticesCount = verticesCount;
					meshData.GrowVertexBuffer();
					meshData.GrowNormalsBuffer();
					array4 = meshData.xyz;
					array5 = meshData.Uv;
					array6 = meshData.Rgba;
					customMeshDataPartInt = meshData.CustomInts;
					array7 = meshData.Flags;
				}
				vars.UpdateChunkMinMax(array4[num23++] = (array[0] = array2[num24++]) + num11, array4[num23++] = (array[1] = array2[num24++]) + num, array4[num23++] = (array[2] = array2[num24++]) + num12);
				array5[num25++] = uv[num26++];
				array5[num25++] = uv[num26++] + num18;
				float num29 = 1f;
				if (vars.block.DrawType == EnumDrawType.JSONAndWater && array[1] < 1f)
				{
					num29 = Math.Max(0.6f, array[1]) - 0.1f;
				}
				if (num14 >= 0)
				{
					float lx = GameMath.Clamp(array[array10[0]], 0f, 1f);
					float ly = GameMath.Clamp(array[array10[1]], 0f, 1f);
					int num30 = GameMath.BiLerpRgbaColor(lx, ly, leftTop, rightTop, leftBottom, rightBottom);
					b = (byte)(num30 & 0xFF);
					b2 = (byte)(num30 >> 8);
					b3 = (byte)(num30 >> 16);
					b4 = (byte)((float)(num30 >> 24) * num2);
				}
				if (flag6)
				{
					if (sourceMesh.CustomFloats == null)
					{
						customMeshDataPartInt.Add(value);
						customMeshDataPartInt.Add(268435456);
						meshData.CustomFloats.Add(0f);
						meshData.CustomFloats.Add(0f);
					}
					else
					{
						customMeshDataPartInt.Add(value);
						customMeshDataPartInt.Add(sourceMesh.CustomInts.Values[num21]);
						meshData.CustomFloats.Add(sourceMesh.CustomFloats.Values[2 * num21]);
						meshData.CustomFloats.Add(sourceMesh.CustomFloats.Values[2 * num21 + 1]);
					}
				}
				else
				{
					customMeshDataPartInt.Add(value);
					if (flag7)
					{
						meshData.CustomShorts.Add(sourceMesh.CustomShorts.Values[2 * num21]);
						meshData.CustomShorts.Add(sourceMesh.CustomShorts.Values[2 * num21 + 1]);
					}
				}
				array6[num27++] = (byte)((float)(int)b * num29);
				array6[num27++] = (byte)((float)(int)b2 * num29);
				array6[num27++] = (byte)((float)(int)b3 * num29);
				array6[num27++] = (byte)((float)(int)b4 * num29);
				array7[verticesCount++] = (vertexFlags | array3[num21] | windFlagsSet[num21]) & windFlagsMask[num21];
				num21++;
			}
			while (--num28 > 0);
			meshData.VerticesCount = verticesCount;
			if (indicesPerFace == 6)
			{
				int num31 = l * indicesPerFace;
				meshData.AddIndices(useSSBOs, num22 + indices[num31++], num22 + indices[num31++], num22 + indices[num31++], num22 + indices[num31++], num22 + indices[num31++], num22 + indices[num31]);
				continue;
			}
			int num32 = l * indicesPerFace;
			for (int n = 0; n < indicesPerFace; n++)
			{
				meshData.AddIndex(num22 + indices[num32++]);
			}
		}
	}

	private void AdjustWindWaveForFluids(MeshData sourceMesh, Block fluidBlock)
	{
		int num = -503316481;
		int verticesCount = sourceMesh.VerticesCount;
		int[] array = windFlagsMask;
		if (fluidBlock.SideSolid.Any)
		{
			for (int i = 0; i < verticesCount; i++)
			{
				array[i] = num;
			}
			return;
		}
		int[] array2 = windFlagsSet;
		int[] flags = sourceMesh.Flags;
		for (int j = 0; j < verticesCount; j++)
		{
			int num2 = flags[j] & 0x1E000000;
			if (num2 == 33554432 || num2 == 100663296)
			{
				array2[j] = 738197504;
				array[j] = -1073741825;
			}
		}
	}

	private static float[] NewFloatArray(int size)
	{
		if (reusableFloatArray == null || reusableFloatArray.Length < size)
		{
			reusableFloatArray = new float[size];
		}
		return reusableFloatArray;
	}

	private static int[] NewIntArray(int size)
	{
		if (reusableIntArray == null || reusableIntArray.Length < size)
		{
			reusableIntArray = new int[size];
		}
		return reusableIntArray;
	}
}
