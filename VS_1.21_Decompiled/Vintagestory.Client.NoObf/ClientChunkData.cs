using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

internal class ClientChunkData : ChunkData
{
	private int[][] light2;

	private FastRWLock light2Lock;

	private int blocksArrayCount;

	private Block blockAir;

	private System.Func<int, Block> GetBlockAsBlock;

	private ClientChunkData(ChunkDataPool chunkDataPool)
		: base(chunkDataPool)
	{
		GetBlockAsBlock = getBlockAir;
		light2Lock = new FastRWLock(chunkDataPool);
	}

	public new static ClientChunkData CreateNew(int chunksize, ChunkDataPool chunkDataPool)
	{
		return new ClientChunkData(chunkDataPool);
	}

	public void BuildFastBlockAccessArray(Block[] blocks)
	{
		int paletteCount;
		if (blocksLayer != null && (paletteCount = blocksLayer.paletteCount) > 0)
		{
			int[] palette = blocksLayer.palette;
			GetBlockAsBlock = blocksLayer.SelectDelegateBlockClient(getBlockAir);
			if (BlockChunkDataLayer.blocksByPaletteIndex == null || BlockChunkDataLayer.blocksByPaletteIndex.Length < paletteCount)
			{
				BlockChunkDataLayer.blocksByPaletteIndex = new Block[paletteCount];
			}
			for (int i = 0; i < paletteCount; i++)
			{
				BlockChunkDataLayer.blocksByPaletteIndex[i] = blocks[palette[i]];
			}
			blocksArrayCount = paletteCount;
		}
		else
		{
			GetBlockAsBlock = getBlockAir;
		}
		blockAir = blocks[0];
	}

	protected Block getBlockAir(int index3d)
	{
		return blockAir;
	}

	public int GetOne(out ushort lightOut, out int lightSatOut, out int fluidBlockId, int index3d)
	{
		light2Lock.AcquireReadLock();
		uint num = ((light2 != null) ? Light2(index3d) : Light(index3d));
		light2Lock.ReleaseReadLock();
		lightOut = (ushort)num;
		lightSatOut = (int)((num >> 16) & 7);
		fluidBlockId = GetFluid(index3d);
		return GetSolidBlock(index3d);
	}

	public void GetRange(Block[] currentChunkBlocksExt, Block[] currentChunkFluidsExt, int[] currentChunkRgbsExt, int extIndex3d, int index3d, int index3dEnd, Block[] blocksFast, ColorUtil.LightUtil lightConverter)
	{
		BlockChunkDataLayer blockChunkDataLayer = blocksLayer;
		if (blockChunkDataLayer == null)
		{
			blockAir = blocksFast[0];
			light2Lock.AcquireReadLock();
			try
			{
				do
				{
					uint num = ((light2 != null) ? Light2(index3d) : Light(index3d));
					currentChunkBlocksExt[++extIndex3d] = blockAir;
					currentChunkRgbsExt[extIndex3d] = lightConverter.ToRgba((ushort)num, (int)((num >> 16) & 7));
					int fluid = GetFluid(index3d);
					currentChunkFluidsExt[extIndex3d] = blocksFast[fluid];
				}
				while (++index3d < index3dEnd);
				return;
			}
			finally
			{
				light2Lock.ReleaseReadLock();
			}
		}
		blockChunkDataLayer.readWriteLock.AcquireReadLock();
		light2Lock.AcquireReadLock();
		try
		{
			do
			{
				uint num2 = ((light2 != null) ? Light2(index3d) : Light(index3d));
				int num3 = blockChunkDataLayer.GetUnsafe(index3d);
				currentChunkBlocksExt[++extIndex3d] = blocksFast[num3];
				currentChunkRgbsExt[extIndex3d] = lightConverter.ToRgba((ushort)num2, (int)((num2 >> 16) & 7));
				num3 = GetFluid(index3d);
				currentChunkFluidsExt[extIndex3d] = blocksFast[num3];
			}
			while (++index3d < index3dEnd);
		}
		finally
		{
			light2Lock.ReleaseReadLock();
			blockChunkDataLayer.readWriteLock.ReleaseReadLock();
		}
	}

	public void GetRange_Faster(Block[] currentChunkBlocksExt, Block[] currentChunkFluidsExt, int[] currentChunkRgbsExt, int extIndex3d, int index3d, int index3dEnd, Block[] blocksFast, ColorUtil.LightUtil lightConverter)
	{
		BlockChunkDataLayer blockChunkDataLayer = blocksLayer;
		if (blockChunkDataLayer == null)
		{
			light2Lock.AcquireReadLock();
			try
			{
				do
				{
					currentChunkBlocksExt[++extIndex3d] = blockAir;
					uint num = ((light2 != null) ? Light2(index3d) : Light(index3d));
					currentChunkRgbsExt[extIndex3d] = lightConverter.ToRgba((ushort)num, (int)((num >> 16) & 7));
					int fluid = GetFluid(index3d);
					currentChunkFluidsExt[extIndex3d] = blocksFast[fluid];
				}
				while (++index3d < index3dEnd);
				return;
			}
			finally
			{
				light2Lock.ReleaseReadLock();
			}
		}
		if (blockChunkDataLayer.paletteCount == blocksArrayCount)
		{
			blockChunkDataLayer.readWriteLock.AcquireReadLock();
			light2Lock.AcquireReadLock();
			try
			{
				do
				{
					currentChunkBlocksExt[++extIndex3d] = GetBlockAsBlock(index3d);
					uint num2 = ((light2 != null) ? Light2(index3d) : Light(index3d));
					currentChunkRgbsExt[extIndex3d] = lightConverter.ToRgba((ushort)num2, (int)((num2 >> 16) & 7));
					int fluid2 = GetFluid(index3d);
					currentChunkFluidsExt[extIndex3d] = blocksFast[fluid2];
				}
				while (++index3d < index3dEnd);
				return;
			}
			finally
			{
				light2Lock.ReleaseReadLock();
				blockChunkDataLayer.readWriteLock.ReleaseReadLock();
			}
		}
		GetRange(currentChunkBlocksExt, currentChunkFluidsExt, currentChunkRgbsExt, extIndex3d, index3d, index3dEnd, blocksFast, lightConverter);
	}

	internal override void EmptyAndReuseArrays(List<int[]> datas)
	{
		GetBlockAsBlock = getBlockAir;
		base.EmptyAndReuseArrays(datas);
		int[][] array = light2;
		if (array == null)
		{
			return;
		}
		light2Lock.AcquireWriteLock();
		light2 = null;
		for (int i = 0; i < array.Length; i++)
		{
			int[] array2 = array[i];
			if (array2 != null)
			{
				datas?.Add(array2);
				array[i] = null;
			}
		}
		light2Lock.ReleaseWriteLock();
	}

	public override void SetSunlight_Buffered(int index3d, int sunLevel)
	{
		if (lightLayer == null)
		{
			lightLayer = new ChunkDataLayer(pool);
			lightLayer.Set(index3d, sunLevel);
			return;
		}
		if (light2 == null)
		{
			StartDoubleBuffering();
		}
		lightLayer.Set(index3d, (lightLayer.Get(index3d) & -32) | sunLevel);
	}

	public override void SetBlocklight_Buffered(int index3d, int lightLevel)
	{
		if (lightLayer == null)
		{
			lightLayer = new ChunkDataLayer(pool);
			lightLayer.Set(index3d, lightLevel);
			return;
		}
		if (light2 == null)
		{
			StartDoubleBuffering();
		}
		lightLayer.Set(index3d, (lightLayer.Get(index3d) & 0x1F) | lightLevel);
	}

	public uint Light2(int index3d)
	{
		int[] array = lightLayer?.palette;
		if (index3d < 0 || array == null)
		{
			return 0u;
		}
		int[][] array2 = light2;
		int num = index3d % 32;
		index3d = index3d / 32 % 1024;
		int num2 = 0;
		int num3 = 1;
		for (int i = 0; i < array2.Length; i++)
		{
			num2 += ((array2[i][index3d] >> num) & 1) * num3;
			num3 *= 2;
		}
		return (uint)array[num2];
	}

	private void StartDoubleBuffering()
	{
		light2 = lightLayer.CopyData();
	}

	public void FinishLightDoubleBuffering()
	{
		int[][] array = light2;
		if (array == null)
		{
			return;
		}
		light2Lock.AcquireWriteLock();
		light2 = null;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != null)
			{
				pool.Return(array[i]);
				array[i] = null;
			}
		}
		light2Lock.ReleaseWriteLock();
	}
}
