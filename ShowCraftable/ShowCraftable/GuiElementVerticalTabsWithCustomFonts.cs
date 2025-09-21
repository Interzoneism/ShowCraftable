using System;
using Cairo;
using Vintagestory.API.Client;

namespace ShowCraftable;

internal sealed class GuiElementVerticalTabsWithCustomFonts : GuiElementVerticalTabs
{
    private readonly string _defaultFontName;
    private readonly string _defaultSelectedFontName;
    private readonly FontWeight _defaultFontWeight;
    private readonly FontWeight _defaultSelectedFontWeight;
    private double[] _textOffsets = Array.Empty<double>();

    public GuiElementVerticalTabsWithCustomFonts(ICoreClientAPI capi, GuiTab[] tabs, CairoFont font, CairoFont selectedFont, ElementBounds bounds, Action<int, GuiTab> onTabClicked)
        : base(capi, tabs, font, selectedFont, bounds, onTabClicked)
    {
        _defaultFontName = font.Fontname;
        _defaultSelectedFontName = selectedFont.Fontname;
        _defaultFontWeight = font.FontWeight;
        _defaultSelectedFontWeight = selectedFont.FontWeight;
    }

    public override void ComposeTextElements(Context ctxStatic, ImageSurface surfaceStatic)
    {
        Bounds.CalcWorldBounds();

        using var surface = new ImageSurface(Format.Argb32, (int)Bounds.InnerWidth + 1, (int)Bounds.InnerHeight + 1);
        using var ctx = new Context(surface);

        double outlineThickness = GuiElement.scaled(1.0);
        double tabSpacing = GuiElement.scaled(unscaledTabSpacing);
        double tabPadding = GuiElement.scaled(unscaledTabPadding);
        tabHeight = GuiElement.scaled(unscaledTabHeight);

        Font.Color[3] = 0.85;

        double tabHeightWithBorder = tabHeight + 1.0;
        Font.SetupContext(ctx);
        FontExtents defaultExtents = Font.GetFontExtents();
        double defaultOffsetY = (tabHeightWithBorder - defaultExtents.Height) / 2.0;

        double maxTabWidth = 0.0;
        for (int i = 0; i < tabs.Length; i++)
        {
            ApplyRegularFontForTab(tabs[i]);
            Font.SetupContext(ctx);
            TextExtents extents = ctx.TextExtents(tabs[i].Name ?? string.Empty);
            double width = extents.Width + 1.0 + 2.0 * tabPadding;
            if (width > maxTabWidth) maxTabWidth = width;
        }

        RestoreRegularFont();

        _textOffsets = new double[tabs.Length];
        double currentY = 0.0;

        for (int i = 0; i < tabs.Length; i++)
        {
            tabWidths[i] = (int)maxTabWidth + 1;

            currentY += tabs[i].PaddingTop;

            double xStart;
            ctx.NewPath();
            if (Right)
            {
                xStart = 1.0;
                ctx.MoveTo(xStart, currentY + tabHeight);
                ctx.LineTo(xStart, currentY);
                ctx.LineTo(xStart + tabWidths[i] + outlineThickness, currentY);
                ctx.ArcNegative(xStart + tabWidths[i], currentY + outlineThickness, outlineThickness, 4.71238899230957, 3.1415927410125732);
                ctx.ArcNegative(xStart + tabWidths[i], currentY - outlineThickness + tabHeight, outlineThickness, 3.1415927410125732, 1.5707963705062866);
            }
            else
            {
                xStart = (int)Bounds.InnerWidth + 1;
                ctx.MoveTo(xStart, currentY + tabHeight);
                ctx.LineTo(xStart, currentY);
                ctx.LineTo(xStart - tabWidths[i] + outlineThickness, currentY);
                ctx.ArcNegative(xStart - tabWidths[i], currentY + outlineThickness, outlineThickness, 4.71238899230957, 3.1415927410125732);
                ctx.ArcNegative(xStart - tabWidths[i], currentY - outlineThickness + tabHeight, outlineThickness, 3.1415927410125732, 1.5707963705062866);
            }

            ctx.ClosePath();
            var bgColor = GuiStyle.DialogDefaultBgColor;
            ctx.SetSourceRGBA(bgColor[0], bgColor[1], bgColor[2], bgColor[3]);
            ctx.FillPreserve();
            ShadePath(ctx);

            ApplyRegularFontForTab(tabs[i]);
            Font.SetupContext(ctx);
            FontExtents extentsForTab = Font.GetFontExtents();
            double offsetY = (tabHeightWithBorder - extentsForTab.Height) / 2.0;
            if (double.IsNaN(offsetY)) offsetY = defaultOffsetY;
            _textOffsets[i] = offsetY;

            double textX = xStart - (!Right ? tabWidths[i] : 0) + tabPadding;
            DrawTextLineAt(ctx, tabs[i].Name ?? string.Empty, textX, currentY + offsetY);

            currentY += tabHeight + tabSpacing;
        }

        RestoreRegularFont();
        Font.Color[3] = 1.0;
        ComposeOverlaysWithCustomFonts();
        generateTexture(surface, ref baseTexture);
    }

    private void ComposeOverlaysWithCustomFonts()
    {
        double outlineThickness = GuiElement.scaled(1.0);
        double tabPadding = GuiElement.scaled(unscaledTabPadding);

        for (int i = 0; i < tabs.Length; i++)
        {
            using var surface = new ImageSurface(Format.Argb32, tabWidths[i] + 1, (int)tabHeight + 1);
            using var ctx = genContext(surface);

            double width = tabWidths[i] + 1;
            ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.0);
            ctx.Paint();
            ctx.NewPath();
            ctx.MoveTo(width, tabHeight + 1.0);
            ctx.LineTo(width, 0.0);
            ctx.LineTo(outlineThickness, 0.0);
            ctx.ArcNegative(0.0, outlineThickness, outlineThickness, 4.71238899230957, 3.1415927410125732);
            ctx.ArcNegative(0.0, tabHeight - outlineThickness, outlineThickness, 3.1415927410125732, 1.5707963705062866);
            ctx.ClosePath();

            var bgColor = GuiStyle.DialogDefaultBgColor;
            ctx.SetSourceRGBA(bgColor[0], bgColor[1], bgColor[2], bgColor[3]);
            ctx.Fill();

            ctx.NewPath();
            if (Right)
            {
                ctx.LineTo(1.0, 1.0);
                ctx.LineTo(width, 1.0);
                ctx.LineTo(width, tabHeight);
                ctx.LineTo(1.0, tabHeight - 1.0);
            }
            else
            {
                ctx.LineTo(1.0 + width, 1.0);
                ctx.LineTo(1.0, 1.0);
                ctx.LineTo(1.0, tabHeight - 1.0);
                ctx.LineTo(1.0 + width, tabHeight);
            }

            const float borderWidth = 2f;
            ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor[0] * 1.6, GuiStyle.DialogStrongBgColor[1] * 1.6, GuiStyle.DialogStrongBgColor[2] * 1.6, 1.0);
            ctx.LineWidth = borderWidth * 1.75;
            ctx.StrokePreserve();
            SurfaceTransformBlur.BlurPartial(surface, 8.0, 16);
            ctx.SetSourceRGBA(0.17647058823529413, 7.0 / 51.0, 11.0 / 85.0, 1.0);
            ctx.LineWidth = borderWidth;
            ctx.Stroke();

            ApplySelectedFontForTab(tabs[i]);
            selectedFont.SetupContext(ctx);
            double offsetY = _textOffsets.Length > i ? _textOffsets[i] : textOffsetY;
            DrawTextLineAt(ctx, tabs[i].Name ?? string.Empty, tabPadding + 2.0, offsetY);

            generateTexture(surface, ref hoverTextures[i]);
        }

        RestoreSelectedFont();
    }

    private void ApplyRegularFontForTab(GuiTab tab)
    {
        var categoryCode = ShowCraftableSystem.TryGetCategoryCode(tab);
        var fontName = ShowCraftableSystem.GetCraftableTabFontName(categoryCode);
        var fontWeight = ShowCraftableSystem.GetCraftableTabFontWeight(categoryCode);
        Font.WithFont(fontName ?? _defaultFontName);
        Font.WithWeight(fontWeight ?? _defaultFontWeight);
    }

    private void RestoreRegularFont()
    {
        Font.WithFont(_defaultFontName);
        Font.WithWeight(_defaultFontWeight);
    }

    private void ApplySelectedFontForTab(GuiTab tab)
    {
        var categoryCode = ShowCraftableSystem.TryGetCategoryCode(tab);
        var fontName = ShowCraftableSystem.GetCraftableTabFontName(categoryCode);
        var fontWeight = ShowCraftableSystem.GetCraftableTabFontWeight(categoryCode);
        selectedFont.WithFont(fontName ?? _defaultSelectedFontName);
        selectedFont.WithWeight(fontWeight ?? _defaultSelectedFontWeight);
    }

    private void RestoreSelectedFont()
    {
        selectedFont.WithFont(_defaultSelectedFontName);
        selectedFont.WithWeight(_defaultSelectedFontWeight);
    }
}
