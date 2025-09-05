using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemHoneyComb : Item
{
	public float ContainedHoneyLitres = 0.2f;

	private WorldInteraction[] interactions;

	public bool CanSqueezeInto(Block block, BlockSelection blockSel)
	{
		BlockPos blockPos = blockSel?.Position;
		if (block is BlockLiquidContainerTopOpened blockLiquidContainerTopOpened)
		{
			if (!(blockPos == null))
			{
				return !blockLiquidContainerTopOpened.IsFull(blockPos);
			}
			return true;
		}
		if (blockPos != null)
		{
			if (block is BlockBarrel blockBarrel && api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBarrel blockEntityBarrel)
			{
				if (!blockEntityBarrel.Sealed)
				{
					return !blockBarrel.IsFull(blockPos);
				}
				return false;
			}
			if (api.World.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityGroundStorage blockEntityGroundStorage)
			{
				ItemSlot slotAt = blockEntityGroundStorage.GetSlotAt(blockSel);
				if (slotAt?.Itemstack?.Block is BlockLiquidContainerTopOpened blockLiquidContainerTopOpened2)
				{
					return !blockLiquidContainerTopOpened2.IsFull(slotAt.Itemstack);
				}
			}
		}
		return false;
	}

	public override void OnLoaded(ICoreAPI api)
	{
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "honeyCombInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Block block in api.World.Blocks)
			{
				if (!(block.Code == null))
				{
					if (block is BlockBarrel)
					{
						list.Add(new ItemStack(block));
					}
					if (CanSqueezeInto(block, null))
					{
						list.Add(new ItemStack(block));
					}
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-squeeze",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel?.Block != null && CanSqueezeInto(blockSel.Block, blockSel) && byEntity.Controls.ShiftKey)
		{
			handling = EnumHandHandling.PreventDefault;
			if (api.World.Side == EnumAppSide.Client)
			{
				byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/squeezehoneycomb"), byEntity, null, randomizePitch: true, 16f, 0.5f);
			}
		}
		else
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel?.Block != null && CanSqueezeInto(blockSel.Block, blockSel))
		{
			if (!byEntity.Controls.ShiftKey)
			{
				return false;
			}
			if (byEntity.World is IClientWorldAccessor)
			{
				byEntity.StartAnimation("squeezehoneycomb");
			}
			return secondsUsed < 2f;
		}
		return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		byEntity.StopAnimation("squeezehoneycomb");
		if (blockSel != null)
		{
			Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
			if (CanSqueezeInto(block, blockSel))
			{
				if (secondsUsed < 1.9f)
				{
					return;
				}
				IWorldAccessor world = byEntity.World;
				if (!CanSqueezeInto(block, blockSel))
				{
					return;
				}
				ItemStack liquidStack = new ItemStack(world.GetItem(new AssetLocation("honeyportion")), 99999);
				if (block is BlockLiquidContainerTopOpened blockLiquidContainerTopOpened)
				{
					if (blockLiquidContainerTopOpened.TryPutLiquid(blockSel.Position, liquidStack, ContainedHoneyLitres) == 0)
					{
						return;
					}
				}
				else if (block is BlockBarrel blockBarrel && api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBarrel blockEntityBarrel)
				{
					if (blockEntityBarrel.Sealed || blockBarrel.TryPutLiquid(blockSel.Position, liquidStack, ContainedHoneyLitres) == 0)
					{
						return;
					}
				}
				else if (api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage blockEntityGroundStorage)
				{
					ItemSlot slotAt = blockEntityGroundStorage.GetSlotAt(blockSel);
					if (slotAt != null && slotAt?.Itemstack?.Block != null && CanSqueezeInto(slotAt.Itemstack.Block, null))
					{
						BlockLiquidContainerTopOpened blockLiquidContainerTopOpened2 = slotAt.Itemstack.Block as BlockLiquidContainerTopOpened;
						blockLiquidContainerTopOpened2.TryPutLiquid(slotAt.Itemstack, liquidStack, ContainedHoneyLitres);
						blockEntityGroundStorage.MarkDirty(redrawOnClient: true);
					}
				}
				slot.TakeOut(1);
				slot.MarkDirty();
				IPlayer player = null;
				if (byEntity is EntityPlayer)
				{
					player = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
				}
				ItemStack itemstack = new ItemStack(world.GetItem(new AssetLocation("beeswax")));
				if (player != null && !player.InventoryManager.TryGiveItemstack(itemstack))
				{
					byEntity.World.SpawnItemEntity(itemstack, byEntity.SidedPos.XYZ);
				}
				return;
			}
		}
		base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		byEntity.StopAnimation("squeezehoneycomb");
		return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason);
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
