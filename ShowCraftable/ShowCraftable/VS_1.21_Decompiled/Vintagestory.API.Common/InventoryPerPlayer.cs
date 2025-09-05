using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public class InventoryPerPlayer : InventoryGeneric
{
	public Dictionary<string, int[]> PlayerQuantities;

	public int[] Quantities;

	public override bool PutLocked { get; set; } = true;

	public InventoryPerPlayer(int quantitySlots, string? invId, ICoreAPI? api, NewSlotDelegate? onNewSlot = null)
		: base(quantitySlots, invId, api, onNewSlot)
	{
		PlayerQuantities = new Dictionary<string, int[]>();
		Quantities = new int[quantitySlots];
	}

	public bool CanTake(ItemSlot fromSlot, ItemStackMoveOperation op)
	{
		return GetPlayerRemaining(op.ActingPlayer.PlayerUID, GetSlotId(fromSlot)) > 0;
	}

	public void AddPlayerUsage(string playerUid, int slotId, int value)
	{
		if (PlayerQuantities.TryGetValue(playerUid, out int[] value2))
		{
			value2[slotId] += value;
		}
	}

	public int GetPlayerRemaining(string playerUid, int slotId)
	{
		if (PlayerQuantities.TryGetValue(playerUid, out int[] value))
		{
			return Math.Max(0, Quantities[slotId] - value[slotId]);
		}
		value = new int[Quantities.Length];
		PlayerQuantities.Add(playerUid, value);
		return Math.Max(0, Quantities[slotId] - value[slotId]);
	}

	public override void MarkSlotDirty(int slotId)
	{
	}

	public void MarkDirty()
	{
		if (Api is ICoreServerAPI coreServerAPI)
		{
			coreServerAPI.World.BlockAccessor.GetBlockEntity(Pos).MarkDirty();
		}
	}

	protected override ItemSlot NewSlot(int slotId)
	{
		return new ItemSlotPerPlayer(this, slotId);
	}

	public void OnPlacementBySchematic()
	{
		for (int i = 0; i < slots.Length; i++)
		{
			Quantities[i] = this[i].StackSize;
		}
	}

	public override void FromTreeAttributes(ITreeAttribute treeAttribute)
	{
		base.FromTreeAttributes(treeAttribute);
		if (treeAttribute["PlayerQuantities"] is TreeAttribute treeAttribute2)
		{
			if (treeAttribute2.Count > 0)
			{
				foreach (var (key, attribute2) in treeAttribute2)
				{
					PlayerQuantities[key] = (attribute2 as IntArrayAttribute)?.value ?? new int[Count];
				}
			}
			else
			{
				PlayerQuantities.Clear();
			}
		}
		Quantities = (treeAttribute["Quantities"] as IntArrayAttribute)?.value ?? new int[Count];
		if (!(Api is ICoreClientAPI coreClientAPI) || !PlayerQuantities.ContainsKey(coreClientAPI.World.Player.PlayerUID))
		{
			return;
		}
		for (int i = 0; i < Quantities.Length; i++)
		{
			if (this[i].Itemstack == null)
			{
				continue;
			}
			int playerRemaining = GetPlayerRemaining(coreClientAPI.World.Player.PlayerUID, i);
			if (playerRemaining == 0)
			{
				if (coreClientAPI.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
				{
					this[i].Itemstack = null;
				}
			}
			else
			{
				this[i].Itemstack.StackSize = playerRemaining;
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute invtree)
	{
		TreeAttribute treeAttribute = new TreeAttribute();
		foreach (KeyValuePair<string, int[]> playerQuantity in PlayerQuantities)
		{
			playerQuantity.Deconstruct(out var key, out var value);
			string key2 = key;
			int[] value2 = value;
			treeAttribute[key2] = new IntArrayAttribute(value2);
		}
		invtree["PlayerQuantities"] = treeAttribute;
		invtree["Quantities"] = new IntArrayAttribute(Quantities);
		base.ToTreeAttributes(invtree);
	}
}
