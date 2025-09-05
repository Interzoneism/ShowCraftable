using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Datastructures;

public class CachedCuboidList : IEnumerable<Cuboidd>, IEnumerable
{
	public Cuboidd[] cuboids = Array.Empty<Cuboidd>();

	public BlockPos[] positions;

	public Block[] blocks;

	public int Count;

	private int populatedSize;

	public void Clear()
	{
		Count = 0;
	}

	public void Add(Cuboidf[] cuboids, int x, int y, int z, Block block = null)
	{
		for (int i = 0; i < cuboids.Length; i++)
		{
			Add(cuboids[i], x, y, z, block);
		}
	}

	public void Add(Cuboidf cuboid, int x, int y, int z, Block block = null)
	{
		if (cuboid == null)
		{
			return;
		}
		if (Count >= populatedSize)
		{
			if (Count >= cuboids.Length)
			{
				ExpandArrays();
			}
			cuboids[Count] = cuboid.OffsetCopyDouble(x, y % 32768, z);
			positions[Count] = new BlockPos(x, y, z);
			blocks[Count] = block;
			populatedSize++;
		}
		else
		{
			cuboids[Count].Set(cuboid.X1 + (float)x, cuboid.Y1 + (float)(y % 32768), cuboid.Z1 + (float)z, cuboid.X2 + (float)x, cuboid.Y2 + (float)(y % 32768), cuboid.Z2 + (float)z);
			positions[Count].Set(x, y, z);
			blocks[Count] = block;
		}
		Count++;
	}

	public IEnumerator<Cuboidd> GetEnumerator()
	{
		for (int i = 0; i < Count; i++)
		{
			yield return cuboids[i];
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private void ExpandArrays()
	{
		int num = ((populatedSize == 0) ? 16 : (populatedSize * 3 / 2));
		Cuboidd[] array = new Cuboidd[num];
		BlockPos[] array2 = new BlockPos[num];
		Block[] array3 = new Block[num];
		for (int i = 0; i < populatedSize; i++)
		{
			array[i] = cuboids[i];
			array2[i] = positions[i];
			array3[i] = blocks[i];
		}
		cuboids = array;
		positions = array2;
		blocks = array3;
	}
}
