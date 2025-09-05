using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class MultiblockStructure
{
	public static int HighlightSlotId = 23;

	public Dictionary<AssetLocation, int> BlockNumbers = new Dictionary<AssetLocation, int>();

	public List<BlockOffsetAndNumber> Offsets = new List<BlockOffsetAndNumber>();

	public string OffsetsOrientation;

	private Dictionary<int, AssetLocation> BlockCodes;

	private List<BlockOffsetAndNumber> TransformedOffsets;

	public int GetOrCreateBlockNumber(Block block)
	{
		if (!BlockNumbers.TryGetValue(block.Code, out var value))
		{
			return BlockNumbers[block.Code] = 1 + BlockNumbers.Count;
		}
		return value;
	}

	public void InitForUse(float rotateYDeg)
	{
		Matrixf matrixf = new Matrixf();
		matrixf.RotateYDeg(rotateYDeg);
		BlockCodes = new Dictionary<int, AssetLocation>();
		TransformedOffsets = new List<BlockOffsetAndNumber>();
		foreach (KeyValuePair<AssetLocation, int> blockNumber in BlockNumbers)
		{
			BlockCodes[blockNumber.Value] = blockNumber.Key;
		}
		for (int i = 0; i < Offsets.Count; i++)
		{
			Vec4i vec4i = Offsets[i];
			Vec4f vec = new Vec4f(vec4i.X, vec4i.Y, vec4i.Z, 0f);
			Vec4f vec4f = matrixf.TransformVector(vec);
			TransformedOffsets.Add(new BlockOffsetAndNumber
			{
				X = (int)Math.Round(vec4f.X),
				Y = (int)Math.Round(vec4f.Y),
				Z = (int)Math.Round(vec4f.Z),
				W = vec4i.W
			});
		}
	}

	public void WalkMatchingBlocks(IWorldAccessor world, BlockPos centerPos, Action<Block, BlockPos> onBlock)
	{
		if (TransformedOffsets == null)
		{
			throw new InvalidOperationException("call InitForUse() first");
		}
		BlockPos blockPos = new BlockPos();
		for (int i = 0; i < TransformedOffsets.Count; i++)
		{
			Vec4i vec4i = TransformedOffsets[i];
			blockPos.Set(centerPos.X + vec4i.X, centerPos.Y + vec4i.Y, centerPos.Z + vec4i.Z);
			Block block = world.BlockAccessor.GetBlock(blockPos);
			if (WildcardUtil.Match(BlockCodes[vec4i.W], block.Code))
			{
				onBlock?.Invoke(block, blockPos);
			}
		}
	}

	public int InCompleteBlockCount(IWorldAccessor world, BlockPos centerPos, PositionMismatchDelegate onMismatch = null)
	{
		if (TransformedOffsets == null)
		{
			throw new InvalidOperationException("call InitForUse() first");
		}
		int num = 0;
		for (int i = 0; i < TransformedOffsets.Count; i++)
		{
			Vec4i vec4i = TransformedOffsets[i];
			Block blockRaw = world.BlockAccessor.GetBlockRaw(centerPos.X + vec4i.X, centerPos.InternalY + vec4i.Y, centerPos.Z + vec4i.Z);
			if (!WildcardUtil.Match(BlockCodes[vec4i.W], blockRaw.Code))
			{
				onMismatch?.Invoke(blockRaw, BlockCodes[vec4i.W]);
				num++;
			}
		}
		return num;
	}

	public void ClearHighlights(IWorldAccessor world, IPlayer player)
	{
		world.HighlightBlocks(player, HighlightSlotId, new List<BlockPos>(), new List<int>());
	}

	public void HighlightIncompleteParts(IWorldAccessor world, IPlayer player, BlockPos centerPos)
	{
		List<BlockPos> list = new List<BlockPos>();
		List<int> list2 = new List<int>();
		for (int i = 0; i < TransformedOffsets.Count; i++)
		{
			Vec4i vec4i = TransformedOffsets[i];
			Block blockRaw = world.BlockAccessor.GetBlockRaw(centerPos.X + vec4i.X, centerPos.InternalY + vec4i.Y, centerPos.Z + vec4i.Z);
			AssetLocation wildcard = BlockCodes[vec4i.W];
			if (!WildcardUtil.Match(BlockCodes[vec4i.W], blockRaw.Code))
			{
				list.Add(new BlockPos(vec4i.X, vec4i.Y, vec4i.Z).Add(centerPos));
				if (blockRaw.Id != 0)
				{
					list2.Add(ColorUtil.ColorFromRgba(215, 94, 94, 64));
					continue;
				}
				int color = world.SearchBlocks(wildcard)[0].GetColor(world.Api as ICoreClientAPI, centerPos);
				color &= 0xFFFFFF;
				color |= 0x60000000;
				list2.Add(color);
			}
		}
		world.HighlightBlocks(player, HighlightSlotId, list, list2);
	}
}
