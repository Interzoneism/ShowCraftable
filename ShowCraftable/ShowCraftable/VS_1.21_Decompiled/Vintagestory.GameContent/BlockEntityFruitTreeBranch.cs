using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityFruitTreeBranch : BlockEntityFruitTreePart
{
	public int SideGrowth;

	public Vec3i ParentOff;

	public int GrowTries;

	public double lastGrowthAttemptTotalDays;

	private MeshData branchMesh;

	private Cuboidf[] colSelBoxes;

	public float? FastForwardGrowth;

	private bool initialized;

	private bool beingBrokenLoopPrevention;

	private static Dictionary<string, int[]> facingRemapByShape = new Dictionary<string, int[]>
	{
		{
			"stem",
			new int[6] { 0, 1, 2, 3, 4, 5 }
		},
		{
			"branch-ud",
			new int[6] { 0, 1, 2, 3, 4, 5 }
		},
		{
			"branch-n",
			new int[6] { 4, 3, 5, 1, 0, 2 }
		},
		{
			"branch-s",
			new int[6] { 4, 3, 5, 1, 0, 2 }
		},
		{
			"branch-w",
			new int[6] { 0, 5, 2, 4, 3, 1 }
		},
		{
			"branch-e",
			new int[6] { 0, 5, 2, 4, 3, 1 }
		},
		{
			"branch-ud-end",
			new int[6] { 0, 1, 2, 3, 4, 5 }
		},
		{
			"branch-n-end",
			new int[6] { 4, 3, 5, 1, 0, 2 }
		},
		{
			"branch-s-end",
			new int[6] { 4, 3, 5, 1, 0, 2 }
		},
		{
			"branch-w-end",
			new int[6] { 0, 5, 2, 4, 3, 1 }
		},
		{
			"branch-e-end",
			new int[6] { 0, 5, 2, 4, 3, 1 }
		}
	};

	public override void Initialize(ICoreAPI api)
	{
		Api = api;
		Block block = base.Block;
		if (block != null && block.Attributes?["foliageBlock"].Exists == true)
		{
			blockFoliage = api.World.GetBlock(AssetLocation.Create(base.Block.Attributes["foliageBlock"].AsString(), base.Block.Code.Domain)) as BlockFruitTreeFoliage;
			blockBranch = base.Block as BlockFruitTreeBranch;
			initCustomBehaviors(null, callInitialize: false);
			base.Initialize(api);
			if (FastForwardGrowth.HasValue && Api.Side == EnumAppSide.Server)
			{
				lastGrowthAttemptTotalDays = Api.World.Calendar.TotalDays - 20.0 - (double)(FastForwardGrowth.Value * 600f);
				InitTreeRoot(TreeType, callInitialize: true);
				FastForwardGrowth = null;
			}
			updateProperties();
		}
	}

	public Cuboidf[] GetColSelBox()
	{
		if (GrowthDir.Axis == EnumAxis.Y)
		{
			return base.Block.CollisionBoxes;
		}
		return colSelBoxes;
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		InitTreeRoot(byItemStack?.Attributes?.GetString("type"), byItemStack != null, byItemStack);
	}

	internal void InteractDebug()
	{
		if (RootOff.Y != 0)
		{
			return;
		}
		FruitTreeRootBH fruitTreeRootBH = (Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(RootOff)) as BlockEntityFruitTreeBranch)?.GetBehavior<FruitTreeRootBH>();
		if (fruitTreeRootBH != null)
		{
			foreach (KeyValuePair<string, FruitTreeProperties> item in fruitTreeRootBH.propsByType)
			{
				item.Value.State = (EnumFruitTreeState)((int)(item.Value.State + 1) % 8);
			}
		}
		MarkDirty(redrawOnClient: true);
	}

	public void InitTreeRoot(string treeType, bool callInitialize, ItemStack parentPlantStack = null)
	{
		if (initialized)
		{
			return;
		}
		initialized = true;
		GrowthDir = BlockFacing.UP;
		PartType = ((!(parentPlantStack?.Collectible.Variant["type"] == "cutting")) ? EnumTreePartType.Branch : EnumTreePartType.Cutting);
		RootOff = new Vec3i();
		if (TreeType == null)
		{
			TreeType = treeType;
		}
		if (PartType == EnumTreePartType.Cutting && parentPlantStack != null)
		{
			BlockEntityFruitTreeBranch blockEntityFruitTreeBranch = Api.World.BlockAccessor.GetBlockEntity(Pos.DownCopy()) as BlockEntityFruitTreeBranch;
			if (Api.World.BlockAccessor.GetBlock(Pos.DownCopy()).Fertility <= 0 && blockEntityFruitTreeBranch == null)
			{
				BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
				foreach (BlockFacing blockFacing in hORIZONTALS)
				{
					if (Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(blockFacing)) is BlockEntityFruitTreeBranch blockEntityFruitTreeBranch2)
					{
						GrowthDir = blockFacing.Opposite;
						RootOff = blockEntityFruitTreeBranch2.RootOff.AddCopy(blockFacing);
						FruitTreeRootBH behavior = Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(RootOff)).GetBehavior<FruitTreeRootBH>();
						behavior.RegisterTreeType(treeType);
						behavior.propsByType[TreeType].OnFruitingStateChange += base.RootBh_OnFruitingStateChange;
						GenMesh();
					}
				}
			}
		}
		updateProperties();
		initCustomBehaviors(parentPlantStack, callInitialize);
		FruitTreeGrowingBranchBH behavior2 = GetBehavior<FruitTreeGrowingBranchBH>();
		if (behavior2 == null)
		{
			return;
		}
		behavior2.VDrive = 3f + (float)Api.World.Rand.NextDouble();
		behavior2.HDrive = 1f;
		if (treeType != null)
		{
			Vec3i rootOff = RootOff;
			if ((object)rootOff != null && rootOff.IsZero)
			{
				FruitTreeProperties fruitTreeProperties = GetBehavior<FruitTreeRootBH>().propsByType[TreeType];
				behavior2.HDrive *= fruitTreeProperties.RootSizeMul;
				behavior2.VDrive *= fruitTreeProperties.RootSizeMul;
			}
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer)
	{
		if (beingBrokenLoopPrevention)
		{
			Api.Logger.Error(new Exception("Fruit tree branch would endlessly loop here"));
			return;
		}
		beingBrokenLoopPrevention = true;
		base.OnBlockBroken(byPlayer);
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing in aLLFACES)
		{
			BlockPos blockPos = Pos.AddCopy(blockFacing);
			Block block = Api.World.BlockAccessor.GetBlock(blockPos);
			if (block == blockFoliage)
			{
				bool flag = false;
				BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
				foreach (BlockFacing facing in hORIZONTALS)
				{
					BlockPos blockPos2 = blockPos.AddCopy(facing);
					if (!(blockPos2 == Pos) && Api.World.BlockAccessor.GetBlock(blockPos2) == blockBranch)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					Api.World.BlockAccessor.BreakBlock(blockPos, byPlayer);
				}
			}
			if (!(block is BlockFruitTreeBranch) || !blockFacing.IsHorizontal)
			{
				continue;
			}
			BlockEntity blockEntity = Api.World.BlockAccessor.GetBlockEntity(blockPos);
			if (blockEntity != null)
			{
				FruitTreeGrowingBranchBH fruitTreeGrowingBranchBH = blockEntity.GetBehavior<FruitTreeGrowingBranchBH>();
				if (fruitTreeGrowingBranchBH == null)
				{
					fruitTreeGrowingBranchBH = new FruitTreeGrowingBranchBH(blockEntity);
					fruitTreeGrowingBranchBH.Initialize(Api, null);
					blockEntity.Behaviors.Add(fruitTreeGrowingBranchBH);
				}
				fruitTreeGrowingBranchBH.OnNeighbourBranchRemoved(blockFacing.Opposite);
			}
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		FruitTreeRootBH fruitTreeRootBH = (Api?.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(RootOff)) as BlockEntityFruitTreeBranch)?.GetBehavior<FruitTreeRootBH>();
		if (fruitTreeRootBH != null)
		{
			fruitTreeRootBH.BlocksRemoved++;
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
	}

	public void updateProperties()
	{
		if (GrowthDir.Axis != EnumAxis.Y)
		{
			float degX = ((GrowthDir.Axis == EnumAxis.Z) ? 90 : 0);
			float degZ = ((GrowthDir.Axis == EnumAxis.X) ? 90 : 0);
			if (base.Block == null || base.Block.CollisionBoxes == null)
			{
				Api?.World.Logger.Warning("BEFruitTreeBranch:updatedProperties() Block {0} or its collision box is null? Block might have incorrect hitboxes now.", base.Block?.Code);
			}
			else
			{
				colSelBoxes = new Cuboidf[1] { base.Block.CollisionBoxes[0].Clone().RotatedCopy(degX, 0f, degZ, new Vec3d(0.5, 0.5, 0.5)) };
			}
		}
		GenMesh();
	}

	public override void GenMesh()
	{
		branchMesh = GenMeshes();
	}

	public MeshData GenMeshes()
	{
		if (capi == null)
		{
			return null;
		}
		if (Api.Side != EnumAppSide.Client || TreeType == null || TreeType == "")
		{
			return null;
		}
		string key = "fruitTreeMeshes" + base.Block.Code.ToShortString();
		Dictionary<int, MeshData> orCreate = ObjectCacheUtil.GetOrCreate(Api, key, () => new Dictionary<int, MeshData>());
		leavesMesh = null;
		if (PartType == EnumTreePartType.Branch && Height > 0)
		{
			GenFoliageMesh(withSticks: false, out leavesMesh, out var _);
		}
		string text = "stem";
		switch (PartType)
		{
		case EnumTreePartType.Cutting:
			if (GrowthDir.Axis == EnumAxis.Y)
			{
				text = "cutting-ud";
			}
			if (GrowthDir.Axis == EnumAxis.X)
			{
				text = "cutting-we";
			}
			if (GrowthDir.Axis == EnumAxis.Z)
			{
				text = "cutting-ns";
			}
			break;
		case EnumTreePartType.Branch:
			text = ((GrowthDir.Axis != EnumAxis.Y) ? ("branch-" + GrowthDir.Code[0]) : "branch-ud");
			if (!(Api.World.BlockAccessor.GetBlock(Pos.AddCopy(GrowthDir)) is BlockFruitTreeBranch))
			{
				text += "-end";
			}
			break;
		case EnumTreePartType.Leaves:
			text = "leaves";
			break;
		}
		int hashCode = getHashCode(text);
		if (orCreate.TryGetValue(hashCode, out var value))
		{
			return value;
		}
		CompositeShape compositeShape = base.Block?.Attributes?["shapes"][text].AsObject<CompositeShape>(null, base.Block.Code.Domain);
		if (compositeShape == null)
		{
			return null;
		}
		nowTesselatingShape = Shape.TryGet(Api, compositeShape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));
		if (nowTesselatingShape == null)
		{
			return null;
		}
		List<string> list = null;
		if (PartType != EnumTreePartType.Cutting)
		{
			list = new List<string>(new string[2] { "stem", "root/branch" });
			if (PartType != EnumTreePartType.Leaves)
			{
				int[] array = facingRemapByShape[text];
				for (int num = 0; num < 8; num++)
				{
					if ((SideGrowth & (1 << num)) > 0)
					{
						char c = BlockFacing.ALLFACES[array[num]].Code[0];
						list.Add("branch-" + c);
						list.Add("root/branch-" + c);
					}
				}
			}
		}
		capi.Tesselator.TesselateShape("fruittreebranch", nowTesselatingShape, out value, this, new Vec3f(compositeShape.rotateX, compositeShape.rotateY, compositeShape.rotateZ), 0, 0, 0, null, list?.ToArray());
		value.ClimateColorMapIds.Fill((byte)0);
		value.SeasonColorMapIds.Fill((byte)0);
		return orCreate[hashCode] = value;
	}

	private int getHashCode(string shapekey)
	{
		return (SideGrowth + "-" + PartType.ToString() + "-" + FoliageState.ToString() + "-" + GrowthDir.Index + "-" + shapekey + "-" + TreeType).GetHashCode();
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (!Api.World.EntityDebugMode)
		{
			return;
		}
		dsc.AppendLine("LeavesState: " + FoliageState);
		dsc.AppendLine("TreeType: " + TreeType);
		dsc.Append("SideGrowth: ");
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing blockFacing in aLLFACES)
		{
			if ((SideGrowth & (1 << blockFacing.Index)) > 0)
			{
				dsc.Append(blockFacing.Code[0]);
			}
		}
		dsc.AppendLine();
		FruitTreeRootBH fruitTreeRootBH = (Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(RootOff)) as BlockEntityFruitTreeBranch)?.GetBehavior<FruitTreeRootBH>();
		if (fruitTreeRootBH == null)
		{
			return;
		}
		foreach (KeyValuePair<string, FruitTreeProperties> item in fruitTreeRootBH.propsByType)
		{
			dsc.AppendLine(item.Key + " " + item.Value.State);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		GrowTries = tree.GetInt("growTries");
		if (tree.HasAttribute("rootOffX"))
		{
			RootOff = new Vec3i(tree.GetInt("rootOffX"), tree.GetInt("rootOffY"), tree.GetInt("rootOffZ"));
		}
		initCustomBehaviors(null, callInitialize: false);
		base.FromTreeAttributes(tree, worldForResolving);
		SideGrowth = tree.GetInt("sideGrowth");
		if (tree.HasAttribute("parentX"))
		{
			ParentOff = new Vec3i(tree.GetInt("parentX"), tree.GetInt("parentY"), tree.GetInt("parentZ"));
		}
		FastForwardGrowth = null;
		if (tree.HasAttribute("fastForwardGrowth"))
		{
			FastForwardGrowth = tree.GetFloat("fastForwardGrowth");
		}
		lastGrowthAttemptTotalDays = tree.GetDouble("lastGrowthAttemptTotalDays");
		if (Api != null)
		{
			updateProperties();
		}
	}

	private void initCustomBehaviors(ItemStack parentPlantStack, bool callInitialize)
	{
		Vec3i rootOff = RootOff;
		if ((object)rootOff != null && rootOff.IsZero && GetBehavior<FruitTreeRootBH>() == null)
		{
			FruitTreeRootBH fruitTreeRootBH = new FruitTreeRootBH(this, parentPlantStack);
			if (callInitialize)
			{
				fruitTreeRootBH.Initialize(Api, null);
			}
			Behaviors.Add(fruitTreeRootBH);
		}
		if (GrowTries < 60 && GetBehavior<FruitTreeGrowingBranchBH>() == null)
		{
			FruitTreeGrowingBranchBH fruitTreeGrowingBranchBH = new FruitTreeGrowingBranchBH(this);
			if (callInitialize)
			{
				fruitTreeGrowingBranchBH.Initialize(Api, null);
			}
			Behaviors.Add(fruitTreeGrowingBranchBH);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("sideGrowth", SideGrowth);
		if (ParentOff != null)
		{
			tree.SetInt("parentX", ParentOff.X);
			tree.SetInt("parentY", ParentOff.Y);
			tree.SetInt("parentZ", ParentOff.Z);
		}
		tree.SetInt("growTries", GrowTries);
		if (FastForwardGrowth.HasValue)
		{
			tree.SetFloat("fastForwardGrowth", FastForwardGrowth.Value);
		}
		tree.SetDouble("lastGrowthAttemptTotalDays", lastGrowthAttemptTotalDays);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		mesher.AddMeshData(branchMesh);
		if (leavesMesh != null)
		{
			mesher.AddMeshData(leavesMesh);
		}
		return true;
	}
}
