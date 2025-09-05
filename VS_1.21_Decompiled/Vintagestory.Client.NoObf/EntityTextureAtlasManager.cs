using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Client.NoObf;

public class EntityTextureAtlasManager : TextureAtlasManager, ITextureAtlasAPI
{
	public EntityTextureAtlasManager(ClientMain game)
		: base(game)
	{
	}

	internal void CollectTextures(List<EntityProperties> entityClasses)
	{
		CompositeTexture value = new CompositeTexture(new AssetLocation("unknown"));
		foreach (EntityProperties entityClass in entityClasses)
		{
			if (game.disposed)
			{
				return;
			}
			if (entityClass == null || entityClass.Client == null)
			{
				continue;
			}
			EntityClientProperties client = entityClass.Client;
			IDictionary<string, CompositeTexture> dictionary = new FastSmallDictionary<string, CompositeTexture>(1);
			if (client.Textures == null && client.LoadedShape?.Textures == null)
			{
				client.Textures["all"] = value;
			}
			if (client.LoadedShape?.Textures != null)
			{
				LoadShapeTextures(dictionary, client.LoadedShape, client.Shape);
			}
			if (client.LoadedAlternateShapes != null)
			{
				for (int i = 0; i < client.LoadedAlternateShapes.Length; i++)
				{
					Shape shape = client.LoadedAlternateShapes[i];
					CompositeShape cshape = client.Shape.Alternates[i];
					if (shape?.Textures != null)
					{
						LoadShapeTextures(dictionary, shape, cshape);
					}
				}
			}
			ResolveTextureCodes(client, client.LoadedShape);
			if (client.Textures != null)
			{
				foreach (KeyValuePair<string, CompositeTexture> texture in client.Textures)
				{
					texture.Value.Bake(game.AssetManager);
					if (texture.Value.Baked.BakedVariants != null)
					{
						for (int j = 0; j < texture.Value.Baked.BakedVariants.Length; j++)
						{
							GetOrAddTextureLocation(new AssetLocationAndSource(texture.Value.Baked.BakedVariants[j].BakedName, "Entity type ", entityClass.Code));
						}
					}
					GetOrAddTextureLocation(new AssetLocationAndSource(texture.Value.Base, "Entity type ", entityClass.Code));
					dictionary[texture.Key] = texture.Value;
				}
			}
			client.Textures = dictionary;
		}
		foreach (EntityProperties entityClass2 in entityClasses)
		{
			if (entityClass2 == null || entityClass2.Client == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, CompositeTexture> texture2 in entityClass2.Client.Textures)
			{
				BakedCompositeTexture baked = texture2.Value.Baked;
				baked.TextureSubId = textureNamesDict[texture2.Value.Baked.BakedName];
				if (baked.BakedVariants != null)
				{
					for (int k = 0; k < baked.BakedVariants.Length; k++)
					{
						baked.BakedVariants[k].TextureSubId = textureNamesDict[baked.BakedVariants[k].BakedName];
					}
				}
			}
		}
	}

	private void LoadShapeTextures(IDictionary<string, CompositeTexture> collectedTextures, Shape shape, CompositeShape cshape)
	{
		foreach (KeyValuePair<string, AssetLocation> texture in shape.Textures)
		{
			CompositeTexture compositeTexture = new CompositeTexture
			{
				Base = texture.Value
			};
			compositeTexture.Bake(game.AssetManager);
			if (compositeTexture.Baked.BakedVariants != null)
			{
				for (int i = 0; i < compositeTexture.Baked.BakedVariants.Length; i++)
				{
					GetOrAddTextureLocation(new AssetLocationAndSource(compositeTexture.Baked.BakedVariants[i].BakedName, "Shape file ", cshape.Base));
				}
			}
			else
			{
				GetOrAddTextureLocation(new AssetLocationAndSource(texture.Value, "Shape file ", cshape.Base));
				collectedTextures[texture.Key] = compositeTexture;
			}
		}
	}

	public void ResolveTextureCodes(EntityClientProperties typeClient, Shape shape)
	{
		if (typeClient.Textures.ContainsKey("all"))
		{
			LoadShapeTextureCodes(shape);
		}
		ResolveTextureDict((FastSmallDictionary<string, CompositeTexture>)typeClient.Textures);
	}
}
