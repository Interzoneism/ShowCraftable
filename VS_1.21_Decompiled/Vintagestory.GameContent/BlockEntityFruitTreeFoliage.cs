using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityFruitTreeFoliage : BlockEntityFruitTreePart
{
	public override void Initialize(ICoreAPI api)
	{
		blockFoliage = base.Block as BlockFruitTreeFoliage;
		string text = base.Block.Attributes?["branchBlock"]?.AsString();
		if (text == null)
		{
			api.World.BlockAccessor.SetBlock(0, Pos);
			return;
		}
		blockBranch = api.World.GetBlock(AssetLocation.Create(text, base.Block.Code.Domain)) as BlockFruitTreeBranch;
		base.Initialize(api);
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		GenMesh();
	}

	public override void GenMesh()
	{
		base.GenFoliageMesh(withSticks: true, out leavesMesh, out sticksMesh);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (Api.World.EntityDebugMode)
		{
			dsc.AppendLine("TreeType: " + TreeType);
			dsc.AppendLine("FoliageState: " + FoliageState);
			dsc.AppendLine("Growthdir: " + GrowthDir);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		EnumFoliageState foliageState = FoliageState;
		if (Api != null && Api.Side == EnumAppSide.Client)
		{
			GenMesh();
			if (foliageState != FoliageState)
			{
				MarkDirty(redrawOnClient: true);
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (sticksMesh == null)
		{
			return true;
		}
		mesher.AddMeshData(leavesMesh);
		mesher.AddMeshData(CopyRndSticksMesh(sticksMesh));
		return true;
	}

	private MeshData CopyRndSticksMesh(MeshData mesh)
	{
		float x = (float)(GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 100) - 50) / 500f;
		float y = (float)(GameMath.MurmurHash3Mod(Pos.X, -Pos.Y, Pos.Z, 100) - 50) / 500f;
		float z = (float)(GameMath.MurmurHash3Mod(Pos.X, Pos.Y, -Pos.Z, 100) - 50) / 500f;
		float num = ((float)GameMath.MurmurHash3Mod(-Pos.X, -Pos.Y, Pos.Z, 100) - 50f) / 150f;
		float num2 = ((float)GameMath.MurmurHash3Mod(-Pos.X, -Pos.Y, -Pos.Z, 100) - 50f) / 150f;
		float radX = num;
		float radZ = num2;
		Vec3f origin = null;
		switch (GrowthDir.Index)
		{
		case 0:
			origin = new Vec3f(0.5f, 0.25f, 1.3125f);
			radX = 0f - num;
			break;
		case 1:
			radX = num2;
			radZ = 0f - num;
			origin = new Vec3f(-0.3125f, 0.25f, 0.5f);
			break;
		case 2:
			origin = new Vec3f(0.5f, 0.25f, -0.3125f);
			break;
		case 3:
			origin = new Vec3f(1.3125f, 0.25f, 0.5f);
			radX = 0f;
			radZ = num;
			break;
		case 4:
			origin = new Vec3f(0.5f, 0f, 0.5f);
			y = 0f;
			break;
		}
		return mesh?.Clone().Translate(x, y, z).Rotate(origin, radX, 0f, radZ);
	}
}
