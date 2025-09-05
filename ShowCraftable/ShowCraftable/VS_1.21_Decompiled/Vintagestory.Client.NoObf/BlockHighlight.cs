using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class BlockHighlight
{
	public MeshRef modelRef;

	public BlockPos origin;

	public BlockPos[] attachmentPoints;

	public Vec3i Size;

	public EnumHighlightBlocksMode mode;

	public EnumHighlightShape shape;

	public float Scale = 1f;

	private int defaultColor = ColorUtil.ToRgba(96, (int)(GuiStyle.DialogDefaultBgColor[2] * 255.0), (int)(GuiStyle.DialogDefaultBgColor[1] * 255.0), (int)(GuiStyle.DialogDefaultBgColor[0] * 255.0));

	public void TesselateModel(ClientMain game, BlockPos[] positions, int[] colors)
	{
		if (modelRef != null)
		{
			game.Platform.DeleteMesh(modelRef);
			modelRef = null;
		}
		if (positions.Length == 0)
		{
			return;
		}
		switch (shape)
		{
		case EnumHighlightShape.Arbitrary:
		case EnumHighlightShape.Cylinder:
			TesselateArbitraryModel(game, positions, colors);
			break;
		case EnumHighlightShape.Cube:
			if (positions.Length == 0)
			{
				modelRef = null;
			}
			else if (positions.Length == 2)
			{
				int color = defaultColor;
				if (colors != null && colors.Length != 0)
				{
					color = colors[0];
				}
				TesselateCubeModel(game, positions[0], positions[1], color);
			}
			else
			{
				TesselateArbitraryModel(game, positions, colors);
			}
			break;
		case EnumHighlightShape.Cubes:
		{
			if (positions.Length < 2 || positions.Length % 2 != 0)
			{
				modelRef = null;
				break;
			}
			MeshData meshData = new MeshData(24, 36, withNormals: false, withUv: false, withRgba: true, withFlags: false);
			int num = defaultColor;
			if (colors != null && colors.Length != 0)
			{
				num = colors[0];
			}
			bool flag = colors != null && colors.Length >= positions.Length / 2;
			BlockPos blockPos = positions[0];
			BlockPos blockPos2 = positions[1];
			origin = new BlockPos(Math.Min(blockPos.X, blockPos2.X), Math.Min(blockPos.Y, blockPos2.Y), Math.Min(blockPos.Z, blockPos2.Z));
			for (int i = 0; i < positions.Length; i += 2)
			{
				GenCubeModel(game, meshData, positions[i], positions[i + 1], flag ? colors[i / 2] : num);
			}
			modelRef = game.Platform.UploadMesh(meshData);
			break;
		}
		case EnumHighlightShape.Ball:
			break;
		}
	}

	private void TesselateCubeModel(ClientMain game, BlockPos start, BlockPos end, int color)
	{
		origin = new BlockPos(Math.Min(start.X, end.X), Math.Min(start.InternalY, end.InternalY), Math.Min(start.Z, end.Z));
		MeshData meshData = new MeshData(24, 36, withNormals: false, withUv: false, withRgba: true, withFlags: false);
		GenCubeModel(game, meshData, start, end, color);
		modelRef = game.Platform.UploadMesh(meshData);
	}

	private void GenCubeModel(ClientMain game, MeshData intoMesh, BlockPos start, BlockPos end, int color)
	{
		BlockPos blockPos = new BlockPos(Math.Min(start.X, end.X), Math.Min(start.InternalY, end.InternalY), Math.Min(start.Z, end.Z));
		int num = Math.Max(start.X, end.X) - blockPos.X;
		int num2 = Math.Max(start.InternalY, end.InternalY) - blockPos.InternalY;
		int num3 = Math.Max(start.Z, end.Z) - blockPos.Z;
		if (num == 0 || num2 == 0 || num3 == 0)
		{
			game.Logger.Error("Cannot generate block highlight. Highlight width, height and length must be above 0");
			return;
		}
		if (mode == EnumHighlightBlocksMode.CenteredToSelectedBlock || mode == EnumHighlightBlocksMode.AttachedToSelectedBlock)
		{
			origin.X = 0;
			origin.Y = 0;
			origin.Z = 0;
			attachmentPoints = new BlockPos[6];
			for (int i = 0; i < 6; i++)
			{
				Vec3i vec3i = BlockFacing.ALLNORMALI[i];
				attachmentPoints[i] = new BlockPos(num / 2 * vec3i.X, num2 / 2 * vec3i.Y, num3 / 2 * vec3i.Z);
			}
		}
		Vec3f centerXyz = new Vec3f((float)num / 2f + (float)blockPos.X - (float)origin.X, (float)num2 / 2f + (float)blockPos.InternalY - (float)origin.Y, (float)num3 / 2f + (float)blockPos.Z - (float)origin.Z);
		Vec3f sizeXyz = new Vec3f(num, num2, num3);
		float[] defaultBlockSideShadingsByFacing = CubeMeshUtil.DefaultBlockSideShadingsByFacing;
		for (int j = 0; j < 6; j++)
		{
			BlockFacing blockFacing = BlockFacing.ALLFACES[j];
			ModelCubeUtilExt.AddFaceSkipTex(intoMesh, blockFacing, centerXyz, sizeXyz, color, defaultBlockSideShadingsByFacing[blockFacing.Index]);
		}
	}

	private void TesselateArbitraryModel(ClientMain game, BlockPos[] positions, int[] colors)
	{
		Dictionary<BlockPos, int> dictionary = new Dictionary<BlockPos, int>();
		BlockPos blockPos = positions[0].Copy();
		BlockPos blockPos2 = positions[0].Copy();
		foreach (BlockPos blockPos3 in positions)
		{
			blockPos.X = Math.Min(blockPos.X, blockPos3.X);
			blockPos.Y = Math.Min(blockPos.Y, blockPos3.Y);
			blockPos.Z = Math.Min(blockPos.Z, blockPos3.Z);
			blockPos2.X = Math.Max(blockPos2.X, blockPos3.X);
			blockPos2.Y = Math.Max(blockPos2.Y, blockPos3.Y);
			blockPos2.Z = Math.Max(blockPos2.Z, blockPos3.Z);
			dictionary[blockPos3] = 0;
		}
		foreach (BlockPos blockPos3 in positions)
		{
			int num = 0;
			if (!dictionary.ContainsKey(blockPos3.AddCopy(BlockFacing.NORTH)))
			{
				num |= BlockFacing.NORTH.Flag;
			}
			if (!dictionary.ContainsKey(blockPos3.AddCopy(BlockFacing.EAST)))
			{
				num |= BlockFacing.EAST.Flag;
			}
			if (!dictionary.ContainsKey(blockPos3.AddCopy(BlockFacing.SOUTH)))
			{
				num |= BlockFacing.SOUTH.Flag;
			}
			if (!dictionary.ContainsKey(blockPos3.AddCopy(BlockFacing.WEST)))
			{
				num |= BlockFacing.WEST.Flag;
			}
			if (!dictionary.ContainsKey(blockPos3.AddCopy(BlockFacing.UP)))
			{
				num |= BlockFacing.UP.Flag;
			}
			if (!dictionary.ContainsKey(blockPos3.AddCopy(BlockFacing.DOWN)))
			{
				num |= BlockFacing.DOWN.Flag;
			}
			dictionary[blockPos3] = num;
		}
		origin = blockPos.Copy();
		if (mode == EnumHighlightBlocksMode.CenteredToSelectedBlock || mode == EnumHighlightBlocksMode.AttachedToSelectedBlock || mode == EnumHighlightBlocksMode.CenteredToBlockSelectionIndex || mode == EnumHighlightBlocksMode.AttachedToBlockSelectionIndex)
		{
			origin.X = 0;
			origin.Y = 0;
			origin.Z = 0;
			if (shape == EnumHighlightShape.Cube)
			{
				Size = new Vec3i(blockPos2.X - blockPos.X + 1, blockPos2.Y - blockPos.Y + 1, blockPos2.Z - blockPos.Z + 1);
			}
			else
			{
				Size = new Vec3i(blockPos2.X - blockPos.X, blockPos2.Y - blockPos.Y, blockPos2.Z - blockPos.Z);
			}
			attachmentPoints = new BlockPos[6];
			for (int k = 0; k < 6; k++)
			{
				Vec3i vec3i = BlockFacing.ALLNORMALI[k];
				if (shape == EnumHighlightShape.Cylinder)
				{
					attachmentPoints[k] = new BlockPos((int)((float)Size.X / 2f * (float)vec3i.X), (int)Math.Ceiling((float)Size.Y / 2f * (float)vec3i.Y), (int)((float)Size.Z / 2f * (float)vec3i.Z));
					if (k == BlockFacing.DOWN.Index)
					{
						attachmentPoints[k].Y--;
					}
					if (k == BlockFacing.WEST.Index)
					{
						attachmentPoints[k].X--;
					}
					if (k == BlockFacing.NORTH.Index)
					{
						attachmentPoints[k].Z--;
					}
				}
				else if (shape == EnumHighlightShape.Cube)
				{
					attachmentPoints[k] = new BlockPos((int)((float)Size.X / 2f * (float)vec3i.X), (int)((float)Size.Y / 2f * (float)vec3i.Y), (int)((float)Size.Z / 2f * (float)vec3i.Z));
					if (Size.Y == 1 && k == BlockFacing.DOWN.Index)
					{
						attachmentPoints[k].Y--;
					}
					if (Size.X == 1 && k == BlockFacing.WEST.Index)
					{
						attachmentPoints[k].X--;
					}
					if (Size.Z == 1 && k == BlockFacing.NORTH.Index)
					{
						attachmentPoints[k].Z--;
					}
				}
				else
				{
					attachmentPoints[k] = new BlockPos((int)((float)Size.X / 2f * (float)vec3i.X), (int)((float)Size.Y / 2f * (float)vec3i.Y), (int)((float)Size.Z / 2f * (float)vec3i.Z));
					if (k == BlockFacing.DOWN.Index)
					{
						attachmentPoints[k].Y--;
					}
					if (k == BlockFacing.WEST.Index)
					{
						attachmentPoints[k].X--;
					}
					if (k == BlockFacing.NORTH.Index)
					{
						attachmentPoints[k].Z--;
					}
				}
			}
		}
		MeshData meshData = new MeshData(positions.Length * 4 * 6, positions.Length * 6 * 6, withNormals: false, withUv: false, withRgba: true, withFlags: false);
		Vec3f vec3f = new Vec3f();
		Vec3f sizeXyz = new Vec3f(1f, 1f, 1f);
		int num2 = defaultColor;
		if (colors != null && colors.Length != 0)
		{
			num2 = colors[0];
		}
		bool flag = colors != null && colors.Length >= positions.Length && colors.Length > 1;
		float[] defaultBlockSideShadingsByFacing = CubeMeshUtil.DefaultBlockSideShadingsByFacing;
		int num3 = 0;
		foreach (KeyValuePair<BlockPos, int> item in dictionary)
		{
			int value = item.Value;
			vec3f.X = (float)(item.Key.X - origin.X) + 0.5f;
			vec3f.Y = (float)(item.Key.InternalY - origin.Y) + 0.5f;
			vec3f.Z = (float)(item.Key.Z - origin.Z) + 0.5f;
			for (int l = 0; l < 6; l++)
			{
				BlockFacing blockFacing = BlockFacing.ALLFACES[l];
				if ((value & blockFacing.Flag) != 0)
				{
					ModelCubeUtilExt.AddFaceSkipTex(meshData, blockFacing, vec3f, sizeXyz, flag ? colors[num3] : num2, defaultBlockSideShadingsByFacing[blockFacing.Index]);
				}
			}
			num3++;
		}
		modelRef = game.Platform.UploadMesh(meshData);
	}

	internal void Dispose(ClientMain game)
	{
		if (modelRef != null)
		{
			game.Platform.DeleteMesh(modelRef);
		}
	}
}
