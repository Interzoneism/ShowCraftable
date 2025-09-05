using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class LiquidTesselator : IBlockTesselator
{
	private readonly int extChunkSize;

	private readonly Block[] extChunkDataFluids;

	private readonly Block[] extChunkDataBlocks;

	internal bool[] isLiquidBlock;

	private readonly int moveUp;

	private readonly int moveSouth;

	private readonly int moveNorthWest;

	private readonly int moveNorthEast;

	private readonly int moveSouthWest;

	private readonly int moveSouthEast;

	private readonly int moveAboveNorth;

	private readonly int moveAboveSouth;

	private readonly int moveAboveEast;

	private readonly int moveAboveWest;

	private const int byte0 = 2;

	private const int byte1 = 3;

	private int lavaFlag;

	private int extraFlags;

	private int chunksize;

	private BlockPos tmpPos = new BlockPos();

	private readonly float[] waterStillFlowVector = new float[8];

	private readonly float[] waterDownFlowVector = new float[8] { 0f, -1f, 0f, -1f, 0f, -1f, 0f, -1f };

	private readonly int[] shouldWave = new int[24];

	private float[] flowVectorsN;

	private float[] flowVectorsE;

	private float[] flowVectorsS;

	private float[] flowVectorsW;

	private float[] upFlowVectors = new float[8];

	private FastVec3f[] upQuadOffsets = new FastVec3f[8];

	public LiquidTesselator(ChunkTesselator tct)
	{
		chunksize = 32;
		extChunkSize = 34;
		extChunkDataFluids = tct.currentChunkFluidBlocksExt;
		extChunkDataBlocks = tct.currentChunkBlocksExt;
		moveUp = extChunkSize * extChunkSize;
		moveSouth = extChunkSize;
		moveNorthWest = -extChunkSize - 1;
		moveNorthEast = -extChunkSize + 1;
		moveSouthWest = extChunkSize - 1;
		moveSouthEast = extChunkSize + 1;
		moveAboveNorth = (extChunkSize - 1) * extChunkSize;
		moveAboveSouth = (extChunkSize + 1) * extChunkSize;
		moveAboveEast = extChunkSize * extChunkSize + 1;
		moveAboveWest = extChunkSize * extChunkSize - 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool SideSolid(int extIndex3d, BlockFacing facing)
	{
		bool flag = extChunkDataFluids[extIndex3d].SideSolid[facing.Index];
		if (!flag)
		{
			flag = extChunkDataBlocks[extIndex3d].SideSolid[facing.Index];
		}
		return flag;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsSameLiquid(int extIndex3d)
	{
		return isLiquidBlock[extChunkDataFluids[extIndex3d].BlockId];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int SameLiquidLevelAt(int extIndex3d)
	{
		return extChunkDataFluids[extIndex3d].LiquidLevel;
	}

	public void Tesselate(TCTCache vars)
	{
		if (isLiquidBlock == null)
		{
			isLiquidBlock = vars.tct.isLiquidBlock;
		}
		int extIndex3d = vars.extIndex3d;
		int liquidLevel = vars.block.LiquidLevel;
		float num = ChunkTesselator.waterLevels[liquidLevel];
		IBlockFlowing blockFlowing = vars.block as IBlockFlowing;
		lavaFlag = ((blockFlowing != null && blockFlowing.IsLava) ? 134217728 : 0);
		extraFlags = 0;
		Block block = extChunkDataFluids[extIndex3d + moveUp];
		_ = extChunkDataFluids[extIndex3d - moveUp];
		Block block2 = extChunkDataBlocks[extIndex3d - moveUp];
		Block block3 = extChunkDataBlocks[extIndex3d];
		upFlowVectors.Fill(0f);
		float[] array = waterStillFlowVector;
		if (!block2.SideSolid.OnSide(BlockFacing.UP) || block2.Replaceable >= 6000)
		{
			flowVectorsN = (SideSolid(extIndex3d - moveSouth, BlockFacing.SOUTH) ? array : waterDownFlowVector);
			flowVectorsE = (SideSolid(extIndex3d + 1, BlockFacing.WEST) ? array : waterDownFlowVector);
			flowVectorsS = (SideSolid(extIndex3d + moveSouth, BlockFacing.NORTH) ? array : waterDownFlowVector);
			flowVectorsW = (SideSolid(extIndex3d - 1, BlockFacing.EAST) ? array : waterDownFlowVector);
		}
		else
		{
			flowVectorsN = (flowVectorsE = (flowVectorsS = (flowVectorsW = array)));
		}
		float num2 = 1f;
		float num3 = 1f;
		float num4 = 1f;
		float num5 = 1f;
		int num6 = 2;
		int num7 = 2;
		int num8 = 2;
		int num9 = 2;
		if (block.MatterState != EnumMatterState.Liquid)
		{
			float[] array2 = upFlowVectors;
			if (liquidLevel == 7)
			{
				bool flag = IsSameLiquid(extIndex3d + moveAboveWest);
				bool flag2 = IsSameLiquid(extIndex3d + moveAboveEast);
				bool num10 = IsSameLiquid(extIndex3d + moveAboveSouth);
				bool flag3 = IsSameLiquid(extIndex3d + moveAboveNorth);
				bool flag4 = flag3 || flag2 || IsSameLiquid(extIndex3d + moveAboveNorth + 1);
				bool flag5 = num10 || flag2 || IsSameLiquid(extIndex3d + moveAboveSouth + 1);
				bool flag6 = num10 || flag || IsSameLiquid(extIndex3d + moveAboveSouth - 1);
				bool num11 = flag3 || flag || IsSameLiquid(extIndex3d + moveAboveNorth - 1);
				num6 = (flag4 ? 2 : 3);
				num7 = (num11 ? 2 : 3);
				num8 = (flag5 ? 2 : 3);
				num9 = (flag6 ? 2 : 3);
				num2 = (num11 ? 1f : num);
				num3 = (flag6 ? 1f : num);
				num4 = (flag4 ? 1f : num);
				num5 = (flag5 ? 1f : num);
				if (num11 && flag6 && flag4 && flag5 && !vars.tct.isPartiallyTransparent[block.BlockId] && block.SideOpaque[5] && vars.drawFaceFlags == 16)
				{
					return;
				}
				Vec3i vec3i = blockFlowing?.FlowNormali ?? null;
				if (vec3i != null)
				{
					float num12 = (float)vec3i.X / 2f;
					float num13 = (float)vec3i.Z / 2f;
					array2[0] = num12;
					array2[1] = num13;
					array2[2] = num12;
					array2[3] = num13;
					array2[4] = num12;
					array2[5] = num13;
					array2[6] = num12;
					array2[7] = num13;
				}
			}
			else
			{
				int num14 = SameLiquidLevelAt(extIndex3d - 1);
				int num15 = SameLiquidLevelAt(extIndex3d + 1);
				int num16 = SameLiquidLevelAt(extIndex3d + moveSouth);
				int num17 = SameLiquidLevelAt(extIndex3d - moveSouth);
				int num18 = SameLiquidLevelAt(extIndex3d + moveNorthWest);
				int num19 = SameLiquidLevelAt(extIndex3d + moveNorthEast);
				int num20 = SameLiquidLevelAt(extIndex3d + moveSouthWest);
				int num21 = SameLiquidLevelAt(extIndex3d + moveSouthEast);
				int num22 = (IsSameLiquid(extIndex3d + moveAboveWest) ? 8 : 0);
				int num23 = (IsSameLiquid(extIndex3d + moveAboveEast) ? 8 : 0);
				int num24 = (IsSameLiquid(extIndex3d + moveAboveSouth) ? 8 : 0);
				int num25 = (IsSameLiquid(extIndex3d + moveAboveNorth) ? 8 : 0);
				int num26 = (IsSameLiquid(extIndex3d + moveAboveNorth - 1) ? 8 : 0);
				int num27 = (IsSameLiquid(extIndex3d + moveAboveNorth + 1) ? 8 : 0);
				int num28 = (IsSameLiquid(extIndex3d + moveAboveSouth - 1) ? 8 : 0);
				int num29 = (IsSameLiquid(extIndex3d + moveAboveSouth + 1) ? 8 : 0);
				num2 = ChunkTesselator.waterLevels[GameMath.Max(liquidLevel, num17, num14, num18, num22, num25, num26)];
				num3 = ChunkTesselator.waterLevels[GameMath.Max(liquidLevel, num16, num14, num20, num24, num22, num28)];
				num4 = ChunkTesselator.waterLevels[GameMath.Max(liquidLevel, num17, num15, num19, num23, num25, num27)];
				num5 = ChunkTesselator.waterLevels[GameMath.Max(liquidLevel, num16, num15, num21, num23, num24, num29)];
				num6 = ((num4 < 1f && num27 == 0 && num25 == 0 && num23 == 0) ? 3 : 2);
				num7 = ((num2 < 1f && num26 == 0 && num22 == 0 && num25 == 0) ? 3 : 2);
				num8 = ((num5 < 1f && num29 == 0 && num24 == 0 && num23 == 0) ? 3 : 2);
				num9 = ((num3 < 1f && num28 == 0 && num24 == 0 && num22 == 0) ? 3 : 2);
				Vec3i vec3i2 = blockFlowing?.FlowNormali ?? null;
				float num30;
				float num31;
				if (vec3i2 != null)
				{
					num30 = (float)vec3i2.X / 2f;
					num31 = (float)vec3i2.Z / 2f;
				}
				else
				{
					float num32 = Cmp(num2, num4);
					float num33 = Cmp(num3, num5);
					float num34 = Cmp(num2, num3);
					float num35 = Cmp(num4, num5);
					num30 = num32 + num33;
					num31 = num34 + num35;
				}
				array2[0] = num30;
				array2[1] = num31;
				array2[2] = num30;
				array2[3] = num31;
				array2[4] = num30;
				array2[5] = num31;
				array2[6] = num30;
				array2[7] = num31;
			}
		}
		int[] array3 = shouldWave;
		array3[16] = num9;
		array3[17] = num8;
		array3[18] = num7;
		array3[19] = num6;
		array3[0] = num7;
		array3[1] = num6;
		array3[8] = num8;
		array3[9] = num9;
		array3[4] = num6;
		array3[5] = num8;
		array3[12] = num9;
		array3[13] = num7;
		int num36 = 0;
		int drawFaceFlags = vars.drawFaceFlags;
		MeshData[] poolForPass = vars.tct.GetPoolForPass(EnumChunkRenderPass.Liquid, 1);
		bool num37 = (1 & drawFaceFlags) != 0;
		bool flag7 = (2 & drawFaceFlags) != 0;
		bool flag8 = (4 & drawFaceFlags) != 0;
		bool flag9 = (8 & drawFaceFlags) != 0;
		bool flag10 = false;
		bool flag11 = false;
		bool flag12 = false;
		bool flag13 = false;
		if (block3.Id != 0)
		{
			tmpPos.Set(vars.posX, vars.posY, vars.posZ);
			tmpPos.SetDimension(vars.dimension);
			flag10 = block3.SideIsSolid(tmpPos, BlockFacing.NORTH.Index);
			flag11 = block3.SideIsSolid(tmpPos, BlockFacing.EAST.Index);
			flag13 = block3.SideIsSolid(tmpPos, BlockFacing.SOUTH.Index);
			flag12 = block3.SideIsSolid(tmpPos, BlockFacing.WEST.Index);
		}
		if ((0x20 & drawFaceFlags) != 0)
		{
			vars.CalcBlockFaceLight(5, extIndex3d - moveUp);
			DrawLiquidBlockFace(vars, 5, 1f, 1f, 1f, 1f, vars.blockFaceVertices[5], upFlowVectors, 20, 1f, 1f, poolForPass);
			num36 += 4;
		}
		if ((0x10 & drawFaceFlags) != 0)
		{
			if (vars.block.LiquidLevel == 7 && vars.rainHeightMap[vars.posZ % chunksize * chunksize + vars.posX % chunksize] <= vars.posY)
			{
				extraFlags = int.MinValue;
			}
			vars.CalcBlockFaceLight(4, extIndex3d + moveUp);
			FastVec3f[] quadOffsets = vars.blockFaceVertices[4];
			if (flag10 || flag11 || flag13 || flag12)
			{
				float z = (flag10 ? 0.01f : 0f);
				float x = (flag11 ? 0.99f : 1f);
				float z2 = (flag13 ? 0.99f : 1f);
				float x2 = (flag12 ? 0.01f : 0f);
				upQuadOffsets[4] = new FastVec3f(x, 1f, z2);
				upQuadOffsets[5] = new FastVec3f(x2, 1f, z2);
				upQuadOffsets[6] = new FastVec3f(x, 1f, z);
				upQuadOffsets[7] = new FastVec3f(x2, 1f, z);
				quadOffsets = upQuadOffsets;
			}
			DrawLiquidBlockFace(vars, 4, num5, num3, num4, num2, quadOffsets, upFlowVectors, 16, 1f, 1f, poolForPass);
			num36 += 4;
			extraFlags = 0;
		}
		if (num37 && !flag10)
		{
			vars.CalcBlockFaceLight(0, extIndex3d - moveSouth);
			DrawLiquidBlockFace(vars, 0, num4, num2, 0f, 0f, vars.blockFaceVertices[0], flowVectorsN, 0, num2, num4, poolForPass);
			num36 += 4;
		}
		if (flag7 && !flag11)
		{
			vars.CalcBlockFaceLight(1, extIndex3d + 1);
			DrawLiquidBlockFace(vars, 1, num5, num4, 0f, 0f, vars.blockFaceVertices[1], flowVectorsE, 4, num4, num5, poolForPass);
			num36 += 4;
		}
		if (flag9 && !flag12)
		{
			vars.CalcBlockFaceLight(3, extIndex3d - 1);
			DrawLiquidBlockFace(vars, 3, num2, num3, 0f, 0f, vars.blockFaceVertices[3], flowVectorsW, 12, num2, num3, poolForPass);
			num36 += 4;
		}
		if (flag8 && !flag13)
		{
			vars.CalcBlockFaceLight(2, extIndex3d + moveSouth);
			DrawLiquidBlockFace(vars, 2, num3, num5, 0f, 0f, vars.blockFaceVertices[2], flowVectorsS, 8, num3, num5, poolForPass);
			num36 += 4;
		}
	}

	private void DrawLiquidBlockFace(TCTCache vars, int tileSide, float northSouthLevel, float southWestLevel, float northEastLevel, float southEastLevel, FastVec3f[] quadOffsets, float[] flowVectors, int shouldWaveOffset, float texHeightLeftRel, float texHeightRightRel, MeshData[] meshPools)
	{
		int value = vars.ColorMapData.Value;
		bool num = vars.RenderPass == EnumChunkRenderPass.Liquid;
		int num2 = vars.fastBlockTextureSubidsByFace[tileSide];
		TextureAtlasPosition textureAtlasPosition = vars.textureAtlasPositionsByTextureSubId[num2];
		MeshData meshData = meshPools[textureAtlasPosition.atlasNumber];
		CustomMeshDataPartInt customInts = meshData.CustomInts;
		int verticesCount = meshData.VerticesCount;
		float num3 = textureAtlasPosition.y2 - textureAtlasPosition.y1;
		int flags = vars.VertexFlags | BlockFacing.AllVertexFlagsNormals[tileSide] | extraFlags;
		float num4 = vars.lx;
		float num5 = vars.ly;
		float num6 = vars.lz;
		FastVec3f fastVec3f = quadOffsets[7];
		meshData.AddVertexWithFlags(num4 + fastVec3f.X, num5 + fastVec3f.Y * southEastLevel, num6 + fastVec3f.Z, textureAtlasPosition.x2, textureAtlasPosition.y1 + num3 * texHeightLeftRel, vars.CurrentLightRGBByCorner[3], flags);
		customInts.Add(value);
		if (num)
		{
			byte b = (byte)(texHeightLeftRel * 255f);
			customInts.Add(shouldWave[shouldWaveOffset + 2] | 0x3FC00 | (b << 18) | lavaFlag | vars.OceanityFlagTL);
		}
		fastVec3f = quadOffsets[5];
		meshData.AddVertexWithFlags(num4 + fastVec3f.X, num5 + fastVec3f.Y * southWestLevel, num6 + fastVec3f.Z, textureAtlasPosition.x2, textureAtlasPosition.y1, vars.CurrentLightRGBByCorner[1], flags);
		customInts.Add(value);
		if (num)
		{
			customInts.Add(shouldWave[shouldWaveOffset] | 0x3FC00 | lavaFlag | vars.OceanityFlagBL);
		}
		fastVec3f = quadOffsets[4];
		meshData.AddVertexWithFlags(num4 + fastVec3f.X, num5 + fastVec3f.Y * northSouthLevel, num6 + fastVec3f.Z, textureAtlasPosition.x1, textureAtlasPosition.y1, vars.CurrentLightRGBByCorner[0], flags);
		customInts.Add(value);
		if (num)
		{
			meshData.CustomInts.Add(shouldWave[shouldWaveOffset + 1] | lavaFlag | vars.OceanityFlagBR);
		}
		fastVec3f = quadOffsets[6];
		meshData.AddVertexWithFlags(num4 + fastVec3f.X, num5 + fastVec3f.Y * northEastLevel, num6 + fastVec3f.Z, textureAtlasPosition.x1, textureAtlasPosition.y1 + num3 * texHeightRightRel, vars.CurrentLightRGBByCorner[2], flags);
		customInts.Add(value);
		if (num)
		{
			byte b2 = (byte)(texHeightRightRel * 255f);
			customInts.Add(shouldWave[shouldWaveOffset + 3] | (b2 << 18) | lavaFlag | vars.OceanityFlagTR);
			meshData.CustomFloats.Add(flowVectors);
		}
		meshData.AddQuadIndices(verticesCount);
		vars.UpdateChunkMinMax(num4, num5, num6);
		vars.UpdateChunkMinMax(num4 + 1f, num5 + 1f, num6 + 1f);
	}

	private float Cmp(float val1, float val2)
	{
		if (!(val1 > val2))
		{
			if (val2 != val1)
			{
				return -0.5f;
			}
			return 0f;
		}
		return 0.5f;
	}
}
