using Cairo;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public class GuiElementPassiveItemSlot : GuiElement
{
	public static double unscaledItemSize = 25.600000381469727;

	public static double unscaledSlotSize = 48.0;

	private ItemSlot slot;

	private IInventory inventory;

	private bool drawBackground;

	private GuiElementStaticText textComposer;

	public GuiElementPassiveItemSlot(ICoreClientAPI capi, ElementBounds bounds, IInventory inventory, ItemSlot slot, bool drawBackground = true)
		: base(capi, bounds)
	{
		this.slot = slot;
		this.inventory = inventory;
		this.drawBackground = drawBackground;
		bounds.fixedWidth = unscaledSlotSize;
		bounds.fixedHeight = unscaledSlotSize;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		Bounds.CalcWorldBounds();
		if (drawBackground)
		{
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.6);
			ElementRoundRectangle(ctx, Bounds);
			ctx.Fill();
			EmbossRoundRectangleElement(ctx, Bounds, inverse: true);
		}
		GuiElement.scaled(unscaledSlotSize);
		ElementBounds bounds = ElementBounds.Fixed(0.0, unscaledSlotSize - GuiStyle.SmallFontSize - 2.0, unscaledSlotSize - 5.0, unscaledSlotSize - 5.0).WithEmptyParent();
		CairoFont cairoFont = CairoFont.WhiteSmallText();
		cairoFont.FontWeight = (FontWeight)1;
		textComposer = new GuiElementStaticText(api, "", EnumTextOrientation.Right, bounds, cairoFont);
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (slot.Itemstack != null)
		{
			double num = GuiElement.scaled(unscaledSlotSize) / 2.0;
			api.Render.PushScissor(Bounds, stacking: true);
			api.Render.RenderItemstackToGui(slot, Bounds.renderX + num, Bounds.renderY + num, 450.0, (float)GuiElement.scaled(unscaledItemSize), -1);
			api.Render.PopScissor();
		}
	}
}
