using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ItemScrapWeaponKit : Item
{
	private float curX;

	private float curY;

	private float prevSecUsed;

	private LCGRandom rnd;

	private ItemStack[] craftResultStacks;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		rnd = new LCGRandom(api.World.Seed);
		JsonItemStack[] array = Attributes["craftingResults"].AsObject<JsonItemStack[]>();
		List<ItemStack> list = new List<ItemStack>();
		foreach (JsonItemStack jsonItemStack in array)
		{
			jsonItemStack.Resolve(api.World, "Scrap weapon kit craft result");
			if (jsonItemStack.ResolvedItemstack != null)
			{
				list.Add(jsonItemStack.ResolvedItemstack);
			}
		}
		craftResultStacks = list.ToArray();
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (slot.Itemstack.TempAttributes.GetBool("consumed"))
		{
			return;
		}
		handling = EnumHandHandling.PreventDefault;
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		if (byPlayer == null)
		{
			return;
		}
		byEntity.World.RegisterCallback(delegate
		{
			if (byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
			{
				byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/player/messycraft"), byPlayer, byPlayer);
			}
		}, 250);
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (byEntity.World is IClientWorldAccessor)
		{
			ModelTransform modelTransform = new ModelTransform();
			modelTransform.EnsureDefaultValues();
			float num = 0f;
			float y = 0f;
			if (secondsUsed > 0.3f)
			{
				int xPos = (int)(secondsUsed * 10f);
				rnd.InitPositionSeed(xPos, 0);
				float num2 = 3f * (rnd.NextFloat() - 0.5f);
				float num3 = 1.5f * (rnd.NextFloat() - 0.5f);
				float num4 = secondsUsed - prevSecUsed;
				num = (curX - num2) * num4 * 2f;
				y = (curY - num3) * num4 * 2f;
			}
			modelTransform.Translation.Set(num - Math.Min(1.5f, secondsUsed * 4f), y, 0f);
			curX = num;
			curY = y;
			prevSecUsed = secondsUsed;
		}
		if (api.World.Side == EnumAppSide.Server)
		{
			return true;
		}
		return secondsUsed < 4.6f;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		return false;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (secondsUsed > 4.5f)
		{
			if (api.Side == EnumAppSide.Server)
			{
				ItemStack itemStack = craftResultStacks[api.World.Rand.Next(craftResultStacks.Length)];
				slot.Itemstack = itemStack.Clone();
				slot.MarkDirty();
			}
			else
			{
				slot.Itemstack.TempAttributes.SetBool("consumed", value: true);
			}
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-craftscrapweapon",
				MouseButton = EnumMouseButton.Right
			}
		};
	}
}
