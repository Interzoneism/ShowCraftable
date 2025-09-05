using HarmonyLib;
using ImprovedHandbookRecipes;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ShowCraftable
{
    // ----------------------------- CLIENT SYSTEM -----------------------------
    public class ShowCraftableSystem : ModSystem
    {
        private Harmony _harmony;
        private ICoreClientAPI _capi;

        private static volatile bool CraftableTabActive;

        public const string HarmonyId = "showcraftable.core";
        public const string CraftableCategoryCode = "craftable";

        private static bool UseServerNearbyScan = true;
        public const string ChannelName = "showcraftablescan";
        private static int NearbyRadius = 12; // standard

        // Expose default radius for ImprovedHandbookRecipes.FillGridButton
        public static int DefaultNearbyRadius => NearbyRadius;

        /// <summary>
        /// Returns candidate source slots from any nearby storage (closed or open), including ground storage, shelves, crates, etc.
        /// Skips any slots passed in <paramref name="skip"/>.
        /// </summary>
        public static IEnumerable<ItemSlot> GetNearbyStorageSlots(ICoreClientAPI capi, int radius, HashSet<ItemSlot> skip = null)
        {
            var result = new List<ItemSlot>();
            try
            {
                var world = capi.World;
                var player = world.Player;
                var pos = player?.Entity?.ServerPos?.AsBlockPos ?? player?.Entity?.Pos?.AsBlockPos;
                if (pos == null) return result;

                int r = Math.Max(0, radius);
                var ba = world.BlockAccessor;

                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -1; dy <= 2; dy++)
                        for (int dz = -r; dz <= r; dz++)
                        {
                            BlockPos bp = pos.AddCopy(dx, dy, dz);
                            var be = ba.GetBlockEntity(bp);
                            if (be == null) continue;

                            var inv = TryGetInventoryFromBE(be);
                            if (inv == null) continue;

                            foreach (var slot in inv)
                            {
                                if (slot?.Itemstack == null) continue;
                                if (slot.StackSize <= 0) continue;
                                if (skip != null && skip.Contains(slot)) continue;
                                result.Add(slot);
                            }
                        }
            }
            catch { }

            return result;
        }

        // ---- Cache (page codes, not page objects) ----
        private static readonly object CacheLock = new();
        private static List<string> CachedPageCodes = new();   // PageCode strings

        private static bool ScanInProgress = false;

        // -------------------- Pause Guard for Handbook --------------------
        /// <summary>
        /// Ensures the handbook does not pause the world while a scan runs.
        /// - On first acquire: set noHandbookPause=true, unpause, sync toggle visuals
        /// - On last release: restore original noHandbookPause; if handbook still open and original implied pause, pause again; sync visuals
        /// Safe for overlapping acquires via refcount.
        /// </summary>
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
                    catch { /* best-effort */ }
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

                        // Only re-pause if the handbook is currently open and the original setting implied pause
                        if (IsHandbookOpen(capi) && !_savedNoHandbookPause)
                        {
                            capi.PauseGame(true);
                        }

                        SyncToggleVisual(capi);
                    }
                    catch { /* best-effort */ }
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

                    // Try both composers; either may be active depending on which view is shown
                    var tDlg = typeof(GuiDialogHandbook);
                    var overview = tDlg.GetField("overviewGui", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(dlg) as GuiComposer;
                    var detail = tDlg.GetField("detailViewGui", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(dlg) as GuiComposer;

                    overview?.GetToggleButton("pausegame")?.SetValue(shouldBePaused);
                    detail?.GetToggleButton("pausegame")?.SetValue(shouldBePaused);
                }
                catch { /* best-effort */ }
            }
        }

        // --------------- Logging (chat + client/server logger + file) ---------------
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

        // In ShowCraftableSystem (so server can call it)
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

        // --- Debounced scan trigger ---
        private static DateTime _lastScanAt = DateTime.MinValue;

        private static void RequestServerScan(ICoreClientAPI capi, int radius, bool includeCrates)
        {
            // Avoid flooding on quick tab switches
            var now = DateTime.UtcNow;
            if ((now - _lastScanAt).TotalMilliseconds < 400) return;
            _lastScanAt = now;

            if (ScanInProgress) return;
            ScanInProgress = true;

            // Hold the unpaused state for the duration of the async scan
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

        // -------------------- Lifecycle --------------------
        public override void StartClientSide(ICoreClientAPI capi)
        {
            _capi = capi;
            _harmony = new Harmony(HarmonyId);

            // Register net channel early
            capi.Network
                .RegisterChannel(ChannelName)
                .RegisterMessageType(typeof(CraftScanRequest))
                .RegisterMessageType(typeof(CraftScanReply))
                .RegisterMessageType(typeof(FetchToGridRequest))
                .SetMessageHandler<CraftScanReply>(OnServerScanReply);

            // Patch tabs & list population
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

            // Initialize Improved Handbook UI and behavior (identical to original; only source slots differ via our scan)
            Handbook_Patch.SetAPI(capi);
            Textures.Load(capi);
            _harmony.PatchAll(typeof(Handbook_Patch).Assembly);

            // Commands
            capi.ChatCommands.Create("craftable")
                .WithDescription("Open Survival Handbook at the Craftable tab (no rescan)")
                .HandleWith(args =>
                {
                    capi.Event.RegisterCallback(_ => OpenCraftableTab(capi), 10);
                    return TextCommandResult.Success();
                });

            capi.ChatCommands.Create("craftablescan")
                .WithDescription("Rebuild Craftable cache (player + nearby containers via server)")
                .HandleWith(args =>
                {
                    if (UseServerNearbyScan)
                    {
                        RequestServerScan(capi, NearbyRadius, includeCrates: true);
                        return TextCommandResult.Success();
                    }

                    ScanInProgress = true;
                    HandbookPauseGuard.Acquire(capi);
                    try
                    {
                        if (UseServerNearbyScan)
                        {
                            capi.Network.GetChannel(ChannelName).SendPacket(new CraftScanRequest
                            {
                                Radius = NearbyRadius,
                                IncludeCrates = true // tills vidare
                            });
                            LogEverywhere(capi, $"[Craftable] Requested server-side nearby container scan (r={NearbyRadius})…", toChat: true);
                            // Rest happens in OnServerScanReply
                            return TextCommandResult.Success();
                        }

                        // Fallback: local-only scan (client side)
                        int pages = RebuildCache(capi, includeNearby: true, radius: NearbyRadius,
                                                 out int outputs, out int fetched, out int usable);
                        LogEverywhere(capi, $"[Craftable] Local scan done: outputs={outputs}, pages={pages}, fetched={fetched}, usable={usable}", toChat: true);
                        TryRefreshOpenDialog(capi);
                    }
                    catch (Exception e)
                    {
                        LogEverywhere(capi, $"[Craftable] Scan failed: {e}", toChat: true);
                    }
                    finally
                    {
                        ScanInProgress = false;
                        // Local path releases; server path releases in OnServerScanReply
                        if (!UseServerNearbyScan) HandbookPauseGuard.Release(capi);
                    }

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

            // Clear cache on level finalize (avoid stale state)
            capi.Event.LevelFinalize += () =>
            {
                lock (CacheLock) CachedPageCodes.Clear();
                LogEverywhere(capi, "[Craftable] LevelFinalize: cache cleared");
            };
        }

        public override void Dispose() => _harmony?.UnpatchAll(HarmonyId);

        // -------------------- Tab injection --------------------
        public static void GenTabs_Postfix(object __instance, ref object __result, ref int curTab)
        {
            try
            {
                var tabs = ((Array)__result)?.Cast<object>().ToList() ?? new List<object>();
                if (tabs.Count == 0) return;

                // Fallback between versions
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

        private static int _pendingScanId;

        private static bool DialogIsOpen(object inst)
        {
            // Fast path
            if (inst is GuiDialog dlg) return dlg.IsOpened();

            // Fallback via reflection (defensive across minor API changes)
            var mi = inst.GetType().GetMethod("IsOpened", BindingFlags.Instance | BindingFlags.Public);
            return mi != null && mi.ReturnType == typeof(bool) && (bool)mi.Invoke(inst, Array.Empty<object>());
        }

        public static void SelectTab_Postfix(object __instance, string code)
        {
            try
            {
                CraftableTabActive = string.Equals(code, CraftableCategoryCode, StringComparison.Ordinal);

                // Bail early if the dialog isn’t open (e.g., rapid close after click)
                if (!DialogIsOpen(__instance))
                {
                    // cancel any pending debounced scan tied to this dialog/tab
                    _pendingScanId++;
                    return;
                }

                var fiCapi = AccessTools.Field(__instance.GetType(), "capi");
                var capi = fiCapi?.GetValue(__instance) as ICoreClientAPI;

                var fiOverview = AccessTools.Field(__instance.GetType(), "overviewGui");
                var composer = fiOverview?.GetValue(__instance);

                // Make overview the active composer (like vanilla Search())
                var piSingle =
                    AccessTools.Property(__instance.GetType(), "SingleComposer")
                    ?? AccessTools.Property(__instance.GetType().BaseType, "SingleComposer");
                try { piSingle?.SetValue(__instance, composer); } catch { /* ignore */ }

                if (!CraftableTabActive)
                {
                    _pendingScanId++; // stop any in-flight debounce
                    return;
                }

                // Clear search box & state, then refresh + debounced server scan
                var miGetTextInput = composer?.GetType().GetMethod("GetTextInput");
                var searchInput = miGetTextInput?.Invoke(composer, new object[] { "searchField" });
                searchInput?.GetType().GetMethod("SetValue")?.Invoke(searchInput, new object[] { "", true });

                AccessTools.Field(__instance.GetType(), "currentSearchText")?.SetValue(__instance, null);
                AccessTools.Method(__instance.GetType(), "FilterItems")?.Invoke(__instance, null);

                if (capi != null && UseServerNearbyScan)
                {
                    // Debounce: capture a token; only the latest token is allowed to run
                    var myScanId = ++_pendingScanId;

                    // If you already have your own debounce utility, use it; otherwise a simple delayed callback works.
                    capi.Event.EnqueueMainThreadTask(() =>
                    {
                        // Drop if a newer scan superseded this or dialog closed in the meantime
                        if (myScanId != _pendingScanId) return;
                        if (!DialogIsOpen(__instance) || !CraftableTabActive) return;

                        RequestServerScan(capi, NearbyRadius, includeCrates: true);
                    }, "CraftableScanKickoff");
                }
            }
            catch (Exception) { /* keep postfix bulletproof */ }
        }

        // -------------------- After pages loaded: refresh our tab if active --------------------
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
                // Ensure the overview composer is the active one (mirrors vanilla Search()).
                var piSingle = AccessTools.Property(__instance.GetType(), "SingleComposer")
                           ?? AccessTools.Property(__instance.GetType().BaseType, "SingleComposer");
                try { piSingle?.SetValue(__instance, composer); } catch { /* ignore */ }

                // Karta: pagecode -> index
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

                // Sökfilter (vanilla-likt)
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

                // Se till att sidor är Visible=true
                foreach (var p in finalPages)
                {
                    var visProp = p.GetType().GetProperty("Visible", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    try { visProp?.SetValue(p, true); } catch { }
                }

                // Fyll listan
                shown.Clear();
                foreach (var p in finalPages) shown.Add(p);

                // Höjd som vanilla använder
                double listHeight = 500d;
                // Replicate the exact UI update logic from the end of the vanilla FilterItems method.
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

                return false; // This must stay to prevent the original method from running
            }
            catch
            {
                return true;
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
                    var code = s.Collectible.Code.ToString();
                    if (string.IsNullOrEmpty(code)) continue;

                    var pc = miPageCodeForStack.Invoke(null, new object[] { s }) as string;
                    if (string.IsNullOrEmpty(pc)) continue;

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

            var pool = BuildResourcePool(capi, includeNearby, radius);
            return RebuildCacheWithPool(capi, pool, out craftableOutputsCount, out fetched, out usable);
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
                if (stack == null) return;

                // Normal path
                CollectibleObject coll = stack.Collectible;

                // Very old-build fallback via reflection (no compile-time reference!)
                if (coll == null)
                {
                    try
                    {
                        var pi = stack.GetType().GetProperty("collectible",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        coll = pi?.GetValue(stack) as CollectibleObject;
                    }
                    catch { /* ignore */ }
                }

                if (coll == null) return;

                var code = coll.Code?.ToString();
                if (string.IsNullOrEmpty(code)) return;

                var k = new Key { Code = code };
                int q = Math.Max(1, stack.StackSize);

                Counts[k] = Counts.TryGetValue(k, out var cur) ? cur + q : q;
                if (!Classes.ContainsKey(k)) Classes[k] = stack.Class;
            }

            public void Add(ItemStack stack, int explicitCount)
            {
                if (stack == null) return;
                var coll = stack.Collectible;
                var code = coll?.Code?.ToString();
                if (string.IsNullOrEmpty(code)) return;

                var k = new Key { Code = code };
                int q = Math.Max(1, explicitCount);

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
            var inv = TryGetInventoryFromBE(be);
            if (inv == null) return;

            foreach (var slot in inv)
            {
                var st = slot?.Itemstack;
                if (st?.Collectible != null) pool.Add(st);
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

        private static ResourcePool BuildResourcePoolPlayerOnly(ICoreClientAPI capi)
        {
            var pool = new ResourcePool();
            var mgr = capi.World?.Player?.InventoryManager;
            if (mgr == null) return pool;

            void AddInv(IInventory inv)
            {
                if (inv == null) return;
                foreach (var slot in inv) if (slot?.Itemstack != null) pool.Add(slot.Itemstack);
            }

            AddInv(mgr.GetOwnInventory("craftinggrid"));
            AddInv(mgr.GetOwnInventory("backpack"));
            AddInv(mgr.GetHotbarInventory());
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
            public int QuantityRequired;
            public List<ItemStack> Options = new();
            public AssetLocation PatternCode;
            public Dictionary<string, string[]> Allowed;
            public EnumItemClass Type;
        }

        // Presence-only check over source pool (not the tmp copy)
        private static bool HasWildcard(
            ResourcePool pool,
            EnumItemClass type,
            AssetLocation patternCode,
            Dictionary<string, string[]> allowed)
        {
            if (patternCode == null) return false;

            foreach (var kv in pool.Counts)
            {
                if (kv.Value <= 0) continue;
                if (!pool.Classes.TryGetValue(kv.Key, out var cls) || cls != type) continue;

                var al = new AssetLocation(kv.Key.Code);
                if (WildcardMatch(patternCode, al, allowed))
                    return true;
            }
            return false;
        }

        // Presence-only: any option present in pool.Counts
        private static bool HasAnyOption(ResourcePool pool, IList<ItemStack> options)
        {
            if (options == null || options.Count == 0) return false;

            for (int i = 0; i < options.Count; i++)
            {
                var st = options[i];
                var coll = st?.Collectible;
                var code = coll?.Code?.ToString();
                if (string.IsNullOrEmpty(code)) continue;

                var key = new Key { Code = code };
                if (pool.Counts.TryGetValue(key, out int have) && have > 0)
                    return true;
            }
            return false;
        }

        // Aggregate across all matching keys until need reaches 0
        private static bool TryConsumeWildcard(
            ResourcePool pool,
            Dictionary<Key, int> tmpCounts,
            EnumItemClass type,
            AssetLocation patternCode,
            Dictionary<string, string[]> allowed,
            int need)
        {
            if (patternCode == null || need <= 0) return need <= 0;

            foreach (var kv in pool.Counts)
            {
                if (need <= 0) break;

                var key = kv.Key;
                if (!pool.Classes.TryGetValue(key, out var cls) || cls != type) continue;
                if (!tmpCounts.TryGetValue(key, out int have) || have <= 0) continue;

                var al = new AssetLocation(key.Code);
                if (!WildcardMatch(patternCode, al, allowed)) continue;

                int take = have < need ? have : need;
                if (take <= 0) continue;

                tmpCounts[key] = have - take;
                need -= take;
            }

            return need == 0;
        }

        // Aggregate consumption across multiple specific options
        private static bool TryConsumeOptions(
            Dictionary<Key, int> tmpCounts,
            IList<ItemStack> options,
            int need)
        {
            if (options == null || options.Count == 0) return false;

            for (int i = 0; i < options.Count && need > 0; i++)
            {
                var st = options[i];
                var coll = st?.Collectible;
                var code = coll?.Code?.ToString();
                if (string.IsNullOrEmpty(code)) continue;

                var key = new Key { Code = code };
                if (!tmpCounts.TryGetValue(key, out int have) || have <= 0) continue;

                int take = have < need ? have : need;
                if (take <= 0) continue;

                tmpCounts[key] = have - take;
                need -= take;
            }

            return need == 0;
        }

        private static bool IsCraftable(GridRecipeShim r, ResourcePool pool)
        {
            // Copy only counts; use pool.Classes for class lookups.
            var tmpCounts = new Dictionary<Key, int>(pool.Counts);

            // 1) Tools: presence check only (never consumed)
            foreach (var ing in r.Ingredients)
            {
                if (!ing.IsTool) continue;

                if (ing.IsWild)
                {
                    if (!HasWildcard(pool, ing.Type, ing.PatternCode, ing.Allowed))
                        return false;
                }
                else
                {
                    if (!HasAnyOption(pool, ing.Options))
                        return false;
                }
            }

            // 2) Materials: must be consumable (may aggregate across many keys/options)
            foreach (var ing in r.Ingredients)
            {
                if (ing.IsTool) continue;

                int need = Math.Max(1, ing.QuantityRequired);
                bool ok = ing.IsWild
                    ? TryConsumeWildcard(pool, tmpCounts, ing.Type, ing.PatternCode, ing.Allowed, need)
                    : TryConsumeOptions(tmpCounts, ing.Options, need);

                if (!ok) return false;
            }

            return true;
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

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        private static object TryGetMember(Type t, object obj, string name)
        {
            if (t == null || obj == null || string.IsNullOrEmpty(name)) return null;

            try
            {
                var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (pi != null) return pi.GetValue(obj);
            }
            catch { }

            try
            {
                var fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fi != null) return fi.GetValue(obj);
            }
            catch { }

            try
            {
                var getter = t.GetMethod("get_" + name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (getter != null && getter.GetParameters().Length == 0) return getter.Invoke(obj, null);
            }
            catch { }

            return null;
        }

        private static bool WildcardMatch(AssetLocation pattern, AssetLocation code, Dictionary<string, string[]> allowed)
        {
            if (pattern == null || code == null) return false;

            // exact
            if (pattern.Equals(code)) return true;

            // domain/* and */path and */* etc.
            if (pattern.Path == "*")
            {
                if (pattern.Domain == "*" || string.Equals(pattern.Domain, code.Domain, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            if (pattern.Domain == "*")
            {
                if (pattern.Path == "*" || string.Equals(pattern.Path, code.Path, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // Allowed variant filters
            if (allowed != null && allowed.Count > 0)
            {
                foreach (var kv in allowed)
                {
                    var key = kv.Key;
                    var vals = kv.Value;
                    if (vals == null || vals.Length == 0) continue;

                    // simple contains check on code.Path (e.g., "*-aged" etc.). Can be extended if needed.
                    foreach (var v in vals)
                    {
                        if (code.Path?.Contains(v, StringComparison.OrdinalIgnoreCase) == true)
                            return true;
                    }
                }
            }

            // simple wildcard '*'
            if (pattern.Path?.Contains("*") == true)
            {
                var pat = pattern.Path.Replace("*", "");
                if (code.Path?.Contains(pat, StringComparison.OrdinalIgnoreCase) == true) return true;
            }

            return false;
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
                    else if (pars.Length == 0)
                    {
                        mi.Invoke(raw, null); return;
                    }
                }
                catch { }
            }
        }

        private void OnServerScanReply(CraftScanReply data)
        {
            try
            {
                // 1) Player-only pool (avoid counting already opened containers twice)
                var pool = BuildResourcePoolPlayerOnly(_capi);

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

                // 3) Compute pages
                int pages = RebuildCacheWithPool(_capi, pool, out int outputs, out int fetched, out int usable);
                LogEverywhere(_capi, $"[Craftable] Server nearby scan ok: outputs={outputs}, pages={pages}, fetched={fetched}, usable={usable}", toChat: true);
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

        // -------------------- Rebuild with precomputed resource pool --------------------
        private static int RebuildCacheWithPool(ICoreClientAPI capi, ResourcePool pool,
                                                out int craftableOutputsCount, out int fetched, out int usable)
        {
            craftableOutputsCount = 0; fetched = 0; usable = 0;

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

            var code2page = BuildPageCodeMapFromAllStacks(capi);
            var resultPageCodes = new HashSet<string>();
            foreach (var code in craftableCodes)
                if (code2page.TryGetValue(code, out var pageCode))
                    resultPageCodes.Add(pageCode);

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

            lock (CacheLock) CachedPageCodes = resultPageCodes.ToList();
            LogEverywhere(capi, $"[Craftable] craftable outputs={craftableOutputsCount}, pagesFromMap={resultPageCodes.Count}", toChat: false);

            return resultPageCodes.Count;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;

            api.Network
               .RegisterChannel(ShowCraftableSystem.ChannelName)
               .RegisterMessageType(typeof(CraftScanRequest))
               .RegisterMessageType(typeof(CraftScanReply))
               .RegisterMessageType(typeof(FetchToGridRequest))
               .SetMessageHandler<CraftScanRequest>(OnScanRequest)
               .SetMessageHandler<FetchToGridRequest>(OnFetchToGridRequest);

        }

        private ICoreServerAPI sapi;

        private void OnFetchToGridRequest(IServerPlayer player, FetchToGridRequest req)
        {
            try
            {
                var mgr = player.InventoryManager;
                var crafting = mgr.GetOwnInventory("craftinggrid");
                if (crafting == null) return;

                // Samla källor i närheten (stängda/öppna spelar ingen roll - servern får ta)
                var pos = player.Entity.Pos.AsBlockPos;
                var ba = sapi.World.BlockAccessor;
                int r = Math.Max(0, req.Radius);

                // Bygg lista över potentiella käll-slots i BE inventories
                var sources = new List<ItemSlot>();
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -1; dy <= 2; dy++)
                        for (int dz = -r; dz <= r; dz++)
                        {
                            var be = ba.GetBlockEntity(pos.AddCopy(dx, dy, dz));
                            var inv = ShowCraftableSystem.TryGetInventoryFromBE(be);
                            if (inv == null) continue;

                            foreach (var s in inv)
                                if (s?.Itemstack != null && s.Itemstack.StackSize > 0)
                                    sources.Add(s);
                        }

                bool AnyMatch(NeedSlotInfo need, ItemStack st)
                {
                    if (st == null || st.StackSize <= 0) return false;

                    // Respektera klass (Item/Block) om specificerat
                    if (need.ItemClass != 0 && (int)st.Class != need.ItemClass) { /* släpp igenom om 0, annars filter */ }

                    if (need.IsWild)
                    {
                        if (need.PatternCode == null) return false;
                        var pat = new AssetLocation(need.PatternCode);
                        var code = st.Collectible?.Code;
                        if (code == null) return false;

                        // Variantfilter (samma som client-sidan)
                        bool match = Vintagestory.API.Util.WildcardUtil.Match(pat, code, need.AllowedVariants);
                        return match;
                    }
                    else
                    {
                        if (need.OptionCodes == null || need.OptionCodes.Length == 0) return false;
                        var scode = st.Collectible?.Code?.ToString();
                        if (string.IsNullOrEmpty(scode)) return false;
                        return Array.IndexOf(need.OptionCodes, scode) >= 0;
                    }
                }

                foreach (var need in req.Needs)
                {
                    int left = Math.Max(1, need.Quantity);
                    if (need.SlotIndex < 0 || need.SlotIndex >= crafting.Count) continue;

                    var target = crafting[need.SlotIndex];

                    foreach (var src in sources.ToArray())
                    {
                        if (left <= 0) break;
                        if (!AnyMatch(need, src.Itemstack)) continue;

                        // Flytta
                        var op = new ItemStackMoveOperation(sapi.World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge, left)
                        {
                            ActingPlayer = player
                        };
                        int before = target.StackSize;
                        mgr.TryTransferTo(src, target, ref op);
                        if (target.StackSize > before)
                        {
                            left -= (target.StackSize - before);

                            // Sync slot changes to client
                            try
                            {
                                src.Inventory?.PerformNotifySlot(src.Inventory.GetSlotId(src));
                                target.Inventory?.PerformNotifySlot(target.Inventory.GetSlotId(target));
                            }
                            catch { /* best effort */ }
                        }

                        // Städa tomma källor
                        if (src.Empty) sources.Remove(src);
                    }
                }
            }
            catch (Exception e)
            {
                sapi.World.Logger.Warning($"[Craftable] OnFetchToGridRequest error: {e}");
            }
        }

        public static void RequestFetchToGrid(ICoreClientAPI capi, List<NeedSlotInfo> needs, int radius)
        {
            if (needs == null || needs.Count == 0) return;
            try
            {
                capi.Network.GetChannel(ChannelName)
                    .SendPacket(new FetchToGridRequest { Radius = radius, Needs = needs });
            }
            catch (Exception e)
            {
                capi.Logger.Warning($"[Craftable] Fetch request failed: {e}");
            }
        }

        // Bygger NeedSlotInfo från ett recept-ingredient och mål-slot i grid
        public static NeedSlotInfo MakeNeedForSlot(GridRecipeIngredient ingr, ItemSlot target, int n)
        {
            if (ingr == null || target == null || n <= 0) return null;

            // --- Hitta slot-index utan SlotNumber ---
            int slotIndex = 0;
            try
            {
                var inv = target.Inventory;
                if (inv != null)
                {
                    slotIndex = -1;
                    for (int i = 0; i < inv.Count; i++)
                    {
                        // referensjämförelse för att hitta just den här sloten
                        if (object.ReferenceEquals(inv[i], target))
                        {
                            slotIndex = i;
                            break;
                        }
                    }
                    if (slotIndex < 0) slotIndex = 0; // defensivt fallback
                }
            }
            catch { /* fallback till 0 om något går snett */ }

            var info = new NeedSlotInfo
            {
                SlotIndex = slotIndex,
                Quantity = n
            };

            // --- Wildcard/konkret ingrediens ---
            if (ingr.IsWildCard)
            {
                info.IsWild = true;
                info.ItemClass = (int)ingr.Type;
                info.PatternCode = ingr.Code?.ToString();
                info.AllowedVariants = ingr.AllowedVariants;
                return info;
            }

            // --- Samla alla konkreta alternativkoder ---
            var optionCodes = new List<string>();

            // Singulärt resolved stack (finns i alla versioner)
            try
            {
                var single = ingr.ResolvedItemstack;
                var code = single?.Collectible?.Code?.ToString();
                if (!string.IsNullOrEmpty(code)) optionCodes.Add(code);
            }
            catch { /* ignorera om ej finns i denna version */ }

            // Multi-resolved stacks: hantera olika namn/kapslingar via reflektion
            try
            {
                var t = ingr.GetType();

                object val = null;
                var pi = t.GetProperty("ResolvedItemstacks") ?? t.GetProperty("ResolvedItemStacks");
                if (pi != null) val = pi.GetValue(ingr);

                if (val == null)
                {
                    var fi = t.GetField("ResolvedItemstacks") ?? t.GetField("ResolvedItemStacks");
                    if (fi != null) val = fi.GetValue(ingr);
                }

                if (val is IEnumerable en)
                {
                    foreach (var obj in en)
                    {
                        var st = obj as ItemStack;
                        var c = st?.Collectible?.Code?.ToString();
                        if (!string.IsNullOrEmpty(c)) optionCodes.Add(c);
                    }
                }
            }
            catch { /* helt ok om den inte finns i denna version */ }

            info.OptionCodes = optionCodes.Distinct().ToArray();
            return info;
        }

        private void OnScanRequest(IServerPlayer fromPlayer, CraftScanRequest req)
        {
            var pos = fromPlayer.Entity.Pos.AsBlockPos;
            var ba = sapi.World.BlockAccessor;
            int r = Math.Max(0, req.Radius);

            var sum = new Dictionary<string, (int count, EnumItemClass cls)>();

            for (int dx = -r; dx <= r; dx++)
                for (int dy = -1; dy <= 2; dy++)
                    for (int dz = -r; dz <= r; dz++)
                    {
                        var be = ba.GetBlockEntity(pos.AddCopy(dx, dy, dz));
                        if (be == null) continue;

                        var inv = ShowCraftableSystem.TryGetInventoryFromBE(be);
                        if (inv == null) continue;

                        // --- Special handling for crates: they only ever hold 1 item type; sum all slots into one ---
                        if (be is BlockEntityCrate)
                        {
                            if (!req.IncludeCrates) continue;

                            ItemStack sample = null;
                            int total = 0;

                            foreach (var slot in inv)
                            {
                                var st = slot?.Itemstack;
                                if (st?.Collectible?.Code == null) continue;

                                if (sample == null) sample = st;
                                total += Math.Max(1, st.StackSize);
                            }

                            if (sample != null)
                            {
                                var code = sample.Collectible.Code?.ToString();
                                var cls = sample.Class;
                                if (sum.TryGetValue(code, out var cur)) sum[code] = (cur.count + total, cls);
                                else sum[code] = (total, cls);
                            }

                            continue; // crate handled; skip generic path
                        }

                        // --- Generic path: sum each slot as-is (works for chests, vessels, shelves, racks, etc.) ---
                        foreach (var slot in inv)
                        {
                            var st = slot?.Itemstack;
                            if (st?.Collectible?.Code == null) continue;

                            string code = st.Collectible.Code.ToString();
                            int add = Math.Max(1, st.StackSize);
                            var cls = st.Class;

                            if (sum.TryGetValue(code, out var cur)) sum[code] = (cur.count + add, cls);
                            else sum[code] = (add, cls);
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

    // ----------------------------- PACKETS -----------------------------
    [ProtoContract]
    public class CraftScanRequest
    {
        [ProtoMember(1)] public int Radius { get; set; }
        [ProtoMember(2)] public bool IncludeCrates { get; set; }
    }

    [ProtoContract]
    public class CraftScanReply
    {
        [ProtoMember(1)] public List<string> Codes { get; set; } = new();
        [ProtoMember(2)] public List<int> Counts { get; set; } = new();
        [ProtoMember(3)] public List<EnumItemClass> Classes { get; set; } = new();
    }

    [ProtoContract]
    public class FetchToGridRequest
    {
        [ProtoMember(1)] public int Radius { get; set; } = ShowCraftableSystem.DefaultNearbyRadius;
        [ProtoMember(2)] public List<NeedSlotInfo> Needs { get; set; } = new();
    }

    [ProtoContract]
    public class NeedSlotInfo
    {
        [ProtoMember(1)] public int SlotIndex { get; set; }         // mål-slot i craftinggrid (0..8)
        [ProtoMember(2)] public bool IsWild { get; set; }
        [ProtoMember(3)] public int Quantity { get; set; }
        [ProtoMember(4)] public int ItemClass { get; set; }         // (int) EnumItemClass
        [ProtoMember(5)] public string PatternCode { get; set; }    // "domain:path"
        [ProtoMember(6)] public string[] AllowedVariants { get; set; }
        [ProtoMember(7)] public string[] OptionCodes { get; set; }  // konkreta koder om ej wildcard
    }
}
