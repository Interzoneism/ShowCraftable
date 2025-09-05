using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class MSShapeFromAttrCacheHelper : ModSystem
{
	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public static bool IsInCache(ICoreClientAPI capi, Block block, IShapeTypeProps cprops, string overrideTextureCode)
	{
		IDictionary<string, CompositeTexture> blockTextures = (block as BlockShapeFromAttributes).blockTextures;
		Shape shapeResolved = cprops.ShapeResolved;
		if (shapeResolved == null)
		{
			return false;
		}
		if (shapeResolved.Textures != null)
		{
			foreach (KeyValuePair<string, AssetLocation> texture in shapeResolved.Textures)
			{
				if (capi.BlockTextureAtlas[texture.Value] == null)
				{
					return false;
				}
			}
		}
		if (blockTextures != null)
		{
			foreach (KeyValuePair<string, CompositeTexture> item in blockTextures)
			{
				if (item.Value.Baked == null)
				{
					item.Value.Bake(capi.Assets);
				}
				if (capi.BlockTextureAtlas[item.Value.Baked.BakedName] == null)
				{
					return false;
				}
			}
		}
		if (cprops.Textures != null)
		{
			foreach (KeyValuePair<string, CompositeTexture> texture2 in cprops.Textures)
			{
				BakedCompositeTexture bakedCompositeTexture = texture2.Value.Baked ?? CompositeTexture.Bake(capi.Assets, texture2.Value);
				if (capi.BlockTextureAtlas[bakedCompositeTexture.BakedName] == null)
				{
					return false;
				}
			}
		}
		if (overrideTextureCode != null && cprops.TextureFlipCode != null && (block as BlockShapeFromAttributes).OverrideTextureGroups[cprops.TextureFlipGroupCode].TryGetValue(overrideTextureCode, out var value))
		{
			if (value.Baked == null)
			{
				value.Bake(capi.Assets);
			}
			if (capi.BlockTextureAtlas[value.Baked.BakedName] == null)
			{
				return false;
			}
		}
		return true;
	}
}
