using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class FruitData
{
	public double transitionDate;

	public int currentStage;

	public BEBehaviorFruiting behavior;

	public Vec3f rotation;

	public int variant;

	public FruitData(int variant, double totalDays, BEBehaviorFruiting be, Vec3f rot)
	{
		this.variant = variant;
		transitionDate = totalDays;
		behavior = be;
		rotation = rot;
	}

	internal void SetRandomRotation(IWorldAccessor world, int index, Vec3d vec3d, BlockPos pos)
	{
		if (rotation == null)
		{
			double y = vec3d.X - (double)pos.X - 0.5;
			double x = vec3d.Z - (double)pos.Z - 0.5;
			double num = (Math.Atan2(y, x) + 3.1415927410125732) % 6.2831854820251465;
			num += (double)((float)(world.Rand.NextDouble() * 6.2831854820251465 - 3.1415927410125732) / 70f);
			if (num < 0.0)
			{
				num += 6.2831854820251465;
			}
			rotation = new Vec3f((float)(world.Rand.NextDouble() * 6.2831854820251465 - 3.1415927410125732) / 50f, (float)num, (float)(world.Rand.NextDouble() * 6.2831854820251465 - 3.1415927410125732) / 50f);
		}
	}
}
