using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BaseAimingAccuracy : AccuracyModifier
{
	public BaseAimingAccuracy(EntityAgent entity)
		: base(entity)
	{
	}

	public override void Update(float dt, ref float accuracy)
	{
		float blended = entity.Stats.GetBlended("rangedWeaponsAcc");
		float blended2 = entity.Stats.GetBlended("rangedWeaponsSpeed");
		float max = Math.Min(1f - 0.075f / blended, 1f);
		accuracy = GameMath.Clamp(base.SecondsSinceAimStart * blended2 * 1.7f, 0f, max);
		if (base.SecondsSinceAimStart >= 0.75f)
		{
			accuracy += GameMath.Sin(base.SecondsSinceAimStart * 8f) / 80f / Math.Max(1f, blended);
		}
	}
}
