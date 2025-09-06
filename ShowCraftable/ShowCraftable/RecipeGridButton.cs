using Vintagestory.API.Client;

namespace ShowCraftable;

public class RecipeGridButton : ButtonRTC
{
    public RecipeGridButton(ICoreClientAPI api) : base(api, 0, "*", "Fetch all the ingredients", -1.0, -1.0)
    {
        Float = EnumFloat.Inline;
        VerticalAlign = EnumVerticalAlign.FixedOffset;
    }

    protected override void OnClick()
    {
        api.Gui.PlaySound("menubutton_press");
    }
}
