using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ItemTextureAtlasManager : TextureAtlasManager, IItemTextureAtlasAPI, ITextureAtlasAPI
{
	public ItemTextureAtlasManager(ClientMain game)
		: base(game)
	{
	}

	internal void CollectTextures(IList<Item> items, Dictionary<AssetLocation, UnloadableShape> shapes)
	{
		CompositeTexture value = new CompositeTexture(new AssetLocation("unknown"));
		AssetManager assetManager = game.Platform.AssetManager;
		foreach (Item item in items)
		{
			if (game.disposed)
			{
				return;
			}
			if (item.Code == null)
			{
				continue;
			}
			ResolveTextureCodes(item, shapes);
			if (item.FirstTexture == null)
			{
				item.Textures["all"] = value;
			}
			foreach (KeyValuePair<string, CompositeTexture> texture in item.Textures)
			{
				texture.Value.Bake(game.Platform.AssetManager);
				if (!ContainsKey(texture.Value.Baked.BakedName))
				{
					SetTextureLocation(new AssetLocationAndSource(texture.Value.Baked.BakedName, "Item ", item.Code));
				}
			}
			if (!(item.Shape?.Base != null) || !shapes.TryGetValue(item.Shape.Base, out var value2))
			{
				continue;
			}
			Dictionary<string, AssetLocation> textures = value2.Textures;
			if (textures == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, AssetLocation> item2 in textures)
			{
				if (!ContainsKey(item2.Value))
				{
					SetTextureLocation(new AssetLocationAndSource(item2.Value, "Shape file ", item.Shape.Base));
				}
				if (!item.Textures.ContainsKey(item2.Key))
				{
					CompositeTexture compositeTexture = new CompositeTexture
					{
						Base = item2.Value.Clone()
					};
					item.Textures[item2.Key] = compositeTexture;
					compositeTexture.Bake(assetManager);
				}
			}
		}
		foreach (Item item3 in items)
		{
			if (item3 == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, CompositeTexture> texture2 in item3.Textures)
			{
				texture2.Value.Baked.TextureSubId = textureNamesDict[texture2.Value.Baked.BakedName];
			}
		}
	}

	public TextureAtlasPosition GetPosition(Item item, string textureName = null, bool returnNullWhenMissing = false)
	{
		if (item.Shape == null || item.Shape.VoxelizeTexture)
		{
			CompositeTexture value = item.FirstTexture;
			if (item.Shape?.Base != null && !item.Textures.TryGetValue(item.Shape.Base.Path.ToString(), out value))
			{
				value = item.FirstTexture;
			}
			int num = value.Baked.TextureSubId;
			return TextureAtlasPositionsByTextureSubId[num];
		}
		return new TextureSource(game, base.Size, item)
		{
			returnNullWhenMissing = returnNullWhenMissing
		}[textureName];
	}

	public void ResolveTextureCodes(Item item, Dictionary<AssetLocation, UnloadableShape> itemShapes)
	{
		if (item.Shape?.Base == null)
		{
			return;
		}
		if (!itemShapes.TryGetValue(item.Shape.Base, out var value))
		{
			game.Logger.VerboseDebug(string.Concat("Not found item shape ", item.Shape.Base, ", for item ", item.Code));
			return;
		}
		item.CheckTextures(game.Logger);
		if (value.Textures == null)
		{
			return;
		}
		foreach (KeyValuePair<string, AssetLocation> texture in value.Textures)
		{
			string key = texture.Key;
			if (item.Textures.TryGetValue(key, out var value2))
			{
				if (value2.Base.Path == "inherit")
				{
					value2.Base = texture.Value.Clone();
				}
				if (value2.BlendedOverlays == null)
				{
					continue;
				}
				BlendedOverlayTexture[] blendedOverlays = value2.BlendedOverlays;
				for (int i = 0; i < blendedOverlays.Length; i++)
				{
					if (blendedOverlays[i].Base.Path == "inherit")
					{
						blendedOverlays[i].Base = texture.Value.Clone();
					}
				}
			}
			else
			{
				item.Textures[key] = new CompositeTexture
				{
					Base = texture.Value.Clone()
				};
			}
		}
	}
}
