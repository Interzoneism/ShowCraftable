using System;

namespace Vintagestory.API.Client;

public abstract class CustomMeshDataPart<T>
{
	private bool customAllocationSize;

	private int allocationSize;

	public T[] Values;

	public int Count;

	public int[] InterleaveSizes;

	public int InterleaveStride;

	public int[] InterleaveOffsets;

	public bool Instanced;

	public bool StaticDraw = true;

	public int BaseOffset;

	public int BufferSize
	{
		get
		{
			if (Values != null)
			{
				return Values.Length;
			}
			return 0;
		}
	}

	public int AllocationSize
	{
		get
		{
			if (!customAllocationSize)
			{
				return Count;
			}
			return allocationSize;
		}
	}

	public CustomMeshDataPart()
	{
	}

	public CustomMeshDataPart(int arraySize)
	{
		Values = new T[arraySize];
	}

	public void GrowBuffer(int growAtLeastBy = 1)
	{
		if (Values == null)
		{
			Values = new T[Math.Max(growAtLeastBy, Count * 2)];
		}
		else
		{
			Array.Resize(ref Values, Math.Max(Values.Length + growAtLeastBy, Count * 2));
		}
	}

	public void Add(T value)
	{
		if (Count >= BufferSize)
		{
			GrowBuffer();
		}
		Values[Count++] = value;
	}

	public void Add4(T value)
	{
		int count = Count;
		if (count + 4 > BufferSize)
		{
			GrowBuffer();
		}
		T[] values = Values;
		values[count++] = value;
		values[count++] = value;
		values[count++] = value;
		values[count++] = value;
		Count = count;
	}

	public void Add(params T[] values)
	{
		if (Count + values.Length >= BufferSize)
		{
			GrowBuffer(values.Length);
		}
		for (int i = 0; i < values.Length; i++)
		{
			Values[Count++] = values[i];
		}
	}

	public void SetAllocationSize(int size)
	{
		customAllocationSize = true;
		allocationSize = size;
	}

	public void AutoAllocationSize()
	{
		customAllocationSize = false;
	}

	public void SetFrom(CustomMeshDataPart<T> meshdatapart)
	{
		customAllocationSize = meshdatapart.customAllocationSize;
		allocationSize = meshdatapart.allocationSize;
		Count = meshdatapart.Count;
		if (meshdatapart.Values != null)
		{
			Values = (T[])meshdatapart.Values.Clone();
		}
		if (meshdatapart.InterleaveSizes != null)
		{
			InterleaveSizes = (int[])meshdatapart.InterleaveSizes.Clone();
		}
		if (meshdatapart.InterleaveOffsets != null)
		{
			InterleaveOffsets = (int[])meshdatapart.InterleaveOffsets.Clone();
		}
		InterleaveStride = meshdatapart.InterleaveStride;
		Instanced = meshdatapart.Instanced;
		StaticDraw = meshdatapart.StaticDraw;
		BaseOffset = meshdatapart.BaseOffset;
	}

	protected CustomMeshDataPart<T> EmptyClone(CustomMeshDataPart<T> cloned)
	{
		cloned.customAllocationSize = customAllocationSize;
		cloned.allocationSize = allocationSize;
		cloned.Count = 0;
		if (Values != null)
		{
			cloned.GrowBuffer(Values.Length);
		}
		if (InterleaveSizes != null)
		{
			cloned.InterleaveSizes = (int[])InterleaveSizes.Clone();
		}
		if (InterleaveOffsets != null)
		{
			cloned.InterleaveOffsets = (int[])InterleaveOffsets.Clone();
		}
		cloned.InterleaveStride = InterleaveStride;
		cloned.Instanced = Instanced;
		cloned.StaticDraw = StaticDraw;
		cloned.BaseOffset = BaseOffset;
		return cloned;
	}
}
