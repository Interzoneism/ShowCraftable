using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorRopeTieable : BlockBehavior
{
	private ClothManager cm;

	public BlockBehaviorRopeTieable(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		cm = api.ModLoader.GetModSystem<ClothManager>();
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		ClothSystem cs = cm.GetClothSystemAttachedToBlock(blockSel.Position);
		if (cs != null)
		{
			Entity byEntity = byPlayer.Entity;
			Vec3d vec3d = new Vec3d(0.0, byEntity.LocalEyePos.Y - 0.25, 0.0).AheadCopy(0.25, byEntity.SidedPos.Pitch, byEntity.SidedPos.Yaw);
			if (!activeHotbarSlot.Empty && activeHotbarSlot.Itemstack.Collectible.Code.Path == "rope")
			{
				if (cs.FirstPoint.PinnedToEntity?.EntityId == byPlayer.Entity.EntityId || cs.LastPoint.PinnedToEntity?.EntityId == byPlayer.Entity.EntityId)
				{
					return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
				}
				int num = activeHotbarSlot.Itemstack.Attributes.GetInt("clothId");
				if (((num == 0) ? null : cm.GetClothSystem(num)) != null)
				{
					return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
				}
				return false;
			}
			ClothPoint clothPoint = ((cs.FirstPoint.PinnedToBlockPos == blockSel.Position) ? cs.FirstPoint : cs.LastPoint);
			ItemStack itemStack = new ItemStack(world.GetItem(new AssetLocation("rope")));
			itemStack.Attributes.SetInt("clothId", cs.ClothId);
			itemStack.Attributes.SetLong("ropeHeldByEntityId", byEntity.EntityId);
			ItemStack ropestack = null;
			if (cs.FirstPoint.PinnedToEntity == byEntity || cs.LastPoint.PinnedToEntity == byEntity)
			{
				byPlayer.Entity.WalkInventory(delegate(ItemSlot slot)
				{
					if (!slot.Empty && slot.Itemstack.Attributes != null && slot.Itemstack.Attributes.GetInt("clothId") == cs.ClothId)
					{
						ropestack = slot.Itemstack;
						return false;
					}
					return true;
				});
				cs.WalkPoints(delegate(ClothPoint point)
				{
					if (point.PinnedToBlockPos != null || point.PinnedToEntity?.EntityId == byEntity.EntityId)
					{
						point.UnPin();
					}
				});
				if (!cs.PinnedAnywhere)
				{
					cm.UnregisterCloth(cs.ClothId);
					if (ropestack != null)
					{
						ropestack.Attributes.RemoveAttribute("clothId");
						ropestack.Attributes.RemoveAttribute("ropeHeldByEntityId");
					}
				}
			}
			if (ropestack == null)
			{
				if (activeHotbarSlot.Empty)
				{
					activeHotbarSlot.Itemstack = itemStack;
					activeHotbarSlot.MarkDirty();
					clothPoint.PinTo(byEntity, vec3d.ToVec3f());
				}
				else if (byPlayer.InventoryManager.TryGiveItemstack(itemStack, slotNotifyEffect: true))
				{
					clothPoint.PinTo(byEntity, vec3d.ToVec3f());
				}
				else
				{
					Entity entity = world.SpawnItemEntity(itemStack, blockSel.Position);
					if (entity != null)
					{
						clothPoint.PinTo(entity, new Vec3f(0f, 0.1f, 0f));
					}
				}
			}
			handling = EnumHandling.PreventDefault;
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
	}
}
