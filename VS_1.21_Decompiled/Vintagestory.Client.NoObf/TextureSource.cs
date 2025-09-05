using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class TextureSource : ITexPositionSource
{
	private ClientMain game;

	public Size2i atlasSize;

	public Entity entity;

	public Block block;

	public Item item;

	private MiniDictionary textureCodeToIdMapping;

	public bool isDecalUv;

	public bool returnNullWhenMissing;

	internal CompositeShape blockShape;

	public TextureAtlasManager atlasMgr;

	public Size2i AtlasSize => atlasSize;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (textureCode == null)
			{
				return atlasMgr.UnknownTexturePos;
			}
			int num = textureCodeToIdMapping[textureCode];
			TextureAtlasPosition textureAtlasPosition;
			if (num == -1 && (returnNullWhenMissing || (num = textureCodeToIdMapping["all"]) == -1))
			{
				if (returnNullWhenMissing)
				{
					return null;
				}
				if (block != null)
				{
					game.Platform.Logger.Error(string.Concat("Missing mapping for texture code #", textureCode, " during shape tesselation of block ", block.Code, " using shape ", block.Shape.Base, ", or one of its alternates"));
				}
				if (item != null)
				{
					game.Platform.Logger.Error(string.Concat("Missing mapping for texture code #", textureCode, " during shape tesselation of item ", item.Code, " using shape ", item.Shape.Base));
				}
				if (entity != null)
				{
					game.Platform.Logger.Error(string.Concat("Missing mapping for texture code #", textureCode, " during shape tesselation of entity ", entity.Code, " using shape ", entity.Properties.Client.Shape.Base, ", or one of its alternates"));
				}
				textureAtlasPosition = atlasMgr.UnknownTexturePos;
			}
			else
			{
				textureAtlasPosition = atlasMgr.TextureAtlasPositionsByTextureSubId[num];
			}
			if (isDecalUv)
			{
				return new TextureAtlasPosition
				{
					atlasNumber = 0,
					atlasTextureId = 0,
					x1 = 0f,
					y1 = 0f,
					x2 = (textureAtlasPosition.x2 - textureAtlasPosition.x1) * (float)atlasMgr.Size.Width / (float)atlasSize.Width,
					y2 = (textureAtlasPosition.y2 - textureAtlasPosition.y1) * (float)atlasMgr.Size.Height / (float)atlasSize.Height
				};
			}
			return textureAtlasPosition;
		}
	}

	public TextureSource(ClientMain game, Size2i atlasSize, Block block, bool forInventory = false)
	{
		this.game = game;
		this.atlasSize = atlasSize;
		this.block = block;
		atlasMgr = game.BlockAtlasManager;
		try
		{
			IDictionary<string, CompositeTexture> dictionary = block.Textures;
			if (forInventory)
			{
				dictionary = block.TexturesInventory;
			}
			textureCodeToIdMapping = new MiniDictionary(dictionary.Count);
			foreach (KeyValuePair<string, CompositeTexture> item in dictionary)
			{
				textureCodeToIdMapping[item.Key] = item.Value.Baked.TextureSubId;
			}
		}
		catch (Exception)
		{
			game.Logger.Error("Unable to initialize TextureSource for block {0}. Will crash now.", block?.Code);
			throw;
		}
	}

	public TextureSource(ClientMain game, Size2i atlasSize, Item item)
	{
		this.game = game;
		this.atlasSize = atlasSize;
		this.item = item;
		atlasMgr = game.ItemAtlasManager;
		Dictionary<string, CompositeTexture> textures = item.Textures;
		textureCodeToIdMapping = new MiniDictionary(textures.Count);
		foreach (KeyValuePair<string, CompositeTexture> item2 in textures)
		{
			textureCodeToIdMapping[item2.Key] = item2.Value.Baked.TextureSubId;
		}
	}

	public TextureSource(ClientMain game, Size2i atlasSize, Entity entity, Dictionary<string, CompositeTexture> extraTextures = null, int altTextureNumber = 0)
	{
		this.game = game;
		this.atlasSize = atlasSize;
		this.entity = entity;
		atlasMgr = game.EntityAtlasManager;
		IDictionary<string, CompositeTexture> textures = entity.Properties.Client.Textures;
		textureCodeToIdMapping = new MiniDictionary(textures.Count);
		foreach (KeyValuePair<string, CompositeTexture> item in textures)
		{
			BakedCompositeTexture baked = item.Value.Baked;
			if (baked.BakedVariants == null)
			{
				textureCodeToIdMapping[item.Key] = baked.TextureSubId;
				continue;
			}
			BakedCompositeTexture bakedCompositeTexture = baked.BakedVariants[altTextureNumber % baked.BakedVariants.Length];
			textureCodeToIdMapping[item.Key] = bakedCompositeTexture.TextureSubId;
		}
		if (extraTextures == null)
		{
			return;
		}
		foreach (KeyValuePair<string, CompositeTexture> extraTexture in extraTextures)
		{
			extraTextures[extraTexture.Key] = extraTexture.Value;
		}
	}

	public TextureSource(ClientMain game, Size2i atlasSize, Block block, int altTextureNumber)
		: this(game, atlasSize, block)
	{
		if (altTextureNumber == -1)
		{
			return;
		}
		foreach (KeyValuePair<string, CompositeTexture> texture in block.Textures)
		{
			BakedCompositeTexture baked = texture.Value.Baked;
			if (baked.BakedVariants != null)
			{
				BakedCompositeTexture bakedCompositeTexture = baked.BakedVariants[altTextureNumber % baked.BakedVariants.Length];
				textureCodeToIdMapping[texture.Key] = bakedCompositeTexture.TextureSubId;
			}
		}
	}

	public void UpdateVariant(Block block, int altTextureNumber)
	{
		foreach (KeyValuePair<string, CompositeTexture> texture in block.Textures)
		{
			BakedCompositeTexture[] bakedVariants = texture.Value.Baked.BakedVariants;
			if (bakedVariants != null && bakedVariants.Length != 0)
			{
				textureCodeToIdMapping[texture.Key] = bakedVariants[altTextureNumber % bakedVariants.Length].TextureSubId;
			}
		}
	}
}
