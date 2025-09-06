using Vintagestory.API.Client;

namespace ShowCraftable;

public class RecipeGridButton : ButtonRTC
{
    // Label "F" (mittcentrerad av knappen själv). Tooltip visas efter ~1s hover.
    public RecipeGridButton(ICoreClientAPI api) : base(api, 0, "F", "Fetch all the ingredients", -1.0, -1.0)
    {
        Float = EnumFloat.Inline;
        VerticalAlign = EnumVerticalAlign.FixedOffset;
    }

    // Gör inget extra – knappelementet spelar sitt eget klickljud (PlaySound = true).
    protected override void OnClick() { /* noop */ }
}
