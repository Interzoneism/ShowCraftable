using Vintagestory.API.Client;

namespace ShowCraftable;

public class RecipeGridButton : ButtonRTC
{
    public RecipeGridButton(ICoreClientAPI api) : base(api, 0, "#", "Fetch all the ingredients", -1.0, -120)
    {
        Float = EnumFloat.Inline;
        VerticalAlign = EnumVerticalAlign.FixedOffset;
    }

    // Gör inget extra – knappelementet spelar sitt eget klickljud (PlaySound = true).
    protected override void OnClick() 
    {
        api.Gui.PlaySound("menubutton_press");
    }

    private struct Bounds
    {
        private int xPos;
        private int yPos;
        private readonly int xLen;
        private readonly int yLen;
        private readonly int outerLen;

        public Bounds(int xLen, int yLen, int outerLen)
        {
            this.xLen = xLen;
            this.yLen = yLen;
            this.outerLen = outerLen;
        }

        public void Align(int outer, int inner)
        {
            int x = outer % outerLen - inner % xLen;
            int y = outer / outerLen - inner / xLen;
            if (x >= 0 && y >= 0 && x + xLen < outerLen && y + yLen < outerLen)
            {
                xPos = x;
                yPos = y;
            }
        }

        public readonly int ToInner(int outer)
            => outer % outerLen - xPos + (outer / outerLen - yPos) * xLen;

        public readonly bool Contains(int i)
        {
            int x = i % outerLen, y = i / outerLen;
            return x >= xPos && x < xPos + xLen
                && y >= yPos && y < yPos + yLen;
        }
    }
}
