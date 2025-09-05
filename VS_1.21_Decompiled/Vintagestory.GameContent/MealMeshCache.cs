using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class MealMeshCache : ModSystem, ITexPositionSource
{
	private ICoreClientAPI? capi;

	private Block? mealtextureSourceBlock;

	private AssetLocation[] pieShapeLocByFillLevel = new AssetLocation[5]
	{
		new AssetLocation("block/food/pie/full-fill0"),
		new AssetLocation("block/food/pie/full-fill1"),
		new AssetLocation("block/food/pie/full-fill2"),
		new AssetLocation("block/food/pie/full-fill3"),
		new AssetLocation("block/food/pie/full-fill4")
	};

	private AssetLocation[] pieShapeBySize = new AssetLocation[4]
	{
		new AssetLocation("block/food/pie/quarter"),
		new AssetLocation("block/food/pie/half"),
		new AssetLocation("block/food/pie/threefourths"),
		new AssetLocation("block/food/pie/full")
	};

	protected Shape? nowTesselatingShape;

	private BlockPie? nowTesselatingBlock;

	private ItemStack[]? contentStacks;

	private AssetLocation? crustTextureLoc;

	private AssetLocation? fillingTextureLoc;

	private AssetLocation? topCrustTextureLoc;

	public AssetLocation[] pieMixedFillingTextures = new AssetLocation[6]
	{
		new AssetLocation("block/food/pie/fill-mixedfruit"),
		new AssetLocation("block/food/pie/fill-mixedvegetable"),
		new AssetLocation("block/food/pie/fill-mixedmeat"),
		new AssetLocation("block/food/pie/fill-mixedgrain"),
		new AssetLocation("block/food/pie/fill-mixedcheese"),
		new AssetLocation("block/food/pie/fill-unknown")
	};

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			AssetLocation assetLocation = crustTextureLoc;
			if (textureCode == "filling")
			{
				assetLocation = fillingTextureLoc;
			}
			if (textureCode == "topcrust")
			{
				assetLocation = topCrustTextureLoc;
			}
			if (assetLocation == null)
			{
				capi.World.Logger.Warning("Missing texture path for pie mesh texture code {0}, seems like a missing texture definition or invalid pie block.", textureCode);
				return capi.BlockTextureAtlas.UnknownTexturePosition;
			}
			TextureAtlasPosition texPos = capi.BlockTextureAtlas[assetLocation];
			if (texPos == null)
			{
				IAsset asset = capi.Assets.TryGet(assetLocation.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
				if (asset != null)
				{
					BitmapRef bmp = asset.ToBitmap(capi);
					capi.BlockTextureAtlas.GetOrInsertTexture(assetLocation, out var _, out texPos, () => bmp);
				}
				else
				{
					capi.World.Logger.Warning("Pie mesh texture {1} not found.", nowTesselatingBlock?.Code, assetLocation);
					texPos = capi.BlockTextureAtlas.UnknownTexturePosition;
				}
			}
			return texPos;
		}
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		capi = api;
		api.Event.LeaveWorld += Event_LeaveWorld;
		api.Event.BlockTexturesLoaded += Event_BlockTexturesLoaded;
	}

	private void Event_BlockTexturesLoaded()
	{
		mealtextureSourceBlock = capi.World.GetBlock(new AssetLocation("claypot-blue-cooked"));
	}

	public override void Dispose()
	{
		ICoreClientAPI? coreClientAPI = capi;
		if (coreClientAPI == null || !coreClientAPI.ObjectCache.TryGetValue("pieMeshRefs", out var value) || !(value is Dictionary<int, MultiTextureMeshRef> dictionary))
		{
			return;
		}
		foreach (KeyValuePair<int, MultiTextureMeshRef> item in dictionary)
		{
			item.Deconstruct(out var _, out var value2);
			value2.Dispose();
		}
		capi.ObjectCache.Remove("pieMeshRefs");
	}

	public MultiTextureMeshRef? GetOrCreatePieMeshRef(ItemStack? pieStack)
	{
		object value;
		Dictionary<int, MultiTextureMeshRef> dictionary = (Dictionary<int, MultiTextureMeshRef>)(capi.ObjectCache.TryGetValue("pieMeshRefs", out value) ? ((value as Dictionary<int, MultiTextureMeshRef>) ?? new Dictionary<int, MultiTextureMeshRef>()) : (capi.ObjectCache["pieMeshRefs"] = new Dictionary<int, MultiTextureMeshRef>()));
		if (!(pieStack?.Block is BlockPie blockPie))
		{
			return null;
		}
		ItemStack[] contents = blockPie.GetContents(capi.World, pieStack);
		string extraKey = "ct" + (BlockPie.GetTopCrustType(pieStack) ?? "full") + "-bl" + pieStack.Attributes.GetAsInt("bakeLevel") + "-ps" + pieStack.Attributes.GetAsInt("pieSize");
		int mealHashCode = GetMealHashCode(blockPie, contents, null, extraKey);
		if (!dictionary.TryGetValue(mealHashCode, out var value2))
		{
			MeshData pieMesh = GetPieMesh(pieStack);
			if (pieMesh == null)
			{
				return null;
			}
			value2 = (dictionary[mealHashCode] = capi.Render.UploadMultiTextureMesh(pieMesh));
		}
		return value2;
	}

	public MeshData? GetPieMesh(ItemStack? pieStack, ModelTransform? transform = null)
	{
		nowTesselatingBlock = pieStack?.Block as BlockPie;
		if (nowTesselatingBlock == null)
		{
			return null;
		}
		contentStacks = nowTesselatingBlock.GetContents(capi.World, pieStack);
		int num = pieStack?.Attributes.GetAsInt("pieSize") ?? 0;
		InPieProperties[] array = contentStacks.Select((ItemStack stack) => stack?.ItemAttributes?["inPieProperties"]?.AsObject<InPieProperties>(null, stack.Collectible.Code.Domain)).ToArray();
		int num2 = pieStack?.Attributes.GetAsInt("bakeLevel") ?? 0;
		if (array.Length == 0)
		{
			return null;
		}
		ItemStack itemStack = contentStacks[1];
		bool flag = true;
		int num3 = 2;
		while (flag && num3 < contentStacks.Length - 1)
		{
			if (contentStacks[num3] != null && itemStack != null)
			{
				flag &= itemStack.Equals(capi.World, contentStacks[num3], GlobalConstants.IgnoredStackAttributes);
				itemStack = contentStacks[num3];
			}
			num3++;
		}
		if (ContentsRotten(contentStacks))
		{
			crustTextureLoc = new AssetLocation("block/rot/rot");
			fillingTextureLoc = new AssetLocation("block/rot/rot");
			topCrustTextureLoc = new AssetLocation("block/rot/rot");
		}
		else
		{
			if (array[0] != null)
			{
				crustTextureLoc = array[0].Texture.Clone();
				crustTextureLoc.Path = crustTextureLoc.Path.Replace("{bakelevel}", (num2 + 1).ToString() ?? "");
				fillingTextureLoc = new AssetLocation("block/transparent");
			}
			topCrustTextureLoc = new AssetLocation("block/transparent");
			if (array[5] != null)
			{
				topCrustTextureLoc = array[5].Texture.Clone();
				topCrustTextureLoc.Path = topCrustTextureLoc.Path.Replace("{bakelevel}", (num2 + 1).ToString() ?? "");
			}
			if (contentStacks[1] != null)
			{
				EnumFoodCategory enumFoodCategory = BlockPie.FillingFoodCategory(contentStacks[1]);
				if (enumFoodCategory == EnumFoodCategory.NoNutrition)
				{
					enumFoodCategory = EnumFoodCategory.Unknown;
				}
				fillingTextureLoc = ((!flag) ? pieMixedFillingTextures[(int)enumFoodCategory] : array[1]?.Texture);
			}
		}
		int num4 = ((contentStacks[1] != null) ? 1 : 0) + ((contentStacks[2] != null) ? 1 : 0) + ((contentStacks[3] != null) ? 1 : 0) + ((contentStacks[4] != null) ? 1 : 0);
		AssetLocation assetLocation = ((num4 == 4) ? pieShapeBySize[num - 1] : pieShapeLocByFillLevel[num4]);
		assetLocation.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		Shape shapeBase = Shape.TryGet(capi, assetLocation);
		string shapeElement = BlockPie.TopCrustTypes.First((PieTopCrustType type) => type.Code.EqualsFast(BlockPie.GetTopCrustType(pieStack) ?? "full")).ShapeElement;
		string[] selectiveElements = new string[5] { "origin/base/crust regular/*", "origin/base/filling/*", "origin/base/base-quarter/*", "origin/base/fillingquarter/*", shapeElement };
		capi.Tesselator.TesselateShape("pie", shapeBase, out var modeldata, this, null, 0, 0, 0, null, selectiveElements);
		if (transform != null)
		{
			modeldata.ModelTransform(transform);
		}
		return modeldata;
	}

	public MultiTextureMeshRef? GetOrCreateMealInContainerMeshRef(Block containerBlock, CookingRecipe? forRecipe, ItemStack?[]? contentStacks, Vec3f? foodTranslate = null)
	{
		object value;
		Dictionary<int, MultiTextureMeshRef> dictionary = (Dictionary<int, MultiTextureMeshRef>)(capi.ObjectCache.TryGetValue("cookedMeshRefs", out value) ? ((value as Dictionary<int, MultiTextureMeshRef>) ?? new Dictionary<int, MultiTextureMeshRef>()) : (capi.ObjectCache["cookedMeshRefs"] = new Dictionary<int, MultiTextureMeshRef>()));
		if (contentStacks == null)
		{
			return null;
		}
		int mealHashCode = GetMealHashCode(containerBlock, contentStacks, foodTranslate);
		if (!dictionary.TryGetValue(mealHashCode, out var value2))
		{
			MeshData data = GenMealInContainerMesh(containerBlock, forRecipe, contentStacks, foodTranslate);
			value2 = (dictionary[mealHashCode] = capi.Render.UploadMultiTextureMesh(data));
		}
		return value2;
	}

	public MeshData GenMealInContainerMesh(Block containerBlock, CookingRecipe? forRecipe, ItemStack?[] contentStacks, Vec3f? foodTranslate = null)
	{
		CompositeShape shape = containerBlock.Shape;
		AssetLocation shapePath = shape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		Shape shapeBase = Shape.TryGet(capi, shapePath);
		capi.Tesselator.TesselateShape("meal", shapeBase, out var modeldata, capi.Tesselator.GetTextureSource(containerBlock), new Vec3f(shape.rotateX, shape.rotateY, shape.rotateZ), 0, 0, 0);
		MeshData meshData = GenMealMesh(forRecipe, contentStacks, foodTranslate);
		if (meshData != null)
		{
			modeldata.AddMeshData(meshData);
		}
		return modeldata;
	}

	public MeshData? GenMealMesh(CookingRecipe? forRecipe, ItemStack?[] contentStacks, Vec3f? foodTranslate = null)
	{
		MealTextureSource mealTextureSource;
		try
		{
			mealTextureSource = new MealTextureSource(capi, mealtextureSourceBlock);
		}
		catch
		{
			capi.Logger.Error("Unable to create meal texture source for recipe: " + forRecipe?.Code + " for: " + mealtextureSourceBlock?.Code.ToShortString());
			throw;
		}
		if (forRecipe != null)
		{
			MeshData meshData = GenFoodMixMesh(contentStacks, forRecipe, foodTranslate);
			if (meshData != null)
			{
				return meshData;
			}
		}
		if (contentStacks != null && contentStacks.Length != 0)
		{
			if (ContentsRotten(contentStacks))
			{
				Shape shapeBase = Shape.TryGet(capi, "shapes/block/food/meal/rot.json");
				capi.Tesselator.TesselateShape("rotcontents", shapeBase, out var modeldata, mealTextureSource, null, 0, 0, 0);
				if (foodTranslate != null)
				{
					modeldata.Translate(foodTranslate);
				}
				return modeldata;
			}
			if (contentStacks[0]?.ItemAttributes?["inContainerTexture"] != null)
			{
				mealTextureSource.ForStack = contentStacks[0];
				Shape shapeBase2 = Shape.TryGet(shapePath: (contentStacks[0].ItemAttributes["inBowlShape"]?.AsObject(new CompositeShape
				{
					Base = new AssetLocation("shapes/block/food/meal/pickled.json")
				}))?.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"), api: capi);
				capi.Tesselator.TesselateShape("picklednmealcontents", shapeBase2, out var modeldata2, mealTextureSource, null, 0, 0, 0);
				return modeldata2;
			}
		}
		return null;
	}

	public static bool ContentsRotten(ItemStack?[] contentStacks)
	{
		for (int i = 0; i < contentStacks.Length; i++)
		{
			if (contentStacks[i]?.Collectible.Code.Path == "rot")
			{
				return true;
			}
		}
		return false;
	}

	public static bool ContentsRotten(InventoryBase inv)
	{
		foreach (ItemSlot item in inv)
		{
			if (item.Itemstack?.Collectible.Code.Path == "rot")
			{
				return true;
			}
		}
		return false;
	}

	public MeshData? GenFoodMixMesh(ItemStack?[] contentStacks, CookingRecipe recipe, Vec3f? foodTranslate)
	{
		MeshData modeldata = null;
		MealTextureSource mealTextureSource = new MealTextureSource(capi, mealtextureSourceBlock);
		AssetLocation shapePath = recipe.Shape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		bool num = ContentsRotten(contentStacks);
		if (num)
		{
			shapePath = new AssetLocation("shapes/block/food/meal/rot.json");
		}
		Shape shapeBase = Shape.TryGet(capi, shapePath);
		Dictionary<CookingRecipeIngredient, int> dictionary = new Dictionary<CookingRecipeIngredient, int>();
		if (num)
		{
			capi.Tesselator.TesselateShape("mealpart", shapeBase, out modeldata, mealTextureSource, new Vec3f(recipe.Shape.rotateX, recipe.Shape.rotateY, recipe.Shape.rotateZ), 0, 0, 0);
		}
		else
		{
			HashSet<string> hashSet = new HashSet<string>();
			for (int i = 0; i < contentStacks.Length; i++)
			{
				mealTextureSource.ForStack = contentStacks[i];
				CookingRecipeIngredient ingrendientFor = recipe.GetIngrendientFor(contentStacks[i], (from val in dictionary
					where val.Key.MaxQuantity <= val.Value
					select val.Key).ToArray());
				if (ingrendientFor == null)
				{
					ingrendientFor = recipe.GetIngrendientFor(contentStacks[i]);
				}
				else
				{
					dictionary.TryGetValue(ingrendientFor, out var value);
					value = (dictionary[ingrendientFor] = value + 1);
				}
				if (ingrendientFor == null)
				{
					continue;
				}
				string[] selectiveElements = null;
				CookingRecipeStack matchingStack = ingrendientFor.GetMatchingStack(contentStacks[i]);
				if (matchingStack == null)
				{
					continue;
				}
				if (matchingStack.ShapeElement != null)
				{
					selectiveElements = new string[1] { matchingStack.ShapeElement };
				}
				mealTextureSource.customTextureMapping = matchingStack.TextureMapping;
				if (!hashSet.Contains(matchingStack.ShapeElement + matchingStack.TextureMapping))
				{
					hashSet.Add(matchingStack.ShapeElement + matchingStack.TextureMapping);
					capi.Tesselator.TesselateShape("mealpart", shapeBase, out var modeldata2, mealTextureSource, new Vec3f(recipe.Shape.rotateX, recipe.Shape.rotateY, recipe.Shape.rotateZ), 0, 0, 0, null, selectiveElements);
					if (modeldata == null)
					{
						modeldata = modeldata2;
					}
					else
					{
						modeldata.AddMeshData(modeldata2);
					}
				}
			}
		}
		if (foodTranslate != null)
		{
			modeldata?.Translate(foodTranslate);
		}
		return modeldata;
	}

	private void Event_LeaveWorld()
	{
		if (capi == null || !capi.ObjectCache.TryGetValue("cookedMeshRefs", out var value) || !(value is Dictionary<int, MultiTextureMeshRef> dictionary))
		{
			return;
		}
		foreach (KeyValuePair<int, MultiTextureMeshRef> item in dictionary)
		{
			item.Value.Dispose();
		}
		capi.ObjectCache.Remove("cookedMeshRefs");
	}

	public int GetMealHashCode(ItemStack stack, Vec3f? translate = null, string extraKey = "")
	{
		ItemStack[] array = (stack.Block as BlockContainer)?.GetContents(capi.World, stack);
		if (array == null)
		{
			return 0;
		}
		if (stack.Block is BlockPie)
		{
			extraKey = extraKey + "ct" + (BlockPie.GetTopCrustType(stack) ?? "full") + "-bl" + stack.Attributes.GetAsInt("bakeLevel") + "-ps" + stack.Attributes.GetAsInt("pieSize");
		}
		return GetMealHashCode(stack.Block, array, translate, extraKey);
	}

	protected int GetMealHashCode(Block block, ItemStack?[] contentStacks, Vec3f? translate = null, string? extraKey = null)
	{
		string text = block.Shape.ToString() + block.Code.ToShortString();
		if (translate != null)
		{
			text = text + translate.X + "/" + translate.Y + "/" + translate.Z;
		}
		string text2 = "";
		for (int i = 0; i < contentStacks.Length; i++)
		{
			if (contentStacks[i] != null)
			{
				if (contentStacks[i].Collectible.Code.Path == "rot")
				{
					return (text + "rotten").GetHashCode();
				}
				text2 += contentStacks[i].Collectible.Code.ToShortString();
			}
		}
		return (text + text2 + extraKey).GetHashCode();
	}
}
