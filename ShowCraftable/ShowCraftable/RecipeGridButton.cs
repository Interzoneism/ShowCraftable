using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ShowCraftable;

public class RecipeGridButton : ButtonFetch
{
    private const double YManualOffset = 0;
    private readonly SlideshowGridRecipeTextComponent slideshow;

    public RecipeGridButton(ICoreClientAPI api, SlideshowGridRecipeTextComponent slideshow)
        : base(api, 0, "#", "Fetch all the ingredients", -1.0, -5.0)
    {
        this.slideshow = slideshow;
        Float = EnumFloat.Inline;
        VerticalAlign = EnumVerticalAlign.FixedOffset;
    }

    protected override void OnClick()
    {
        api.Gui.PlaySound("menubutton_press");

        try
        {
            if (slideshow == null)
            {
                if (ShowCraftableSystem.DebugEnabled)
                {
                    api.ShowChatMessage("[ShowCraftable] RecipeGridButton.OnClick: open a handbook recipe with a crafting grid first.");
                }
                return;
            }

            var variants = GetVariants(slideshow).ToList();
            if (variants.Count == 0)
            {
                if (ShowCraftableSystem.DebugEnabled)
                {
                    api.ShowChatMessage("[ShowCraftable] RecipeGridButton.OnClick: no recipe variants found for this grid.");
                }
                return;
            }

            var rendered = new HashSet<string>(StringComparer.Ordinal);
            var lines = new List<string>();

            for (int i = 0; i < variants.Count; i++)
            {
                var (recipe, unnamed) = variants[i];
                string summary = SummarizeRecipe(recipe, unnamed);

                if (string.IsNullOrWhiteSpace(summary)) continue;
                if (!rendered.Add(summary)) continue; 

                if (variants.Count > 1)
                    lines.Add($"[ShowCraftable] Required (variant {lines.Count + 1}/{variants.Count}): {summary}");
                else
                    lines.Add($"[ShowCraftable] Required: {summary}");
            }

            if (lines.Count == 0)
            {
                if (ShowCraftableSystem.DebugEnabled)
                {
                    api.ShowChatMessage("[ShowCraftable] RecipeGridButton.OnClick: could not summarize ingredients for this recipe.");
                }
                return;
            }

            if (ShowCraftableSystem.DebugEnabled)
            {
                foreach (var line in lines)
                    api.ShowChatMessage(line);
            }

            bool guardAcquired = false;
            bool requestSent = false;
            try
            {
                ShowCraftableSystem.AcquireHandbookPauseGuard(api);
                guardAcquired = true;

                var reqVariants = BuildIngredientLists(variants);
                if (reqVariants.Count > 0)
                {
                    var req = new CraftScanRequest
                    {
                        Radius = 12,
                        IncludeCrates = true,
                        CollectItems = true,
                        Variants = reqVariants
                    };

                    api.Network.GetChannel(ShowCraftableSystem.ChannelName).SendPacket(req);
                    requestSent = true;
                }
            }
            catch (Exception e)
            {
                if (guardAcquired)
                {
                    ShowCraftableSystem.ReleaseHandbookPauseGuard(api);
                    guardAcquired = false;
                }
                if (ShowCraftableSystem.DebugEnabled)
                {
                    api.ShowChatMessage("[ShowCraftable] RecipeGridButton.OnClick: fetch request failed: " + e.Message);
                }
            }
            finally
            {
                if (guardAcquired && !requestSent)
                {
                    ShowCraftableSystem.ReleaseHandbookPauseGuard(api);
                }
            }
        }
        catch (Exception e)
        {
            if (ShowCraftableSystem.DebugEnabled)
            {
                api.ShowChatMessage("[ShowCraftable] RecipeGridButton.OnClick: ingredient listing failed: " + e.Message);
            }
        }
    }

    public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
    {
        var result = base.CalcBounds(flowPath, currentLineHeight, offsetX, lineY, out nextOffsetX);
        var r = BoundsPerLine[0];
        r.Y += GuiElement.scaled(YManualOffset);
        BoundsPerLine[0] = r;
        return result;
    }

    private static IEnumerable<string> FlattenAllowed(object allowedObj)
    {
        if (allowedObj == null) yield break;

        if (allowedObj is IEnumerable<string> es)
        {
            foreach (var s in es) if (!string.IsNullOrEmpty(s)) yield return s;
            yield break;
        }

        var t = allowedObj.GetType();
        var keysProp = t.GetProperty("Keys");
        var indexer = t.GetProperty("Item");
        if (keysProp == null || indexer == null) yield break;

        if (keysProp.GetValue(allowedObj) is IEnumerable keys)
        {
            foreach (var k in keys)
            {
                var arr = indexer.GetValue(allowedObj, new object[] { k }) as string[];
                if (arr == null) continue;
                foreach (var s in arr) if (!string.IsNullOrEmpty(s)) yield return s;
            }
        }
    }

    private static IEnumerable<(GridRecipe recipe, Dictionary<int, ItemStack[]> unnamed)> GetVariants(SlideshowGridRecipeTextComponent slide)
    {
        var t = slide.GetType();
        var fi = t.GetField("GridRecipesAndUnIn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var arr = fi?.GetValue(slide) as Array;
        if (arr == null) yield break;

        for (int i = 0; i < arr.Length; i++)
        {
            var item = arr.GetValue(i);
            if (item == null) continue;

            var it = item.GetType();
            var recipeObj = GetMember(it, item, "Recipe") as GridRecipe;
            var unnamedObj = GetMember(it, item, "unnamedIngredients");

            yield return (recipeObj, ConvertUnnamedDict(unnamedObj));
        }
    }

    private static object GetMember(Type t, object obj, string name)
    {
        if (t == null || obj == null) return null;
        var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (pi != null) return pi.GetValue(obj);
        var fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (fi != null) return fi.GetValue(obj);
        return null;
    }

    private static Dictionary<int, ItemStack[]> ConvertUnnamedDict(object genericDict)
    {
        var res = new Dictionary<int, ItemStack[]>();
        if (genericDict == null) return res;

        var t = genericDict.GetType();
        var keysProp = t.GetProperty("Keys");
        var indexer = t.GetProperty("Item"); 
        if (keysProp == null || indexer == null) return res;

        if (keysProp.GetValue(genericDict) is IEnumerable keysEnum)
        {
            foreach (var k in keysEnum)
            {
                int key = (k is int i) ? i : Convert.ToInt32(k);
                var val = indexer.GetValue(genericDict, new object[] { k }) as ItemStack[];
                if (val != null) res[key] = val;
            }
        }
        return res;
    }

    private static List<CraftIngredientList> BuildIngredientLists(List<(GridRecipe recipe, Dictionary<int, ItemStack[]> unnamed)> variants)
    {
        var result = new List<CraftIngredientList>();
        foreach (var (recipe, unnamed) in variants)
        {
            if (recipe?.resolvedIngredients == null || recipe.resolvedIngredients.Length == 0) continue;

            var list = new CraftIngredientList();

            for (int idx = 0; idx < recipe.resolvedIngredients.Length; idx++)
            {
                var ing = recipe.resolvedIngredients[idx];
                if (ing == null) continue;

                bool isWild = TryGetBool(ing, "IsWildCard");
                int qty = isWild ? TryGetInt(ing, "Quantity", 1)
                                  : Math.Max(1, TryGetStack(ing, "ResolvedItemstack")?.StackSize ?? 1);

                var ci = new CraftIngredient { IsWildcard = isWild, Quantity = qty };

                if (unnamed != null && unnamed.TryGetValue(idx, out var options) && options != null)
                {
                    foreach (var opt in options)
                    {
                        var code = opt?.Collectible?.Code?.ToString();
                        if (!string.IsNullOrEmpty(code) && !ci.Codes.Contains(code)) ci.Codes.Add(code);
                    }
                }
                else
                {
                    var st = TryGetStack(ing, "ResolvedItemstack");
                    var code = st?.Collectible?.Code?.ToString();
                    if (!string.IsNullOrEmpty(code) && !ci.Codes.Contains(code)) ci.Codes.Add(code);
                }

                if (isWild)
                {
                    var pattern = TryGetAssetLocation(ing, "Code");
                    if (pattern != null) ci.PatternCode = pattern.ToString();

                    var it = ing.GetType();
                    var allowedObj = GetMember(it, ing, "AllowedVariants");
                    var allowed = new HashSet<string>(FlattenAllowed(allowedObj), StringComparer.OrdinalIgnoreCase);
                    if (allowed.Count > 0) ci.Allowed.AddRange(allowed);

                    var typeObj = GetMember(it, ing, "Type");
                    if (typeObj is EnumItemClass cls)
                    {
                        ci.Type = cls;
                        ci.HasType = true;
                    }
                }

                list.Ingredients.Add(ci);
            }

            if (list.Ingredients.Count > 0) result.Add(list);
        }

        return result;
    }

    private string SummarizeRecipe(GridRecipe recipe, Dictionary<int, ItemStack[]> unnamed)
    {
        if (recipe == null || recipe.resolvedIngredients == null || recipe.resolvedIngredients.Length == 0)
            return null;

        var items = new List<(string label, int qty)>();

        for (int idx = 0; idx < recipe.resolvedIngredients.Length; idx++)
        {
            var ing = recipe.resolvedIngredients[idx];
            if (ing == null) continue;

            bool isTool = TryGetBool(ing, "IsTool");

            bool isWild = TryGetBool(ing, "IsWildCard");

            int qty = isWild
                ? TryGetInt(ing, "Quantity", 1)
                : Math.Max(1, TryGetStack(ing, "ResolvedItemstack")?.StackSize ?? 1);

            string label;

            if (!isWild)
            {
                var st = TryGetStack(ing, "ResolvedItemstack");
                label = LabelFromStack(st, includeVariant: true, pluralize: qty > 1);
            }
            else
            {
                var pattern = TryGetAssetLocation(ing, "Code");
                if (pattern != null)
                {
                    string baseName = BaseNameFromPattern(pattern);
                    label = WildcardLabel(baseName, qty);
                }
                else if (unnamed != null && unnamed.TryGetValue(idx, out var options) && options != null && options.Length > 0)
                {
                    string baseName = BaseNameFromStack(options[0]);
                    label = WildcardLabel(baseName, qty);
                }
                else
                {
                    label = WildcardLabel(null, qty);
                }
            }

            if (isTool) label += " (tool)";

            items.Add((label, qty));
        }

        var merged = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (label, qty) in items)
        {
            if (string.IsNullOrWhiteSpace(label)) continue;
            merged[label] = merged.TryGetValue(label, out var cur) ? cur + qty : qty;
        }

        var parts = merged.Select(kv => $"{kv.Value} {kv.Key}").ToList();
        return JoinWithCommasAndAnd(parts);
    }

    private static bool TryGetBool(object obj, string name)
    {
        var t = obj.GetType();
        var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (pi?.PropertyType == typeof(bool)) return (bool)pi.GetValue(obj);
        var fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (fi?.FieldType == typeof(bool)) return (bool)fi.GetValue(obj);
        return false;
    }

    private static int TryGetInt(object obj, string name, int def = 0)
    {
        var t = obj.GetType();
        var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (pi?.PropertyType == typeof(int)) return (int)pi.GetValue(obj);
        var fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (fi?.FieldType == typeof(int)) return (int)fi.GetValue(obj);
        return def;
    }

    private static ItemStack TryGetStack(object obj, string name)
    {
        var t = obj.GetType();
        var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (pi != null) return pi.GetValue(obj) as ItemStack;
        var fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (fi != null) return fi.GetValue(obj) as ItemStack;
        return null;
    }

    private static AssetLocation TryGetAssetLocation(object obj, string name)
    {
        var t = obj.GetType();
        var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (pi != null) return pi.GetValue(obj) as AssetLocation;
        var fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (fi != null) return fi.GetValue(obj) as AssetLocation;
        return null;
    }

    private string LabelFromStack(ItemStack st, bool includeVariant, bool pluralize)
    {
        if (st?.Collectible?.Code == null) return "item";
        var path = st.Collectible.Code.Path;

        var (baseName, variant) = SplitBaseAndVariant(path);
        if (pluralize) baseName = Pluralize(baseName);

        if (includeVariant && !string.IsNullOrEmpty(variant))
            return $"{baseName} ({variant})";

        return baseName;
    }

    private static string BaseNameFromStack(ItemStack st)
    {
        if (st?.Collectible?.Code == null) return "item";
        var path = st.Collectible.Code.Path;
        var (baseName, _) = SplitBaseAndVariant(path);
        return baseName;
    }

    private static string BaseNameFromPattern(AssetLocation pattern)
    {
        if (pattern == null) return "item";
        var p = pattern.Path ?? "";
        p = p.Replace("*", "");
        while (true)
        {
            int s = p.IndexOf('{'); int e = p.IndexOf('}');
            if (s < 0 || e < 0 || e <= s) break;
            p = p.Remove(s, e - s + 1);
        }
        var (baseName, _) = SplitBaseAndVariant(p);
        return string.IsNullOrWhiteSpace(baseName) ? "item" : baseName;
    }

    private static (string Base, string Variant) SplitBaseAndVariant(string codePath)
    {
        if (string.IsNullOrEmpty(codePath)) return ("item", null);

        string norm = codePath.Replace("/", " ").Replace("_", " ").Replace(".", " ");

        int dash = norm.LastIndexOf('-');
        string basePart, variant = null;

        if (dash > 0 && dash < norm.Length - 1)
        {
            basePart = norm.Substring(0, dash);
            variant = norm.Substring(dash + 1);
        }
        else
        {
            basePart = norm;
        }

        basePart = CleanupWords(basePart);
        variant = CleanupWords(variant);

        return (string.IsNullOrWhiteSpace(basePart) ? "item" : basePart, string.IsNullOrWhiteSpace(variant) ? null : variant);
    }

    private static string CleanupWords(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        s = s.Replace("-", " ");
        while (s.Contains("  ")) s = s.Replace("  ", " ");
        return s.Trim();
    }

    private static string Pluralize(string noun)
    {
        if (string.IsNullOrWhiteSpace(noun)) return noun;
        noun = noun.Trim();
        if (noun.EndsWith("s", StringComparison.OrdinalIgnoreCase)) return noun;
        if (noun.EndsWith("y", StringComparison.OrdinalIgnoreCase) && noun.Length > 1 && !"aeiou".Contains(char.ToLowerInvariant(noun[noun.Length - 2])))
            return noun.Substring(0, noun.Length - 1) + "ies";
        return noun + "s";
    }

    private static string WildcardLabel(string baseName, int qty)
    {
        baseName ??= "item";
        if (baseName.Contains(' '))
            return $"{baseName} (any)";
        return "any " + (qty > 1 ? Pluralize(baseName) : baseName);
    }

    private static string JoinWithCommasAndAnd(IList<string> parts)
    {
        if (parts == null || parts.Count == 0) return "";
        if (parts.Count == 1) return parts[0];
        if (parts.Count == 2) return parts[0] + " and " + parts[1];
        var sb = new StringBuilder();
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0) sb.Append(i == parts.Count - 1 ? ", and " : ", ");
            sb.Append(parts[i]);
        }
        return sb.ToString();
    }
}
