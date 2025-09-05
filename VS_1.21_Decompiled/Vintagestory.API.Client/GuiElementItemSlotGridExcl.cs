using System;
using System.Linq;
using Cairo;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public class GuiElementItemSlotGridExcl : GuiElementItemSlotGridBase
{
	private int[] excludingSlots;

	public GuiElementItemSlotGridExcl(ICoreClientAPI capi, IInventory inventory, Action<object> sendPacketHandler, int columns, int[] excludingSlots, ElementBounds bounds)
		: base(capi, inventory, sendPacketHandler, columns, bounds)
	{
		this.excludingSlots = excludingSlots;
		InitDicts();
		SendPacketHandler = sendPacketHandler;
	}

	internal void InitDicts()
	{
		availableSlots.Clear();
		renderedSlots.Clear();
		if (excludingSlots != null)
		{
			for (int i = 0; i < inventory.Count; i++)
			{
				if (!excludingSlots.Contains(i))
				{
					ItemSlot value = inventory[i];
					availableSlots.Add(i, value);
					renderedSlots.Add(i, value);
				}
			}
		}
		else
		{
			for (int j = 0; j < inventory.Count; j++)
			{
				availableSlots.Add(j, inventory[j]);
				renderedSlots.Add(j, inventory[j]);
			}
		}
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		InitDicts();
		base.ComposeElements(ctx, surface);
	}

	public override void PostRenderInteractiveElements(float deltaTime)
	{
		if (inventory.DirtySlots.Count > 0)
		{
			InitDicts();
		}
		base.PostRenderInteractiveElements(deltaTime);
	}
}
