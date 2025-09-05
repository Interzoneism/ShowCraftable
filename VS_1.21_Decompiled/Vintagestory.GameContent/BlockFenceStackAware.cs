using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockFenceStackAware : BlockFence
{
	private ICoreClientAPI capi;

	private Dictionary<string, MeshData> continousFenceMeches;

	private string cntCode;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
		if (capi != null)
		{
			continousFenceMeches = ObjectCacheUtil.GetOrCreate(capi, Code.Domain + ":" + FirstCodePart() + "-continousFenceMeches", () => new Dictionary<string, MeshData>());
			cntCode = Code.ToShortString();
		}
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[4]] is BlockFence)
		{
			int num = GameMath.MurmurHash3Mod(pos.X, pos.Y, pos.Z, 8) + 1;
			if (!continousFenceMeches.TryGetValue(cntCode + num, out var value))
			{
				AssetLocation assetLocation = Shape.Base.Clone();
				assetLocation.Path = assetLocation.Path.Replace("-top", "");
				assetLocation.WithPathAppendixOnce(".json");
				assetLocation.WithPathPrefixOnce("shapes/");
				Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, assetLocation);
				CompositeTexture compositeTexture = Textures["wall"];
				int textureSubId = compositeTexture.Baked.TextureSubId;
				compositeTexture.Baked.TextureSubId = compositeTexture.Baked.BakedVariants[GameMath.MurmurHash3Mod(pos.X, pos.Y, pos.Z, compositeTexture.Alternates.Length)].TextureSubId;
				capi.Tesselator.TesselateShape(this, shape, out value, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ), Shape.QuantityElements, Shape.SelectiveElements);
				compositeTexture.Baked.TextureSubId = textureSubId;
				continousFenceMeches[cntCode] = value;
			}
			sourceMesh = value;
		}
	}
}
