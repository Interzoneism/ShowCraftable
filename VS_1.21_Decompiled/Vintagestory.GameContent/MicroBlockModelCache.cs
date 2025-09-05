using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class MicroBlockModelCache : ModSystem
{
	private Dictionary<long, CachedModel> cachedModels = new Dictionary<long, CachedModel>();

	private long nextMeshId = 1L;

	private ICoreClientAPI capi;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		capi = api;
		api.Event.LeaveWorld += Event_LeaveWorld;
		api.Event.RegisterGameTickListener(OnSlowTick, 1000);
	}

	private void OnSlowTick(float dt)
	{
		List<long> list = new List<long>();
		foreach (KeyValuePair<long, CachedModel> cachedModel in cachedModels)
		{
			cachedModel.Value.Age += 1f;
			if (cachedModel.Value.Age > 180f)
			{
				list.Add(cachedModel.Key);
			}
		}
		foreach (long item in list)
		{
			cachedModels[item].MeshRef.Dispose();
			cachedModels.Remove(item);
		}
	}

	public MultiTextureMeshRef GetOrCreateMeshRef(ItemStack forStack)
	{
		long key = forStack.Attributes.GetLong("meshId", 0L);
		if (!cachedModels.ContainsKey(key))
		{
			MultiTextureMeshRef multiTextureMeshRef = CreateModel(forStack);
			forStack.Attributes.SetLong("meshId", nextMeshId);
			cachedModels[nextMeshId++] = new CachedModel
			{
				MeshRef = multiTextureMeshRef,
				Age = 0f
			};
			return multiTextureMeshRef;
		}
		cachedModels[key].Age = 0f;
		return cachedModels[key].MeshRef;
	}

	private MultiTextureMeshRef CreateModel(ItemStack forStack)
	{
		ITreeAttribute treeAttribute = forStack.Attributes;
		if (treeAttribute == null)
		{
			treeAttribute = new TreeAttribute();
		}
		int[] array = BlockEntityMicroBlock.MaterialIdsFromAttributes(treeAttribute, capi.World);
		uint[] array2 = (treeAttribute["cuboids"] as IntArrayAttribute)?.AsUint;
		if (array2 == null)
		{
			array2 = (treeAttribute["cuboids"] as LongArrayAttribute)?.AsUint;
		}
		List<uint> voxelCuboids = ((array2 == null) ? new List<uint>() : new List<uint>(array2));
		Block block = capi.World.Blocks[array[0]];
		bool num = block.Attributes?.IsTrue("chiselShapeFromCollisionBox") ?? false;
		uint[] array3 = null;
		if (num)
		{
			Cuboidf[] collisionBoxes = block.CollisionBoxes;
			array3 = new uint[collisionBoxes.Length];
			for (int i = 0; i < collisionBoxes.Length; i++)
			{
				Cuboidf cuboidf = collisionBoxes[i];
				uint num2 = BlockEntityMicroBlock.ToUint((int)(16f * cuboidf.X1), (int)(16f * cuboidf.Y1), (int)(16f * cuboidf.Z1), (int)(16f * cuboidf.X2), (int)(16f * cuboidf.Y2), (int)(16f * cuboidf.Z2), 0);
				array3[i] = num2;
			}
		}
		MeshData meshData = BlockEntityMicroBlock.CreateMesh(capi, voxelCuboids, array, null, null, array3);
		meshData.Rgba.Fill(byte.MaxValue);
		return capi.Render.UploadMultiTextureMesh(meshData);
	}

	private void Event_LeaveWorld()
	{
		foreach (KeyValuePair<long, CachedModel> cachedModel in cachedModels)
		{
			cachedModel.Value.MeshRef.Dispose();
		}
	}
}
