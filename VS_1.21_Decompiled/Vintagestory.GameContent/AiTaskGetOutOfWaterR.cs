using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskGetOutOfWaterR : AiTaskBaseR
{
	protected Vec3d target = new Vec3d();

	protected bool done;

	protected int searchAttempts;

	protected const float minimumRangeOffset = 0.6f;

	protected const int triesPerAttempt = 10;

	private readonly Vec3d tmpPos = new Vec3d();

	private readonly BlockPos pos = new BlockPos(0);

	private AiTaskGetOutOfWaterConfig Config => GetConfig<AiTaskGetOutOfWaterConfig>();

	public AiTaskGetOutOfWaterR(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		baseConfig = AiTaskBaseR.LoadConfig<AiTaskGetOutOfWaterConfig>(entity, taskConfig, aiConfig);
		Config.WhenSwimming = true;
	}

	public override bool ShouldExecute()
	{
		if (!PreconditionsSatisficed())
		{
			return false;
		}
		int num = GameMath.Min(Config.MinimumRangeToSeekLand, (int)((float)Config.MinimumRangeToSeekLand * 0.6f + (float)searchAttempts * Config.RangeSearchAttemptsFactor));
		target.Y = entity.ServerPos.Y;
		int num2 = 10;
		int num3 = (int)entity.ServerPos.X;
		int num4 = (int)entity.ServerPos.Z;
		IBlockAccessor blockAccessor = entity.World.BlockAccessor;
		while (num2-- > 0)
		{
			pos.X = num3 + base.Rand.Next(num + 1) - num / 2;
			pos.Z = num4 + base.Rand.Next(num + 1) - num / 2;
			pos.Y = blockAccessor.GetTerrainMapheightAt(pos) + 1;
			if (!blockAccessor.GetBlock(pos, 2).IsLiquid() && !entity.World.CollisionTester.IsColliding(blockAccessor, entity.CollisionBox, tmpPos.Set((double)pos.X + 0.5, (float)pos.Y + 0.1f, (double)pos.Z + 0.5)) && entity.World.CollisionTester.IsColliding(blockAccessor, entity.CollisionBox, tmpPos.Set((double)pos.X + 0.5, (float)pos.Y - 0.1f, (double)pos.Z + 0.5)))
			{
				target.Set((double)pos.X + 0.5, pos.Y + 1, (double)pos.Z + 0.5);
				return true;
			}
		}
		searchAttempts++;
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		searchAttempts = 0;
		done = false;
		pathTraverser.WalkTowards(target, Config.MoveSpeed, 0.5f, OnGoalReached, OnStuck);
	}

	public override bool ContinueExecute(float dt)
	{
		if (base.Rand.NextDouble() < (double)Config.ChanceToStopTask && !entity.FeetInLiquid)
		{
			return false;
		}
		return !done;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		pathTraverser.Stop();
	}

	protected virtual void OnStuck()
	{
		done = true;
	}

	protected virtual void OnGoalReached()
	{
		done = true;
	}
}
