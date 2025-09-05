using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class EntityBehaviorRipHarvestable : EntityBehavior
{
	protected float ripHarvestChance = 0.25f;

	public EntityBehaviorRipHarvestable(Entity entity)
		: base(entity)
	{
	}

	public override string PropertyName()
	{
		return "ripharvestable";
	}

	public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
	{
		if (!(damage > 0f) || entity.World.Side != EnumAppSide.Server)
		{
			return;
		}
		EntityAgent entityAgent = damageSource.SourceEntity as EntityAgent;
		ItemSlot itemSlot = entityAgent?.RightHandItemSlot;
		if (itemSlot?.Itemstack?.ItemAttributes != null && itemSlot.Itemstack.ItemAttributes.IsTrue("ripHarvest") && entity.World.Rand.NextDouble() < (double)ripHarvestChance)
		{
			EntityBehaviorHarvestable behavior = entity.GetBehavior<EntityBehaviorHarvestable>();
			behavior?.GenerateDrops((entityAgent as EntityPlayer).Player);
			ItemSlot firstNonEmptySlot = behavior.Inventory.FirstNonEmptySlot;
			if (firstNonEmptySlot != null)
			{
				entity.World.SpawnItemEntity(firstNonEmptySlot.TakeOutWhole(), entity.ServerPos.XYZ);
			}
		}
	}
}
