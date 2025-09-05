using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemStrawDummy : Item
{
	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel == null)
		{
			return;
		}
		IPlayer player = byEntity.World.PlayerByUid((byEntity as EntityPlayer)?.PlayerUID);
		int num = (int)((float)(blockSel.Position.X + ((!blockSel.DidOffset) ? blockSel.Face.Normali.X : 0)) + 0.5f);
		int num2 = blockSel.Position.Y + ((!blockSel.DidOffset) ? blockSel.Face.Normali.Y : 0);
		int num3 = (int)((float)(blockSel.Position.Z + ((!blockSel.DidOffset) ? blockSel.Face.Normali.Z : 0)) + 0.5f);
		BlockPos pos = new BlockPos(num, num2, num3);
		if (!byEntity.World.Claims.TryAccess(player, pos, EnumBlockAccessFlags.BuildOrBreak))
		{
			slot.MarkDirty();
			return;
		}
		if (!(byEntity is EntityPlayer) || player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			slot.TakeOut(1);
			slot.MarkDirty();
		}
		EntityProperties entityType = byEntity.World.GetEntityType(new AssetLocation("strawdummy"));
		Entity entity = byEntity.World.ClassRegistry.CreateEntity(entityType);
		if (entity != null)
		{
			entity.ServerPos.X = num;
			entity.ServerPos.Y = num2;
			entity.ServerPos.Z = num3;
			entity.ServerPos.Yaw = byEntity.SidedPos.Yaw + (float)Math.PI / 2f;
			if (player != null && player.PlayerUID != null)
			{
				entity.WatchedAttributes.SetString("ownerUid", player.PlayerUID);
			}
			entity.Pos.SetFrom(entity.ServerPos);
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/torch"), entity, player);
			byEntity.World.SpawnEntity(entity);
			handling = EnumHandHandling.PreventDefaultAction;
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-place",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
