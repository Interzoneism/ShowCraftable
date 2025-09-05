using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockSlabSnowRemove : Block, ITexPositionSource
{
	private MeshData groundSnowLessMesh;

	private MeshData groundSnowedMesh;

	private bool testGroundSnowRemoval;

	private bool testGroundSnowAdd;

	private BlockFacing rot;

	private ICoreClientAPI capi;

	private AssetLocation snowLoc = new AssetLocation("block/liquid/snow/normal1");

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode] => capi.BlockTextureAtlas[snowLoc] ?? capi.BlockTextureAtlas.UnknownTexturePosition;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
		string text = Variant["rot"];
		rot = BlockFacing.FromCode(text);
		int num;
		if (Variant["cover"] == "snow")
		{
			switch (text)
			{
			default:
				num = ((text == "west") ? 1 : 0);
				break;
			case "north":
			case "east":
			case "south":
				num = 1;
				break;
			}
		}
		else
		{
			num = 0;
		}
		testGroundSnowRemoval = (byte)num != 0;
		int num2;
		if (Variant["cover"] == "free")
		{
			switch (text)
			{
			default:
				num2 = ((text == "west") ? 1 : 0);
				break;
			case "north":
			case "east":
			case "south":
				num2 = 1;
				break;
			}
		}
		else
		{
			num2 = 0;
		}
		testGroundSnowAdd = (byte)num2 != 0;
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (testGroundSnowRemoval && !SolidBlockBelow(chunkExtBlocks, extIndex3d, pos))
		{
			if (groundSnowLessMesh == null)
			{
				groundSnowLessMesh = sourceMesh.Clone();
				groundSnowLessMesh.RemoveVertices(24);
				groundSnowLessMesh.XyzFacesCount -= 6;
			}
			sourceMesh = groundSnowLessMesh;
		}
		if (!testGroundSnowAdd || chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[rot.Opposite.Index]].BlockMaterial != EnumBlockMaterial.Snow || !SolidBlockBelow(chunkExtBlocks, extIndex3d, pos))
		{
			return;
		}
		if (groundSnowedMesh == null)
		{
			Shape shapeBase = Vintagestory.API.Common.Shape.TryGet(api, "shapes/block/basic/slab/snow-" + Variant["rot"] + ".json");
			(api as ICoreClientAPI).Tesselator.TesselateShape("slab snow cover", shapeBase, out groundSnowedMesh, this, null, 0, 0, 0);
			for (int i = 0; i < groundSnowedMesh.RenderPassCount; i++)
			{
				groundSnowedMesh.RenderPassesAndExtraBits[i] = 0;
			}
			groundSnowedMesh.AddMeshData(sourceMesh);
		}
		sourceMesh = groundSnowedMesh;
	}

	private bool SolidBlockBelow(Block[] chunkExtBlocks, int extIndex3d, BlockPos pos)
	{
		if (chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[5]].SideSolid[BlockFacing.UP.Index])
		{
			return true;
		}
		return api.World.BlockAccessor.GetBlock(pos.DownCopy(), 2).SideSolid[BlockFacing.UP.Index];
	}

	public override float GetAmbientSoundStrength(IWorldAccessor world, BlockPos pos)
	{
		ClimateCondition selfClimateCond = capi.World.Player.Entity.selfClimateCond;
		if (FirstCodePart() == "glassslab" && selfClimateCond != null && selfClimateCond.Rainfall > 0.1f && selfClimateCond.Temperature > 3f && (world.BlockAccessor.GetRainMapHeightAt(pos) <= pos.Y || world.BlockAccessor.GetDistanceToRainFall(pos, 3) <= 2))
		{
			return selfClimateCond.Rainfall;
		}
		return 0f;
	}
}
