using System.Collections.Generic;
using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

public class DynFoliageProperties
{
	public string TexturesBasePath;

	public Dictionary<string, CompositeTexture> Textures;

	public CompositeTexture LeafParticlesTexture;

	public CompositeTexture BlossomParticlesTexture;

	public string SeasonColorMap = "seasonalFoliage";

	public string ClimateColorMap = "climatePlantTint";

	public void Rebase(DynFoliageProperties props)
	{
		if (TexturesBasePath == null)
		{
			TexturesBasePath = props.TexturesBasePath;
		}
		if (Textures == null)
		{
			Textures = new Dictionary<string, CompositeTexture>();
			foreach (KeyValuePair<string, CompositeTexture> texture in props.Textures)
			{
				Textures[texture.Key] = texture.Value.Clone();
			}
		}
		LeafParticlesTexture = props.LeafParticlesTexture?.Clone();
		BlossomParticlesTexture = props.BlossomParticlesTexture?.Clone();
	}

	public TextureAtlasPosition GetOrLoadTexture(ICoreClientAPI capi, string key)
	{
		if (Textures.TryGetValue(key, out var value))
		{
			if (value.Baked != null)
			{
				int textureSubId = value.Baked.TextureSubId;
				if (textureSubId > 0)
				{
					return capi.BlockTextureAtlas.Positions[textureSubId];
				}
			}
			value.Bake(capi.Assets);
			capi.BlockTextureAtlas.GetOrInsertTexture(value.Baked.BakedName, out var textureSubId2, out var texPos);
			value.Baked.TextureSubId = textureSubId2;
			return texPos;
		}
		return null;
	}
}
