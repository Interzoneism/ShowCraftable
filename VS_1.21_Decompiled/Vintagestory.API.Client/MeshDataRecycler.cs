using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Vintagestory.API.Client;

public class MeshDataRecycler
{
	public const int MinimumSizeForRecycling = 4096;

	public const int TTL = 15000;

	private const int smallLimit = 368;

	private const int mediumLimit = 3072;

	private const int Four = 4;

	private SortedList<float, MeshData> smallSizes = new SortedList<float, MeshData>();

	private SortedList<float, MeshData> mediumSizes = new SortedList<float, MeshData>();

	private SortedList<float, MeshData> largeSizes = new SortedList<float, MeshData>();

	private IClientWorldAccessor game;

	private bool disposed;

	private ConcurrentQueue<MeshData> forRecycling = new ConcurrentQueue<MeshData>();

	private FieldInfo keysAccessor;

	public MeshDataRecycler(IClientWorldAccessor clientMain)
	{
		game = clientMain;
		Type typeFromHandle = typeof(SortedList<float, MeshData>);
		keysAccessor = typeFromHandle.GetField("keys", BindingFlags.Instance | BindingFlags.NonPublic);
	}

	public MeshData GetOrCreateMesh(int minimumVertices)
	{
		minimumVertices = (minimumVertices + 4 - 1) / 4 * 4;
		MeshData meshData = (disposed ? null : GetRecycled(minimumVertices));
		if (meshData == null)
		{
			if (!disposed)
			{
				minimumVertices = (minimumVertices * 41 / 40 + 4 - 1) / 4 * 4;
			}
			meshData = new MeshData(minimumVertices);
		}
		else if (meshData.IndicesMax != meshData.VerticesMax * 6 / 4)
		{
			meshData.Indices = new int[meshData.VerticesMax * 6 / 4];
			meshData.IndicesMax = meshData.Indices.Length;
		}
		meshData.Recyclable = true;
		return meshData;
	}

	public void DoRecycling()
	{
		if (disposed)
		{
			forRecycling.Clear();
			smallSizes.Clear();
			mediumSizes.Clear();
			largeSizes.Clear();
		}
		if (forRecycling.IsEmpty)
		{
			return;
		}
		ControlSizeOfLists();
		MeshData result;
		while (!forRecycling.IsEmpty && forRecycling.TryDequeue(out result))
		{
			int num = result.VerticesMax / 4;
			if (num < 368)
			{
				TryAdd(smallSizes, num, result);
			}
			else if (num < 3072)
			{
				TryAdd(mediumSizes, num, result);
			}
			else
			{
				TryAdd(largeSizes, num, result);
			}
			result.RecyclingTime = game.ElapsedMilliseconds;
		}
	}

	public void Recycle(MeshData meshData)
	{
		if (!disposed)
		{
			forRecycling.Enqueue(meshData);
		}
	}

	public void Dispose()
	{
		disposed = true;
	}

	private void ControlSizeOfLists()
	{
		RemoveOldest(smallSizes, 300000);
		RemoveOldest(mediumSizes, 900000);
		RemoveOldest(largeSizes, 2240000);
	}

	private void RemoveOldest(SortedList<float, MeshData> list, int maxSize)
	{
		if (list.Count == 0)
		{
			return;
		}
		int num = 0;
		int index = 0;
		long recyclingTime = list.GetValueAtIndex(0).RecyclingTime;
		int num2 = 0;
		foreach (KeyValuePair<float, MeshData> item in list)
		{
			num += (int)item.Key;
			if (item.Value.RecyclingTime < recyclingTime)
			{
				index = num2;
				recyclingTime = item.Value.RecyclingTime;
			}
			num2++;
		}
		if (num > maxSize || recyclingTime < game.ElapsedMilliseconds - 15000)
		{
			list.GetValueAtIndex(index).DisposeBasicData();
			list.RemoveAt(index);
		}
	}

	private MeshData GetRecycled(int minimumCapacity)
	{
		if (disposed)
		{
			return null;
		}
		int num = minimumCapacity / 4;
		if (num < 368)
		{
			return TryGet(smallSizes, num);
		}
		if (num < 3072)
		{
			return TryGet(mediumSizes, num);
		}
		return TryGet(largeSizes, num);
	}

	private void TryAdd(SortedList<float, MeshData> list, int intkey, MeshData entry)
	{
		float num = intkey;
		while (num < (float)(intkey + 1))
		{
			if (list.TryAdd(num, entry))
			{
				return;
			}
			float num2 = num + 0.25f;
			if (num2 == num)
			{
				num2 = num + 0.5f;
			}
			if (num2 == num)
			{
				break;
			}
			num = num2;
		}
		entry.DisposeBasicData();
	}

	private MeshData TryGet(SortedList<float, MeshData> list, int entrySize)
	{
		if (list.Count == 0)
		{
			return null;
		}
		int num = Array.BinarySearch((float[])keysAccessor.GetValue(list), 0, list.Count, entrySize, null);
		if (num < 0)
		{
			num = ~num;
			if (num >= list.Count)
			{
				return null;
			}
			if ((int)list.GetKeyAtIndex(num) > entrySize * 5 / 4 + 64)
			{
				return null;
			}
		}
		MeshData valueAtIndex = list.GetValueAtIndex(num);
		list.RemoveAt(num);
		return valueAtIndex;
	}
}
