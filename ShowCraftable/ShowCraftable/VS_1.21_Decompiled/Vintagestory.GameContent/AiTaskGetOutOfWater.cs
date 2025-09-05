using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskGetOutOfWater : AiTaskBase
{
	private Vec3d target = new Vec3d();

	private BlockPos pos = new BlockPos();

	private bool done;

	private float moveSpeed = 0.03f;

	private int searchattempts;

	public AiTaskGetOutOfWater(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
		: base(entity, taskConfig, aiConfig)
	{
		moveSpeed = taskConfig["movespeed"].AsFloat(0.06f);
	}

	public override bool ShouldExecute()
	{
		if (!entity.Swimming)
		{
			return false;
		}
		if (base.rand.NextDouble() > 0.03999999910593033)
		{
			return false;
		}
		int num = GameMath.Min(50, 30 + searchattempts * 2);
		target.Y = entity.ServerPos.Y;
		int num2 = 10;
		int num3 = (int)entity.ServerPos.X;
		int num4 = (int)entity.ServerPos.Z;
		IBlockAccessor blockAccessor = entity.World.BlockAccessor;
		Vec3d vec3d = new Vec3d();
		while (num2-- > 0)
		{
			pos.X = num3 + base.rand.Next(num + 1) - num / 2;
			pos.Z = num4 + base.rand.Next(num + 1) - num / 2;
			pos.Y = blockAccessor.GetTerrainMapheightAt(pos) + 1;
			if (!blockAccessor.GetBlock(pos, 2).IsLiquid())
			{
				blockAccessor.GetBlock(pos);
				if (!entity.World.CollisionTester.IsColliding(blockAccessor, entity.CollisionBox, vec3d.Set((double)pos.X + 0.5, (float)pos.Y + 0.1f, (double)pos.Z + 0.5)) && entity.World.CollisionTester.IsColliding(blockAccessor, entity.CollisionBox, vec3d.Set((double)pos.X + 0.5, (float)pos.Y - 0.1f, (double)pos.Z + 0.5)))
				{
					target.Set((double)pos.X + 0.5, pos.Y + 1, (double)pos.Z + 0.5);
					return true;
				}
			}
		}
		searchattempts++;
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		searchattempts = 0;
		done = false;
		pathTraverser.WalkTowards(target, moveSpeed, 0.5f, OnGoalReached, OnStuck);
	}

	public override bool ContinueExecute(float dt)
	{
		if (!IsInValidDayTimeHours(initialRandomness: false))
		{
			return false;
		}
		if (base.rand.NextDouble() < 0.10000000149011612 && !entity.FeetInLiquid)
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

	private void OnStuck()
	{
		done = true;
	}

	private void OnGoalReached()
	{
		done = true;
	}
}
