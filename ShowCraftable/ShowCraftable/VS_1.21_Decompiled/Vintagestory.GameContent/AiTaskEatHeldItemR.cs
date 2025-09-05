using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class AiTaskEatHeldItemR : AiTaskBaseR
{
	protected float currentUseTime;

	protected bool soundPlayed;

	protected bool isEdible;

	protected EntityBehaviorMultiplyBase? multiplyBehavior;

	private AiTaskEatHeldItemConfig Config => GetConfig<AiTaskEatHeldItemConfig>();

	public AiTaskEatHeldItemR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskEatHeldItemConfig>(entity, taskConfig, aiConfig);
	}

	public override void AfterInitialize()
	{
		base.AfterInitialize();
		multiplyBehavior = entity.GetBehavior<EntityBehaviorMultiplyBase>();
	}

	public override bool ShouldExecute()
	{
		if (!PreconditionsSatisficed())
		{
			return false;
		}
		if (multiplyBehavior != null && !multiplyBehavior.ShouldEat && entity.World.Rand.NextDouble() >= (double)Config.ChanceToUseFoodWithoutEating)
		{
			return false;
		}
		ItemSlot slot = GetSlot();
		if (slot == null || slot.Empty)
		{
			return false;
		}
		ItemStack itemstack = slot.Itemstack;
		if (itemstack == null)
		{
			return false;
		}
		if (!SuitableFoodSource(itemstack))
		{
			if (!slot.Empty)
			{
				entity.World.SpawnItemEntity(slot.TakeOutWhole(), entity.ServerPos.XYZ);
			}
			return false;
		}
		isEdible = true;
		return true;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		soundPlayed = false;
		currentUseTime = 0f;
	}

	public override bool ContinueExecute(float dt)
	{
		base.ContinueExecute(dt);
		currentUseTime += dt;
		ItemSlot slot = GetSlot();
		if (slot == null || slot.Empty)
		{
			return false;
		}
		entity.World.SpawnCubeParticles(entity.ServerPos.XYZ, slot.Itemstack, 0.25f, 1, 0.25f + 0.5f * (float)entity.World.Rand.NextDouble());
		if (currentUseTime >= Config.DurationSec)
		{
			if (isEdible && Config.ConsumePortion)
			{
				ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("hunger");
				if (treeAttribute == null)
				{
					treeAttribute = (ITreeAttribute)(entity.WatchedAttributes["hunger"] = new TreeAttribute());
				}
				treeAttribute.SetFloat("saturation", Config.SaturationPerPortion + treeAttribute.GetFloat("saturation"));
			}
			slot.TakeOut(1);
			return false;
		}
		return true;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		if (cancelled)
		{
			cooldownUntilTotalHours = 0.0;
		}
	}

	protected virtual bool SuitableFoodSource(ItemStack itemStack)
	{
		return Config.Diet?.Matches(itemStack) ?? true;
	}

	protected virtual ItemSlot? GetSlot()
	{
		return Config.HandToEatFrom switch
		{
			EnumHand.Left => entity.LeftHandItemSlot, 
			EnumHand.Right => entity.RightHandItemSlot, 
			_ => null, 
		};
	}
}
