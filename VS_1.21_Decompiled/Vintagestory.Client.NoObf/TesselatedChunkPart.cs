using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class TesselatedChunkPart
{
	internal int atlasNumber;

	internal MeshData modelDataLod0;

	internal MeshData modelDataLod1;

	internal MeshData modelDataNotLod2Far;

	internal MeshData modelDataLod2Far;

	internal EnumChunkRenderPass pass;

	internal void AddToPools(ChunkRenderer cr, List<ModelDataPoolLocation> locations, Vec3i chunkOrigin, int dimension, Sphere boundingSphere, Bools cullVisible)
	{
		bool flag = dimension == 1 && BlockAccessorMovable.IsTransparent(chunkOrigin);
		MeshDataPoolManager pools = cr.poolsByRenderPass[flag ? Math.Max(3, (int)pass) : ((int)pass)][atlasNumber];
		if (modelDataLod0 != null)
		{
			cr.SetInterleaveStrides(modelDataLod0, pass);
			AddModelAndStoreLocation(pools, locations, modelDataLod0, chunkOrigin, dimension, boundingSphere, cullVisible, 0);
		}
		if (modelDataLod1 != null)
		{
			cr.SetInterleaveStrides(modelDataLod1, pass);
			AddModelAndStoreLocation(pools, locations, modelDataLod1, chunkOrigin, dimension, boundingSphere, cullVisible, 1);
		}
		if (modelDataNotLod2Far != null)
		{
			cr.SetInterleaveStrides(modelDataNotLod2Far, pass);
			AddModelAndStoreLocation(pools, locations, modelDataNotLod2Far, chunkOrigin, dimension, boundingSphere, cullVisible, 2);
		}
		if (modelDataLod2Far != null)
		{
			cr.SetInterleaveStrides(modelDataLod2Far, pass);
			AddModelAndStoreLocation(pools, locations, modelDataLod2Far, chunkOrigin, dimension, boundingSphere, cullVisible, 3);
		}
		Dispose();
	}

	internal void AddModelAndStoreLocation(MeshDataPoolManager pools, List<ModelDataPoolLocation> locations, MeshData modeldata, Vec3i modelOrigin, int dimension, Sphere frustumCullSphere, Bools cullVisible, int lodLevel)
	{
		ModelDataPoolLocation modelDataPoolLocation = pools.AddModel(modeldata, modelOrigin, dimension, frustumCullSphere);
		if (modelDataPoolLocation != null)
		{
			modelDataPoolLocation.CullVisible = cullVisible;
			modelDataPoolLocation.LodLevel = lodLevel;
			locations.Add(modelDataPoolLocation);
		}
	}

	internal void Dispose()
	{
		modelDataLod0?.Dispose();
		modelDataLod1?.Dispose();
		modelDataNotLod2Far?.Dispose();
		modelDataLod2Far?.Dispose();
	}
}
