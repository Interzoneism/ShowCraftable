using System.Collections.Generic;

namespace Vintagestory.API.Client;

public class MeshDataPoolMasterManager
{
	private List<MeshDataPool> modelPools = new List<MeshDataPool>();

	private ICoreClientAPI capi;

	public float currentDt;

	public float[] currentModelViewMatrix;

	public float[] shadowMVPMatrix;

	public bool DelayedPoolLocationRemoval;

	private Queue<ModelDataPoolLocation[]>[] removalQueue = new Queue<ModelDataPoolLocation[]>[4]
	{
		new Queue<ModelDataPoolLocation[]>(),
		new Queue<ModelDataPoolLocation[]>(),
		new Queue<ModelDataPoolLocation[]>(),
		new Queue<ModelDataPoolLocation[]>()
	};

	public MeshDataPoolMasterManager(ICoreClientAPI capi)
	{
		this.capi = capi;
	}

	public void RemoveDataPoolLocations(ModelDataPoolLocation[] locations)
	{
		if (DelayedPoolLocationRemoval)
		{
			for (int i = 0; i < locations.Length; i++)
			{
				locations[i].Hide = true;
			}
			removalQueue[0].Enqueue(locations);
		}
		else
		{
			RemoveLocationsNow(locations);
		}
	}

	public void OnFrame(float dt, float[] modelviewMatrix, float[] shadowMVPMatrix)
	{
		currentDt = dt;
		currentModelViewMatrix = modelviewMatrix;
		this.shadowMVPMatrix = shadowMVPMatrix;
		while (removalQueue[3].Count > 0)
		{
			RemoveLocationsNow(removalQueue[3].Dequeue());
		}
		while (removalQueue[2].Count > 0)
		{
			removalQueue[3].Enqueue(removalQueue[2].Dequeue());
		}
		while (removalQueue[1].Count > 0)
		{
			removalQueue[2].Enqueue(removalQueue[1].Dequeue());
		}
		while (removalQueue[0].Count > 0)
		{
			removalQueue[1].Enqueue(removalQueue[0].Dequeue());
		}
	}

	private void RemoveLocationsNow(ModelDataPoolLocation[] locations)
	{
		for (int i = 0; i < locations.Length; i++)
		{
			if (locations[i] == null || modelPools[locations[i].PoolId] == null)
			{
				capi.World.Logger.Error("Could not remove model data from the master pool. Something wonky is happening. Ignoring for now.");
			}
			else
			{
				modelPools[locations[i].PoolId].RemoveLocation(locations[i]);
			}
		}
	}

	public void AddModelDataPool(MeshDataPool pool)
	{
		pool.poolId = modelPools.Count;
		modelPools.Add(pool);
	}

	public void DisposeAllPools(ICoreClientAPI capi)
	{
		for (int i = 0; i < modelPools.Count; i++)
		{
			modelPools[i].Dispose(capi);
		}
	}

	public float CalcFragmentation()
	{
		long num = 0L;
		long num2 = 0L;
		for (int i = 0; i < modelPools.Count; i++)
		{
			num += modelPools[i].UsedVertices;
			num2 += modelPools[i].VerticesPoolSize;
		}
		return (float)(1.0 - (double)num / (double)num2);
	}

	public int QuantityModelDataPools()
	{
		return modelPools.Count;
	}
}
