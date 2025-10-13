using System;
using System.Collections.Generic;

namespace ShowCraftable;

public class ShowCraftableConfig
{
    public bool EnableFetchButton { get; set; } = true;

    public bool DisableFetchButtonOnServer { get; set; } = false;

    public bool UseDefaultFont { get; set; } = false;

    public int SearchDistanceItems { get; set; } = 20;

    public int AllStacksPartitions { get; set; } = -1;

    public Dictionary<string, string> GroupPageCategoryNames { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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

        if (GroupPageCategoryNames == null)
        {
            GroupPageCategoryNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        else if (GroupPageCategoryNames.Comparer != StringComparer.OrdinalIgnoreCase)
        {
            GroupPageCategoryNames = new Dictionary<string, string>(GroupPageCategoryNames, StringComparer.OrdinalIgnoreCase);
        }

        var cleaned = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in GroupPageCategoryNames)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key)) continue;
            if (kvp.Value == null) continue;
            cleaned[kvp.Key.Trim()] = kvp.Value;
        }

        GroupPageCategoryNames = cleaned;
    }
}
