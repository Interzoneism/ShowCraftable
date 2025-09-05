using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class MeshDataPoolManager
{
	private List<MeshDataPool> pools = new List<MeshDataPool>();

	internal FrustumCulling frustumCuller;

	private ICoreClientAPI capi;

	private MeshDataPoolMasterManager masterPool;

	private CustomMeshDataPartFloat customFloats;

	private CustomMeshDataPartShort customShorts;

	private CustomMeshDataPartByte customBytes;

	private CustomMeshDataPartInt customInts;

	private int defaultVertexPoolSize;

	private int defaultIndexPoolSize;

	private int maxPartsPerPool;

	private Vec3f tmp = new Vec3f();

	public MeshDataPoolManager(MeshDataPoolMasterManager masterPool, FrustumCulling frustumCuller, ICoreClientAPI capi, int defaultVertexPoolSize, int defaultIndexPoolSize, int maxPartsPerPool, CustomMeshDataPartFloat customFloats = null, CustomMeshDataPartShort customShorts = null, CustomMeshDataPartByte customBytes = null, CustomMeshDataPartInt customInts = null)
	{
		this.masterPool = masterPool;
		this.frustumCuller = frustumCuller;
		this.capi = capi;
		this.customFloats = customFloats;
		this.customBytes = customBytes;
		this.customInts = customInts;
		this.customShorts = customShorts;
		this.defaultIndexPoolSize = defaultIndexPoolSize;
		this.defaultVertexPoolSize = defaultVertexPoolSize;
		this.maxPartsPerPool = maxPartsPerPool;
	}

	public ModelDataPoolLocation AddModel(MeshData modeldata, Vec3i modelOrigin, int dimension, Sphere frustumCullSphere)
	{
		ModelDataPoolLocation modelDataPoolLocation = null;
		for (int i = 0; i < pools.Count; i++)
		{
			modelDataPoolLocation = pools[i].TryAdd(capi, modeldata, modelOrigin, dimension, frustumCullSphere);
			if (modelDataPoolLocation != null)
			{
				break;
			}
		}
		if (modelDataPoolLocation == null)
		{
			int num = Math.Max(modeldata.VerticesCount, defaultVertexPoolSize);
			int indicesPoolSize = Math.Max(modeldata.IndicesCount, defaultIndexPoolSize);
			if (num > defaultVertexPoolSize)
			{
				capi.World.Logger.Warning("Chunk (or some other mesh source at origin: {0}) exceeds default geometric complexity maximum of {1} vertices and {2} indices. You must be loading some very complex objects (#v = {3}, #i = {4}). Adjusted Pool size accordingly.", modelOrigin, defaultVertexPoolSize, defaultIndexPoolSize, modeldata.VerticesCount, modeldata.IndicesCount);
			}
			MeshDataPool meshDataPool = MeshDataPool.AllocateNewPool(capi, num, indicesPoolSize, maxPartsPerPool, customFloats, customShorts, customBytes, customInts);
			meshDataPool.poolOrigin = modelOrigin.Clone();
			meshDataPool.dimensionId = dimension;
			masterPool.AddModelDataPool(meshDataPool);
			pools.Add(meshDataPool);
			modelDataPoolLocation = meshDataPool.TryAdd(capi, modeldata, modelOrigin, dimension, frustumCullSphere);
		}
		if (modelDataPoolLocation == null)
		{
			capi.World.Logger.Fatal("Can't add modeldata (probably a tesselated chunk @{0}) to the model data pool list, blocks will likely be invisible. Potential reasons are the parts per pool were exceeded, or other code reasons, please report this. Default pool size is {1} vertices and {2} indices. Mesh size (#v = {3}, #i = {4}). Try increasing MaxVertexSize and MaxIndexSize.", modelOrigin, defaultVertexPoolSize, defaultIndexPoolSize, modeldata.VerticesCount, modeldata.IndicesCount);
		}
		return modelDataPoolLocation;
	}

	public void Render(Vec3d playerpos, string originUniformName, EnumFrustumCullMode frustumCullMode = EnumFrustumCullMode.CullNormal)
	{
		int count = pools.Count;
		for (int i = 0; i < count; i++)
		{
			MeshDataPool meshDataPool = pools[i];
			if (meshDataPool.dimensionId == 1)
			{
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				if (!capi.World.TryGetMiniDimension(meshDataPool.poolOrigin, out var dimension) || dimension.selectionTrackingOriginalPos == null)
				{
					continue;
				}
				meshDataPool.SetFullyVisible();
				if (meshDataPool.indicesGroupsCount == 0)
				{
					continue;
				}
				FastVec3d renderOffset = dimension.GetRenderOffset(masterPool.currentDt);
				IShaderProgram currentActiveShader = capi.Render.CurrentActiveShader;
				if (currentActiveShader.HasUniform("modelViewMatrix"))
				{
					currentActiveShader.UniformMatrix("modelViewMatrix", dimension.GetRenderTransformMatrix(masterPool.currentModelViewMatrix, playerpos));
					flag = true;
					if (currentActiveShader.HasUniform("forcedTransparency"))
					{
						currentActiveShader.Uniform("forcedTransparency", capi.Settings.Float["previewTransparency"]);
						flag3 = true;
					}
				}
				else if (currentActiveShader.HasUniform("mvpMatrix"))
				{
					currentActiveShader.UniformMatrix("mvpMatrix", dimension.GetRenderTransformMatrix(masterPool.shadowMVPMatrix, playerpos));
					flag2 = true;
				}
				currentActiveShader.Uniform(originUniformName, tmp.Set((float)((double)meshDataPool.poolOrigin.X + renderOffset.X - playerpos.X), (float)((double)meshDataPool.poolOrigin.Y + renderOffset.Y - playerpos.Y), (float)((double)meshDataPool.poolOrigin.Z + renderOffset.Z - playerpos.Z)));
				try
				{
					meshDataPool.RenderMesh(capi.Render);
				}
				finally
				{
					if (flag)
					{
						capi.Render.CurrentActiveShader.UniformMatrix("modelViewMatrix", masterPool.currentModelViewMatrix);
					}
					if (flag2)
					{
						capi.Render.CurrentActiveShader.UniformMatrix("mvpMatrix", masterPool.shadowMVPMatrix);
					}
					if (flag3)
					{
						capi.Render.CurrentActiveShader.Uniform("forcedTransparency", 0f);
					}
				}
			}
			else
			{
				meshDataPool.FrustumCull(frustumCuller, frustumCullMode);
				if (meshDataPool.indicesGroupsCount != 0)
				{
					capi.Render.CurrentActiveShader.Uniform(originUniformName, tmp.Set((float)((double)meshDataPool.poolOrigin.X - playerpos.X), (float)((double)(meshDataPool.poolOrigin.Y + meshDataPool.dimensionId * 32768) - playerpos.Y), (float)((double)meshDataPool.poolOrigin.Z - playerpos.Z)));
					meshDataPool.RenderMesh(capi.Render);
				}
			}
		}
	}

	public void GetStats(ref long usedVideoMemory, ref long renderedTris, ref long allocatedTris)
	{
		long num = (capi.Render.UseSSBOs ? 16 : 28) + ((customFloats != null) ? customFloats.InterleaveStride : 0) + ((customShorts != null) ? customShorts.InterleaveStride : 0) + ((customBytes != null) ? customBytes.InterleaveStride : 0) + ((customInts != null) ? customInts.InterleaveStride : 0);
		int num2 = ((!capi.Render.UseSSBOs) ? 4 : 0);
		int num3 = 0;
		for (int i = 0; i < pools.Count; i++)
		{
			MeshDataPool meshDataPool = pools[i];
			usedVideoMemory += meshDataPool.VerticesPoolSize * num + meshDataPool.IndicesPoolSize * num2;
			renderedTris += meshDataPool.RenderedTriangles;
			allocatedTris += meshDataPool.AllocatedTris;
			if (meshDataPool.IndicesPoolSize > num3)
			{
				num3 = meshDataPool.IndicesPoolSize;
			}
		}
		if (capi.Render.UseSSBOs)
		{
			usedVideoMemory += num3 * 4;
		}
	}
}
