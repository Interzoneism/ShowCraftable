using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockLogSection : Block
{
	private int barkFace1;

	private int barkFace2 = 1;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		string text = Variant["rotation"];
		string text2 = ((text != null) ? string.Intern(text) : null);
		string text3 = Variant["segment"];
		string text4 = ((text3 != null) ? string.Intern(text3) : null);
		if (text4 == null || text4.Length != 2)
		{
			return;
		}
		switch (text2)
		{
		case "ud":
			barkFace1 = BlockFacing.FromFirstLetter(text4[0])?.Index ?? 0;
			barkFace2 = BlockFacing.FromFirstLetter(text4[1])?.Index ?? 1;
			alternatingVOffset = true;
			alternatingVOffsetFaces = 48;
			break;
		case "we":
			switch (text4)
			{
			case "ne":
				barkFace1 = 4;
				barkFace2 = 0;
				break;
			case "se":
				barkFace1 = 5;
				barkFace2 = 0;
				break;
			case "nw":
				barkFace1 = 4;
				barkFace2 = 2;
				break;
			case "sw":
				barkFace1 = 5;
				barkFace2 = 2;
				break;
			}
			alternatingVOffset = true;
			alternatingVOffsetFaces = 10;
			break;
		case "ns":
			switch (text4)
			{
			case "ne":
				barkFace1 = 4;
				barkFace2 = 1;
				break;
			case "se":
				barkFace1 = 5;
				barkFace2 = 1;
				break;
			case "nw":
				barkFace1 = 4;
				barkFace2 = 3;
				break;
			case "sw":
				barkFace1 = 5;
				barkFace2 = 3;
				break;
			}
			alternatingVOffset = true;
			alternatingVOffsetFaces = 5;
			break;
		}
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		BlockLogSection[] array = new BlockLogSection[6];
		string text = GetBlocksAround(array, world.BlockAccessor, blockSel.Position.Copy());
		if (text == null || (byPlayer != null && byPlayer.Entity.Controls.ShiftKey))
		{
			switch (blockSel.Face.Axis)
			{
			case EnumAxis.X:
				text = "we";
				break;
			case EnumAxis.Y:
				text = "ud";
				break;
			case EnumAxis.Z:
				text = "ns";
				break;
			}
		}
		SelectSimilarBlocksAround(array, text);
		int num = 0;
		int num2 = 1;
		int num3 = 2;
		int num4 = 3;
		int num5 = 4;
		int num6 = 5;
		if (text == "we")
		{
			num = 4;
			num2 = 0;
			num3 = 5;
			num4 = 2;
			num5 = 3;
			num6 = 1;
		}
		else if (text == "ns")
		{
			num = 4;
			num2 = 1;
			num3 = 5;
			num4 = 3;
			num5 = 0;
			num6 = 2;
		}
		string text2 = null;
		if (array[num] != null && array[num3] == null)
		{
			text2 = "s" + BlockFacing.FromFirstLetter(array[num].LastCodePart(1)[1]).Code.ToLowerInvariant()[0];
		}
		if (array[num3] != null && array[num] == null)
		{
			text2 = "n" + BlockFacing.FromFirstLetter(array[num3].LastCodePart(1)[1]).Code.ToLowerInvariant()[0];
		}
		if (array[num2] != null && array[num4] == null)
		{
			text2 = BlockFacing.FromFirstLetter(array[num2].LastCodePart(1)[0]).Code.ToLowerInvariant()[0] + "w";
		}
		if (array[num4] != null && array[num2] == null)
		{
			text2 = BlockFacing.FromFirstLetter(array[num4].LastCodePart(1)[0]).Code.ToLowerInvariant()[0] + "e";
		}
		if (text2 == null)
		{
			text2 = array[num6]?.LastCodePart(1) ?? array[num5]?.LastCodePart(1) ?? "ne";
		}
		Block block = world.BlockAccessor.GetBlock(CodeWithParts(text2, text));
		if (block == null)
		{
			block = world.BlockAccessor.GetBlock(CodeWithParts(text));
		}
		if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			block.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			return true;
		}
		return false;
	}

	private void SelectSimilarBlocksAround(BlockLogSection[] around, string rotation)
	{
		int index = BlockFacing.FromFirstLetter(rotation[0]).Index;
		int index2 = BlockFacing.FromFirstLetter(rotation[1]).Index;
		for (int i = 0; i < 6; i++)
		{
			BlockLogSection blockLogSection = around[i];
			if (blockLogSection != null)
			{
				if (blockLogSection.LastCodePart() != rotation)
				{
					around[i] = null;
				}
				else if (i != index && i != index2 && !blockLogSection.IsBarkFace(i))
				{
					around[i] = null;
				}
			}
		}
	}

	private bool IsBarkFace(int i)
	{
		if (barkFace1 != i)
		{
			return barkFace2 == i;
		}
		return true;
	}

	private string GetBlocksAround(BlockLogSection[] around, IBlockAccessor blockAccessor, BlockPos pos)
	{
		string[] array = new string[3];
		int[] array2 = new int[3];
		if (blockAccessor.GetBlock(pos.North()) is BlockLogSection blockLogSection)
		{
			around[0] = blockLogSection;
			UpdateAny(blockLogSection.LastCodePart(), array, array2);
		}
		if (blockAccessor.GetBlock(pos.South().East()) is BlockLogSection blockLogSection2)
		{
			around[1] = blockLogSection2;
			UpdateAny(blockLogSection2.LastCodePart(), array, array2);
		}
		if (blockAccessor.GetBlock(pos.South().West()) is BlockLogSection blockLogSection3)
		{
			around[2] = blockLogSection3;
			UpdateAny(blockLogSection3.LastCodePart(), array, array2);
		}
		if (blockAccessor.GetBlock(pos.North().West()) is BlockLogSection blockLogSection4)
		{
			around[3] = blockLogSection4;
			UpdateAny(blockLogSection4.LastCodePart(), array, array2);
		}
		if (blockAccessor.GetBlock(pos.East().Up()) is BlockLogSection blockLogSection5)
		{
			around[4] = blockLogSection5;
			UpdateAny(blockLogSection5.LastCodePart(), array, array2);
		}
		if (blockAccessor.GetBlock(pos.Down().Down()) is BlockLogSection blockLogSection6)
		{
			around[5] = blockLogSection6;
			UpdateAny(blockLogSection6.LastCodePart(), array, array2);
		}
		if (array2[1] > array2[0])
		{
			array[0] = array[1];
			array2[0] = array2[1];
		}
		if (array2[2] <= array2[0])
		{
			return array[0];
		}
		return array[2];
	}

	private void UpdateAny(string part, string[] any, int[] anyCount)
	{
		for (int i = 0; i < 3; i++)
		{
			if (any[i] == null || any[i] == part)
			{
				any[i] = part;
				anyCount[i]++;
				break;
			}
		}
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.GetBlock(CodeWithParts("ne", "ud")));
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		if (LastCodePart() == "ud")
		{
			return Code;
		}
		string[] array = new string[2] { "ns", "we" };
		int num = GameMath.Mod(angle / 90, 4);
		if (LastCodePart() == "we")
		{
			num++;
		}
		return CodeWithParts(array[num % 2]);
	}
}
