using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class MeshDataPool
{
	public int MaxPartsPerPool;

	public int VerticesPoolSize;

	public int IndicesPoolSize;

	internal MeshRef modelRef;

	internal int poolId;

	internal List<ModelDataPoolLocation> poolLocations = new List<ModelDataPoolLocation>();

	public int[] indicesStartsByte;

	public int[] indicesSizes;

	public int indicesGroupsCount;

	public int indicesPosition;

	public int verticesPosition;

	public float CurrentFragmentation;

	public int UsedVertices;

	internal Vec3i poolOrigin;

	internal int dimensionId;

	public int RenderedTriangles;

	public int AllocatedTris;

	private MeshDataPool(int verticesPoolSize, int indicesPoolSize, int maxPartsPerPool)
	{
		MaxPartsPerPool = maxPartsPerPool;
		IndicesPoolSize = indicesPoolSize;
		VerticesPoolSize = verticesPoolSize;
	}

	public static MeshDataPool AllocateNewPool(ICoreClientAPI capi, int verticesPoolSize, int indicesPoolSize, int maxPartsPerPool, CustomMeshDataPartFloat customFloats = null, CustomMeshDataPartShort customShorts = null, CustomMeshDataPartByte customBytes = null, CustomMeshDataPartInt customInts = null)
	{
		MeshDataPool obj = new MeshDataPool(verticesPoolSize, indicesPoolSize, maxPartsPerPool)
		{
			indicesStartsByte = new int[maxPartsPerPool * 2],
			indicesSizes = new int[maxPartsPerPool]
		};
		customFloats?.SetAllocationSize(verticesPoolSize * customFloats.InterleaveStride / 4);
		customShorts?.SetAllocationSize(verticesPoolSize * customShorts.InterleaveStride / 2);
		customBytes?.SetAllocationSize(verticesPoolSize * customBytes.InterleaveStride);
		customInts?.SetAllocationSize(verticesPoolSize * customInts.InterleaveStride / 4);
		obj.modelRef = capi.Render.AllocateEmptyMesh(12 * verticesPoolSize, 0, 8 * verticesPoolSize, 4 * verticesPoolSize, 4 * verticesPoolSize, 4 * indicesPoolSize, customFloats, customShorts, customBytes, customInts, EnumDrawMode.Triangles, staticDraw: false);
		return obj;
	}

	public ModelDataPoolLocation TryAdd(ICoreClientAPI capi, MeshData modeldata, Vec3i modelOrigin, int dimension, Sphere frustumCullSphere)
	{
		if (poolLocations.Count >= MaxPartsPerPool || dimension != dimensionId)
		{
			return null;
		}
		if (poolOrigin != null)
		{
			if (poolLocations.Count == 0)
			{
				poolOrigin.Set(modelOrigin);
			}
			if (modelOrigin.SquareDistanceTo(poolOrigin) > 25000000)
			{
				return null;
			}
		}
		if (CurrentFragmentation > 0.03f)
		{
			ModelDataPoolLocation modelDataPoolLocation = TrySqueezeInbetween(capi, modeldata, modelOrigin, frustumCullSphere);
			if (modelDataPoolLocation != null)
			{
				return modelDataPoolLocation;
			}
		}
		return TryAppend(capi, modeldata, modelOrigin, frustumCullSphere);
	}

	private ModelDataPoolLocation TrySqueezeInbetween(ICoreClientAPI capi, MeshData modeldata, Vec3i modelOrigin, Sphere frustumCullSphere)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < poolLocations.Count; i++)
		{
			ModelDataPoolLocation modelDataPoolLocation = poolLocations[i];
			if (modelDataPoolLocation.IndicesStart - num2 > modeldata.IndicesCount && modelDataPoolLocation.VerticesStart - num > modeldata.VerticesCount)
			{
				return InsertAt(capi, modeldata, modelOrigin, frustumCullSphere, num2, num, i);
			}
			num2 = modelDataPoolLocation.IndicesEnd;
			num = modelDataPoolLocation.VerticesEnd;
		}
		return null;
	}

	private ModelDataPoolLocation TryAppend(ICoreClientAPI capi, MeshData modeldata, Vec3i modelOrigin, Sphere frustumCullSphere)
	{
		if (modeldata.IndicesCount + indicesPosition > IndicesPoolSize || modeldata.VerticesCount + verticesPosition > VerticesPoolSize)
		{
			return null;
		}
		ModelDataPoolLocation result = InsertAt(capi, modeldata, modelOrigin, frustumCullSphere, indicesPosition, verticesPosition, -1);
		indicesPosition += modeldata.IndicesCount;
		verticesPosition += modeldata.VerticesCount;
		return result;
	}

	private ModelDataPoolLocation InsertAt(ICoreClientAPI capi, MeshData modeldata, Vec3i modelOrigin, Sphere frustumCullSphere, int indexPosition, int vertexPosition, int listPosition)
	{
		if (vertexPosition > 0)
		{
			for (int i = 0; i < modeldata.IndicesCount; i++)
			{
				modeldata.Indices[i] += vertexPosition;
			}
		}
		if (poolOrigin != null)
		{
			int num = modelOrigin.X - poolOrigin.X;
			int num2 = modelOrigin.Y - poolOrigin.Y;
			int num3 = modelOrigin.Z - poolOrigin.Z;
			for (int j = 0; j < modeldata.VerticesCount; j++)
			{
				modeldata.xyz[3 * j] += num;
				modeldata.xyz[3 * j + 1] += num2;
				modeldata.xyz[3 * j + 2] += num3;
			}
		}
		modeldata.XyzOffset = vertexPosition * 12;
		modeldata.NormalsOffset = vertexPosition * 4;
		modeldata.RgbaOffset = vertexPosition * 4;
		modeldata.Rgba2Offset = vertexPosition * 4;
		modeldata.UvOffset = vertexPosition * 8;
		modeldata.FlagsOffset = vertexPosition * 4;
		modeldata.IndicesOffset = indexPosition * 4;
		if (modeldata.CustomFloats != null)
		{
			modeldata.CustomFloats.BaseOffset = vertexPosition * modeldata.CustomFloats.InterleaveStride;
		}
		if (modeldata.CustomShorts != null)
		{
			modeldata.CustomShorts.BaseOffset = vertexPosition * modeldata.CustomShorts.InterleaveStride;
		}
		if (modeldata.CustomBytes != null)
		{
			modeldata.CustomBytes.BaseOffset = vertexPosition * modeldata.CustomBytes.InterleaveStride;
		}
		if (modeldata.CustomInts != null)
		{
			modeldata.CustomInts.BaseOffset = vertexPosition * modeldata.CustomInts.InterleaveStride;
		}
		capi.Render.UpdateChunkMesh(modelRef, modeldata);
		ModelDataPoolLocation modelDataPoolLocation = new ModelDataPoolLocation
		{
			IndicesStart = indexPosition,
			IndicesEnd = indexPosition + modeldata.IndicesCount,
			VerticesStart = vertexPosition,
			VerticesEnd = vertexPosition + modeldata.VerticesCount,
			PoolId = poolId,
			FrustumCullSphere = frustumCullSphere
		};
		if (listPosition != -1)
		{
			poolLocations.Insert(listPosition, modelDataPoolLocation);
		}
		else
		{
			poolLocations.Add(modelDataPoolLocation);
		}
		CalcFragmentation();
		return modelDataPoolLocation;
	}

	public void RemoveLocation(ModelDataPoolLocation location)
	{
		if (location.PoolId != poolId)
		{
			throw new Exception("invalid call");
		}
		if (!poolLocations.Remove(location))
		{
			throw new InvalidOperationException("Tried to remove mesh that does not exist. This shouldn't happen");
		}
		if (poolLocations.Count == 0)
		{
			indicesPosition = 0;
			verticesPosition = 0;
		}
		else if (location.IndicesEnd == indicesPosition && location.VerticesEnd == verticesPosition)
		{
			indicesPosition = poolLocations[poolLocations.Count - 1].IndicesEnd;
			verticesPosition = poolLocations[poolLocations.Count - 1].VerticesEnd;
		}
		CalcFragmentation();
	}

	public void Draw(ICoreClientAPI capi, FrustumCulling frustumCuller, EnumFrustumCullMode frustumCullMode)
	{
		FrustumCull(frustumCuller, frustumCullMode);
		capi.Render.RenderMesh(modelRef, indicesStartsByte, indicesSizes, indicesGroupsCount);
	}

	public void FrustumCull(FrustumCulling frustumCuller, EnumFrustumCullMode frustumCullMode)
	{
		indicesGroupsCount = 0;
		RenderedTriangles = 0;
		AllocatedTris = 0;
		for (int i = 0; i < poolLocations.Count; i++)
		{
			ModelDataPoolLocation modelDataPoolLocation = poolLocations[i];
			int num = modelDataPoolLocation.IndicesEnd - modelDataPoolLocation.IndicesStart;
			if (modelDataPoolLocation.IsVisible(frustumCullMode, frustumCuller))
			{
				indicesStartsByte[indicesGroupsCount * 2] = modelDataPoolLocation.IndicesStart * 4;
				indicesSizes[indicesGroupsCount] = num;
				RenderedTriangles += num / 3;
				indicesGroupsCount++;
			}
			AllocatedTris += num / 3;
		}
	}

	public void SetFullyVisible()
	{
		indicesGroupsCount = 0;
		RenderedTriangles = 0;
		AllocatedTris = 0;
		for (int i = 0; i < poolLocations.Count; i++)
		{
			ModelDataPoolLocation modelDataPoolLocation = poolLocations[i];
			int num = modelDataPoolLocation.IndicesEnd - modelDataPoolLocation.IndicesStart;
			indicesStartsByte[indicesGroupsCount * 2] = modelDataPoolLocation.IndicesStart * 4;
			indicesSizes[indicesGroupsCount] = num;
			RenderedTriangles += num / 3;
			indicesGroupsCount++;
			AllocatedTris += num / 3;
		}
	}

	public bool IsEmpty()
	{
		return poolLocations.Count == 0;
	}

	public void Dispose(ICoreClientAPI capi)
	{
		capi.Render.DeleteMesh(modelRef);
	}

	public void CalcFragmentation()
	{
		int num = 0;
		int num2 = 0;
		UsedVertices = 0;
		if (verticesPosition == 0)
		{
			CurrentFragmentation = 0f;
			return;
		}
		foreach (ModelDataPoolLocation poolLocation in poolLocations)
		{
			UsedVertices += poolLocation.VerticesEnd - poolLocation.VerticesStart;
			num2 += Math.Max(0, poolLocation.VerticesStart - num);
			num = poolLocation.VerticesEnd;
		}
		CurrentFragmentation = (float)num2 / (float)verticesPosition;
	}

	public float GetFragmentation()
	{
		return CurrentFragmentation;
	}

	public void RenderMesh(IRenderAPI render)
	{
		render.RenderMesh(modelRef, indicesStartsByte, indicesSizes, indicesGroupsCount);
	}
}
