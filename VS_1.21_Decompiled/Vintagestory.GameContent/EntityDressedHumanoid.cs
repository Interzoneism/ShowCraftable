using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityDressedHumanoid : EntityHumanoid
{
	private EntityBehaviorVillagerInv ebhv;

	private HumanoidOutfits humanoidOutfits;

	public Dictionary<string, WeightedCode[]> partialRandomOutfitsOverride;

	public override ItemSlot RightHandItemSlot => ebhv?.Inventory[0];

	public override ItemSlot LeftHandItemSlot => ebhv?.Inventory[1];

	public string OutfitConfigFileName => base.Properties.Attributes["outfitConfigFileName"].AsString("traderaccessories");

	public string[] OutfitSlots
	{
		get
		{
			return (WatchedAttributes["outfitslots"] as StringArrayAttribute)?.value;
		}
		set
		{
			if (value == null)
			{
				WatchedAttributes.RemoveAttribute("outfitslots");
			}
			else
			{
				WatchedAttributes["outfitslots"] = new StringArrayAttribute(value);
			}
			WatchedAttributes.MarkPathDirty("outfitslots");
		}
	}

	public string[] OutfitCodes
	{
		get
		{
			return (WatchedAttributes["outfitcodes"] as StringArrayAttribute)?.value;
		}
		set
		{
			if (value == null)
			{
				WatchedAttributes.RemoveAttribute("outfitcodes");
			}
			else
			{
				for (int i = 0; i < value.Length; i++)
				{
					if (value[i] == null)
					{
						value[i] = "";
					}
				}
				WatchedAttributes["outfitcodes"] = new StringArrayAttribute(value);
			}
			WatchedAttributes.MarkPathDirty("outfitcodes");
		}
	}

	public void LoadOutfitCodes()
	{
		if (Api.Side != EnumAppSide.Server)
		{
			return;
		}
		Dictionary<string, string> dictionary = base.Properties.Attributes["outfit"].AsObject<Dictionary<string, string>>();
		if (dictionary != null)
		{
			OutfitCodes = dictionary.Values.ToArray();
			OutfitSlots = dictionary.Keys.ToArray();
			return;
		}
		if (partialRandomOutfitsOverride == null)
		{
			partialRandomOutfitsOverride = base.Properties.Attributes["partialRandomOutfits"].AsObject<Dictionary<string, WeightedCode[]>>();
		}
		Dictionary<string, string> randomOutfit = humanoidOutfits.GetRandomOutfit(OutfitConfigFileName, partialRandomOutfitsOverride);
		OutfitSlots = randomOutfit.Keys.ToArray();
		OutfitCodes = randomOutfit.Values.ToArray();
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		humanoidOutfits = Api.ModLoader.GetModSystem<HumanoidOutfits>();
		if (api.Side == EnumAppSide.Server)
		{
			if (OutfitCodes == null)
			{
				LoadOutfitCodes();
			}
		}
		else
		{
			WatchedAttributes.RegisterModifiedListener("outfitcodes", onOutfitsChanged);
		}
		ebhv = GetBehavior<EntityBehaviorVillagerInv>();
	}

	private void onOutfitsChanged()
	{
		MarkShapeModified();
	}

	public override void OnTesselation(ref Shape entityShape, string shapePathForLogging)
	{
		ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
		FastSmallDictionary<string, CompositeTexture> fastSmallDictionary = new FastSmallDictionary<string, CompositeTexture>(0);
		base.Properties.Client.Textures = fastSmallDictionary;
		foreach (KeyValuePair<string, CompositeTexture> texture in Api.World.GetEntityType(Code).Client.Textures)
		{
			fastSmallDictionary[texture.Key] = texture.Value;
			texture.Value.Bake(coreClientAPI.Assets);
		}
		Shape entityShape2 = (entityShape = entityShape.Clone());
		string[] outfitCodes = OutfitCodes;
		TexturedWeightedCompositeShape[] array = humanoidOutfits.Outfit2Shapes(OutfitConfigFileName, OutfitCodes);
		string[] outfitSlots = OutfitSlots;
		if (outfitSlots != null)
		{
			for (int i = 0; i < outfitSlots.Length && i < array.Length; i++)
			{
				TexturedWeightedCompositeShape texturedWeightedCompositeShape = array[i];
				if (texturedWeightedCompositeShape != null && !(texturedWeightedCompositeShape.Base == null))
				{
					addGearToShape(outfitSlots[i], texturedWeightedCompositeShape, entityShape2, shapePathForLogging, null, texturedWeightedCompositeShape.Textures);
				}
			}
			foreach (KeyValuePair<string, AssetLocation> texture2 in entityShape.Textures)
			{
				if (!fastSmallDictionary.ContainsKey(texture2.Key))
				{
					CompositeTexture compositeTexture = new CompositeTexture(texture2.Value);
					compositeTexture.Bake(coreClientAPI.Assets);
					fastSmallDictionary[texture2.Key] = compositeTexture;
				}
			}
		}
		for (int j = 0; j < outfitCodes.Length; j++)
		{
			TexturedWeightedCompositeShape texturedWeightedCompositeShape2 = array[j];
			if (texturedWeightedCompositeShape2 == null)
			{
				continue;
			}
			if (texturedWeightedCompositeShape2.DisableElements != null)
			{
				entityShape.RemoveElements(texturedWeightedCompositeShape2.DisableElements);
			}
			if (texturedWeightedCompositeShape2.OverrideTextures == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, AssetLocation> overrideTexture in texturedWeightedCompositeShape2.OverrideTextures)
			{
				AssetLocation value = overrideTexture.Value;
				entityShape.Textures[overrideTexture.Key] = value;
				fastSmallDictionary[overrideTexture.Key] = CreateCompositeTexture(value, coreClientAPI, new SourceStringComponents("Outfit config file {0}, Outfit slot {1}, Outfit type {2}, Override Texture {3}", OutfitConfigFileName, OutfitSlots[j], OutfitCodes[j], overrideTexture.Key));
			}
		}
		bool shapeIsCloned = true;
		base.OnTesselation(ref entityShape, shapePathForLogging, ref shapeIsCloned);
	}

	private CompositeTexture CreateCompositeTexture(AssetLocation loc, ICoreClientAPI capi, SourceStringComponents sourceForLogging)
	{
		CompositeTexture compositeTexture = new CompositeTexture(loc);
		compositeTexture.Bake(capi.Assets);
		capi.EntityTextureAtlas.GetOrInsertTexture(new AssetLocationAndSource(compositeTexture.Baked.TextureFilenames[0], sourceForLogging), out var textureSubId, out var _);
		compositeTexture.Baked.TextureSubId = textureSubId;
		return compositeTexture;
	}

	protected void addGearToShape(string prefixcode, CompositeShape cshape, Shape entityShape, string shapePathForLogging, string[] disableElements = null, Dictionary<string, AssetLocation> textureOverrides = null)
	{
		if (disableElements != null)
		{
			entityShape.RemoveElements(disableElements);
		}
		AssetLocation shapePath = cshape.Base.CopyWithPath("shapes/" + cshape.Base.Path + ".json");
		Shape shape = Shape.TryGet(Api, shapePath);
		if (shape == null)
		{
			Api.World.Logger.Warning("Compositshape {0} (code: {2}) defined but not found or errored, was supposed to be at {1}. Part will be invisible.", cshape.Base, shapePath, prefixcode);
			return;
		}
		if (prefixcode != null && prefixcode.Length > 0)
		{
			prefixcode += "-";
		}
		if (textureOverrides != null)
		{
			foreach (KeyValuePair<string, AssetLocation> textureOverride in textureOverrides)
			{
				shape.Textures[prefixcode + textureOverride.Key] = textureOverride.Value;
			}
		}
		foreach (KeyValuePair<string, AssetLocation> texture in shape.Textures)
		{
			entityShape.TextureSizes[prefixcode + texture.Key] = new int[2] { shape.TextureWidth, shape.TextureHeight };
		}
		ICoreClientAPI capi = Api as ICoreClientAPI;
		IDictionary<string, CompositeTexture> clientTextures = base.Properties.Client.Textures;
		shape.SubclassForStepParenting(prefixcode);
		shape.ResolveReferences(Api.Logger, shapePath);
		entityShape.StepParentShape(shape, shapePath.ToShortString(), shapePathForLogging, Api.Logger, delegate(string texcode, AssetLocation loc)
		{
			string key = prefixcode + texcode;
			if (!clientTextures.ContainsKey(key))
			{
				clientTextures[key] = CreateCompositeTexture(loc, capi, new SourceStringComponents("Humanoid outfit", shapePath));
			}
		});
	}
}
