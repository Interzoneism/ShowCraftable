using System;
using Cairo;
using Vintagestory.API.Client;

namespace VSSurvivalMod.Systems.ChiselModes;

public class FourByChiselMode : ChiselMode
{
	public override int ChiselSize => 4;

	public override DrawSkillIconDelegate DrawAction(ICoreClientAPI capi)
	{
		return Drawcreate16_svg;
	}

	public void Drawcreate16_svg(Context cr, int x, int y, float width, float height, double[] rgba)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Expected O, but got Unknown
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Expected O, but got Unknown
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Expected O, but got Unknown
		//IL_03a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a6: Expected O, but got Unknown
		//IL_047d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0483: Expected O, but got Unknown
		//IL_04b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b6: Expected O, but got Unknown
		//IL_05b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b6: Expected O, but got Unknown
		//IL_068d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0693: Expected O, but got Unknown
		//IL_06c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c6: Expected O, but got Unknown
		//IL_07c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c6: Expected O, but got Unknown
		//IL_089d: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a3: Expected O, but got Unknown
		//IL_08d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d6: Expected O, but got Unknown
		//IL_09d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d6: Expected O, but got Unknown
		//IL_0aad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab3: Expected O, but got Unknown
		//IL_0ae0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ae6: Expected O, but got Unknown
		//IL_0be0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0be6: Expected O, but got Unknown
		//IL_0cbd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cc3: Expected O, but got Unknown
		//IL_0cf0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cf6: Expected O, but got Unknown
		//IL_0df0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0df6: Expected O, but got Unknown
		//IL_0ecd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ed3: Expected O, but got Unknown
		//IL_0f00: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f06: Expected O, but got Unknown
		//IL_1000: Unknown result type (might be due to invalid IL or missing references)
		//IL_1006: Expected O, but got Unknown
		//IL_10dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_10e3: Expected O, but got Unknown
		//IL_1110: Unknown result type (might be due to invalid IL or missing references)
		//IL_1116: Expected O, but got Unknown
		//IL_1210: Unknown result type (might be due to invalid IL or missing references)
		//IL_1216: Expected O, but got Unknown
		//IL_12ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_12f3: Expected O, but got Unknown
		//IL_1320: Unknown result type (might be due to invalid IL or missing references)
		//IL_1326: Expected O, but got Unknown
		//IL_1420: Unknown result type (might be due to invalid IL or missing references)
		//IL_1426: Expected O, but got Unknown
		//IL_14fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_1503: Expected O, but got Unknown
		//IL_1530: Unknown result type (might be due to invalid IL or missing references)
		//IL_1536: Expected O, but got Unknown
		//IL_1630: Unknown result type (might be due to invalid IL or missing references)
		//IL_1636: Expected O, but got Unknown
		//IL_170d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1713: Expected O, but got Unknown
		//IL_1740: Unknown result type (might be due to invalid IL or missing references)
		//IL_1746: Expected O, but got Unknown
		//IL_1840: Unknown result type (might be due to invalid IL or missing references)
		//IL_1846: Expected O, but got Unknown
		//IL_191d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1923: Expected O, but got Unknown
		//IL_1950: Unknown result type (might be due to invalid IL or missing references)
		//IL_1956: Expected O, but got Unknown
		//IL_1a50: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a56: Expected O, but got Unknown
		//IL_1b2d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b33: Expected O, but got Unknown
		//IL_1b60: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b66: Expected O, but got Unknown
		//IL_1c60: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c66: Expected O, but got Unknown
		//IL_1d3d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d43: Expected O, but got Unknown
		//IL_1d70: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d76: Expected O, but got Unknown
		//IL_1e70: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e76: Expected O, but got Unknown
		//IL_1f4d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f53: Expected O, but got Unknown
		//IL_1f80: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f86: Expected O, but got Unknown
		//IL_2080: Unknown result type (might be due to invalid IL or missing references)
		//IL_2086: Expected O, but got Unknown
		//IL_215d: Unknown result type (might be due to invalid IL or missing references)
		//IL_2163: Expected O, but got Unknown
		Pattern val = null;
		Matrix matrix = cr.Matrix;
		cr.Save();
		float num = 146f;
		float num2 = 146f;
		float num3 = Math.Min(width / num, height / num2);
		matrix.Translate((double)((float)x + Math.Max(0f, (width - num * num3) / 2f)), (double)((float)y + Math.Max(0f, (height - num2 * num3) / 2f)));
		matrix.Scale((double)num3, (double)num3);
		cr.Matrix = matrix;
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(4.25, 4.0);
		cr.LineTo(29.25, 4.0);
		cr.LineTo(29.25, 29.0);
		cr.LineTo(4.25, 29.0);
		cr.ClosePath();
		cr.MoveTo(4.25, 4.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(4.25, 4.0);
		cr.LineTo(29.25, 4.0);
		cr.LineTo(29.25, 29.0);
		cr.LineTo(4.25, 29.0);
		cr.ClosePath();
		cr.MoveTo(4.25, 4.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(41.75, 4.0);
		cr.LineTo(66.75, 4.0);
		cr.LineTo(66.75, 29.0);
		cr.LineTo(41.75, 29.0);
		cr.ClosePath();
		cr.MoveTo(41.75, 4.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(41.75, 4.0);
		cr.LineTo(66.75, 4.0);
		cr.LineTo(66.75, 29.0);
		cr.LineTo(41.75, 29.0);
		cr.ClosePath();
		cr.MoveTo(41.75, 4.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(4.25, 42.0);
		cr.LineTo(29.25, 42.0);
		cr.LineTo(29.25, 67.0);
		cr.LineTo(4.25, 67.0);
		cr.ClosePath();
		cr.MoveTo(4.25, 42.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(4.25, 42.0);
		cr.LineTo(29.25, 42.0);
		cr.LineTo(29.25, 67.0);
		cr.LineTo(4.25, 67.0);
		cr.ClosePath();
		cr.MoveTo(4.25, 42.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(41.75, 42.0);
		cr.LineTo(66.75, 42.0);
		cr.LineTo(66.75, 67.0);
		cr.LineTo(41.75, 67.0);
		cr.ClosePath();
		cr.MoveTo(41.75, 42.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(41.75, 42.0);
		cr.LineTo(66.75, 42.0);
		cr.LineTo(66.75, 67.0);
		cr.LineTo(41.75, 67.0);
		cr.ClosePath();
		cr.MoveTo(41.75, 42.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(79.351563, 4.0);
		cr.LineTo(104.351563, 4.0);
		cr.LineTo(104.351563, 29.0);
		cr.LineTo(79.351563, 29.0);
		cr.ClosePath();
		cr.MoveTo(79.351563, 4.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(79.351563, 4.0);
		cr.LineTo(104.351563, 4.0);
		cr.LineTo(104.351563, 29.0);
		cr.LineTo(79.351563, 29.0);
		cr.ClosePath();
		cr.MoveTo(79.351563, 4.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(116.851563, 4.0);
		cr.LineTo(141.851563, 4.0);
		cr.LineTo(141.851563, 29.0);
		cr.LineTo(116.851563, 29.0);
		cr.ClosePath();
		cr.MoveTo(116.851563, 4.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(116.851563, 4.0);
		cr.LineTo(141.851563, 4.0);
		cr.LineTo(141.851563, 29.0);
		cr.LineTo(116.851563, 29.0);
		cr.ClosePath();
		cr.MoveTo(116.851563, 4.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(79.351563, 42.0);
		cr.LineTo(104.351563, 42.0);
		cr.LineTo(104.351563, 67.0);
		cr.LineTo(79.351563, 67.0);
		cr.ClosePath();
		cr.MoveTo(79.351563, 42.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(79.351563, 42.0);
		cr.LineTo(104.351563, 42.0);
		cr.LineTo(104.351563, 67.0);
		cr.LineTo(79.351563, 67.0);
		cr.ClosePath();
		cr.MoveTo(79.351563, 42.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(116.851563, 42.0);
		cr.LineTo(141.851563, 42.0);
		cr.LineTo(141.851563, 67.0);
		cr.LineTo(116.851563, 67.0);
		cr.ClosePath();
		cr.MoveTo(116.851563, 42.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(116.851563, 42.0);
		cr.LineTo(141.851563, 42.0);
		cr.LineTo(141.851563, 67.0);
		cr.LineTo(116.851563, 67.0);
		cr.ClosePath();
		cr.MoveTo(116.851563, 42.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(4.148438, 79.0);
		cr.LineTo(29.148438, 79.0);
		cr.LineTo(29.148438, 104.0);
		cr.LineTo(4.148438, 104.0);
		cr.ClosePath();
		cr.MoveTo(4.148438, 79.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(4.148438, 79.0);
		cr.LineTo(29.148438, 79.0);
		cr.LineTo(29.148438, 104.0);
		cr.LineTo(4.148438, 104.0);
		cr.ClosePath();
		cr.MoveTo(4.148438, 79.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(41.648438, 79.0);
		cr.LineTo(66.648438, 79.0);
		cr.LineTo(66.648438, 104.0);
		cr.LineTo(41.648438, 104.0);
		cr.ClosePath();
		cr.MoveTo(41.648438, 79.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(41.648438, 79.0);
		cr.LineTo(66.648438, 79.0);
		cr.LineTo(66.648438, 104.0);
		cr.LineTo(41.648438, 104.0);
		cr.ClosePath();
		cr.MoveTo(41.648438, 79.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(4.148438, 117.0);
		cr.LineTo(29.148438, 117.0);
		cr.LineTo(29.148438, 142.0);
		cr.LineTo(4.148438, 142.0);
		cr.ClosePath();
		cr.MoveTo(4.148438, 117.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(4.148438, 117.0);
		cr.LineTo(29.148438, 117.0);
		cr.LineTo(29.148438, 142.0);
		cr.LineTo(4.148438, 142.0);
		cr.ClosePath();
		cr.MoveTo(4.148438, 117.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(41.648438, 117.0);
		cr.LineTo(66.648438, 117.0);
		cr.LineTo(66.648438, 142.0);
		cr.LineTo(41.648438, 142.0);
		cr.ClosePath();
		cr.MoveTo(41.648438, 117.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(41.648438, 117.0);
		cr.LineTo(66.648438, 117.0);
		cr.LineTo(66.648438, 142.0);
		cr.LineTo(41.648438, 142.0);
		cr.ClosePath();
		cr.MoveTo(41.648438, 117.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(79.25, 79.0);
		cr.LineTo(104.25, 79.0);
		cr.LineTo(104.25, 104.0);
		cr.LineTo(79.25, 104.0);
		cr.ClosePath();
		cr.MoveTo(79.25, 79.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(79.25, 79.0);
		cr.LineTo(104.25, 79.0);
		cr.LineTo(104.25, 104.0);
		cr.LineTo(79.25, 104.0);
		cr.ClosePath();
		cr.MoveTo(79.25, 79.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(116.75, 79.0);
		cr.LineTo(141.75, 79.0);
		cr.LineTo(141.75, 104.0);
		cr.LineTo(116.75, 104.0);
		cr.ClosePath();
		cr.MoveTo(116.75, 79.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(116.75, 79.0);
		cr.LineTo(141.75, 79.0);
		cr.LineTo(141.75, 104.0);
		cr.LineTo(116.75, 104.0);
		cr.ClosePath();
		cr.MoveTo(116.75, 79.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(79.25, 117.0);
		cr.LineTo(104.25, 117.0);
		cr.LineTo(104.25, 142.0);
		cr.LineTo(79.25, 142.0);
		cr.ClosePath();
		cr.MoveTo(79.25, 117.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(79.25, 117.0);
		cr.LineTo(104.25, 117.0);
		cr.LineTo(104.25, 142.0);
		cr.LineTo(79.25, 142.0);
		cr.ClosePath();
		cr.MoveTo(79.25, 117.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
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
		cr.MoveTo(116.75, 117.0);
		cr.LineTo(141.75, 117.0);
		cr.LineTo(141.75, 142.0);
		cr.LineTo(116.75, 142.0);
		cr.ClosePath();
		cr.MoveTo(116.75, 117.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		cr.FillRule = (FillRule)0;
		cr.FillPreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 8.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(116.75, 117.0);
		cr.LineTo(141.75, 117.0);
		cr.LineTo(141.75, 142.0);
		cr.LineTo(116.75, 142.0);
		cr.ClosePath();
		cr.MoveTo(116.75, 117.0);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.0, 0.0, 0.0, 1.0, 240.15, -333.7);
		val.Matrix = matrix;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Restore();
	}
}
