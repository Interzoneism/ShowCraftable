using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskIdleR : AiTaskBaseTargetableR
{
	private AiTaskIdleConfig Config => GetConfig<AiTaskIdleConfig>();

	public AiTaskIdleR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskIdleConfig>(entity, taskConfig, aiConfig);
	}

	public override bool ShouldExecute()
	{
		if (!PreconditionsSatisficed())
		{
			return false;
		}
		if (CheckForTargetToStop())
		{
			return false;
		}
		if (!CheckForBlockBelow())
		{
			return false;
		}
		return true;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		entity.IdleSoundChanceModifier = 0f;
	}

	public override bool ContinueExecute(float dt)
	{
		if (!base.ContinueExecute(dt))
		{
			return false;
		}
		if (base.Rand.NextDouble() <= (double)Config.ChanceToCheckTarget && CheckForTargetToStop())
		{
			return false;
		}
		return true;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		entity.IdleSoundChanceModifier = 1f;
	}

	protected virtual bool CheckForTargetToStop()
	{
		if (!Config.StopWhenTargetDetected)
		{
			return false;
		}
		if (!CheckAndResetSearchCooldown())
		{
			return false;
		}
		return SearchForTarget();
	}

	protected virtual bool CheckForBlockBelow()
	{
		Block blockRaw = entity.World.BlockAccessor.GetBlockRaw((int)entity.ServerPos.X, (int)entity.ServerPos.InternalY - 1, (int)entity.ServerPos.Z, 1);
		if (Config.CheckForSolidUpSide && !blockRaw.SideSolid[BlockFacing.UP.Index])
		{
			return false;
		}
		if (Config.IgnoreBlockCodeAndTags)
		{
			return true;
		}
		Block blockRaw2 = entity.World.BlockAccessor.GetBlockRaw((int)entity.ServerPos.X, (int)entity.ServerPos.InternalY, (int)entity.ServerPos.Z);
		if (blockRaw2.Replaceable >= Config.MinBlockInsideReplaceable)
		{
			return CheckForBlock(blockRaw);
		}
		return CheckForBlock(blockRaw2);
	}

	protected virtual bool CheckForBlock(Block block)
	{
		if (Config.IgnoreBlockCodeAndTags)
		{
			return true;
		}
		if (Config.AllowedBlockBelowTags != BlockTagRule.Empty && !Config.AllowedBlockBelowTags.Intersects(block.Tags))
		{
			return false;
		}
		if (Config.SkipBlockBelowTags != BlockTagRule.Empty && Config.SkipBlockBelowTags.Intersects(block.Tags))
		{
			return false;
		}
		if (Config.AllowedBlockBelowCode != null && !block.WildCardMatch(Config.AllowedBlockBelowCode))
		{
			return false;
		}
		return true;
	}
}
