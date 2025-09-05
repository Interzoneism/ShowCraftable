using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ItemCheese : Item
{
	public string Type => Variant["type"];

	public string Part => Variant["part"];

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (byEntity.Controls.ShiftKey && blockSel != null)
		{
			IPlayer player = null;
			if (byEntity is EntityPlayer)
			{
				player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			if (player == null || !byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
			{
				return;
			}
			Block block = api.World.GetBlock(new AssetLocation("cheese"));
			BlockPos blockPos = blockSel.Position.AddCopy(blockSel.Face);
			string failureCode = "";
			BlockSelection blockSelection = blockSel.Clone();
			blockSelection.Position.Add(blockSel.Face);
			if (block.TryPlaceBlock(api.World, player, slot.Itemstack, blockSelection, ref failureCode))
			{
				if (api.World.BlockAccessor.GetBlockEntity(blockPos) is BECheese)
				{
					slot.TakeOut(1);
					slot.MarkDirty();
				}
				api.World.PlaySoundAt(block.Sounds.Place, (double)blockPos.X + 0.5, blockPos.InternalY, (double)blockPos.Z + 0.5, player);
				handling = EnumHandHandling.PreventDefault;
			}
			else
			{
				(api as ICoreClientAPI)?.TriggerIngameError(this, failureCode, Lang.Get("placefailure-" + failureCode));
			}
		}
		else
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		}
	}

	public override ItemStack OnTransitionNow(ItemSlot slot, TransitionableProperties props)
	{
		if (props.Type == EnumTransitionType.Ripen)
		{
			BlockPos pos = slot.Inventory.Pos;
			if (pos != null)
			{
				Room roomForPosition = api.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(pos);
				int lightLevel = api.World.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight);
				if (roomForPosition.ExitCount > 0 && lightLevel < 2)
				{
					return new ItemStack(api.World.GetItem(new AssetLocation("cheese-blue-4slice")));
				}
			}
		}
		return base.OnTransitionNow(slot, props);
	}
}
