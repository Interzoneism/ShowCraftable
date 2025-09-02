// File: ShowCraftableModSystem.cs
// A from-scratch, robust implementation for a "Craftable" handbook tab.
// It gathers recipes from all major crafting systems (grid, smithing, knapping, etc.)
// into a unified cache for high-performance checking.
//
// Requires: HarmonyLib, VintagestoryAPI.dll, VintagestoryLib.dll, Vintagestory.GameContent.dll

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ShowCraftable.Features
{
    #region Unified Recipe Wrapper
    // ======================================================================================================
    // UNIFIED RECIPE CACHE
    // This is the core of the new system. We wrap all different recipe types from the game
    // into a single, standardized format. This makes checking them much easier and faster.
    // ======================================================================================================

    /// <summary>
    /// A standardized wrapper for a single ingredient, abstracting away the differences
    /// between Grid, Smithing, Barrel ingredients, etc.
    /// </summary>
    public class StandardizedIngredient
    {
        public bool IsTool { get; set; }
        public int Quantity { get; set; }
        public CraftingRecipeIngredient BackingIngredient { get; set; }

        public bool SatisfiesAsIngredient(ItemStack stack) => BackingIngredient.SatisfiesAsIngredient(stack);
    }

    /// <summary>
    /// A standardized wrapper for any type of recipe in the game.
    /// </summary>
    public class StandardizedRecipe
    {
        public string RecipeName { get; set; }
        public List<StandardizedIngredient> Ingredients { get; set; } = new List<StandardizedIngredient>();
        public List<ItemStack> Outputs { get; set; } = new List<ItemStack>();
    }

    #endregion

    public class ShowCraftableSystem : ModSystem
    {
        // ======================================================================================================
        // MOD SYSTEM & HARMONY SETUP
        // ======================================================================================================

        private Harmony _harmony;
        private ICoreClientAPI _capi;

        public const string HarmonyId = "showcraftable.core";
        public const string CraftableCategoryCode = "craftable";

        // --- Caches ---
        private static readonly object GlobalLock = new();
        private static List<StandardizedRecipe> _masterRecipeList = new();
        private static List<string> _cachedCraftablePageCodes = new();
        private static bool _scanInProgress = false;
        private static bool _masterListInitialized = false;

        public override void StartClientSide(ICoreClientAPI capi)
        {
            _capi = capi;
            _harmony = new Harmony(HarmonyId);

            // --- Harmony Patches ---
            var tHandbook = AccessTools.TypeByName("Vintagestory.GameContent.GuiDialogHandbook");
            _harmony.Patch(AccessTools.Method(tHandbook, "FilterItems"), prefix: new HarmonyMethod(typeof(ShowCraftableSystem), nameof(FilterItems_Prefix)));

            var tSurvivalHandbook = AccessTools.TypeByName("Vintagestory.GameContent.GuiDialogSurvivalHandbook");
            _harmony.Patch(AccessTools.Method(tSurvivalHandbook, "genTabs"), postfix: new HarmonyMethod(typeof(ShowCraftableSystem), nameof(GenTabs_Postfix)));

            // --- Event Listeners & Commands ---
            capi.Event.LevelFinalize += OnLevelFinalize;
            capi.Event.PlayerJoin += (p) => OnLevelFinalize(); // Re-init if player changes

            capi.ChatCommands.Create("craftablescan")
                .WithDescription("Scans your inventory to see what you can craft right now.")
                .HandleWith(OnCraftableScanCommand);
        }

        public override void Dispose() => _harmony?.UnpatchAll(HarmonyId);

        /// <summary>
        /// Resets caches when the world is finalized or a new player joins.
        /// </summary>
        private void OnLevelFinalize()
        {
            lock (GlobalLock)
            {
                _masterRecipeList.Clear();
                _cachedCraftablePageCodes.Clear();
                _masterListInitialized = false;
            }
        }


        #region Recipe Caching & Collection
        // ======================================================================================================
        // RECIPE CACHING & COLLECTION
        // Gathers all recipes from the game on the first scan and converts them to our standard format.
        // ======================================================================================================

        private void InitializeMasterRecipeList()
        {
            _capi.Logger.Notification("[Craftable] Building master recipe list for the first time...");
            var masterList = new List<StandardizedRecipe>();

            // --- 1. Grid Recipes ---
            foreach (var recipe in _capi.World.GridRecipes)
            {
                recipe.ResolveIngredients(_capi.World);
                if (recipe.Output?.ResolvedItemstack == null) continue;

                masterList.Add(new StandardizedRecipe
                {
                    RecipeName = recipe.Name.ToString(),
                    // FIX: Handle cases where an ingredient's ResolvedItemstack is null (e.g., wildcards)
                    // by falling back to the ingredient's base Quantity property.
                    Ingredients = recipe.resolvedIngredients
                        .Where(ing => ing != null)
                        .Select(ing => new StandardizedIngredient
                        {
                            BackingIngredient = ing,
                            Quantity = ing.ResolvedItemstack?.StackSize ?? ing.Quantity,
                            IsTool = ing.IsTool
                        }).ToList(),
                    Outputs = new List<ItemStack> { recipe.Output.ResolvedItemstack }
                });
            }

            // --- 2. Smithing Recipes ---
            foreach (var recipe in _capi.GetSmithingRecipes())
            {
                if (recipe.Output?.ResolvedItemstack == null) continue;
                masterList.Add(new StandardizedRecipe
                {
                    RecipeName = recipe.Name.ToString(),
                    Ingredients = new List<StandardizedIngredient> { new StandardizedIngredient { BackingIngredient = recipe.Ingredient, Quantity = recipe.Ingredient.Quantity } },
                    Outputs = new List<ItemStack> { recipe.Output.ResolvedItemstack }
                });
            }

            // --- 3. Knapping Recipes ---
            foreach (var recipe in _capi.GetKnappingRecipes())
            {
                if (recipe.Output?.ResolvedItemstack == null) continue;
                masterList.Add(new StandardizedRecipe
                {
                    RecipeName = recipe.Name.ToString(),
                    Ingredients = new List<StandardizedIngredient> { new StandardizedIngredient { BackingIngredient = recipe.Ingredient, Quantity = 1 } },
                    Outputs = new List<ItemStack> { recipe.Output.ResolvedItemstack }
                });
            }

            // --- 4. Clay Forming Recipes ---
            foreach (var recipe in _capi.GetClayformingRecipes())
            {
                if (recipe.Output?.ResolvedItemstack == null) continue;
                masterList.Add(new StandardizedRecipe
                {
                    RecipeName = recipe.Name.ToString(),
                    Ingredients = new List<StandardizedIngredient> { new StandardizedIngredient { BackingIngredient = recipe.Ingredient, Quantity = recipe.Ingredient.Quantity } },
                    Outputs = new List<ItemStack> { recipe.Output.ResolvedItemstack }
                });
            }

            // --- 5. Barrel Recipes ---
            foreach (var recipe in _capi.GetBarrelRecipes())
            {
                // FIX: Add a null check for the Ingredients array, as some barrel recipes may not have any.
                if (recipe.Output?.ResolvedItemstack == null || recipe.Ingredients == null) continue;
                masterList.Add(new StandardizedRecipe
                {
                    RecipeName = recipe.Name.ToString(),
                    Ingredients = recipe.Ingredients
                        .Where(ing => ing != null) // Also protect against null entries within the array
                        .Select(ing => new StandardizedIngredient { BackingIngredient = ing, Quantity = ing.Quantity }).ToList(),
                    Outputs = new List<ItemStack> { recipe.Output.ResolvedItemstack }
                });
            }

            _capi.Logger.Notification($"[Craftable] Master recipe list initialized with {masterList.Count} unique recipes.");

            lock (GlobalLock)
            {
                _masterRecipeList = masterList;
                _masterListInitialized = true;
            }
        }

        #endregion


        #region Craftable Scan Logic
        // ======================================================================================================
        // CRAFTABLE SCAN LOGIC
        // This is the main logic that runs when the user types /craftablescan.
        // ======================================================================================================

        private TextCommandResult OnCraftableScanCommand(TextCommandCallingArgs args)
        {
            if (_scanInProgress)
            {
                _capi.ShowChatMessage("A scan is already in progress.");
                return TextCommandResult.Success();
            }

            _scanInProgress = true;
            var t0 = DateTime.UtcNow;

            try
            {
                lock (GlobalLock)
                {
                    if (!_masterListInitialized)
                    {
                        InitializeMasterRecipeList();
                    }
                }

                var resourcePool = BuildPlayerResourcePool();

                var craftableOutputCodes = new HashSet<string>();
                foreach (var recipe in _masterRecipeList)
                {
                    if (IsRecipeCraftable(recipe, resourcePool))
                    {
                        foreach (var output in recipe.Outputs)
                        {
                            craftableOutputCodes.Add(output.Collectible.Code.ToString());
                        }
                    }
                }

                var pageCodes = MapItemCodesToHandbookPages(craftableOutputCodes);

                lock (GlobalLock)
                {
                    _cachedCraftablePageCodes = pageCodes.ToList();
                }

                var ms = (int)(DateTime.UtcNow - t0).TotalMilliseconds;
                _capi.ShowChatMessage($"[Craftable] Scan complete: Found {_cachedCraftablePageCodes.Count} handbook pages from {craftableOutputCodes.Count} craftable items ({ms} ms).");

                TryRefreshOpenDialog();
            }
            catch (Exception e)
            {
                _capi.Logger.Error($"[Craftable] Scan failed: {e}");
                _capi.ShowChatMessage("Scan failed. See client-main.txt log for details.");
            }
            finally
            {
                _scanInProgress = false;
            }

            return TextCommandResult.Success();
        }

        private Dictionary<string, int> BuildPlayerResourcePool()
        {
            var pool = new Dictionary<string, int>();
            var player = _capi.World.Player;

            void AddInv(IInventory inv)
            {
                if (inv == null) return;
                foreach (var slot in inv)
                {
                    if (!slot.Empty)
                    {
                        string code = slot.Itemstack.Collectible.Code.ToString();
                        pool.TryGetValue(code, out int current);
                        pool[code] = current + slot.Itemstack.StackSize;
                    }
                }
            }

            AddInv(player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName));
            AddInv(player.InventoryManager.GetHotbarInventory());

            return pool;
        }

        /// <summary>
        /// Helper to get a CollectibleObject from an AssetLocation, trying both Item and Block.
        /// </summary>
        private CollectibleObject GetCollectibleFromCode(AssetLocation code)
        {
            return (CollectibleObject)_capi.World.GetItem(code) ?? _capi.World.GetBlock(code);
        }

        private bool IsRecipeCraftable(StandardizedRecipe recipe, Dictionary<string, int> resourcePool)
        {
            var tempPool = new Dictionary<string, int>(resourcePool);

            foreach (var ingredient in recipe.Ingredients)
            {
                bool foundMatch = false;
                string matchedCode = null;

                foreach (var item in tempPool)
                {
                    var collectible = GetCollectibleFromCode(new AssetLocation(item.Key));
                    if (collectible == null) continue;

                    var testStack = new ItemStack(collectible);
                    if (ingredient.SatisfiesAsIngredient(testStack))
                    {
                        if (item.Value >= ingredient.Quantity)
                        {
                            foundMatch = true;
                            matchedCode = item.Key;
                            break;
                        }
                    }
                }

                if (foundMatch)
                {
                    if (!ingredient.IsTool)
                    {
                        tempPool[matchedCode] -= ingredient.Quantity;
                    }
                }
                else
                {
                    return false; // Missing an ingredient
                }
            }

            return true; // All ingredients satisfied
        }

        private HashSet<string> MapItemCodesToHandbookPages(HashSet<string> craftableItemCodes)
        {
            var pageCodes = new HashSet<string>();
            var handbookSystem = _capi.ModLoader.GetModSystem<ModSystemSurvivalHandbook>();

            var handbook = AccessTools.Field(typeof(ModSystemSurvivalHandbook), "dialog").GetValue(handbookSystem) as GuiDialogHandbook;
            if (handbook == null) return pageCodes;

            var allPages = (List<GuiHandbookPage>)AccessTools.Field(handbook.GetType(), "allHandbookPages").GetValue(handbook);

            foreach (var page in allPages)
            {
                if (page is GuiHandbookItemStackPage itemPage)
                {
                    var stacks = (IEnumerable<ItemStack>)AccessTools.Property(typeof(GuiHandbookItemStackPage), "Stacks").GetValue(itemPage);
                    if (stacks == null) continue;

                    foreach (var stack in stacks)
                    {
                        if (craftableItemCodes.Contains(stack.Collectible.Code.ToString()))
                        {
                            pageCodes.Add(page.PageCode);
                            break;
                        }
                    }
                }
            }

            return pageCodes;
        }

        #endregion

        #region GUI Interaction & Harmony Patches
        // ======================================================================================================
        // GUI INTERACTION & HARMONY PATCHES
        // Code to inject the tab, handle filtering, and open the handbook.
        // ======================================================================================================

        private void TryRefreshOpenDialog()
        {
            var handbookSystem = _capi.ModLoader.GetModSystem<ModSystemSurvivalHandbook>();
            var handbook = AccessTools.Field(typeof(ModSystemSurvivalHandbook), "dialog").GetValue(handbookSystem) as GuiDialogHandbook;

            if (handbook != null && handbook.IsOpened())
            {
                var currentCategory = (string)AccessTools.Field(handbook.GetType(), "currentCatgoryCode").GetValue(handbook);
                if (currentCategory == CraftableCategoryCode)
                {
                    AccessTools.Method(handbook.GetType(), "FilterItems").Invoke(handbook, null);
                }
            }
        }

        public static bool FilterItems_Prefix(object __instance)
        {
            var dialog = __instance as GuiDialogHandbook;
            if (dialog == null) return true;

            string cat = (string)AccessTools.Field(dialog.GetType(), "currentCatgoryCode").GetValue(dialog);
            if (!string.Equals(cat, CraftableCategoryCode, StringComparison.Ordinal)) return true;

            try
            {
                var shown = (System.Collections.IList)AccessTools.Field(dialog.GetType(), "shownHandbookPages").GetValue(dialog);
                var composer = (GuiComposer)AccessTools.Field(dialog.GetType(), "overviewGui").GetValue(dialog);
                string q = (string)AccessTools.Field(dialog.GetType(), "currentSearchText").GetValue(dialog);

                shown?.Clear();
                if (composer == null || shown == null) return true;

                var allPages = (List<GuiHandbookPage>)AccessTools.Field(dialog.GetType(), "allHandbookPages").GetValue(dialog);
                var pageMap = (Dictionary<string, int>)AccessTools.Field(dialog.GetType(), "pageNumberByPageCode").GetValue(dialog);

                List<GuiHandbookPage> pagesToShow = new List<GuiHandbookPage>();
                lock (GlobalLock)
                {
                    foreach (string pageCode in _cachedCraftablePageCodes)
                    {
                        if (pageMap.TryGetValue(pageCode, out int pageIndex) && pageIndex < allPages.Count)
                        {
                            pagesToShow.Add(allPages[pageIndex]);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(q))
                {
                    var weighted = pagesToShow
                        .Select(p => (Page: p, Weight: p.GetTextMatchWeight(q.ToLowerInvariant())))
                        .Where(p => p.Weight > 0)
                        .OrderByDescending(p => p.Weight);

                    foreach (var item in weighted) shown.Add(item.Page);
                }
                else
                {
                    pagesToShow.Sort((p1, p2) => {
                        var p1Text = (string)AccessTools.Property(typeof(GuiHandbookPage), "LinkText").GetValue(p1);
                        var p2Text = (string)AccessTools.Property(typeof(GuiHandbookPage), "LinkText").GetValue(p2);
                        return string.Compare(p1Text, p2Text, StringComparison.CurrentCulture);
                    });
                    foreach (var p in pagesToShow) shown.Add(p);
                }

                var stacklist = composer.GetFlatList("stacklist");
                stacklist.CalcTotalHeight();
                double listHeight = (double)AccessTools.Field(dialog.GetType(), "listHeight").GetValue(dialog);
                composer.GetScrollbar("scrollbar").SetHeights((float)listHeight, (float)stacklist.insideBounds.fixedHeight);
            }
            catch (Exception ex)
            {
                var capi = AccessTools.Field(typeof(GuiDialog), "capi").GetValue(dialog) as ICoreClientAPI;
                capi?.Logger.Error($"[Craftable] Error in FilterItems_Prefix: {ex}");
                return true; // Fallback to original on error
            }

            return false; // Prevent original
        }

        public static void GenTabs_Postfix(object __instance, ref Array __result)
        {
            var tabs = __result.Cast<GuiTab>().ToList();
            if (tabs.Any(t => (t is HandbookTab tab) && tab.CategoryCode == CraftableCategoryCode)) return;

            tabs.Insert(1, new HandbookTab
            {
                Name = "Craftable",
                CategoryCode = CraftableCategoryCode,
            });

            __result = tabs.ToArray();
        }

        #endregion
    }
}

