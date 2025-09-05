using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class CrossTesselator : IBlockTesselator
{
	private static Vec3f startRot = new Vec3f();

	private static Vec3f endRot = new Vec3f();

	public void Tesselate(TCTCache vars)
	{
		vars.drawFaceFlags = 3;
		DrawCross(vars, 1.41f);
	}

	public static void DrawCross(TCTCache vars, float vScaleY)
	{
		Block block = vars.block;
		TextureAtlasPosition[] textureAtlasPositionsByTextureSubId = vars.textureAtlasPositionsByTextureSubId;
		int[] fastBlockTextureSubidsByFace = vars.fastBlockTextureSubidsByFace;
		bool hasAlternates = block.HasAlternates;
		bool randomizeRotations = block.RandomizeRotations;
		int value = vars.ColorMapData.Value;
		int num = block.VertexFlags.All & -503316481;
		int all = block.VertexFlags.All;
		num |= BlockFacing.UP.NormalPackedFlags;
		all |= BlockFacing.UP.NormalPackedFlags;
		int color = vars.tct.currentChunkRgbsExt[vars.extIndex3d];
		BakedCompositeTexture[][] array = null;
		int k = 0;
		if (hasAlternates || randomizeRotations)
		{
			if (hasAlternates)
			{
				array = block.FastTextureVariants;
			}
			k = GameMath.MurmurHash3(vars.posX, (block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? vars.posY : 0, vars.posZ);
		}
		MeshData[] poolForPass = vars.tct.GetPoolForPass(block.RenderPass, (!vars.block.DoNotRenderAtLod2) ? 1 : 2);
		for (int num2 = 0; num2 < 2; num2++)
		{
			int num3 = num2 * 2;
			int num4;
			if (hasAlternates)
			{
				BakedCompositeTexture[] array2 = array[num3];
				if (array2 != null)
				{
					num4 = array2[GameMath.Mod(k, array2.Length)].TextureSubId;
					goto IL_011b;
				}
			}
			num4 = fastBlockTextureSubidsByFace[num3];
			goto IL_011b;
			IL_011b:
			TextureAtlasPosition textureAtlasPosition = textureAtlasPositionsByTextureSubId[num4];
			MeshData obj = poolForPass[textureAtlasPosition.atlasNumber];
			int verticesCount = obj.VerticesCount;
			float finalX = vars.finalX;
			float num5 = vars.finalY;
			float num6 = vars.finalZ;
			float x;
			float y;
			float z;
			if (randomizeRotations)
			{
				float[] matrix = TesselationMetaData.randomRotMatrices[GameMath.Mod(k, TesselationMetaData.randomRotations.Length)];
				Mat4f.MulWithVec3_Position(matrix, num2, 0f, 0f, startRot);
				Mat4f.MulWithVec3_Position(matrix, 1f - (float)num2, vScaleY, 1f, endRot);
				x = endRot.X + finalX;
				finalX += startRot.X;
				y = endRot.Y + num5;
				num5 += startRot.Y;
				z = endRot.Z + num6;
				num6 += startRot.Z;
			}
			else
			{
				x = finalX + (1f - (float)num2);
				y = num5 + vScaleY;
				z = num6 + 1f;
				finalX += (float)num2;
			}
			obj.AddVertexWithFlags(x, num5, z, textureAtlasPosition.x2, textureAtlasPosition.y2, color, num);
			obj.AddVertexWithFlags(x, y, z, textureAtlasPosition.x2, textureAtlasPosition.y1, color, all);
			obj.AddVertexWithFlags(finalX, y, num6, textureAtlasPosition.x1, textureAtlasPosition.y1, color, all);
			obj.AddVertexWithFlags(finalX, num5, num6, textureAtlasPosition.x1, textureAtlasPosition.y2, color, num);
			vars.UpdateChunkMinMax(finalX, num5, num6);
			vars.UpdateChunkMinMax(x, y, z);
			obj.CustomInts.Add4(value);
			obj.AddQuadIndices(verticesCount);
		}
	}
}
