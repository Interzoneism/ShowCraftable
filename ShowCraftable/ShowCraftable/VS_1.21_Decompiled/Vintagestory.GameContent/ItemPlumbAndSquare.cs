using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemPlumbAndSquare : Item
{
	private WorldInteraction[] interactions;

	private List<LoadedTexture> symbols;

	public override void OnLoaded(ICoreAPI api)
	{
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "plumbAndSquareInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject collectible in api.World.Collectibles)
			{
				JsonObject attributes = collectible.Attributes;
				if (attributes != null && attributes["reinforcementStrength"].AsInt() > 0)
				{
					list.Add(new ItemStack(collectible));
				}
			}
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-reinforceblock",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				},
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-removereinforcement",
					MouseButton = EnumMouseButton.Left,
					Itemstacks = list.ToArray()
				}
			};
		});
		symbols = new List<LoadedTexture>();
		symbols.Add(GenTexture(1, 1));
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		if (!(api is ICoreClientAPI) || symbols == null)
		{
			return;
		}
		foreach (LoadedTexture symbol in symbols)
		{
			symbol.Dispose();
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		if (handling == EnumHandHandling.PreventDefault)
		{
			return;
		}
		if (byEntity.World.Side == EnumAppSide.Client)
		{
			handling = EnumHandHandling.PreventDefaultAction;
		}
		else
		{
			if (blockSel == null || !((byEntity as EntityPlayer).Player is IServerPlayer serverPlayer) || !byEntity.World.Claims.TryAccess(serverPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
			{
				return;
			}
			ModSystemBlockReinforcement modSystem = byEntity.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
			ItemSlot itemSlot = modSystem.FindResourceForReinforcing(serverPlayer);
			if (itemSlot == null)
			{
				return;
			}
			int strength = itemSlot.Itemstack.ItemAttributes["reinforcementStrength"].AsInt();
			int num = slot.Itemstack.Attributes.GetInt("toolMode");
			int num2 = 0;
			PlayerGroupMembership[] groups = serverPlayer.GetGroups();
			if (num > 0 && num - 1 < groups.Length)
			{
				num2 = groups[num - 1].GroupUid;
			}
			if (!api.World.BlockAccessor.GetBlock(blockSel.Position).HasBehavior<BlockBehaviorReinforcable>())
			{
				serverPlayer.SendIngameError("notreinforcable", "This block can not be reinforced!");
				return;
			}
			if (!((num2 > 0) ? modSystem.StrengthenBlock(blockSel.Position, serverPlayer, strength, num2) : modSystem.StrengthenBlock(blockSel.Position, serverPlayer, strength)))
			{
				serverPlayer.SendIngameError("alreadyreinforced", "Cannot reinforce block, it's already reinforced!");
				return;
			}
			itemSlot.TakeOut(1);
			itemSlot.MarkDirty();
			BlockPos position = blockSel.Position;
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/tool/reinforce"), position, 0.0);
			handling = EnumHandHandling.PreventDefaultAction;
			if (byEntity.World.Side == EnumAppSide.Client)
			{
				((byEntity as EntityPlayer)?.Player as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			}
		}
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (byEntity.World.Side == EnumAppSide.Client)
		{
			handling = EnumHandHandling.PreventDefaultAction;
		}
		else
		{
			if (blockSel == null)
			{
				return;
			}
			ModSystemBlockReinforcement modSystem = byEntity.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
			if (!((byEntity as EntityPlayer).Player is IServerPlayer serverPlayer))
			{
				return;
			}
			BlockReinforcement reinforcment = modSystem.GetReinforcment(blockSel.Position);
			string errorCode = "";
			if (!modSystem.TryRemoveReinforcement(blockSel.Position, serverPlayer, ref errorCode))
			{
				if (errorCode == "notownblock")
				{
					serverPlayer.SendIngameError("cantremove", "Cannot remove reinforcement. This block does not belong to you");
				}
				else
				{
					serverPlayer.SendIngameError("cantremove", "Cannot remove reinforcement. It's not reinforced");
				}
				return;
			}
			if (reinforcment.Locked)
			{
				ItemStack itemstack = new ItemStack(byEntity.World.GetItem(new AssetLocation(reinforcment.LockedByItemCode)));
				if (!serverPlayer.InventoryManager.TryGiveItemstack(itemstack, slotNotifyEffect: true))
				{
					byEntity.World.SpawnItemEntity(itemstack, byEntity.ServerPos.XYZ);
				}
			}
			BlockPos position = blockSel.Position;
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/tool/reinforce"), position, 0.0);
			handling = EnumHandHandling.PreventDefaultAction;
		}
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
	{
		slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
	{
		return Math.Min(1 + byPlayer.GetGroups().Length - 1, slot.Itemstack.Attributes.GetInt("toolMode"));
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		PlayerGroupMembership[] groups = forPlayer.GetGroups();
		SkillItem[] array = new SkillItem[1 + groups.Length];
		ICoreClientAPI capi = api as ICoreClientAPI;
		int num = 1;
		LoadedTexture texture = FetchOrCreateTexture(num);
		array[0] = new SkillItem
		{
			Code = new AssetLocation("self"),
			Name = Lang.Get("Reinforce for yourself")
		}.WithIcon(capi, texture);
		for (int i = 0; i < groups.Length; i++)
		{
			texture = FetchOrCreateTexture(++num);
			array[i + 1] = new SkillItem
			{
				Code = new AssetLocation("group"),
				Name = Lang.Get("Reinforce for group " + groups[i].GroupName)
			}.WithIcon(capi, texture);
		}
		return array;
	}

	private LoadedTexture FetchOrCreateTexture(int seed)
	{
		if (symbols.Count >= seed)
		{
			return symbols[seed - 1];
		}
		LoadedTexture loadedTexture = GenTexture(seed, seed);
		symbols.Add(loadedTexture);
		return loadedTexture;
	}

	private LoadedTexture GenTexture(int seed, int addLines)
	{
		ICoreClientAPI capi = api as ICoreClientAPI;
		return capi.Gui.Icons.GenTexture(48, 48, delegate(Context ctx, ImageSurface surface)
		{
			capi.Gui.Icons.DrawRandomSymbol(ctx, 0.0, 0.0, 48.0, GuiStyle.MacroIconColor, 2.0, seed, addLines);
		});
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
