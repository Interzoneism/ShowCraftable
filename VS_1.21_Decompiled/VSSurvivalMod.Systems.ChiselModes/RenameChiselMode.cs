using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VSSurvivalMod.Systems.ChiselModes;

public class RenameChiselMode : ChiselMode
{
	public override DrawSkillIconDelegate DrawAction(ICoreClientAPI capi)
	{
		return Drawedit_svg;
	}

	public override bool Apply(BlockEntityChisel chiselEntity, IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool isBreak, byte currentMaterialIndex)
	{
		_ = (IClientWorldAccessor)chiselEntity.Api.World;
		string prevName = chiselEntity.BlockName;
		GuiDialogBlockEntityTextInput guiDialogBlockEntityTextInput = new GuiDialogBlockEntityTextInput(Lang.Get("Block name"), chiselEntity.Pos, chiselEntity.BlockName, chiselEntity.Api as ICoreClientAPI, new TextAreaConfig
		{
			MaxWidth = 500
		});
		guiDialogBlockEntityTextInput.OnTextChanged = delegate(string text)
		{
			chiselEntity.BlockName = text;
		};
		guiDialogBlockEntityTextInput.OnCloseCancel = delegate
		{
			chiselEntity.BlockName = prevName;
		};
		guiDialogBlockEntityTextInput.TryOpen();
		return false;
	}

	public void Drawedit_svg(Context cr, int x, int y, float width, float height, double[] rgba)
	{
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Expected O, but got Unknown
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Expected O, but got Unknown
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Expected O, but got Unknown
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Expected O, but got Unknown
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Expected O, but got Unknown
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Expected O, but got Unknown
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03da: Expected O, but got Unknown
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_0469: Expected O, but got Unknown
		Pattern val = null;
		Matrix matrix = cr.Matrix;
		cr.Save();
		float num = 382f;
		float num2 = 200f;
		float num3 = Math.Min(width / num, height / num2);
		matrix.Translate((double)((float)x + Math.Max(0f, (width - num * num3) / 2f)), (double)((float)y + Math.Max(0f, (height - num2 * num3) / 2f)));
		matrix.Scale((double)num3, (double)num3);
		cr.Matrix = matrix;
		cr.Operator = (Operator)2;
		cr.LineWidth = 9.0;
		cr.MiterLimit = 4.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(10.628906, 10.628906);
		cr.LineTo(371.445313, 10.628906);
		cr.LineTo(371.445313, 189.617188);
		cr.LineTo(10.628906, 189.617188);
		cr.ClosePath();
		cr.MoveTo(10.628906, 10.628906);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(3.543307, 0.0, 0.0, 3.543307, -219.495455, -129.753943);
		val.Matrix = matrix;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 9.0;
		cr.MiterLimit = 4.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(75.972656, 47.5625);
		cr.LineTo(75.972656, 150.789063);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(3.543307, 0.0, 0.0, 3.543307, -219.495455, -129.753943);
		val.Matrix = matrix;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 9.0;
		cr.MiterLimit = 4.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(52.308594, 49.4375);
		cr.LineTo(98.714844, 49.4375);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(3.543307, 0.0, 0.0, 3.543307, -219.495455, -129.753943);
		val.Matrix = matrix;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Operator = (Operator)2;
		cr.LineWidth = 9.0;
		cr.MiterLimit = 4.0;
		cr.LineCap = (LineCap)0;
		cr.LineJoin = (LineJoin)0;
		val = (Pattern)new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(val);
		cr.NewPath();
		cr.MoveTo(53.265625, 151.5);
		cr.LineTo(99.667969, 151.5);
		cr.Tolerance = 0.1;
		cr.Antialias = (Antialias)0;
		matrix = new Matrix(3.543307, 0.0, 0.0, 3.543307, -219.495455, -129.753943);
		val.Matrix = matrix;
		cr.StrokePreserve();
		if (val != null)
		{
			val.Dispose();
		}
		cr.Restore();
	}
}
