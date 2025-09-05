using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VSSurvivalMod.Systems.ChiselModes;

public class RotateChiselMode : ChiselMode
{
	public override DrawSkillIconDelegate DrawAction(ICoreClientAPI capi)
	{
		return Drawrotate_svg;
	}

	public override bool Apply(BlockEntityChisel chiselEntity, IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool isBreak, byte currentMaterialIndex)
	{
		chiselEntity.RotateModel(isBreak ? 90 : (-90), null);
		chiselEntity.RebuildCuboidList();
		return true;
	}

	public void Drawrotate_svg(Context cr, int x, int y, float width, float height, double[] rgba)
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Expected O, but got Unknown
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Expected O, but got Unknown
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Expected O, but got Unknown
		Matrix matrix = cr.Matrix;
		cr.Save();
		float num = 119f;
		float num2 = 115f;
		float num3 = Math.Min(width / num, height / num2);
		matrix.Translate((double)((float)x + Math.Max(0f, (width - num * num3) / 2f)), (double)((float)y + Math.Max(0f, (height - num2 * num3) / 2f)));
		matrix.Scale((double)num3, (double)num3);
		cr.Matrix = matrix;
		cr.Operator = (Operator)2;
		cr.LineWidth = 15.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		Pattern val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(100.761719, 29.972656);
		cr.CurveTo(116.078125, 46.824219, 111.929688, 74.050781, 98.03125, 89.949219);
		cr.CurveTo(78.730469, 112.148438, 45.628906, 113.027344, 23.527344, 93.726563);
		cr.CurveTo(-13.023438, 56.238281, 17.898438, 7.355469, 61.082031, 7.5);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 219.348174, -337.87843);
		val.Matrix = matrix;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(81.890625, 11.0625);
		cr.CurveTo(86.824219, 21.769531, 91.550781, 36.472656, 92.332031, 47.808594);
		cr.LineTo(100.761719, 29.972656);
		cr.LineTo(118.585938, 21.652344);
		cr.CurveTo(107.269531, 20.804688, 92.609375, 15.976563, 81.890625, 11.0625);
		cr.ClosePath();
		cr.MoveTo(81.890625, 11.0625);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Restore();
	}
}
