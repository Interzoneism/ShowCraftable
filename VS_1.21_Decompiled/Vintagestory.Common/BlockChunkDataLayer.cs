using System;
using Vintagestory.API.Common;

namespace Vintagestory.Common;

public class BlockChunkDataLayer : ChunkDataLayer
{
	public static Block[] blocksByPaletteIndex;

	public BlockChunkDataLayer(ChunkDataPool chunkDataPool)
		: base(chunkDataPool)
	{
	}

	internal void UpdateToFluidsLayer(BlockChunkDataLayer fluidsLayer)
	{
		GameMain game = pool.Game;
		for (int i = 1; i < paletteCount; i++)
		{
			Block block = game.Blocks[palette[i]];
			if (block.ForFluidsLayer)
			{
				MoveToOtherLayer(i, palette[i], fluidsLayer);
				DeleteFromPalette(i);
				i--;
			}
			else if (block.RemapToLiquidsLayer != null)
			{
				Block block2 = game.GetBlock(new AssetLocation(block.RemapToLiquidsLayer));
				if (block2 != null)
				{
					AddToOtherLayer(i, block2.BlockId, fluidsLayer);
				}
			}
		}
	}

	internal void MoveToOtherLayer(int search, int fluidBlockId, BlockChunkDataLayer fluidsLayer)
	{
		int paletteIndex = fluidsLayer.GetPaletteIndex(fluidBlockId);
		readWriteLock.AcquireWriteLock();
		int num = bitsize;
		for (int i = 0; i < 32768; i += 32)
		{
			int num2 = i / 32;
			int num3 = -1;
			for (int j = 0; j < num; j++)
			{
				int num4 = dataBits[j][num2];
				num3 &= ((((search >> j) & 1) == 1) ? num4 : (~num4));
			}
			if (num3 != 0)
			{
				fluidsLayer.Write(paletteIndex, num2, num3);
				int num5 = ~num3;
				for (int k = 0; k < num; k++)
				{
					dataBits[k][num2] &= num5;
				}
			}
		}
		readWriteLock.ReleaseWriteLock();
	}

	internal void AddToOtherLayer(int search, int fluidBlockId, BlockChunkDataLayer fluidsLayer)
	{
		int paletteIndex = fluidsLayer.GetPaletteIndex(fluidBlockId);
		readWriteLock.AcquireReadLock();
		int num = bitsize;
		for (int i = 0; i < 32768; i += 32)
		{
			int num2 = i / 32;
			int num3 = -1;
			for (int j = 0; j < num; j++)
			{
				int num4 = dataBits[j][num2];
				num3 &= ((((search >> j) & 1) == 1) ? num4 : (~num4));
			}
			if (num3 != 0)
			{
				fluidsLayer.Write(paletteIndex, num2, num3);
			}
		}
		readWriteLock.ReleaseReadLock();
	}

	private int GetPaletteIndex(int value)
	{
		int num;
		if (palette != null)
		{
			num = 0;
			while (true)
			{
				if (num < paletteCount)
				{
					if (palette[num] == value)
					{
						break;
					}
					num++;
					continue;
				}
				lock (palette)
				{
					if (num == palette.Length)
					{
						num = MakeSpaceInPalette();
					}
					palette[num] = value;
					paletteCount++;
				}
				break;
			}
		}
		else
		{
			if (value == 0)
			{
				return 0;
			}
			NewDataBitsWithFirstValue(value);
			num = 1;
		}
		return num;
	}

	private Block getBlockOne(int index3d)
	{
		int num = index3d % 32;
		index3d /= 32;
		return blocksByPaletteIndex[(dataBit0[index3d] >> num) & 1];
	}

	private Block getBlockTwo(int index3d)
	{
		int num = index3d % 32;
		index3d /= 32;
		return blocksByPaletteIndex[((dataBit0[index3d] >> num) & 1) + 2 * ((dataBit1[index3d] >> num) & 1)];
	}

	private Block getBlockThree(int index3d)
	{
		int num = index3d % 32;
		index3d /= 32;
		return blocksByPaletteIndex[((dataBit0[index3d] >> num) & 1) + 2 * ((dataBit1[index3d] >> num) & 1) + 4 * ((dataBit2[index3d] >> num) & 1)];
	}

	private Block getBlockFour(int index3d)
	{
		int num = index3d % 32;
		index3d /= 32;
		return blocksByPaletteIndex[((dataBit0[index3d] >> num) & 1) + 2 * ((dataBit1[index3d] >> num) & 1) + 4 * ((dataBit2[index3d] >> num) & 1) + 8 * ((dataBit3[index3d] >> num) & 1)];
	}

	private Block getBlockFive(int index3d)
	{
		int num = index3d % 32;
		index3d /= 32;
		return blocksByPaletteIndex[((dataBit0[index3d] >> num) & 1) + 2 * ((dataBit1[index3d] >> num) & 1) + 4 * ((dataBit2[index3d] >> num) & 1) + 8 * ((dataBit3[index3d] >> num) & 1) + 16 * ((dataBits[4][index3d] >> num) & 1)];
	}

	private Block getBlockGeneralCase(int index3d)
	{
		int num = index3d % 32;
		index3d /= 32;
		int num2 = 1;
		int num3 = 0;
		for (int i = 0; i < bitsize; i++)
		{
			num3 += ((dataBits[i][index3d] >> num) & 1) * num2;
			num2 *= 2;
		}
		return blocksByPaletteIndex[num3];
	}

	public System.Func<int, Block> SelectDelegateBlockClient(System.Func<int, Block> getBlockAir)
	{
		return bitsize switch
		{
			0 => getBlockAir, 
			1 => getBlockOne, 
			2 => getBlockTwo, 
			3 => getBlockThree, 
			4 => getBlockFour, 
			5 => getBlockFive, 
			_ => getBlockGeneralCase, 
		};
	}

	public static void Dispose()
	{
		blocksByPaletteIndex = null;
	}
}
