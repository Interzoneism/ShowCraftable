using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemPlantableSeed : Item
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "seedInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Block block in api.World.Blocks)
			{
				if (!(block.Code == null) && block.EntityClass != null && api.World.ClassRegistry.GetBlockEntity(block.EntityClass) == typeof(BlockEntityFarmland))
				{
					list.Add(new ItemStack(block));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-plant",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		BlockPos position = blockSel.Position;
		string text = itemslot.Itemstack.Collectible.Variant["type"];
		if (text == "bellpepper")
		{
			return;
		}
		BlockEntity blockEntity = byEntity.World.BlockAccessor.GetBlockEntity(position);
		if (blockEntity is BlockEntityFarmland)
		{
			Block block = byEntity.World.GetBlock(CodeWithPath("crop-" + text + "-1"));
			if (block == null)
			{
				return;
			}
			IPlayer player = null;
			if (byEntity is EntityPlayer)
			{
				player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			bool num = ((BlockEntityFarmland)blockEntity).TryPlant(block, itemslot, byEntity, blockSel);
			if (num)
			{
				byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/plant"), position, 0.4375, player);
				((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				if (player == null || player.WorldData?.CurrentGameMode != EnumGameMode.Creative)
				{
					itemslot.TakeOut(1);
					itemslot.MarkDirty();
				}
			}
			if (num)
			{
				handHandling = EnumHandHandling.PreventDefault;
			}
		}
		else
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		Block block = world.GetBlock(CodeWithPath("crop-" + inSlot.Itemstack.Collectible.Variant["type"] + "-1"));
		if (block != null && block.CropProps != null)
		{
			dsc.AppendLine(Lang.Get("soil-nutrition-requirement") + block.CropProps.RequiredNutrient);
			dsc.AppendLine(Lang.Get("soil-nutrition-consumption") + block.CropProps.NutrientConsumption);
			double num = block.CropProps.TotalGrowthDays;
			num = ((!(num > 0.0)) ? ((double)(block.CropProps.TotalGrowthMonths * (float)world.Calendar.DaysPerMonth)) : (num / 12.0 * (double)world.Calendar.DaysPerMonth));
			num /= api.World.Config.GetDecimal("cropGrowthRateMul", 1.0);
			dsc.AppendLine(Lang.Get("soil-growth-time") + " " + Lang.Get("count-days", Math.Round(num, 1)));
			dsc.AppendLine(Lang.Get("crop-coldresistance", Math.Round(block.CropProps.ColdDamageBelow, 1)));
			dsc.AppendLine(Lang.Get("crop-heatresistance", Math.Round(block.CropProps.HeatDamageAbove, 1)));
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
