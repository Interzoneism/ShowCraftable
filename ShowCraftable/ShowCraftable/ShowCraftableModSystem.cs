// File: ShowCraftableModSystem.cs
// Adds a "Craftable" tab that lists handbook pages for items currently craftable in the 3x3 grid.
// List is built on-demand via .craftablescan and cached as page codes (stable across dialog rebuilds).
//
// Requires: HarmonyLib, VintagestoryAPI.dll, VintagestoryLib.dll, Vintagestory.GameContent.dll

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace ShowCraftable.Features
{
    public class ShowCraftableSystem : ModSystem
    {
        private Harmony _harmony;
        private ICoreClientAPI _capi;

        public const string HarmonyId = "showcraftable.core";
        public const string CraftableCategoryCode = "craftable";

        // Start with inventories + opened containers; you can flip IncludeNearbyContainers later.
        private static bool IncludeNearbyContainers = false;
        private static int ScanRadius = 8;

        // ---- Cache (page codes, not page objects) ----
        private static readonly object CacheLock = new();
        private static List<string> CachedPageCodes = new();   // PageCode strings

        private static bool ScanInProgress = false;

        // --------------- Logging (chat + client/server logger + file) ---------------
        private static void LogEverywhere(ICoreClientAPI capi, string msg, bool toChat = false)
        {
            try { capi.Logger?.Notification(msg); } catch { }
            try { capi.World?.Logger?.Notification(msg); } catch { }
            if (toChat) { try { capi.ShowChatMessage(msg); } catch { } }

            try
            {
                // Prefer api.GetOrCreateDataPath("ShowCraftable") if present
                string basePath = null;
                var m = typeof(ICoreAPI).GetMethod("GetOrCreateDataPath", BindingFlags.Public | BindingFlags.Instance);
                if (m != null)
                {
                    basePath = (string)m.Invoke(capi, new object[] { "ShowCraftable" });
                }
                if (string.IsNullOrEmpty(basePath))
                {
                    // Fallback somewhere writable
                    basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShowCraftable");
                }

                Directory.CreateDirectory(basePath);
                var f = Path.Combine(basePath, "craftable.log");
                File.AppendAllText(f, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {msg}\n");
            }
            catch { }
        }

        // --------------- Lifecycle ---------------

        public override void StartClientSide(ICoreClientAPI capi)
        {
            _capi = capi;

            // Harmony init
            _harmony = new Harmony(HarmonyId);

            // --- Inject "Craftable" tab in Survival Handbook ---
            var tSurv = AccessTools.TypeByName("Vintagestory.GameContent.GuiDialogSurvivalHandbook");
            var miGenTabs = AccessTools.Method(tSurv, "genTabs");
            _harmony.Patch(miGenTabs,
                postfix: new HarmonyMethod(typeof(ShowCraftableSystem), nameof(GenTabs_Postfix)));

            // --- Override FilterItems for our tab (render from our cache) ---
            var tBase = AccessTools.TypeByName("Vintagestory.GameContent.GuiDialogHandbook");
            var miFilter = AccessTools.Method(tBase, "FilterItems");
            _harmony.Patch(miFilter,
                prefix: new HarmonyMethod(typeof(ShowCraftableSystem), nameof(FilterItems_Prefix)));

            // --- When selecting our tab: clear search + forced refresh ---
            var miSelect = AccessTools.Method(tBase, "selectTab");
            _harmony.Patch(miSelect,
                postfix: new HarmonyMethod(typeof(ShowCraftableSystem), nameof(SelectTab_Postfix)));

            // --- When handbook finished loading pages async: auto-refresh if our tab is active ---
            var miLoadAsync = AccessTools.Method(tBase, "LoadPages_Async");
            _harmony.Patch(miLoadAsync,
                postfix: new HarmonyMethod(typeof(ShowCraftableSystem), nameof(AfterPagesLoaded_Postfix)));

            // --- .craftable : open handbook and jump to Craftable tab (no rescan) ---
            capi.ChatCommands.Create("craftable")
                .WithDescription("Open Survival Handbook at the Craftable tab (no rescan)")
                .HandleWith(args =>
                {
                    capi.Event.RegisterCallback(_ => OpenCraftableTab(capi), 10);
                    return TextCommandResult.Success();
                });

            // --- .craftablescan : rebuild cache on-demand (grid recipes only) ---
            capi.ChatCommands.Create("craftablescan")
                .WithDescription("Rebuild the Craftable cache now (grid recipes only)")
                .HandleWith(args =>
                {
                    if (ScanInProgress)
                    {
                        LogEverywhere(capi, "[Craftable] Scan already in progress…", toChat: true);
                        return TextCommandResult.Success();
                    }

                    ScanInProgress = true;
                    var t0 = DateTime.UtcNow;
                    try
                    {
                        int pages = RebuildCache(capi, IncludeNearbyContainers, ScanRadius,
                                                 out int outputsCount, out int fetched, out int usable);
                        var ms = (int)(DateTime.UtcNow - t0).TotalMilliseconds;

                        LogEverywhere(capi,
                            $"[Craftable] Scan done: outputs={outputsCount}, pages={pages}, fetched={fetched}, usable={usable}, {ms} ms",
                            toChat: true);

                        // If dialog already open on our tab: refresh list
                        TryRefreshOpenDialog(capi);
                    }
                    catch (Exception e)
                    {
                        LogEverywhere(capi, $"[Craftable] Scan failed: {e}", toChat: true);
                    }
                    finally
                    {
                        ScanInProgress = false;
                    }

                    return TextCommandResult.Success();
                });

            // --- .craftabledump : dump cache + how many can resolve against current dialog ---
            capi.ChatCommands.Create("craftabledump")
                .WithDescription("Dump Craftable cache & resolution stats")
                .HandleWith(args =>
                {
                    try
                    {
                        List<string> codes;
                        lock (CacheLock) codes = CachedPageCodes.ToList();

                        LogEverywhere(capi,
                            $"[Craftable] dump: cached={codes.Count}, first10=[{string.Join(", ", codes.Take(10))}]",
                            toChat: true);

                        // Count how many of pageCodes currently exist in the dialog's pageNumberByPageCode
                        var msType = AccessTools.TypeByName("Vintagestory.GameContent.ModSystemSurvivalHandbook");
                        var ms = msType != null ? GetModSystemByType(capi, msType) : null;
                        var dlg = AccessTools.Field(msType, "dialog")?.GetValue(ms);
                        var pageMap = dlg != null
                            ? AccessTools.Field(dlg.GetType(), "pageNumberByPageCode")?.GetValue(dlg) as Dictionary<string, int>
                            : null;

                        if (pageMap != null)
                        {
                            int have = 0, miss = 0;
                            foreach (var c in codes) { if (pageMap.ContainsKey(c)) have++; else miss++; }
                            LogEverywhere(capi, $"[Craftable] resolve: haveInMap={have}, missingInMap={miss}", toChat: true);
                        }
                        else
                        {
                            LogEverywhere(capi, "[Craftable] resolve: page map not available (dialog not initialized?)", toChat: true);
                        }
                    }
                    catch (Exception e)
                    {
                        LogEverywhere(capi, $"[Craftable] dump failed: {e}", toChat: true);
                    }

                    return TextCommandResult.Success();
                });

            // --- World finalize: clear cache (avoid stale state between joins) ---
            capi.Event.LevelFinalize += () =>
            {
                lock (CacheLock) CachedPageCodes.Clear();
                LogEverywhere(capi, "[Craftable] LevelFinalize: cache cleared");
            };
        }

        public override void Dispose() => _harmony?.UnpatchAll(HarmonyId);

        // -------------------- Inject the "Craftable" tab --------------------

        public static void GenTabs_Postfix(object __instance, ref object __result, ref int curTab)
        {
            try
            {
                var tabs = ((Array)__result)?.Cast<object>().ToList() ?? new List<object>();
                if (tabs.Count == 0) return;

                // HandbookTab (derived from GuiTab) lives in GameContent
                var tabType = AccessTools.TypeByName("Vintagestory.GameContent.HandbookTab");
                if (tabType == null) return;

                object GetPF(Type t, object o, string name)
                {
                    var pi = AccessTools.Property(t, name);
                    if (pi != null) return pi.GetValue(o);
                    var fi = AccessTools.Field(t, name);
                    return fi?.GetValue(o);
                }
                void SetPF(Type t, object o, string name, object val)
                {
                    var pi = AccessTools.Property(t, name);
                    if (pi != null && pi.CanWrite) { pi.SetValue(o, val); return; }
                    var fi = AccessTools.Field(t, name);
                    if (fi != null) fi.SetValue(o, val);
                }

                // Avoid duplicates
                foreach (var t in tabs)
                {
                    var cat = GetPF(tabType, t, "CategoryCode") as string;
                    if (string.Equals(cat, CraftableCategoryCode, StringComparison.OrdinalIgnoreCase))
                    {
                        __result = ToTypedArray(tabType, tabs);
                        return;
                    }
                }

                var newTab = Activator.CreateInstance(tabType);
                SetPF(tabType, newTab, "Name", "Craftable");
                SetPF(tabType, newTab, "CategoryCode", CraftableCategoryCode);
                SetPF(tabType, newTab, "DataInt", tabs.Count);
                SetPF(tabType, newTab, "PaddingTop", 20.0);

                int insertAt = Math.Min(2, tabs.Count); // after "Everything"
                tabs.Insert(insertAt, newTab);
                __result = ToTypedArray(tabType, tabs);
            }
            catch { }
        }

        private static Array ToTypedArray(Type elementType, List<object> list)
        {
            var arr = Array.CreateInstance(elementType, list.Count);
            for (int i = 0; i < list.Count; i++) arr.SetValue(list[i], i);
            return arr;
        }

        // -------------------- Clear search & refresh when selecting our tab --------------------

        public static void SelectTab_Postfix(object __instance, string code)
        {
            try
            {
                if (!string.Equals(code, CraftableCategoryCode, StringComparison.Ordinal)) return;

                var fiOverview = AccessTools.Field(__instance.GetType(), "overviewGui");
                var composer = fiOverview?.GetValue(__instance);
                if (composer != null)
                {
                    var miGetTextInput = composer.GetType().GetMethod("GetTextInput");
                    var searchInput = miGetTextInput?.Invoke(composer, new object[] { "searchField" });
                    searchInput?.GetType().GetMethod("SetValue")?.Invoke(searchInput, new object[] { "", true });
                }

                var fiSearch = AccessTools.Field(__instance.GetType(), "currentSearchText");
                fiSearch?.SetValue(__instance, null);

                AccessTools.Method(__instance.GetType(), "FilterItems")?.Invoke(__instance, null);
            }
            catch { }
        }

        // -------------------- After handbook pages are loaded async: refresh our tab if active --------------------

        public static void AfterPagesLoaded_Postfix(object __instance)
        {
            try
            {
                var fiCapi = AccessTools.Field(__instance.GetType(), "capi");
                var capi = fiCapi?.GetValue(__instance) as ICoreClientAPI;
                var cur = AccessTools.Field(__instance.GetType(), "currentCatgoryCode")?.GetValue(__instance) as string;

                if (capi != null && string.Equals(cur, CraftableCategoryCode, StringComparison.Ordinal))
                {
                    AccessTools.Method(__instance.GetType(), "FilterItems")?.Invoke(__instance, null);
                    LogEverywhere(capi, "[Craftable] AfterPagesLoaded: refreshed Craftable tab");
                }
            }
            catch { }
        }

        // -------------------- FilterItems override for Craftable tab --------------------

        public static bool FilterItems_Prefix(object __instance)
{
    try
    {
        string cat = (string)AccessTools.Field(__instance.GetType(), "currentCatgoryCode").GetValue(__instance);
        if (!string.Equals(cat, CraftableCategoryCode, StringComparison.Ordinal)) return true;

        var fiCapi       = AccessTools.Field(__instance.GetType(), "capi");
        var fiShown      = AccessTools.Field(__instance.GetType(), "shownHandbookPages");
        var fiOverview   = AccessTools.Field(__instance.GetType(), "overviewGui");
        var fiListHeight = AccessTools.Field(__instance.GetType(), "listHeight");
        var fiSearch     = AccessTools.Field(__instance.GetType(), "currentSearchText");
        var fiLoading    = AccessTools.Field(__instance.GetType(), "loadingPagesAsync");

        var capi     = fiCapi?.GetValue(__instance) as ICoreClientAPI;
        var shown    = fiShown?.GetValue(__instance) as System.Collections.IList;
        var composer = fiOverview?.GetValue(__instance);
        string q     = (string)fiSearch?.GetValue(__instance);
        bool loading = fiLoading != null && (bool)fiLoading.GetValue(__instance);

        if (shown == null || composer == null) return true;

        // Map pagecodes -> actual page objects in this dialog
        var pageMap  = AccessTools.Field(__instance.GetType(), "pageNumberByPageCode")?.GetValue(__instance) as Dictionary<string, int>;
        var allPages = AccessTools.Field(__instance.GetType(), "allHandbookPages")?.GetValue(__instance) as System.Collections.IList;
        if (pageMap == null || allPages == null) return true;

        List<string> codesSnapshot;
        lock (CacheLock) codesSnapshot = CachedPageCodes.ToList();

        var resolvedPages = new List<object>();
        int missing = 0;
        foreach (var code in codesSnapshot)
        {
            if (pageMap.TryGetValue(code, out int idx) && idx >= 0 && idx < allPages.Count)
            {
                var pg = allPages[idx];
                if (pg != null) resolvedPages.Add(pg);
            }
            else missing++;
        }

        if (capi != null)
        {
            var sampleCodes = string.Join(", ", codesSnapshot.Take(5));
            LogEverywhere(capi, $"[Craftable] UI resolve: cached={codesSnapshot.Count}, resolved={resolvedPages.Count}, missing={missing}, loading={loading}, sample codes=[{sampleCodes}]");
        }

        // Search filter
        List<object> finalPages;
        if (!string.IsNullOrWhiteSpace(q))
        {
            string s = q.ToLowerInvariant().Trim();
            var weighted = new List<(object Page, float W)>();
            foreach (var p in resolvedPages)
            {
                var mi = p.GetType().GetMethod("GetTextMatchWeight");
                float w = mi == null ? 0f : (float)mi.Invoke(p, new object[] { s });
                if (w > 0) weighted.Add((p, w));
            }
            finalPages = weighted.OrderByDescending(x => x.W).Select(x => x.Page).ToList();
        }
        else
        {
            finalPages = resolvedPages;
        }

        // Viktigt: säkerställ att varje sida är Visible=true (FlatList ritar bara Visible)
        foreach (var p in finalPages)
        {
            var visProp = p.GetType().GetProperty("Visible", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            try { visProp?.SetValue(p, true); } catch { }
        }

        // Fyll listan (mutera befintlig IList)
        shown.Clear();
        foreach (var p in finalPages) shown.Add(p);

        // Rebind + resize + log
        TryBindAndResizeList(capi, composer, shown, fiListHeight?.GetValue(__instance) as double? ?? (double)(fiListHeight?.GetValue(__instance) ?? 500d));

        return false;
    }
    catch
    {
        return true;
    }
}

private static void TryBindAndResizeList(ICoreClientAPI capi, object composer, System.Collections.IList shown, double listHeight)
{
    try
    {
        if (composer == null) return;

        // Hämta flatlist via GetFlatList(...) eller fallback GetElement(...)
        object stacklist = null;

        var miGetFlat = composer.GetType().GetMethod("GetFlatList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (miGetFlat != null)
        {
            stacklist = miGetFlat.Invoke(composer, new object[] { "stacklist" });
        }
        if (stacklist == null)
        {
            var miGetElem = composer.GetType().GetMethod("GetElement", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            stacklist = miGetElem?.Invoke(composer, new object[] { "stacklist" });
        }
        if (stacklist == null)
        {
            LogEverywhere(capi, "[Craftable] UI: could not obtain stacklist element");
            return;
        }

        // Säkerställ att FlatList.Elements refererar till 'shown'
        var fiElements = stacklist.GetType().GetField("Elements", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var current = fiElements?.GetValue(stacklist) as System.Collections.IList;

        if (fiElements != null && !object.ReferenceEquals(current, shown))
        {
            fiElements.SetValue(stacklist, shown);
            LogEverywhere(capi, "[Craftable] UI: rebound FlatList.Elements to shownHandbookPages");
        }

        // Räkna om totalhöjd
        stacklist.GetType().GetMethod("CalcTotalHeight", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                 ?.Invoke(stacklist, Array.Empty<object>());

        // Scrollbar heights (som vanilla)
        var miGetScrollbar = composer.GetType().GetMethod("GetScrollbar", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var scrollbar = miGetScrollbar?.Invoke(composer, new object[] { "scrollbar" });

        double total = 0d;
        var insideBounds = stacklist.GetType().GetField("insideBounds", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(stacklist);
        if (insideBounds != null)
        {
            var fiFixedH = insideBounds.GetType().GetField("fixedHeight", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fiFixedH != null) total = Convert.ToDouble(fiFixedH.GetValue(insideBounds));
        }

        scrollbar?.GetType().GetMethod("SetHeights", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                 ?.Invoke(scrollbar, new object[] { (float)listHeight, (float)total });

        // Debug: hur många synliga ritas?
        int visible = 0;
        foreach (var e in shown)
        {
            var vis = e?.GetType().GetProperty("Visible", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(e) as bool?;
            if (vis == true) visible++;
        }
        LogEverywhere(capi, $"[Craftable] UI: shown.Count={shown.Count}, visible={visible}, totalHeight={total:F1}");
    }
    catch (Exception e)
    {
        LogEverywhere(capi, $"[Craftable] UI bind/resize error: {e}");
    }
}



        // -------------------- Dialog helpers --------------------

        private static void OpenCraftableTab(ICoreClientAPI capi)
        {
            try
            {
                var msType = AccessTools.TypeByName("Vintagestory.GameContent.ModSystemSurvivalHandbook");
                if (msType == null) return;
                var ms = GetModSystemByType(capi, msType);
                if (ms == null) { capi.Event.RegisterCallback(_ => OpenCraftableTab(capi), 100); return; }
                var fiDialog = AccessTools.Field(msType, "dialog");
                var dlg = fiDialog?.GetValue(ms);
                if (dlg == null) { capi.Event.RegisterCallback(_ => OpenCraftableTab(capi), 100); return; }
                dlg.GetType().GetMethod("TryOpen")?.Invoke(dlg, null);
                dlg.GetType().GetMethod("selectTab")?.Invoke(dlg, new object[] { CraftableCategoryCode });
            }
            catch { }
        }

        private static void TryRefreshOpenDialog(ICoreClientAPI capi)
        {
            try
            {
                var msType = AccessTools.TypeByName("Vintagestory.GameContent.ModSystemSurvivalHandbook");
                var ms = msType != null ? GetModSystemByType(capi, msType) : null;
                if (ms == null) return;
                var fiDialog = AccessTools.Field(msType, "dialog");
                var dlg = fiDialog?.GetValue(ms);
                if (dlg == null) return;
                var cur = AccessTools.Field(dlg.GetType(), "currentCatgoryCode")?.GetValue(dlg) as string;
                if (!string.Equals(cur, CraftableCategoryCode, StringComparison.Ordinal)) return;
                AccessTools.Method(dlg.GetType(), "FilterItems")?.Invoke(dlg, null);
            }
            catch { }
        }

        private static object GetModSystemByType(ICoreClientAPI capi, Type msType)
        {
            var loader = capi.ModLoader;
            var t = loader.GetType();
            var gen = t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                       .FirstOrDefault(m => m.Name == "GetModSystem" && m.IsGenericMethodDefinition);
            if (gen != null)
            {
                try
                {
                    var gm = gen.MakeGenericMethod(msType);
                    var pars = gm.GetParameters();
                    return pars.Length == 1 && pars[0].ParameterType == typeof(bool)
                        ? gm.Invoke(loader, new object[] { true })
                        : gm.Invoke(loader, null);
                }
                catch { }
            }
            var byType = t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                          .FirstOrDefault(m => m.Name == "GetModSystem" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(Type));
            if (byType != null) { try { return byType.Invoke(loader, new object[] { msType }); } catch { } }
            return null;
        }

        // -------------------- Cache rebuild (on-demand) --------------------

        // Build map: "domain:path" -> PageCode using the SurvivalHandbook's allstacks
        private static Dictionary<string, string> BuildPageCodeMapFromAllStacks(ICoreClientAPI capi)
        {
            var map = new Dictionary<string, string>();
            try
            {
                var msType = AccessTools.TypeByName("Vintagestory.GameContent.ModSystemSurvivalHandbook");
                var ms = msType != null ? GetModSystemByType(capi, msType) : null;
                if (ms == null) return map;

                var fiAllStacks = AccessTools.Field(msType, "allstacks");
                var arr = fiAllStacks?.GetValue(ms) as ItemStack[];
                if (arr == null || arr.Length == 0) return map;

                var ghType = AccessTools.TypeByName("Vintagestory.GameContent.GuiHandbookItemStackPage");
                var miPageCodeForStack = ghType?.GetMethod("PageCodeForStack", BindingFlags.Public | BindingFlags.Static);
                if (miPageCodeForStack == null) return map;

                foreach (var s in arr)
                {
                    if (s?.Collectible?.Code == null) continue;
                    var code = s.Collectible.Code.ToString(); // "domain:path"
                    if (string.IsNullOrEmpty(code)) continue;

                    // Compute the exact page code vanilla uses
                    var pc = miPageCodeForStack.Invoke(null, new object[] { s }) as string;
                    if (string.IsNullOrEmpty(pc)) continue;

                    // First one wins (some codes may appear multiple times as variants)
                    if (!map.ContainsKey(code)) map[code] = pc;
                }
            }
            catch { }
            return map;
        }

        private static ItemStack MakeStackFromCode(ICoreClientAPI capi, string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            var loc = new AssetLocation(code);
            var item = capi.World.GetItem(loc);
            if (item != null) return new ItemStack(item, 1);
            var block = capi.World.GetBlock(loc);
            if (block != null) return new ItemStack(block, 1);
            return null;
        }

        private static int RebuildCache(ICoreClientAPI capi, bool includeNearby, int radius,
                                        out int craftableOutputsCount, out int fetched, out int usable)
        {
            craftableOutputsCount = 0; fetched = 0; usable = 0;

            // Access current handbook dialog internals
            var msType = AccessTools.TypeByName("Vintagestory.GameContent.ModSystemSurvivalHandbook");
            var ms = msType != null ? GetModSystemByType(capi, msType) : null;
            if (ms == null) throw new InvalidOperationException("SurvivalHandbook system not found.");

            var fiDialog = AccessTools.Field(msType, "dialog");
            var dlg = fiDialog?.GetValue(ms);
            if (dlg == null) throw new InvalidOperationException("SurvivalHandbook dialog not initialized.");

            var fiAllPages = AccessTools.Field(dlg.GetType(), "allHandbookPages");
            var allPages = fiAllPages?.GetValue(dlg) as System.Collections.IList;
            if (allPages == null) throw new InvalidOperationException("Could not access handbook pages.");

            var fiPageMap = AccessTools.Field(dlg.GetType(), "pageNumberByPageCode");
            var pageMap = fiPageMap?.GetValue(dlg) as Dictionary<string, int>;
            if (pageMap == null) throw new InvalidOperationException("Could not access pageNumberByPageCode.");

            // Build resource pool
            var pool = BuildResourcePool(capi, includeNearby, radius);

            // Evaluate craftable outputs (as collectible codes)
            var recipes = GetAllGridRecipes(capi, out fetched, out usable);
            var craftableCodes = new HashSet<string>();
            foreach (var r in recipes)
            {
                if (IsCraftable(r, pool))
                {
                    foreach (var os in r.Outputs)
                    {
                        var code = os?.Collectible?.Code?.ToString();
                        if (!string.IsNullOrEmpty(code)) craftableCodes.Add(code);
                    }
                }
            }

            craftableOutputsCount = craftableCodes.Count;
            LogEverywhere(capi, $"[Craftable] craftable outputs={craftableOutputsCount}");

            // Build code->pagecode map from allstacks and translate craftable codes to page codes
            var code2page = BuildPageCodeMapFromAllStacks(capi);
            var resultPageCodes = new HashSet<string>();
            int matchedCodes = 0;

            foreach (var code in craftableCodes)
            {
                if (code2page.TryGetValue(code, out var pageCode))
                {
                    matchedCodes++;
                    resultPageCodes.Add(pageCode);
                }
            }

            // Fallback: if nothing matched (e.g., very exotic outputs), try the direct PageCodeForStack route
            if (resultPageCodes.Count == 0)
            {
                var ghType = AccessTools.TypeByName("Vintagestory.GameContent.GuiHandbookItemStackPage");
                var miPageCodeForStack = ghType?.GetMethod("PageCodeForStack", BindingFlags.Public | BindingFlags.Static);
                if (miPageCodeForStack != null)
                {
                    foreach (var code in craftableCodes)
                    {
                        var stack = MakeStackFromCode(capi, code);
                        if (stack == null) continue;
                        var pc = miPageCodeForStack.Invoke(null, new object[] { stack }) as string;
                        if (!string.IsNullOrEmpty(pc)) resultPageCodes.Add(pc);
                    }
                }
            }

            // Persist cache
            lock (CacheLock) CachedPageCodes = resultPageCodes.ToList();

            // Diagnostic
            var matched = resultPageCodes.Count;
            var sampleOut = string.Join(", ", craftableCodes.Take(6));
            var samplePages = string.Join(", ", resultPageCodes.Take(6));
            LogEverywhere(capi, $"[Craftable] craftable outputs={craftableOutputsCount}, pagesFromMap={matched}, sample outputs=[{sampleOut}], sample pagecodes=[{samplePages}]", toChat: true);

            return resultPageCodes.Count;
        }

        // -------------------- Resource pool --------------------

        private struct Key : IEquatable<Key>
        {
            public string Code;
            public bool Equals(Key other) => Code == other.Code;
            public override bool Equals(object obj) => obj is Key k && Equals(k);
            public override int GetHashCode() => Code?.GetHashCode() ?? 0;
        }

        private sealed class ResourcePool
        {
            public readonly Dictionary<Key, int> Counts = new();
            public readonly Dictionary<Key, EnumItemClass> Classes = new();

            public void Add(ItemStack stack)
            {
                if (stack == null || stack.Collectible == null) return;
                var code = stack.Collectible.Code?.ToString();
                if (string.IsNullOrEmpty(code)) return;
                var k = new Key { Code = code };
                int q = Math.Max(1, stack.StackSize);
                Counts[k] = Counts.TryGetValue(k, out var cur) ? cur + q : q;
                if (!Classes.ContainsKey(k)) Classes[k] = stack.Class;
            }

            public bool TryConsumeAny(IEnumerable<ItemStack> options, int quantity, bool consume)
            {
                if (options == null) return false;
                foreach (var opt in options)
                {
                    var code = opt?.Collectible?.Code?.ToString();
                    if (string.IsNullOrEmpty(code)) continue;
                    var k = new Key { Code = code };
                    if (Counts.TryGetValue(k, out int have) && have >= quantity)
                    {
                        if (consume)
                        {
                            have -= quantity;
                            if (have <= 0) { Counts.Remove(k); Classes.Remove(k); }
                            else Counts[k] = have;
                        }
                        return true;
                    }
                }
                return false;
            }

            public bool HasAny(IEnumerable<ItemStack> options)
            {
                if (options == null) return false;
                foreach (var opt in options)
                {
                    var code = opt?.Collectible?.Code?.ToString();
                    if (string.IsNullOrEmpty(code)) continue;
                    if (Counts.ContainsKey(new Key { Code = code })) return true;
                }
                return false;
            }

            public bool TryConsumeWildcard(EnumItemClass type, AssetLocation pattern, Dictionary<string, string[]> allowed, int quantity, bool consume)
            {
                if (pattern == null) return false;

                foreach (var kv in Counts.ToArray())
                {
                    var k = kv.Key;
                    if (!Classes.TryGetValue(k, out var cls) || cls != type) continue;

                    var code = new AssetLocation(k.Code);
                    if (!WildcardMatch(pattern, code, allowed)) continue;

                    if (kv.Value >= quantity)
                    {
                        if (consume)
                        {
                            int left = kv.Value - quantity;
                            if (left <= 0) { Counts.Remove(k); Classes.Remove(k); }
                            else Counts[k] = left;
                        }
                        return true;
                    }
                }
                return false;
            }
        }

        private static void TryAddInventoryFromBE(BlockEntity be, ResourcePool pool)
        {
            if (be == null) return;
            var pi = be.GetType().GetProperty("Inventory");
            var invObj = pi?.GetValue(be) ?? be.GetType().GetField("Inventory")?.GetValue(be);
            if (invObj is IInventory inv)
            {
                foreach (var slot in inv) if (slot?.Itemstack != null) pool.Add(slot.Itemstack);
            }
        }

        private static ResourcePool BuildResourcePool(ICoreClientAPI capi, bool includeNearby, int radius)
        {
            var pool = new ResourcePool();
            var mgr = capi.World?.Player?.InventoryManager;

            if (mgr != null)
            {
                void AddInv(IInventory inv)
                {
                    if (inv == null) return;
                    foreach (var slot in inv) if (slot?.Itemstack != null) pool.Add(slot.Itemstack);
                }
                AddInv(mgr.GetOwnInventory("craftinggrid"));
                AddInv(mgr.GetOwnInventory("backpack"));
                AddInv(mgr.GetHotbarInventory());

                // Opened containers
                foreach (var inv in mgr.OpenedInventories)
                {
                    if (inv is InventoryGeneric gen)
                        foreach (var slot in gen) if (slot?.Itemstack != null) pool.Add(slot.Itemstack);
                }
            }

            if (includeNearby)
            {
                var pos = capi.World?.Player?.Entity?.ServerPos.AsBlockPos ?? capi.World?.Player?.Entity?.Pos.AsBlockPos;
                if (pos != null)
                {
                    BlockPos bp = pos;
                    int r = Math.Max(0, radius);
                    for (int dx = -r; dx <= r; dx++)
                        for (int dy = -1; dy <= 2; dy++)
                            for (int dz = -r; dz <= r; dz++)
                            {
                                var be = capi.World.BlockAccessor.GetBlockEntity(bp.AddCopy(dx, dy, dz));
                                TryAddInventoryFromBE(be, pool);
                            }
                }
            }

            return pool;
        }

        // -------------------- Recipes (grid) --------------------

        private sealed class GridRecipeShim
        {
            public object Raw;
            public List<GridIngredientShim> Ingredients = new();
            public List<ItemStack> Outputs = new();
        }

        private sealed class GridIngredientShim
        {
            public bool IsTool;
            public bool IsWild;
            public int QuantityRequired;                  // Wild: Quantity, Exact: ResolvedItemstack.StackSize
            public List<ItemStack> Options = new();       // for exact matches
            public AssetLocation PatternCode;             // ingredient.Code
            public Dictionary<string, string[]> Allowed;  // ingredient.AllowedVariants
            public EnumItemClass Type;                    // Item/Block
        }

        private static bool IsCraftable(GridRecipeShim r, ResourcePool pool)
        {
            var tmp = new ResourcePool();
            foreach (var kv in pool.Counts) tmp.Counts[kv.Key] = kv.Value;
            foreach (var kv in pool.Classes) tmp.Classes[kv.Key] = kv.Value;

            // Tools: presence only
            foreach (var ing in r.Ingredients)
            {
                if (!ing.IsTool) continue;
                if (ing.IsWild)
                {
                    if (!tmp.TryConsumeWildcard(ing.Type, ing.PatternCode, ing.Allowed, 1, consume: false))
                        return false;
                }
                else
                {
                    if (!tmp.HasAny(ing.Options)) return false;
                }
            }
            // Consumables
            foreach (var ing in r.Ingredients)
            {
                if (ing.IsTool) continue;
                int need = Math.Max(1, ing.QuantityRequired);
                bool ok = ing.IsWild
                    ? tmp.TryConsumeWildcard(ing.Type, ing.PatternCode, ing.Allowed, need, consume: true)
                    : tmp.TryConsumeAny(ing.Options, need, consume: true);
                if (!ok) return false;
            }
            return true;
        }

        private static List<GridRecipeShim> GetAllGridRecipes(ICoreClientAPI capi, out int fetched, out int usable)
        {
            var list = new List<GridRecipeShim>();
            fetched = 0; usable = 0;

            var world = capi.World;
            IEnumerable<object> rawRecipes = Enumerable.Empty<object>();

            var pi = world.GetType().GetProperty("GridRecipes", BindingFlags.Public | BindingFlags.Instance);
            var fi = world.GetType().GetField("GridRecipes", BindingFlags.Public | BindingFlags.Instance);
            object val = pi?.GetValue(world) ?? fi?.GetValue(world);
            if (val is System.Collections.IEnumerable en) rawRecipes = en.Cast<object>();
            else rawRecipes = FetchGridRecipesMulti(capi);

            foreach (var raw in rawRecipes)
            {
                fetched++;
                var shim = TryBuildGridShimResolving(raw, capi);
                if (shim != null && shim.Outputs.Count > 0)
                {
                    usable++;
                    list.Add(shim);
                }
            }

            LogEverywhere(capi, $"[Craftable] Grid recipes fetched={fetched}, usable={usable}");
            return list;
        }

        private static IEnumerable<object> FetchGridRecipesMulti(ICoreClientAPI capi)
        {
            var w = capi.World;
            var sources = new List<object>();

            foreach (var host in new object[] { capi, w, w?.Api })
            {
                var mi = host?.GetType().GetMethod("GetGridRecipes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (mi != null && mi.GetParameters().Length == 0)
                {
                    try { var res = mi.Invoke(host, null); if (res is System.Collections.IEnumerable en) sources.Add(en); } catch { }
                }
            }

            foreach (var (host, name) in new (object host, string name)[] { (w, "CraftingRecipes"), (w?.Api, "CraftingRecipes") })
            {
                var v = TryGetMember(host?.GetType(), host, name);
                if (v is System.Collections.IEnumerable en) sources.Add(en);
            }

            var rr = TryGetMember(w?.GetType(), w, "RecipeRegistry") ?? TryGetMember(w?.Api?.GetType(), w?.Api, "RecipeRegistry");
            if (rr != null)
            {
                var grids = TryGetMember(rr.GetType(), rr, "GridRecipes");
                if (grids is System.Collections.IEnumerable en) sources.Add(en);
            }

            var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (var src in sources)
            {
                if (src is System.Collections.IEnumerable en)
                    foreach (var r in en) if (r != null && seen.Add(r)) yield return r;
            }
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        private static GridRecipeShim TryBuildGridShimResolving(object raw, ICoreClientAPI capi)
        {
            if (raw == null) return null;
            var t = raw.GetType();
            if (!t.Name.Contains("GridRecipe", StringComparison.OrdinalIgnoreCase)) return null;

            EnsureRecipeResolved(raw, t, capi);

            var shim = new GridRecipeShim { Raw = raw };

            var resolvedIngreds = TryGetMember(t, raw, "resolvedIngredients");
            if (resolvedIngreds is System.Collections.IEnumerable ien)
            {
                foreach (var ingRaw in ien)
                {
                    if (ingRaw == null) continue;
                    var it = ingRaw.GetType();
                    var gi = new GridIngredientShim();

                    gi.IsTool = TryGetMember(it, ingRaw, "IsTool") as bool? ?? false;
                    gi.IsWild = TryGetMember(it, ingRaw, "IsWildCard") as bool? ?? false;

                    gi.PatternCode = TryGetMember(it, ingRaw, "Code") as AssetLocation;
                    gi.Allowed = TryGetMember(it, ingRaw, "AllowedVariants") as Dictionary<string, string[]>;
                    gi.Type = (EnumItemClass)(TryGetMember(it, ingRaw, "Type") as EnumItemClass? ?? EnumItemClass.Item);

                    var resolvedStack = TryGetMember(it, ingRaw, "ResolvedItemstack") as ItemStack;
                    var resolvedList = TryGetMember(it, ingRaw, "ResolvedItemstacks") as System.Collections.IEnumerable;

                    if (gi.IsWild)
                    {
                        gi.QuantityRequired = TryGetMember(it, ingRaw, "Quantity") as int? ?? 1;
                    }
                    else
                    {
                        if (resolvedStack != null) gi.Options.Add(resolvedStack);
                        else if (resolvedList != null) foreach (var s in resolvedList) if (s is ItemStack st) gi.Options.Add(st);

                        int qty = 1;
                        if (resolvedStack != null) qty = Math.Max(1, resolvedStack.StackSize);
                        else if (gi.Options.Count > 0) qty = Math.Max(1, gi.Options[0].StackSize);
                        gi.QuantityRequired = qty;
                    }

                    if (gi.IsWild || gi.Options.Count > 0) shim.Ingredients.Add(gi);
                }
            }

            // Output via CraftingRecipeIngredient.ResolvedItemstack
            var outputIng = TryGetMember(t, raw, "Output");
            if (outputIng != null)
            {
                var outStack = TryGetMember(outputIng.GetType(), outputIng, "ResolvedItemstack") as ItemStack;
                if (outStack != null && outStack.Collectible != null) shim.Outputs.Add(outStack);
            }

            var outs = TryGetMember(t, raw, "ResolvedOutputs") ?? TryGetMember(t, raw, "Outputs");
            if (outs is System.Collections.IEnumerable outEnum)
                foreach (var o in outEnum)
                    if (o is ItemStack os && os.Collectible != null) shim.Outputs.Add(os);

            return shim;
        }

        private static void EnsureRecipeResolved(object raw, Type t, ICoreClientAPI capi)
        {
            var mi = t.GetMethod("ResolveIngredients", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi != null)
            {
                try
                {
                    var pars = mi.GetParameters();
                    if (pars.Length == 1)
                    {
                        var p0 = pars[0].ParameterType;
                        if (p0.IsInstanceOfType(capi.World)) { mi.Invoke(raw, new object[] { capi.World }); return; }
                        if (p0.IsInstanceOfType(capi.World?.Api)) { mi.Invoke(raw, new object[] { capi.World.Api }); return; }
                        if (p0.IsInstanceOfType(capi)) { mi.Invoke(raw, new object[] { capi }); return; }
                    }
                }
                catch { }
            }
        }

        private static object TryGetMember(object obj, string name)
        {
            if (obj == null) return null;
            return TryGetMember(obj.GetType(), obj, name);
        }

        private static object TryGetMember(Type t, object obj, string name)
        {
            if (t == null || obj == null) return null;
            var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (pi != null) return pi.GetValue(obj);
            var fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi != null) return fi.GetValue(obj);
            return null;
        }

        // -------------------- Wildcard matching helper (API differences safe) --------------------

        private static bool WildcardMatch(AssetLocation pattern, AssetLocation code, Dictionary<string, string[]> allowed)
        {
            var methods = typeof(WildcardUtil).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                              .Where(m => m.Name == "Match");
            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (ps.Length == 3 &&
                    ps[0].ParameterType == typeof(AssetLocation) &&
                    ps[1].ParameterType == typeof(AssetLocation) &&
                    ps[2].ParameterType == typeof(Dictionary<string, string[]>))
                {
                    return (bool)m.Invoke(null, new object[] { pattern, code, allowed });
                }
            }

            var mi2 = typeof(WildcardUtil).GetMethod("Match", new[] { typeof(AssetLocation), typeof(AssetLocation), typeof(string[]) });
            if (mi2 != null) return (bool)mi2.Invoke(null, new object[] { pattern, code, null });

            var mi3 = typeof(WildcardUtil).GetMethod("Match", new[] { typeof(AssetLocation), typeof(AssetLocation) });
            if (mi3 != null) return (bool)mi3.Invoke(null, new object[] { pattern, code });

            return pattern != null && code != null && pattern.Equals(code);
        }
    }
}
