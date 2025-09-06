using System;
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

    private double timeInside = 0.0;
    private double lastRenderX = 0.0;
    private double lastRenderY = 0.0;

    private readonly GuiElementTextButton button;
    private readonly GuiElementHoverText hover;
    private readonly ElementBounds bounds;

    /// <param name="index">0 = första knappen under grid, 1 = nästa osv.</param>
    /// <param name="label">Text i knappen</param>
    /// <param name="tooltipKeyOrText">Lang-nyckel eller ren text</param>
    /// <param name="unusedOffsetX">Tidigare offset – inte längre använd</param>
    /// <param name="unusedOffsetY">Tidigare offset – inte längre använd</param>
    protected ButtonRTC(ICoreClientAPI api, int index, string label, string tooltipKeyOrText, double unusedOffsetX, double unusedOffsetY) : base(api)
    {
        Float = EnumFloat.Inline;
        VerticalAlign = EnumVerticalAlign.FixedOffset;

        this.index = index;
        this.label = label;
        tooltip = tooltipKeyOrText != null ? Lang.Get(tooltipKeyOrText) : label;

        double size = Math.Ceiling(GuiElement.scaled(UnscaledSize));
        bounds = new GlobalBounds(0.0, 0.0, size, size);
        button = CreateButton(bounds);
        hover = CreateHover(bounds);
    }

    private GuiElementTextButton CreateButton(ElementBounds bounds)
    {
        var font = CairoFont.ButtonText();
        var fontDown = CairoFont.ButtonPressedText();

        // Håll dig till UNscaled här, så Vintage Story skalar korrekt.
        font.UnscaledFontsize = 14f;
        fontDown.UnscaledFontsize = 14f;

        // Skapa knappelementet (centerar texten åt oss som standard)
        var btn = new GuiElementTextButton(api, label, font, fontDown, Click, bounds, EnumButtonStyle.Small);

        // Låt knappen spela sitt eget standardljud.
        btn.PlaySound = true;

        // Säkerställ att interna text-element är uppbyggda.
        btn.ComposeElements(null, null);
        return btn;
    }

    private GuiElementHoverText CreateHover(ElementBounds bounds)
    {
        var hover = new GuiElementHoverText(api, tooltip, CairoFont.WhiteSmallText(), 200, bounds);
        hover.SetAutoDisplay(false);
        return hover;
    }

    protected void SetExtraTip(string text = null) => hover.SetNewText(text == null ? tooltip : $"{tooltip}\n{text}");

    // Det enda klicket gör här är att trigga knappelementets egna ljud (PlaySound = true).
    protected abstract void OnClick();

    private bool Click()
    {
        OnClick();
        return true;
    }

    protected virtual bool Visible => true;

    public override EnumCalcBoundsResult CalcBounds(
        TextFlowPath[] flowPath, double currentLineHeight,
        double offsetX, double lineY, out double nextOffsetX)
    {
        double size = GuiElement.scaled(UnscaledSize);

        // Högerkant (samma ankarpunkt som IH)
        double x = offsetX - GuiElement.scaled(3.0);

        // TOPP-ankrat: start vid gridens överkant, sen nedåt per index
        double y = lineY + GuiElement.scaled(Margin) + index * (size + GuiElement.scaled(Margin));

        BoundsPerLine = new LineRectangled[] { new(x, y, size, size) };
        bounds.fixedWidth = bounds.fixedHeight = size;

        nextOffsetX = offsetX;
        return EnumCalcBoundsResult.Continue;
    }





    public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
    {
        if (!Visible) return;

        lastRenderX = renderX;
        lastRenderY = renderY;
        SetBounds(lastRenderX, lastRenderY);

        button.RenderInteractiveElements(deltaTime);

        hover.SetVisible(MouseOverFor(1.0, deltaTime));
        hover.RenderInteractiveElements(deltaTime);
    }

    private bool MouseOverFor(double time, double delta)
    {
        if (bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
            timeInside += delta;
        else
            timeInside = 0.0;

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

    public override void OnMouseDown(MouseEvent args)
    {
        if (!Visible) return;
        SetBounds(lastRenderX, lastRenderY);
        button.OnMouseDown(api, args);
    }

    public override void OnMouseUp(MouseEvent args)
    {
        if (!Visible) return;
        SetBounds(lastRenderX, lastRenderY);
        button.OnMouseUp(api, args);
    }

    public override void OnMouseMove(MouseEvent args)
    {
        if (!Visible) return;
        SetBounds(lastRenderX, lastRenderY);
        button.OnMouseMove(api, args);
    }

    public override void Dispose()
    {
        button.Dispose();
        hover.Dispose();
    }

    private class GlobalBounds : ElementBounds
    {
        public GlobalBounds(double x, double y, double width, double height)
        {
            absFixedX = x;
            absFixedY = y;
            absInnerWidth = fixedWidth = width;
            absInnerHeight = fixedHeight = height;
            BothSizing = ElementSizing.Fixed;
            ParentBounds = new();
        }

        public ElementBounds MakeChild() => Fill.WithParent(this);

        public override double bgDrawX => absFixedX;
        public override double bgDrawY => absFixedY;
        public override double renderX => absFixedX + renderOffsetX;
        public override double renderY => absFixedY + renderOffsetY;
        public override double absX => absFixedX;
        public override double absY => absFixedY;
    }
}
