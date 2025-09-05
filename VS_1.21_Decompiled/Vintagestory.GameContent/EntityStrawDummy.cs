using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityStrawDummy : EntityHumanoid
{
	public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
	{
		if (!Api.World.Claims.TryAccess(((EntityPlayer)byEntity).Player, Pos.AsBlockPos, EnumBlockAccessFlags.Use) || !Alive || World.Side == EnumAppSide.Client || mode == EnumInteractMode.Attack)
		{
			base.OnInteract(byEntity, slot, hitPosition, mode);
			return;
		}
		string text = WatchedAttributes.GetString("ownerUid");
		string text2 = (byEntity as EntityPlayer)?.PlayerUID;
		if (text2 != null && (text == null || text == "" || text == text2) && byEntity.Controls.ShiftKey)
		{
			ItemStack itemStack = new ItemStack(byEntity.World.GetItem(new AssetLocation("strawdummy")));
			if (!byEntity.TryGiveItemStack(itemStack))
			{
				byEntity.World.SpawnItemEntity(itemStack, ServerPos.XYZ);
			}
			byEntity.World.Logger.Audit("{0} Took 1x{1} at {2}.", byEntity.GetName(), itemStack.Collectible.Code, ServerPos.AsBlockPos);
			Die();
		}
		else
		{
			base.OnInteract(byEntity, slot, hitPosition, mode);
		}
	}
}
