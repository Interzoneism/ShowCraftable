using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BEMPMultiblock : BlockEntity
{
	public BlockPos Principal { get; set; }

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
	{
		base.FromTreeAttributes(tree, world);
		int num = tree.GetInt("cx");
		int num2 = tree.GetInt("cy");
		int num3 = tree.GetInt("cz");
		if (num2 == -1 && num == -1 && num3 == -1)
		{
			Principal = null;
			return;
		}
		Principal = new BlockPos(num, num2, num3);
		if (world.BlockAccessor.GetBlockEntity(Principal) is IGearAcceptor gearAcceptor)
		{
			gearAcceptor.RemoveGearAt(Pos);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("cx", (Principal == null) ? (-1) : Principal.X);
		tree.SetInt("cy", (Principal == null) ? (-1) : Principal.Y);
		tree.SetInt("cz", (Principal == null) ? (-1) : Principal.Z);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		if (Api.World.EntityDebugMode)
		{
			if (Principal == null)
			{
				sb.AppendLine("null center");
				return;
			}
			sb.AppendLine("center at " + Principal);
		}
		if (!(Principal == null))
		{
			BlockEntity obj = Api.World?.BlockAccessor.GetBlockEntity(Principal);
			if (obj == null)
			{
				sb.AppendLine("null be");
			}
			obj?.GetBlockInfo(forPlayer, sb);
		}
	}
}
