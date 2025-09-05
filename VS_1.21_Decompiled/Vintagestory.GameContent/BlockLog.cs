using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockLog : Block
{
	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return Drops[0].ResolvedItemstack.Clone();
	}

	public override void AddMiningTierInfo(StringBuilder sb)
	{
		if (Code.PathStartsWith("log-grown"))
		{
			int num = Attributes?["treeFellingGroupSpreadIndex"].AsInt() ?? 0;
			num += RequiredMiningTier - 4;
			if (num < RequiredMiningTier)
			{
				num = RequiredMiningTier;
			}
			string text = "?";
			if (num < Block.miningTierNames.Length)
			{
				text = Block.miningTierNames[num];
			}
			sb.AppendLine(Lang.Get("Requires tool tier {0} ({1}) to break", num, (text == "?") ? text : Lang.Get(text)));
		}
		else
		{
			base.AddMiningTierInfo(sb);
		}
	}
}
