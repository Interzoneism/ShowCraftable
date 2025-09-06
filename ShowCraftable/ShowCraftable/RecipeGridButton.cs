using Vintagestory.API.Client;

namespace ShowCraftable;

public class RecipeGridButton : ButtonRTC
{
    // "#": samma stil som IH, men med liten vertikal finjustering för god optik
    // Justera offsetY ±1 om din UI-scale gör den en pixel off.
    public RecipeGridButton(ICoreClientAPI api)
        : base(api, index: 0, label: "#", tooltipText: "Fetch ingredients", offsetX: -1.0, offsetY: -9.5)
    {
        Float = EnumFloat.Inline;
        VerticalAlign = EnumVerticalAlign.FixedOffset;
    }

    protected override void OnClick()
    {
        // endast spel-ljudet (gör inget mer)
        api.Gui.PlaySound("menubutton_press");
    }
}
