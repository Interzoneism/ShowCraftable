using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class BlockTextureAtlasManager : TextureAtlasManager, IBlockTextureAtlasAPI, ITextureAtlasAPI
{
	private List<KeyValuePair<string, CompositeTexture>> replacements = new List<KeyValuePair<string, CompositeTexture>>();

	public BlockTextureAtlasManager(ClientMain game)
		: base(game)
	{
	}

	internal void CollectTextures(IList<Block> blocks, OrderedDictionary<AssetLocation, UnloadableShape> shapes)
	{
		Block block = null;
		int snowtextureSubid = 0;
		AssetLocation other = new AssetLocation("snowlayer-1");
		Dictionary<AssetLocation, CompositeTexture> basicTexturesCache = new Dictionary<AssetLocation, CompositeTexture>();
		foreach (Block block2 in blocks)
		{
			if (!(block2.Code == null))
			{
				block2.EnsureValidTextures(game.Logger);
				ResolveTextureCodes(block2, shapes, basicTexturesCache);
			}
		}
		foreach (Block block3 in blocks)
		{
			if (game.disposed)
			{
				return;
			}
			if (!(block3.Code == null) && block3.DrawType == EnumDrawType.TopSoil)
			{
				collectTexturesForBlock(block3, shapes);
			}
		}
		foreach (Block block4 in blocks)
		{
			if (game.disposed)
			{
				return;
			}
			if (!(block4.Code == null) && block4.DrawType != EnumDrawType.TopSoil)
			{
				collectTexturesForBlock(block4, shapes);
				if (block == null && block4.Code.Equals(other))
				{
					block = block4;
					compose(block, 0);
					snowtextureSubid = block?.Textures[BlockFacing.UP.Code].Baked.TextureSubId ?? 0;
				}
			}
		}
		foreach (Block block5 in blocks)
		{
			if (block5 != null && !(block5.Code == null))
			{
				compose(block5, snowtextureSubid);
			}
		}
	}

	private void collectTexturesForBlock(Block block, OrderedDictionary<AssetLocation, UnloadableShape> shapes)
	{
		block.OnCollectTextures(game.api, this);
		if (block.Shape != null)
		{
			collectAndBakeTexturesFromShape(block, block.Shape, inv: false, shapes);
		}
		if (block.ShapeInventory != null)
		{
			collectAndBakeTexturesFromShape(block, block.ShapeInventory, inv: true, shapes);
		}
	}

	private void compose(Block block, int snowtextureSubid)
	{
		int blockId = block.BlockId;
		foreach (KeyValuePair<string, CompositeTexture> item in block.TexturesInventory)
		{
			item.Value.Baked.TextureSubId = textureNamesDict[item.Value.Baked.BakedName];
		}
		foreach (KeyValuePair<string, CompositeTexture> texture in block.Textures)
		{
			BakedCompositeTexture baked = texture.Value.Baked;
			baked.TextureSubId = textureNamesDict[baked.BakedName];
			if (baked.BakedVariants != null)
			{
				for (int i = 0; i < baked.BakedVariants.Length; i++)
				{
					baked.BakedVariants[i].TextureSubId = textureNamesDict[baked.BakedVariants[i].BakedName];
				}
			}
			if (baked.BakedTiles != null)
			{
				for (int j = 0; j < baked.BakedTiles.Length; j++)
				{
					baked.BakedTiles[j].TextureSubId = textureNamesDict[baked.BakedTiles[j].BakedName];
				}
			}
		}
		if (block.DrawType != EnumDrawType.JSON)
		{
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing blockFacing in aLLFACES)
			{
				if (block.Textures.TryGetValue(blockFacing.Code, out var value))
				{
					int num = value.Baked.TextureSubId;
					game.FastBlockTextureSubidsByBlockAndFace[blockId][blockFacing.Index] = num;
				}
			}
			if (block.Textures.TryGetValue("specialSecondTexture", out var value2))
			{
				game.FastBlockTextureSubidsByBlockAndFace[blockId][6] = value2.Baked.TextureSubId;
			}
			else
			{
				game.FastBlockTextureSubidsByBlockAndFace[blockId][6] = game.FastBlockTextureSubidsByBlockAndFace[blockId][BlockFacing.UP.Index];
			}
		}
		if (block.DrawType == EnumDrawType.JSONAndSnowLayer || block.DrawType == EnumDrawType.CrossAndSnowlayer || block.DrawType == EnumDrawType.CrossAndSnowlayer_2 || block.DrawType == EnumDrawType.CrossAndSnowlayer_3 || block.DrawType == EnumDrawType.CrossAndSnowlayer_4)
		{
			game.FastBlockTextureSubidsByBlockAndFace[blockId][6] = snowtextureSubid;
		}
	}

	private void collectAndBakeTexturesFromShape(Block block, CompositeShape shape, bool inv, OrderedDictionary<AssetLocation, UnloadableShape> shapes)
	{
		if (shapes.TryGetValue(shape.Base, out var value))
		{
			IDictionary<string, CompositeTexture> targetDict = (inv ? block.TexturesInventory : block.Textures);
			CollectAndBakeTexturesFromShape(value, targetDict, shape.Base);
		}
		if (shape.BakedAlternates != null)
		{
			CompositeShape[] bakedAlternates = shape.BakedAlternates;
			foreach (CompositeShape shape2 in bakedAlternates)
			{
				collectAndBakeTexturesFromShape(block, shape2, inv, shapes);
			}
		}
	}

	public override void CollectAndBakeTexturesFromShape(Shape compositeShape, IDictionary<string, CompositeTexture> targetDict, AssetLocation baseLoc)
	{
		Dictionary<string, AssetLocation> textures = compositeShape.Textures;
		if (textures == null)
		{
			return;
		}
		foreach (KeyValuePair<string, AssetLocation> item in textures)
		{
			if (!targetDict.ContainsKey(item.Key))
			{
				CompositeTexture compositeTexture = new CompositeTexture(item.Value);
				compositeTexture.Bake(game.AssetManager);
				AssetLocationAndSource assetLocationAndSource = new AssetLocationAndSource(compositeTexture.Baked.BakedName, "Shape file ", baseLoc);
				if (item.Key == "specialSecondTexture")
				{
					assetLocationAndSource.AddToAllAtlasses = true;
				}
				compositeTexture.Baked.TextureSubId = GetOrAddTextureLocation(assetLocationAndSource);
				targetDict[item.Key] = compositeTexture;
			}
		}
	}

	public void ResolveTextureCodes(Block block, OrderedDictionary<AssetLocation, UnloadableShape> blockShapes, Dictionary<AssetLocation, CompositeTexture> basicTexturesCache)
	{
		blockShapes.TryGetValue(block.Shape.Base, out var value);
		UnloadableShape unloadableShape = ((block.ShapeInventory == null) ? null : blockShapes[block.ShapeInventory.Base]);
		if (value != null && !value.Loaded)
		{
			value.Load(game, new AssetLocationAndSource(block.Shape.Base));
		}
		if (unloadableShape != null && !unloadableShape.Loaded)
		{
			unloadableShape.Load(game, new AssetLocationAndSource(block.Shape.Base));
		}
		bool flag = block.Textures.ContainsKey("all");
		bool flag2 = block.TexturesInventory.ContainsKey("all");
		if (flag || flag2)
		{
			LoadAllTextureCodes(block, value);
		}
		if (block.Textures.Count > 0)
		{
			ResolveTextureDict((TextureDictionary)block.Textures);
		}
		if (block.TexturesInventory.Count > 0)
		{
			ResolveTextureDict((TextureDictionary)block.TexturesInventory);
		}
		if (value?.Textures != null)
		{
			if (value.TexturesResolved == null)
			{
				value.ResolveTextures(basicTexturesCache);
			}
			foreach (KeyValuePair<string, CompositeTexture> item in value.TexturesResolved)
			{
				string key = item.Key;
				CompositeTexture value2 = item.Value;
				if (block.Textures.TryGetValue(key, out var value3))
				{
					if (value3.Base.Path == "inherit")
					{
						value3.Base = value2.Base;
					}
					if (value3.BlendedOverlays == null)
					{
						continue;
					}
					BlendedOverlayTexture[] blendedOverlays = value3.BlendedOverlays;
					for (int i = 0; i < blendedOverlays.Length; i++)
					{
						if (blendedOverlays[i].Base.Path == "inherit")
						{
							blendedOverlays[i].Base = value2.Base;
						}
					}
				}
				else if (!flag || !(key == "all"))
				{
					block.Textures.Add(key, value2);
				}
			}
		}
		replacements.Clear();
		foreach (KeyValuePair<string, CompositeTexture> texture in block.Textures)
		{
			CompositeTexture compositeTexture = texture.Value;
			if (compositeTexture.IsBasic())
			{
				if (basicTexturesCache.TryGetValue(compositeTexture.Base, out var value4))
				{
					if (compositeTexture != value4)
					{
						replacements.Add(new KeyValuePair<string, CompositeTexture>(texture.Key, value4));
						compositeTexture = value4;
					}
				}
				else
				{
					basicTexturesCache.Add(compositeTexture.Base, compositeTexture);
				}
			}
			((TextureDictionary)block.TexturesInventory).AddIfNotPresent(texture.Key, compositeTexture);
		}
		foreach (KeyValuePair<string, CompositeTexture> replacement in replacements)
		{
			block.Textures[replacement.Key] = replacement.Value;
		}
		if (unloadableShape == null || unloadableShape.Textures == null)
		{
			return;
		}
		if (unloadableShape.TexturesResolved == null)
		{
			unloadableShape.ResolveTextures(basicTexturesCache);
		}
		foreach (KeyValuePair<string, CompositeTexture> item2 in unloadableShape.TexturesResolved)
		{
			string key2 = item2.Key;
			if (!flag2 || !(key2 == "all"))
			{
				((TextureDictionary)block.TexturesInventory).AddIfNotPresent(key2, item2.Value);
			}
		}
	}

	public void LoadAllTextureCodes(Block block, Shape blockShape)
	{
		LoadShapeTextureCodes(blockShape);
		if (block.DrawType == EnumDrawType.Cube)
		{
			textureCodes.Add("west");
			textureCodes.Add("east");
			textureCodes.Add("north");
			textureCodes.Add("south");
			textureCodes.Add("up");
			textureCodes.Add("down");
		}
	}

	public TextureAtlasPosition GetPosition(Block block, string textureName, bool returnNullWhenMissing = false)
	{
		return new TextureSource(game, base.Size, block)
		{
			returnNullWhenMissing = returnNullWhenMissing
		}[textureName];
	}

	public override TextureAtlas RuntimeCreateNewAtlas(string itemclass)
	{
		TextureAtlas textureAtlas = base.RuntimeCreateNewAtlas(itemclass);
		int[] textureIds = game.TerrainChunkTesselator.RuntimeCreateNewBlockTextureAtlas(textureAtlas.textureId);
		game.chunkRenderer.RuntimeAddBlockTextureAtlas(textureIds);
		return textureAtlas;
	}
}
