using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class TCTCache : IGeometryTester
{
	public const long DARK = 789516L;

	public FastVec3f[][] blockFaceVertices = CubeFaceVertices.blockFaceVertices;

	public int lx;

	public int ly;

	public int lz;

	public int posX;

	public int posY;

	public int posZ;

	public int dimension;

	public int extIndex3d;

	public int index3d;

	public float finalX;

	public float finalY;

	public float finalZ;

	public float xMin;

	public float xMax;

	public float yMin;

	public float yMax;

	public float zMin;

	public float zMax;

	public int drawFaceFlags;

	public int blockId;

	public Block block;

	public ShapeTesselatorManager shapes;

	public float[] preRotationMatrix;

	public int textureSubId;

	public float textureVOffset;

	public int decorSubPosition;

	public int decorRotationData;

	public ColorMapData ColorMapData;

	public int VertexFlags;

	public EnumChunkRenderPass RenderPass;

	public float occ;

	public float halfoccInverted;

	private readonly int[] neighbourLightRGBS;

	public readonly int[] CurrentLightRGBByCorner;

	public ChunkTesselator tct;

	public TextureAtlasPosition[] textureAtlasPositionsByTextureSubId;

	public int[] fastBlockTextureSubidsByFace;

	public bool aoAndSmoothShadows;

	public const int chunkSize = 32;

	public const int extChunkSize = 34;

	public const int extMovey = 1156;

	internal Dictionary<BlockPos, BlockEntity> blockEntitiesOfChunk = new Dictionary<BlockPos, BlockEntity>();

	private BlockPos tmpPos = new BlockPos();

	public ushort[] rainHeightMap;

	public int OceanityFlagTL;

	public int OceanityFlagTR;

	public int OceanityFlagBL;

	public int OceanityFlagBR;

	public TCTCache(ChunkTesselator tct)
	{
		this.tct = tct;
		blockFaceVertices = CubeFaceVertices.blockFaceVertices;
		occ = 0.67f;
		halfoccInverted = 0.0196875f;
		neighbourLightRGBS = new int[9];
		CurrentLightRGBByCorner = new int[4];
	}

	internal void Start(ClientMain game)
	{
		shapes = game.TesselatorManager;
	}

	internal void SetDimension(int dim)
	{
		dimension = dim;
		tmpPos.SetDimension(dim);
	}

	internal long CalcBlockFaceLight(int tileSide, int extNeibIndex3d)
	{
		int num = extIndex3d;
		int[] currentLightRGBByCorner = CurrentLightRGBByCorner;
		if (!aoAndSmoothShadows || !this.block.SideAo[tileSide])
		{
			int num2 = tct.currentChunkRgbsExt[extNeibIndex3d];
			if (this.block.DrawType == EnumDrawType.JSON && !this.block.SideAo[tileSide])
			{
				int num3 = (int)(GameMath.Clamp(((float?)tct.currentChunkBlocksExt[extNeibIndex3d]?.LightAbsorption / 32f).GetValueOrDefault(), 0f, 1f) * 255f);
				int num4 = tct.currentChunkRgbsExt[num];
				int num5 = Math.Max((num4 >> 24) & 0xFF, ((num2 >> 24) & 0xFF) - num3);
				int num6 = ColorUtil.Rgb2HSv(num2);
				int num7 = num6 & 0xFF;
				int num8 = Math.Max(0, num7 - num3);
				if (num8 != num7)
				{
					num6 = (num6 & 0xFFFF00) | num8;
					num2 = ColorUtil.Hsv2Rgb(num6);
				}
				int num9 = Math.Max((byte)(num4 >> 16), (byte)(num2 >> 16));
				int num10 = Math.Max((byte)(num4 >> 8), (byte)(num2 >> 8));
				int num11 = Math.Max((byte)num4, (byte)num2);
				num2 = (num5 << 24) | (num9 << 16) | (num10 << 8) | num11;
			}
			currentLightRGBByCorner[0] = (currentLightRGBByCorner[1] = (currentLightRGBByCorner[2] = (currentLightRGBByCorner[3] = num2)));
			return num2 * 4;
		}
		int[] array = neighbourLightRGBS;
		int[] currentChunkRgbsExt = tct.currentChunkRgbsExt;
		Block[] currentChunkBlocksExt = tct.currentChunkBlocksExt;
		Block[] currentChunkFluidBlocksExt = tct.currentChunkFluidBlocksExt;
		Vec3iAndFacingFlags[] array2 = CubeFaceVertices.blockFaceVerticesCentered[tileSide];
		int num12 = currentChunkRgbsExt[extNeibIndex3d];
		bool flag = this.block.BlockMaterial == EnumBlockMaterial.Leaves;
		Block block = currentChunkFluidBlocksExt[extNeibIndex3d];
		bool flag2;
		if (block.LightAbsorption > 0)
		{
			flag2 = true;
		}
		else
		{
			BlockFacing blockFacing = BlockFacing.ALLFACES[tileSide];
			block = currentChunkBlocksExt[extNeibIndex3d];
			flag2 = block.DoEmitSideAo(this, blockFacing.Opposite);
		}
		float frontAo = (flag2 ? occ : 1f);
		int num13 = 0;
		int num14 = 0;
		int num15 = 0;
		while (num15 < 8)
		{
			num13 <<= 1;
			Vec3iAndFacingFlags vec3iAndFacingFlags = array2[num15];
			int num16 = num + vec3iAndFacingFlags.extIndexOffset;
			Block block2 = currentChunkFluidBlocksExt[num16];
			if (block2.LightAbsorption > 0)
			{
				flag2 = false;
				num13 |= 1;
				if (num15 <= 3)
				{
					num13 <<= 1;
					num13 |= 1;
				}
				else
				{
					num14 <<= 1;
					if (!block.DoEmitSideAoByFlag(this, array2[8], vec3iAndFacingFlags.FacingFlags) || (block.ForFluidsLayer && block.LightAbsorption > 0))
					{
						num14 |= 1;
					}
				}
			}
			else
			{
				block2 = currentChunkBlocksExt[num16];
				if (num15 <= 3)
				{
					flag2 = block2.DoEmitSideAoByFlag(this, vec3iAndFacingFlags, vec3iAndFacingFlags.OppositeFlagsUpperOrLeft) || (flag && block2.BlockMaterial == EnumBlockMaterial.Leaves);
					if (!flag2)
					{
						num13 |= 1;
					}
					num13 <<= 1;
					if (!block2.DoEmitSideAoByFlag(this, vec3iAndFacingFlags, vec3iAndFacingFlags.OppositeFlagsLowerOrRight) && (!flag || block2.BlockMaterial != EnumBlockMaterial.Leaves))
					{
						num13 |= 1;
						flag2 = false;
					}
				}
				else
				{
					num14 <<= 1;
					flag2 = block2.DoEmitSideAoByFlag(this, vec3iAndFacingFlags, vec3iAndFacingFlags.OppositeFlags) || (flag && block2.BlockMaterial == EnumBlockMaterial.Leaves);
					if (!flag2)
					{
						num13 |= 1;
					}
					if (!block.DoEmitSideAoByFlag(this, array2[8], vec3iAndFacingFlags.FacingFlags) || (block.ForFluidsLayer && block.LightAbsorption > 0))
					{
						num14 |= 1;
					}
				}
			}
			num15++;
			array[num15] = (flag2 ? num12 : currentChunkRgbsExt[num16]);
		}
		int ndirbetween = 8 * (num13 & 1);
		int ndirbetween2 = 7 * ((num13 >>= 1) & 1);
		int ndirbetween3 = 6 * ((num13 >>= 1) & 1);
		int ndirbetween4 = 5 * ((num13 >>= 1) & 1);
		int ndir = 4 * ((num13 >>= 1) & 1);
		int ndir2 = 4 * ((num13 >>= 1) & 1);
		int ndir3 = 3 * ((num13 >>= 1) & 1);
		int ndir4 = 3 * ((num13 >>= 1) & 1);
		int ndir5 = 2 * ((num13 >>= 1) & 1);
		int ndir6 = 2 * ((num13 >>= 1) & 1);
		int ndir7 = (num13 >>= 1) & 1;
		int ndir8 = num13 >> 1;
		ushort s = (ushort)((num12 >> 24) & 0xFF);
		ushort r = (ushort)((num12 >> 16) & 0xFF);
		ushort g = (ushort)((num12 >> 8) & 0xFF);
		ushort b = (ushort)(num12 & 0xFF);
		return (long)(currentLightRGBByCorner[0] = CornerAoRGB(ndir7, ndir4, ndirbetween4, num14 & 1, frontAo, s, r, g, b)) + (long)(currentLightRGBByCorner[1] = CornerAoRGB(ndir8, ndir2, ndirbetween3, (num14 >> 1) & 1, frontAo, s, r, g, b)) + (currentLightRGBByCorner[2] = CornerAoRGB(ndir5, ndir3, ndirbetween2, (num14 >> 2) & 1, frontAo, s, r, g, b)) + (currentLightRGBByCorner[3] = CornerAoRGB(ndir6, ndir, ndirbetween, (num14 >> 3) & 1, frontAo, s, r, g, b));
	}

	private int CornerAoRGB(int ndir1, int ndir2, int ndirbetween, int frontCorner, float frontAo, ushort s, ushort r, ushort g, ushort b)
	{
		float num2;
		if (ndir1 + ndir2 == 0 || frontCorner + ndirbetween == 0)
		{
			float num = halfoccInverted * (float)GameMath.Clamp(block.LightAbsorption, 0, 32);
			num2 = Math.Min(occ, 1f - num);
		}
		else
		{
			num2 = ((ndir1 * ndir2 * ndirbetween == 0) ? occ : frontAo);
			int num3 = 1;
			if (ndir1 > 0)
			{
				int num4 = neighbourLightRGBS[ndir1];
				s += (ushort)((num4 >> 24) & 0xFF);
				r += (ushort)((num4 >> 16) & 0xFF);
				g += (ushort)((num4 >> 8) & 0xFF);
				b += (ushort)(num4 & 0xFF);
				num3++;
			}
			if (ndir2 > 0)
			{
				int num4 = neighbourLightRGBS[ndir2];
				s += (ushort)((num4 >> 24) & 0xFF);
				r += (ushort)((num4 >> 16) & 0xFF);
				g += (ushort)((num4 >> 8) & 0xFF);
				b += (ushort)(num4 & 0xFF);
				num3++;
			}
			if (ndirbetween > 0)
			{
				int num4 = neighbourLightRGBS[ndirbetween];
				s += (ushort)((num4 >> 24) & 0xFF);
				r += (ushort)((num4 >> 16) & 0xFF);
				g += (ushort)((num4 >> 8) & 0xFF);
				b += (ushort)(num4 & 0xFF);
				num3++;
			}
			num2 /= (float)num3;
		}
		return ((int)((float)(int)s * num2) << 24) | ((int)((float)(int)r * num2) << 16) | ((int)((float)(int)g * num2) << 8) | (int)((float)(int)b * num2);
	}

	public BlockEntity GetCurrentBlockEntityOnSide(BlockFacing side)
	{
		tmpPos.Set(posX, posY, posZ).Offset(side);
		return tct.game.BlockAccessor.GetBlockEntity(tmpPos);
	}

	public BlockEntity GetCurrentBlockEntityOnSide(Vec3iAndFacingFlags neibOffset)
	{
		tmpPos.Set(posX + neibOffset.X, posY + neibOffset.Y, posZ + neibOffset.Z);
		return tct.game.BlockAccessor.GetBlockEntity(tmpPos);
	}

	public void UpdateChunkMinMax(float x, float y, float z)
	{
		if (x < xMin)
		{
			xMin = x;
		}
		else if (x > xMax)
		{
			xMax = x;
		}
		if (y < yMin)
		{
			yMin = y;
		}
		else if (y > yMax)
		{
			yMax = y;
		}
		if (z < zMin)
		{
			zMin = z;
		}
		else if (z > zMax)
		{
			zMax = z;
		}
	}
}
