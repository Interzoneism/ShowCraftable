using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ItemRollable : Item, IContainedMeshSource
{
	private string rolledShape;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		rolledShape = Attributes["rolledShape"].AsString();
	}

	public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
	{
		if (!Attributes.KeyExists("rolledShape"))
		{
			return null;
		}
		ICoreClientAPI obj = api as ICoreClientAPI;
		AssetLocation location = AssetLocation.Create(Attributes["rolledShape"].AsString(), Code.Domain).WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		Shape shape = obj.Assets.TryGet(location).ToObject<Shape>();
		ContainedTextureSource texSource = new ContainedTextureSource(obj, targetAtlas, shape.Textures, $"For displayed item {Code}");
		obj.Tesselator.TesselateShape(new TesselationMetaData
		{
			TexSource = texSource
		}, shape, out var modeldata);
		return modeldata;
	}

	public string GetMeshCacheKey(ItemStack itemstack)
	{
		return string.Concat(itemstack.Collectible.Code, "-", rolledShape);
	}
}
