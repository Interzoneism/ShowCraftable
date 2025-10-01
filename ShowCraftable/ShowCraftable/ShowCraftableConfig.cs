using System.Collections.Generic;

namespace ShowCraftable;

public class ShowCraftableConfig
{
    public bool EnableFetchButton { get; set; } = true;

    public bool DisableFetchButtonOnServer { get; set; } = false;

    public bool UseDefaultFont { get; set; } = false;

    public int SearchDistanceItems { get; set; } = 20;

    public int AllStacksPartitions { get; set; } = -1;

    public List<CraftableTabConfig> CraftableTabs { get; set; } = CraftableTabConfig.CreateDefaultList();

    public void Normalize()
    {
        if (SearchDistanceItems < 0)
        {
            SearchDistanceItems = 0;
        }

        if (AllStacksPartitions < -1)
        {
            AllStacksPartitions = -1;
        }

        CraftableTabs ??= CraftableTabConfig.CreateDefaultList();
        CraftableTabConfig.NormalizeList(CraftableTabs);
    }
}

public class CraftableTabConfig
{
    public string VariantKey { get; set; }
        = string.Empty;

    public string TabKey { get; set; }
        = string.Empty;

    public string CategoryCode { get; set; }
        = string.Empty;

    public string DisplayName { get; set; }
        = string.Empty;

    public bool IncludeAll { get; set; }
        = false;

    public bool ModsOnly { get; set; }
        = false;

    public bool WoodOnly { get; set; }
        = false;

    public bool StoneOnly { get; set; }
        = false;

    public bool Enabled { get; set; }
        = true;

    public double? PaddingTop { get; set; }
        = null;

    public string FontName { get; set; }
        = null;

    public string FontWeight { get; set; }
        = null;

    public static List<CraftableTabConfig> CreateDefaultList()
    {
        return new List<CraftableTabConfig>
        {
            new()
            {
                VariantKey = "all",
                TabKey = "allTab",
                CategoryCode = ShowCraftableSystem.CraftableAllCategoryCode,
                DisplayName = "Craftable",
                IncludeAll = true,
                PaddingTop = 20.0,
                FontName = "Arial Black"
            },
            new()
            {
                VariantKey = "van",
                TabKey = "craftableTab",
                CategoryCode = ShowCraftableSystem.CraftableCategoryCode,
                DisplayName = "● Base Items",
                PaddingTop = 5.0,
                FontName = "Arial",
                FontWeight = "Bold"
            },
            new()
            {
                VariantKey = "mods",
                TabKey = "modTab",
                CategoryCode = ShowCraftableSystem.CraftableModsCategoryCode,
                DisplayName = "● Mod Items",
                PaddingTop = 5.0,
                ModsOnly = true,
                FontName = "Arial",
                FontWeight = "Bold"
            },
            new()
            {
                VariantKey = "wood",
                TabKey = "woodTab",
                CategoryCode = ShowCraftableSystem.CraftableWoodCategoryCode,
                DisplayName = "● Wood Types",
                PaddingTop = 5.0,
                WoodOnly = true,
                FontName = "Arial",
                FontWeight = "Bold"
            },
            new()
            {
                VariantKey = "stone",
                TabKey = "stoneTab",
                CategoryCode = ShowCraftableSystem.CraftableStoneCategoryCode,
                DisplayName = "● Stone Types",
                PaddingTop = 5.0,
                StoneOnly = true,
                FontName = "Arial",
                FontWeight = "Bold"
            }
        };
    }

    public static void NormalizeList(List<CraftableTabConfig> tabs)
    {
        if (tabs == null) return;

        var defaults = CreateDefaultList();
        var byVariant = new Dictionary<string, CraftableTabConfig>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var tab in tabs)
        {
            if (tab == null) continue;
            tab.VariantKey = NormalizeString(tab.VariantKey);
            tab.TabKey = NormalizeString(tab.TabKey);
            tab.CategoryCode = NormalizeString(tab.CategoryCode);
            tab.DisplayName = string.IsNullOrWhiteSpace(tab.DisplayName) ? string.Empty : tab.DisplayName;
            if (!byVariant.ContainsKey(tab.VariantKey))
            {
                byVariant[tab.VariantKey] = tab;
            }
        }

        foreach (var def in defaults)
        {
            if (!byVariant.TryGetValue(def.VariantKey, out var existing))
            {
                tabs.Add(Clone(def));
            }
            else
            {
                if (string.IsNullOrEmpty(existing.TabKey)) existing.TabKey = def.TabKey;
                if (string.IsNullOrEmpty(existing.CategoryCode)) existing.CategoryCode = def.CategoryCode;
                if (string.IsNullOrEmpty(existing.DisplayName)) existing.DisplayName = def.DisplayName;
                if (existing.PaddingTop == null) existing.PaddingTop = def.PaddingTop;
                if (string.IsNullOrEmpty(existing.FontName)) existing.FontName = def.FontName;
                if (string.IsNullOrEmpty(existing.FontWeight)) existing.FontWeight = def.FontWeight;
            }
        }
    }

    private static CraftableTabConfig Clone(CraftableTabConfig source)
    {
        return new CraftableTabConfig
        {
            VariantKey = source.VariantKey,
            TabKey = source.TabKey,
            CategoryCode = source.CategoryCode,
            DisplayName = source.DisplayName,
            IncludeAll = source.IncludeAll,
            ModsOnly = source.ModsOnly,
            WoodOnly = source.WoodOnly,
            StoneOnly = source.StoneOnly,
            Enabled = source.Enabled,
            PaddingTop = source.PaddingTop,
            FontName = source.FontName,
            FontWeight = source.FontWeight
        };
    }

    private static string NormalizeString(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
