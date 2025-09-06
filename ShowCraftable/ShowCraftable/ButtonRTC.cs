using System;
using HarmonyLib;                  // <— nytt
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace ShowCraftable;

public abstract class ButtonRTC : RichTextComponentBase
{
    protected const double UnscaledSize = 24.0;
    protected const double Margin = 2.0;

    private readonly string label;
    private readonly string tooltip;
    private readonly int index;
    private readonly double offsetX;
    private readonly double offsetY;

    private double timeInside = 0.0;
    private double lastRenderX = 0.0;
    private double lastRenderY = 0.0;

    private readonly GuiElementTextButton button;
    private readonly GuiElementHoverText hover;
    private readonly ElementBounds bounds;

    /// <param name="index">0 = överst till höger om grid, 1 = nästa under, osv.</param>
    /// <param name="label">Knapptext/symbol</param>
    /// <param name="tooltipText">Tooltip (ren text räcker)</param>
    /// <param name="offsetX">finjustering X i glyph-pixel</param>
    /// <param name="offsetY">finjustering Y i glyph-pixel</param>
    protected ButtonRTC(ICoreClientAPI api, int index, string label, string tooltipText, double offsetX, double offsetY) : base(api)
    {
        Float = EnumFloat.Inline;
        VerticalAlign = EnumVerticalAlign.FixedOffset;

        this.index = index;
        this.label = label;
        this.offsetX = offsetX;
        this.offsetY = offsetY;
        tooltip = tooltipText ?? label;

        double size = Math.Ceiling(GuiElement.scaled(UnscaledSize));
        bounds = new GlobalBounds(0.0, 0.0, size, size);
        button = CreateButton(bounds);
        hover = CreateHover(bounds);
    }

    private GuiElementTextButton CreateButton(ElementBounds eb)
    {
        // Samma typsnitt och "stora" storlek som Improved Handbook
        var font = CairoFont.ButtonText();
        var fontDown = CairoFont.ButtonPressedText();
        font.UnscaledFontsize = GuiElement.scaled(32.0);
        fontDown.UnscaledFontsize = GuiElement.scaled(32.0);   // :contentReference[oaicite:2]{index=2}

        var btn = new GuiElementTextButton(api, label, font, fontDown, Click, eb, EnumButtonStyle.Small);
        btn.PlaySound = true; // låt standard-knapp-ljudet spelas

        // Exakt samma centreringsknep: intern text-offset med dubbel-scalad pixel-offset
        var trav = Traverse.Create(btn);
        AdjustOffsets(trav.Field<GuiElementStaticText>("normalText").Value);
        AdjustOffsets(trav.Field<GuiElementStaticText>("pressedText").Value);

        btn.ComposeElements(null, null);
        return btn;
    }

    private void AdjustOffsets(GuiElementStaticText t)
    {
        if (t == null) return;
        t.offsetX = GuiElement.scaled(GuiElement.scaled(offsetX));
        t.offsetY = GuiElement.scaled(GuiElement.scaled(offsetY));   // :contentReference[oaicite:3]{index=3}
    }

    private GuiElementHoverText CreateHover(ElementBounds eb)
    {
        var h = new GuiElementHoverText(api, tooltip, CairoFont.WhiteSmallText(), 200, eb);
        h.SetAutoDisplay(false);
        return h;
    }

    protected abstract void OnClick();

    private bool Click() { OnClick(); return true; }

    protected virtual bool Visible => true;

    // Placering: till höger om grid (x), överkanten (y), stapla med index
    public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
    {
        double size = GuiElement.scaled(UnscaledSize);
        double x = offsetX - GuiElement.scaled(3.0);
        double y = lineY + GuiElement.scaled(Margin + index * (UnscaledSize + Margin));

        BoundsPerLine = new[] { new LineRectangled(x, y, size, size) };
        bounds.fixedWidth = bounds.fixedHeight = size;
        nextOffsetX = offsetX;
        return EnumCalcBoundsResult.Continue;
    }

    public override void RenderInteractiveElements(float dt, double rx, double ry, double rz)
    {
        if (!Visible) return;
        lastRenderX = rx;
        lastRenderY = ry;
        SetBounds(lastRenderX, lastRenderY);
        button.RenderInteractiveElements(dt);

        hover.SetVisible(MouseOverFor(1.0, dt));
        hover.RenderInteractiveElements(dt);
    }

    private bool MouseOverFor(double time, double delta)
    {
        if (bounds.PointInside(api.Input.MouseX, api.Input.MouseY)) timeInside += delta;
        else timeInside = 0.0;
        return timeInside > time;
    }

    private void SetBounds(double xOffset = 0.0, double yOffset = 0.0)
    {
        var r = BoundsPerLine[0];
        bounds.absInnerWidth = r.Width;
        bounds.absInnerHeight = r.Height;
        bounds.absFixedX = xOffset + r.X;
        bounds.absFixedY = yOffset + r.Y;
    }

    public override void OnMouseDown(MouseEvent args) { if (Visible) { SetBounds(lastRenderX, lastRenderY); button.OnMouseDown(api, args); } }
    public override void OnMouseUp(MouseEvent args) { if (Visible) { SetBounds(lastRenderX, lastRenderY); button.OnMouseUp(api, args); } }
    public override void OnMouseMove(MouseEvent args) { if (Visible) { SetBounds(lastRenderX, lastRenderY); button.OnMouseMove(api, args); } }

    public override void Dispose() { button.Dispose(); hover.Dispose(); }

    private class GlobalBounds : ElementBounds
    {
        public GlobalBounds(double x, double y, double w, double h)
        {
            absFixedX = x; absFixedY = y;
            absInnerWidth = fixedWidth = w;
            absInnerHeight = fixedHeight = h;
            BothSizing = ElementSizing.Fixed;
            ParentBounds = new();
        }
        public override double bgDrawX => absFixedX;
        public override double bgDrawY => absFixedY;
        public override double renderX => absFixedX + renderOffsetX;
        public override double renderY => absFixedY + renderOffsetY;
        public override double absX => absFixedX;
        public override double absY => absFixedY;
    }
}
