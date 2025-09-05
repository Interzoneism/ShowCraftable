using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorMultiblock : BlockBehavior
{
	[DocumentAsJson("Recommended", "3", false)]
	private int SizeX;

	[DocumentAsJson("Recommended", "3", false)]
	private int SizeY;

	[DocumentAsJson("Recommended", "3", false)]
	private int SizeZ;

	[DocumentAsJson("Recommended", "(1, 0, 1)", false)]
	private Vec3i ControllerPositionRel;

	private string type;

	public BlockBehaviorMultiblock(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		SizeX = properties["sizex"].AsInt(3);
		SizeY = properties["sizey"].AsInt(3);
		SizeZ = properties["sizez"].AsInt(3);
		type = properties["type"].AsString("monolithic");
		ControllerPositionRel = properties["cposition"].AsObject(new Vec3i(1, 0, 1));
	}

	public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		bool blocked = false;
		IterateOverEach(blockSel.Position, delegate(BlockPos mpos)
		{
			if (mpos == blockSel.Position)
			{
				return true;
			}
			if (!world.BlockAccessor.GetBlock(mpos).IsReplacableBy(block))
			{
				blocked = true;
				return false;
			}
			return true;
		});
		if (blocked)
		{
			handling = EnumHandling.PreventDefault;
			failureCode = "notenoughspace";
			return false;
		}
		return true;
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		IterateOverEach(pos, delegate(BlockPos mpos)
		{
			if (mpos == pos)
			{
				return true;
			}
			int num = mpos.X - pos.X;
			int num2 = mpos.Y - pos.Y;
			int num3 = mpos.Z - pos.Z;
			string text = ((num < 0) ? "n" : ((num > 0) ? "p" : "")) + Math.Abs(num);
			string text2 = ((num2 < 0) ? "n" : ((num2 > 0) ? "p" : "")) + Math.Abs(num2);
			string text3 = ((num3 < 0) ? "n" : ((num3 > 0) ? "p" : "")) + Math.Abs(num3);
			AssetLocation assetLocation = new AssetLocation("multiblock-" + type + "-" + text + "-" + text2 + "-" + text3);
			Block block = world.GetBlock(assetLocation);
			if (block == null)
			{
				throw new IndexOutOfRangeException("Multiblocks are currently limited to 5x5x5 with the controller being in the middle of it, yours likely exceeds the limit because I could not find block with code " + assetLocation.Path);
			}
			world.BlockAccessor.SetBlock(block.Id, mpos);
			return true;
		});
	}

	public void IterateOverEach(BlockPos controllerPos, ActionConsumable<BlockPos> onBlock)
	{
		int num = controllerPos.X - ControllerPositionRel.X;
		int num2 = controllerPos.Y - ControllerPositionRel.Y;
		int num3 = controllerPos.Z - ControllerPositionRel.Z;
		BlockPos blockPos = new BlockPos(controllerPos.dimension);
		for (int i = 0; i < SizeX; i++)
		{
			for (int j = 0; j < SizeY; j++)
			{
				for (int k = 0; k < SizeZ; k++)
				{
					blockPos.Set(num + i, num2 + j, num3 + k);
					if (!onBlock(blockPos))
					{
						return;
					}
				}
			}
		}
	}

	public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		IterateOverEach(pos, delegate(BlockPos mpos)
		{
			if (mpos == pos)
			{
				return true;
			}
			if (world.BlockAccessor.GetBlock(mpos) is BlockMultiblock)
			{
				world.BlockAccessor.SetBlock(0, mpos);
			}
			return true;
		});
	}
}
