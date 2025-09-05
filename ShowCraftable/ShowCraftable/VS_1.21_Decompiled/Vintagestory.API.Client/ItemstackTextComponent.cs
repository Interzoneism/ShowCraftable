using System;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class ItemstackTextComponent : ItemstackComponentBase
{
	private DummySlot slot;

	private double size;

	public bool ShowStacksize;

	private Action<ItemStack> onStackClicked;

	public ItemstackTextComponent(ICoreClientAPI capi, ItemStack itemstack, double size, double rightSidePadding = 0.0, EnumFloat floatType = EnumFloat.Left, Action<ItemStack> onStackClicked = null)
		: base(capi)
	{
		size = GuiElement.scaled(size);
		slot = new DummySlot(itemstack);
		this.onStackClicked = onStackClicked;
		Float = floatType;
		this.size = size;
		BoundsPerLine = new LineRectangled[1]
		{
			new LineRectangled(0.0, 0.0, size, size)
		};
		PaddingRight = GuiElement.scaled(rightSidePadding);
	}

	public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		TextFlowPath currentFlowPathSection = GetCurrentFlowPathSection(flowPath, lineY);
		offsetX += GuiElement.scaled(PaddingLeft);
		bool flag = offsetX + BoundsPerLine[0].Width > currentFlowPathSection.X2;
		BoundsPerLine[0].X = (flag ? 0.0 : offsetX);
		BoundsPerLine[0].Y = lineY + (flag ? currentLineHeight : 0.0);
		if (Float == EnumFloat.Right)
		{
			BoundsPerLine[0].X = currentFlowPathSection.X2 - size;
		}
		BoundsPerLine[0].Width = size + GuiElement.scaled(PaddingRight);
		nextOffsetX = (flag ? 0.0 : offsetX) + BoundsPerLine[0].Width;
		if (!flag)
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
		LineRectangled lineRectangled = BoundsPerLine[0];
		double num = GuiElement.scaled(PaddingLeft);
		double num2 = GuiElement.scaled(PaddingRight);
		double num3 = lineRectangled.Width - num - num2;
		ElementBounds elementBounds = ElementBounds.FixedSize((int)(lineRectangled.Width / (double)RuntimeEnv.GUIScale), (int)(lineRectangled.Height / (double)RuntimeEnv.GUIScale));
		elementBounds.ParentBounds = capi.Gui.WindowBounds;
		elementBounds.CalcWorldBounds();
		elementBounds.absFixedX = renderX + lineRectangled.X;
		elementBounds.absFixedY = renderY + lineRectangled.Y + offY;
		api.Render.PushScissor(elementBounds, stacking: true);
		api.Render.RenderItemstackToGui(slot, renderX + lineRectangled.X + num + num3 * 0.5 + offX, renderY + lineRectangled.Y + lineRectangled.Height * 0.5 + offY, GuiElement.scaled(100.0), (float)size * 0.58f, -1, shading: true, rotate: false, ShowStacksize);
		api.Render.PopScissor();
		int num4 = (int)((double)api.Input.MouseX - renderX);
		int num5 = (int)((double)api.Input.MouseY - renderY);
		if (lineRectangled.PointInside(num4, num5))
		{
			RenderItemstackTooltip(slot, renderX + (double)num4 + offX, renderY + (double)num5 + offY, deltaTime);
		}
	}

	public override void OnMouseDown(MouseEvent args)
	{
		LineRectangled[] boundsPerLine = BoundsPerLine;
		for (int i = 0; i < boundsPerLine.Length; i++)
		{
			if (boundsPerLine[i].PointInside(args.X, args.Y))
			{
				onStackClicked?.Invoke(slot.Itemstack);
			}
		}
	}
}
