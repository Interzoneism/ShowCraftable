using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockFenceGate : BlockBaseDoor
{
	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		CanStep = false;
	}

	public override string GetKnobOrientation()
	{
		return GetKnobOrientation(this);
	}

	public override BlockFacing GetDirection()
	{
		return BlockFacing.FromFirstLetter(Variant["type"]);
	}

	public string GetKnobOrientation(Block block)
	{
		return Variant["knobOrientation"];
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			BlockFacing[] array = Block.SuggestedHVOrientation(byPlayer, blockSel);
			string text = ((array[0] == BlockFacing.NORTH || array[0] == BlockFacing.SOUTH) ? "n" : "w");
			bool neighbourOpen;
			string suggestedKnobOrientation = GetSuggestedKnobOrientation(world.BlockAccessor, blockSel.Position, array[0], out neighbourOpen);
			AssetLocation code = CodeWithVariants(new string[3] { "type", "state", "knobOrientation" }, new string[3]
			{
				text,
				neighbourOpen ? "opened" : "closed",
				suggestedKnobOrientation
			});
			world.BlockAccessor.SetBlock(world.BlockAccessor.GetBlock(code).BlockId, blockSel.Position);
			return true;
		}
		return false;
	}

	private string GetSuggestedKnobOrientation(IBlockAccessor ba, BlockPos pos, BlockFacing facing, out bool neighbourOpen)
	{
		string result = "left";
		Block block = ba.GetBlock(pos.AddCopy(facing.GetCW()));
		Block block2 = ba.GetBlock(pos.AddCopy(facing.GetCCW()));
		bool flag = facing == BlockFacing.EAST || facing == BlockFacing.SOUTH;
		bool flag2 = IsSameDoor(block);
		bool flag3 = IsSameDoor(block2);
		if (flag2 && flag3)
		{
			result = "left";
			neighbourOpen = (block as BlockBaseDoor).IsOpened();
		}
		else if (flag2)
		{
			if (GetKnobOrientation(block) == "right")
			{
				result = (flag ? "left" : "right");
				neighbourOpen = false;
			}
			else
			{
				result = (flag ? "right" : "left");
				neighbourOpen = (block as BlockBaseDoor).IsOpened();
			}
		}
		else if (flag3)
		{
			if (GetKnobOrientation(block2) == "right")
			{
				result = (flag ? "right" : "left");
				neighbourOpen = false;
			}
			else
			{
				result = (flag ? "left" : "right");
				neighbourOpen = (block2 as BlockBaseDoor).IsOpened();
			}
		}
		else
		{
			neighbourOpen = false;
			if ((block.Attributes?.IsTrue("isFence") ?? false) ^ (block2.Attributes?.IsTrue("isFence") ?? false))
			{
				result = ((flag ^ (block2.Attributes?.IsTrue("isFence") ?? false)) ? "left" : "right");
			}
			else if (block2.Replaceable >= 6000 && block.Replaceable < 6000)
			{
				result = (flag ? "left" : "right");
			}
			else if (block.Replaceable >= 6000 && block2.Replaceable < 6000)
			{
				result = (flag ? "right" : "left");
			}
		}
		return result;
	}

	protected override void Open(IWorldAccessor world, IPlayer byPlayer, BlockPos pos)
	{
		AssetLocation code = CodeWithVariant("state", IsOpened() ? "closed" : "opened");
		world.BlockAccessor.ExchangeBlock(world.BlockAccessor.GetBlock(code).BlockId, pos);
	}

	public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs)
	{
		if (activationArgs == null || !activationArgs.HasAttribute("opened") || activationArgs.GetBool("opened") != IsOpened())
		{
			Open(world, caller.Player, blockSel.Position);
		}
	}

	protected override BlockPos TryGetConnectedDoorPos(BlockPos pos)
	{
		string knobOrientation = GetKnobOrientation();
		BlockFacing direction = GetDirection();
		if (!(knobOrientation == "right"))
		{
			return pos.AddCopy(direction.GetCW());
		}
		return pos.AddCopy(direction.GetCCW());
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[4] { "type", "state", "knobOrientation", "cover" }, new string[4] { "n", "closed", "left", "free" }));
		return new ItemStack[1]
		{
			new ItemStack(block)
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariants(new string[4] { "type", "state", "knobOrientation", "cover" }, new string[4] { "n", "closed", "left", "free" })));
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		BlockFacing blockFacing = BlockFacing.FromFirstLetter(Variant["type"]);
		BlockFacing blockFacing2 = BlockFacing.HORIZONTALS_ANGLEORDER[(blockFacing.HorizontalAngleIndex + angle / 90) % 4];
		string text = Variant["type"];
		string text2 = text;
		if (blockFacing.Axis != blockFacing2.Axis)
		{
			text2 = ((text == "n") ? "w" : "n");
		}
		string text3 = Variant["knobOrientation"];
		if (text == "n" && text2 == "w" && text3 == "right" && angle == 90)
		{
			text3 = "left";
		}
		else if (text == "n" && text2 == "w" && text3 == "left" && angle == 90)
		{
			text3 = "right";
		}
		else if (text == "w" && text2 == "n" && text3 == "right" && angle == 270)
		{
			text3 = "left";
		}
		else if (text == "w" && text2 == "n" && text3 == "left" && angle == 270)
		{
			text3 = "right";
		}
		return CodeWithVariants(new string[2] { "type", "knobOrientation" }, new string[2] { text2, text3 });
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
	}
}
