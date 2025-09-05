using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SurfaceLayerTesselator : IBlockTesselator
{
	private const int caveArtPerBlock = 16;

	private static float decorFaceOffset = 0.002f;

	private float[] uv = new float[8];

	public void Tesselate(TCTCache vars)
	{
		int num = 0;
		TextureAtlasPosition[] textureAtlasPositionsByTextureSubId = vars.textureAtlasPositionsByTextureSubId;
		int[] fastBlockTextureSubidsByFace = vars.fastBlockTextureSubidsByFace;
		bool hasAlternates = vars.block.HasAlternates;
		int value = vars.ColorMapData.Value;
		int num2 = vars.drawFaceFlags ^ 0x3F;
		int extIndex3d = vars.extIndex3d;
		int vertexFlags = vars.VertexFlags;
		FastVec3f[][] blockFaceVertices = vars.blockFaceVertices;
		BakedCompositeTexture[][] array = null;
		int k = 0;
		if (hasAlternates)
		{
			array = vars.block.FastTextureVariants;
			k = GameMath.MurmurHash3(vars.posX, vars.posY, vars.posZ);
		}
		int rotIndex = 0;
		if (vars.block.RandomizeRotations)
		{
			rotIndex = GameMath.MurmurHash3Mod(vars.posX, vars.posY, vars.posZ, 4);
		}
		MeshData[] poolForPass = vars.tct.GetPoolForPass(vars.RenderPass, (!vars.block.DoNotRenderAtLod2) ? 1 : 2);
		MeshData[] poolForPass2 = vars.tct.GetPoolForPass(vars.RenderPass, 0);
		for (int i = 0; i < 6; i++)
		{
			if ((num2 & (1 << i)) == 0)
			{
				continue;
			}
			int index = BlockFacing.ALLFACES[i].Opposite.Index;
			long num3 = vars.CalcBlockFaceLight(index, extIndex3d + TileSideEnum.MoveIndex[index]);
			int num4;
			if (hasAlternates)
			{
				BakedCompositeTexture[] array2 = array[i];
				if (array2 != null)
				{
					num4 = array2[GameMath.Mod(k, array2.Length)].TextureSubId;
					goto IL_0149;
				}
			}
			num4 = fastBlockTextureSubidsByFace[i];
			goto IL_0149;
			IL_0149:
			DrawBlockFace(vars, i, blockFaceVertices[i], textureAtlasPositionsByTextureSubId[num4], vertexFlags | BlockFacing.ALLFACES[TileSideEnum.GetOpposite(i)].NormalPackedFlags, value, (num3 == 789516) ? poolForPass2 : poolForPass, 1f, rotIndex);
			num += 4;
		}
	}

	public void DrawBlockFace(TCTCache vars, int tileSide, FastVec3f[] quadOffsets, TextureAtlasPosition texPos, int flags, int colorMapDataValue, MeshData[] meshPools, float blockHeight = 1f, int rotIndex = 0)
	{
		MeshData meshData = meshPools[texPos.atlasNumber];
		int verticesCount = meshData.VerticesCount;
		int[] currentLightRGBByCorner = vars.CurrentLightRGBByCorner;
		float uvx = texPos.x1;
		float uvy = texPos.y1;
		float uvx2 = texPos.x2;
		float uvy2 = texPos.y2;
		if (rotIndex > 1)
		{
			uvx = texPos.x2;
			uvy = texPos.y2;
			uvx2 = texPos.x1;
			uvy2 = texPos.y1;
		}
		if (rotIndex == 1 || rotIndex == 3)
		{
			float num = uvx2 - uvx;
			float num2 = uvy2 - uvy;
			float num3 = uvx;
			float num4 = uvy;
			uvx = num3 + num;
			uvy = num4;
			uvx2 = num3;
			uvy2 = num4 + num2;
		}
		Vec3f normalf = BlockFacing.ALLFACES[tileSide].Normalf;
		float num5 = vars.finalX - normalf.X * decorFaceOffset;
		float num6 = vars.finalY - normalf.Y * decorFaceOffset;
		float num7 = vars.finalZ - normalf.Z * decorFaceOffset;
		float num8 = 1.0001f;
		float num9 = 0f;
		float num10 = 0f;
		float num11 = 0f;
		float num12 = 0f;
		float num13 = 0f;
		float num14 = 0f;
		float num15 = 0f;
		float num16 = 0f;
		float num17 = 0f;
		float num18 = 0f;
		int num19 = (8 - vars.decorRotationData % 4 * 2) % 8;
		float[] array = uv;
		if (vars.decorSubPosition > 0)
		{
			string path = vars.block.Code.Path;
			float num20 = (uvx2 - uvx) / (float)GlobalConstants.CaveArtColsPerRow;
			float num21 = (uvy2 - uvy) / (float)GlobalConstants.CaveArtColsPerRow;
			uvx += (float)(path[path.Length - 3] - 49) * num20;
			uvy += (float)(path[path.Length - 1] - 49) * num21;
			uvx2 = uvx + num20;
			uvy2 = uvy + num21;
			int num22 = vars.decorSubPosition - 1;
			num8 /= 16f;
			float num23 = (float)(num22 % 16) * num8;
			float num24 = (float)(num22 / 16) * num8;
			float num25 = 0.9375f;
			num8 *= 4f;
			float num26 = num8;
			float num27 = 1f - num8;
			switch (tileSide)
			{
			case 0:
				num5 += num23 - num8;
				num6 += num25 - num24 - num8;
				if (num23 > num27)
				{
					if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.EAST))
					{
						float num40 = (num23 - num27) / 0.5f;
						cropRightSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num40, vars.decorRotationData);
						num9 = (0f - num8) * 2f * num40;
						num10 = (0f - num8) * 2f * num40;
					}
				}
				else if (num23 < num26 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.WEST))
				{
					float num41 = (num26 - num23) / 0.5f;
					cropLeftSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num41, vars.decorRotationData);
					num11 = num8 * 2f * num41;
					num12 = num8 * 2f * num41;
				}
				if (num25 - num24 > num27)
				{
					if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.UP))
					{
						float num42 = (num25 - num24 - num27) / 0.5f;
						cropTopSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num42, vars.decorRotationData);
						num13 = (0f - num8) * 2f * num42;
					}
				}
				else if (num25 - num24 < num26 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.DOWN))
				{
					float num43 = (num26 - num25 + num24) / 0.5f;
					cropBottomSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num43, vars.decorRotationData);
					num14 = num8 * 2f * num43;
				}
				break;
			case 1:
				num7 += num23 - num8;
				num6 += num25 - num24 - num8;
				num5 += 0.5f;
				if (num23 > num27)
				{
					if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.SOUTH))
					{
						float num36 = (num23 - num27) / 0.5f;
						cropRightSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num36, vars.decorRotationData);
						num15 = (0f - num8) * 2f * num36;
						num16 = (0f - num8) * 2f * num36;
					}
				}
				else if (num23 < num26 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.NORTH))
				{
					float num37 = (num26 - num23) / 0.5f;
					cropLeftSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num37, vars.decorRotationData);
					num17 = num8 * 2f * num37;
					num18 = num8 * 2f * num37;
				}
				if (num25 - num24 > num27)
				{
					if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.UP))
					{
						float num38 = (num25 - num24 - num27) / 0.5f;
						cropTopSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num38, vars.decorRotationData);
						num13 = (0f - num8) * 2f * num38;
					}
				}
				else if (num25 - num24 < num26 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.DOWN))
				{
					float num39 = (num26 - num25 + num24) / 0.5f;
					cropBottomSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num39, vars.decorRotationData);
					num14 = num8 * 2f * num39;
				}
				break;
			case 2:
				num5 += num25 - num23 - num8;
				num6 += num25 - num24 - num8;
				num7 += 0.5f;
				if (num25 - num23 > num27)
				{
					if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.EAST))
					{
						float num44 = (num25 - num23 - num27) / 0.5f;
						cropLeftSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num44, vars.decorRotationData);
						num11 = (0f - num8) * 2f * num44;
						num12 = (0f - num8) * 2f * num44;
					}
				}
				else if (num25 - num23 < num26 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.WEST))
				{
					float num45 = (num26 - num25 + num23) / 0.5f;
					cropRightSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num45, vars.decorRotationData);
					num9 = num8 * 2f * num45;
					num10 = num8 * 2f * num45;
				}
				if (num25 - num24 > num27)
				{
					if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.UP))
					{
						float num46 = (num25 - num24 - num27) / 0.5f;
						cropTopSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num46, vars.decorRotationData);
						num13 = (0f - num8) * 2f * num46;
					}
				}
				else if (num25 - num24 < num26 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.DOWN))
				{
					float num47 = (num26 - num25 + num24) / 0.5f;
					cropBottomSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num47, vars.decorRotationData);
					num14 = num8 * 2f * num47;
				}
				break;
			case 3:
				num7 += num25 - num23 - num8;
				num6 += num25 - num24 - num8;
				if (num25 - num23 > num27)
				{
					if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.SOUTH))
					{
						float num48 = (num25 - num23 - num27) / 0.5f;
						cropLeftSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num48, vars.decorRotationData);
						num17 = (0f - num8) * 2f * num48;
						num18 = (0f - num8) * 2f * num48;
					}
				}
				else if (num25 - num23 < num26 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.NORTH))
				{
					float num49 = (num26 - num25 + num23) / 0.5f;
					cropRightSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num49, vars.decorRotationData);
					num15 = num8 * 2f * num49;
					num16 = num8 * 2f * num49;
				}
				if (num25 - num24 > num27)
				{
					if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.UP))
					{
						float num50 = (num25 - num24 - num27) / 0.5f;
						cropTopSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num50, vars.decorRotationData);
						num13 = (0f - num8) * 2f * num50;
					}
				}
				else if (num25 - num24 < num26 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.DOWN))
				{
					float num51 = (num26 - num25 + num24) / 0.5f;
					cropBottomSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num51, vars.decorRotationData);
					num14 = num8 * 2f * num51;
				}
				break;
			case 4:
				num5 += num23 - num8;
				num7 += num25 - num24 - num8;
				num6 += 0.5f;
				if (num23 > num27)
				{
					if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.EAST))
					{
						float num32 = (num23 - num27) / 0.5f;
						cropRightSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num32, vars.decorRotationData);
						num9 = (0f - num8) * 2f * num32;
						num10 = (0f - num8) * 2f * num32;
					}
				}
				else if (num23 < num26 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.WEST))
				{
					float num33 = (num26 - num23) / 0.5f;
					cropLeftSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num33, vars.decorRotationData);
					num11 = num8 * 2f * num33;
					num12 = num8 * 2f * num33;
				}
				if (num25 - num24 > num27)
				{
					if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.SOUTH))
					{
						float num34 = (num25 - num24 - num27) / 0.5f;
						cropTopSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num34, vars.decorRotationData);
						num17 = (0f - num8) * 2f * num34;
						num15 = (0f - num8) * 2f * num34;
					}
				}
				else if (num25 - num24 < num26 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.NORTH))
				{
					float num35 = (num26 - num25 + num24) / 0.5f;
					cropBottomSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num35, vars.decorRotationData);
					num18 = num8 * 2f * num35;
					num16 = num8 * 2f * num35;
				}
				break;
			case 5:
				num5 += num23 - num8;
				num7 += num24 - num8;
				if (num23 > num27)
				{
					if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.EAST))
					{
						float num28 = (num23 - num27) / 0.5f;
						cropRightSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num28, vars.decorRotationData);
						num9 = (0f - num8) * 2f * num28;
						num10 = (0f - num8) * 2f * num28;
					}
				}
				else if (num23 < num26 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.WEST))
				{
					float num29 = (num26 - num23) / 0.5f;
					cropLeftSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num29, vars.decorRotationData);
					num11 = num8 * 2f * num29;
					num12 = num8 * 2f * num29;
				}
				if (num24 > num27)
				{
					if (!CaveArtBlockOnSide(vars, tileSide, BlockFacing.SOUTH))
					{
						float num30 = (num24 - num27) / 0.5f;
						cropBottomSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num30, vars.decorRotationData);
						num18 = (0f - num8) * 2f * num30;
						num16 = (0f - num8) * 2f * num30;
					}
				}
				else if (num24 < num26 && !CaveArtBlockOnSide(vars, tileSide, BlockFacing.NORTH))
				{
					float num31 = (num26 - num24) / 0.5f;
					cropTopSide(ref uvx, ref uvx2, ref uvy, ref uvy2, num20, num21, num31, vars.decorRotationData);
					num17 = num8 * 2f * num31;
					num15 = num8 * 2f * num31;
				}
				break;
			}
			num8 *= 2f;
		}
		if ((vars.decorRotationData & 4) > 0)
		{
			array[0] = (array[6] = uvx);
			array[2] = (array[4] = uvx2);
		}
		else
		{
			array[0] = (array[6] = uvx2);
			array[2] = (array[4] = uvx);
		}
		array[1] = (array[3] = uvy);
		array[5] = (array[7] = uvy2);
		FastVec3f fastVec3f = quadOffsets[6];
		meshData.AddVertexWithFlags(num5 + fastVec3f.X * num8 + num10, num6 + fastVec3f.Y * num8 + num14, num7 + fastVec3f.Z * num8 + num16, array[(num19 + 4) % 8], array[(num19 + 5) % 8], currentLightRGBByCorner[(tileSide <= 3) ? 3 : 0], flags);
		fastVec3f = quadOffsets[4];
		meshData.AddVertexWithFlags(num5 + fastVec3f.X * num8 + num9, num6 + fastVec3f.Y * num8 + num13, num7 + fastVec3f.Z * num8 + num15, array[(num19 + 2) % 8], array[(num19 + 3) % 8], currentLightRGBByCorner[(tileSide <= 3) ? 1 : 2], flags);
		fastVec3f = quadOffsets[5];
		meshData.AddVertexWithFlags(num5 + fastVec3f.X * num8 + num11, num6 + fastVec3f.Y * num8 + num13, num7 + fastVec3f.Z * num8 + num17, array[num19], array[num19 + 1], currentLightRGBByCorner[(tileSide > 3) ? 3 : 0], flags);
		fastVec3f = quadOffsets[7];
		meshData.AddVertexWithFlags(num5 + fastVec3f.X * num8 + num12, num6 + fastVec3f.Y * num8 + num14, num7 + fastVec3f.Z * num8 + num18, array[(num19 + 6) % 8], array[(num19 + 7) % 8], currentLightRGBByCorner[(tileSide > 3) ? 1 : 2], flags);
		meshData.CustomInts.Add4(colorMapDataValue);
		meshData.AddQuadIndices(verticesCount);
	}

	private void cropRightSide(ref float uvx1, ref float uvx2, ref float uvy1, ref float uvy2, float xSize, float ySize, float excess, int rot)
	{
		switch (rot % 8)
		{
		case 0:
		case 6:
			uvx1 += xSize * excess;
			break;
		case 1:
		case 5:
			uvy1 += ySize * excess;
			break;
		case 2:
		case 4:
			uvx2 -= xSize * excess;
			break;
		case 3:
		case 7:
			uvy2 -= ySize * excess;
			break;
		}
	}

	private void cropLeftSide(ref float uvx1, ref float uvx2, ref float uvy1, ref float uvy2, float xSize, float ySize, float excess, int rot)
	{
		switch (rot % 8)
		{
		case 2:
		case 4:
			uvx1 += xSize * excess;
			break;
		case 3:
		case 7:
			uvy1 += ySize * excess;
			break;
		case 0:
		case 6:
			uvx2 -= xSize * excess;
			break;
		case 1:
		case 5:
			uvy2 -= ySize * excess;
			break;
		}
	}

	private void cropTopSide(ref float uvx1, ref float uvx2, ref float uvy1, ref float uvy2, float xSize, float ySize, float excess, int rot)
	{
		switch (rot % 8)
		{
		case 3:
		case 5:
			uvx1 += xSize * excess;
			break;
		case 0:
		case 4:
			uvy1 += ySize * excess;
			break;
		case 1:
		case 7:
			uvx2 -= xSize * excess;
			break;
		case 2:
		case 6:
			uvy2 -= ySize * excess;
			break;
		}
	}

	private void cropBottomSide(ref float uvx1, ref float uvx2, ref float uvy1, ref float uvy2, float xSize, float ySize, float excess, int rot)
	{
		switch (rot % 8)
		{
		case 1:
		case 7:
			uvx1 += xSize * excess;
			break;
		case 2:
		case 6:
			uvy1 += ySize * excess;
			break;
		case 3:
		case 5:
			uvx2 -= xSize * excess;
			break;
		case 0:
		case 4:
			uvy2 -= ySize * excess;
			break;
		}
	}

	private bool CaveArtBlockOnSide(TCTCache vars, int tileSide, BlockFacing neibDir)
	{
		int num = vars.extIndex3d;
		EnumBlockMaterial blockMaterial = vars.tct.currentChunkBlocksExt[num].BlockMaterial;
		switch (neibDir.Index)
		{
		case 0:
			num -= 34;
			break;
		case 1:
			num++;
			break;
		case 2:
			num += 34;
			break;
		case 3:
			num--;
			break;
		case 4:
			num += 1156;
			break;
		case 5:
			num -= 1156;
			break;
		}
		Block block = vars.tct.currentChunkBlocksExt[num];
		if (block.SideSolid[tileSide])
		{
			return block.BlockMaterial == blockMaterial;
		}
		return false;
	}
}
