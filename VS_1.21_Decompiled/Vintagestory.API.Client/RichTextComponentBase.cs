using Cairo;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public abstract class RichTextComponentBase
{
	protected ICoreClientAPI api;

	public string MouseOverCursor { get; protected set; }

	public virtual LineRectangled[] BoundsPerLine { get; protected set; }

	public virtual double UnscaledMarginTop { get; set; }

	public virtual double PaddingRight { get; set; }

	public virtual double PaddingLeft { get; set; }

	public virtual EnumFloat Float { get; set; } = EnumFloat.Inline;

	public virtual Vec4f RenderColor { get; set; }

	public virtual EnumVerticalAlign VerticalAlign { get; set; } = EnumVerticalAlign.Bottom;

	public RichTextComponentBase(ICoreClientAPI api)
	{
		this.api = api;
	}

	public virtual void ComposeElements(Context ctx, ImageSurface surface)
	{
	}

	public virtual void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
	}

	public virtual EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		nextOffsetX = offsetX;
		return EnumCalcBoundsResult.Continue;
	}

	protected virtual TextFlowPath GetCurrentFlowPathSection(TextFlowPath[] flowPath, double posY)
	{
		for (int i = 0; i < flowPath.Length; i++)
		{
			if (flowPath[i].Y1 <= posY && flowPath[i].Y2 >= posY)
			{
				return flowPath[i];
			}
		}
		return null;
	}

	public virtual void OnMouseMove(MouseEvent args)
	{
	}

	public virtual void OnMouseDown(MouseEvent args)
	{
	}

	public virtual void OnMouseUp(MouseEvent args)
	{
	}

	public virtual void Dispose()
	{
	}

	public virtual bool UseMouseOverCursor(ElementBounds richtextBounds)
	{
		int num = (int)((double)api.Input.MouseX - richtextBounds.absX);
		int num2 = (int)((double)api.Input.MouseY - richtextBounds.absY);
		for (int i = 0; i < BoundsPerLine.Length; i++)
		{
			if (BoundsPerLine[i].PointInside(num, num2))
			{
				return true;
			}
		}
		return false;
	}
}
