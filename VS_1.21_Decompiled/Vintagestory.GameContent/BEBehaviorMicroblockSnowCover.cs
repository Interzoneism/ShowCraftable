using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorMicroblockSnowCover : BlockEntityBehavior, IRotatable, IMicroblockBehavior
{
	public int SnowLevel;

	public int PrevSnowLevel;

	public int snowLayerBlockId;

	public List<uint> SnowCuboids = new List<uint>();

	public List<uint> GroundSnowCuboids = new List<uint>();

	public MeshData SnowMesh;

	private BlockEntityMicroBlock beMicroBlock;

	private CuboidWithMaterial[] aboveCuboids;

	public BEBehaviorMicroblockSnowCover(BlockEntity blockentity)
		: base(blockentity)
	{
		beMicroBlock = blockentity as BlockEntityMicroBlock;
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		SnowLevel = (int)base.Block.snowLevel;
		snowLayerBlockId = (base.Block as BlockMicroBlock)?.snowLayerBlockId ?? 0;
	}

	public void RotateModel(int degrees, EnumAxis? flipAroundAxis)
	{
		if (flipAroundAxis.HasValue)
		{
			SnowCuboids = new List<uint>();
			GroundSnowCuboids = new List<uint>();
			SnowLevel = 0;
			if (Api != null)
			{
				Api.World.BlockAccessor.ExchangeBlock((base.Block as BlockMicroBlock).notSnowCovered.Id, base.Pos);
			}
		}
		else
		{
			beMicroBlock.TransformList(degrees, flipAroundAxis, SnowCuboids);
			beMicroBlock.TransformList(degrees, flipAroundAxis, GroundSnowCuboids);
		}
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int byDegrees, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAroundAxis)
	{
		uint[] array = (tree["snowcuboids"] as IntArrayAttribute)?.AsUint;
		SnowCuboids = ((array == null) ? new List<uint>(0) : new List<uint>(array));
		uint[] array2 = (tree["groundSnowCuboids"] as IntArrayAttribute)?.AsUint;
		GroundSnowCuboids = ((array2 == null) ? new List<uint>(0) : new List<uint>(array2));
		tree["snowcuboids"] = new IntArrayAttribute(SnowCuboids.ToArray());
		tree["groundSnowCuboids"] = new IntArrayAttribute(GroundSnowCuboids.ToArray());
	}

	private void buildSnowCuboids(BoolArray16x16x16 Voxels)
	{
		List<uint> list = new List<uint>();
		List<uint> list2 = new List<uint>();
		BlockEntityMicroBlock blockEntityMicroBlock = Api?.World.BlockAccessor.GetBlockEntity(base.Pos.UpCopy()) as BlockEntityMicroBlock;
		CuboidWithMaterial[] array = null;
		bool[,] array2 = new bool[16, 16];
		for (int num = 15; num >= 0; num--)
		{
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					if (array2[i, j])
					{
						continue;
					}
					bool flag = num == 0 && !Voxels[i, num, j];
					if (!flag && !Voxels[i, num, j])
					{
						continue;
					}
					if (num == 15 && blockEntityMicroBlock != null)
					{
						if (array == null)
						{
							array = new CuboidWithMaterial[blockEntityMicroBlock.VoxelCuboids.Count];
							for (int k = 0; k < array.Length; k++)
							{
								BlockEntityMicroBlock.FromUint(blockEntityMicroBlock.VoxelCuboids[k], array[k] = new CuboidWithMaterial());
							}
						}
						for (int l = 0; l < array.Length; l++)
						{
							array[l].Contains(i, num, j);
						}
					}
					CuboidWithMaterial cuboidWithMaterial = new CuboidWithMaterial
					{
						Material = 0,
						X1 = i,
						Y1 = num,
						Z1 = j,
						X2 = i,
						Y2 = num + 1,
						Z2 = j
					};
					bool flag2 = true;
					while (flag2)
					{
						flag2 = false;
						flag2 |= TrySnowableSurfaceGrowX(cuboidWithMaterial, Voxels, array2, flag);
						flag2 |= TrySnowableSurfaceGrowZ(cuboidWithMaterial, Voxels, array2, flag);
					}
					if (cuboidWithMaterial.SizeX == 0 || cuboidWithMaterial.SizeZ == 0)
					{
						continue;
					}
					for (int m = cuboidWithMaterial.Z1; m < cuboidWithMaterial.Z2; m++)
					{
						for (int n = cuboidWithMaterial.X1; n < cuboidWithMaterial.X2; n++)
						{
							array2[n, m] = true;
						}
					}
					if (flag)
					{
						list2.Add(BlockEntityMicroBlock.ToUint(cuboidWithMaterial));
					}
					else
					{
						list.Add(BlockEntityMicroBlock.ToUint(cuboidWithMaterial));
					}
					break;
				}
			}
		}
		aboveCuboids = array;
		GroundSnowCuboids = list2;
		SnowCuboids = list;
	}

	private void GenSnowMesh()
	{
		if (beMicroBlock != null)
		{
			beMicroBlock.ConvertToVoxels(out var voxels, out var _);
			buildSnowCuboids(voxels);
		}
		if (SnowCuboids.Count > 0 && SnowLevel > 0)
		{
			SnowMesh = BlockEntityMicroBlock.CreateMesh(Api as ICoreClientAPI, SnowCuboids, new int[1] { snowLayerBlockId }, null, 0, beMicroBlock.OriginalVoxelCuboids, base.Pos);
			SnowMesh.Translate(0f, 0.0625f, 0f);
			SnowMesh.Scale(new Vec3f(0.5f, 0f, 0.5f), 0.999f, 1f, 0.999f);
			if (Api.World.BlockAccessor.IsSideSolid(base.Pos.X, base.Pos.Y - 1, base.Pos.Z, BlockFacing.UP))
			{
				SnowMesh.AddMeshData(BlockEntityMicroBlock.CreateMesh(Api as ICoreClientAPI, GroundSnowCuboids, new int[1] { snowLayerBlockId }, null, 0, beMicroBlock.OriginalVoxelCuboids, base.Pos));
			}
		}
		else
		{
			SnowMesh = null;
		}
	}

	protected bool TrySnowableSurfaceGrowX(CuboidWithMaterial cub, BoolArray16x16x16 voxels, bool[,] voxelVisited, bool ground)
	{
		if (cub.X2 > 15)
		{
			return false;
		}
		int num;
		for (num = cub.Z1; num < cub.Z2; num++)
		{
			num = Math.Min(15, num);
			if (aboveCuboids != null)
			{
				for (int i = 0; i < aboveCuboids.Length; i++)
				{
					if (aboveCuboids[i].Contains(cub.X2, 0, num))
					{
						return false;
					}
				}
			}
			if (voxels[cub.X2, cub.Y1, num] == ground || voxelVisited[cub.X2, num] || (cub.Y1 < 15 && voxels[cub.X2, cub.Y1 + 1, num]))
			{
				return false;
			}
		}
		cub.X2++;
		return true;
	}

	protected bool TrySnowableSurfaceGrowZ(CuboidWithMaterial cub, BoolArray16x16x16 voxels, bool[,] voxelVisited, bool ground)
	{
		if (cub.Z2 > 15)
		{
			return false;
		}
		int num;
		for (num = cub.X1; num < cub.X2; num++)
		{
			num = Math.Min(15, num);
			if (aboveCuboids != null)
			{
				for (int i = 0; i < aboveCuboids.Length; i++)
				{
					if (aboveCuboids[i].Contains(num, 0, cub.Z2))
					{
						return false;
					}
				}
			}
			if (voxels[num, cub.Y1, cub.Z2] == ground || voxelVisited[num, cub.Z2] || (cub.Y1 < 15 && voxels[num, cub.Y1 + 1, cub.Z2]))
			{
				return false;
			}
		}
		cub.Z2++;
		return true;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		SnowLevel = (int)base.Block.snowLevel;
		if (SnowLevel == 0)
		{
			if (Api.World.BlockAccessor.GetBlockEntity(base.Pos.UpCopy()) is BlockEntityMicroBlock blockEntityMicroBlock && blockEntityMicroBlock.Block.snowLevel > 0f && blockEntityMicroBlock.VolumeRel < 0.0625f)
			{
				SnowLevel = (int)blockEntityMicroBlock.Block.snowLevel;
			}
			if (SnowLevel == 0)
			{
				return false;
			}
		}
		if (PrevSnowLevel != SnowLevel || SnowMesh == null)
		{
			GenSnowMesh();
			PrevSnowLevel = SnowLevel;
		}
		mesher.AddMeshData(SnowMesh);
		return false;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		uint[] array = (tree["snowcuboids"] as IntArrayAttribute)?.AsUint;
		uint[] array2 = (tree["groundSnowCuboids"] as IntArrayAttribute)?.AsUint;
		if (array != null && array2 != null)
		{
			SnowCuboids = new List<uint>(array);
			GroundSnowCuboids = new List<uint>(array2);
		}
		else
		{
			SnowMesh = null;
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		if (SnowCuboids.Count > 0)
		{
			tree["snowcuboids"] = new IntArrayAttribute(SnowCuboids.ToArray());
		}
		if (GroundSnowCuboids.Count > 0)
		{
			tree["groundSnowCuboids"] = new IntArrayAttribute(GroundSnowCuboids.ToArray());
		}
	}

	public void RebuildCuboidList(BoolArray16x16x16 voxels, byte[,,] voxelMaterial)
	{
		buildSnowCuboids(voxels);
	}

	public void RegenMesh()
	{
		SnowLevel = (int)base.Block.snowLevel;
		if (SnowLevel == 0)
		{
			BlockEntityMicroBlock blockEntityMicroBlock = Api.World.BlockAccessor.GetBlockEntity(base.Pos.Up()) as BlockEntityMicroBlock;
			base.Pos.Down();
			if (blockEntityMicroBlock != null && blockEntityMicroBlock.Block.snowLevel > 0f && blockEntityMicroBlock.VolumeRel < 0.0625f)
			{
				SnowLevel = (int)blockEntityMicroBlock.Block.snowLevel;
			}
			if (SnowLevel == 0)
			{
				return;
			}
		}
		GenSnowMesh();
	}
}
