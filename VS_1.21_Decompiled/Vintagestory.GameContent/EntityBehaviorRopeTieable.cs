using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorRopeTieable : EntityBehavior
{
	private int minGeneration;

	public IntArrayAttribute ClothIds => entity.WatchedAttributes["clothIds"] as IntArrayAttribute;

	public EntityBehaviorRopeTieable(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		minGeneration = attributes?["minGeneration"].AsInt() ?? 0;
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		base.AfterInitialized(onFirstSpawn);
		EntityBehaviorTaskAI behavior = entity.GetBehavior<EntityBehaviorTaskAI>();
		if (behavior != null)
		{
			behavior.TaskManager.OnShouldExecuteTask += (IAiTask t) => (entity as EntityAgent)?.MountedOn == null || t is AiTaskIdle;
		}
		IntArrayAttribute clothIds = ClothIds;
		if (clothIds != null && clothIds.value.Length != 0)
		{
			int[] value = clothIds.value;
			foreach (int clothid in value)
			{
				entity.World.Api.ModLoader.GetModSystem<ClothManager>().GetClothSystem(clothid)?.OnPinnnedEntityLoaded(entity);
			}
		}
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
		IntArrayAttribute clothIds = ClothIds;
		if (clothIds == null || clothIds.value.Length == 0)
		{
			return;
		}
		int num = ClothIds.value[0];
		ClothIds.RemoveInt(num);
		ClothManager modSystem = byEntity.World.Api.ModLoader.GetModSystem<ClothManager>();
		ClothSystem sys = modSystem.GetClothSystem(num);
		if (sys == null)
		{
			return;
		}
		Detach(sys);
		ItemSlot ropeInhandsSlot = null;
		ClothPoint[] ends = sys.Ends;
		byEntity.WalkInventory(delegate(ItemSlot slot)
		{
			if (slot.Empty)
			{
				return true;
			}
			if (slot.Itemstack.Collectible is ItemRope)
			{
				if (slot.Itemstack.Attributes.GetInt("clothId") == sys.ClothId)
				{
					ropeInhandsSlot = slot;
				}
				return false;
			}
			return true;
		});
		if (ropeInhandsSlot == null)
		{
			Vec3d vec3d = new Vec3d(0.0, byEntity.LocalEyePos.Y - 0.25, 0.0).AheadCopy(0.25, byEntity.SidedPos.Pitch, byEntity.SidedPos.Yaw);
			(ends[0].Pinned ? ends[1] : ends[0]).PinTo(byEntity, vec3d.ToVec3f());
			if ((ends[0].PinnedToEntity as EntityItem)?.Itemstack?.Collectible is ItemRope || (ends[1].PinnedToEntity as EntityItem)?.Itemstack?.Collectible is ItemRope)
			{
				ItemStack itemStack = new ItemStack(entity.World.GetItem(new AssetLocation("rope")));
				itemStack.Attributes.SetInt("clothId", sys.ClothId);
				itemStack.Attributes.SetLong("ropeHeldByEntityId", byEntity.EntityId);
				if (!byEntity.TryGiveItemStack(itemStack))
				{
					entity.World.SpawnItemEntity(itemStack, byEntity.Pos.XYZ);
				}
			}
		}
		else
		{
			ends[0].UnPin();
			ends[1].UnPin();
			ropeInhandsSlot.Itemstack.Attributes.RemoveAttribute("clothId");
			ropeInhandsSlot.Itemstack.Attributes.RemoveAttribute("ropeHeldByEntityId");
			modSystem.UnregisterCloth(sys.ClothId);
		}
	}

	public override string PropertyName()
	{
		return "ropetieable";
	}

	public void Detach(ClothSystem sys)
	{
		if (ClothIds == null)
		{
			return;
		}
		ClothIds.RemoveInt(sys.ClothId);
		if (ClothIds.value.Length == 0)
		{
			entity.WatchedAttributes.RemoveAttribute("clothIds");
		}
		sys.WalkPoints(delegate(ClothPoint point)
		{
			if (point.PinnedToEntity?.EntityId == entity.EntityId)
			{
				point.UnPin();
			}
		});
	}

	public bool CanAttach()
	{
		if (entity.WatchedAttributes.GetInt("generation") < minGeneration)
		{
			(entity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "toowild", Lang.Get("Animal is too wild to attach a rope to"));
			return false;
		}
		return true;
	}

	public void Attach(ClothSystem sys, ClothPoint point)
	{
		if (!entity.WatchedAttributes.HasAttribute("clothIds"))
		{
			entity.WatchedAttributes["clothIds"] = new IntArrayAttribute(new int[1] { sys.ClothId });
		}
		if (!ClothIds.value.Contains(sys.ClothId))
		{
			ClothIds.AddInt(sys.ClothId);
		}
		point.PinTo(entity, new Vec3f(0f, 0.5f, 0f));
	}
}
