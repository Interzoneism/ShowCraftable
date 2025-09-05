using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockOmokTable : Block
{
	private Cuboidf[] seleBoxes;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		int num = 15;
		seleBoxes = new Cuboidf[num * num];
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				seleBoxes[j * num + i] = new Cuboidf
				{
					X1 = (0.5f + (float)i) / 16f,
					Y1 = 0.0625f,
					Z1 = (0.5f + (float)j) / 16f,
					X2 = (1.5f + (float)i) / 16f,
					Y2 = 0.125f,
					Z2 = (1.5f + (float)j) / 16f
				};
			}
		}
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (seleBoxes == null)
		{
			return base.GetSelectionBoxes(blockAccessor, pos);
		}
		return seleBoxes;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityOmokTable blockEntityOmokTable)
		{
			return blockEntityOmokTable.OnInteract(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}
}
