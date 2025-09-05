using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BlockBehaviorHorizontalAttachable : BlockBehavior
{
	[DocumentAsJson("Optional", "False", false)]
	private bool handleDrops = true;

	[DocumentAsJson("Optional", "north", false)]
	private string dropBlockFace = "north";

	[DocumentAsJson("Optional", "None", false)]
	private string dropBlock;

	[DocumentAsJson("Optional", "None", false)]
	private Dictionary<string, Cuboidi> attachmentAreas;

	public BlockBehaviorHorizontalAttachable(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		handleDrops = properties["handleDrops"].AsBool(defaultValue: true);
		if (properties["dropBlockFace"].Exists)
		{
			dropBlockFace = properties["dropBlockFace"].AsString();
		}
		if (properties["dropBlock"].Exists)
		{
			dropBlock = properties["dropBlock"].AsString();
		}
		Dictionary<string, RotatableCube> dictionary = properties["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>();
		attachmentAreas = new Dictionary<string, Cuboidi>();
		if (dictionary != null)
		{
			foreach (KeyValuePair<string, RotatableCube> item in dictionary)
			{
				item.Value.Origin.Set(8.0, 8.0, 8.0);
				attachmentAreas[item.Key] = item.Value.RotatedCopy().ConvertToCuboidi();
			}
			return;
		}
		attachmentAreas["up"] = properties["attachmentArea"].AsObject<Cuboidi>();
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		if (blockSel.Face.IsHorizontal && TryAttachTo(world, byPlayer, blockSel, itemstack, ref failureCode))
		{
			return true;
		}
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		blockSel = blockSel.Clone();
		for (int i = 0; i < hORIZONTALS.Length; i++)
		{
			blockSel.Face = hORIZONTALS[i];
			if (TryAttachTo(world, byPlayer, blockSel, itemstack, ref failureCode))
			{
				return true;
			}
		}
		failureCode = "requirehorizontalattachable";
		return false;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
	{
		if (handleDrops)
		{
			handled = EnumHandling.PreventDefault;
			if (dropBlock == null)
			{
				return new ItemStack[1]
				{
					new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithParts(dropBlockFace)))
				};
			}
			return new ItemStack[1]
			{
				new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation(dropBlock)))
			};
		}
		handled = EnumHandling.PassThrough;
		return null;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (dropBlock != null)
		{
			return new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation(dropBlock)));
		}
		return new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithParts(dropBlockFace)));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (!CanBlockStay(world, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	private bool TryAttachTo(IWorldAccessor world, IPlayer player, BlockSelection blockSel, ItemStack itemstack, ref string failureCode)
	{
		BlockFacing opposite = blockSel.Face.Opposite;
		BlockPos pos = blockSel.Position.AddCopy(opposite);
		Block obj = world.BlockAccessor.GetBlock(pos);
		Block block = world.BlockAccessor.GetBlock(base.block.CodeWithParts(opposite.Code));
		Cuboidi value = null;
		attachmentAreas?.TryGetValue(opposite.Code, out value);
		if (obj.CanAttachBlockAt(world.BlockAccessor, base.block, pos, blockSel.Face, value) && block.CanPlaceBlock(world, player, blockSel, ref failureCode))
		{
			block.DoPlaceBlock(world, player, blockSel, itemstack);
			return true;
		}
		return false;
	}

	private bool CanBlockStay(IWorldAccessor world, BlockPos pos)
	{
		BlockFacing blockFacing = BlockFacing.FromCode(block.Code.Path.Split('-')[^1]);
		Block obj = world.BlockAccessor.GetBlock(pos.AddCopy(blockFacing));
		Cuboidi value = null;
		attachmentAreas?.TryGetValue(blockFacing.Code, out value);
		return obj.CanAttachBlockAt(world.BlockAccessor, block, pos.AddCopy(blockFacing), blockFacing.Opposite, value);
	}

	public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handled, Cuboidi attachmentArea = null)
	{
		handled = EnumHandling.PreventDefault;
		return false;
	}

	public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		int num = GameMath.Mod(BlockFacing.FromCode(block.LastCodePart()).HorizontalAngleIndex - angle / 90, 4);
		BlockFacing blockFacing = BlockFacing.HORIZONTALS_ANGLEORDER[num];
		return block.CodeWithParts(blockFacing.Code);
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		BlockFacing blockFacing = BlockFacing.FromCode(block.LastCodePart());
		if (blockFacing.Axis == axis)
		{
			return block.CodeWithParts(blockFacing.Opposite.Code);
		}
		return block.Code;
	}
}
