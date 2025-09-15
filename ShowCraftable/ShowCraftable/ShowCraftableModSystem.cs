using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
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
        private static volatile bool CraftableModsTabActive;

        public const string HarmonyId = "showcraftable.core";
        public const string CraftableCategoryCode = "craftable";
        public const string CraftableModsCategoryCode = "craftablemods";

        public const string ChannelName = "showcraftablescan";
        private static int NearbyRadius = 12;

        private static readonly object CacheLock = new();
        private static List<string> CachedPageCodes = new();
        private static readonly Dictionary<string, List<string>> ScanResultsCache = new();

        private static readonly object PageCodeMapLock = new();
        private static Dictionary<StackKey, string> AllStacksPageCodeMap = new();
        private static ItemStack[] AllStacksPageCodeMapSource;

        private static readonly Dictionary<string, Dictionary<string, int>> WildTokenCountsMemo
    = new(StringComparer.Ordinal);


        private static bool ScanInProgress = false;
        private static int LastDialogPageCount;

        private static Dictionary<string, List<(GridRecipeShim Recipe, string GroupKey)>> codeToRecipeGroups = new();
        // Maps a collectible code -> all ingredient group keys (gkeys) that the code satisfies.
        // Built at index time, used at runtime for fast group aggregation.
        private static Dictionary<string, HashSet<string>> codeToGkeys =
            new(StringComparer.Ordinal);
        private static Dictionary<GridRecipeShim, Dictionary<string, int>> recipeGroupNeeds = new();
        private static int recipesFetched;
        private static int recipesUsable;

        private static Task recipeIndexBuildTask;
        private static volatile int recipeIndexBuildTotal;
        private static volatile int recipeIndexBuildProgress;
        private static volatile bool recipeIndexBuilt = false;
        private static volatile bool recipeIndexForMods = false;

        // New tab
        public const string CraftableWoodCategoryCode = "craftablewoodtypes";

        // UI + index state
        private static volatile bool CraftableWoodTabActive;
        private static volatile bool recipeIndexForWoodOnly;


        internal static bool DebugEnabled = true;

        private sealed class WildGroup
        {
            public GridRecipeShim Recipe;
            public string GroupKey;
            public EnumItemClass Type;
            public AssetLocation Pattern;
            public string[] Allowed;
        }

        private static readonly List<WildGroup> wildcardGroups = new();

        private static readonly Dictionary<string, List<(GridRecipeShim Recipe, string GroupKey)>> wildMatchCache
            = new(StringComparer.Ordinal);

        [ProtoContract]
        private class CachedIngredient
        {
            [ProtoMember(1)] public bool IsTool;
            [ProtoMember(2)] public bool IsWild;
            [ProtoMember(3)] public int QuantityRequired;
            [ProtoMember(4)] public List<byte[]> Options = new();
            [ProtoMember(5)] public string PatternCode;
            [ProtoMember(6)] public string[] Allowed;
            [ProtoMember(7)] public EnumItemClass Type;
        }

        [ProtoContract]
        private class CachedRecipe
        {
            [ProtoMember(1)] public List<CachedIngredient> Ingredients = new();
            [ProtoMember(2)] public List<byte[]> Outputs = new();
            [ProtoMember(3)] public Dictionary<string, int> Needs = new();
        }

        [ProtoContract]
        private class CodeRecipeRef
        {
            [ProtoMember(1)] public int Recipe;
            [ProtoMember(2)] public string GroupKey;
        }

        [ProtoContract]
        private class RecipeIndexCache
        {
            [ProtoMember(1)] public List<CachedRecipe> Recipes { get; set; } = new();
            [ProtoMember(2)] public Dictionary<string, List<CodeRecipeRef>> CodeToRecipes { get; set; } = new();
            // NEW: optional since older caches won’t have it
            [ProtoMember(3)] public Dictionary<string, List<string>> CodeToGkeys { get; set; } = new();
        }


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

        private readonly record struct StackKey(string Code, string Material, string Type);
        private static string GetAttrStringSafe(ItemStack st, string key)
        {
            try { return st?.Attributes?.GetString(key, null); } catch { return null; }
        }
        private static StackKey KeyFor(ItemStack st)
        {
            var code = st?.Collectible?.Code?.ToString() ?? "";
            var material = GetAttrStringSafe(st, "material") ?? "";
            var type = GetAttrStringSafe(st, "type") ?? "";
            return new StackKey(code, material, type);
        }

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
        private static ItemStack KeyToItemStack(ICoreClientAPI capi, StackKey key)
        {
            // Rebuild a stack from the key: code + attrs we stored.
            return MakeStackFromCodeAndAttrs(capi, key.Code, key.Material, key.Type);
        }

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

        private static Dictionary<string, int> GetWildcardTokenCounts(ResourcePool pool, AssetLocation pattern, string[] allowed)
        {
            var res = new Dictionary<string, int>(StringComparer.Ordinal);
            if (pool == null || pattern == null) return res;

            string allowedCsv = (allowed != null && allowed.Length > 0) ? string.Join(",", allowed.OrderBy(x => x)) : "";
            string memoKey = (pool.GetSignature() ?? "") + "||" + (pattern.ToString() ?? "") + "||" + allowedCsv;

            lock (WildTokenCountsMemo)
                if (WildTokenCountsMemo.TryGetValue(memoKey, out var cached)) return cached;

            string patPath = pattern.Path ?? pattern.ToString();
            foreach (var kv in pool.Counts)
            {
                var codeStr = kv.Key.Code;
                try
                {
                    var al = new AssetLocation(codeStr);
                    if (!WildcardUtil.Match(pattern, al, (allowed != null && allowed.Length > 0) ? allowed : null)) continue;

                    var token = ExtractTokenFromPath(patPath, PathPart(al.Path ?? al.ToString()));
                    if (string.IsNullOrEmpty(token)) continue;

                    res[token] = res.TryGetValue(token, out var cur) ? cur + kv.Value : kv.Value;
                }
                catch { /* ignore */ }
            }

            lock (WildTokenCountsMemo) WildTokenCountsMemo[memoKey] = res;
            return res;
        }


        private static void ExpandOutputsForRecipe(
            ICoreClientAPI capi,
            ResourcePool pool,
            GridRecipeShim recipe,
            HashSet<StackKey> dest,
            Dictionary<GridRecipeShim, Dictionary<string, int>> originalNeeds)
        {
            if (recipe?.Outputs == null || recipe.Outputs.Count == 0) return;

            var wild = recipe.Ingredients?.FirstOrDefault(i => i != null && i.IsWild && i.PatternCode != null);
            string[] allowed = wild?.Allowed;

            int neededFromWild = 0;
            if (wild != null && originalNeeds != null && originalNeeds.TryGetValue(recipe, out var needMap) && needMap != null)
            {
                var gkey = $"wild:{wild.PatternCode}|{string.Join(",", (allowed ?? Array.Empty<string>()).OrderBy(x => x))}|T:{wild.Type}";
                if (!needMap.TryGetValue(gkey, out neededFromWild))
                    neededFromWild = Math.Max(1, wild.QuantityRequired);
            }

            Dictionary<string, int> tokenCounts = null;
            if (wild != null) tokenCounts = GetWildcardTokenCounts(pool, wild.PatternCode, allowed);

            foreach (var os in recipe.Outputs)
            {
                var ocode = os?.Collectible?.Code?.ToString();
                if (string.IsNullOrEmpty(ocode)) continue;

                string outType = GetAttrStringSafe(os, "type");
                string outMat = GetAttrStringSafe(os, "material");

                bool canExpand = wild != null && tokenCounts != null && tokenCounts.Count > 0;

                if (canExpand)
                {
                    foreach (var kv in tokenCounts)
                    {
                        string token = kv.Key;
                        int have = kv.Value;
                        if (neededFromWild > 0 && have < neededFromWild) continue;

                        string finalCode = ocode;
                        if (!string.IsNullOrEmpty(outMat) && finalCode.Contains(outMat))
                        {
                            finalCode = finalCode.Replace(outMat, token);
                        }

                        dest.Add(new StackKey(finalCode, token, outType ?? ""));
                    }
                }
                else
                {
                    dest.Add(new StackKey(ocode, outMat ?? "", outType ?? ""));
                }
            }
        }

        // Hard-coded wood species (source of truth)
        private static readonly string[] WoodSpecies = new[]
        {
            "birch","maple","pine","acacia","kapok","baldcypress",
            "larch","redwood","ebony","walnut","purpleheart","oak","aged"
        };

        private static bool IsSep(char c) => c == '-' || c == '_' || c == '/' || c == '.';

        // boundary-aware match: (^|[-_/.])mat($|[-_/.])
        private static bool ContainsWoodMatInCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return false;
            foreach (var m in WoodSpecies)
            {
                int idx = code.IndexOf(m, StringComparison.OrdinalIgnoreCase);
                while (idx >= 0)
                {
                    bool leftOk = idx == 0 || IsSep(code[idx - 1]);
                    int right = idx + m.Length;
                    bool rightOk = right >= code.Length || IsSep(code[right]);
                    if (leftOk && rightOk) return true;
                    idx = code.IndexOf(m, idx + m.Length, StringComparison.OrdinalIgnoreCase);
                }
            }
            return false;
        }

        private static bool ContainsWoodMatInAttributes(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;

            // Normalize: lowercase + strip whitespace
            var buf = new char[s.Length];
            int j = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (!char.IsWhiteSpace(c)) buf[j++] = char.ToLowerInvariant(c);
            }
            string t = new string(buf, 0, j);

            // Extract un/quoted value after key:
            // material:foo   material:"foo"   type:wood-foo  type:"wood-foo"
            static string ExtractValue(string text, string key)
            {
                // support material:foo, "material":foo, and 'material':foo
                int i = text.IndexOf(key + ":", StringComparison.Ordinal);
                if (i >= 0)
                {
                    i += key.Length + 1;
                }
                else
                {
                    i = text.IndexOf("\"" + key + "\":", StringComparison.Ordinal);
                    if (i < 0) i = text.IndexOf("'" + key + "':", StringComparison.Ordinal);
                    if (i < 0) return null;
                    i += key.Length + 3; // skip the quote, key, quote and colon
                }

                if (i >= text.Length) return null;

                char qc = (text[i] == '"' || text[i] == '\'') ? text[i++] : '\0';
                int start = i;

                while (i < text.Length)
                {
                    char c = text[i];
                    if ((qc != '\0' && c == qc) || (qc == '\0' && (c == ',' || c == '}' || c == ']')))
                        break;
                    i++;
                }
                return text.Substring(start, i - start); // already lowercase/whitespace-free
            }

            // Unresolved tokens
            var mval = ExtractValue(t, "material");
            if (mval != null)
            {
                if (mval == "{wood}") return true; // material:{wood} OR material:"{wood}"
                foreach (var m in WoodSpecies) if (mval == m) return true; // material:maple
            }

            var typeVal = ExtractValue(t, "type");
            if (typeVal != null)
            {
                if (typeVal == "wood-{wood}") return true; // type:wood-{wood}
                foreach (var m in WoodSpecies) if (typeVal == "wood-" + m) return true; // type:wood-maple
            }

            // Tolerant fallback
            foreach (var m in WoodSpecies)
            {
                if (t.IndexOf("wood-" + m, StringComparison.Ordinal) >= 0) return true;
            }

            return false;
        }




        private static void LogEverywhere(ICoreClientAPI capi, string msg, bool toChat = false)
        {
            if (!DebugEnabled) return;

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

            capi.Event.LevelFinalize += () =>
            {
                lock (CacheLock)
                {
                    CachedPageCodes.Clear();
                    ScanResultsCache.Clear();
                }
                codeToRecipeGroups.Clear();
                recipeGroupNeeds.Clear();
                wildcardGroups.Clear();
                wildMatchCache.Clear();
                recipeIndexBuilt = false;
                InvalidatePageCodeMapCache();
                LogEverywhere(capi, "[Craftable] LevelFinalize: cache reset");
                StartRecipeIndexBuild(capi, false, false);
            };

            capi.Event.LeaveWorld += InvalidatePageCodeMapCache;
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

                bool craftableExists = false;
                bool craftableModsExists = false;
                foreach (var t in tabs)
                {
                    var cat = GetPF(tabType, t, "CategoryCode") as string;
                    if (string.Equals(cat, CraftableCategoryCode, StringComparison.OrdinalIgnoreCase)) craftableExists = true;
                    if (string.Equals(cat, CraftableModsCategoryCode, StringComparison.OrdinalIgnoreCase)) craftableModsExists = true;
                }

                bool craftableWoodExists = false;
                foreach (var t in tabs)
                {
                    var cat = GetPF(tabType, t, "CategoryCode") as string;
                    if (string.Equals(cat, CraftableCategoryCode, StringComparison.OrdinalIgnoreCase)) craftableExists = true;
                    if (string.Equals(cat, CraftableModsCategoryCode, StringComparison.OrdinalIgnoreCase)) craftableModsExists = true;
                    if (string.Equals(cat, CraftableWoodCategoryCode, StringComparison.OrdinalIgnoreCase)) craftableWoodExists = true;
                }


                int insertAt = Math.Min(2, tabs.Count);
                if (!craftableExists)
                {
                    var newTab = Activator.CreateInstance(tabType);
                    SetPF(tabType, newTab, "Name", "Craftable");
                    SetPF(tabType, newTab, "CategoryCode", CraftableCategoryCode);
                    SetPF(tabType, newTab, "DataInt", tabs.Count);
                    SetPF(tabType, newTab, "PaddingTop", 20.0);
                    tabs.Insert(insertAt, newTab);
                    insertAt++;
                }

                if (!craftableModsExists)
                {
                    var newTabMods = Activator.CreateInstance(tabType);
                    SetPF(tabType, newTabMods, "Name", "Craftable (Mods)");
                    SetPF(tabType, newTabMods, "CategoryCode", CraftableModsCategoryCode);
                    SetPF(tabType, newTabMods, "DataInt", tabs.Count);
                    SetPF(tabType, newTabMods, "PaddingTop", 20.0);
                    tabs.Insert(insertAt, newTabMods);
                }

                if (!craftableWoodExists)
                {
                    var woodTab = Activator.CreateInstance(tabType);
                    SetPF(tabType, woodTab, "Name", "Craftable Wood Types");
                    SetPF(tabType, woodTab, "CategoryCode", CraftableWoodCategoryCode);
                    SetPF(tabType, woodTab, "DataInt", tabs.Count);
                    SetPF(tabType, woodTab, "PaddingTop", 20.0);
                    tabs.Insert(insertAt, woodTab);
                }



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
                CraftableModsTabActive = string.Equals(code, CraftableModsCategoryCode, StringComparison.Ordinal);
                CraftableWoodTabActive = string.Equals(code, CraftableWoodCategoryCode, StringComparison.Ordinal);

                bool modsOnly = CraftableModsTabActive;
                bool woodOnly = CraftableWoodTabActive;
                bool anyCraftable = CraftableTabActive || CraftableModsTabActive || CraftableWoodTabActive;


                if (!DialogIsOpen(__instance))
                {
                    _pendingScanId++;
                    return;
                }

                var fiCapi = AccessTools.Field(__instance.GetType(), "capi");
                var capi = fiCapi?.GetValue(__instance) as ICoreClientAPI ?? _staticCapi;

                if (CraftableTabActive)
                    LogEverywhere(capi, "[Craftable] Craftable tab selected by user");
                else if (CraftableModsTabActive)
                    LogEverywhere(capi, "[Craftable] Craftable (Mods) tab selected by user");
                else if (CraftableWoodTabActive)
                    LogEverywhere(capi, "[Craftable] Craftable Wood Types tab selected by user");

                var fiOverview = AccessTools.Field(__instance.GetType(), "overviewGui");
                var composer = fiOverview?.GetValue(__instance) as GuiComposer;

                var piSingle =
                    AccessTools.Property(__instance.GetType(), "SingleComposer")
                    ?? AccessTools.Property(__instance.GetType().BaseType, "SingleComposer");
                try { piSingle?.SetValue(__instance, composer); } catch { }

                if (!anyCraftable)
                {
                    _pendingScanId++;
                    return;
                }

                var miGetTextInput = composer?.GetType().GetMethod("GetTextInput");
                var searchInput = miGetTextInput?.Invoke(composer, new object[] { "searchField" });
                searchInput?.GetType().GetMethod("SetValue")?.Invoke(searchInput, new object[] { "", true });

                AccessTools.Field(__instance.GetType(), "currentSearchText")?.SetValue(__instance, null);

                if (capi != null && composer != null)
                {
                    // Clear the current list on the main thread and refresh the GUI to show an empty tab
                    capi.Event.EnqueueMainThreadTask(() =>
                    {
                        try
                        {
                            var stacklist = composer.GetFlatList("stacklist");
                            stacklist?.Elements.Clear();
                            stacklist?.CalcTotalHeight();
                            var shown = AccessTools.Field(__instance.GetType(), "shownHandbookPages")?.GetValue(__instance) as System.Collections.IList;
                            shown?.Clear();

                            // Temporarily empty the cache so FilterItems renders an empty list
                            List<string> snapshot;
                            lock (CacheLock)
                            {
                                snapshot = CachedPageCodes.ToList();
                                CachedPageCodes.Clear();
                            }

                            AccessTools.Method(__instance.GetType(), "FilterItems")?.Invoke(__instance, null);

                            // Restore cache for later reuse
                            lock (CacheLock) CachedPageCodes = snapshot;
                        }
                        catch { }
                    }, "SCClearCraftableList");

                    try
                    {
                        // Adding or modifying GUI elements during tab selection can
                        // trigger an InvalidOperationException because the element
                        // collection is being iterated for event propagation. Defer
                        // the update to the main thread to avoid modifying the
                        // collection while it is in use.
                        capi.Event.EnqueueMainThreadTask(() => SetUpdatingText(capi, true), "SCUpdatingText");
                    }
                    catch { }

                    var myScanId = ++_pendingScanId;

                    capi.Event.EnqueueMainThreadTask(() =>
                    {
                        if (myScanId != _pendingScanId) return;
                        if (!DialogIsOpen(__instance) || (!CraftableTabActive && !CraftableModsTabActive && !CraftableWoodTabActive)) return;

                        // After the empty state was shown, repopulate from cache if available
                        bool haveCache;
                        lock (CacheLock) haveCache = CachedPageCodes.Count > 0;
                        if (haveCache)
                        {
                            AccessTools.Method(__instance.GetType(), "FilterItems")?.Invoke(__instance, null);
                        }

                        if (!recipeIndexBuilt || recipeIndexForMods != modsOnly || recipeIndexForWoodOnly != woodOnly)
                        {
                            StartRecipeIndexBuild(capi, modsOnly, woodOnly);
                            int total = Math.Max(1, recipeIndexBuildTotal);
                            capi.ShowChatMessage($"[Craftable] Building recipe index {recipeIndexBuildProgress}/{total}...");
                            if (recipeIndexBuildTask != null)
                            {
                                recipeIndexBuildTask.ContinueWith(_ =>
                                {
                                    capi.Event.EnqueueMainThreadTask(() =>
                                    {
                                        if (myScanId != _pendingScanId) return;
                                        if (!DialogIsOpen(__instance) || (!CraftableTabActive && !CraftableModsTabActive && !CraftableWoodTabActive)) return;
                                        RequestServerScan(capi, NearbyRadius, includeCrates: true);
                                    }, "CraftableScanKickoff2");
                                });
                            }
                            return;
                        }

                        RequestServerScan(capi, NearbyRadius, includeCrates: true);
                    }, "CraftableScanKickoff");
                }
                else
                {
                    try
                    {
                        var stacklist = composer?.GetFlatList("stacklist");
                        stacklist?.Elements.Clear();
                        stacklist?.CalcTotalHeight();
                        var shown = AccessTools.Field(__instance.GetType(), "shownHandbookPages")?.GetValue(__instance) as System.Collections.IList;
                        shown?.Clear();

                        List<string> snapshot;
                        lock (CacheLock)
                        {
                            snapshot = CachedPageCodes.ToList();
                            CachedPageCodes.Clear();
                        }

                        AccessTools.Method(__instance.GetType(), "FilterItems")?.Invoke(__instance, null);

                        lock (CacheLock) CachedPageCodes = snapshot;
                    }
                    catch { }
                }
            }
            catch (Exception) { }
        }

        private static void SetUpdatingText(ICoreClientAPI capi, bool show)
        {
            try
            {
                var msType = AccessTools.TypeByName("Vintagestory.GameContent.ModSystemSurvivalHandbook");
                var ms = msType != null ? GetModSystemByType(capi, msType) : null;
                var dlg = msType != null ? AccessTools.Field(msType, "dialog")?.GetValue(ms) : null;
                var composer = dlg != null
                    ? AccessTools.Field(dlg.GetType(), "overviewGui")?.GetValue(dlg) as GuiComposer
                    : null;
                if (composer == null) return;

                var dt = composer.GetDynamicText("scUpdating");
                bool needsRecompose = false;
                if (show)
                {
                    if (dt == null)
                    {
                        var search = composer.GetTextInput("searchField");
                        if (search == null) return;
                        var sb = search.Bounds;
                        var tb = ElementBounds.Fixed(0, sb.fixedY, 120, sb.fixedHeight);
                        tb.ParentBounds = sb.ParentBounds;
                        tb.FixedRightOf(sb, 10);
                        dt = new GuiElementDynamicText(capi, "Updating...", CairoFont.WhiteSmallishText(), tb);
                        composer.AddInteractiveElement(dt, "scUpdating");
                        needsRecompose = true;
                    }
                    else
                    {
                        dt.SetNewText("Updating...");
                    }
                }
                else
                {
                    dt?.SetNewText("");
                }

                if (needsRecompose) composer.ReCompose();
            }
            catch { }
        }

        private static bool IsWoodRecipe(object recipeOrOutput)
        {
            if (recipeOrOutput == null) return false;

            bool CheckOne(object o)
            {
                if (o == null) return false;

                string code = null;
                string attrs = null;

                // Direct ItemStack support
                if (o is ItemStack stack)
                {
                    code = stack.Collectible?.Code?.ToString();
                    attrs = stack.Attributes?.ToJsonToken()?.ToString();
                }
                else
                {
                    var t = o.GetType();

                    // Try get Code as AssetLocation or string
                    var al = TryGetMember(t, o, "Code") as AssetLocation;
                    code = al?.ToString() ?? TryGetMember(t, o, "code") as string;

                    // Attributes may be a TreeAttribute or already stringified; ToString() covers both in your codebase
                    var attrsObj = TryGetMember(t, o, "Attributes");
                    if (attrsObj == null)
                    {
                        // Some recipe structures move attributes to a resolved item stack
                        var rst = TryGetMember(t, o, "ResolvedItemstack") as ItemStack;
                        if (rst != null)
                        {
                            code ??= rst.Collectible?.Code?.ToString();
                            attrsObj = rst.Attributes;
                        }
                    }

                    if (attrsObj is IAttribute attr)
                        attrs = attr.ToJsonToken();
                    else
                        attrs = attrsObj?.ToString();
                }

                if (!string.IsNullOrEmpty(code))
                {
                    if (code.IndexOf("{wood}", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    if (ContainsWoodMatInCode(code)) return true;
                }

                if (!string.IsNullOrEmpty(attrs))
                {
                    if (attrs.IndexOf("{wood}", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    if (ContainsWoodMatInAttributes(attrs)) return true;
                }

                return false;
            }

            // Check single output
            var rt = recipeOrOutput.GetType();
            var outOne = TryGetMember(rt, recipeOrOutput, "Output");
            if (CheckOne(outOne)) return true;

            // Check multi-outputs
            var outs = TryGetMember(rt, recipeOrOutput, "Outputs") as System.Collections.IEnumerable;
            if (outs != null) foreach (var o in outs) if (CheckOne(o)) return true;

            // If caller passed an output-like object directly
            return CheckOne(recipeOrOutput);
        }



        // True if the recipe belongs to a mod (domain != "game")
        private static bool IsModRecipe(ICoreClientAPI capi, object raw)
        {
            if (raw == null) return false;
            try
            {
                var t = raw.GetType();

                // Prefer the recipe's own registry/name domain
                var nameAl = TryGetMember(t, raw, "Name") as AssetLocation;
                if (nameAl != null)
                    return !string.Equals(nameAl.Domain, "game", StringComparison.Ordinal);

                // Fallback: inspect outputs' domains
                object outsObj = TryGetMember(t, raw, "ResolvedOutputs") ?? TryGetMember(t, raw, "Outputs");
                if (outsObj is System.Collections.IEnumerable en)
                {
                    foreach (var o in en)
                    {
                        if (o == null) continue;

                        ItemStack st = o as ItemStack;
                        if (st == null)
                        {
                            var ot = o.GetType();
                            st = TryGetMember(ot, o, "ResolvedItemstack") as ItemStack;
                            if (st == null)
                            {
                                var code = TryGetMember(ot, o, "Code") as AssetLocation;
                                if (code != null) st = MakeStackFromCode(capi, code.ToString());
                            }
                        }

                        var dom = st?.Collectible?.Code?.Domain;
                        if (!string.IsNullOrEmpty(dom) && !string.Equals(dom, "game", StringComparison.Ordinal))
                            return true;
                    }
                }
            }
            catch { /* best-effort */ }

            return false; // default to vanilla
        }


        public static void AfterPagesLoaded_Postfix(object __instance)
        {
            try
            {
                var fiCapi = AccessTools.Field(__instance.GetType(), "capi");
                var capi = fiCapi?.GetValue(__instance) as ICoreClientAPI;
                var cur = AccessTools.Field(__instance.GetType(), "currentCatgoryCode")?.GetValue(__instance) as string;

                if (capi != null && (
                    string.Equals(cur, CraftableCategoryCode, StringComparison.Ordinal) ||
                    string.Equals(cur, CraftableModsCategoryCode, StringComparison.Ordinal) ||
                    string.Equals(cur, CraftableWoodCategoryCode, StringComparison.Ordinal)))
                {
                    AccessTools.Method(__instance.GetType(), "FilterItems")?.Invoke(__instance, null);
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
                if (!(string.Equals(cat, CraftableCategoryCode, StringComparison.Ordinal) ||
                      string.Equals(cat, CraftableModsCategoryCode, StringComparison.Ordinal) ||
                      string.Equals(cat, CraftableWoodCategoryCode, StringComparison.Ordinal))) return true;


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
                if (ms == null)
                {
                    LastDialogPageCount = 0;
                    return;
                }
                var fiDialog = AccessTools.Field(msType, "dialog");
                var dlg = fiDialog?.GetValue(ms);
                if (dlg == null)
                {
                    LastDialogPageCount = 0;
                    return;
                }
                var cur = AccessTools.Field(dlg.GetType(), "currentCatgoryCode")?.GetValue(dlg) as string;
                if (!(string.Equals(cur, CraftableCategoryCode, StringComparison.Ordinal) ||
                      string.Equals(cur, CraftableModsCategoryCode, StringComparison.Ordinal) ||
                      string.Equals(cur, CraftableWoodCategoryCode, StringComparison.Ordinal)))
                {
                    LastDialogPageCount = 0;
                    return;
                }


                int count;
                lock (CacheLock) count = CachedPageCodes.Count;
                if (count == LastDialogPageCount) return;
                LastDialogPageCount = count;

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

                    // Also add a forgiving code-only key (helps when attrs live on block variants)
                    var codeOnly = new StackKey(key.Code, "", "");
                    if (!map.ContainsKey(codeOnly)) map[codeOnly] = pc;
                }
            }
            catch { }
            return map;
        }

        private static Dictionary<StackKey, string> GetCachedPageCodeMap(ICoreClientAPI capi)
        {
            try
            {
                var msType = AccessTools.TypeByName("Vintagestory.GameContent.ModSystemSurvivalHandbook");
                var ms = msType != null ? GetModSystemByType(capi, msType) : null;
                var fiAllStacks = AccessTools.Field(msType, "allstacks");
                var arr = fiAllStacks?.GetValue(ms) as ItemStack[];
                lock (PageCodeMapLock)
                {
                    if (!ReferenceEquals(arr, AllStacksPageCodeMapSource) || AllStacksPageCodeMap.Count == 0)
                    {
                        AllStacksPageCodeMap = BuildPageCodeMapFromAllStacks(capi);
                        AllStacksPageCodeMapSource = arr;
                    }
                    return AllStacksPageCodeMap;
                }
            }
            catch
            {
                return AllStacksPageCodeMap;
            }
        }

        private static void InvalidatePageCodeMapCache()
        {
            lock (PageCodeMapLock)
            {
                AllStacksPageCodeMap.Clear();
                AllStacksPageCodeMapSource = null;
            }
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

                var coll = stack.Collectible;
                if (coll == null || coll.Code == null) return;

                var k = new Key { Code = coll.Code.ToString() };
                int addQty = Math.Max(1, stack.StackSize);

                // Accumulate quantity
                if (Counts.TryGetValue(k, out var cur)) Counts[k] = cur + addQty;
                else Counts[k] = addQty;

                // Record effective class once.
                // Treat carried blocks (ItemBlock stacks) as Block so block-typed wildcards can match.
                if (!Classes.ContainsKey(k))
                {
                    var eff = stack.Class;
                    if (eff == EnumItemClass.Item && stack.Block != null) eff = EnumItemClass.Block;
                    Classes[k] = eff;
                }
            }

            public string GetSignature()
            {
                var sb = new StringBuilder();
                foreach (var kv in Counts.OrderBy(k => k.Key.Code))
                {
                    var cls = Classes.TryGetValue(kv.Key, out var c) ? c : 0;
                    sb.Append(kv.Key.Code).Append(':').Append(kv.Value)
                      .Append(':').Append((int)cls).Append('|');
                }
                return sb.ToString();
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
                    if (!WildcardUtil.Match(pattern, code, allowed)) continue;

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
            public bool IsMod;
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

        private static List<GridRecipeShim> GetAllGridRecipes(ICoreClientAPI capi, out int fetched, out int usable, bool modsOnly)
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
                    if ((modsOnly && shim.IsMod) || (!modsOnly && !shim.IsMod))
                    {
                        usable++;
                        list.Add(shim);
                    }
                }
            }

            sw.Stop();
            LogEverywhere(capi, $"[Craftable] Grid recipes fetched={fetched}, usable={usable}, ms={sw.ElapsedMilliseconds}, gridMemberFound={gridMemberFound}, usedWorldList={usedGridRecipes}");
            return list;
        }

        private static string GetCachePath(ICoreClientAPI capi, bool modsOnly, bool woodOnly)
        {
            string basePath;
            try
            {
                var m = typeof(ICoreAPI).GetMethod("GetOrCreateDataPath", BindingFlags.Public | BindingFlags.Instance);
                basePath = m != null ? (string)m.Invoke(capi, new object[] { "ShowCraftable" }) : null;
            }
            catch { basePath = null; }
            if (string.IsNullOrEmpty(basePath))
                basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShowCraftable");
            Directory.CreateDirectory(basePath);
            string name = modsOnly ? "recipeindex_mods.bin" : (woodOnly ? "recipeindex_wood.bin" : "recipeindex_vanilla.bin");
            return Path.Combine(basePath, name);
        }


        private static bool LoadRecipeIndex(ICoreClientAPI capi, bool modsOnly, bool woodOnly)
        {
            try
            {
                var path = GetCachePath(capi, modsOnly, woodOnly);
                if (!File.Exists(path)) return false;
                RecipeIndexCache data;
                using (var fs = File.OpenRead(path)) data = Serializer.Deserialize<RecipeIndexCache>(fs);

                codeToRecipeGroups.Clear();
                recipeGroupNeeds.Clear();
                wildcardGroups.Clear();
                wildMatchCache.Clear();

                var recipes = new List<GridRecipeShim>();
                foreach (var cr in data.Recipes)
                {
                    var r = new GridRecipeShim();
                    foreach (var ci in cr.Ingredients)
                    {
                        var gi = new GridIngredientShim
                        {
                            IsTool = ci.IsTool,
                            IsWild = ci.IsWild,
                            QuantityRequired = ci.QuantityRequired,
                            PatternCode = ci.PatternCode != null ? new AssetLocation(ci.PatternCode) : null,
                            Allowed = ci.Allowed,
                            Type = ci.Type
                        };
                        if (ci.Options != null)
                        {
                            foreach (var b in ci.Options)
                            {
                                try
                                {
                                    var st = new ItemStack(b);
                                    st.ResolveBlockOrItem(capi.World);
                                    gi.Options.Add(st);
                                }
                                catch { }
                            }
                        }
                        r.Ingredients.Add(gi);
                        if (gi.IsWild && gi.PatternCode != null)
                        {
                            var gkey = $"wild:{gi.PatternCode}|{string.Join(",", (gi.Allowed ?? Array.Empty<string>()).OrderBy(x => x))}|T:{gi.Type}";
                            wildcardGroups.Add(new WildGroup { Recipe = r, GroupKey = gkey, Type = gi.Type, Pattern = gi.PatternCode, Allowed = gi.Allowed });
                        }
                    }
                    if (cr.Outputs != null)
                    {
                        foreach (var b in cr.Outputs)
                        {
                            try
                            {
                                var st = new ItemStack(b);
                                st.ResolveBlockOrItem(capi.World);
                                r.Outputs.Add(st);
                            }
                            catch { }
                        }
                    }
                    recipes.Add(r);
                    recipeGroupNeeds[r] = cr.Needs ?? new Dictionary<string, int>(StringComparer.Ordinal);
                }

                foreach (var kv in data.CodeToRecipes)
                {
                    var list = new List<(GridRecipeShim Recipe, string GroupKey)>();
                    foreach (var rref in kv.Value)
                    {
                        if (rref.Recipe >= 0 && rref.Recipe < recipes.Count)
                            list.Add((recipes[rref.Recipe], rref.GroupKey));
                    }
                    codeToRecipeGroups[kv.Key] = list;
                }
                codeToGkeys.Clear();
                if (data.CodeToGkeys != null && data.CodeToGkeys.Count > 0)
                {
                    foreach (var kv in data.CodeToGkeys)
                        codeToGkeys[kv.Key] = new HashSet<string>(kv.Value ?? new List<string>(), StringComparer.Ordinal);
                }
                else
                {
                    // Back-compat: derive set of gkeys per code from (code -> (recipe,gkey))
                    foreach (var kv in codeToRecipeGroups)
                    {
                        if (!codeToGkeys.TryGetValue(kv.Key, out var gset))
                            codeToGkeys[kv.Key] = gset = new HashSet<string>(StringComparer.Ordinal);
                        foreach (var pair in kv.Value)
                            gset.Add(pair.GroupKey);
                    }
                }


                recipeIndexBuilt = true;
                return true;
            }
            catch { return false; }
        }

        private static void StartRecipeIndexBuild(ICoreClientAPI capi, bool modsOnly, bool woodOnly)
        {
            if (recipeIndexBuilt && recipeIndexForMods == modsOnly && recipeIndexForWoodOnly == woodOnly) return;
            if (recipeIndexBuildTask != null && !recipeIndexBuildTask.IsCompleted)
            {
                recipeIndexBuildTask.ContinueWith(_ => StartRecipeIndexBuild(capi, modsOnly, woodOnly));
                return;
            }
            recipeIndexBuilt = false;
            lock (CacheLock) { CachedPageCodes.Clear(); ScanResultsCache.Clear(); }
            recipeIndexBuildTask = Task.Run(() =>
            {
                if (!LoadRecipeIndex(capi, modsOnly, woodOnly)) BuildRecipeIndex(capi, modsOnly, woodOnly);
                recipeIndexBuilt = true;
                recipeIndexForMods = modsOnly;
                recipeIndexForWoodOnly = woodOnly;
                GetCachedPageCodeMap(capi);
            });
        }
        private static void BuildRecipeIndex(ICoreClientAPI capi, bool modsOnly, bool woodOnly)
        {
            var sw = Stopwatch.StartNew();
            codeToRecipeGroups.Clear();
            recipeGroupNeeds.Clear();
            codeToGkeys.Clear();

            var recipes = GetAllGridRecipes(capi, out recipesFetched, out recipesUsable, modsOnly);
            if (woodOnly)
                recipes = recipes.Where(r => !r.IsMod && IsWoodRecipe(r.Raw)).ToList();
            else if (!modsOnly)
                recipes = recipes.Where(r => !r.IsMod && !IsWoodRecipe(r.Raw)).ToList();

            recipeIndexBuildTotal = recipes.Count;
            recipeIndexBuildProgress = 0;

            var wildCache = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            IEnumerable<string> GetWildMatches(GridIngredientShim ing)
            {
                if (ing == null || ing.PatternCode == null) return Array.Empty<string>();

                var allowed = ing.Allowed ?? Array.Empty<string>();
                string sig = $"wild:{ing.PatternCode}|{string.Join(",", allowed.OrderBy(x => x))}|T:{ing.Type}";

                if (wildCache.TryGetValue(sig, out var cached)) return cached;

                var results = new List<string>();

                IEnumerable<CollectibleObject> coll = null;
                if (ing.Type == EnumItemClass.Item) coll = capi.World.Items?.Where(i => i != null);
                else if (ing.Type == EnumItemClass.Block) coll = capi.World.Blocks?.Where(b => b != null);

                if (coll != null)
                {
                    foreach (var c in coll)
                    {
                        var code = c?.Code?.ToString();
                        if (string.IsNullOrEmpty(code)) continue;
                        try
                        {
                            var al = new AssetLocation(code);
                            if (WildcardUtil.Match(ing.PatternCode, al, allowed)) results.Add(code);
                        }
                        catch { /* best-effort */ }
                    }
                }

                var distinct = results.Distinct(StringComparer.Ordinal).ToList();
                wildCache[sig] = distinct;
                return distinct;
            }

            foreach (var r in recipes)
            {
                var groups = new Dictionary<string, int>(StringComparer.Ordinal);

                foreach (var ing in r.Ingredients)
                {
                    if (ing == null) continue;

                    string groupKey;
                    IEnumerable<string> satisfiableCodes;

                    if (ing.IsWild)
                    {
                        var allowed = ing.Allowed ?? Array.Empty<string>();
                        groupKey = $"wild:{ing.PatternCode}|{string.Join(",", allowed.OrderBy(x => x))}|T:{ing.Type}";
                        satisfiableCodes = GetWildMatches(ing);   
                    }
                    else
                    {
                        var codes = ing.Options
                            .Select(st => st?.Collectible?.Code?.ToString())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Distinct(StringComparer.Ordinal);
                        groupKey = string.Join("|", codes.OrderBy(s => s));
                        satisfiableCodes = codes;
                    }

                    int qty = Math.Max(1, ing.QuantityRequired);
                    if (ing.IsTool) qty = 1;
                    if (!groups.TryGetValue(groupKey, out var cur)) cur = 0;
                    groups[groupKey] = cur + qty;

                    foreach (var code in satisfiableCodes)
                    {
                        if (string.IsNullOrEmpty(code)) continue;
                        if (!codeToRecipeGroups.TryGetValue(code, out var list))
                            codeToRecipeGroups[code] = list = new List<(GridRecipeShim Recipe, string GroupKey)>();
                        if (!list.Any(p => ReferenceEquals(p.Recipe, r) && p.GroupKey == groupKey))
                            list.Add((r, groupKey));
                    }

                    // Also fill the code -> gkeys index (recipe-agnostic)
                    foreach (var code in satisfiableCodes)
                    {
                        if (string.IsNullOrEmpty(code)) continue;
                        if (!codeToGkeys.TryGetValue(code, out var gset))
                            codeToGkeys[code] = gset = new HashSet<string>(StringComparer.Ordinal);
                        gset.Add(groupKey);
                    }
                }

                recipeGroupNeeds[r] = groups;
                recipeIndexBuildProgress++;
            }

            sw.Stop();
            long elapsedMs = sw.ElapsedMilliseconds;
            capi.Event.EnqueueMainThreadTask(() =>
                LogEverywhere(capi, $"[Craftable] BuildRecipeIndex took {elapsedMs}ms"), null);
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

            var nameAl = TryGetMember(t, raw, "Name") as AssetLocation;
            if (nameAl != null)
            {
                shim.IsMod = !string.Equals(nameAl.Domain, "game", StringComparison.Ordinal);
            }
            else
            {
                shim.IsMod = shim.Outputs.Any(o => o?.Collectible?.Code != null &&
                    !string.Equals(o.Collectible.Code.Domain, "game", StringComparison.Ordinal));
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

        private static ResourcePool ClonePool(ResourcePool pool)
        {
            var res = new ResourcePool();
            foreach (var kv in pool.Counts)
            {
                res.Counts[new Key { Code = kv.Key.Code }] = kv.Value;
            }
            foreach (var kv in pool.Classes)
            {
                res.Classes[new Key { Code = kv.Key.Code }] = kv.Value;
            }
            return res;
        }

        private static bool RecipeSatisfiedByPool(ICoreClientAPI capi, ResourcePool pool, GridRecipe recipe, ItemStack desired)
        {
            var shim = TryBuildGridShim(recipe, capi);
            if (shim == null) return false;

            string target = desired == null ? null :
                ((desired.Collectible?.Code?.ToString() ?? "") + " " + ((desired.Attributes as TreeAttribute)?.ToJsonToken() ?? ""));

            var temp = ClonePool(pool);
            foreach (var ing in shim.Ingredients)
            {
                if (ing == null) continue;
                if (ing.IsWild)
                {
                    string[] allowed = ing.Allowed;
                    if (target != null && allowed != null && allowed.Length > 0)
                    {
                        string match = allowed.FirstOrDefault(v => target.Contains(v));
                        if (match != null) allowed = new[] { match };
                    }
                    if (!temp.TryConsumeWildcard(ing.Type, ing.PatternCode, allowed, Math.Max(1, ing.QuantityRequired), true))
                        return false;
                }
                else
                {
                    if (!temp.TryConsumeAny(ing.Options, Math.Max(1, ing.QuantityRequired), true))
                        return false;
                }
            }

            if (desired != null)
            {
                bool match = false;
                foreach (var st in shim.Outputs)
                {
                    if (st != null && st.Satisfies(desired) && desired.Satisfies(st)) { match = true; break; }
                }
                if (!match) return false;
            }

            return true;
        }

        // New overload: filter by mod origin (modsOnly==true => only mod recipes; false => only vanilla; null => all)
        private static List<GridRecipe> CollectGridRecipesForStack(ICoreClientAPI capi, ItemStack stack, bool? modsOnly)
        {
            var list = new List<GridRecipe>();
            try
            {
                foreach (var gr in capi.World.GridRecipes)
                {
                    if (!gr.ShowInCreatedBy) continue;

                    // Filter by recipe origin if requested
                    if (modsOnly.HasValue)
                    {
                        bool isMod = IsModRecipe(capi, gr);
                        if (modsOnly.Value != isMod) continue;
                    }

                    // Keep your existing matching logic
                    var outStack = gr.Output?.ResolvedItemstack;
                    if (outStack != null && outStack.Satisfies(stack))
                    {
                        list.Add(gr);
                        continue;
                    }

                    var ingreds = gr.resolvedIngredients?.ToArray();
                    if (ingreds == null) continue;

                    foreach (var ing in ingreds)
                    {
                        var ret = ing?.ReturnedStack?.ResolvedItemstack;
                        if (ret != null && ret.Satisfies(stack))
                        {
                            var resStack = ing.ResolvedItemstack;
                            if (resStack != null && !resStack.Satisfies(stack))
                            {
                                list.Add(gr);
                                break;
                            }
                        }
                    }
                }
            }
            catch { /* best-effort */ }

            return list;
        }

        // Back-compat shim (null = no filter, same behavior as before)
        private static List<GridRecipe> CollectGridRecipesForStack(ICoreClientAPI capi, ItemStack stack)
            => CollectGridRecipesForStack(capi, stack, null);


        // Choose your default here (or wire to a config)
        private const int DefaultAllStacksPartitions = 8;

        // Backward-compatible entrypoint
        private static void AddCraftablePagesFromAllStacks(ICoreClientAPI capi, ResourcePool pool, HashSet<string> dest)
        {
            AddCraftablePagesFromAllStacks(capi, pool, dest, DefaultAllStacksPartitions);
        }

        // Parallelizable overload (you control `partitions`)
        private static void AddCraftablePagesFromAllStacks(ICoreClientAPI capi, ResourcePool pool, HashSet<string> dest, int partitions)
        {
            try
            {
                // Snapshot handbook stacks once
                var msType = AccessTools.TypeByName("Vintagestory.GameContent.ModSystemSurvivalHandbook");
                var ms = msType != null ? GetModSystemByType(capi, msType) : null;
                var stacks = AccessTools.Field(msType, "allstacks")?.GetValue(ms) as ItemStack[];
                if (stacks == null || stacks.Length == 0) return;

                // Reflect once per call
                var ghType = AccessTools.TypeByName("Vintagestory.GameContent.GuiHandbookItemStackPage");
                var ctor = ghType?.GetConstructor(new[] { typeof(ICoreClientAPI), typeof(ItemStack) });
                var fiStack = AccessTools.Field(ghType, "Stack");
                var miPageCode = ghType?.GetMethod("PageCodeForStack", BindingFlags.Public | BindingFlags.Static);
                if (ctor == null || miPageCode == null) return;

                var swTotal = Stopwatch.StartNew();

                // Normalize partitions
                if (partitions < 1) partitions = 1;
                if (partitions == 1 || stacks.Length < 2)
                {
                    // Serial path (same logic as before)
                    for (int i = 0; i < stacks.Length; i++)
                    {
                        var st = stacks[i];
                        if (st?.Collectible == null) continue;

                        object page = ctor.Invoke(new object[] { capi, st });
                        var pStack = fiStack?.GetValue(page) as ItemStack ?? st;

                        var recipes = CollectGridRecipesForStack(capi, pStack, modsOnly: false);
                        foreach (var r in recipes)
                        {
                            if (IsWoodRecipe(r)) continue;
                            if (RecipeSatisfiedByPool(capi, pool, r, pStack))
                            {
                                var pc = miPageCode.Invoke(null, new object[] { pStack }) as string;
                                if (!string.IsNullOrEmpty(pc)) dest.Add(pc);
                                break;
                            }
                        }
                    }

                    swTotal.Stop();
                    LogEverywhere(capi, $"[Craftable] Partitioned scan processed {stacks.Length} stacks serially in {swTotal.ElapsedMilliseconds}ms");
                    return;
                }

                partitions = Math.Min(partitions, stacks.Length);

                // Evenly distribute remainder across the first `extra` partitions
                int baseSize = stacks.Length / partitions;
                int extra = stacks.Length % partitions;

                var tasks = new Task<HashSet<string>>[partitions];
                int offset = 0;

                for (int p = 0; p < partitions; p++)
                {
                    int len = baseSize + (p < extra ? 1 : 0);
                    int sliceStart = offset;
                    int sliceEnd = sliceStart + len;
                    offset = sliceEnd;
                    int partIndex = p;
                    int localLen = len;

                    tasks[p] = Task.Run(() =>
                    {
                        var swPart = Stopwatch.StartNew();
                        var local = new HashSet<string>(StringComparer.Ordinal);

                        for (int i = sliceStart; i < sliceEnd; i++)
                        {
                            var st = stacks[i];
                            if (st?.Collectible == null) continue;

                            object page = ctor.Invoke(new object[] { capi, st });
                            var pStack = fiStack?.GetValue(page) as ItemStack ?? st;

                            var recipes = CollectGridRecipesForStack(capi, pStack, modsOnly: false);
                            foreach (var r in recipes)
                            {
                                if (IsWoodRecipe(r)) continue;
                                if (RecipeSatisfiedByPool(capi, pool, r, pStack))
                                {
                                    var pc = miPageCode.Invoke(null, new object[] { pStack }) as string;
                                    if (!string.IsNullOrEmpty(pc)) local.Add(pc);
                                    break;
                                }
                            }
                        }

                        swPart.Stop();
                        LogEverywhere(capi, $"[Craftable] Partition {partIndex + 1}/{partitions} processed {localLen} stacks in {swPart.ElapsedMilliseconds}ms");
                        return local;
                    });
                }

                Task.WaitAll(tasks);

                swTotal.Stop();
                LogEverywhere(capi, $"[Craftable] Partitioned scan processed {stacks.Length} stacks in {swTotal.ElapsedMilliseconds}ms using {partitions} partitions");

                // Merge results back into caller-owned set
                foreach (var t in tasks)
                    foreach (var pc in t.Result)
                        dest.Add(pc);
            }
            catch
            {
                // best-effort, match your existing error handling style
            }
        }



        // Identical workflow, but ONLY considers mod recipes
        private static void AddCraftablePagesFromAllStacksFromModStacks(ICoreClientAPI capi, ResourcePool pool, HashSet<string> dest)
        {
            try
            {
                var msType = AccessTools.TypeByName("Vintagestory.GameContent.ModSystemSurvivalHandbook");
                var ms = msType != null ? GetModSystemByType(capi, msType) : null;
                var stacks = AccessTools.Field(msType, "allstacks")?.GetValue(ms) as ItemStack[];
                if (stacks == null || stacks.Length == 0) return;

                var ghType = AccessTools.TypeByName("Vintagestory.GameContent.GuiHandbookItemStackPage");
                var ctor = ghType?.GetConstructor(new[] { typeof(ICoreClientAPI), typeof(ItemStack) });
                var fiStack = AccessTools.Field(ghType, "Stack");
                var miPageCode = ghType?.GetMethod("PageCodeForStack", BindingFlags.Public | BindingFlags.Static);
                if (ctor == null || miPageCode == null) return;

                foreach (var st in stacks)
                {
                    if (st?.Collectible == null) continue;

                    object page = ctor.Invoke(new object[] { capi, st });
                    var pStack = fiStack?.GetValue(page) as ItemStack ?? st;

                    // ONLY MOD recipes here
                    var recipes = CollectGridRecipesForStack(capi, pStack, modsOnly: true);

                    foreach (var r in recipes)
                    {
                        if (RecipeSatisfiedByPool(capi, pool, r, pStack))
                        {
                            var pc = miPageCode.Invoke(null, new object[] { pStack }) as string;
                            if (!string.IsNullOrEmpty(pc)) dest.Add(pc);
                            break;
                        }
                    }
                }
            }
            catch { /* best-effort */ }
        }

        private static void AddCraftablePagesFromAllStacks_WoodOnly(ICoreClientAPI capi, ResourcePool pool, HashSet<string> dest)
        {
            try
            {
                var msType = AccessTools.TypeByName("Vintagestory.GameContent.ModSystemSurvivalHandbook");
                var ms = msType != null ? GetModSystemByType(capi, msType) : null;
                var stacks = AccessTools.Field(msType, "allstacks")?.GetValue(ms) as ItemStack[];
                if (stacks == null || stacks.Length == 0) return;

                var ghType = AccessTools.TypeByName("Vintagestory.GameContent.GuiHandbookItemStackPage");
                var ctor = ghType?.GetConstructor(new[] { typeof(ICoreClientAPI), typeof(ItemStack) });
                var fiStack = AccessTools.Field(ghType, "Stack");
                var miPageCode = ghType?.GetMethod("PageCodeForStack", BindingFlags.Public | BindingFlags.Static);
                if (ctor == null || miPageCode == null) return;

                foreach (var st in stacks)
                {
                    if (st?.Collectible == null) continue;
                    object page = ctor.Invoke(new object[] { capi, st });
                    var pStack = fiStack?.GetValue(page) as ItemStack ?? st;

                    // vanilla only, then wood only
                    var recipes = CollectGridRecipesForStack(capi, pStack, modsOnly: false);
                    foreach (var r in recipes)
                    {
                        if (!IsWoodRecipe(r)) continue;
                        if (RecipeSatisfiedByPool(capi, pool, r, pStack))
                        {
                            var pc = miPageCode.Invoke(null, new object[] { pStack }) as string;
                            if (!string.IsNullOrEmpty(pc)) dest.Add(pc);
                            break;
                        }
                    }
                }
            }
            catch { /* best-effort */ }
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
                string sigPrefix = recipeIndexForMods ? "mods|" : (recipeIndexForWoodOnly ? "wood|" : "van|");
                string sig = sigPrefix + pool.GetSignature();

                List<string> cached;
                bool reused = false;
                lock (CacheLock)
                {
                    if (ScanResultsCache.TryGetValue(sig, out cached))
                    {
                        CachedPageCodes = cached.ToList();
                        reused = true;
                    }
                }

                if (reused)
                {
                    _capi.Event.EnqueueMainThreadTask(() =>
                    {
                        LogEverywhere(_capi, $"[Craftable] Server nearby scan reused cache: pages={CachedPageCodes.Count}", toChat: true);
                        TryRefreshOpenDialog(_capi);
                        SetUpdatingText(_capi, false);
                    }, null);

                    ScanInProgress = false;
                    HandbookPauseGuard.Release(_capi);
                    return;
                }

                Task.Run(() =>
                {
                    try
                    {
                        int pages = RebuildCacheWithPool(_capi, pool, out int outputs, out int fetched, out int usable);
                        lock (CacheLock) ScanResultsCache[sig] = CachedPageCodes.ToList();
                        _capi.Event.EnqueueMainThreadTask(() => LogEverywhere(_capi, $"[Craftable] Server nearby scan merged: outputs={outputs}, pages={pages}, fetched={fetched}, usable={usable}", toChat: true), null);
                    }
                    catch (Exception e)
                    {
                        _capi.Event.EnqueueMainThreadTask(() => LogEverywhere(_capi, $"[Craftable] OnServerScanReply error: {e}", toChat: true), null);
                    }
                    finally
                    {
                        ScanInProgress = false;
                        HandbookPauseGuard.Release(_capi);
                        _capi.Event.EnqueueMainThreadTask(() => SetUpdatingText(_capi, false), null);
                    }
                });
            }
            catch (Exception e)
            {
                LogEverywhere(_capi, $"[Craftable] OnServerScanReply error: {e}", toChat: true);
                ScanInProgress = false;
                HandbookPauseGuard.Release(_capi);
                _capi.Event.EnqueueMainThreadTask(() => SetUpdatingText(_capi, false), null);
            }
        }


        private static int RebuildCacheWithPool(
    ICoreClientAPI capi, ResourcePool pool,
    out int craftableOutputsCount, out int fetched, out int usable)
        {
            var sw = Stopwatch.StartNew();
            craftableOutputsCount = 0; fetched = recipesFetched; usable = recipesUsable;

            // Clone original needs so we can compute per-recipe "remaining" without touching the canonical map
            var candidates = new HashSet<GridRecipeShim>(ReferenceEqualityComparer.Instance);
            foreach (var pkv in pool.Counts)
            {
                var code = pkv.Key.Code;
                if (codeToRecipeGroups.TryGetValue(code, out var uses))
                    foreach (var (r, _) in uses) candidates.Add(r);
            }

            var remaining = new Dictionary<GridRecipeShim, Dictionary<string, int>>(ReferenceEqualityComparer.Instance);
            foreach (var r in candidates)
                remaining[r] = recipeGroupNeeds[r].ToDictionary(g => g.Key, g => g.Value, StringComparer.Ordinal);

            // --- Helpers -------------------------------------------------------------
            static bool TryParseGroupType(string gkey, out EnumItemClass need)
            {
                need = default;
                if (gkey == null) return false;
                // Wild gkeys are like: wild:{pattern}|{allowed_csv}|T:{Type}
                int tpos = gkey.LastIndexOf("|T:", StringComparison.Ordinal);
                if (!gkey.StartsWith("wild:", StringComparison.Ordinal) || tpos < 0) return false;
                var t = gkey.Substring(tpos + 3);
                if (Enum.TryParse<EnumItemClass>(t, out var parsed)) { need = parsed; return true; }
                return false;
            }

            bool ClassCompatible(EnumItemClass have, EnumItemClass need, AssetLocation code)
            {
                if (have == need) return true;
                if (need == EnumItemClass.Block && have == EnumItemClass.Item)
                    return capi.World.GetBlock(code) != null; // carried block (ItemBlock)
                if (need == EnumItemClass.Item && have == EnumItemClass.Block)
                    return capi.World.GetItem(code) != null;  // item form exists
                return false;
            }
            // ------------------------------------------------------------------------

            // Pass A: aggregate total availability per group key from the pool
            var groupAvail = new Dictionary<string, int>(StringComparer.Ordinal);

            // Also build gkey -> codes (limited to codes we actually have in the pool). This powers the overlap detector.
            // NEW Pass A': subtract per code + detect ambiguity per code.
            // We no longer build groupAvail or gkey->codes. We subtract straight into `remaining`.
            var ambRecipes = new HashSet<GridRecipeShim>(ReferenceEqualityComparer.Instance);

            foreach (var pkv in pool.Counts)
            {
                var code = pkv.Key.Code;
                var haveQty = pkv.Value;
                if (haveQty <= 0) continue;

                if (!codeToRecipeGroups.TryGetValue(code, out var uses) || uses == null || uses.Count == 0)
                    continue;

                // For this specific code, track how many distinct gkeys each recipe hit.
                var hitsPerRecipe = new Dictionary<GridRecipeShim, int>(ReferenceEqualityComparer.Instance);

                foreach (var (recipe, gkey) in uses)
                {
                    if (!remaining.TryGetValue(recipe, out var groups)) continue;
                    if (!groups.TryGetValue(gkey, out var need) || need <= 0) continue;

                    int take = Math.Min(need, haveQty);
                    if (take > 0) groups[gkey] = need - take;

                    // Ambiguity = this code satisfies >=2 distinct gkeys in the SAME recipe
                    hitsPerRecipe[recipe] = (hitsPerRecipe.TryGetValue(recipe, out var c) ? c : 0) + 1;
                }

                foreach (var kv2 in hitsPerRecipe)
                    if (kv2.Value >= 2) ambRecipes.Add(kv2.Key);
            }

            // No Pass B, no RecipeIsAmbiguous() needed anymore.

            bool CanSatisfyPrecisely_NoGkeyToCodes(GridRecipeShim recipe, ResourcePool poolLocal)
            {
                if (recipe == null) return false;

                // Local copy (code -> qty). Start from pool counts so we don't rebuild a filtered map first.
                var codeAvail = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var pkv in poolLocal.Counts) codeAvail[pkv.Key.Code] = pkv.Value;

                foreach (var gkv in recipeGroupNeeds[recipe])
                {
                    int need = gkv.Value;
                    if (need <= 0) continue;

                    IEnumerable<string> candidates;
                    if (gkv.Key.StartsWith("wild:", StringComparison.Ordinal))
                    {
                        // Filter *only* the player's codes by membership in this gkey to keep it small
                        candidates = codeAvail.Keys.Where(c =>
                            codeToGkeys.TryGetValue(c, out var set) && set != null && set.Contains(gkv.Key));
                    }
                    else
                    {
                        // Exact group: groupKey is a '|'-joined code list
                        candidates = gkv.Key.Split('|').Where(c => codeAvail.ContainsKey(c));
                    }

                    int taken = 0;
                    foreach (var c in candidates)
                    {
                        int have = codeAvail.TryGetValue(c, out var v) ? v : 0;
                        if (have <= 0) continue;
                        int take = Math.Min(need - taken, have);
                        if (take > 0)
                        {
                            codeAvail[c] = have - take;
                            taken += take;
                            if (taken >= need) break;
                        }
                    }

                    if (taken < need) return false;
                }

                return true;
            }


            // Pass C: collect craftable outputs; validate only ambiguous ones.
            var craftableKeys = new HashSet<StackKey>();
            foreach (var kv in remaining)
            {
                var recipe = kv.Key;
                var groups = kv.Value;

                bool prelimOk = groups.Count > 0 && groups.All(g => g.Value <= 0);
                if (!prelimOk) continue;

                bool ok = !ambRecipes.Contains(recipe) ? true : CanSatisfyPrecisely_NoGkeyToCodes(recipe, pool);
                if (!ok) continue;

                ExpandOutputsForRecipe(capi, pool, recipe, craftableKeys, recipeGroupNeeds);
            }


            craftableOutputsCount = craftableKeys.Count;

            // --- page-code resolution remains identical to your original code ---
            var key2page = GetCachedPageCodeMap(capi);
            var resultPageCodes = new HashSet<string>(StringComparer.Ordinal);
            var ghType = AccessTools.TypeByName("Vintagestory.GameContent.GuiHandbookItemStackPage");
            var miPageCodeForStack = ghType?.GetMethod("PageCodeForStack", BindingFlags.Public | BindingFlags.Static);

            int fromMap = 0;
            int attrFallbacks = 0;
            int codeOnlyFallbacks = 0;
            int hbStackFallbacks = 0;

            const int chunkSize = 64;
            int processed = 0;

            void Flush()
            {
                lock (CacheLock) CachedPageCodes = resultPageCodes.ToList();
                capi.Event.EnqueueMainThreadTask(() => TryRefreshOpenDialog(capi), null);
            }

            foreach (var key in craftableKeys)
            {
                string pageCode = null;
                bool found;

                lock (PageCodeMapLock)
                {
                    found = key2page.TryGetValue(key, out pageCode);
                }

                if (found)
                {
                    resultPageCodes.Add(pageCode);
                    fromMap++;
                }
                else if (miPageCodeForStack != null)
                {
                    try
                    {
                        var st = KeyToItemStack(capi, key);
                        if (st != null)
                        {
                            pageCode = (string)miPageCodeForStack.Invoke(null, new object[] { st });
                            if (!string.IsNullOrEmpty(pageCode)) resultPageCodes.Add(pageCode);
                            else hbStackFallbacks++;
                        }
                    }
                    catch
                    {
                        hbStackFallbacks++;
                    }
                }

                processed++;
                if (processed % chunkSize == 0) Flush();
            }

            // compute how many outputs didn’t resolve to a page via the fast map/fallbacks
            int misses = craftableKeys.Count - (fromMap + attrFallbacks + codeOnlyFallbacks);

            if (misses > 0 && misses <= 42)
            {
                if (recipeIndexForMods)
                    AddCraftablePagesFromAllStacksFromModStacks(capi, pool, resultPageCodes);
                else if (recipeIndexForWoodOnly)
                    AddCraftablePagesFromAllStacks_WoodOnly(capi, pool, resultPageCodes);
                else
                    AddCraftablePagesFromAllStacks(capi, pool, resultPageCodes);
            }



            craftableOutputsCount = resultPageCodes.Count;
            Flush();

            sw.Stop();

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
                    if (WildcardUtil.Match(pattern, code, allowed)) return true;
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
                            if (!WildcardUtil.Match(pattern, al, allowed)) continue;
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
                        if (!WildcardUtil.Match(pattern, al, allowed)) continue;
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
                    var cls = (st.Class == EnumItemClass.Item && st.Block != null) ? EnumItemClass.Block : st.Class;


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
                            var cls = (st.Class == EnumItemClass.Item && st.Block != null) ? EnumItemClass.Block : st.Class;


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
