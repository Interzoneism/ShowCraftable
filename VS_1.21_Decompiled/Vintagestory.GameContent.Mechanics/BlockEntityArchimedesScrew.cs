using System;

namespace Vintagestory.GameContent.Mechanics;

public class BlockEntityArchimedesScrew : BlockEntityItemFlow
{
	public override float ItemFlowRate
	{
		get
		{
			BEBehaviorMPArchimedesScrew behavior = GetBehavior<BEBehaviorMPArchimedesScrew>();
			if (behavior?.Network == null)
			{
				return 0f;
			}
			return Math.Abs(behavior.Network.Speed) * itemFlowRate;
		}
	}
}
