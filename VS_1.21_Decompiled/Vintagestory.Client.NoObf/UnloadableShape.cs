using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class UnloadableShape : Shape
{
	public bool Loaded;

	public IDictionary<string, CompositeTexture> TexturesResolved;

	public void Unload()
	{
		Loaded = false;
		Textures = null;
		Elements = null;
		Animations = null;
		AnimationsByCrc32 = null;
		JointsById = null;
	}

	public bool Load(ClientMain game, AssetLocationAndSource srcandLoc)
	{
		Loaded = true;
		AssetLocation assetLocation = srcandLoc.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");
		IAsset asset = ScreenManager.Platform.AssetManager.TryGet(assetLocation);
		if (asset == null)
		{
			game.Platform.Logger.Warning("Did not find required shape {0} anywhere. (defined in {1})", assetLocation, srcandLoc.Source);
			return false;
		}
		try
		{
			ShapeElement.locationForLogging = assetLocation;
			JsonUtil.PopulateObject(this, Asset.BytesToString(asset.Data), asset.Location.Domain);
			return true;
		}
		catch (Exception ex)
		{
			game.Platform.Logger.Warning("Failed parsing shape model {0}\n{1}", assetLocation, ex.Message);
			return false;
		}
	}

	public void ResolveTextures(Dictionary<AssetLocation, CompositeTexture> shapeTexturesCache)
	{
		FastSmallDictionary<string, CompositeTexture> fastSmallDictionary = new FastSmallDictionary<string, CompositeTexture>(Textures.Count);
		foreach (KeyValuePair<string, AssetLocation> texture in Textures)
		{
			AssetLocation value = texture.Value;
			if (!shapeTexturesCache.TryGetValue(value, out var value2))
			{
				value2 = (shapeTexturesCache[value] = new CompositeTexture
				{
					Base = value
				});
			}
			fastSmallDictionary.Add(texture.Key, value2);
		}
		TexturesResolved = fastSmallDictionary;
	}
}
