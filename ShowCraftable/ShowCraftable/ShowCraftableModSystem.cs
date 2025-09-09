using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using ProtoBuf;

namespace ShowCraftable
{
    public class ShowCraftableSystem : ModSystem
    {
        private Harmony _harmony;
        private ICoreClientAPI _capi;
        private static ICoreClientAPI _staticCapi;

        private static volatile bool CraftableTabActive;

        public const string HarmonyId = "showcraftable.core";
        public const string CraftableCategoryCode = "craftable";

        public const string ChannelName = "showcraftablescan";
        private static int NearbyRadius = 12;

        private static readonly object CacheLock = new();
        private static List<string> CachedPageCodes = new();

        private static bool ScanInProgress = false;

        private static Dictionary<string, List<(GridRecipeShim Recipe, string GroupKey)>> codeToRecipeGroups = new();
        private static Dictionary<GridRecipeShim, Dictionary<string, int>> recipeGroupNeeds = new();
        private static int recipesFetched;
        private static int recipesUsable;

        private static class HandbookPauseGuard
        {
            private static int _refCount;
            private static bool _savedNoHandbookPause;

            public static void Acquire(ICoreClientAPI capi)
            {
                if (capi == null || !capi.IsSinglePlayer || capi.OpenedToLan) return;

                if (Interlocked.Increment(ref _refCount) == 1)
                {
                    try
                    {
                        _savedNoHandbookPause = capi.Settings.Bool["noHandbookPause"];
                        capi.Settings.Bool["noHandbookPause"] = true;
                        capi.PauseGame(false);
                        SyncToggleVisual(capi);
                    }
                    catch { }
                }
            }

            public static void Release(ICoreClientAPI capi)
            {
                if (capi == null || !capi.IsSinglePlayer || capi.OpenedToLan) return;

                if (Interlocked.Decrement(ref _refCount) == 0)
                {
                    try
                    {
                        capi.Settings.Bool["noHandbookPause"] = _savedNoHandbookPause;


                        if (IsHandbookOpen(capi) && !_savedNoHandbookPause)
                        {
                            capi.PauseGame(true);
                        }

                        SyncToggleVisual(capi);
                    }
                    catch { }
                }
            }

            private static bool IsHandbookOpen(ICoreClientAPI capi)
            {
                try { return capi.OpenedGuis?.OfType<GuiDialogHandbook>()?.Any() == true; }
                catch { return false; }
            }

            private static void SyncToggleVisual(ICoreClientAPI capi)
            {
                try
                {
                    var dlg = capi.OpenedGuis?.OfType<GuiDialogHandbook>()?.FirstOrDefault();
                    if (dlg == null) return;

                    bool shouldBePaused = !capi.Settings.Bool["noHandbookPause"];


                    var tDlg = typeof(GuiDialogHandbook);
                    var overview = tDlg.GetField("overviewGui", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(dlg) as GuiComposer;
                    var detail = tDlg.GetField("detailViewGui", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(dlg) as GuiComposer;

                    overview?.GetToggleButton("pausegame")?.SetValue(shouldBePaused);
                    detail?.GetToggleButton("pausegame")?.SetValue(shouldBePaused);
                }
                catch { }
            }
        }

        internal static void AcquireHandbookPauseGuard(ICoreClientAPI capi) => HandbookPauseGuard.Acquire(capi);
        internal static void ReleaseHandbookPauseGuard(ICoreClientAPI capi) => HandbookPauseGuard.Release(capi);

        // --- Attribute-aware handbook keys ---
        private readonly record struct StackKey(string Code, string Material, string Type);

        // Pulls "material" and "type" (if present) from a stack
        private static string GetAttrStringSafe(ItemStack st, string key)
        {
            try { return st?.Attributes?.GetString(key, null); } catch { return null; }
        }

        // Builds an attribute-aware key from a stack
        private static StackKey KeyFor(ItemStack st)
        {
            var code = st?.Collectible?.Code?.ToString() ?? "";
            var material = GetAttrStringSafe(st, "material") ?? "";
            var type = GetAttrStringSafe(st, "type") ?? "";
            return new StackKey(code, material, type);
        }

        // Creates a stack and assigns "material" / "type" attributes if provided
        private static ItemStack MakeStackFromCodeAndAttrs(ICoreClientAPI capi, string code, string material, string type)
        {
            var st = MakeStackFromCode(capi, code);
            if (st == null) return null;
            try
            {
                if (!string.IsNullOrEmpty(material)) st.Attributes.SetString("material", material);
                if (!string.IsNullOrEmpty(type)) st.Attributes.SetString("type", type);
            }
            catch { /* best effort */ }
            return st;
        }

        // Extract the * token out of code path vs. a "path-with-*" pattern
        private static string ExtractTokenFromPath(string patternPath, string codePath)
        {
            if (string.IsNullOrEmpty(patternPath) || string.IsNullOrEmpty(codePath)) return null;
            int i = patternPath.IndexOf('*');
            if (i < 0) return null;
            string pre = patternPath.Substring(0, i);
            string post = patternPath.Substring(i + 1);
            if (!codePath.StartsWith(pre) || !codePath.EndsWith(post)) return null;
            return codePath.Substring(pre.Length, codePath.Length - pre.Length - post.Length);
        }

        private static string PathPart(string domainCode)
        {
            int c = domainCode?.IndexOf(':') ?? -1;
            return c >= 0 ? domainCode.Substring(c + 1) : domainCode ?? "";
        }

        // Aggregate counts in the pool per wildcard *token* (e.g., plank-* -> oak/birch/…)
        private static Dictionary<string, int> GetWildcardTokenCounts(ResourcePool pool, AssetLocation pattern, string[] allowed)
        {
            var res = new Dictionary<string, int>(StringComparer.Ordinal);
            if (pool == null || pattern == null) return res;

            string patPath = pattern.Path ?? pattern.ToString();
            foreach (var kv in pool.Counts)
            {
                var codeStr = kv.Key.Code;
                try
                {
                    var al = new AssetLocation(codeStr);
                    // Respect Allowed[] when present
                    if (!WildcardMatch(pattern, al, (allowed != null && allowed.Length > 0) ? allowed : null)) continue;

                    var token = ExtractTokenFromPath(patPath, PathPart(al.Path ?? al.ToString()));
                    if (string.IsNullOrEmpty(token)) continue;

                    res[token] = res.TryGetValue(token, out var cur) ? cur + kv.Value : kv.Value;
                }
                catch { /* ignore */ }
            }
            return res;
        }

        // Expand a satisfiable recipe into attribute-aware output keys
        private static void ExpandOutputsForRecipe(
            ICoreClientAPI capi,
            ResourcePool pool,
            GridRecipeShim recipe,
            HashSet<StackKey> dest,
            Dictionary<GridRecipeShim, Dictionary<string, int>> originalNeeds)
        {
            if (recipe?.Outputs == null || recipe.Outputs.Count == 0) return;

            // Try to find a single relevant wildcard ingredient (e.g., plank-*) that "drives" {wood} material
            var wild = recipe.Ingredients?.FirstOrDefault(i => i != null && i.IsWild && i.PatternCode != null);
            string[] allowed = wild?.Allowed;

            // Compute the original quantity required for that wildcard group (if any)
            int neededFromWild = 0;
            if (wild != null && originalNeeds != null && originalNeeds.TryGetValue(recipe, out var needMap) && needMap != null)
            {
                var gkey = $"wild:{wild.PatternCode}|{string.Join(",", (allowed ?? Array.Empty<string>()).OrderBy(x => x))}|T:{wild.Type}";
                if (!needMap.TryGetValue(gkey, out neededFromWild))
                    neededFromWild = Math.Max(1, wild.QuantityRequired);
            }

            // If we have a wildcard, pre-compute token counts in the pool
            Dictionary<string, int> tokenCounts = null;
            if (wild != null) tokenCounts = GetWildcardTokenCounts(pool, wild.PatternCode, allowed);

            foreach (var os in recipe.Outputs)
            {
                var ocode = os?.Collectible?.Code?.ToString();
                if (string.IsNullOrEmpty(ocode)) continue;

                string outType = GetAttrStringSafe(os, "type");      // e.g., "2row1col" or "normal"
                string outMat = GetAttrStringSafe(os, "material");  // may be null or "{wood}"

                bool needsMaterialExpansion = string.IsNullOrEmpty(outMat) || outMat.IndexOf('{') >= 0 || outMat.IndexOf('}') >= 0;

                if (wild != null && tokenCounts != null && tokenCounts.Count > 0 && needsMaterialExpansion)
                {
                    // Expand one output per token that individually meets the required quantity
                    foreach (var kv in tokenCounts)
                    {
                        string token = kv.Key;
                        int have = kv.Value;
                        if (neededFromWild <= 0 || have >= neededFromWild)
                        {
                            dest.Add(new StackKey(ocode, token, outType ?? ""));
                        }
                    }
                }
                else
                {
                    // No expansion necessary: just use whatever attributes the output already carries (or none)
                    dest.Add(new StackKey(ocode, outMat ?? "", outType ?? ""));
                }
            }
        }

        private static void LogEverywhere(ICoreClientAPI capi, string msg, bool toChat = false)
        {
            try { capi.Logger?.Notification(msg); } catch { }
            try { capi.World?.Logger?.Notification(msg); } catch { }
            if (toChat) { try { capi.ShowChatMessage(msg); } catch { } }

            try
            {
                string basePath = null;
                var m = typeof(ICoreAPI).GetMethod("GetOrCreateDataPath", BindingFlags.Public | BindingFlags.Instance);
                if (m != null) basePath = (string)m.Invoke(capi, new object[] { "ShowCraftable" });
                if (string.IsNullOrEmpty(basePath))
                {
                    basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShowCraftable");
                }
                Directory.CreateDirectory(basePath);
                var f = Path.Combine(basePath, "craftable.log");
                File.AppendAllText(f, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {msg}\n");
            }
            catch { }
        }


        internal static IInventory TryGetInventoryFromBE(BlockEntity be)
        {
            if (be == null) return null;

            if (be is IBlockEntityContainer ibec && ibec.Inventory != null) return ibec.Inventory;

            var t = be.GetType();
            var pi = t.GetProperty("Inventory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var invObj = pi?.GetValue(be);
            if (invObj == null)
                invObj = t.GetField("Inventory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(be);
            if (invObj == null)
                invObj = t.GetField("inventory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(be);

            return invObj as IInventory;
        }


        private static DateTime _lastScanAt = DateTime.MinValue;

        private static void RequestServerScan(ICoreClientAPI capi, int radius, bool includeCrates)
        {

            var now = DateTime.UtcNow;
            if ((now - _lastScanAt).TotalMilliseconds < 400) return;
            _lastScanAt = now;

            if (ScanInProgress) return;
            ScanInProgress = true;


            HandbookPauseGuard.Acquire(capi);

            try
            {
                capi.Network.GetChannel(ChannelName).SendPacket(new CraftScanRequest
                {
                    Radius = radius,
                    IncludeCrates = includeCrates
                });
                LogEverywhere(capi, $"[Craftable] Requested server scan (r={radius}, crates={includeCrates})");
            }
            catch (Exception e)
            {
                ScanInProgress = false;
                HandbookPauseGuard.Release(capi);
                LogEverywhere(capi, $"[Craftable] scan send failed: {e}", toChat: true);
            }
        }


        public override void StartClientSide(ICoreClientAPI capi)
        {
            _capi = capi;
            _staticCapi = capi;
            _harmony = new Harmony(HarmonyId);


            capi.Network
                .RegisterChannel(ChannelName)
                .RegisterMessageType(typeof(CraftScanRequest))
                .RegisterMessageType(typeof(CraftScanReply))
                .SetMessageHandler<CraftScanReply>(OnServerScanReply);


            var tSurv = AccessTools.TypeByName("Vintagestory.GameContent.GuiDialogSurvivalHandbook");
            var miGenTabs = AccessTools.Method(tSurv, "genTabs");
            _harmony.Patch(miGenTabs, postfix: new HarmonyMethod(typeof(ShowCraftableSystem), nameof(GenTabs_Postfix)));

            var tBase = AccessTools.TypeByName("Vintagestory.GameContent.GuiDialogHandbook");
            var miFilter = AccessTools.Method(tBase, "FilterItems");
            _harmony.Patch(miFilter, prefix: new HarmonyMethod(typeof(ShowCraftableSystem), nameof(FilterItems_Prefix)));

            var miSelect = AccessTools.Method(tBase, "selectTab");
            _harmony.Patch(miSelect, postfix: new HarmonyMethod(typeof(ShowCraftableSystem), nameof(SelectTab_Postfix)));

            var miLoadAsync = AccessTools.Method(tBase, "LoadPages_Async");
            _harmony.Patch(miLoadAsync, postfix: new HarmonyMethod(typeof(ShowCraftableSystem), nameof(AfterPagesLoaded_Postfix)));

            var tBeh = AccessTools.TypeByName("Vintagestory.GameContent.CollectibleBehaviorHandbookTextAndExtraInfo");
            var miAddInfo = AccessTools.Method(tBeh, "addCreatedByInfo");
            _harmony.Patch(miAddInfo, postfix: new HarmonyMethod(typeof(ShowCraftableSystem), nameof(AddRecipeButton_Postfix)));



            capi.ChatCommands.Create("craftable")
                .WithDescription("Open Survival Handbook at the Craftable tab (no rescan)")
                .HandleWith(args =>
                {
                    capi.Event.RegisterCallback(_ => OpenCraftableTab(capi), 10);
                    return TextCommandResult.Success();
                });

            capi.ChatCommands.Create("craftabledump")
                .WithDescription("Dump Craftable cache & resolution stats")
                .HandleWith(args =>
                {
                    try
                    {
                        List<string> codes;
                        lock (CacheLock) codes = CachedPageCodes.ToList();

                        LogEverywhere(capi, $"[Craftable] dump: cached={codes.Count}, first10=[{string.Join(", ", codes.Take(10))}]", toChat: true);

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


            capi.Event.LevelFinalize += () =>
            {
                lock (CacheLock) CachedPageCodes.Clear();
                codeToRecipeGroups.Clear();
                recipeGroupNeeds.Clear();
                BuildRecipeIndex(capi);
                LogEverywhere(capi, "[Craftable] LevelFinalize: cache cleared");
            };
        }

        public override void Dispose() => _harmony?.UnpatchAll(HarmonyId);


        public static void GenTabs_Postfix(object __instance, ref object __result, ref int curTab)
        {
            try
            {
                var tabs = ((Array)__result)?.Cast<object>().ToList() ?? new List<object>();
                if (tabs.Count == 0) return;


                var tabType = AccessTools.TypeByName("Vintagestory.GameContent.HandbookTab")
                           ?? AccessTools.TypeByName("Vintagestory.GameContent.GuiTab");
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

                int insertAt = Math.Min(2, tabs.Count);
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

        private static int _pendingScanId;

        private static bool DialogIsOpen(object inst)
        {
            if (inst is GuiDialog dlg) return dlg.IsOpened();
            var mi = inst.GetType().GetMethod("IsOpened", BindingFlags.Instance | BindingFlags.Public);
            return mi != null && mi.ReturnType == typeof(bool) && (bool)mi.Invoke(inst, Array.Empty<object>());
        }

        public static void SelectTab_Postfix(object __instance, string code)
        {
            try
            {
                CraftableTabActive = string.Equals(code, CraftableCategoryCode, StringComparison.Ordinal);


                if (!DialogIsOpen(__instance))
                {
                    _pendingScanId++;
                    return;
                }

                var fiCapi = AccessTools.Field(__instance.GetType(), "capi");
                var capi = fiCapi?.GetValue(__instance) as ICoreClientAPI;

                var fiOverview = AccessTools.Field(__instance.GetType(), "overviewGui");
                var composer = fiOverview?.GetValue(__instance);

                var piSingle =
                    AccessTools.Property(__instance.GetType(), "SingleComposer")
                    ?? AccessTools.Property(__instance.GetType().BaseType, "SingleComposer");
                try { piSingle?.SetValue(__instance, composer); } catch { }

                if (!CraftableTabActive)
                {
                    _pendingScanId++;
                    return;
                }

                var miGetTextInput = composer?.GetType().GetMethod("GetTextInput");
                var searchInput = miGetTextInput?.Invoke(composer, new object[] { "searchField" });
                searchInput?.GetType().GetMethod("SetValue")?.Invoke(searchInput, new object[] { "", true });

                AccessTools.Field(__instance.GetType(), "currentSearchText")?.SetValue(__instance, null);
                AccessTools.Method(__instance.GetType(), "FilterItems")?.Invoke(__instance, null);

                if (capi != null)
                {

                    var myScanId = ++_pendingScanId;


                    capi.Event.EnqueueMainThreadTask(() =>
                    {

                        if (myScanId != _pendingScanId) return;
                        if (!DialogIsOpen(__instance) || !CraftableTabActive) return;

                        RequestServerScan(capi, NearbyRadius, includeCrates: true);
                    }, "CraftableScanKickoff");
                }
            }
            catch (Exception) { }
        }

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

        private static void AddRecipeButton_Postfix(List<RichTextComponentBase> components)
        {
            if (_staticCapi == null || components == null) return;
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] is SlideshowGridRecipeTextComponent slide)
                {
                    components.Insert(i + 1, new RecipeGridButton(_staticCapi, slide));
                    i++;
                }
            }
        }


        public static bool FilterItems_Prefix(object __instance)
        {
            try
            {
                string cat = (string)AccessTools.Field(__instance.GetType(), "currentCatgoryCode").GetValue(__instance);
                if (!string.Equals(cat, CraftableCategoryCode, StringComparison.Ordinal)) return true;

                var fiCapi = AccessTools.Field(__instance.GetType(), "capi");
                var fiShown = AccessTools.Field(__instance.GetType(), "shownHandbookPages");
                var fiOverview = AccessTools.Field(__instance.GetType(), "overviewGui");
                var fiListH = AccessTools.Field(__instance.GetType(), "listHeight");
                var fiSearch = AccessTools.Field(__instance.GetType(), "currentSearchText");
                var fiLoading = AccessTools.Field(__instance.GetType(), "loadingPagesAsync");

                var capi = fiCapi?.GetValue(__instance) as ICoreClientAPI;
                var shown = fiShown?.GetValue(__instance) as System.Collections.IList;
                if (fiOverview?.GetValue(__instance) is not GuiComposer composer) return true;
                string q = (string)fiSearch?.GetValue(__instance);
                bool loading = fiLoading != null && (bool)fiLoading.GetValue(__instance);

                if (shown == null || composer == null) return true;

                var piSingle = AccessTools.Property(__instance.GetType(), "SingleComposer")
                           ?? AccessTools.Property(__instance.GetType().BaseType, "SingleComposer");
                try { piSingle?.SetValue(__instance, composer); } catch { }

                var pageMap = AccessTools.Field(__instance.GetType(), "pageNumberByPageCode")?.GetValue(__instance) as Dictionary<string, int>;
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


                foreach (var p in finalPages)
                {
                    var visProp = p.GetType().GetProperty("Visible", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    try { visProp?.SetValue(p, true); } catch { }
                }

                shown.Clear();
                foreach (var p in finalPages) shown.Add(p);


                double listHeight = 500d;

                GuiElementFlatList stacklist = composer.GetFlatList("stacklist");
                if (stacklist != null)
                {
                    stacklist.CalcTotalHeight();

                    var scrollbar = composer.GetScrollbar("scrollbar");
                    if (scrollbar != null)
                    {
                        scrollbar.SetHeights((float)listHeight, (float)stacklist.insideBounds.fixedHeight);

                        if (!string.IsNullOrWhiteSpace(q))
                        {
                            scrollbar.CurrentYPosition = 0;
                            stacklist.insideBounds.fixedY = 3f;
                            stacklist.insideBounds.CalcWorldBounds();
                        }
                    }
                }

                return false;
            }
            catch
            {
                return true;
            }
        }


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


        private static Dictionary<StackKey, string> BuildPageCodeMapFromAllStacks(ICoreClientAPI capi)
        {
            var map = new Dictionary<StackKey, string>();
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
                    var key = KeyFor(s);
                    var pc = miPageCodeForStack.Invoke(null, new object[] { s }) as string;
                    if (string.IsNullOrEmpty(pc)) continue;

                    // NOTE: if duplicates exist, keep the first
                    if (!map.ContainsKey(key)) map[key] = pc;
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
                if (stack == null) return;

                CollectibleObject coll = stack.Collectible;

                if (coll == null)
                {
                    try
                    {
                        var pi = stack.GetType().GetProperty("collectible",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        coll = pi?.GetValue(stack) as CollectibleObject;
                    }
                    catch { }
                }

                if (coll == null) return;

                var code = coll.Code?.ToString();
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
            public bool TryConsumeWildcard(EnumItemClass type, AssetLocation pattern, string[] allowed, int quantity, bool consume)
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
            public int QuantityRequired;
            public List<ItemStack> Options = new();
            public AssetLocation PatternCode;
            public string[] Allowed;
            public EnumItemClass Type;
        }

        private static List<GridRecipeShim> GetAllGridRecipes(ICoreClientAPI capi, out int fetched, out int usable)
        {
            var sw = Stopwatch.StartNew();
            var list = new List<GridRecipeShim>();
            fetched = 0; usable = 0;

            var world = capi.World;
            IEnumerable<object> rawRecipes = Enumerable.Empty<object>();

            var pi = world.GetType().GetProperty("GridRecipes", BindingFlags.Public | BindingFlags.Instance);
            var fi = world.GetType().GetField("GridRecipes", BindingFlags.Public | BindingFlags.Instance);
            bool gridMemberFound = pi != null || fi != null;
            object val = pi?.GetValue(world) ?? fi?.GetValue(world);
            bool usedGridRecipes = val is System.Collections.IEnumerable;
            if (usedGridRecipes)
                rawRecipes = ((System.Collections.IEnumerable)val).Cast<object>();
            else
                rawRecipes = FetchGridRecipesMulti(capi);

            foreach (var raw in rawRecipes)
            {
                fetched++;
                var shim = TryBuildGridShim(raw, capi);
                if (shim != null && shim.Outputs.Count > 0)
                {
                    usable++;
                    list.Add(shim);
                }
            }

            sw.Stop();
            LogEverywhere(capi, $"[Craftable] Grid recipes fetched={fetched}, usable={usable}, ms={sw.ElapsedMilliseconds}, gridMemberFound={gridMemberFound}, usedWorldList={usedGridRecipes}");
            return list;
        }
        private static void BuildRecipeIndex(ICoreClientAPI capi)
        {
            codeToRecipeGroups.Clear();
            recipeGroupNeeds.Clear();

            var recipes = GetAllGridRecipes(capi, out recipesFetched, out recipesUsable);
            foreach (var r in recipes)
            {
                var groups = new Dictionary<string, int>(StringComparer.Ordinal);

                foreach (var ing in r.Ingredients)
                {
                    string groupKey;
                    IEnumerable<string> satisfiableCodes;

                    if (ing.IsWild)
                    {
                        var allowed = ing.Allowed ?? Array.Empty<string>();
                        groupKey = $"wild:{ing.PatternCode}|{string.Join(",", allowed.OrderBy(x => x))}|T:{ing.Type}";
                        satisfiableCodes = AllCodesMatching(capi, ing);   // preexpanded once at index time
                    }
                    else
                    {
                        var codes = ing.Options
                            .Select(st => st?.Collectible?.Code?.ToString())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Distinct()
                            .OrderBy(s => s)
                            .ToList();

                        if (codes.Count == 0) continue;

                        groupKey = "opts:" + string.Join(",", codes);
                        satisfiableCodes = codes;
                    }

                    int qty = Math.Max(1, ing.QuantityRequired);
                    if (ing.IsTool) qty = 1;

                    groups[groupKey] = groups.TryGetValue(groupKey, out var cur) ? cur + qty : qty;

                    foreach (var code in satisfiableCodes)
                    {
                        if (!codeToRecipeGroups.TryGetValue(code, out var list))
                            codeToRecipeGroups[code] = list = new List<(GridRecipeShim, string)>();
                        if (!list.Contains((r, groupKey))) list.Add((r, groupKey));
                    }
                }

                if (groups.Count > 0)
                    recipeGroupNeeds[r] = groups;
            }
        }


        private static IEnumerable<string> AllCodesMatching(ICoreClientAPI capi, GridIngredientShim ing)
        {
            var results = new List<string>();
            if (ing.PatternCode == null) return results;

            IEnumerable<CollectibleObject> coll = null;
            if (ing.Type == EnumItemClass.Item)
                coll = capi.World.Items?.Where(i => i != null);
            else if (ing.Type == EnumItemClass.Block)
                coll = capi.World.Blocks?.Where(b => b != null);

            if (coll != null)
            {
                foreach (var c in coll)
                {
                    var code = c.Code?.ToString();
                    if (string.IsNullOrEmpty(code)) continue;
                    try
                    {
                        var al = new AssetLocation(code);
                        if (WildcardMatch(ing.PatternCode, al, ing.Allowed)) results.Add(code);
                    }
                    catch { }
                }
            }

            return results;
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

            foreach (var tuple in new (object host, string name)[] { (w, "CraftingRecipes"), (w?.Api, "CraftingRecipes") })
            {
                var v = TryGetMember(tuple.host?.GetType(), tuple.host, tuple.name);
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

        private static GridRecipeShim TryBuildGridShim(object raw, ICoreClientAPI capi)
        {
            if (raw == null) return null;
            var t = raw.GetType();
            if (!t.Name.Contains("GridRecipe", StringComparison.OrdinalIgnoreCase)) return null;

            var shim = new GridRecipeShim { Raw = raw };

            bool hadResolved = false;
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
                    var allowedObj = TryGetMember(it, ingRaw, "AllowedVariants");
                    gi.Allowed = allowedObj as string[];
                    if (gi.Allowed == null && allowedObj is Dictionary<string, string[]> dict)
                    {
                        gi.Allowed = dict.SelectMany(kv => kv.Value ?? Array.Empty<string>()).Distinct().ToArray();
                    }
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
                    hadResolved = true;
                }
            }

            if (!hadResolved)
            {
                var ingreds = TryGetMember(t, raw, "Ingredients") as System.Collections.IDictionary;
                var pattern = TryGetMember(t, raw, "IngredientPattern") as string;
                if (ingreds != null && pattern != null)
                {
                    foreach (var ch in pattern)
                    {
                        if (ch == ' ' || ch == '_') continue;
                        string key = ch.ToString();
                        if (!ingreds.Contains(key)) continue;

                        var ingRaw = ingreds[key];
                        if (ingRaw == null) continue;
                        var it = ingRaw.GetType();
                        var gi = new GridIngredientShim();

                        gi.IsTool = TryGetMember(it, ingRaw, "IsTool") as bool? ?? false;
                        gi.PatternCode = TryGetMember(it, ingRaw, "Code") as AssetLocation;
                        var allowedObj = TryGetMember(it, ingRaw, "AllowedVariants");
                        gi.Allowed = allowedObj as string[];
                        if (gi.Allowed == null && allowedObj is Dictionary<string, string[]> dict)
                            gi.Allowed = dict.SelectMany(kv => kv.Value ?? Array.Empty<string>()).Distinct().ToArray();
                        gi.Type = (EnumItemClass)(TryGetMember(it, ingRaw, "Type") as EnumItemClass? ?? EnumItemClass.Item);
                        gi.QuantityRequired = TryGetMember(it, ingRaw, "Quantity") as int? ?? 1;

                        bool isWild = TryGetMember(it, ingRaw, "IsWildCard") as bool? ?? false;
                        if (!isWild)
                        {
                            var path = gi.PatternCode?.Path;
                            if (path != null && (path.Contains("*") || path.Contains("{") || path.Contains("}") || path.StartsWith("@")))
                                isWild = true;
                        }
                        gi.IsWild = isWild;

                        if (!gi.IsWild)
                        {
                            var codeStr = gi.PatternCode?.ToString();
                            var st = MakeStackFromCode(capi, codeStr);
                            if (st != null)
                            {
                                st.StackSize = gi.QuantityRequired;
                                gi.Options.Add(st);
                            }
                        }

                        if (gi.IsWild || gi.Options.Count > 0) shim.Ingredients.Add(gi);
                    }
                }
            }

            var outputIng = TryGetMember(t, raw, "Output");
            if (outputIng != null)
            {
                var ot = outputIng.GetType();
                var outStack = TryGetMember(ot, outputIng, "ResolvedItemstack") as ItemStack;
                if (outStack != null && outStack.Collectible != null)
                {
                    shim.Outputs.Add(outStack);
                }
                else
                {
                    var code = TryGetMember(ot, outputIng, "Code") as AssetLocation;
                    var stack = MakeStackFromCode(capi, code?.ToString());
                    if (stack != null)
                    {
                        int qty = TryGetMember(ot, outputIng, "Quantity") as int? ?? 1;
                        stack.StackSize = Math.Max(1, qty);
                        shim.Outputs.Add(stack);
                    }
                }
            }

            var outs = TryGetMember(t, raw, "ResolvedOutputs") ?? TryGetMember(t, raw, "Outputs");
            if (outs is System.Collections.IEnumerable outEnum)
            {
                foreach (var o in outEnum)
                {
                    if (o == null) continue;
                    if (o is ItemStack os)
                    {
                        if (os.Collectible != null) shim.Outputs.Add(os);
                        else
                        {
                            var codeProp = TryGetMember(o.GetType(), o, "Code") as AssetLocation;
                            var st = MakeStackFromCode(capi, codeProp?.ToString());
                            if (st != null) shim.Outputs.Add(st);
                        }
                    }
                    else
                    {
                        var ot = o.GetType();
                        var stack = TryGetMember(ot, o, "ResolvedItemstack") as ItemStack;
                        if (stack != null && stack.Collectible != null)
                        {
                            shim.Outputs.Add(stack);
                        }
                        else
                        {
                            var code = TryGetMember(ot, o, "Code") as AssetLocation;
                            var st = MakeStackFromCode(capi, code?.ToString());
                            if (st != null)
                            {
                                int qty = TryGetMember(ot, o, "Quantity") as int? ?? 1;
                                st.StackSize = Math.Max(1, qty);
                                shim.Outputs.Add(st);
                            }
                        }
                    }
                }
            }

            return shim;
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

        private static bool WildcardMatch(AssetLocation pattern, AssetLocation code, string[] allowed)
        {
            var mi = typeof(WildcardUtil).GetMethod("Match", new[] { typeof(AssetLocation), typeof(AssetLocation), typeof(string[]) });
            if (mi != null)
            {
                return (bool)mi.Invoke(null, new object[] { pattern, code, allowed });
            }

            var mi2 = typeof(WildcardUtil).GetMethod("Match", new[] { typeof(AssetLocation), typeof(AssetLocation), typeof(Dictionary<string, string[]>) });
            if (mi2 != null)
            {
                return (bool)mi2.Invoke(null, new object[] { pattern, code, null });
            }

            var mi3 = typeof(WildcardUtil).GetMethod("Match", new[] { typeof(AssetLocation), typeof(AssetLocation) });
            if (mi3 != null)
            {
                return (bool)mi3.Invoke(null, new object[] { pattern, code });
            }

            return pattern != null && code != null && pattern.Equals(code);
        }

        private void OnServerScanReply(CraftScanReply data)
        {
            try
            {

                var pool = new ResourcePool();

                for (int i = 0; i < data.Codes.Count; i++)
                {
                    string code = data.Codes[i];
                    int cnt = data.Counts[i];
                    var cls = data.Classes[i];

                    var loc = new AssetLocation(code);
                    ItemStack st = null;
                    if (cls == EnumItemClass.Item)
                    {
                        var it = _capi.World.GetItem(loc);
                        if (it != null) st = new ItemStack(it, cnt);
                    }
                    else
                    {
                        var bl = _capi.World.GetBlock(loc);
                        if (bl != null) st = new ItemStack(bl, cnt);
                    }
                    if (st != null) pool.Add(st);
                }


                int pages = RebuildCacheWithPool(_capi, pool, out int outputs, out int fetched, out int usable);
                LogEverywhere(_capi, $"[Craftable] Server nearby scan merged: outputs={outputs}, pages={pages}, fetched={fetched}, usable={usable}", toChat: true);
                TryRefreshOpenDialog(_capi);
            }
            catch (Exception e)
            {
                LogEverywhere(_capi, $"[Craftable] OnServerScanReply error: {e}", toChat: true);
            }
            finally
            {
                ScanInProgress = false;
                HandbookPauseGuard.Release(_capi);
            }
        }


        private static int RebuildCacheWithPool(ICoreClientAPI capi, ResourcePool pool,
     out int craftableOutputsCount, out int fetched, out int usable)
        {
            craftableOutputsCount = 0; fetched = recipesFetched; usable = recipesUsable;

            // Clone the per-recipe group needs so we can subtract
            var remaining = recipeGroupNeeds.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.ToDictionary(g => g.Key, g => g.Value, StringComparer.Ordinal)
            );

            // First pass: subtract available counts from each recipe group
            foreach (var kv in pool.Counts)
            {
                var code = kv.Key.Code;
                var have = kv.Value;
                if (!codeToRecipeGroups.TryGetValue(code, out var targets) || targets == null) continue;

                foreach (var (recipe, groupKey) in targets)
                {
                    if (!remaining.TryGetValue(recipe, out var groups)) continue;
                    if (!groups.TryGetValue(groupKey, out var need) || need <= 0) continue;
                    int take = Math.Min(need, have);
                    if (take <= 0) continue;
                    groups[groupKey] = need - take;
                }
            }

            // Second pass: any recipe fully satisfied? expand its outputs into attribute-aware keys
            var craftableKeys = new HashSet<StackKey>();
            foreach (var kv in remaining)
            {
                var recipe = kv.Key;
                var groups = kv.Value;
                bool ok = groups.Count > 0 && groups.All(g => g.Value <= 0);
                if (!ok) continue;

                ExpandOutputsForRecipe(capi, pool, recipe, craftableKeys, recipeGroupNeeds);
            }

            craftableOutputsCount = craftableKeys.Count;

            // Map keys -> handbook pages
            var key2page = BuildPageCodeMapFromAllStacks(capi);
            var resultPageCodes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var key in craftableKeys)
                if (key2page.TryGetValue(key, out var pageCode))
                    resultPageCodes.Add(pageCode);

            // Fallback: compute pages via PageCodeForStack if the prebuilt map missed something
            if (resultPageCodes.Count == 0)
            {
                var ghType = AccessTools.TypeByName("Vintagestory.GameContent.GuiHandbookItemStackPage");
                var miPageCodeForStack = ghType?.GetMethod("PageCodeForStack", BindingFlags.Public | BindingFlags.Static);
                if (miPageCodeForStack != null)
                {
                    foreach (var key in craftableKeys)
                    {
                        var stack = MakeStackFromCodeAndAttrs(capi, key.Code, key.Material, key.Type);
                        if (stack == null) continue;
                        var pc = miPageCodeForStack.Invoke(null, new object[] { stack }) as string;
                        if (!string.IsNullOrEmpty(pc)) resultPageCodes.Add(pc);
                    }
                }
            }

            lock (CacheLock) CachedPageCodes = resultPageCodes.ToList();
            LogEverywhere(capi, $"[Craftable] craftable outputs={craftableOutputsCount}, pagesFromMap={resultPageCodes.Count}", toChat: false);
            return resultPageCodes.Count;
        }


    }

    [ProtoContract]
    public class CraftIngredient
    {
        [ProtoMember(1)] public bool IsWildcard { get; set; }
        [ProtoMember(2)] public int Quantity { get; set; }
        [ProtoMember(3)] public List<string> Codes { get; set; } = new();
        [ProtoMember(4)] public string PatternCode { get; set; }
        [ProtoMember(5)] public List<string> Allowed { get; set; } = new();
        [ProtoMember(6)] public EnumItemClass Type { get; set; }
        [ProtoMember(7)] public bool HasType { get; set; }
    }

    [ProtoContract]
    public class CraftIngredientList
    {
        [ProtoMember(1)] public List<CraftIngredient> Ingredients { get; set; } = new();
    }

    [ProtoContract]
    public class CraftScanRequest
    {
        [ProtoMember(1)] public int Radius { get; set; }
        [ProtoMember(2)] public bool IncludeCrates { get; set; }
        [ProtoMember(3)] public bool CollectItems { get; set; }
        [ProtoMember(4)] public List<CraftIngredientList> Variants { get; set; } = new();
    }

    [ProtoContract]
    public class CraftScanReply
    {
        [ProtoMember(1)] public List<string> Codes { get; set; } = new();
        [ProtoMember(2)] public List<int> Counts { get; set; } = new();
        [ProtoMember(3)] public List<EnumItemClass> Classes { get; set; } = new();
    }

    public class ShowCraftableServerSystem : ModSystem
    {
        private ICoreServerAPI sapi;

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;

            api.Network
               .RegisterChannel(ShowCraftableSystem.ChannelName)
               .RegisterMessageType(typeof(CraftScanRequest))
               .RegisterMessageType(typeof(CraftScanReply))
               .SetMessageHandler<CraftScanRequest>(OnScanRequest);
        }

        private static bool WildcardMatch(AssetLocation pattern, AssetLocation code, string[] allowed)
        {
            try
            {
                if (allowed != null && allowed.Length > 0)
                {
                    var mi = typeof(WildcardUtil).GetMethod(
                        "Match",
                        new[] { typeof(AssetLocation), typeof(AssetLocation), typeof(string[]) }
                    );
                    if (mi != null)
                    {
                        return (bool)mi.Invoke(null, new object[] { pattern, code, allowed });
                    }
                }
            }
            catch { /* fall through to 2-arg */ }

            return WildcardUtil.Match(pattern, code);
        }


        private class SlotRef
        {
            public ItemSlot Slot;
            public string Code;
            public EnumItemClass Class;
            public BlockEntity BlockEntity;
        }

        private bool SlotMatches(SlotRef slot, CraftIngredient ing)
        {
            if (ing == null || slot?.Slot?.Itemstack == null) return false;

            if (ing.Codes != null && ing.Codes.Contains(slot.Code)) return true;

            if (ing.IsWildcard && !string.IsNullOrEmpty(ing.PatternCode))
            {
                try
                {
                    if (ing.HasType && slot.Class != ing.Type) return false;

                    var pattern = new AssetLocation(ing.PatternCode);
                    var code = new AssetLocation(slot.Code);
                    var allowed = (ing.Allowed != null && ing.Allowed.Count > 0) ? ing.Allowed.ToArray() : null;
                    if (WildcardMatch(pattern, code, allowed)) return true;
                }
                catch { }
            }

            return false;
        }

        private bool CanSatisfyVariant(Dictionary<string, int> counts, CraftIngredientList variant)
        {
            if (variant == null) return false;

            var tmp = new Dictionary<string, int>(counts);

            foreach (var ing in variant.Ingredients)
            {
                int need = ing.Quantity;
                if (need <= 0) continue;

                if (ing.Codes != null && ing.Codes.Count > 0)
                {
                    int sum = 0;
                    foreach (var code in ing.Codes)
                    {
                        if (tmp.TryGetValue(code, out var have)) sum += have;
                        if (sum >= need) break;
                    }
                    if (sum < need) return false;

                    foreach (var code in ing.Codes)
                    {
                        if (need <= 0) break;
                        if (!tmp.TryGetValue(code, out var have) || have <= 0) continue;
                        int take = Math.Min(need, have);
                        tmp[code] = have - take;
                        if (tmp[code] <= 0) tmp.Remove(code);
                        need -= take;
                    }
                }
                else if (ing.IsWildcard && !string.IsNullOrEmpty(ing.PatternCode))
                {
                    var pattern = new AssetLocation(ing.PatternCode);
                    var allowed = (ing.Allowed != null && ing.Allowed.Count > 0) ? ing.Allowed.ToArray() : null;

                    foreach (var kv in tmp.ToList())
                    {
                        if (need <= 0) break;
                        try
                        {
                            var al = new AssetLocation(kv.Key);
                            if (!WildcardMatch(pattern, al, allowed)) continue;
                        }
                        catch { continue; }

                        int take = Math.Min(need, kv.Value);
                        tmp[kv.Key] = kv.Value - take;
                        if (tmp[kv.Key] <= 0) tmp.Remove(kv.Key);
                        need -= take;
                    }
                    if (need > 0) return false;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private bool ExecuteVariant(List<SlotRef> slots, CraftIngredientList variant, IServerPlayer player,
            Dictionary<string, (int count, EnumItemClass cls)> sum, Dictionary<string, int> playerCounts)
        {
            bool allFetched = true;
            var pcount = playerCounts != null ? new Dictionary<string, int>(playerCounts) : new Dictionary<string, int>();
            foreach (var ing in variant.Ingredients)
            {
                int need = ing.Quantity;
                if (need <= 0) continue;

                need = DeductFrom(pcount, ing, need);
                if (need <= 0) continue;

                foreach (var sr in slots)
                {
                    if (need <= 0) break;
                    if (!SlotMatches(sr, ing)) continue;

                    int take = Math.Min(need, sr.Slot.StackSize);
                    var taken = sr.Slot.TakeOut(take);
                    if (taken == null) continue;

                    int before = taken.StackSize;
                    player.InventoryManager.TryGiveItemstack(taken);
                    int moved = before - taken.StackSize;
                    need -= moved;

                    if (taken.StackSize > 0)
                    {
                        var leftoverSlot = new DummySlot(taken);
                        leftoverSlot.TryPutInto(player.Entity.World, sr.Slot, taken.StackSize);
                        if (!leftoverSlot.Empty)
                        {
                            player.Entity.World.SpawnItemEntity(leftoverSlot.Itemstack, player.Entity.Pos.XYZ);
                        }
                    }

                    sr.Slot.MarkDirty();
                    if (sr.BlockEntity is BlockEntityGroundStorage gs)
                    {
                        if (gs.Inventory.Empty && !gs.clientsideFirstPlacement)
                        {
                            gs.Api.World.BlockAccessor.SetBlock(0, gs.Pos);
                            gs.Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(gs.Pos);
                        }
                        else
                        {
                            gs.updateMeshes();
                            gs.MarkDirty(true);
                        }
                    }
                    else
                    {
                        sr.BlockEntity?.MarkDirty(true);
                    }

                    if (sum.TryGetValue(sr.Code, out var cur))
                    {
                        int left = cur.count - moved;
                        if (left <= 0) sum.Remove(sr.Code);
                        else sum[sr.Code] = (left, cur.cls);
                    }
                }

                if (need > 0)
                {
                    allFetched = false;
                }
            }

            return allFetched;
        }

        private List<ItemStack> PreviewVariant(List<SlotRef> slots, CraftIngredientList variant, Dictionary<string, int> playerCounts)
        {
            var list = new List<ItemStack>();
            var pcount = playerCounts != null ? new Dictionary<string, int>(playerCounts) : new Dictionary<string, int>();
            foreach (var ing in variant.Ingredients)
            {
                int need = ing.Quantity;
                if (need <= 0) continue;

                need = DeductFrom(pcount, ing, need);
                if (need <= 0) continue;

                foreach (var sr in slots)
                {
                    if (need <= 0) break;
                    if (!SlotMatches(sr, ing)) continue;

                    int take = Math.Min(need, sr.Slot.StackSize);
                    var clone = sr.Slot.Itemstack.Clone();
                    clone.StackSize = take;
                    list.Add(clone);
                    need -= take;
                }
            }
            return list;
        }

        private int DeductFrom(Dictionary<string, int> counts, CraftIngredient ing, int need)
        {
            if (counts == null || counts.Count == 0 || need <= 0) return need;

            if (ing.Codes != null && ing.Codes.Count > 0)
            {
                foreach (var code in ing.Codes)
                {
                    if (need <= 0) break;
                    if (!counts.TryGetValue(code, out var have) || have <= 0) continue;
                    int take = Math.Min(need, have);
                    have -= take;
                    if (have <= 0) counts.Remove(code);
                    else counts[code] = have;
                    need -= take;
                }
            }
            else if (ing.IsWildcard && !string.IsNullOrEmpty(ing.PatternCode))
            {
                var pattern = new AssetLocation(ing.PatternCode);
                foreach (var kv in counts.ToList())
                {
                    if (need <= 0) break;
                    try
                    {
                        var al = new AssetLocation(kv.Key);
                        var allowed = (ing.Allowed != null && ing.Allowed.Count > 0) ? ing.Allowed.ToArray() : null;
                        if (!WildcardMatch(pattern, al, allowed)) continue;
                    }
                    catch { continue; }

                    int take = Math.Min(need, kv.Value);
                    int left = kv.Value - take;
                    if (left <= 0) counts.Remove(kv.Key);
                    else counts[kv.Key] = left;
                    need -= take;
                }
            }

            return need;
        }

        private bool HasInventorySpace(IServerPlayer player, List<ItemStack> items)
        {
            if (items == null || items.Count == 0) return true;

            var need = new Dictionary<string, (ItemStack sample, int count)>();
            foreach (var st in items)
            {
                if (st?.Collectible?.Code == null) continue;
                string code = st.Collectible.Code.ToString();
                if (need.TryGetValue(code, out var entry))
                    need[code] = (entry.sample, entry.count + st.StackSize);
                else
                    need[code] = (st.Clone(), st.StackSize);
            }

            int emptySlots = 0;

            var invs = new IInventory[]
            {
                player.InventoryManager.GetOwnInventory("hotbar"),
                player.InventoryManager.GetOwnInventory("craftinggrid"),
                player.InventoryManager.GetOwnInventory("backpack")
            };

            var seenSlotRefs = new HashSet<ItemSlot>();
            var seenStackRefs = new HashSet<ItemStack>();
            var seenKeys = new HashSet<string>();

            bool Skip(IInventory inv, int i, ItemSlot slot)
            {
                bool dup = false;
                if (slot != null && !seenSlotRefs.Add(slot)) dup = true;
                var st = slot?.Itemstack;
                if (st != null && !seenStackRefs.Add(st)) dup = true;
                string key = null;
                try { key = $"{inv?.InventoryID}:{i}"; } catch { }
                if (key != null && !seenKeys.Add(key)) dup = true;
                return dup;
            }

            foreach (var inv in invs)
            {
                if (inv == null) continue;

                for (int i = 0; i < inv.Count; i++)
                {
                    var slot = inv[i];
                    if (Skip(inv, i, slot)) continue;

                    var existing = slot.Itemstack;
                    if (existing == null)
                    {
                        emptySlots++;
                        continue;
                    }

                    string code = existing.Collectible?.Code?.ToString();
                    if (code == null || !need.TryGetValue(code, out var entry)) continue;

                    int cap = existing.Collectible.GetMergableQuantity(existing, entry.sample, EnumMergePriority.AutoMerge);
                    if (cap <= 0) continue;

                    int moved = Math.Min(cap, entry.count);
                    entry.count -= moved;
                    if (entry.count <= 0) need.Remove(code);
                    else need[code] = entry;
                }
            }

            int requiredSlots = 0;
            foreach (var kv in need.Values)
            {
                int max = kv.sample.Collectible.MaxStackSize;
                requiredSlots += (kv.count + max - 1) / max;
            }

            return requiredSlots <= emptySlots;
        }


        private void OnScanRequest(IServerPlayer fromPlayer, CraftScanRequest req)
        {
            var pos = fromPlayer.Entity.Pos.AsBlockPos;
            var ba = sapi.World.BlockAccessor;
            int r = Math.Max(0, req.Radius);

            var sum = new Dictionary<string, (int count, EnumItemClass cls)>();
            var slots = new List<SlotRef>();
            var playerCounts = new Dictionary<string, int>();

            var seenSlotRefs = new HashSet<ItemSlot>();     
            var seenStackRefs = new HashSet<ItemStack>();  
            var seenKeys = new HashSet<string>();       

            bool IsDuplicate(IInventory inv, int index, ItemSlot slot)
            {
                bool dup = false;
                if (slot != null && !seenSlotRefs.Add(slot)) dup = true;
                var st = slot?.Itemstack;
                if (st != null && !seenStackRefs.Add(st)) dup = true;
                string key = null;
                try { key = $"{inv?.InventoryID}:{index}"; } catch { }
                if (key != null && !seenKeys.Add(key)) dup = true;
                return dup;
            }

            var pinvs = new IInventory[]
            {
                fromPlayer.InventoryManager.GetOwnInventory("hotbar"),
                fromPlayer.InventoryManager.GetOwnInventory("craftinggrid"),
                fromPlayer.InventoryManager.GetOwnInventory("backpack")
            };

            foreach (var inv in pinvs)
            {
                if (inv == null) continue;

                for (int i = 0; i < inv.Count; i++)
                {
                    var slot = inv[i];
                    if (IsDuplicate(inv, i, slot)) continue;

                    var st = slot?.Itemstack;
                    if (st?.Collectible?.Code == null) continue;

                    string code = st.Collectible.Code.ToString();
                    int add = Math.Max(1, st.StackSize);
                    var cls = st.Class;

                    if (sum.TryGetValue(code, out var cur)) sum[code] = (cur.count + add, cls); else sum[code] = (add, cls);
                    if (playerCounts.TryGetValue(code, out var pc)) playerCounts[code] = pc + add; else playerCounts[code] = add;
                }
            }

            var msbr = sapi.ModLoader.GetModSystem<ModSystemBlockReinforcement>();

            for (int dx = -r; dx <= r; dx++)
                for (int dy = -1; dy <= 2; dy++)
                    for (int dz = -r; dz <= r; dz++)
                    {
                        var be = ba.GetBlockEntity(pos.AddCopy(dx, dy, dz));
                        if (be == null) continue;

                        if (msbr?.IsLockedForInteract(be.Pos, fromPlayer) == true) continue;

                        var inv = ShowCraftableSystem.TryGetInventoryFromBE(be);
                        if (inv == null) continue;
                        if (be is BlockEntityCrate && !req.IncludeCrates) continue;

                        for (int i = 0; i < inv.Count; i++)
                        {
                            var slot = inv[i];
                            if (IsDuplicate(inv, i, slot)) continue;

                            var st = slot?.Itemstack;
                            if (st?.Collectible?.Code == null) continue;

                            string code = st.Collectible.Code.ToString();
                            int add = Math.Max(1, st.StackSize);
                            var cls = st.Class;

                            if (sum.TryGetValue(code, out var cur)) sum[code] = (cur.count + add, cls); else sum[code] = (add, cls);

                            slots.Add(new SlotRef { Slot = slot, Code = code, Class = cls, BlockEntity = be });
                        }
                    }

            if (req.CollectItems && req.Variants != null && req.Variants.Count > 0)
            {
                var counts = sum.ToDictionary(kv => kv.Key, kv => kv.Value.count);
                bool done = false;
                bool nospace = false;
                bool partial = false;

                foreach (var variant in req.Variants)
                {
                    bool ignorePlayer = CanSatisfyVariant(playerCounts, variant);
                    if (ignorePlayer)
                    {
                        fromPlayer.SendMessage(GlobalConstants.InfoLogChatGroup,
                            "[ShowCraftable] You already have the required ingredients.", EnumChatType.Notification);
                        done = true;
                        break;
                    }
                    Dictionary<string, int> checkCounts;
                    Dictionary<string, int> pcounts;

                    if (ignorePlayer)
                    {
                        checkCounts = new Dictionary<string, int>(counts);
                        foreach (var kv in playerCounts)
                        {
                            if (checkCounts.TryGetValue(kv.Key, out var have))
                            {
                                int left = have - kv.Value;
                                if (left <= 0) checkCounts.Remove(kv.Key);
                                else checkCounts[kv.Key] = left;
                            }
                        }
                        pcounts = null; 
                    }
                    else
                    {
                        checkCounts = counts;
                        pcounts = playerCounts;
                    }

                    if (!CanSatisfyVariant(checkCounts, variant)) continue;

                    var preview = PreviewVariant(slots, variant, pcounts);
                    if (!HasInventorySpace(fromPlayer, preview))
                    {
                        nospace = true;
                        break;
                    }

                    bool success = ExecuteVariant(slots, variant, fromPlayer, sum, pcounts);
                    done = true;
                    if (!success) partial = true;
                    break;
                }

                if (!done)
                {
                    string msg = nospace
                        ? "[ShowCraftable] Not enough inventory space to fetch the ingredients!"
                        : "[ShowCraftable] Could not collect required ingredients";
                    fromPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, msg, EnumChatType.CommandError);
                }
                else if (partial)
                {
                    fromPlayer.SendMessage(GlobalConstants.InfoLogChatGroup,
                        "[ShowCraftable] Could not get all ingredients, your inventory is full!",
                        EnumChatType.CommandError);
                }
            }

            var reply = new CraftScanReply
            {
                Codes = sum.Keys.ToList(),
                Counts = sum.Values.Select(v => v.count).ToList(),
                Classes = sum.Values.Select(v => v.cls).ToList()
            };

            sapi.Network.GetChannel(ShowCraftableSystem.ChannelName).SendPacket(reply, fromPlayer);
        }


    }
}
