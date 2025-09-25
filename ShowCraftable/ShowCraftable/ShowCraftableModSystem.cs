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
using System.Runtime.CompilerServices;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using ProtoBuf;
using ClientGuiComposerHelpers = Vintagestory.API.Client.GuiComposerHelpers;

namespace ShowCraftable
{
    public class ShowCraftableSystem : ModSystem
    {
        private Harmony _harmony;
        private ICoreClientAPI _capi;
        public const string HarmonyId = "showcraftable.core";
        internal static bool DebugEnabled = false;
        internal static int ConfiguredSearchRadius => Math.Max(0, Config?.SearchDistanceItems ?? 20);
        internal static int ConfiguredAllStacksPartitions => Math.Max(-1, Config?.AllStacksPartitions ?? -1);
        internal static bool UseDefaultFont => Config?.UseDefaultFont ?? false;
        private const string ConfigFileName = "ShowCraftable.json";
        private const string AllTabKeyName = "allTab";
        private const string CraftableTabKeyName = "craftableTab";
        private const string ModTabKeyName = "modTab";
        private const string StoneTabKeyName = "stoneTab";
        private const string WoodTabKeyName = "woodTab";
        private const string CraftableAllTabDisplayName = "Craftable";
        private const string BaseItemsTabDisplayName = "● Base Items";
        private const string WoodTypesTabDisplayName = "● Wood Types";
        private const string StoneTypesTabDisplayName = "● Stone Types";
        private const string ModItemsTabDisplayName = "● Mod Items";
        private const string ArialFontName = "Arial";
        private const string ArialBlackFontName = "Arial Black";
        private const int SlowStackLogThresholdMs = 175;
        private static bool ScanQueueCheckScheduled;
        private static Dictionary<GridRecipeShim, Dictionary<string, int>> recipeGroupNeeds = new();
        private static Dictionary<StackKey, List<GridRecipeShim>> outputsIndex = new();
        private static Dictionary<StackKey, string> AllStacksPageCodeMap = new();
        private static Dictionary<string, HashSet<string>> codeToGkeys = new(StringComparer.Ordinal);
        private static Dictionary<string, List<(GridRecipeShim Recipe, string GroupKey)>> codeToRecipeGroups = new();
        private static Dictionary<string, List<(GridRecipeShim Recipe, string GroupKey)>> wildMatchCache = new(StringComparer.Ordinal);
        private static ICoreClientAPI _staticCapi;
        private static int LastDialogPageCount;
        private static int NearbyRadius = 20;
        private static int recipesFetched;
        private static int recipesUsable;
        private static int ScanSeq;
        private static int _pendingScanId;
        private static ItemStack[] AllStacksPageCodeMapSource;
        private static List<string> CachedPageCodes = new();
        private static List<string> AllTabCache = new();
        private static List<string> CraftableTabCache = new();
        private static List<string> ModTabCache = new();
        private static List<string> s_EmptyPages;
        private static List<string> StoneTypeTabCache = new();
        private static List<string> WoodTypeTabCache = new();
        private static List<WildGroup> wildcardGroups = new();
        private static readonly Dictionary<int, ScanRequestInfo> InflightById = new();
        private static readonly Dictionary<string, bool> TabReadyToUpdateUi = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Dictionary<string, int>> WildTokenCountsMemo = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, RecipeIndexData> recipeIndexByVariant = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, ulong> TabPoolDNA = new(StringComparer.Ordinal);
        private static readonly object CacheLock = new();
        private static readonly object DnaLock = new();
        private static readonly object InflightMapLock = new();
        private static readonly object LogFileLock = new();
        private static readonly object PageCodeMapLock = new();
        private static readonly object PendingScanLock = new();
        private static readonly object ScanQueueLock = new();
        private static readonly object TabUiStateLock = new();
        private static ScanRequestInfo? QueuedScanRequest;
        private static ShowCraftableConfig Config = new();
        private static string PendingScanTabKey;
        private static string PendingScanVariantKey;
        private static Task recipeIndexBuildTask;
        private static int recipeIndexBuildToken;
        private static volatile bool recipeIndexBuildStarted = false;
        private static volatile bool CraftableAllTabActive;
        private static volatile bool CraftableModsTabActive;
        private static volatile bool CraftableStoneTabActive;
        private static volatile bool CraftableTabActive;
        private static volatile bool CraftableWoodTabActive;
        private static volatile bool recipeIndexBuilt = false;
        private static volatile bool recipeIndexForMods = false;
        private static volatile bool recipeIndexForStoneOnly;
        private static volatile bool recipeIndexForWoodOnly;
        private static volatile bool ScanInProgress = false;
        private static int FetchInProgressFlag = 0;
        private static int ServerFetchDisabledFlag = 0;
        private static volatile int recipeIndexBuildProgress;
        private static volatile int recipeIndexBuildTotal;
        public const string ChannelName = "showcraftablescan";
        public const string CraftableAllCategoryCode = "craftableall";
        public const string CraftableCategoryCode = "craftable";
        public const string CraftableModsCategoryCode = "craftablemods";
        public const string CraftableStoneCategoryCode = "craftablestonetypes";
        public const string CraftableWoodCategoryCode = "craftablewoodtypes";

        internal static bool IsCraftableCategoryCode(string categoryCode)
        {
            if (string.IsNullOrEmpty(categoryCode)) return false;

            return string.Equals(categoryCode, CraftableAllCategoryCode, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(categoryCode, CraftableCategoryCode, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(categoryCode, CraftableModsCategoryCode, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(categoryCode, CraftableWoodCategoryCode, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(categoryCode, CraftableStoneCategoryCode, StringComparison.OrdinalIgnoreCase);
        }

        internal static string GetCraftableTabFontName(string categoryCode)
        {
            if (UseDefaultFont) return null;

            if (string.Equals(categoryCode, CraftableAllCategoryCode, StringComparison.OrdinalIgnoreCase)) return ArialBlackFontName;

            if (string.Equals(categoryCode, CraftableCategoryCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(categoryCode, CraftableModsCategoryCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(categoryCode, CraftableWoodCategoryCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(categoryCode, CraftableStoneCategoryCode, StringComparison.OrdinalIgnoreCase))
            {
                return ArialFontName;
            }

            return null;
        }

        internal static FontWeight? GetCraftableTabFontWeight(string categoryCode)
        {
            if (UseDefaultFont) return null;

            if (string.Equals(categoryCode, CraftableCategoryCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(categoryCode, CraftableModsCategoryCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(categoryCode, CraftableWoodCategoryCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(categoryCode, CraftableStoneCategoryCode, StringComparison.OrdinalIgnoreCase))
            {
                return FontWeight.Bold;
            }

            return null;
        }

        internal static bool IsFetchButtonEnabled()
        {
            return Config?.EnableFetchButton == true && !IsServerFetchDisabled();
        }

        internal static bool IsFetchButtonEnabledInConfig()
        {
            return Config?.EnableFetchButton == true;
        }

        internal static bool IsServerFetchDisabled()
        {
            return Volatile.Read(ref ServerFetchDisabledFlag) != 0;
        }

        internal static void SetServerFetchDisabled(bool disabled)
        {
            Interlocked.Exchange(ref ServerFetchDisabledFlag, disabled ? 1 : 0);
        }

        internal static void SetServerFetchDisabledAndSave(ICoreAPI api, bool disabled)
        {
            SetServerFetchDisabled(disabled);
            if (Config != null)
            {
                Config.DisableFetchButtonOnServer = disabled;
                SaveConfig(api);
            }
        }

        [ProtoContract]
        private class CachedIngredient
        {
            [ProtoMember(1)] public bool IsTool = false;
            [ProtoMember(2)] public bool IsWild = false;
            [ProtoMember(3)] public int QuantityRequired = 0;
            [ProtoMember(4)] public List<byte[]> Options = new();
            [ProtoMember(5)] public string PatternCode = null;
            [ProtoMember(6)] public string[] Allowed = null;
            [ProtoMember(7)] public EnumItemClass Type = default;
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
            [ProtoMember(1)] public int Recipe = 0;
            [ProtoMember(2)] public string GroupKey = null;
        }
        [ProtoContract]
        private class RecipeIndexCache
        {
            [ProtoMember(1)] public List<CachedRecipe> Recipes { get; set; } = new();
            [ProtoMember(2)] public Dictionary<string, List<CodeRecipeRef>> CodeToRecipes { get; set; } = new();
            [ProtoMember(3)] public Dictionary<string, List<string>> CodeToGkeys { get; set; } = new();
        }
        private struct ScanRequestInfo
        {
            public bool IncludeAll;
            public bool ModsOnly;
            public bool WoodOnly;
            public bool StoneOnly;
            public string TabKey;

            public ScanRequestInfo(bool includeAll, bool modsOnly, bool woodOnly, bool stoneOnly, string tabKey)
            {
                IncludeAll = includeAll;
                ModsOnly = modsOnly;
                WoodOnly = woodOnly;
                StoneOnly = stoneOnly;
                TabKey = tabKey;
            }
        }

        private sealed class WildGroup
        {
            public GridRecipeShim Recipe;
            public string GroupKey;
            public EnumItemClass Type;
            public AssetLocation Pattern;
            public string[] Allowed;
        }

        private static readonly (string VariantKey, bool IncludeAll, bool ModsOnly, bool WoodOnly, bool StoneOnly)[] RecipeIndexVariants =
        {
            ("van", IncludeAll: false, ModsOnly: false, WoodOnly: false, StoneOnly: false),
            ("mods", IncludeAll: false, ModsOnly: true, WoodOnly: false, StoneOnly: false),
            ("wood", IncludeAll: false, ModsOnly: false, WoodOnly: true, StoneOnly: false),
            ("stone", IncludeAll: false, ModsOnly: false, WoodOnly: false, StoneOnly: true),
            ("all", IncludeAll: true, ModsOnly: false, WoodOnly: false, StoneOnly: false)
        };

        private static ulong ComputeResourcePoolDNA(ResourcePool pool)
        {
            if (pool == null) return 0UL;

            var entries = new List<(string Code, int Qty, int Cls)>();
            foreach (var kv in pool.Counts)
            {
                var code = kv.Key.Code ?? "";
                var qty = Math.Max(0, kv.Value);
                int cls = 0;
                try
                {
                    if (pool.Classes.TryGetValue(kv.Key, out var ecl)) cls = (int)ecl;
                }
                catch { }
                entries.Add((code, qty, cls));
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.Code, b.Code));

            const ulong FNV_OFFSET = 14695981039346656037UL;
            const ulong FNV_PRIME = 1099511628211UL;
            ulong h = FNV_OFFSET;

            foreach (var (code, qty, cls) in entries)
            {
                h = FnvAddStr(h, code ?? "");
                h = FnvAddChar(h, '|');
                h = FnvAddInt(h, cls);
                h = FnvAddChar(h, '|');
                h = FnvAddInt(h, qty);
            }

            return h;

            static ulong FnvAddStr(ulong h, string s)
            {
                if (s == null) return h;
                unchecked
                {
                    for (int i = 0; i < s.Length; i++)
                    {
                        h ^= (byte)s[i];
                        h *= FNV_PRIME;
                    }
                }
                return h;
            }

            static ulong FnvAddInt(ulong h, int v)
            {
                unchecked
                {
                    h ^= (byte)(v);
                    h *= FNV_PRIME;
                    h ^= (byte)(v >> 8);
                    h *= FNV_PRIME;
                    h ^= (byte)(v >> 16);
                    h *= FNV_PRIME;
                    h ^= (byte)(v >> 24);
                    h *= FNV_PRIME;
                }
                return h;
            }

            static ulong FnvAddChar(ulong h, char c)
            {
                unchecked
                {
                    h ^= (byte)c;
                    h *= FNV_PRIME;
                }
                return h;
            }
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

        internal static bool TryBeginFetch()
        {
            if (!IsFetchButtonEnabled()) return false;
            if (ScanInProgress) return false;
            return Interlocked.CompareExchange(ref FetchInProgressFlag, 1, 0) == 0;
        }

        internal static void EndFetch()
        {
            Interlocked.Exchange(ref FetchInProgressFlag, 0);
        }

        internal static bool IsFetchInProgress()
        {
            return Volatile.Read(ref FetchInProgressFlag) != 0;
        }

        internal static bool IsScanInProgress()
        {
            return ScanInProgress;
        }

        private static string GetVariantKey(bool includeAll, bool modsOnly, bool woodOnly, bool stoneOnly)
        {
            if (includeAll) return "all";
            if (modsOnly) return "mods";
            if (woodOnly) return "wood";
            if (stoneOnly) return "stone";
            return "van";
        }

        private static string GetTabKey(bool includeAll, bool modsOnly, bool woodOnly, bool stoneOnly)
        {
            if (includeAll) return AllTabKeyName;
            if (modsOnly) return ModTabKeyName;
            if (woodOnly) return WoodTabKeyName;
            if (stoneOnly) return StoneTabKeyName;
            return CraftableTabKeyName;
        }

        private static string TabKeyFromVariant(string variantKey)
        {
            return variantKey switch
            {
                "all" => AllTabKeyName,
                "mods" => ModTabKeyName,
                "wood" => WoodTabKeyName,
                "stone" => StoneTabKeyName,
                "van" => CraftableTabKeyName,
                _ => CraftableTabKeyName
            };
        }

        private static string VariantKeyFromTabKey(string tabKey)
        {
            if (string.Equals(tabKey, AllTabKeyName, StringComparison.Ordinal)) return "all";
            if (string.Equals(tabKey, ModTabKeyName, StringComparison.Ordinal)) return "mods";
            if (string.Equals(tabKey, WoodTabKeyName, StringComparison.Ordinal)) return "wood";
            if (string.Equals(tabKey, StoneTabKeyName, StringComparison.Ordinal)) return "stone";
            return "van";
        }

        private static string GetActiveTabKey()
        {
            if (CraftableAllTabActive) return AllTabKeyName;
            if (CraftableModsTabActive) return ModTabKeyName;
            if (CraftableWoodTabActive) return WoodTabKeyName;
            if (CraftableStoneTabActive) return StoneTabKeyName;
            if (CraftableTabActive) return CraftableTabKeyName;
            return CraftableTabKeyName;
        }

        private static bool IsTabActive(string tabKey)
        {
            if (string.Equals(tabKey, AllTabKeyName, StringComparison.Ordinal)) return CraftableAllTabActive;
            if (string.Equals(tabKey, ModTabKeyName, StringComparison.Ordinal)) return CraftableModsTabActive;
            if (string.Equals(tabKey, WoodTabKeyName, StringComparison.Ordinal)) return CraftableWoodTabActive;
            if (string.Equals(tabKey, StoneTabKeyName, StringComparison.Ordinal)) return CraftableStoneTabActive;
            if (string.Equals(tabKey, CraftableTabKeyName, StringComparison.Ordinal)) return CraftableTabActive;
            return false;
        }

        private static bool IsKnownTabKey(string tabKey)
        {
            return string.Equals(tabKey, AllTabKeyName, StringComparison.Ordinal)
                || string.Equals(tabKey, ModTabKeyName, StringComparison.Ordinal)
                || string.Equals(tabKey, WoodTabKeyName, StringComparison.Ordinal)
                || string.Equals(tabKey, StoneTabKeyName, StringComparison.Ordinal)
                || string.Equals(tabKey, CraftableTabKeyName, StringComparison.Ordinal);
        }

        private static List<string> GetTabCache(string tabKey)
        {
            if (string.Equals(tabKey, AllTabKeyName, StringComparison.Ordinal)) return AllTabCache;
            if (string.Equals(tabKey, ModTabKeyName, StringComparison.Ordinal)) return ModTabCache;
            if (string.Equals(tabKey, WoodTabKeyName, StringComparison.Ordinal)) return WoodTypeTabCache;
            if (string.Equals(tabKey, StoneTabKeyName, StringComparison.Ordinal)) return StoneTypeTabCache;
            if (string.Equals(tabKey, CraftableTabKeyName, StringComparison.Ordinal)) return CraftableTabCache;

            return s_EmptyPages ??= new List<string>();
        }

        private static void SetTabCache(string tabKey, IEnumerable<string> pages)
        {
            if (!IsKnownTabKey(tabKey))
            {
                LogEverywhere(_staticCapi, $"[Cache] Ignoring SetTabCache for unknown tabKey='{tabKey}'", toChat: true);
                return;
            }

            var list = pages != null ? new List<string>(pages) : new List<string>();
            if (string.Equals(tabKey, AllTabKeyName, StringComparison.Ordinal)) AllTabCache = list;
            else if (string.Equals(tabKey, ModTabKeyName, StringComparison.Ordinal)) ModTabCache = list;
            else if (string.Equals(tabKey, WoodTabKeyName, StringComparison.Ordinal)) WoodTypeTabCache = list;
            else if (string.Equals(tabKey, StoneTabKeyName, StringComparison.Ordinal)) StoneTypeTabCache = list;
            else CraftableTabCache = list;
        }

        private static List<string> GetTabCacheSnapshot(string tabKey)
        {
            var cache = GetTabCache(tabKey);
            return cache != null ? new List<string>(cache) : new List<string>();
        }

        private static void SetTabReadyToUpdateUI(string tabKey, bool ready)
        {
            if (string.IsNullOrEmpty(tabKey)) return;
            lock (TabUiStateLock)
            {
                TabReadyToUpdateUi[tabKey] = ready;
            }
        }

        private static bool TryAcquireTabUpdateTicket(string tabKey, bool requireReadyFlag)
        {
            if (string.IsNullOrEmpty(tabKey)) return false;

            lock (TabUiStateLock)
            {
                if (!IsTabActive(tabKey)) return false;

                if (TabReadyToUpdateUi.TryGetValue(tabKey, out var ready) && ready)
                {
                    TabReadyToUpdateUi[tabKey] = false;
                    return true;
                }

                if (!requireReadyFlag)
                {
                    TabReadyToUpdateUi[tabKey] = false;
                    return true;
                }

                return false;
            }
        }

        private static string FormatTabScanState(string tabKey)
        {
            if (string.IsNullOrEmpty(tabKey)) return "dna=<none>, cachePages=0";

            ulong dna;
            bool hasDna;
            lock (DnaLock)
            {
                hasDna = TabPoolDNA.TryGetValue(tabKey, out dna);
            }

            int cachePages;
            lock (CacheLock)
            {
                var cache = GetTabCache(tabKey);
                cachePages = cache?.Count ?? 0;
            }

            string dnaText = hasDna ? $"0x{dna:X16}" : "<none>";
            return $"dna={dnaText}, cachePages={cachePages}";
        }

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
            catch { }
            return st;
        }

        private static ItemStack KeyToItemStack(ICoreClientAPI capi, StackKey key)
        {
            return MakeStackFromCodeAndAttrs(capi, key.Code, key.Material, key.Type);
        }

        private sealed class RecipeIndexData
        {
            public Dictionary<string, List<(GridRecipeShim Recipe, string GroupKey)>> CodeToRecipeGroups { get; }
                = new(StringComparer.Ordinal);

            public Dictionary<string, HashSet<string>> CodeToGkeys { get; }
                = new(StringComparer.Ordinal);

            public Dictionary<GridRecipeShim, Dictionary<string, int>> RecipeGroupNeeds { get; }
                = new();

            public List<WildGroup> WildcardGroups { get; }
                = new();

            public Dictionary<string, List<(GridRecipeShim Recipe, string GroupKey)>> WildMatchCache { get; }
                = new(StringComparer.Ordinal);

            public Dictionary<StackKey, List<GridRecipeShim>> OutputsIndex { get; set; }
                = new();

            public int RecipesFetched { get; set; }
            public int RecipesUsable { get; set; }
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
                catch { }
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
                            finalCode = finalCode.Replace(outMat, token);

                        finalCode = finalCode
                            .Replace("{wood}", token)
                            .Replace("{rock}", token);

                        string mat2 = outMat;
                        if (!string.IsNullOrEmpty(mat2))
                            mat2 = mat2.Replace("{wood}", token).Replace("{rock}", token);

                        string type2 = outType;
                        if (!string.IsNullOrEmpty(type2))
                            type2 = type2.Replace("{wood}", token).Replace("{rock}", token);

                        if (DebugEnabled)
                        {
                            bool changed = !string.Equals(finalCode, ocode, StringComparison.Ordinal);
                            bool lookedLikeVariant =
                                (ocode?.Contains("{wood}") == true) || (ocode?.Contains("{rock}") == true) ||
                                (outMat?.Contains("{wood}") == true) || (outMat?.Contains("{rock}") == true);

                            if (changed || lookedLikeVariant)
                            {
                                LogEverywhere(capi, $"Expanded: {ocode} -> {finalCode} (token={token}, material={(mat2 ?? outMat ?? "<null>")}, type={(type2 ?? outType ?? "<null>")})", caller: nameof(BuildRecipeIndex));
                            }
                        }

                        dest.Add(new StackKey(finalCode, mat2 ?? token, type2 ?? ""));
                    }
                }
                else
                {
                    dest.Add(new StackKey(ocode, outMat ?? "", outType ?? ""));
                }
            }
        }

        private static readonly string[] WoodSpecies = new[]
        {
            "birch","maple","pine","acacia","kapok","baldcypress",
            "larch","redwood","ebony","walnut","purpleheart","oak","aged"
        };

        private static readonly string[] StoneSpecies = new[]
        {
            "andesite","basalt","bauxite","chalk","chert","claystone",
            "conglomerate","granite","kimberlite","limestone",
            "whitemarble","redmarble","greenmarble",
            "peridotite","phyllite","sandstone","shale","slate","suevite"
        };

        private static bool IsSep(char c) => c == '-' || c == '_' || c == '/' || c == '.';

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

            var buf = new char[s.Length];
            int j = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (!char.IsWhiteSpace(c)) buf[j++] = char.ToLowerInvariant(c);
            }
            string t = new string(buf, 0, j);

            static string ExtractValue(string text, string key)
            {
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
                    i += key.Length + 3; 
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
                return text.Substring(start, i - start); 
            }

            var mval = ExtractValue(t, "material");
            if (mval != null)
            {
                if (mval == "{wood}") return true; 
                foreach (var m in WoodSpecies) if (mval == m) return true;
            }

            var typeVal = ExtractValue(t, "type");
            if (typeVal != null)
            {
                if (typeVal == "wood-{wood}") return true;
                foreach (var m in WoodSpecies) if (typeVal == "wood-" + m) return true;
            }

            foreach (var m in WoodSpecies)
            {
                if (t.IndexOf("wood-" + m, StringComparison.Ordinal) >= 0) return true;
            }

            return false;
        }

        private static bool ContainsStoneMatInCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return false;
            foreach (var m in StoneSpecies)
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

        private static bool ContainsStoneMatInAttributes(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;

            var buf = new char[s.Length];
            int j = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (!char.IsWhiteSpace(c)) buf[j++] = char.ToLowerInvariant(c);
            }
            string t = new string(buf, 0, j);

            static string ExtractValue(string text, string key)
            {
                int i = text.IndexOf(key + ":", StringComparison.Ordinal);
                if (i >= 0) i += key.Length + 1;
                else
                {
                    i = text.IndexOf("\"" + key + "\":", StringComparison.Ordinal);
                    if (i < 0) i = text.IndexOf("'" + key + "':", StringComparison.Ordinal);
                    if (i < 0) return null;
                    i += key.Length + 3;
                }
                if (i >= text.Length) return null;

                char qc = (text[i] == '"' || text[i] == '\'') ? text[i++] : '\0';
                int start = i;
                while (i < text.Length)
                {
                    char c = text[i];
                    if ((qc != '\0' && c == qc) || (qc == '\0' && (c == ',' || c == '}' || c == ']'))) break;
                    i++;
                }
                return text.Substring(start, i - start);
            }

            var mval = ExtractValue(t, "material");
            if (mval != null)
            {
                foreach (var m in StoneSpecies) if (mval == m) return true;
            }

            var typeVal = ExtractValue(t, "type");
            if (typeVal != null)
            {
                foreach (var m in StoneSpecies) if (typeVal == "stone-" + m) return true;
            }

            foreach (var m in StoneSpecies)
            {
                if (t.IndexOf("stone-" + m, StringComparison.Ordinal) >= 0) return true;
            }
            return false;
        }

        private static bool IsStoneRecipe(object recipeOrOutput)
        {
            if (recipeOrOutput == null) return false;

            bool CheckOne(object o)
            {
                if (o == null) return false;

                string code = null;
                string attrs = null;

                if (o is ItemStack stack)
                {
                    code = stack.Collectible?.Code?.ToString();
                    attrs = stack.Attributes?.ToJsonToken()?.ToString();
                }
                else
                {
                    var t = o.GetType();
                    var al = TryGetMember(t, o, "Code") as AssetLocation;
                    code = al?.ToString() ?? TryGetMember(t, o, "code") as string;

                    var attrsObj = TryGetMember(t, o, "Attributes");
                    if (attrsObj == null)
                    {
                        var rst = TryGetMember(t, o, "ResolvedItemstack") as ItemStack;
                        if (rst != null)
                        {
                            code ??= rst.Collectible?.Code?.ToString();
                            attrsObj = rst.Attributes;
                        }
                    }
                    if (attrsObj is IAttribute attr) attrs = attr.ToJsonToken();
                    else attrs = attrsObj?.ToString();
                }

                if (!string.IsNullOrEmpty(code))
                {
                    if (ContainsStoneMatInCode(code)) return true;
                }
                if (!string.IsNullOrEmpty(attrs))
                {
                    if (ContainsStoneMatInAttributes(attrs)) return true;
                }
                return false;
            }

            var rt = recipeOrOutput.GetType();
            var outOne = TryGetMember(rt, recipeOrOutput, "Output");
            if (CheckOne(outOne)) return true;

            var outs = TryGetMember(rt, recipeOrOutput, "Outputs") as System.Collections.IEnumerable;
            if (outs != null) foreach (var o in outs) if (CheckOne(o)) return true;

            return CheckOne(recipeOrOutput);
        }

        private static void LogEverywhere(ICoreClientAPI capi, string msg, bool toChat = false, [CallerMemberName] string caller = null)
        {
            if (!DebugEnabled) return;

            string fullMsg = $"[Craftable] {caller}: {msg}";

            try { capi.Logger?.Notification(fullMsg); } catch { }
            try { capi.World?.Logger?.Notification(fullMsg); } catch { }
            if (toChat) { try { capi.ShowChatMessage(fullMsg); } catch { } }

            try
            {
                string basePath = null;
                var m = typeof(ICoreAPI).GetMethod("GetOrCreateDataPath", BindingFlags.Public | BindingFlags.Instance);
                if (m != null) basePath = (string)m.Invoke(capi, new object[] { "ShowCraftable" });
                if (string.IsNullOrEmpty(basePath))
                    basePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShowCraftable");
                Directory.CreateDirectory(basePath);
                var f = System.IO.Path.Combine(basePath, "craftable.log");
                lock (LogFileLock)
                {
                    File.AppendAllText(f, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [Craftable] {caller}: {msg}\n");
                }
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

        private static void RequestServerScan(
            ICoreClientAPI capi,
            int radius,
            bool includeAll,
            bool modsOnly,
            bool woodOnly,
            bool stoneOnly,
            string tabKey,
            bool allowQueue = true)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                if (capi == null) return;

                var now = DateTime.UtcNow;
                if ((now - _lastScanAt).TotalMilliseconds < 400 && allowQueue)
                {
                    EnqueueScanRequest(capi, includeAll, modsOnly, woodOnly, stoneOnly, tabKey);
                    return;
                }
                _lastScanAt = now;

                if (ScanInProgress)
                {
                    if (allowQueue) EnqueueScanRequest(capi, includeAll, modsOnly, woodOnly, stoneOnly, tabKey);
                    return;
                }
                ScanInProgress = true;
                HandbookPauseGuard.Acquire(capi);

                var variantKey = GetVariantKey(includeAll, modsOnly, woodOnly, stoneOnly);

                lock (PendingScanLock)
                {
                    PendingScanVariantKey = variantKey;
                    PendingScanTabKey = tabKey;
                }

                int scanId = Interlocked.Increment(ref ScanSeq);
                lock (InflightMapLock)
                {
                    InflightById[scanId] = new ScanRequestInfo(includeAll, modsOnly, woodOnly, stoneOnly, tabKey);
                }

                try
                {
                    capi.Network.GetChannel(ChannelName).SendPacket(new CraftScanRequest
                    {
                        Radius = radius,
                        CollectItems = true,
                        Variants = new List<CraftIngredientList>(),
                        ScanId = scanId,
                        TabKey = tabKey
                    });
                    LogEverywhere(capi, $"[Scan] → sent ScanId={scanId} (radius={radius}, variant={variantKey}, tab={tabKey})");
                }
                catch (Exception e)
                {
                    lock (PendingScanLock)
                    {
                        PendingScanVariantKey = null;
                        PendingScanTabKey = null;
                    }
                    lock (InflightMapLock) InflightById.Remove(scanId);

                    FinishScan(capi);
                    LogEverywhere(capi, $"Failed to send scan request: {e}", toChat: true);
                    capi.Event.EnqueueMainThreadTask(() => TryProcessQueuedScan(capi), "SCProcessScanQueueFail");
                }
            }
            finally
            {
                sw.Stop();
                LogEverywhere(capi, $"RequestServerScan completed in {sw.ElapsedMilliseconds}ms", caller: nameof(RequestServerScan));
            }
        }

        private static void EnqueueScanRequest(ICoreClientAPI capi, bool includeAll, bool modsOnly, bool woodOnly, bool stoneOnly, string tabKey)
        {
            lock (ScanQueueLock)
            {
                QueuedScanRequest = new ScanRequestInfo(includeAll, modsOnly, woodOnly, stoneOnly, tabKey);
            }
            EnsureQueueCheckScheduled(capi);
        }

        private static void EnsureQueueCheckScheduled(ICoreClientAPI capi)
        {
            if (capi == null) return;
            bool shouldSchedule = false;
            lock (ScanQueueLock)
            {
                if (QueuedScanRequest.HasValue && !ScanQueueCheckScheduled)
                {
                    ScanQueueCheckScheduled = true;
                    shouldSchedule = true;
                }
            }

            if (shouldSchedule)
            {
                capi.Event.RegisterCallback(dt =>
                {
                    lock (ScanQueueLock)
                    {
                        ScanQueueCheckScheduled = false;
                    }
                    TryProcessQueuedScan(capi);
                }, 300, permittedWhilePaused: true);
            }
        }

        private static void TryProcessQueuedScan(ICoreClientAPI capi)
        {
            if (capi == null) return;

            ScanRequestInfo? request = null;
            lock (ScanQueueLock)
            {
                if (!ScanInProgress && QueuedScanRequest.HasValue)
                {
                    request = QueuedScanRequest;
                    QueuedScanRequest = null;
                }
            }

            if (request.HasValue)
            {
                RequestServerScan(capi, NearbyRadius, request.Value.IncludeAll, request.Value.ModsOnly, request.Value.WoodOnly, request.Value.StoneOnly, request.Value.TabKey, allowQueue: false);
            }
            else
            {
                EnsureQueueCheckScheduled(capi);
            }
        }

        private static void FinishScan(ICoreClientAPI capi)
        {
            ScanInProgress = false;
            EndFetch();
            HandbookPauseGuard.Release(capi);
        }

        private static void FinishScanAndProcessQueue(ICoreClientAPI capi)
        {
            FinishScan(capi);
            TryProcessQueuedScan(capi);
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            LoadConfig(api);
        }

        private static void LoadConfig(ICoreAPI api)
        {
            if (api == null) return;

            ShowCraftableConfig config = null;
            try
            {
                config = api.LoadModConfig<ShowCraftableConfig>(ConfigFileName);
            }
            catch (Exception e)
            {
                api.Logger?.Warning("[ShowCraftable] Failed to load config {0}: {1}", ConfigFileName, e);
            }

            if (config == null)
            {
                config = new ShowCraftableConfig();
            }

            config.Normalize();

            Config = config;
            NearbyRadius = ConfiguredSearchRadius;
            SetServerFetchDisabled(Config.DisableFetchButtonOnServer);

            SaveConfig(api);
        }

        private static void SaveConfig(ICoreAPI api)
        {
            if (api == null) return;

            try
            {
                api.StoreModConfig(Config, ConfigFileName);
            }
            catch (Exception e)
            {
                api.Logger?.Warning("[ShowCraftable] Failed to save config {0}: {1}", ConfigFileName, e);
            }
        }

        public override void StartClientSide(ICoreClientAPI capi)
        {
            _capi = capi;
            _staticCapi = capi;
            _harmony = new Harmony(HarmonyId);

            var miAddVerticalTabs = AccessTools.Method(typeof(ClientGuiComposerHelpers), nameof(ClientGuiComposerHelpers.AddVerticalTabs));
            _harmony.Patch(miAddVerticalTabs, prefix: new HarmonyMethod(typeof(ShowCraftableSystem), nameof(AddVerticalTabs_Prefix)));

            IClientNetworkChannel channel = capi.Network.RegisterChannel(ChannelName);
            channel.RegisterMessageType(typeof(CraftScanRequest));
            channel.RegisterMessageType(typeof(CraftScanReply));
            channel.RegisterMessageType(typeof(FetchAvailabilityMessage));
            channel.SetMessageHandler<CraftScanReply>(OnServerScanReply);
            channel.SetMessageHandler<FetchAvailabilityMessage>(OnFetchAvailabilityMessage);

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

            capi.Event.LevelFinalize += () =>
            {
                lock (ScanQueueLock) { QueuedScanRequest = null; ScanQueueCheckScheduled = false; }
                lock (PendingScanLock) { PendingScanVariantKey = null; PendingScanTabKey = null; }
                lock (InflightMapLock) InflightById.Clear();
                ScanInProgress = false;
                InvalidatePageCodeMapCache();
                StartRecipeIndexBuild(capi);
            };
            capi.Event.LeaveWorld += OnLeaveWorld;
        }

        private void OnLeaveWorld()
        {
            ClearAllCaches();
            _capi = null;
        }

        public override void Dispose() => _harmony?.UnpatchAll(HarmonyId);

        public static bool AddVerticalTabs_Prefix(GuiComposer composer, GuiTab[] tabs, ElementBounds bounds, Action<int, GuiTab> onTabClicked, string key, ref GuiComposer __result)
        {
            if (composer == null || composer.Composed) return true;
            if (!ShouldApplyCraftableFonts(tabs)) return true;

            var font = CairoFont.WhiteDetailText().WithFontSize(17f);
            var selectedFont = CairoFont.WhiteDetailText().WithFontSize(17f).WithColor(GuiStyle.ActiveButtonTextColor);
            composer.AddInteractiveElement(new GuiElementVerticalTabsWithCustomFonts(composer.Api, tabs, font, selectedFont, bounds, onTabClicked), key);
            __result = composer;
            return false;
        }

        private static bool ShouldApplyCraftableFonts(GuiTab[] tabs)
        {
            if (tabs == null || tabs.Length == 0) return false;
            if (UseDefaultFont) return false;

            foreach (var tab in tabs)
            {
                var categoryCode = TryGetCategoryCode(tab);
                if (GetCraftableTabFontName(categoryCode) != null) return true;
            }

            return false;
        }

        internal static string TryGetCategoryCode(GuiTab tab)
        {
            if (tab == null) return null;
            if (tab is HandbookTab handbookTab) return handbookTab.CategoryCode;

            var type = tab.GetType();
            var pi = type.GetProperty("CategoryCode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null) return pi.GetValue(tab) as string;

            var fi = type.GetField("CategoryCode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return fi?.GetValue(tab) as string;
        }

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

                bool craftableAllExists = false;
                bool craftableExists = false;
                bool craftableModsExists = false;
                bool craftableWoodExists = false;
                bool craftableStoneExists = false;
                foreach (var t in tabs)
                {
                    var cat = GetPF(tabType, t, "CategoryCode") as string;
                    if (string.Equals(cat, CraftableAllCategoryCode, StringComparison.OrdinalIgnoreCase))
                    {
                        craftableAllExists = true;
                        SetPF(tabType, t, "Name", CraftableAllTabDisplayName);
                    }
                    if (string.Equals(cat, CraftableCategoryCode, StringComparison.OrdinalIgnoreCase))
                    {
                        craftableExists = true;
                        SetPF(tabType, t, "Name", BaseItemsTabDisplayName);
                    }
                    if (string.Equals(cat, CraftableModsCategoryCode, StringComparison.OrdinalIgnoreCase))
                    {
                        craftableModsExists = true;
                        SetPF(tabType, t, "Name", ModItemsTabDisplayName);
                    }
                    if (string.Equals(cat, CraftableWoodCategoryCode, StringComparison.OrdinalIgnoreCase))
                    {
                        craftableWoodExists = true;
                        SetPF(tabType, t, "Name", WoodTypesTabDisplayName);
                    }
                    if (string.Equals(cat, CraftableStoneCategoryCode, StringComparison.OrdinalIgnoreCase))
                    {
                        craftableStoneExists = true;
                        SetPF(tabType, t, "Name", StoneTypesTabDisplayName);
                    }
                }

                int insertAt = tabs.Count;
                if (!craftableAllExists)
                {
                    var allTab = Activator.CreateInstance(tabType);
                    SetPF(tabType, allTab, "Name", CraftableAllTabDisplayName);
                    SetPF(tabType, allTab, "CategoryCode", CraftableAllCategoryCode);
                    SetPF(tabType, allTab, "DataInt", tabs.Count);
                    SetPF(tabType, allTab, "PaddingTop", 20.0);
                    tabs.Insert(insertAt, allTab);
                    insertAt++;
                    craftableAllExists = true;
                }

                if (!craftableExists)
                {
                    var newTab = Activator.CreateInstance(tabType);
                    SetPF(tabType, newTab, "Name", BaseItemsTabDisplayName);
                    SetPF(tabType, newTab, "CategoryCode", CraftableCategoryCode);
                    SetPF(tabType, newTab, "DataInt", tabs.Count);
                    SetPF(tabType, newTab, "PaddingTop", craftableAllExists ? 5.0 : 20.0);
                    tabs.Insert(insertAt, newTab);
                    insertAt++;
                }

                if (!craftableWoodExists)
                {
                    var woodTab = Activator.CreateInstance(tabType);
                    SetPF(tabType, woodTab, "Name", WoodTypesTabDisplayName);
                    SetPF(tabType, woodTab, "CategoryCode", CraftableWoodCategoryCode);
                    SetPF(tabType, woodTab, "DataInt", tabs.Count);
                    SetPF(tabType, woodTab, "PaddingTop", 5.0);
                    tabs.Insert(insertAt, woodTab);
                    insertAt++;
                }

                if (!craftableStoneExists)
                {
                    var stoneTab = Activator.CreateInstance(tabType);
                    SetPF(tabType, stoneTab, "Name", StoneTypesTabDisplayName);
                    SetPF(tabType, stoneTab, "CategoryCode", CraftableStoneCategoryCode);
                    SetPF(tabType, stoneTab, "DataInt", tabs.Count);
                    SetPF(tabType, stoneTab, "PaddingTop", 5.0);
                    tabs.Insert(insertAt, stoneTab);
                    insertAt++;
                }

                if (!craftableModsExists)
                {
                    var newTabMods = Activator.CreateInstance(tabType);
                    SetPF(tabType, newTabMods, "Name", ModItemsTabDisplayName);
                    SetPF(tabType, newTabMods, "CategoryCode", CraftableModsCategoryCode);
                    SetPF(tabType, newTabMods, "DataInt", tabs.Count);
                    SetPF(tabType, newTabMods, "PaddingTop", 5.0);
                    tabs.Insert(insertAt, newTabMods);
                    insertAt++;
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

        private static bool DialogIsOpen(object inst)
        {
            if (inst is GuiDialog dlg) return dlg.IsOpened();
            var mi = inst.GetType().GetMethod("IsOpened", BindingFlags.Instance | BindingFlags.Public);
            return mi != null && mi.ReturnType == typeof(bool) && (bool)mi.Invoke(inst, Array.Empty<object>());
        }

        public static void SelectTab_Postfix(object __instance, string code)
        {
            ICoreClientAPI capi = null;
            var sw = Stopwatch.StartNew();
            try
            {
                CraftableAllTabActive = string.Equals(code, CraftableAllCategoryCode, StringComparison.Ordinal);
                CraftableTabActive = string.Equals(code, CraftableCategoryCode, StringComparison.Ordinal);
                CraftableModsTabActive = string.Equals(code, CraftableModsCategoryCode, StringComparison.Ordinal);
                CraftableWoodTabActive = string.Equals(code, CraftableWoodCategoryCode, StringComparison.Ordinal);
                CraftableStoneTabActive = string.Equals(code, CraftableStoneCategoryCode, StringComparison.Ordinal);

                bool includeAll = CraftableAllTabActive;
                bool modsOnly = CraftableModsTabActive;
                bool woodOnly = CraftableWoodTabActive;
                bool stoneOnly = CraftableStoneTabActive;

                bool anyCraftable = CraftableAllTabActive || CraftableTabActive || CraftableModsTabActive || CraftableWoodTabActive || CraftableStoneTabActive;
                string tabKey = GetTabKey(includeAll, modsOnly, woodOnly, stoneOnly);
                string variantKey = GetVariantKey(includeAll, modsOnly, woodOnly, stoneOnly);

                if (!DialogIsOpen(__instance))
                {
                    _pendingScanId++;
                    return;
                }

                var fiCapi = AccessTools.Field(__instance.GetType(), "capi");
                capi = fiCapi?.GetValue(__instance) as ICoreClientAPI ?? _staticCapi;

                string diagnosticsSuffix = anyCraftable ? $" ({FormatTabScanState(tabKey)})" : string.Empty;

                if (CraftableAllTabActive) LogEverywhere(capi, $"Craftable tab selected by user{diagnosticsSuffix}");
                else if (CraftableTabActive) LogEverywhere(capi, $"Base Items tab selected by user{diagnosticsSuffix}");
                else if (CraftableModsTabActive) LogEverywhere(capi, $"Mod Items tab selected by user{diagnosticsSuffix}");
                else if (CraftableWoodTabActive) LogEverywhere(capi, $"Wood Types tab selected by user{diagnosticsSuffix}");
                else if (CraftableStoneTabActive) LogEverywhere(capi, $"Stone Types tab selected by user{diagnosticsSuffix}");

                var fiOverview = AccessTools.Field(__instance.GetType(), "overviewGui");
                var composer = fiOverview?.GetValue(__instance) as GuiComposer;

                var piSingle = AccessTools.Property(__instance.GetType(), "SingleComposer")
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

                bool variantReady;
                lock (recipeIndexByVariant)
                {
                    variantReady = recipeIndexByVariant.ContainsKey(variantKey);
                }
                if (variantReady)
                {
                    ApplyRecipeIndexVariant(variantKey);
                }
                else
                {
                    StartRecipeIndexBuild(capi);
                }

                lock (CacheLock)
                {
                    CachedPageCodes = GetTabCacheSnapshot(tabKey);
                }
                LastDialogPageCount = -1;
                TryRefreshOpenDialog(capi);
                TryUpdateActiveTabFromCache(capi, tabKey, requireReadyFlag: false);

                var myScanId = ++_pendingScanId;
                capi.Event.EnqueueMainThreadTask(() =>
                {
                    if (myScanId != _pendingScanId) return;
                    if (!DialogIsOpen(__instance)) return;
                    RequestServerScan(capi, NearbyRadius, includeAll, modsOnly, woodOnly, stoneOnly, tabKey);
                }, "SCScanOnTabSelect");
            }
            catch (Exception e)
            {
                LogEverywhere(capi ?? _staticCapi, $"Error in SelectTab_Postfix: {e}");
            }
            finally
            {
                sw.Stop();
                var logApi = capi ?? _staticCapi;
                if (logApi != null) LogEverywhere(logApi, $"SelectTab_Postfix completed in {sw.ElapsedMilliseconds}ms", caller: nameof(SelectTab_Postfix));
            }
        }

        private static void SetUpdatingText(ICoreClientAPI capi, bool show)
        {
            try
            {
                capi?.Event?.RegisterCallback(_ =>
                {
                    bool guardAcquired = false;
                    try
                    {
                        if (!show && capi?.IsGamePaused == true)
                        {
                            HandbookPauseGuard.Acquire(capi);
                            guardAcquired = true;
                        }

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
                    finally
                    {
                        if (guardAcquired)
                        {
                            HandbookPauseGuard.Release(capi);
                        }
                    }
                }, 1, permittedWhilePaused: true);
            }
            catch { }
        }

        private static bool TryUpdateActiveTabFromCache(ICoreClientAPI capi, string tabKey, bool requireReadyFlag = true)
        {
            if (!TryAcquireTabUpdateTicket(tabKey, requireReadyFlag)) return false;

            lock (CacheLock)
            {
                CachedPageCodes = GetTabCacheSnapshot(tabKey);
            }

            LastDialogPageCount = -1;
            TryRefreshOpenDialog(capi);
            SetUpdatingText(capi, false);
            return true;
        }

        private static bool IsWoodRecipe(object recipeOrOutput)
        {
            if (recipeOrOutput == null) return false;

            bool CheckOne(object o)
            {
                if (o == null) return false;

                string code = null;
                string attrs = null;

                if (o is ItemStack stack)
                {
                    code = stack.Collectible?.Code?.ToString();
                    attrs = stack.Attributes?.ToJsonToken()?.ToString();
                }
                else
                {
                    var t = o.GetType();

                    var al = TryGetMember(t, o, "Code") as AssetLocation;
                    code = al?.ToString() ?? TryGetMember(t, o, "code") as string;

                    var attrsObj = TryGetMember(t, o, "Attributes");
                    if (attrsObj == null)
                    {
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

            var rt = recipeOrOutput.GetType();
            var outOne = TryGetMember(rt, recipeOrOutput, "Output");
            if (CheckOne(outOne)) return true;

            var outs = TryGetMember(rt, recipeOrOutput, "Outputs") as System.Collections.IEnumerable;
            if (outs != null) foreach (var o in outs) if (CheckOne(o)) return true;

            return CheckOne(recipeOrOutput);
        }

        public static void AfterPagesLoaded_Postfix(object __instance)
        {
            try
            {
                var fiCapi = AccessTools.Field(__instance.GetType(), "capi");
                var capi = fiCapi?.GetValue(__instance) as ICoreClientAPI;
                var cur = AccessTools.Field(__instance.GetType(), "currentCatgoryCode")?.GetValue(__instance) as string;

                if (capi != null && (
                    string.Equals(cur, CraftableAllCategoryCode, StringComparison.Ordinal) ||
                    string.Equals(cur, CraftableCategoryCode, StringComparison.Ordinal) ||
                    string.Equals(cur, CraftableModsCategoryCode, StringComparison.Ordinal) ||
                    string.Equals(cur, CraftableWoodCategoryCode, StringComparison.Ordinal) ||
                    string.Equals(cur, CraftableStoneCategoryCode, StringComparison.Ordinal)))
                {
                    var fiPages = AccessTools.Field(__instance.GetType(), "allHandbookPages");
                    var fiDict = AccessTools.Field(__instance.GetType(), "pageNumberByPageCode");
                    var pages = fiPages?.GetValue(__instance) as System.Collections.IList;
                    var dict = fiDict?.GetValue(__instance) as System.Collections.IDictionary;
                    if (pages != null && dict != null)
                    {
                        var sorted = pages.Cast<object>()
                            .OrderBy(p => GetPageTitle(p), StringComparer.OrdinalIgnoreCase)
                            .ToList();
                        pages.Clear();
                        dict.Clear();
                        for (int i = 0; i < sorted.Count; i++)
                        {
                            var page = sorted[i];
                            pages.Add(page);
                            var piCode = AccessTools.Property(page.GetType(), "PageCode");
                            var code = piCode?.GetValue(page) as string;
                            if (code != null) dict[code] = i;
                            AccessTools.Field(page.GetType(), "PageNumber")?.SetValue(page, i);
                        }
                    }

                    AccessTools.Method(__instance.GetType(), "FilterItems")?.Invoke(__instance, null);
                }

            }
            catch { }
        }

        private static string GetPageTitle(object page)
        {
            var fiTitle = AccessTools.Field(page.GetType(), "TextCacheTitle");
            var title = fiTitle?.GetValue(page) as string;
            if (!string.IsNullOrEmpty(title)) return title;
            var piCode = AccessTools.Property(page.GetType(), "PageCode");
            return piCode?.GetValue(page) as string ?? string.Empty;
        }

        private static void AddRecipeButton_Postfix(List<RichTextComponentBase> components)
        {
            if (_staticCapi == null || components == null) return;
            if (!IsFetchButtonEnabled()) return;
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
                if (!(string.Equals(cat, CraftableAllCategoryCode, StringComparison.Ordinal) ||
                      string.Equals(cat, CraftableCategoryCode, StringComparison.Ordinal) ||
                      string.Equals(cat, CraftableModsCategoryCode, StringComparison.Ordinal) ||
                      string.Equals(cat, CraftableWoodCategoryCode, StringComparison.Ordinal) ||
                      string.Equals(cat, CraftableStoneCategoryCode, StringComparison.Ordinal))) return true;

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
                    finalPages = weighted
                        .OrderByDescending(x => x.W)
                        .ThenBy(x => GetPageTitle(x.Page), StringComparer.OrdinalIgnoreCase)
                        .Select(x => x.Page)
                        .ToList();
                }
                else
                {
                    finalPages = resolvedPages
                        .OrderBy(p => GetPageTitle(p), StringComparer.OrdinalIgnoreCase)
                        .ToList();
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
                if (!(string.Equals(cur, CraftableAllCategoryCode, StringComparison.Ordinal) ||
                        string.Equals(cur, CraftableCategoryCode, StringComparison.Ordinal) ||
                        string.Equals(cur, CraftableModsCategoryCode, StringComparison.Ordinal) ||
                        string.Equals(cur, CraftableWoodCategoryCode, StringComparison.Ordinal) ||
                        string.Equals(cur, CraftableStoneCategoryCode, StringComparison.Ordinal)))
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

                    if (!map.ContainsKey(key)) map[key] = pc;

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

        private static void ClearAllCaches()
        {
            Interlocked.Increment(ref recipeIndexBuildToken);

            lock (ScanQueueLock)
            {
                QueuedScanRequest = null;
                ScanQueueCheckScheduled = false;
            }

            lock (PendingScanLock)
            {
                PendingScanVariantKey = null;
                PendingScanTabKey = null;
            }

            lock (InflightMapLock) InflightById.Clear();

            lock (TabUiStateLock) TabReadyToUpdateUi.Clear();

            lock (DnaLock) TabPoolDNA.Clear();

            lock (WildTokenCountsMemo) WildTokenCountsMemo.Clear();

            lock (recipeIndexByVariant)
            {
                recipeIndexByVariant.Clear();
                recipeIndexBuildStarted = false;
                recipeIndexBuilt = false;
            }

            recipeIndexBuildTask = null;

            lock (CacheLock)
            {
                CachedPageCodes = new List<string>();
                AllTabCache = new List<string>();
                CraftableTabCache = new List<string>();
                ModTabCache = new List<string>();
                StoneTypeTabCache = new List<string>();
                WoodTypeTabCache = new List<string>();
                s_EmptyPages = null;
            }

            recipeGroupNeeds = new Dictionary<GridRecipeShim, Dictionary<string, int>>();
            outputsIndex = new Dictionary<StackKey, List<GridRecipeShim>>();
            codeToRecipeGroups = new Dictionary<string, List<(GridRecipeShim Recipe, string GroupKey)>>();
            codeToGkeys = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            wildMatchCache = new Dictionary<string, List<(GridRecipeShim Recipe, string GroupKey)>>(StringComparer.Ordinal);
            wildcardGroups = new List<WildGroup>();

            recipesFetched = 0;
            recipesUsable = 0;
            ScanSeq = 0;
            _pendingScanId = 0;
            LastDialogPageCount = 0;
            NearbyRadius = ConfiguredSearchRadius;
            recipeIndexForMods = false;
            recipeIndexForStoneOnly = false;
            recipeIndexForWoodOnly = false;
            CraftableAllTabActive = false;
            CraftableModsTabActive = false;
            CraftableStoneTabActive = false;
            CraftableTabActive = false;
            CraftableWoodTabActive = false;
            ScanInProgress = false;
            recipeIndexBuildProgress = 0;
            recipeIndexBuildTotal = 0;

            InvalidatePageCodeMapCache();
            _staticCapi = null;
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

                if (Counts.TryGetValue(k, out var cur)) Counts[k] = cur + addQty;
                else Counts[k] = addQty;

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

        private static bool CodeStartsWithBowl(AssetLocation code)
        {
            if (code == null) return false;

            string path = code.Path;
            if (string.IsNullOrEmpty(path))
            {
                string full = code.ToString();
                if (!string.IsNullOrEmpty(full))
                {
                    int colonIndex = full.IndexOf(':');
                    path = colonIndex >= 0 ? full.Substring(colonIndex + 1) : full;
                }
            }

            return !string.IsNullOrEmpty(path)
                && path.StartsWith("bowl", StringComparison.OrdinalIgnoreCase);
        }

        private static bool StackRepresentsBowl(ItemStack stack)
        {
            if (stack?.Collectible?.Code == null) return false;
            return CodeStartsWithBowl(stack.Collectible.Code);
        }

        private static bool ShouldSkipGridRecipe(GridRecipeShim shim)
        {
            if (shim == null) return false;

            if (ShouldSkipRawGridRecipe(shim.Raw)) return true;

            if (shim.Outputs != null)
            {
                foreach (var output in shim.Outputs)
                {
                    if (StackRepresentsBowl(output)) return true;
                }
            }

            if (shim.Ingredients != null)
            {
                foreach (var ingredient in shim.Ingredients)
                {
                    if (ingredient == null) continue;

                    if (ingredient.PatternCode != null && CodeStartsWithBowl(ingredient.PatternCode))
                        return true;

                    if (ingredient.Options != null)
                    {
                        foreach (var option in ingredient.Options)
                        {
                            if (StackRepresentsBowl(option)) return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool ShouldSkipRawGridRecipe(object raw)
        {
            if (raw == null) return false;

            var type = raw.GetType();
            if (type == null || !type.Name.Contains("GridRecipe", StringComparison.OrdinalIgnoreCase)) return false;

            var name = TryGetMember(type, raw, "Name") as AssetLocation;
            return ShouldSkipRecipeByName(name);
        }

        private static bool ShouldSkipRecipeByName(AssetLocation name)
        {
            if (name == null) return false;

            if (!string.Equals(name.Domain, "game", StringComparison.Ordinal)) return false;

            string path = name.Path;
            if (string.IsNullOrEmpty(path)) path = name.ToShortString();
            if (string.IsNullOrEmpty(path)) return false;

            return string.Equals(path, "recipes/grid/nuggets", StringComparison.Ordinal);
        }

        private static List<GridRecipeShim> GetAllGridRecipes(ICoreClientAPI capi, out int fetched, out int usable, bool? modsOnly)
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
                if (raw == null) continue;
                if (ShouldSkipRawGridRecipe(raw)) continue;

                fetched++;
                var shim = TryBuildGridShim(raw, capi);
                if (shim != null && shim.Outputs.Count > 0 && !ShouldSkipGridRecipe(shim))
                {
                    if (!modsOnly.HasValue || modsOnly.Value == shim.IsMod)
                    {
                        usable++;
                        list.Add(shim);
                    }
                }
            }

            sw.Stop();
            LogEverywhere(capi, $"Fetched {fetched} grid recipes, {usable} usable, gridMemberFound={gridMemberFound}, usedWorldList={usedGridRecipes}, elapsedMs={sw.ElapsedMilliseconds}");
            return list;
        }

        private static string GetCachePath(ICoreClientAPI capi, bool includeAll, bool modsOnly, bool woodOnly, bool stoneOnly)
        {
            string basePath;
            try
            {
                var m = typeof(ICoreAPI).GetMethod("GetOrCreateDataPath", BindingFlags.Public | BindingFlags.Instance);
                basePath = m != null ? (string)m.Invoke(capi, new object[] { "ShowCraftable" }) : null;
            }
            catch { basePath = null; }
            if (string.IsNullOrEmpty(basePath))
                basePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShowCraftable");
            Directory.CreateDirectory(basePath);

            string name =
                includeAll ? "recipeindex_all.bin" :
                modsOnly ? "recipeindex_mods.bin" :
                woodOnly ? "recipeindex_wood.bin" :
                stoneOnly ? "recipeindex_stone.bin" :
                            "recipeindex_vanilla.bin";

            return System.IO.Path.Combine(basePath, name);
        }

        private static int RecommendParallelism(
            int workItems,
            int chunkSize = 64,
            int reserveCores = 2,
            int minCap = 8,
            int maxCap = 24,
            double fraction = 0.65
        )
        {
            int cores = Math.Max(1, Environment.ProcessorCount);
            int usable = Math.Max(1, cores - reserveCores);
            int chunks = Math.Max(1, (workItems + chunkSize - 1) / chunkSize);

            int target = (int)Math.Round(usable * fraction);
            int cap = Math.Clamp(target, minCap, maxCap);

            int mdp = Math.Min(Math.Min(cap, usable), chunks);
            return Math.Max(1, mdp);
        }

        private static void StoreRecipeIndex(string variantKey, RecipeIndexData data)
        {
            if (string.IsNullOrEmpty(variantKey) || data == null) return;
            lock (recipeIndexByVariant)
            {
                recipeIndexByVariant[variantKey] = data;
            }
        }

        private static bool TryGetRecipeIndex(string variantKey, out RecipeIndexData data)
        {
            data = null;
            if (string.IsNullOrEmpty(variantKey)) return false;

            lock (recipeIndexByVariant)
            {
                if (recipeIndexByVariant.TryGetValue(variantKey, out var existing))
                {
                    data = existing;
                    return data != null;
                }
            }

            return false;
        }

        private static bool ApplyRecipeIndexVariant(string variantKey)
        {
            if (!TryGetRecipeIndex(variantKey, out var data)) return false;

            codeToRecipeGroups = data.CodeToRecipeGroups ?? new Dictionary<string, List<(GridRecipeShim Recipe, string GroupKey)>>(StringComparer.Ordinal);

            recipeGroupNeeds = data.RecipeGroupNeeds ?? new Dictionary<GridRecipeShim, Dictionary<string, int>>();

            codeToGkeys = data.CodeToGkeys ?? new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            wildcardGroups = data.WildcardGroups ?? new List<WildGroup>();

            wildMatchCache = data.WildMatchCache ?? new Dictionary<string, List<(GridRecipeShim Recipe, string GroupKey)>>(StringComparer.Ordinal);

            outputsIndex = data.OutputsIndex ?? new Dictionary<StackKey, List<GridRecipeShim>>();

            recipesFetched = data.RecipesFetched;
            recipesUsable = data.RecipesUsable;

            recipeIndexForMods = string.Equals(variantKey, "mods", StringComparison.Ordinal);
            recipeIndexForWoodOnly = string.Equals(variantKey, "wood", StringComparison.Ordinal);
            recipeIndexForStoneOnly = string.Equals(variantKey, "stone", StringComparison.Ordinal);

            return true;
        }

        private static bool EnsureRecipeIndexVariantReady(ICoreClientAPI capi, string variantKey, int timeoutMs = 15000)
        {
            if (string.IsNullOrEmpty(variantKey)) return false;

            if (TryGetRecipeIndex(variantKey, out _))
            {
                return ApplyRecipeIndexVariant(variantKey);
            }

            StartRecipeIndexBuild(capi);

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                Thread.Sleep(50);

                if (TryGetRecipeIndex(variantKey, out _))
                {
                    return ApplyRecipeIndexVariant(variantKey);
                }
            }

            LogEverywhere(capi ?? _staticCapi, $"Recipe index variant {variantKey} not ready after waiting {timeoutMs}ms", caller: nameof(EnsureRecipeIndexVariantReady));
            return false;
        }

        private static RecipeIndexData LoadRecipeIndex(ICoreClientAPI capi, bool includeAll, bool modsOnly, bool woodOnly, bool stoneOnly)
        {
            try
            {
                var path = GetCachePath(capi, includeAll, modsOnly, woodOnly, stoneOnly);
                if (!File.Exists(path)) return null;
                RecipeIndexCache data;
                using (var fs = File.OpenRead(path)) data = Serializer.Deserialize<RecipeIndexCache>(fs);

                var index = new RecipeIndexData();
                var recipes = new List<GridRecipeShim>();
                int[] recipeIndexMap = (data.Recipes != null)
                    ? new int[data.Recipes.Count]
                    : Array.Empty<int>();
                for (int i = 0; i < recipeIndexMap.Length; i++) recipeIndexMap[i] = -1;

                if (data.Recipes != null)
                {
                    for (int recipeIdx = 0; recipeIdx < data.Recipes.Count; recipeIdx++)
                    {
                        var cr = data.Recipes[recipeIdx];
                        if (cr == null) continue;

                        var r = new GridRecipeShim();
                        var recipeWildGroups = new List<WildGroup>();

                        foreach (var ci in (cr.Ingredients != null ? cr.Ingredients : new List<CachedIngredient>()))
                        {
                            if (ci == null) continue;

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
                                    if (b == null) continue;
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
                                recipeWildGroups.Add(new WildGroup { Recipe = r, GroupKey = gkey, Type = gi.Type, Pattern = gi.PatternCode, Allowed = gi.Allowed });
                            }
                        }

                        if (cr.Outputs != null)
                        {
                            foreach (var b in cr.Outputs)
                            {
                                if (b == null) continue;
                                try
                                {
                                    var st = new ItemStack(b);
                                    st.ResolveBlockOrItem(capi.World);
                                    r.Outputs.Add(st);
                                }
                                catch { }
                            }
                        }

                        if (r.Outputs == null || r.Outputs.Count == 0) continue;
                        if (ShouldSkipGridRecipe(r)) continue;

                        recipeIndexMap[recipeIdx] = recipes.Count;
                        recipes.Add(r);
                        foreach (var wild in recipeWildGroups) index.WildcardGroups.Add(wild);
                        index.RecipeGroupNeeds[r] = cr.Needs ?? new Dictionary<string, int>(StringComparer.Ordinal);
                    }
                }

                foreach (var kv in data.CodeToRecipes ?? new Dictionary<string, List<CodeRecipeRef>>())
                {
                    var list = new List<(GridRecipeShim Recipe, string GroupKey)>();
                    if (kv.Value != null)
                    {
                        foreach (var rref in kv.Value)
                        {
                            if (rref == null) continue;
                            if (rref.Recipe < 0 || rref.Recipe >= recipeIndexMap.Length) continue;

                            int mapped = recipeIndexMap[rref.Recipe];
                            if (mapped < 0 || mapped >= recipes.Count) continue;

                            list.Add((recipes[mapped], rref.GroupKey));
                        }
                    }
                    index.CodeToRecipeGroups[kv.Key] = list;
                }

                foreach (var kv in index.CodeToRecipeGroups)
                {
                    if (!index.CodeToGkeys.TryGetValue(kv.Key, out var gset))
                        index.CodeToGkeys[kv.Key] = gset = new HashSet<string>(StringComparer.Ordinal);

                    foreach (var pair in kv.Value)
                    {
                        if (!string.IsNullOrEmpty(pair.GroupKey)) gset.Add(pair.GroupKey);
                    }
                }

                index.OutputsIndex = BuildOutputsIndexFrom(recipes);
                index.RecipesFetched = recipes.Count;
                index.RecipesUsable = recipes.Count;
                return index;
            }
            catch { return null; }
        }

        private static void StartRecipeIndexBuild(ICoreClientAPI capi)
        {
            if (capi == null) return;

            bool shouldStart = false;
            int buildToken = 0;
            lock (recipeIndexByVariant)
            {
                bool allPresent = RecipeIndexVariants.All(v => recipeIndexByVariant.ContainsKey(v.VariantKey));
                if (recipeIndexBuilt && allPresent) return;
                if (recipeIndexBuildStarted) return;

                recipeIndexBuildStarted = true;
                recipeIndexBuilt = false;
                buildToken = Interlocked.Increment(ref recipeIndexBuildToken);
                shouldStart = true;
            }

            if (!shouldStart) return;

            var localToken = buildToken;
            var sw = Stopwatch.StartNew();
            try
            {
                recipeIndexBuildTask = Task.Run(() =>
                {
                    try
                    {
                        foreach (var variant in RecipeIndexVariants)
                        {
                            if (localToken != Volatile.Read(ref recipeIndexBuildToken)) return;

                            var variantKey = variant.VariantKey;
                            var data = LoadRecipeIndex(capi, variant.IncludeAll, variant.ModsOnly, variant.WoodOnly, variant.StoneOnly);
                            bool rebuilt = false;
                            if (data == null)
                            {
                                data = BuildRecipeIndex(capi, variant.IncludeAll, variant.ModsOnly, variant.WoodOnly, variant.StoneOnly);
                                rebuilt = true;
                            }

                            if (localToken != Volatile.Read(ref recipeIndexBuildToken)) return;

                            StoreRecipeIndex(variantKey, data);

                            if (rebuilt)
                            {
                                LogEverywhere(capi,
                                    $"Recipe index built for variant={variantKey}; keeping DNA & cache. Next scan will decide.",
                                    caller: nameof(StartRecipeIndexBuild));
                            }

                            if (localToken != Volatile.Read(ref recipeIndexBuildToken)) return;

                            var activeTab = GetActiveTabKey();
                            if (string.Equals(activeTab, TabKeyFromVariant(variantKey), StringComparison.Ordinal))
                            {
                                ApplyRecipeIndexVariant(variantKey);
                            }
                        }

                        if (localToken != Volatile.Read(ref recipeIndexBuildToken)) return;

                        recipeIndexBuilt = true;
                        var activeVariantFinal = VariantKeyFromTabKey(GetActiveTabKey());
                        if (localToken != Volatile.Read(ref recipeIndexBuildToken)) return;
                        ApplyRecipeIndexVariant(activeVariantFinal);
                        if (localToken != Volatile.Read(ref recipeIndexBuildToken)) return;
                        GetCachedPageCodeMap(capi);
                    }
                    catch (Exception e)
                    {
                        LogEverywhere(capi, $"Failed to build recipe index: {e}", caller: nameof(StartRecipeIndexBuild));
                    }
                    finally
                    {
                        if (localToken != Volatile.Read(ref recipeIndexBuildToken))
                        {
                            lock (recipeIndexByVariant)
                            {
                                recipeIndexBuildStarted = false;
                                recipeIndexBuilt = false;
                            }
                        }
                    }
                });
            }
            finally
            {
                sw.Stop();
                LogEverywhere(capi, $"StartRecipeIndexBuild completed in {sw.ElapsedMilliseconds}ms", caller: nameof(StartRecipeIndexBuild));
            }
        }

        private static RecipeIndexData BuildRecipeIndex(ICoreClientAPI capi, bool includeAll, bool modsOnly, bool woodOnly, bool stoneOnly)
        {
            var sw = Stopwatch.StartNew();
            var index = new RecipeIndexData();

            var recipes = GetAllGridRecipes(capi, out var fetched, out var usable, includeAll ? (bool?)null : modsOnly);

            if (includeAll)
            {
                // keep all recipes
            }
            else if (woodOnly)
                recipes = recipes.Where(r => !r.IsMod && IsWoodRecipe(r.Raw)).ToList();
            else if (stoneOnly)
                recipes = recipes.Where(r => !r.IsMod && IsStoneRecipe(r.Raw)).ToList();
            else if (!modsOnly)
                recipes = recipes.Where(r => !r.IsMod && !IsWoodRecipe(r.Raw) && !IsStoneRecipe(r.Raw)).ToList();

            index.RecipesFetched = fetched;
            index.RecipesUsable = usable;

            recipeIndexBuildTotal = recipes.Count;
            recipeIndexBuildProgress = 0;
            index.OutputsIndex = BuildOutputsIndexFrom(recipes);

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
                        catch { }
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
                        if (ing.PatternCode != null)
                        {
                            index.WildcardGroups.Add(new WildGroup { Recipe = r, GroupKey = groupKey, Type = ing.Type, Pattern = ing.PatternCode, Allowed = ing.Allowed });
                        }
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
                        if (!index.CodeToRecipeGroups.TryGetValue(code, out var list))
                            index.CodeToRecipeGroups[code] = list = new List<(GridRecipeShim Recipe, string GroupKey)>();
                        if (!list.Any(p => ReferenceEquals(p.Recipe, r) && p.GroupKey == groupKey))
                            list.Add((r, groupKey));
                    }

                    foreach (var code in satisfiableCodes)
                    {
                        if (string.IsNullOrEmpty(code)) continue;
                        if (!index.CodeToGkeys.TryGetValue(code, out var gset))
                            index.CodeToGkeys[code] = gset = new HashSet<string>(StringComparer.Ordinal);
                        gset.Add(groupKey);
                    }
                }

                index.RecipeGroupNeeds[r] = groups;
                recipeIndexBuildProgress++;
            }

            sw.Stop();
            long elapsedMs = sw.ElapsedMilliseconds;
            capi.Event.EnqueueMainThreadTask(() =>
                LogEverywhere(capi, $"Recipe index build processed {recipeIndexBuildProgress}/{recipeIndexBuildTotal} recipes in {elapsedMs}ms", caller: nameof(BuildRecipeIndex)), null);

            return index;
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

            var nameAl = TryGetMember(t, raw, "Name") as AssetLocation;
            if (ShouldSkipRecipeByName(nameAl)) return null;

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
        private static Dictionary<StackKey, List<GridRecipeShim>> BuildOutputsIndexFrom(List<GridRecipeShim> recipes)
        {
            var index = new Dictionary<StackKey, List<GridRecipeShim>>();
            foreach (var r in recipes)
            {
                if (r?.Outputs == null) continue;
                foreach (var o in r.Outputs)
                {
                    if (o?.Collectible?.Code == null) continue;
                    var key = KeyFor(o);
                    if (!index.TryGetValue(key, out var list))
                        index[key] = list = new List<GridRecipeShim>(2);
                    list.Add(r);
                }
            }
            return index;
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

        private static bool RecipeOutputsMatchDesired(GridRecipeShim shim, ItemStack desired)
        {
            if (desired == null) return true;
            if (shim == null || shim.Outputs == null || shim.Outputs.Count == 0) return false;

            foreach (var st in shim.Outputs)
            {
                if (st != null && st.Satisfies(desired) && desired.Satisfies(st))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RecipeSatisfiedByPool(ICoreClientAPI capi, ResourcePool pool, GridRecipeShim shim, ItemStack desired)
        {
            if (shim == null) return false;
            if (!RecipeOutputsMatchDesired(shim, desired)) return false;

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

            return true;
        }

        private static IEnumerable<GridRecipeShim> CandidateShimsForStack(
            ICoreClientAPI capi,
            ItemStack desired,
            bool? modsOnly,
            Dictionary<StackKey, List<GridRecipeShim>> index)
        {
            var key = KeyFor(desired);
            if (index == null || !index.TryGetValue(key, out var list) || list == null) yield break;

            foreach (var shim in list)
            {
                if (shim == null) continue;
                if (modsOnly.HasValue && (modsOnly.Value != shim.IsMod)) continue;
                yield return shim;
            }
        }

        private static void AddCraftablePagesFromAllStacksFiltered(
            ICoreClientAPI capi,
            ResourcePool pool,
            HashSet<string> dest,
            int partitions,
            Dictionary<StackKey, List<GridRecipeShim>> index,
            System.Func<GridRecipeShim, bool> recipePredicate,
            string callerName)
        {
            try
            {
                var predicate = recipePredicate ?? (_ => true);
                callerName ??= nameof(AddCraftablePagesFromAllStacks);

                var msType = AccessTools.TypeByName("Vintagestory.GameContent.ModSystemSurvivalHandbook");
                var ms = msType != null ? GetModSystemByType(capi, msType) : null;
                var stacks = AccessTools.Field(msType, "allstacks")?.GetValue(ms) as ItemStack[];
                if (stacks == null || stacks.Length == 0) return;

                var ghType = AccessTools.TypeByName("Vintagestory.GameContent.GuiHandbookItemStackPage");
                var ctor = ghType?.GetConstructor(new[] { typeof(ICoreClientAPI), typeof(ItemStack) });
                var fiStack = AccessTools.Field(ghType, "Stack");
                var miPageCode = ghType?.GetMethod("PageCodeForStack", BindingFlags.Public | BindingFlags.Static);
                if (ctor == null || miPageCode == null) return;

                const int chunk = 32;
                if (partitions <= 0) partitions = RecommendParallelism(stacks.Length, chunkSize: chunk);
                if (partitions == 1 || stacks.Length < 2)
                {
                    foreach (var st in stacks)
                    {
                        if (st?.Collectible == null) continue;
                        object page = ctor.Invoke(new object[] { capi, st });
                        var pStack = fiStack?.GetValue(page) as ItemStack ?? st;

                        foreach (var shim in CandidateShimsForStack(capi, pStack, modsOnly: false, index))
                        {
                            if (!predicate(shim)) continue;
                            if (RecipeSatisfiedByPool(capi, pool, shim, pStack))
                            {
                                var pc = miPageCode.Invoke(null, new object[] { pStack }) as string;
                                if (!string.IsNullOrEmpty(pc)) dest.Add(pc);
                                break;
                            }
                        }
                    }
                    return;
                }

                int[] order;
                if (index != null && index.Count > 0)
                {
                    order = Enumerable.Range(0, stacks.Length)
                        .Select(i =>
                        {
                            var st = stacks[i];
                            var key = KeyFor(st);
                            return (i, weight: index.TryGetValue(key, out var list) ? list.Count : 0);
                        })
                        .OrderByDescending(t => t.weight)
                        .Select(t => t.i)
                        .ToArray();
                }
                else
                {
                    order = Enumerable.Range(0, stacks.Length).ToArray();
                }

                partitions = Math.Min(partitions, stacks.Length);

                var swTotal = Stopwatch.StartNew();
                var tasks = new Task<HashSet<string>>[partitions];
                int next = 0;

                for (int p = 0; p < partitions; p++)
                {
                    int partIndex = p;
                    tasks[p] = Task.Run(() =>
                    {
                        var local = new HashSet<string>(StringComparer.Ordinal);
                        var swPart = Stopwatch.StartNew();
                        var swSlice = Stopwatch.StartNew();
                        int processed = 0;

                        while (true)
                        {
                            int start = Interlocked.Add(ref next, chunk) - chunk;
                            if (start >= order.Length) break;
                            int end = Math.Min(start + chunk, order.Length);

                            for (int k = start; k < end; k++)
                            {
                                int i = order[k];
                                var st = stacks[i];
                                if (st?.Collectible == null) continue;

                                var swItem = Stopwatch.StartNew();
                                object page = ctor.Invoke(new object[] { capi, st });
                                var pStack = fiStack?.GetValue(page) as ItemStack ?? st;

                                var key2page = GetCachedPageCodeMap(capi);

                                foreach (var shim in CandidateShimsForStack(capi, pStack, modsOnly: false, index))
                                {
                                    if (!predicate(shim)) continue;
                                    if (RecipeSatisfiedByPool(capi, pool, shim, pStack))
                                    {
                                        if (!key2page.TryGetValue(KeyFor(pStack), out var pageCode))
                                            pageCode = miPageCode.Invoke(null, new object[] { pStack }) as string;

                                        if (!string.IsNullOrEmpty(pageCode)) local.Add(pageCode);
                                        break;
                                    }
                                }
                                swItem.Stop();
                                if (swItem.ElapsedMilliseconds > SlowStackLogThresholdMs)
                                {
                                    string stackIdent = pStack?.Collectible?.Code?.ToString()
                                        ?? st.Collectible?.Code?.ToString()
                                        ?? $"index={i}";
                                    LogEverywhere(capi,
                                        $"Slow stack {stackIdent} took {swItem.ElapsedMilliseconds}ms to process on partition {partIndex + 1}/{partitions}",
                                        caller: callerName);
                                }
                                processed++;
                            }

                            if (swSlice.ElapsedMilliseconds >= 3)
                            {
                                Thread.Yield();
                                swSlice.Restart();
                            }
                        }

                        swPart.Stop();
                        LogEverywhere(capi, $"Partition {partIndex + 1}/{partitions} processed {processed} item stacks in {swPart.ElapsedMilliseconds}ms",
                            caller: callerName);
                        return local;
                    });
                }

                Task.WaitAll(tasks);
                foreach (var t in tasks) foreach (var pc in t.Result) dest.Add(pc);

                swTotal.Stop();
                LogEverywhere(capi, $"Processed {stacks.Length} item stacks in {swTotal.ElapsedMilliseconds}ms using {partitions} partitions",
                    caller: callerName);
            }
            catch { }
        }

        private static void AddCraftablePagesFromAllStacks(
            ICoreClientAPI capi, ResourcePool pool, HashSet<string> dest,
            Dictionary<StackKey, List<GridRecipeShim>> index)
        {
            AddCraftablePagesFromAllStacks(capi, pool, dest, ConfiguredAllStacksPartitions, index);
        }

        private static void AddCraftablePagesFromAllStacks(
            ICoreClientAPI capi, ResourcePool pool, HashSet<string> dest, int partitions,
            Dictionary<StackKey, List<GridRecipeShim>> index)
        {
            AddCraftablePagesFromAllStacksFiltered(
                capi,
                pool,
                dest,
                partitions,
                index,
                shim => !IsWoodRecipe(shim.Raw),
                nameof(AddCraftablePagesFromAllStacks));
        }

        private static void AddCraftablePagesFromAllStacksFromModStacks(
    ICoreClientAPI capi, ResourcePool pool, HashSet<string> dest,
    Dictionary<StackKey, List<GridRecipeShim>> index)
        {
            AddCraftablePagesFromAllStacksFromModStacks(capi, pool, dest, ConfiguredAllStacksPartitions, index);
        }

        private static void AddCraftablePagesFromAllStacksFromModStacks(
            ICoreClientAPI capi, ResourcePool pool, HashSet<string> dest, int partitions,
            Dictionary<StackKey, List<GridRecipeShim>> index)
        {
            var swTotal = Stopwatch.StartNew();
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

                const int chunk = 64;
                if (partitions <= 0) partitions = RecommendParallelism(stacks.Length, chunkSize: chunk);
                if (partitions == 1 || stacks.Length < 2)
                {
                    foreach (var st in stacks)
                    {
                        if (st?.Collectible == null) continue;
                        object page = ctor.Invoke(new object[] { capi, st });
                        var pStack = fiStack?.GetValue(page) as ItemStack ?? st;

                        foreach (var shim in CandidateShimsForStack(capi, pStack, modsOnly: true, index))
                        {
                            if (RecipeSatisfiedByPool(capi, pool, shim, pStack))
                            {
                                var pc = miPageCode.Invoke(null, new object[] { pStack }) as string;
                                if (!string.IsNullOrEmpty(pc)) dest.Add(pc);
                                break;
                            }
                        }
                    }
                    return;
                }

                partitions = Math.Min(partitions, stacks.Length);
                var tasks = new Task<HashSet<string>>[partitions];
                int next = 0;

                for (int p = 0; p < partitions; p++)
                {
                    int partIndex = p;
                    tasks[p] = Task.Run(() =>
                    {
                        var local = new HashSet<string>(StringComparer.Ordinal);
                        var swPart = Stopwatch.StartNew();
                        var swSlice = Stopwatch.StartNew();
                        int processed = 0;

                        while (true)
                        {
                            int start = Interlocked.Add(ref next, chunk) - chunk;
                            if (start >= stacks.Length) break;
                            int end = Math.Min(start + chunk, stacks.Length);

                            for (int i = start; i < end; i++)
                            {
                                var st = stacks[i];
                                if (st?.Collectible == null) continue;

                                var swItem = Stopwatch.StartNew();
                                object page = ctor.Invoke(new object[] { capi, st });
                                var pStack = fiStack?.GetValue(page) as ItemStack ?? st;

                                foreach (var shim in CandidateShimsForStack(capi, pStack, modsOnly: true, index))
                                {
                                    if (RecipeSatisfiedByPool(capi, pool, shim, pStack))
                                    {
                                        var pc = miPageCode.Invoke(null, new object[] { pStack }) as string;
                                        if (!string.IsNullOrEmpty(pc)) local.Add(pc);
                                        break;
                                    }
                                }
                                swItem.Stop();
                                if (swItem.ElapsedMilliseconds > SlowStackLogThresholdMs)
                                {
                                    string stackIdent = pStack?.Collectible?.Code?.ToString()
                                        ?? st.Collectible?.Code?.ToString()
                                        ?? $"index={i}";
                                    LogEverywhere(capi,
                                        $"Slow stack {stackIdent} took {swItem.ElapsedMilliseconds}ms to process on partition {partIndex + 1}/{partitions}",
                                        caller: nameof(AddCraftablePagesFromAllStacksFromModStacks));
                                }
                                processed++;
                            }

                            if (swSlice.ElapsedMilliseconds >= 3)
                            {
                                Thread.Yield();
                                swSlice.Restart();
                            }
                        }

                        swPart.Stop();
                        LogEverywhere(capi, $"Partition {partIndex + 1}/{partitions} processed {processed} item stacks in {swPart.ElapsedMilliseconds}ms",
                            caller: nameof(AddCraftablePagesFromAllStacksFromModStacks));
                        return local;
                    });
                }

                Task.WaitAll(tasks);
                foreach (var t in tasks) foreach (var pc in t.Result) dest.Add(pc);

                swTotal.Stop();
                LogEverywhere(capi, $"Processed {stacks.Length} item stacks in {swTotal.ElapsedMilliseconds}ms using {partitions} partitions",
                    caller: nameof(AddCraftablePagesFromAllStacksFromModStacks));
            }
            catch { }
        }

        private static void AddCraftablePagesFromAllStacks_WoodOnly(
            ICoreClientAPI capi, ResourcePool pool, HashSet<string> dest,
            Dictionary<StackKey, List<GridRecipeShim>> index)
        {
            AddCraftablePagesFromAllStacks_WoodOnly(
                capi,
                pool,
                dest,
                ConfiguredAllStacksPartitions,
                index);
        }

        private static void AddCraftablePagesFromAllStacks_WoodOnly(
            ICoreClientAPI capi, ResourcePool pool, HashSet<string> dest, int partitions,
            Dictionary<StackKey, List<GridRecipeShim>> index)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                AddCraftablePagesFromAllStacksFiltered(
                    capi,
                    pool,
                    dest,
                    partitions,
                    index,
                    shim => IsWoodRecipe(shim.Raw),
                    nameof(AddCraftablePagesFromAllStacks_WoodOnly));
            }
            catch { }
            finally
            {
                sw.Stop();
                LogEverywhere(capi, $"AddCraftablePagesFromAllStacks_WoodOnly completed in {sw.ElapsedMilliseconds}ms",
                    caller: nameof(AddCraftablePagesFromAllStacks_WoodOnly));
            }
        }

        private static void AddCraftablePagesFromAllStacks_StoneOnly(
            ICoreClientAPI capi, ResourcePool pool, HashSet<string> dest,
            Dictionary<StackKey, List<GridRecipeShim>> index)
        {
            AddCraftablePagesFromAllStacks_StoneOnly(
                capi,
                pool,
                dest,
                ConfiguredAllStacksPartitions,
                index);
        }

        private static void AddCraftablePagesFromAllStacks_StoneOnly(
            ICoreClientAPI capi, ResourcePool pool, HashSet<string> dest, int partitions,
            Dictionary<StackKey, List<GridRecipeShim>> index)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                AddCraftablePagesFromAllStacksFiltered(
                    capi,
                    pool,
                    dest,
                    partitions,
                    index,
                    shim => IsStoneRecipe(shim.Raw),
                    nameof(AddCraftablePagesFromAllStacks_StoneOnly));
            }
            catch { }
            finally
            {
                sw.Stop();
                LogEverywhere(capi, $"AddCraftablePagesFromAllStacks_StoneOnly completed in {sw.ElapsedMilliseconds}ms",
                    caller: nameof(AddCraftablePagesFromAllStacks_StoneOnly));
            }
        }

        private void OnServerScanReply(CraftScanReply data)
        {
            try
            {
                if (data?.IsFetch == true)
                {
                    int codeCount = data.Codes?.Count ?? 0;
                    int scanId = data.ScanId;

                    _capi.Event.EnqueueMainThreadTask(() =>
                    {
                        try
                        {
                            LogEverywhere(_capi,
                                $"[Fetch] ← #{scanId} stacks={codeCount} (skipping cache rebuild)",
                                toChat: true);
                        }
                        finally
                        {
                            FinishScanAndProcessQueue(_capi);
                        }
                    }, "SCFetchReply");

                    return;
                }

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

                string tabKey = data.TabKey;
                string variantKey = null;
                bool recipeAll = false, recipeModsOnly = false, recipeWoodOnly = false, recipeStoneOnly = false;

                ScanRequestInfo info = default;
                bool haveInfo = false;
                if (data.ScanId != 0)
                {
                    lock (InflightMapLock)
                    {
                        if (InflightById.TryGetValue(data.ScanId, out info))
                        {
                            InflightById.Remove(data.ScanId);
                            haveInfo = true;
                        }
                    }
                }

                if (string.IsNullOrEmpty(tabKey) && haveInfo && !string.IsNullOrEmpty(info.TabKey))
                {
                    tabKey = info.TabKey;
                    recipeAll = info.IncludeAll;
                    recipeModsOnly = info.ModsOnly;
                    recipeWoodOnly = info.WoodOnly;
                    recipeStoneOnly = info.StoneOnly;
                    variantKey = GetVariantKey(recipeAll, recipeModsOnly, recipeWoodOnly, recipeStoneOnly);
                }

                if (string.IsNullOrEmpty(tabKey))
                {
                    lock (PendingScanLock)
                    {
                        variantKey = PendingScanVariantKey;
                        tabKey = PendingScanTabKey;
                        PendingScanVariantKey = null;
                        PendingScanTabKey = null;
                    }
                    if (string.IsNullOrEmpty(tabKey))
                        tabKey = !string.IsNullOrEmpty(variantKey) ? TabKeyFromVariant(variantKey) : GetActiveTabKey();
                    if (string.IsNullOrEmpty(variantKey)) variantKey = VariantKeyFromTabKey(tabKey);

                    recipeAll = string.Equals(variantKey, "all", StringComparison.Ordinal);
                    recipeModsOnly = string.Equals(variantKey, "mods", StringComparison.Ordinal);
                    recipeWoodOnly = string.Equals(variantKey, "wood", StringComparison.Ordinal);
                    recipeStoneOnly = string.Equals(variantKey, "stone", StringComparison.Ordinal);
                }

                ulong dna = ComputeResourcePoolDNA(pool);
                bool dnaMatch;
                ulong prev;
                lock (DnaLock)
                {
                    dnaMatch = TabPoolDNA.TryGetValue(tabKey, out prev) && prev == dna;
                }

                int cachedPages;
                lock (CacheLock)
                {
                    var cache = GetTabCache(tabKey);
                    cachedPages = cache?.Count ?? 0;
                }
                bool cacheEmpty = cachedPages == 0;

                if (dnaMatch && !cacheEmpty)
                {
                    SetTabReadyToUpdateUI(tabKey, true);
                    _capi.Event.EnqueueMainThreadTask(() =>
                    {
                        try
                        {
                            LogEverywhere(_capi, $"[Scan] ← #{data.ScanId} DNA MATCH tab={tabKey} dna=0x{dna:X16} pages={cachedPages}", toChat: true);
                            TryUpdateActiveTabFromCache(_capi, tabKey);
                        }
                        finally
                        {
                            FinishScanAndProcessQueue(_capi);
                        }
                    }, "SCScanMatch");
                    return;
                }

                string rebuildReason = dnaMatch ? "CACHE EMPTY" : "DNA MISMATCH";
                SetTabReadyToUpdateUI(tabKey, false);

                _capi.Event.EnqueueMainThreadTask(() =>
                {
                    if (IsTabActive(tabKey)) SetUpdatingText(_capi, true);
                }, "SCSetUpdating");

                Task.Run(() =>
                {
                    try
                    {
                        int pages = RebuildCacheWithPool(_capi, pool, tabKey,
                            out int outputs, out int fetched, out int usable, out _);

                        lock (DnaLock) { TabPoolDNA[tabKey] = dna; }
                        SetTabReadyToUpdateUI(tabKey, true);

                        _capi.Event.EnqueueMainThreadTask(() =>
                        {
                            try
                            {
                                LogEverywhere(_capi,
                                    $"[Scan] ← #{data.ScanId} REBUILD ({rebuildReason}) tab={tabKey} dna=0x{dna:X16} outputs={outputs}, pages={pages}, fetched={fetched}, usable={usable}",
                                    toChat: true);
                                TryUpdateActiveTabFromCache(_capi, tabKey);
                            }
                            finally
                            {
                                FinishScanAndProcessQueue(_capi);
                            }
                        }, "SCShowNewCache");
                    }
                    catch (Exception ex)
                    {
                        _capi.Event.EnqueueMainThreadTask(() =>
                        {
                            try
                            {
                                LogEverywhere(_capi, $"Rebuild failed: {ex}", toChat: true);
                            }
                            finally
                            {
                                FinishScanAndProcessQueue(_capi);
                            }
                        }, "SCRebuildFail");
                    }
                });
            }
            catch (Exception e)
            {
                LogEverywhere(_capi, $"OnServerScanReply error: {e}", toChat: true);
                FinishScan(_capi);
                _capi?.Event.EnqueueMainThreadTask(() => TryProcessQueuedScan(_capi), "SCScanReplyError");
            }
        }

        private void OnFetchAvailabilityMessage(FetchAvailabilityMessage message)
        {
            try
            {
                bool disabled = message?.FetchDisabled == true;
                bool wasDisabled = IsServerFetchDisabled();
                SetServerFetchDisabled(disabled);

                if (_capi == null || wasDisabled == disabled) return;

                string text = disabled
                    ? "[ShowCraftable] Fetching ingredients has been disabled by the server."
                    : "[ShowCraftable] Fetching ingredients has been enabled by the server.";

                _capi.Event.EnqueueMainThreadTask(() =>
                {
                    try
                    {
                        _capi.ShowChatMessage(text);
                    }
                    catch { }
                }, "SCFetchAvailabilityChanged");
            }
            catch { }
        }

        private static int RebuildCacheWithPool(
    ICoreClientAPI capi, ResourcePool pool, string tabKey,
    out int craftableOutputsCount, out int fetched, out int usable,
    out List<string> generatedPageCodes)
        {
            var sw = Stopwatch.StartNew();
            var variantKey = VariantKeyFromTabKey(tabKey);
            if (!EnsureRecipeIndexVariantReady(capi, variantKey))
            {
                throw new InvalidOperationException($"Recipe index variant '{variantKey}' not ready");
            }

            bool variantAll = string.Equals(variantKey, "all", StringComparison.Ordinal);
            bool variantModsOnly = string.Equals(variantKey, "mods", StringComparison.Ordinal);
            bool variantWoodOnly = string.Equals(variantKey, "wood", StringComparison.Ordinal);
            bool variantStoneOnly = string.Equals(variantKey, "stone", StringComparison.Ordinal);
            craftableOutputsCount = 0; fetched = recipesFetched; usable = recipesUsable;

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

            var groupAvail = new Dictionary<string, int>(StringComparer.Ordinal);

            var ambRecipes = new HashSet<GridRecipeShim>(ReferenceEqualityComparer.Instance);

            foreach (var pkv in pool.Counts)
            {
                var code = pkv.Key.Code;
                var haveQty = pkv.Value;
                if (haveQty <= 0) continue;

                if (!codeToRecipeGroups.TryGetValue(code, out var uses) || uses == null || uses.Count == 0)
                    continue;

                var hitsPerRecipe = new Dictionary<GridRecipeShim, int>(ReferenceEqualityComparer.Instance);

                foreach (var (recipe, gkey) in uses)
                {
                    if (!remaining.TryGetValue(recipe, out var groups)) continue;
                    if (!groups.TryGetValue(gkey, out var need) || need <= 0) continue;

                    int take = Math.Min(need, haveQty);
                    if (take > 0) groups[gkey] = need - take;

                    hitsPerRecipe[recipe] = (hitsPerRecipe.TryGetValue(recipe, out var c) ? c : 0) + 1;
                }

                foreach (var kv2 in hitsPerRecipe)
                    if (kv2.Value >= 2) ambRecipes.Add(kv2.Key);
            }

            bool CanSatisfyPrecisely_NoGkeyToCodes(GridRecipeShim recipe, ResourcePool poolLocal)
            {
                if (recipe == null) return false;

                var codeAvail = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var pkv in poolLocal.Counts) codeAvail[pkv.Key.Code] = pkv.Value;

                foreach (var gkv in recipeGroupNeeds[recipe])
                {
                    int need = gkv.Value;
                    if (need <= 0) continue;

                    IEnumerable<string> candidates;
                    if (gkv.Key.StartsWith("wild:", StringComparison.Ordinal))
                    {
                        candidates = codeAvail.Keys.Where(c =>
                            codeToGkeys.TryGetValue(c, out var set) && set != null && set.Contains(gkv.Key));
                    }
                    else
                    {
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

            var key2page = GetCachedPageCodeMap(capi);
            var resultPageCodes = new HashSet<string>(StringComparer.Ordinal);
            var ghType = AccessTools.TypeByName("Vintagestory.GameContent.GuiHandbookItemStackPage");
            var miPageCodeForStack = ghType?.GetMethod("PageCodeForStack", BindingFlags.Public | BindingFlags.Static);

            int fromMap = 0;
            int hbStackFallbacks = 0;

            const int chunkSize = 64;
            int processed = 0;

            void Flush()
            {
                if (!IsTabActive(tabKey)) return;
                var snapshot = resultPageCodes.ToList();
                lock (CacheLock) CachedPageCodes = snapshot;
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

            if (!TryGetRecipeIndex(variantKey, out var idxData))
                throw new InvalidOperationException($"Recipe index variant '{variantKey}' not ready");
            var indexSnapshot = idxData.OutputsIndex ?? new Dictionary<StackKey, List<GridRecipeShim>>();

            if (variantAll)
            {
                AddCraftablePagesFromAllStacksFromModStacks(capi, pool, resultPageCodes, indexSnapshot);
                AddCraftablePagesFromAllStacks_WoodOnly(capi, pool, resultPageCodes, indexSnapshot);
                AddCraftablePagesFromAllStacks_StoneOnly(capi, pool, resultPageCodes, indexSnapshot);
                AddCraftablePagesFromAllStacks(capi, pool, resultPageCodes, indexSnapshot);
            }
            else if (variantModsOnly)
                AddCraftablePagesFromAllStacksFromModStacks(capi, pool, resultPageCodes, indexSnapshot);
            else if (variantWoodOnly)
                AddCraftablePagesFromAllStacks_WoodOnly(capi, pool, resultPageCodes, indexSnapshot);
            else if (variantStoneOnly)
                AddCraftablePagesFromAllStacks_StoneOnly(capi, pool, resultPageCodes, indexSnapshot);
            else
                AddCraftablePagesFromAllStacks(capi, pool, resultPageCodes, indexSnapshot);

            craftableOutputsCount = resultPageCodes.Count;
            Flush();

            var finalPages = resultPageCodes.ToList();
            generatedPageCodes = finalPages;

            lock (CacheLock)
            {
                SetTabCache(tabKey, finalPages);
            }

            sw.Stop();
            LogEverywhere(capi, $"RebuildCacheWithPool completed in {sw.ElapsedMilliseconds}ms", caller: nameof(RebuildCacheWithPool));

            return finalPages.Count;
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
        [ProtoMember(2)] public bool CollectItems { get; set; }
        [ProtoMember(3)] public List<CraftIngredientList> Variants { get; set; } = new();
        [ProtoMember(4)] public int ScanId { get; set; }
        [ProtoMember(5)] public string TabKey { get; set; }
    }

    [ProtoContract]
    public class CraftScanReply
    {
        [ProtoMember(1)] public List<string> Codes { get; set; } = new();
        [ProtoMember(2)] public List<int> Counts { get; set; } = new();
        [ProtoMember(3)] public List<EnumItemClass> Classes { get; set; } = new();
        [ProtoMember(4)] public int ScanId { get; set; }
        [ProtoMember(5)] public string TabKey { get; set; }
        [ProtoMember(6)] public bool IsFetch { get; set; }
    }

    [ProtoContract]
    public class FetchAvailabilityMessage
    {
        [ProtoMember(1)] public bool FetchDisabled { get; set; }
    }

    public class ShowCraftableServerSystem : ModSystem
    {
        private ICoreServerAPI sapi;
        private IServerNetworkChannel channel;

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;

            channel = api.Network.RegisterChannel(ShowCraftableSystem.ChannelName);
            channel.RegisterMessageType(typeof(CraftScanRequest));
            channel.RegisterMessageType(typeof(CraftScanReply));
            channel.RegisterMessageType(typeof(FetchAvailabilityMessage));
            channel.SetMessageHandler<CraftScanRequest>(OnScanRequest);

            api.Event.PlayerNowPlaying += Event_PlayerNowPlaying;

            RegisterCommands();

            BroadcastFetchAvailability();
        }

        private void RegisterCommands()
        {
            sapi?.ChatCommands
                ?.Create("showcraftablefetch")
                ?.RequiresPrivilege(Privilege.controlserver)
                ?.WithDescription("Controls the ShowCraftable fetch button for all clients.")
                ?.WithArgs(sapi.ChatCommands.Parsers.OptionalWord("state"))
                ?.HandleWith(OnShowCraftableFetchCommand);
        }

        private TextCommandResult OnShowCraftableFetchCommand(TextCommandCallingArgs args)
        {
            string arg = args?.RawArgs?.PopWord()?.ToLowerInvariant();

            if (string.IsNullOrEmpty(arg) || arg == "status")
            {
                string status = ShowCraftableSystem.IsServerFetchDisabled() ? "disabled" : "enabled";
                return TextCommandResult.Success($"Fetch button is currently {status} for all clients.");
            }

            bool disable;
            if (arg == "on" || arg == "enable" || arg == "true")
            {
                disable = false;
            }
            else if (arg == "off" || arg == "disable" || arg == "false")
            {
                disable = true;
            }
            else
            {
                return TextCommandResult.Error("Usage: /showcraftablefetch <on|off|enable|disable|true|false|status>");
            }

            ShowCraftableSystem.SetServerFetchDisabledAndSave(sapi, disable);
            BroadcastFetchAvailability();

            string result = disable ? "disabled" : "enabled";
            return TextCommandResult.Success($"Fetch button has been {result} for all clients.");
        }

        private void Event_PlayerNowPlaying(IServerPlayer byPlayer)
        {
            SendFetchAvailability(byPlayer);
        }

        private void BroadcastFetchAvailability()
        {
            if (channel == null || sapi?.World?.AllOnlinePlayers == null) return;

            var message = new FetchAvailabilityMessage
            {
                FetchDisabled = ShowCraftableSystem.IsServerFetchDisabled()
            };

            foreach (var player in sapi.World.AllOnlinePlayers)
            {
                if (player is IServerPlayer serverPlayer)
                {
                    channel.SendPacket(message, serverPlayer);
                }
            }
        }

        private void SendFetchAvailability(IServerPlayer player)
        {
            if (player == null || channel == null) return;

            var message = new FetchAvailabilityMessage
            {
                FetchDisabled = ShowCraftableSystem.IsServerFetchDisabled()
            };

            channel.SendPacket(message, player);
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

            bool isFetch = req.CollectItems && req.Variants != null && req.Variants.Count > 0;

            if (isFetch && ShowCraftableSystem.IsServerFetchDisabled())
            {
                fromPlayer?.SendMessage(GlobalConstants.InfoLogChatGroup,
                    "[ShowCraftable] Fetching ingredients is disabled by the server.",
                    EnumChatType.CommandError);

                var denied = new CraftScanReply
                {
                    ScanId = req.ScanId,
                    TabKey = req.TabKey,
                    IsFetch = true
                };

                channel?.SendPacket(denied, fromPlayer);
                return;
            }

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

            if (isFetch)
            {
                var counts = sum.ToDictionary(kv => kv.Key, kv => kv.Value.count);
                bool done = false;
                bool nospace = false;
                bool partial = false;

                foreach (var variant in req.Variants)
                {
                    bool ignorePlayer = CanSatisfyVariant(playerCounts, variant);
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

                    if (!CanSatisfyVariant(checkCounts, variant))
                    {
                        continue;
                    }

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
                    if (nospace)
                    {
                        string msg = "[ShowCraftable] Not enough inventory space to fetch the ingredients!";
                        fromPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, msg, EnumChatType.CommandError);
                    }
                    else
                    {
                        string msg = "[ShowCraftable] Could not find required ingredients to fetch.";
                        fromPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, msg, EnumChatType.CommandError);
                    }
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
                Classes = sum.Values.Select(v => v.cls).ToList(),
                ScanId = req.ScanId,
                TabKey = req.TabKey,
                IsFetch = isFetch
            };
            sapi.Network.GetChannel(ShowCraftableSystem.ChannelName).SendPacket(reply, fromPlayer);

        }

    }
}
