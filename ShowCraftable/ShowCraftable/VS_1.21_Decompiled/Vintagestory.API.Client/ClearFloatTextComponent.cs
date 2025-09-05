using Cairo;

namespace Vintagestory.API.Client;

public class ClearFloatTextComponent : RichTextComponent
{
	public ClearFloatTextComponent(ICoreClientAPI api, float unScaleMarginTop = 0f)
		: base(api, "", CairoFont.WhiteDetailText())
	{
		Float = EnumFloat.None;
		UnscaledMarginTop = unScaleMarginTop;
	}

	public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		double num = lineY;
		foreach (TextFlowPath textFlowPath in flowPath)
		{
			if (textFlowPath.Y1 <= lineY && textFlowPath.Y2 >= lineY)
			{
				if (!(textFlowPath.X1 > 0.0))
				{
					break;
				}
				num = textFlowPath.Y2;
			}
		}
		BoundsPerLine = new LineRectangled[1]
		{
			new LineRectangled(0.0, lineY, 10.0, num - lineY + 1.0)
		};
		nextOffsetX = 0.0;
		if (Float != EnumFloat.None)
		{
			return EnumCalcBoundsResult.Continue;
		}
		return EnumCalcBoundsResult.Nextline;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
	}

	public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
	}
}
