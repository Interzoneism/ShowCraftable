using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class BlockStalagSection : Block
{
	public string[] Thicknesses = new string[6] { "14", "12", "10", "08", "06", "04" };

	public int ThicknessInt;

	public Dictionary<string, int> thicknessIndex = new Dictionary<string, int>
	{
		{ "14", 0 },
		{ "12", 1 },
		{ "10", 2 },
		{ "08", 3 },
		{ "06", 4 },
		{ "04", 5 }
	};

	public string Thickness => Variant["thickness"];

	public override void OnLoaded(ICoreAPI api)
	{
		CanStep = false;
		ThicknessInt = int.Parse(Variant["thickness"]);
		base.OnLoaded(api);
	}

	public Block GetBlock(IWorldAccessor world, string rocktype, string thickness)
	{
		return world.GetBlock(CodeWithParts(rocktype, thickness));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (IsSurroundedByNonSolid(world, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	public bool IsSurroundedByNonSolid(IWorldAccessor world, BlockPos pos)
	{
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing in aLLFACES)
		{
			BlockPos pos2 = pos.AddCopy(blockFacing.Normali);
			Block block = world.BlockAccessor.GetBlock(pos2);
			if (block.SideSolid[blockFacing.Opposite.Index] || block is BlockStalagSection)
			{
				return false;
			}
		}
		return true;
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		bool flag = false;
		if (blockAccessor.GetBlock(pos).Replaceable < 6000)
		{
			return false;
		}
		pos = pos.Copy();
		ModStdWorldGen modStdWorldGen = null;
		if (blockAccessor is IWorldGenBlockAccessor worldGenBlockAccessor)
		{
			modStdWorldGen = worldGenBlockAccessor.WorldgenWorldAccessor.Api.ModLoader.GetModSystem<GenVegetationAndPatches>();
		}
		for (int i = 0; i < 5 + worldGenRand.NextInt(25); i++)
		{
			if (pos.Y >= 15 && (modStdWorldGen == null || modStdWorldGen.GetIntersectingStructure(pos, ModStdWorldGen.SkipStalagHashCode) == null))
			{
				flag |= TryGenStalag(blockAccessor, pos, worldGenRand.NextInt(4), worldGenRand);
				pos.X += worldGenRand.NextInt(9) - 4;
				pos.Y += worldGenRand.NextInt(3) - 1;
				pos.Z += worldGenRand.NextInt(9) - 4;
			}
		}
		return flag;
	}

	private bool TryGenStalag(IBlockAccessor blockAccessor, BlockPos pos, int thickOff, IRandom worldGenRand)
	{
		bool flag = false;
		for (int i = 0; i < 5; i++)
		{
			Block blockAbove = blockAccessor.GetBlockAbove(pos, i, 1);
			if (blockAbove.SideSolid[BlockFacing.DOWN.Index] && blockAbove.BlockMaterial == EnumBlockMaterial.Stone)
			{
				if (blockAbove.Variant.TryGetValue("rock", out var value))
				{
					GrowDownFrom(blockAccessor, pos.AddCopy(0, i - 1, 0), value, thickOff, worldGenRand);
					flag = true;
				}
				break;
			}
			if (blockAbove.Id != 0)
			{
				break;
			}
		}
		if (!flag)
		{
			return false;
		}
		for (int j = 0; j < 12; j++)
		{
			Block blockBelow = blockAccessor.GetBlockBelow(pos, j, 1);
			if (blockBelow.SideSolid[BlockFacing.UP.Index] && blockBelow.BlockMaterial == EnumBlockMaterial.Stone)
			{
				if (blockBelow.Variant.TryGetValue("rock", out var value2))
				{
					GrowUpFrom(blockAccessor, pos.AddCopy(0, -j + 1, 0), value2, thickOff);
					flag = true;
				}
				break;
			}
			if (blockBelow.Id != 0 && !(blockBelow is BlockStalagSection))
			{
				break;
			}
		}
		return flag;
	}

	private void GrowUpFrom(IBlockAccessor blockAccessor, BlockPos pos, string rocktype, int thickOff)
	{
		for (int i = thicknessIndex[Thickness] + thickOff; i < Thicknesses.Length; i++)
		{
			BlockStalagSection blockStalagSection = (BlockStalagSection)GetBlock(api.World, rocktype, Thicknesses[i]);
			if (blockStalagSection != null)
			{
				Block block = blockAccessor.GetBlock(pos);
				if (block.Replaceable < 6000 && !((block as BlockStalagSection)?.ThicknessInt < blockStalagSection.ThicknessInt))
				{
					break;
				}
				blockAccessor.SetBlock(blockStalagSection.BlockId, pos);
				pos.Y++;
			}
		}
	}

	private void GrowDownFrom(IBlockAccessor blockAccessor, BlockPos pos, string rocktype, int thickOff, IRandom worldGenRand)
	{
		for (int i = thicknessIndex[Thickness] + thickOff + worldGenRand.NextInt(2); i < Thicknesses.Length; i++)
		{
			Block block = GetBlock(api.World, rocktype, Thicknesses[i]);
			if (block != null)
			{
				if (blockAccessor.GetBlock(pos).Replaceable < 6000)
				{
					break;
				}
				blockAccessor.SetBlock(block.BlockId, pos);
				pos.Y--;
			}
		}
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		return Lang.Get("block-speleothem", Lang.Get("rock-" + Variant["rock"]));
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		return Lang.Get("block-speleothem", Lang.Get("rock-" + Variant["rock"]));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine();
		dsc.AppendLine(Lang.Get("rock-" + Variant["rock"]));
	}
}
