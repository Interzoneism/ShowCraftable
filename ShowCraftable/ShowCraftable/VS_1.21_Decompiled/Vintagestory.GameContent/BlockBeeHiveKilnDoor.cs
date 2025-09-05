using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBeeHiveKilnDoor : BlockGeneric
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		BlockPos position = blockSel.Position;
		IBlockAccessor blockAccessor = world.BlockAccessor;
		if (blockAccessor.GetBlock(position, 1).Id == 0 && CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return placeDoor(world, byPlayer, itemstack, blockSel, position, blockAccessor);
		}
		return false;
	}

	public bool placeDoor(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, BlockPos pos, IBlockAccessor ba)
	{
		ba.SetBlock(BlockId, pos);
		BEBehaviorDoor bEBehaviorDoor = ba.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>();
		BlockEntityBeeHiveKiln obj = bEBehaviorDoor.Blockentity as BlockEntityBeeHiveKiln;
		bEBehaviorDoor.RotateYRad = BEBehaviorDoor.getRotateYRad(byPlayer, blockSel);
		bEBehaviorDoor.RotateYRad += ((bEBehaviorDoor.RotateYRad == -(float)Math.PI) ? (-(float)Math.PI) : ((float)Math.PI));
		bEBehaviorDoor.SetupRotationsAndColSelBoxes(initalSetup: true);
		obj.Orientation = BlockFacing.HorizontalFromAngle(bEBehaviorDoor.RotateYRad - (float)Math.PI / 2f);
		obj.Init();
		double totalHoursHeatReceived = itemstack.Attributes.GetDouble("totalHoursHeatReceived");
		obj.TotalHoursHeatReceived = totalHoursHeatReceived;
		obj.TotalHoursLastUpdate = world.Calendar.TotalHours;
		if (world.Side == EnumAppSide.Server)
		{
			GetBehavior<BlockBehaviorDoor>().placeMultiblockParts(world, pos);
		}
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockPos position = blockSel.Position;
		if (byPlayer.WorldData.EntityControls.CtrlKey && world.BlockAccessor.GetBlockEntity(position) is BlockEntityBeeHiveKiln blockEntityBeeHiveKiln)
		{
			blockEntityBeeHiveKiln.Interact(byPlayer);
			(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		ItemStack[] drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
		BlockEntityBeeHiveKiln blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityBeeHiveKiln>(pos);
		drops[0].Attributes["totalHoursHeatReceived"] = new DoubleAttribute(blockEntity.TotalHoursHeatReceived);
		return drops;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		BlockEntityBeeHiveKiln blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityBeeHiveKiln>(selection.Position);
		if (blockEntity != null && !blockEntity.StructureComplete)
		{
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-mulblock-struc-show",
					HotKeyCodes = new string[1] { "ctrl" },
					MouseButton = EnumMouseButton.Right
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-mulblock-struc-hide",
					HotKeyCodes = new string[2] { "ctrl", "shift" },
					MouseButton = EnumMouseButton.Right
				}
			};
		}
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
	}
}
