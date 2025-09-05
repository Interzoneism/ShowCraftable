using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class ContainedTextureSource : ITexPositionSource
{
	private ITextureAtlasAPI targetAtlas;

	private ICoreClientAPI capi;

	public Dictionary<string, AssetLocation> Textures = new Dictionary<string, AssetLocation>();

	private string sourceForErrorLogging;

	public Size2i AtlasSize => targetAtlas.Size;

	public TextureAtlasPosition this[string textureCode] => getOrCreateTexPos(Textures[textureCode]);

	public ContainedTextureSource(ICoreClientAPI capi, ITextureAtlasAPI targetAtlas, Dictionary<string, AssetLocation> textures, string sourceForErrorLogging)
	{
		this.capi = capi;
		this.targetAtlas = targetAtlas;
		Textures = textures;
		this.sourceForErrorLogging = sourceForErrorLogging;
	}

	protected TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
	{
		TextureAtlasPosition texPos = targetAtlas[texturePath];
		if (texPos == null)
		{
			IAsset texAsset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
			if (texAsset != null)
			{
				targetAtlas.GetOrInsertTexture(texturePath, out var _, out texPos, () => texAsset.ToBitmap(capi));
				if (texPos == null)
				{
					capi.World.Logger.Error("{0}, require texture {1} which exists, but unable to upload it or allocate space", sourceForErrorLogging, texturePath);
					texPos = targetAtlas.UnknownTexturePosition;
				}
			}
			else
			{
				capi.World.Logger.Error("{0}, require texture {1}, but no such texture found.", sourceForErrorLogging, texturePath);
				texPos = targetAtlas.UnknownTexturePosition;
			}
		}
		return texPos;
	}
}
