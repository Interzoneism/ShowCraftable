using Vintagestory.API.Client;

namespace ShowCraftable;

public class RecipeGridButton : ButtonRTC
{
    private const double YManualOffset = 0;

    public RecipeGridButton(ICoreClientAPI api)
        : base(api, 0, "#", "Fetch all the ingredients", -1.0, -5.0)
    {
        Float = EnumFloat.Inline;
        VerticalAlign = EnumVerticalAlign.FixedOffset;
    }

    protected override void OnClick()
    {
        api.Gui.PlaySound("menubutton_press");
    }

    public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
    {
        var result = base.CalcBounds(flowPath, currentLineHeight, offsetX, lineY, out nextOffsetX);
        var r = BoundsPerLine[0];
        r.Y += GuiElement.scaled(YManualOffset);
        BoundsPerLine[0] = r;
        return result;
    }
}
