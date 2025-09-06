using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using ShowCraftable;

namespace ImprovedHandbookRecipes;
public class FillGridButton : ButtonRTC
{

    private readonly bool max;
    private readonly GridRecipe[] recipes;

    public FillGridButton(ICoreClientAPI api,
                          bool max,
                          GridRecipeAndUnnamedIngredients[] recipes)
        : base(api, max ? 1 : 0, max ? "*" : "+", max ? "addmax" : "addone", -1.0, max ? -7.5 : -10.5)
    {
        this.max = max;
        this.recipes = recipes
            .Select(x => x.Recipe)
            .ToArray();

        Float = EnumFloat.Inline;
        VerticalAlign = EnumVerticalAlign.FixedOffset;
    }

    protected override void OnClick()
    {
        _ = HandleClickAsync();
    }

    private async Task HandleClickAsync()
    {
        try
        {
            if (await TryFillGrid())
            {
                api.Gui.PlaySound("menubutton_press");
            }
            else
            {
                api.Gui.PlaySound("menubutton_wood");
            }
        }
        catch (Exception e)
        {
            api.Logger.Error("FillGridButton error: {0}", e);
        }
    }

    private async Task<bool> TryFillGrid()
    {
        var player = api.World.Player;
        IPlayerInventoryManager manager = player.InventoryManager;
        var crafting = manager.GetOwnInventory("craftinggrid");
        var backPack = manager.GetOwnInventory("backpack");
        var hotbar = manager.GetHotbarInventory();
        var input = crafting.Take(9).ToArray();

        bool shift = api.Input.ShiftHeld();
        var chests = shift
            ? Enumerable.Empty<ItemSlot>()
            : manager.OpenedInventories
                .OfType<InventoryGeneric>()
                .SelectMany(x => x);

        var stacks = backPack
            .Concat(hotbar)
            .Where(x => x is not ItemSlotBackpack)
            .Concat(chests)
            .ToList();

        var available = stacks.Concat(input)
            .NonEmpty()
            .Select(x => (x.Itemstack.Collectible.Code, x.StackSize))
            .GroupBy(x => x.Code)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.StackSize));

        var ordered = recipes
            .Select(r => new { Recipe = r, Score = r.Matches(player, input, 3) ? 2 : CanMake(r) ? 1 : 0 })
            .OrderByDescending(x => x.Score)
            .Select(x => x.Recipe);

        foreach (var recipe in ordered)
        {
            var inSlots = crafting.Take(9).ToArray();
            var avail = stacks.ToList();
            bool applied = false;
            bool last;
            do
            {
                last = await AddIngredients(inSlots, recipe, avail, shift);
                applied |= last;
            } while (max && last);
            if (applied) return true;
        }

        return false;

        bool CanMake(GridRecipe recipe)
        {
            var ingredients = recipe.resolvedIngredients
                .Where(x => x != null)
                .ToArray();

            var grouped = ingredients
                .Where(x => !x.IsWildCard && !x.IsTool)
                .GroupBy(x => x.Code);
            foreach (var g in grouped)
            {
                if (available.GetValueOrDefault(g.Key) < g.Sum(z => z.Quantity))
                {
                    return false;
                }
            }

            if (!ingredients.Any(x => x.IsWildCard || x.IsTool))
            {
                return true;
            }

            Dictionary<ItemSlot, int> used = new();
            var ordered = ingredients
                .Where(x => !x.IsWildCard)
                .Concat(ingredients.Where(x => x.IsWildCard));
            foreach (var ingredient in ordered)
            {
                int need = ingredient.Quantity;
                foreach (var slot in input.Concat(stacks))
                {
                    if (!Satisfies(ingredient, slot.Itemstack)) continue;
                    if (!used.TryGetValue(slot, out int size)) size = 0;
                    int take = Math.Min(need, slot.StackSize - size);
                    if (take <= 0) continue;
                    used[slot] = size + take;
                    need -= take;
                    if (need == 0) break;
                }
                if (need > 0) return false;
            }
            return true;
        }
    }


    private async Task<bool> AddIngredients(ItemSlot[] input, GridRecipe recipe, List<ItemSlot> available, bool shift)
    {
        List<(ItemSlot from, ItemSlot to, int n)> ops = new();
        Dictionary<ItemSlot, int> remaining = new();
        var ingredients = recipe.resolvedIngredients;

        var needs = new List<NeedSlotInfo>();
        List<ItemSlot> empties = new();

        ItemSlot[] slots = input;
        ItemStack[] stacks = input.Select(s => s.Itemstack).ToArray();

        if (recipe.Shapeless)
        {
            bool[] used = new bool[stacks.Length];
            ItemSlot[] newSlots = new ItemSlot[ingredients.Length];
            ItemStack[] newStacks = new ItemStack[ingredients.Length];
            for (int i = 0; i < ingredients.Length; i++)
            {
                var ingr = ingredients[i];
                for (int j = 0; j < stacks.Length; j++)
                {
                    if (used[j]) continue;
                    if (ingr?.SatisfiesAsIngredient(stacks[j], false) ?? false)
                    {
                        newSlots[i] = slots[j];
                        newStacks[i] = stacks[j];
                        used[j] = true;
                        break;
                    }
                }
            }
            for (int j = 0; j < stacks.Length; j++)
            {
                if (!used[j] && stacks[j] != null)
                {
                    empties.Add(slots[j]);
                    stacks[j] = null;
                }
            }
            for (int i = 0; i < newSlots.Length; i++)
            {
                if (newSlots[i] == null)
                {
                    int idx = -1;
                    for (int k = 0; k < slots.Length; k++)
                    {
                        if (!used[k] && stacks[k] == null)
                        {
                            idx = k;
                            break;
                        }
                    }
                    if (idx >= 0)
                    {
                        newSlots[i] = slots[idx];
                        used[idx] = true;
                    }
                }
            }
            slots = newSlots;
            stacks = newStacks;
        }
        else if (recipe.Width * recipe.Height < 9)
        {
            Bounds bounds = new(recipe.Width, recipe.Height, 3);
            for (int i = 8; i >= 0; i--)
            {
                if (stacks[i] != null)
                {
                    for (int j = 0; j < ingredients.Length; j++)
                    {
                        if (Satisfies(ingredients[j], stacks[i])) { bounds.Align(i, j); break; }
                    }
                }
            }
            for (int i = 0; i < slots.Length; i++)
            {
                bool inside = bounds.Contains(i);
                if (inside)
                {
                    int inner = bounds.ToInner(i);
                    if (!Satisfies(ingredients[inner], stacks[i]) && stacks[i] != null)
                    {
                        empties.Add(slots[i]);
                        stacks[i] = null;
                    }
                }
                else if (stacks[i] != null)
                {
                    empties.Add(slots[i]);
                    stacks[i] = null;
                }
            }
            var fSlots = new List<ItemSlot>();
            var fStacks = new List<ItemStack>();
            for (int i = 0; i < slots.Length; i++)
            {
                if (bounds.Contains(i)) { fSlots.Add(slots[i]); fStacks.Add(stacks[i]); }
            }
            slots = fSlots.ToArray();
            stacks = fStacks.ToArray();
        }
        else
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (!Satisfies(ingredients[i], stacks[i]) && stacks[i] != null)
                {
                    empties.Add(slots[i]);
                    stacks[i] = null;
                }
            }
        }

        available.RemoveAll(slot => slot.Empty);

        var counts = ingredients
            .Select((x, i) => i < stacks.Length ? CurrentSets(x, stacks[i]) : -1)
            .Where(x => x >= 0);
        int complete = counts.Any() ? counts.Min() : 0;

        for (int i = 0, len = slots.Length; i < len; i++)
        {
            var ingredient = ingredients[i];
            if (ingredient == null) continue;
            var stack = stacks[i];
            int n = ingredient.Quantity;
            if (stack != null)
            {
                int size = stack.StackSize;
                if (ingredient.IsTool) continue;
                if (size > complete * n)
                {
                    n = (complete + 1) * n - size;
                    if (n <= 0) continue;
                }
                int capacity = stack.Collectible.MaxStackSize - size;
                if (capacity <= 0) continue;
                if (n > capacity) n = capacity;
            }
            foreach (var slot in available)
            {
                if (!Satisfies(ingredient, slot?.Itemstack) || !slots[i].CanTakeFrom(slot)) continue;
                if (!remaining.TryGetValue(slot, out int size)) size = remaining[slot] = slot.Itemstack.StackSize;
                int take = Math.Min(size, n);
                ops.Add((slot, slots[i], take));
                remaining[slot] = size - take;
                n -= take;
                if (n <= 0) break;
            }
            if (n > 0)
            {
                if (shift) return false;
                var need = ShowCraftableSystem.MakeNeedForSlot(ingredient, slots[i], n);
                if (need != null) needs.Add(need);
            }
        }

        if (needs.Count > 0)
        {
            if (!await ShowCraftableSystem.CanFetchToGrid(api, needs, ShowCraftableSystem.DefaultNearbyRadius)) return false;
        }

        var player = api.World.Player;
        var manager = player.InventoryManager;
        bool change = false;

        foreach (var slot in empties)
        {
            Empty(slot);
        }
        foreach (var (from, to, n) in ops)
        {
            ItemStackMoveOperation op = new(api.World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge, n);
            op.ActingPlayer = player;
            int before = to.StackSize;
            var packet = manager.TryTransferTo(from, to, ref op);
            if (to.StackSize > before)
            {
                SendPacket(packet);
                change = true;
            }
        }
        if (needs.Count > 0)
        {
            ShowCraftableSystem.RequestFetchToGrid(api, needs, ShowCraftableSystem.DefaultNearbyRadius);
        }
        return change;

        int CurrentSets(GridRecipeIngredient ingr, ItemStack stack)
        {
            if (ingr == null) return -1;
            if (ingr.IsTool) return (stack?.StackSize > 0) ? -1 : 0;
            return stack?.StackSize / ingr.Quantity ?? 0;
        }
        void Empty(ItemSlot slot)
        {
            var player = api.World.Player;
            ItemStackMoveOperation op = new(api.World, EnumMouseButton.Left, EnumModifierKey.CTRL, EnumMergePriority.AutoMerge, slot.StackSize);
            op.ActingPlayer = player;
            player.InventoryManager.TryTransferAway(slot, ref op, false)?.Foreach(SendPacket);
            if (!slot.Empty) player.InventoryManager.DropItem(slot, true);
        }
        void SendPacket(object obj)
        {
            if (obj is Packet_Client packet)
            {
                api.Network.SendPacketClient(packet);
            }
        }
    }


    private static bool Satisfies(GridRecipeIngredient ingredient, ItemStack invStack)
        => invStack?.StackSize > 0
        && ingredient != null
        && ingredient.SatisfiesAsIngredient(invStack, false)
        && (!ingredient.IsTool || invStack.Collectible.GetRemainingDurability(invStack) >= ingredient.ToolDurabilityCost);

    protected override bool Visible => api.Gui.OpenedGuis.OfType<GuiDialogInventory>().Any();


    private struct Bounds
    {
        private int xPos;
        private int yPos;
        private readonly int xLen;
        private readonly int yLen;
        private readonly int outerLen;

        public Bounds(int xLen, int yLen, int outerLen)
        {
            this.xLen = xLen;
            this.yLen = yLen;
            this.outerLen = outerLen;
        }

        public void Align(int outer, int inner)
        {
            int x = outer % outerLen - inner % xLen;
            int y = outer / outerLen - inner / xLen;
            if (x >= 0 && y >= 0 && x + xLen < outerLen && y + yLen < outerLen)
            {
                xPos = x; yPos = y;
            }
        }

        public readonly int ToInner(int outer) => outer % outerLen - xPos + (outer / outerLen - yPos) * xLen;

        public readonly bool Contains(int i)
        {
            int x = i % outerLen, y = i / outerLen;
            return x >= xPos && x < xPos + xLen && y >= yPos && y < yPos + yLen;
        }
    }
}
