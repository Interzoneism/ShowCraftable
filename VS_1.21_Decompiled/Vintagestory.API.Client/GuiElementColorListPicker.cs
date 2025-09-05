using Cairo;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementColorListPicker : GuiElementElementListPickerBase<int>
{
	public GuiElementColorListPicker(ICoreClientAPI capi, int elem, ElementBounds bounds)
		: base(capi, elem, bounds)
	{
	}

	public override void DrawElement(int color, Context ctx, ImageSurface surface)
	{
		double[] array = ColorUtil.ToRGBADoubles(color);
		ctx.SetSourceRGBA(array[0], array[1], array[2], 1.0);
		GuiElement.RoundRectangle(ctx, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, 1.0);
		ctx.Fill();
	}
}
