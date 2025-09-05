using System;
using Cairo;
using Vintagestory.API.Client;

namespace VSSurvivalMod.Systems.ChiselModes;

public class EightByChiselModeData : ChiselMode
{
	public override int ChiselSize => 8;

	public override DrawSkillIconDelegate DrawAction(ICoreClientAPI capi)
	{
		return Drawcreate64_svg;
	}

	public void Drawcreate64_svg(Context cr, int x, int y, float width, float height, double[] rgba)
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
		//IL_2190: Unknown result type (might be due to invalid IL or missing references)
		//IL_2196: Expected O, but got Unknown
		//IL_2290: Unknown result type (might be due to invalid IL or missing references)
		//IL_2296: Expected O, but got Unknown
		//IL_236d: Unknown result type (might be due to invalid IL or missing references)
		//IL_2373: Expected O, but got Unknown
		//IL_23a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_23a6: Expected O, but got Unknown
		//IL_24a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_24a6: Expected O, but got Unknown
		//IL_257d: Unknown result type (might be due to invalid IL or missing references)
		//IL_2583: Expected O, but got Unknown
		//IL_25b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_25b6: Expected O, but got Unknown
		//IL_26b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_26b6: Expected O, but got Unknown
		//IL_278d: Unknown result type (might be due to invalid IL or missing references)
		//IL_2793: Expected O, but got Unknown
		//IL_27c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_27c6: Expected O, but got Unknown
		//IL_28c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_28c6: Expected O, but got Unknown
		//IL_299d: Unknown result type (might be due to invalid IL or missing references)
		//IL_29a3: Expected O, but got Unknown
		//IL_29d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_29d6: Expected O, but got Unknown
		//IL_2ad0: Unknown result type (might be due to invalid IL or missing references)
		//IL_2ad6: Expected O, but got Unknown
		//IL_2bad: Unknown result type (might be due to invalid IL or missing references)
		//IL_2bb3: Expected O, but got Unknown
		//IL_2be0: Unknown result type (might be due to invalid IL or missing references)
		//IL_2be6: Expected O, but got Unknown
		//IL_2ce0: Unknown result type (might be due to invalid IL or missing references)
		//IL_2ce6: Expected O, but got Unknown
		//IL_2dbd: Unknown result type (might be due to invalid IL or missing references)
		//IL_2dc3: Expected O, but got Unknown
		//IL_2df0: Unknown result type (might be due to invalid IL or missing references)
		//IL_2df6: Expected O, but got Unknown
		//IL_2ef0: Unknown result type (might be due to invalid IL or missing references)
		//IL_2ef6: Expected O, but got Unknown
		//IL_2fcd: Unknown result type (might be due to invalid IL or missing references)
		//IL_2fd3: Expected O, but got Unknown
		//IL_3000: Unknown result type (might be due to invalid IL or missing references)
		//IL_3006: Expected O, but got Unknown
		//IL_3100: Unknown result type (might be due to invalid IL or missing references)
		//IL_3106: Expected O, but got Unknown
		//IL_31dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_31e3: Expected O, but got Unknown
		//IL_3210: Unknown result type (might be due to invalid IL or missing references)
		//IL_3216: Expected O, but got Unknown
		//IL_3310: Unknown result type (might be due to invalid IL or missing references)
		//IL_3316: Expected O, but got Unknown
		//IL_33ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_33f3: Expected O, but got Unknown
		//IL_3420: Unknown result type (might be due to invalid IL or missing references)
		//IL_3426: Expected O, but got Unknown
		//IL_3520: Unknown result type (might be due to invalid IL or missing references)
		//IL_3526: Expected O, but got Unknown
		//IL_35fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_3603: Expected O, but got Unknown
		//IL_3630: Unknown result type (might be due to invalid IL or missing references)
		//IL_3636: Expected O, but got Unknown
		//IL_3730: Unknown result type (might be due to invalid IL or missing references)
		//IL_3736: Expected O, but got Unknown
		//IL_380d: Unknown result type (might be due to invalid IL or missing references)
		//IL_3813: Expected O, but got Unknown
		//IL_3840: Unknown result type (might be due to invalid IL or missing references)
		//IL_3846: Expected O, but got Unknown
		//IL_3940: Unknown result type (might be due to invalid IL or missing references)
		//IL_3946: Expected O, but got Unknown
		//IL_3a1d: Unknown result type (might be due to invalid IL or missing references)
		//IL_3a23: Expected O, but got Unknown
		//IL_3a50: Unknown result type (might be due to invalid IL or missing references)
		//IL_3a56: Expected O, but got Unknown
		//IL_3b50: Unknown result type (might be due to invalid IL or missing references)
		//IL_3b56: Expected O, but got Unknown
		//IL_3c2d: Unknown result type (might be due to invalid IL or missing references)
		//IL_3c33: Expected O, but got Unknown
		//IL_3c60: Unknown result type (might be due to invalid IL or missing references)
		//IL_3c66: Expected O, but got Unknown
		//IL_3d60: Unknown result type (might be due to invalid IL or missing references)
		//IL_3d66: Expected O, but got Unknown
		//IL_3e3d: Unknown result type (might be due to invalid IL or missing references)
		//IL_3e43: Expected O, but got Unknown
		//IL_3e70: Unknown result type (might be due to invalid IL or missing references)
		//IL_3e76: Expected O, but got Unknown
		//IL_3f70: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f76: Expected O, but got Unknown
		//IL_404d: Unknown result type (might be due to invalid IL or missing references)
		//IL_4053: Expected O, but got Unknown
		//IL_4080: Unknown result type (might be due to invalid IL or missing references)
		//IL_4086: Expected O, but got Unknown
		//IL_4180: Unknown result type (might be due to invalid IL or missing references)
		//IL_4186: Expected O, but got Unknown
		//IL_425d: Unknown result type (might be due to invalid IL or missing references)
		//IL_4263: Expected O, but got Unknown
		//IL_4290: Unknown result type (might be due to invalid IL or missing references)
		//IL_4296: Expected O, but got Unknown
		//IL_4390: Unknown result type (might be due to invalid IL or missing references)
		//IL_4396: Expected O, but got Unknown
		//IL_446d: Unknown result type (might be due to invalid IL or missing references)
		//IL_4473: Expected O, but got Unknown
		//IL_44a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_44a6: Expected O, but got Unknown
		//IL_45a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_45a6: Expected O, but got Unknown
		//IL_467d: Unknown result type (might be due to invalid IL or missing references)
		//IL_4683: Expected O, but got Unknown
		//IL_46b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_46b6: Expected O, but got Unknown
		//IL_47b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_47b6: Expected O, but got Unknown
		//IL_488d: Unknown result type (might be due to invalid IL or missing references)
		//IL_4893: Expected O, but got Unknown
		//IL_48c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_48c6: Expected O, but got Unknown
		//IL_49c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_49c6: Expected O, but got Unknown
		//IL_4a9d: Unknown result type (might be due to invalid IL or missing references)
		//IL_4aa3: Expected O, but got Unknown
		//IL_4ad0: Unknown result type (might be due to invalid IL or missing references)
		//IL_4ad6: Expected O, but got Unknown
		//IL_4bd0: Unknown result type (might be due to invalid IL or missing references)
		//IL_4bd6: Expected O, but got Unknown
		//IL_4cad: Unknown result type (might be due to invalid IL or missing references)
		//IL_4cb3: Expected O, but got Unknown
		//IL_4ce0: Unknown result type (might be due to invalid IL or missing references)
		//IL_4ce6: Expected O, but got Unknown
		//IL_4de0: Unknown result type (might be due to invalid IL or missing references)
		//IL_4de6: Expected O, but got Unknown
		//IL_4ebd: Unknown result type (might be due to invalid IL or missing references)
		//IL_4ec3: Expected O, but got Unknown
		//IL_4ef0: Unknown result type (might be due to invalid IL or missing references)
		//IL_4ef6: Expected O, but got Unknown
		//IL_4ff0: Unknown result type (might be due to invalid IL or missing references)
		//IL_4ff6: Expected O, but got Unknown
		//IL_50cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_50d3: Expected O, but got Unknown
		//IL_5100: Unknown result type (might be due to invalid IL or missing references)
		//IL_5106: Expected O, but got Unknown
		//IL_5200: Unknown result type (might be due to invalid IL or missing references)
		//IL_5206: Expected O, but got Unknown
		//IL_52dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_52e3: Expected O, but got Unknown
		//IL_5310: Unknown result type (might be due to invalid IL or missing references)
		//IL_5316: Expected O, but got Unknown
		//IL_5410: Unknown result type (might be due to invalid IL or missing references)
		//IL_5416: Expected O, but got Unknown
		//IL_54ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_54f3: Expected O, but got Unknown
		//IL_5520: Unknown result type (might be due to invalid IL or missing references)
		//IL_5526: Expected O, but got Unknown
		//IL_5620: Unknown result type (might be due to invalid IL or missing references)
		//IL_5626: Expected O, but got Unknown
		//IL_56fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_5703: Expected O, but got Unknown
		//IL_5730: Unknown result type (might be due to invalid IL or missing references)
		//IL_5736: Expected O, but got Unknown
		//IL_5830: Unknown result type (might be due to invalid IL or missing references)
		//IL_5836: Expected O, but got Unknown
		//IL_590d: Unknown result type (might be due to invalid IL or missing references)
		//IL_5913: Expected O, but got Unknown
		//IL_5940: Unknown result type (might be due to invalid IL or missing references)
		//IL_5946: Expected O, but got Unknown
		//IL_5a40: Unknown result type (might be due to invalid IL or missing references)
		//IL_5a46: Expected O, but got Unknown
		//IL_5b1d: Unknown result type (might be due to invalid IL or missing references)
		//IL_5b23: Expected O, but got Unknown
		//IL_5b50: Unknown result type (might be due to invalid IL or missing references)
		//IL_5b56: Expected O, but got Unknown
		//IL_5c50: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c56: Expected O, but got Unknown
		//IL_5d2d: Unknown result type (might be due to invalid IL or missing references)
		//IL_5d33: Expected O, but got Unknown
		//IL_5d60: Unknown result type (might be due to invalid IL or missing references)
		//IL_5d66: Expected O, but got Unknown
		//IL_5e60: Unknown result type (might be due to invalid IL or missing references)
		//IL_5e66: Expected O, but got Unknown
		//IL_5f3d: Unknown result type (might be due to invalid IL or missing references)
		//IL_5f43: Expected O, but got Unknown
		//IL_5f70: Unknown result type (might be due to invalid IL or missing references)
		//IL_5f76: Expected O, but got Unknown
		//IL_6070: Unknown result type (might be due to invalid IL or missing references)
		//IL_6076: Expected O, but got Unknown
		//IL_614d: Unknown result type (might be due to invalid IL or missing references)
		//IL_6153: Expected O, but got Unknown
		//IL_6180: Unknown result type (might be due to invalid IL or missing references)
		//IL_6186: Expected O, but got Unknown
		//IL_6280: Unknown result type (might be due to invalid IL or missing references)
		//IL_6286: Expected O, but got Unknown
		//IL_635d: Unknown result type (might be due to invalid IL or missing references)
		//IL_6363: Expected O, but got Unknown
		//IL_6390: Unknown result type (might be due to invalid IL or missing references)
		//IL_6396: Expected O, but got Unknown
		//IL_6490: Unknown result type (might be due to invalid IL or missing references)
		//IL_6496: Expected O, but got Unknown
		//IL_656d: Unknown result type (might be due to invalid IL or missing references)
		//IL_6573: Expected O, but got Unknown
		//IL_65a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_65a6: Expected O, but got Unknown
		//IL_66a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_66a6: Expected O, but got Unknown
		//IL_677d: Unknown result type (might be due to invalid IL or missing references)
		//IL_6783: Expected O, but got Unknown
		//IL_67b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_67b6: Expected O, but got Unknown
		//IL_68b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_68b6: Expected O, but got Unknown
		//IL_698d: Unknown result type (might be due to invalid IL or missing references)
		//IL_6993: Expected O, but got Unknown
		//IL_69c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_69c6: Expected O, but got Unknown
		//IL_6ac0: Unknown result type (might be due to invalid IL or missing references)
		//IL_6ac6: Expected O, but got Unknown
		//IL_6b9d: Unknown result type (might be due to invalid IL or missing references)
		//IL_6ba3: Expected O, but got Unknown
		//IL_6bd0: Unknown result type (might be due to invalid IL or missing references)
		//IL_6bd6: Expected O, but got Unknown
		//IL_6cd0: Unknown result type (might be due to invalid IL or missing references)
		//IL_6cd6: Expected O, but got Unknown
		//IL_6dad: Unknown result type (might be due to invalid IL or missing references)
		//IL_6db3: Expected O, but got Unknown
		//IL_6de0: Unknown result type (might be due to invalid IL or missing references)
		//IL_6de6: Expected O, but got Unknown
		//IL_6ee0: Unknown result type (might be due to invalid IL or missing references)
		//IL_6ee6: Expected O, but got Unknown
		//IL_6fbd: Unknown result type (might be due to invalid IL or missing references)
		//IL_6fc3: Expected O, but got Unknown
		//IL_6ff0: Unknown result type (might be due to invalid IL or missing references)
		//IL_6ff6: Expected O, but got Unknown
		//IL_70f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_70f6: Expected O, but got Unknown
		//IL_71cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_71d3: Expected O, but got Unknown
		//IL_7200: Unknown result type (might be due to invalid IL or missing references)
		//IL_7206: Expected O, but got Unknown
		//IL_7300: Unknown result type (might be due to invalid IL or missing references)
		//IL_7306: Expected O, but got Unknown
		//IL_73dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_73e3: Expected O, but got Unknown
		//IL_7410: Unknown result type (might be due to invalid IL or missing references)
		//IL_7416: Expected O, but got Unknown
		//IL_7510: Unknown result type (might be due to invalid IL or missing references)
		//IL_7516: Expected O, but got Unknown
		//IL_75ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_75f3: Expected O, but got Unknown
		//IL_7620: Unknown result type (might be due to invalid IL or missing references)
		//IL_7626: Expected O, but got Unknown
		//IL_7720: Unknown result type (might be due to invalid IL or missing references)
		//IL_7726: Expected O, but got Unknown
		//IL_77fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_7803: Expected O, but got Unknown
		//IL_7830: Unknown result type (might be due to invalid IL or missing references)
		//IL_7836: Expected O, but got Unknown
		//IL_7930: Unknown result type (might be due to invalid IL or missing references)
		//IL_7936: Expected O, but got Unknown
		//IL_7a0d: Unknown result type (might be due to invalid IL or missing references)
		//IL_7a13: Expected O, but got Unknown
		//IL_7a40: Unknown result type (might be due to invalid IL or missing references)
		//IL_7a46: Expected O, but got Unknown
		//IL_7b40: Unknown result type (might be due to invalid IL or missing references)
		//IL_7b46: Expected O, but got Unknown
		//IL_7c1d: Unknown result type (might be due to invalid IL or missing references)
		//IL_7c23: Expected O, but got Unknown
		//IL_7c50: Unknown result type (might be due to invalid IL or missing references)
		//IL_7c56: Expected O, but got Unknown
		//IL_7d50: Unknown result type (might be due to invalid IL or missing references)
		//IL_7d56: Expected O, but got Unknown
		//IL_7e2d: Unknown result type (might be due to invalid IL or missing references)
		//IL_7e33: Expected O, but got Unknown
		//IL_7e60: Unknown result type (might be due to invalid IL or missing references)
		//IL_7e66: Expected O, but got Unknown
		//IL_7f60: Unknown result type (might be due to invalid IL or missing references)
		//IL_7f66: Expected O, but got Unknown
		//IL_803d: Unknown result type (might be due to invalid IL or missing references)
		//IL_8043: Expected O, but got Unknown
		//IL_8070: Unknown result type (might be due to invalid IL or missing references)
		//IL_8076: Expected O, but got Unknown
		//IL_8170: Unknown result type (might be due to invalid IL or missing references)
		//IL_8176: Expected O, but got Unknown
		//IL_824d: Unknown result type (might be due to invalid IL or missing references)
		//IL_8253: Expected O, but got Unknown
		//IL_8280: Unknown result type (might be due to invalid IL or missing references)
		//IL_8286: Expected O, but got Unknown
		//IL_8380: Unknown result type (might be due to invalid IL or missing references)
		//IL_8386: Expected O, but got Unknown
		//IL_845d: Unknown result type (might be due to invalid IL or missing references)
		//IL_8463: Expected O, but got Unknown
		Pattern val = null;
		Matrix matrix = cr.Matrix;
		cr.Save();
		float num = 296f;
		float num2 = 296f;
		float num3 = Math.Min(width / num, height / num2);
		matrix.Translate((double)((float)x + Math.Max(0f, (width - num * num3) / 2f)), (double)((float)y + Math.Max(0f, (height - num2 * num3) / 2f)));
		matrix.Scale((double)num3, (double)num3);
		cr.Matrix = matrix;
		cr.Operator = (Operator)2;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(4.105469, 4.007813);
		cr.LineTo(29.148438, 4.007813);
		cr.LineTo(29.148438, 29.050781);
		cr.LineTo(4.105469, 29.050781);
		cr.ClosePath();
		cr.MoveTo(4.105469, 4.007813);
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
		cr.MoveTo(4.105469, 4.007813);
		cr.LineTo(29.148438, 4.007813);
		cr.LineTo(29.148438, 29.050781);
		cr.LineTo(4.105469, 29.050781);
		cr.ClosePath();
		cr.MoveTo(4.105469, 4.007813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(41.671875, 4.007813);
		cr.LineTo(66.710938, 4.007813);
		cr.LineTo(66.710938, 29.050781);
		cr.LineTo(41.671875, 29.050781);
		cr.ClosePath();
		cr.MoveTo(41.671875, 4.007813);
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
		cr.MoveTo(41.671875, 4.007813);
		cr.LineTo(66.710938, 4.007813);
		cr.LineTo(66.710938, 29.050781);
		cr.LineTo(41.671875, 29.050781);
		cr.ClosePath();
		cr.MoveTo(41.671875, 4.007813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(4.105469, 42.070313);
		cr.LineTo(29.148438, 42.070313);
		cr.LineTo(29.148438, 67.113281);
		cr.LineTo(4.105469, 67.113281);
		cr.ClosePath();
		cr.MoveTo(4.105469, 42.070313);
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
		cr.MoveTo(4.105469, 42.070313);
		cr.LineTo(29.148438, 42.070313);
		cr.LineTo(29.148438, 67.113281);
		cr.LineTo(4.105469, 67.113281);
		cr.ClosePath();
		cr.MoveTo(4.105469, 42.070313);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(41.671875, 42.070313);
		cr.LineTo(66.710938, 42.070313);
		cr.LineTo(66.710938, 67.113281);
		cr.LineTo(41.671875, 67.113281);
		cr.ClosePath();
		cr.MoveTo(41.671875, 42.070313);
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
		cr.MoveTo(41.671875, 42.070313);
		cr.LineTo(66.710938, 42.070313);
		cr.LineTo(66.710938, 67.113281);
		cr.LineTo(41.671875, 67.113281);
		cr.ClosePath();
		cr.MoveTo(41.671875, 42.070313);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(79.335938, 4.007813);
		cr.LineTo(104.375, 4.007813);
		cr.LineTo(104.375, 29.050781);
		cr.LineTo(79.335938, 29.050781);
		cr.ClosePath();
		cr.MoveTo(79.335938, 4.007813);
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
		cr.MoveTo(79.335938, 4.007813);
		cr.LineTo(104.375, 4.007813);
		cr.LineTo(104.375, 29.050781);
		cr.LineTo(79.335938, 29.050781);
		cr.ClosePath();
		cr.MoveTo(79.335938, 4.007813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(116.898438, 4.007813);
		cr.LineTo(141.941406, 4.007813);
		cr.LineTo(141.941406, 29.050781);
		cr.LineTo(116.898438, 29.050781);
		cr.ClosePath();
		cr.MoveTo(116.898438, 4.007813);
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
		cr.MoveTo(116.898438, 4.007813);
		cr.LineTo(141.941406, 4.007813);
		cr.LineTo(141.941406, 29.050781);
		cr.LineTo(116.898438, 29.050781);
		cr.ClosePath();
		cr.MoveTo(116.898438, 4.007813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(79.335938, 42.070313);
		cr.LineTo(104.375, 42.070313);
		cr.LineTo(104.375, 67.113281);
		cr.LineTo(79.335938, 67.113281);
		cr.ClosePath();
		cr.MoveTo(79.335938, 42.070313);
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
		cr.MoveTo(79.335938, 42.070313);
		cr.LineTo(104.375, 42.070313);
		cr.LineTo(104.375, 67.113281);
		cr.LineTo(79.335938, 67.113281);
		cr.ClosePath();
		cr.MoveTo(79.335938, 42.070313);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(116.898438, 42.070313);
		cr.LineTo(141.941406, 42.070313);
		cr.LineTo(141.941406, 67.113281);
		cr.LineTo(116.898438, 67.113281);
		cr.ClosePath();
		cr.MoveTo(116.898438, 42.070313);
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
		cr.MoveTo(116.898438, 42.070313);
		cr.LineTo(141.941406, 42.070313);
		cr.LineTo(141.941406, 67.113281);
		cr.LineTo(116.898438, 67.113281);
		cr.ClosePath();
		cr.MoveTo(116.898438, 42.070313);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(4.007813, 79.132813);
		cr.LineTo(29.050781, 79.132813);
		cr.LineTo(29.050781, 104.175781);
		cr.LineTo(4.007813, 104.175781);
		cr.ClosePath();
		cr.MoveTo(4.007813, 79.132813);
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
		cr.MoveTo(4.007813, 79.132813);
		cr.LineTo(29.050781, 79.132813);
		cr.LineTo(29.050781, 104.175781);
		cr.LineTo(4.007813, 104.175781);
		cr.ClosePath();
		cr.MoveTo(4.007813, 79.132813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(41.570313, 79.132813);
		cr.LineTo(66.613281, 79.132813);
		cr.LineTo(66.613281, 104.175781);
		cr.LineTo(41.570313, 104.175781);
		cr.ClosePath();
		cr.MoveTo(41.570313, 79.132813);
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
		cr.MoveTo(41.570313, 79.132813);
		cr.LineTo(66.613281, 79.132813);
		cr.LineTo(66.613281, 104.175781);
		cr.LineTo(41.570313, 104.175781);
		cr.ClosePath();
		cr.MoveTo(41.570313, 79.132813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(4.007813, 117.199219);
		cr.LineTo(29.050781, 117.199219);
		cr.LineTo(29.050781, 142.242188);
		cr.LineTo(4.007813, 142.242188);
		cr.ClosePath();
		cr.MoveTo(4.007813, 117.199219);
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
		cr.MoveTo(4.007813, 117.199219);
		cr.LineTo(29.050781, 117.199219);
		cr.LineTo(29.050781, 142.242188);
		cr.LineTo(4.007813, 142.242188);
		cr.ClosePath();
		cr.MoveTo(4.007813, 117.199219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(41.570313, 117.199219);
		cr.LineTo(66.613281, 117.199219);
		cr.LineTo(66.613281, 142.242188);
		cr.LineTo(41.570313, 142.242188);
		cr.ClosePath();
		cr.MoveTo(41.570313, 117.199219);
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
		cr.MoveTo(41.570313, 117.199219);
		cr.LineTo(66.613281, 117.199219);
		cr.LineTo(66.613281, 142.242188);
		cr.LineTo(41.570313, 142.242188);
		cr.ClosePath();
		cr.MoveTo(41.570313, 117.199219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(79.234375, 79.132813);
		cr.LineTo(104.277344, 79.132813);
		cr.LineTo(104.277344, 104.175781);
		cr.LineTo(79.234375, 104.175781);
		cr.ClosePath();
		cr.MoveTo(79.234375, 79.132813);
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
		cr.MoveTo(79.234375, 79.132813);
		cr.LineTo(104.277344, 79.132813);
		cr.LineTo(104.277344, 104.175781);
		cr.LineTo(79.234375, 104.175781);
		cr.ClosePath();
		cr.MoveTo(79.234375, 79.132813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(116.796875, 79.132813);
		cr.LineTo(141.839844, 79.132813);
		cr.LineTo(141.839844, 104.175781);
		cr.LineTo(116.796875, 104.175781);
		cr.ClosePath();
		cr.MoveTo(116.796875, 79.132813);
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
		cr.MoveTo(116.796875, 79.132813);
		cr.LineTo(141.839844, 79.132813);
		cr.LineTo(141.839844, 104.175781);
		cr.LineTo(116.796875, 104.175781);
		cr.ClosePath();
		cr.MoveTo(116.796875, 79.132813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(79.234375, 117.199219);
		cr.LineTo(104.277344, 117.199219);
		cr.LineTo(104.277344, 142.242188);
		cr.LineTo(79.234375, 142.242188);
		cr.ClosePath();
		cr.MoveTo(79.234375, 117.199219);
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
		cr.MoveTo(79.234375, 117.199219);
		cr.LineTo(104.277344, 117.199219);
		cr.LineTo(104.277344, 142.242188);
		cr.LineTo(79.234375, 142.242188);
		cr.ClosePath();
		cr.MoveTo(79.234375, 117.199219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(116.796875, 117.199219);
		cr.LineTo(141.839844, 117.199219);
		cr.LineTo(141.839844, 142.242188);
		cr.LineTo(116.796875, 142.242188);
		cr.ClosePath();
		cr.MoveTo(116.796875, 117.199219);
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
		cr.MoveTo(116.796875, 117.199219);
		cr.LineTo(141.839844, 117.199219);
		cr.LineTo(141.839844, 142.242188);
		cr.LineTo(116.796875, 142.242188);
		cr.ClosePath();
		cr.MoveTo(116.796875, 117.199219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(154.160156, 4.007813);
		cr.LineTo(179.203125, 4.007813);
		cr.LineTo(179.203125, 29.050781);
		cr.LineTo(154.160156, 29.050781);
		cr.ClosePath();
		cr.MoveTo(154.160156, 4.007813);
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
		cr.MoveTo(154.160156, 4.007813);
		cr.LineTo(179.203125, 4.007813);
		cr.LineTo(179.203125, 29.050781);
		cr.LineTo(154.160156, 29.050781);
		cr.ClosePath();
		cr.MoveTo(154.160156, 4.007813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(191.722656, 4.007813);
		cr.LineTo(216.765625, 4.007813);
		cr.LineTo(216.765625, 29.050781);
		cr.LineTo(191.722656, 29.050781);
		cr.ClosePath();
		cr.MoveTo(191.722656, 4.007813);
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
		cr.MoveTo(191.722656, 4.007813);
		cr.LineTo(216.765625, 4.007813);
		cr.LineTo(216.765625, 29.050781);
		cr.LineTo(191.722656, 29.050781);
		cr.ClosePath();
		cr.MoveTo(191.722656, 4.007813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(154.160156, 42.070313);
		cr.LineTo(179.203125, 42.070313);
		cr.LineTo(179.203125, 67.113281);
		cr.LineTo(154.160156, 67.113281);
		cr.ClosePath();
		cr.MoveTo(154.160156, 42.070313);
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
		cr.MoveTo(154.160156, 42.070313);
		cr.LineTo(179.203125, 42.070313);
		cr.LineTo(179.203125, 67.113281);
		cr.LineTo(154.160156, 67.113281);
		cr.ClosePath();
		cr.MoveTo(154.160156, 42.070313);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(191.722656, 42.070313);
		cr.LineTo(216.765625, 42.070313);
		cr.LineTo(216.765625, 67.113281);
		cr.LineTo(191.722656, 67.113281);
		cr.ClosePath();
		cr.MoveTo(191.722656, 42.070313);
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
		cr.MoveTo(191.722656, 42.070313);
		cr.LineTo(216.765625, 42.070313);
		cr.LineTo(216.765625, 67.113281);
		cr.LineTo(191.722656, 67.113281);
		cr.ClosePath();
		cr.MoveTo(191.722656, 42.070313);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(229.386719, 4.007813);
		cr.LineTo(254.429688, 4.007813);
		cr.LineTo(254.429688, 29.050781);
		cr.LineTo(229.386719, 29.050781);
		cr.ClosePath();
		cr.MoveTo(229.386719, 4.007813);
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
		cr.MoveTo(229.386719, 4.007813);
		cr.LineTo(254.429688, 4.007813);
		cr.LineTo(254.429688, 29.050781);
		cr.LineTo(229.386719, 29.050781);
		cr.ClosePath();
		cr.MoveTo(229.386719, 4.007813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(266.949219, 4.007813);
		cr.LineTo(291.992188, 4.007813);
		cr.LineTo(291.992188, 29.050781);
		cr.LineTo(266.949219, 29.050781);
		cr.ClosePath();
		cr.MoveTo(266.949219, 4.007813);
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
		cr.MoveTo(266.949219, 4.007813);
		cr.LineTo(291.992188, 4.007813);
		cr.LineTo(291.992188, 29.050781);
		cr.LineTo(266.949219, 29.050781);
		cr.ClosePath();
		cr.MoveTo(266.949219, 4.007813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(229.386719, 42.070313);
		cr.LineTo(254.429688, 42.070313);
		cr.LineTo(254.429688, 67.113281);
		cr.LineTo(229.386719, 67.113281);
		cr.ClosePath();
		cr.MoveTo(229.386719, 42.070313);
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
		cr.MoveTo(229.386719, 42.070313);
		cr.LineTo(254.429688, 42.070313);
		cr.LineTo(254.429688, 67.113281);
		cr.LineTo(229.386719, 67.113281);
		cr.ClosePath();
		cr.MoveTo(229.386719, 42.070313);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(266.949219, 42.070313);
		cr.LineTo(291.992188, 42.070313);
		cr.LineTo(291.992188, 67.113281);
		cr.LineTo(266.949219, 67.113281);
		cr.ClosePath();
		cr.MoveTo(266.949219, 42.070313);
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
		cr.MoveTo(266.949219, 42.070313);
		cr.LineTo(291.992188, 42.070313);
		cr.LineTo(291.992188, 67.113281);
		cr.LineTo(266.949219, 67.113281);
		cr.ClosePath();
		cr.MoveTo(266.949219, 42.070313);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(154.058594, 79.132813);
		cr.LineTo(179.101563, 79.132813);
		cr.LineTo(179.101563, 104.175781);
		cr.LineTo(154.058594, 104.175781);
		cr.ClosePath();
		cr.MoveTo(154.058594, 79.132813);
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
		cr.MoveTo(154.058594, 79.132813);
		cr.LineTo(179.101563, 79.132813);
		cr.LineTo(179.101563, 104.175781);
		cr.LineTo(154.058594, 104.175781);
		cr.ClosePath();
		cr.MoveTo(154.058594, 79.132813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(191.625, 79.132813);
		cr.LineTo(216.664063, 79.132813);
		cr.LineTo(216.664063, 104.175781);
		cr.LineTo(191.625, 104.175781);
		cr.ClosePath();
		cr.MoveTo(191.625, 79.132813);
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
		cr.MoveTo(191.625, 79.132813);
		cr.LineTo(216.664063, 79.132813);
		cr.LineTo(216.664063, 104.175781);
		cr.LineTo(191.625, 104.175781);
		cr.ClosePath();
		cr.MoveTo(191.625, 79.132813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(154.058594, 117.199219);
		cr.LineTo(179.101563, 117.199219);
		cr.LineTo(179.101563, 142.242188);
		cr.LineTo(154.058594, 142.242188);
		cr.ClosePath();
		cr.MoveTo(154.058594, 117.199219);
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
		cr.MoveTo(154.058594, 117.199219);
		cr.LineTo(179.101563, 117.199219);
		cr.LineTo(179.101563, 142.242188);
		cr.LineTo(154.058594, 142.242188);
		cr.ClosePath();
		cr.MoveTo(154.058594, 117.199219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(191.625, 117.199219);
		cr.LineTo(216.664063, 117.199219);
		cr.LineTo(216.664063, 142.242188);
		cr.LineTo(191.625, 142.242188);
		cr.ClosePath();
		cr.MoveTo(191.625, 117.199219);
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
		cr.MoveTo(191.625, 117.199219);
		cr.LineTo(216.664063, 117.199219);
		cr.LineTo(216.664063, 142.242188);
		cr.LineTo(191.625, 142.242188);
		cr.ClosePath();
		cr.MoveTo(191.625, 117.199219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(229.289063, 79.132813);
		cr.LineTo(254.328125, 79.132813);
		cr.LineTo(254.328125, 104.175781);
		cr.LineTo(229.289063, 104.175781);
		cr.ClosePath();
		cr.MoveTo(229.289063, 79.132813);
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
		cr.MoveTo(229.289063, 79.132813);
		cr.LineTo(254.328125, 79.132813);
		cr.LineTo(254.328125, 104.175781);
		cr.LineTo(229.289063, 104.175781);
		cr.ClosePath();
		cr.MoveTo(229.289063, 79.132813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(266.851563, 79.132813);
		cr.LineTo(291.894531, 79.132813);
		cr.LineTo(291.894531, 104.175781);
		cr.LineTo(266.851563, 104.175781);
		cr.ClosePath();
		cr.MoveTo(266.851563, 79.132813);
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
		cr.MoveTo(266.851563, 79.132813);
		cr.LineTo(291.894531, 79.132813);
		cr.LineTo(291.894531, 104.175781);
		cr.LineTo(266.851563, 104.175781);
		cr.ClosePath();
		cr.MoveTo(266.851563, 79.132813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(229.289063, 117.199219);
		cr.LineTo(254.328125, 117.199219);
		cr.LineTo(254.328125, 142.242188);
		cr.LineTo(229.289063, 142.242188);
		cr.ClosePath();
		cr.MoveTo(229.289063, 117.199219);
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
		cr.MoveTo(229.289063, 117.199219);
		cr.LineTo(254.328125, 117.199219);
		cr.LineTo(254.328125, 142.242188);
		cr.LineTo(229.289063, 142.242188);
		cr.ClosePath();
		cr.MoveTo(229.289063, 117.199219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(266.851563, 117.199219);
		cr.LineTo(291.894531, 117.199219);
		cr.LineTo(291.894531, 142.242188);
		cr.LineTo(266.851563, 142.242188);
		cr.ClosePath();
		cr.MoveTo(266.851563, 117.199219);
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
		cr.MoveTo(266.851563, 117.199219);
		cr.LineTo(291.894531, 117.199219);
		cr.LineTo(291.894531, 142.242188);
		cr.LineTo(266.851563, 142.242188);
		cr.ClosePath();
		cr.MoveTo(266.851563, 117.199219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(4.105469, 153.757813);
		cr.LineTo(29.148438, 153.757813);
		cr.LineTo(29.148438, 178.800781);
		cr.LineTo(4.105469, 178.800781);
		cr.ClosePath();
		cr.MoveTo(4.105469, 153.757813);
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
		cr.MoveTo(4.105469, 153.757813);
		cr.LineTo(29.148438, 153.757813);
		cr.LineTo(29.148438, 178.800781);
		cr.LineTo(4.105469, 178.800781);
		cr.ClosePath();
		cr.MoveTo(4.105469, 153.757813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(41.671875, 153.757813);
		cr.LineTo(66.710938, 153.757813);
		cr.LineTo(66.710938, 178.800781);
		cr.LineTo(41.671875, 178.800781);
		cr.ClosePath();
		cr.MoveTo(41.671875, 153.757813);
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
		cr.MoveTo(41.671875, 153.757813);
		cr.LineTo(66.710938, 153.757813);
		cr.LineTo(66.710938, 178.800781);
		cr.LineTo(41.671875, 178.800781);
		cr.ClosePath();
		cr.MoveTo(41.671875, 153.757813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(4.105469, 191.824219);
		cr.LineTo(29.148438, 191.824219);
		cr.LineTo(29.148438, 216.867188);
		cr.LineTo(4.105469, 216.867188);
		cr.ClosePath();
		cr.MoveTo(4.105469, 191.824219);
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
		cr.MoveTo(4.105469, 191.824219);
		cr.LineTo(29.148438, 191.824219);
		cr.LineTo(29.148438, 216.867188);
		cr.LineTo(4.105469, 216.867188);
		cr.ClosePath();
		cr.MoveTo(4.105469, 191.824219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(41.671875, 191.824219);
		cr.LineTo(66.710938, 191.824219);
		cr.LineTo(66.710938, 216.867188);
		cr.LineTo(41.671875, 216.867188);
		cr.ClosePath();
		cr.MoveTo(41.671875, 191.824219);
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
		cr.MoveTo(41.671875, 191.824219);
		cr.LineTo(66.710938, 191.824219);
		cr.LineTo(66.710938, 216.867188);
		cr.LineTo(41.671875, 216.867188);
		cr.ClosePath();
		cr.MoveTo(41.671875, 191.824219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(79.335938, 153.757813);
		cr.LineTo(104.375, 153.757813);
		cr.LineTo(104.375, 178.800781);
		cr.LineTo(79.335938, 178.800781);
		cr.ClosePath();
		cr.MoveTo(79.335938, 153.757813);
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
		cr.MoveTo(79.335938, 153.757813);
		cr.LineTo(104.375, 153.757813);
		cr.LineTo(104.375, 178.800781);
		cr.LineTo(79.335938, 178.800781);
		cr.ClosePath();
		cr.MoveTo(79.335938, 153.757813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(116.898438, 153.757813);
		cr.LineTo(141.941406, 153.757813);
		cr.LineTo(141.941406, 178.800781);
		cr.LineTo(116.898438, 178.800781);
		cr.ClosePath();
		cr.MoveTo(116.898438, 153.757813);
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
		cr.MoveTo(116.898438, 153.757813);
		cr.LineTo(141.941406, 153.757813);
		cr.LineTo(141.941406, 178.800781);
		cr.LineTo(116.898438, 178.800781);
		cr.ClosePath();
		cr.MoveTo(116.898438, 153.757813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(79.335938, 191.824219);
		cr.LineTo(104.375, 191.824219);
		cr.LineTo(104.375, 216.867188);
		cr.LineTo(79.335938, 216.867188);
		cr.ClosePath();
		cr.MoveTo(79.335938, 191.824219);
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
		cr.MoveTo(79.335938, 191.824219);
		cr.LineTo(104.375, 191.824219);
		cr.LineTo(104.375, 216.867188);
		cr.LineTo(79.335938, 216.867188);
		cr.ClosePath();
		cr.MoveTo(79.335938, 191.824219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(116.898438, 191.824219);
		cr.LineTo(141.941406, 191.824219);
		cr.LineTo(141.941406, 216.867188);
		cr.LineTo(116.898438, 216.867188);
		cr.ClosePath();
		cr.MoveTo(116.898438, 191.824219);
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
		cr.MoveTo(116.898438, 191.824219);
		cr.LineTo(141.941406, 191.824219);
		cr.LineTo(141.941406, 216.867188);
		cr.LineTo(116.898438, 216.867188);
		cr.ClosePath();
		cr.MoveTo(116.898438, 191.824219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(4.007813, 228.886719);
		cr.LineTo(29.050781, 228.886719);
		cr.LineTo(29.050781, 253.929688);
		cr.LineTo(4.007813, 253.929688);
		cr.ClosePath();
		cr.MoveTo(4.007813, 228.886719);
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
		cr.MoveTo(4.007813, 228.886719);
		cr.LineTo(29.050781, 228.886719);
		cr.LineTo(29.050781, 253.929688);
		cr.LineTo(4.007813, 253.929688);
		cr.ClosePath();
		cr.MoveTo(4.007813, 228.886719);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(41.570313, 228.886719);
		cr.LineTo(66.613281, 228.886719);
		cr.LineTo(66.613281, 253.929688);
		cr.LineTo(41.570313, 253.929688);
		cr.ClosePath();
		cr.MoveTo(41.570313, 228.886719);
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
		cr.MoveTo(41.570313, 228.886719);
		cr.LineTo(66.613281, 228.886719);
		cr.LineTo(66.613281, 253.929688);
		cr.LineTo(41.570313, 253.929688);
		cr.ClosePath();
		cr.MoveTo(41.570313, 228.886719);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(4.007813, 266.949219);
		cr.LineTo(29.050781, 266.949219);
		cr.LineTo(29.050781, 291.992188);
		cr.LineTo(4.007813, 291.992188);
		cr.ClosePath();
		cr.MoveTo(4.007813, 266.949219);
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
		cr.MoveTo(4.007813, 266.949219);
		cr.LineTo(29.050781, 266.949219);
		cr.LineTo(29.050781, 291.992188);
		cr.LineTo(4.007813, 291.992188);
		cr.ClosePath();
		cr.MoveTo(4.007813, 266.949219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(41.570313, 266.949219);
		cr.LineTo(66.613281, 266.949219);
		cr.LineTo(66.613281, 291.992188);
		cr.LineTo(41.570313, 291.992188);
		cr.ClosePath();
		cr.MoveTo(41.570313, 266.949219);
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
		cr.MoveTo(41.570313, 266.949219);
		cr.LineTo(66.613281, 266.949219);
		cr.LineTo(66.613281, 291.992188);
		cr.LineTo(41.570313, 291.992188);
		cr.ClosePath();
		cr.MoveTo(41.570313, 266.949219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(79.234375, 228.886719);
		cr.LineTo(104.277344, 228.886719);
		cr.LineTo(104.277344, 253.929688);
		cr.LineTo(79.234375, 253.929688);
		cr.ClosePath();
		cr.MoveTo(79.234375, 228.886719);
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
		cr.MoveTo(79.234375, 228.886719);
		cr.LineTo(104.277344, 228.886719);
		cr.LineTo(104.277344, 253.929688);
		cr.LineTo(79.234375, 253.929688);
		cr.ClosePath();
		cr.MoveTo(79.234375, 228.886719);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(116.796875, 228.886719);
		cr.LineTo(141.839844, 228.886719);
		cr.LineTo(141.839844, 253.929688);
		cr.LineTo(116.796875, 253.929688);
		cr.ClosePath();
		cr.MoveTo(116.796875, 228.886719);
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
		cr.MoveTo(116.796875, 228.886719);
		cr.LineTo(141.839844, 228.886719);
		cr.LineTo(141.839844, 253.929688);
		cr.LineTo(116.796875, 253.929688);
		cr.ClosePath();
		cr.MoveTo(116.796875, 228.886719);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(79.234375, 266.949219);
		cr.LineTo(104.277344, 266.949219);
		cr.LineTo(104.277344, 291.992188);
		cr.LineTo(79.234375, 291.992188);
		cr.ClosePath();
		cr.MoveTo(79.234375, 266.949219);
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
		cr.MoveTo(79.234375, 266.949219);
		cr.LineTo(104.277344, 266.949219);
		cr.LineTo(104.277344, 291.992188);
		cr.LineTo(79.234375, 291.992188);
		cr.ClosePath();
		cr.MoveTo(79.234375, 266.949219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(116.796875, 266.949219);
		cr.LineTo(141.839844, 266.949219);
		cr.LineTo(141.839844, 291.992188);
		cr.LineTo(116.796875, 291.992188);
		cr.ClosePath();
		cr.MoveTo(116.796875, 266.949219);
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
		cr.MoveTo(116.796875, 266.949219);
		cr.LineTo(141.839844, 266.949219);
		cr.LineTo(141.839844, 291.992188);
		cr.LineTo(116.796875, 291.992188);
		cr.ClosePath();
		cr.MoveTo(116.796875, 266.949219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(154.058594, 153.757813);
		cr.LineTo(179.101563, 153.757813);
		cr.LineTo(179.101563, 178.800781);
		cr.LineTo(154.058594, 178.800781);
		cr.ClosePath();
		cr.MoveTo(154.058594, 153.757813);
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
		cr.MoveTo(154.058594, 153.757813);
		cr.LineTo(179.101563, 153.757813);
		cr.LineTo(179.101563, 178.800781);
		cr.LineTo(154.058594, 178.800781);
		cr.ClosePath();
		cr.MoveTo(154.058594, 153.757813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(191.625, 153.757813);
		cr.LineTo(216.664063, 153.757813);
		cr.LineTo(216.664063, 178.800781);
		cr.LineTo(191.625, 178.800781);
		cr.ClosePath();
		cr.MoveTo(191.625, 153.757813);
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
		cr.MoveTo(191.625, 153.757813);
		cr.LineTo(216.664063, 153.757813);
		cr.LineTo(216.664063, 178.800781);
		cr.LineTo(191.625, 178.800781);
		cr.ClosePath();
		cr.MoveTo(191.625, 153.757813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(154.058594, 191.824219);
		cr.LineTo(179.101563, 191.824219);
		cr.LineTo(179.101563, 216.867188);
		cr.LineTo(154.058594, 216.867188);
		cr.ClosePath();
		cr.MoveTo(154.058594, 191.824219);
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
		cr.MoveTo(154.058594, 191.824219);
		cr.LineTo(179.101563, 191.824219);
		cr.LineTo(179.101563, 216.867188);
		cr.LineTo(154.058594, 216.867188);
		cr.ClosePath();
		cr.MoveTo(154.058594, 191.824219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(191.625, 191.824219);
		cr.LineTo(216.664063, 191.824219);
		cr.LineTo(216.664063, 216.867188);
		cr.LineTo(191.625, 216.867188);
		cr.ClosePath();
		cr.MoveTo(191.625, 191.824219);
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
		cr.MoveTo(191.625, 191.824219);
		cr.LineTo(216.664063, 191.824219);
		cr.LineTo(216.664063, 216.867188);
		cr.LineTo(191.625, 216.867188);
		cr.ClosePath();
		cr.MoveTo(191.625, 191.824219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(229.289063, 153.757813);
		cr.LineTo(254.328125, 153.757813);
		cr.LineTo(254.328125, 178.800781);
		cr.LineTo(229.289063, 178.800781);
		cr.ClosePath();
		cr.MoveTo(229.289063, 153.757813);
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
		cr.MoveTo(229.289063, 153.757813);
		cr.LineTo(254.328125, 153.757813);
		cr.LineTo(254.328125, 178.800781);
		cr.LineTo(229.289063, 178.800781);
		cr.ClosePath();
		cr.MoveTo(229.289063, 153.757813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(266.851563, 153.757813);
		cr.LineTo(291.894531, 153.757813);
		cr.LineTo(291.894531, 178.800781);
		cr.LineTo(266.851563, 178.800781);
		cr.ClosePath();
		cr.MoveTo(266.851563, 153.757813);
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
		cr.MoveTo(266.851563, 153.757813);
		cr.LineTo(291.894531, 153.757813);
		cr.LineTo(291.894531, 178.800781);
		cr.LineTo(266.851563, 178.800781);
		cr.ClosePath();
		cr.MoveTo(266.851563, 153.757813);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(229.289063, 191.824219);
		cr.LineTo(254.328125, 191.824219);
		cr.LineTo(254.328125, 216.867188);
		cr.LineTo(229.289063, 216.867188);
		cr.ClosePath();
		cr.MoveTo(229.289063, 191.824219);
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
		cr.MoveTo(229.289063, 191.824219);
		cr.LineTo(254.328125, 191.824219);
		cr.LineTo(254.328125, 216.867188);
		cr.LineTo(229.289063, 216.867188);
		cr.ClosePath();
		cr.MoveTo(229.289063, 191.824219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(266.851563, 191.824219);
		cr.LineTo(291.894531, 191.824219);
		cr.LineTo(291.894531, 216.867188);
		cr.LineTo(266.851563, 216.867188);
		cr.ClosePath();
		cr.MoveTo(266.851563, 191.824219);
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
		cr.MoveTo(266.851563, 191.824219);
		cr.LineTo(291.894531, 191.824219);
		cr.LineTo(291.894531, 216.867188);
		cr.LineTo(266.851563, 216.867188);
		cr.ClosePath();
		cr.MoveTo(266.851563, 191.824219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(153.960938, 228.886719);
		cr.LineTo(179.003906, 228.886719);
		cr.LineTo(179.003906, 253.929688);
		cr.LineTo(153.960938, 253.929688);
		cr.ClosePath();
		cr.MoveTo(153.960938, 228.886719);
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
		cr.MoveTo(153.960938, 228.886719);
		cr.LineTo(179.003906, 228.886719);
		cr.LineTo(179.003906, 253.929688);
		cr.LineTo(153.960938, 253.929688);
		cr.ClosePath();
		cr.MoveTo(153.960938, 228.886719);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(191.523438, 228.886719);
		cr.LineTo(216.566406, 228.886719);
		cr.LineTo(216.566406, 253.929688);
		cr.LineTo(191.523438, 253.929688);
		cr.ClosePath();
		cr.MoveTo(191.523438, 228.886719);
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
		cr.MoveTo(191.523438, 228.886719);
		cr.LineTo(216.566406, 228.886719);
		cr.LineTo(216.566406, 253.929688);
		cr.LineTo(191.523438, 253.929688);
		cr.ClosePath();
		cr.MoveTo(191.523438, 228.886719);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(153.960938, 266.949219);
		cr.LineTo(179.003906, 266.949219);
		cr.LineTo(179.003906, 291.992188);
		cr.LineTo(153.960938, 291.992188);
		cr.ClosePath();
		cr.MoveTo(153.960938, 266.949219);
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
		cr.MoveTo(153.960938, 266.949219);
		cr.LineTo(179.003906, 266.949219);
		cr.LineTo(179.003906, 291.992188);
		cr.LineTo(153.960938, 291.992188);
		cr.ClosePath();
		cr.MoveTo(153.960938, 266.949219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(191.523438, 266.949219);
		cr.LineTo(216.566406, 266.949219);
		cr.LineTo(216.566406, 291.992188);
		cr.LineTo(191.523438, 291.992188);
		cr.ClosePath();
		cr.MoveTo(191.523438, 266.949219);
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
		cr.MoveTo(191.523438, 266.949219);
		cr.LineTo(216.566406, 266.949219);
		cr.LineTo(216.566406, 291.992188);
		cr.LineTo(191.523438, 291.992188);
		cr.ClosePath();
		cr.MoveTo(191.523438, 266.949219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(229.1875, 228.886719);
		cr.LineTo(254.230469, 228.886719);
		cr.LineTo(254.230469, 253.929688);
		cr.LineTo(229.1875, 253.929688);
		cr.ClosePath();
		cr.MoveTo(229.1875, 228.886719);
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
		cr.MoveTo(229.1875, 228.886719);
		cr.LineTo(254.230469, 228.886719);
		cr.LineTo(254.230469, 253.929688);
		cr.LineTo(229.1875, 253.929688);
		cr.ClosePath();
		cr.MoveTo(229.1875, 228.886719);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(266.75, 228.886719);
		cr.LineTo(291.792969, 228.886719);
		cr.LineTo(291.792969, 253.929688);
		cr.LineTo(266.75, 253.929688);
		cr.ClosePath();
		cr.MoveTo(266.75, 228.886719);
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
		cr.MoveTo(266.75, 228.886719);
		cr.LineTo(291.792969, 228.886719);
		cr.LineTo(291.792969, 253.929688);
		cr.LineTo(266.75, 253.929688);
		cr.ClosePath();
		cr.MoveTo(266.75, 228.886719);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(229.1875, 266.949219);
		cr.LineTo(254.230469, 266.949219);
		cr.LineTo(254.230469, 291.992188);
		cr.LineTo(229.1875, 291.992188);
		cr.ClosePath();
		cr.MoveTo(229.1875, 266.949219);
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
		cr.MoveTo(229.1875, 266.949219);
		cr.LineTo(254.230469, 266.949219);
		cr.LineTo(254.230469, 291.992188);
		cr.LineTo(229.1875, 291.992188);
		cr.ClosePath();
		cr.MoveTo(229.1875, 266.949219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
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
		cr.MoveTo(266.75, 266.949219);
		cr.LineTo(291.792969, 266.949219);
		cr.LineTo(291.792969, 291.992188);
		cr.LineTo(266.75, 291.992188);
		cr.ClosePath();
		cr.MoveTo(266.75, 266.949219);
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
		cr.MoveTo(266.75, 266.949219);
		cr.LineTo(291.792969, 266.949219);
		cr.LineTo(291.792969, 291.992188);
		cr.LineTo(266.75, 291.992188);
		cr.ClosePath();
		cr.MoveTo(266.75, 266.949219);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(1.001692, 0.0, 0.0, 1.001692, 232.392555, -324.548223);
		val.Matrix = matrix;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Restore();
	}
}
