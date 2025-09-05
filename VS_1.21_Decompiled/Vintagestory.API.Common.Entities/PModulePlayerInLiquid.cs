using System;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common.Entities;

public class PModulePlayerInLiquid : PModuleInLiquid
{
	private long lastPush;

	private readonly IPlayer player;

	private BlockPos tmpPos = new BlockPos();

	public PModulePlayerInLiquid(EntityPlayer entityPlayer)
	{
		player = entityPlayer.World.PlayerByUid(entityPlayer.PlayerUID);
	}

	public override void HandleSwimming(float dt, Entity entity, EntityPos pos, EntityControls controls)
	{
		if ((controls.TriesToMove || controls.Jump) && entity.World.ElapsedMilliseconds - lastPush > 2000)
		{
			Push = 6f;
			lastPush = entity.World.ElapsedMilliseconds;
			entity.PlayEntitySound("swim", player);
		}
		else
		{
			Push = Math.Max(1f, Push - 0.1f * dt * 60f);
		}
		tmpPos.dimension = pos.Dimension;
		tmpPos.Set((int)pos.X, (int)pos.Y, (int)pos.Z);
		Block block = entity.World.BlockAccessor.GetBlock(tmpPos, 2);
		Block blockAbove = entity.World.BlockAccessor.GetBlockAbove(tmpPos, 1, 2);
		Block blockAbove2 = entity.World.BlockAccessor.GetBlockAbove(tmpPos, 2, 2);
		float num = GameMath.Clamp((float)(int)pos.Y + (float)block.LiquidLevel / 8f + (blockAbove.IsLiquid() ? 1.125f : 0f) + (blockAbove2.IsLiquid() ? 1.125f : 0f) - (float)pos.Y - (float)entity.SwimmingOffsetY, 0f, 1f);
		num = Math.Min(1f, num + 0.075f);
		double y = 0.0;
		if (controls.Jump)
		{
			if (num > 0.1f || !controls.TriesToMove)
			{
				y = 0.005f * num * dt * 60f;
			}
		}
		else
		{
			y = controls.FlyVector.Y * (double)(1f + Push) * 0.029999999329447746 * (double)num;
		}
		pos.Motion.Add(controls.FlyVector.X * (double)(1f + Push) * 0.029999999329447746, y, controls.FlyVector.Z * (double)(1f + Push) * 0.029999999329447746);
	}
}
