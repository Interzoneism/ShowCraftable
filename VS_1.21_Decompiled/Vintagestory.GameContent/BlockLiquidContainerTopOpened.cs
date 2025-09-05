using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockLiquidContainerTopOpened : BlockLiquidContainerBase, IContainedMeshSource, IContainedCustomName
{
	private LiquidTopOpenContainerProps Props;

	private MeshData origcontainermesh;

	private Shape contentShape;

	private Shape liquidContentShape;

	protected virtual string meshRefsCacheKey => Code.ToShortString() + "meshRefs";

	protected virtual AssetLocation emptyShapeLoc => Props.EmptyShapeLoc;

	protected virtual AssetLocation contentShapeLoc => Props.OpaqueContentShapeLoc;

	protected virtual AssetLocation liquidContentShapeLoc => Props.LiquidContentShapeLoc;

	public override float TransferSizeLitres => Props.TransferSizeLitres;

	public override float CapacityLitres => Props.CapacityLitres;

	public override bool CanDrinkFrom => true;

	public override bool IsTopOpened => true;

	public override bool AllowHeldLiquidTransfer => true;

	protected virtual float liquidMaxYTranslate => Props.LiquidMaxYTranslate;

	protected virtual float liquidYTranslatePerLitre => liquidMaxYTranslate / CapacityLitres;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Props = new LiquidTopOpenContainerProps();
		JsonObject attributes = Attributes;
		if (attributes != null && attributes["liquidContainerProps"].Exists)
		{
			Props = Attributes["liquidContainerProps"].AsObject<LiquidTopOpenContainerProps>(null, Code.Domain);
		}
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		object value;
		Dictionary<int, MultiTextureMeshRef> dictionary = (Dictionary<int, MultiTextureMeshRef>)(capi.ObjectCache.TryGetValue(meshRefsCacheKey, out value) ? (value as Dictionary<int, MultiTextureMeshRef>) : (capi.ObjectCache[meshRefsCacheKey] = new Dictionary<int, MultiTextureMeshRef>()));
		ItemStack content = GetContent(itemstack);
		if (content != null)
		{
			int stackCacheHashCode = GetStackCacheHashCode(content);
			if (!dictionary.TryGetValue(stackCacheHashCode, out var value2))
			{
				MeshData data = GenMesh(capi, content);
				value2 = (dictionary[stackCacheHashCode] = capi.Render.UploadMultiTextureMesh(data));
			}
			renderinfo.ModelRef = value2;
		}
	}

	protected int GetStackCacheHashCode(ItemStack contentStack)
	{
		return (contentStack.StackSize + "x" + contentStack.Collectible.Code.ToShortString()).GetHashCode();
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (!(api is ICoreClientAPI coreClientAPI) || !coreClientAPI.ObjectCache.TryGetValue(meshRefsCacheKey, out var value))
		{
			return;
		}
		foreach (KeyValuePair<int, MultiTextureMeshRef> item in value as Dictionary<int, MultiTextureMeshRef>)
		{
			item.Value.Dispose();
		}
		coreClientAPI.ObjectCache.Remove(meshRefsCacheKey);
	}

	public MeshData GenMesh(ICoreClientAPI capi, ItemStack contentStack, BlockPos forBlockPos = null)
	{
		if (origcontainermesh == null)
		{
			Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, emptyShapeLoc.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));
			if (shape == null)
			{
				capi.World.Logger.Error("Empty shape {0} not found. Liquid container {1} will be invisible.", emptyShapeLoc, Code);
				return new MeshData();
			}
			capi.Tesselator.TesselateShape(this, shape, out origcontainermesh, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ));
		}
		MeshData meshData = origcontainermesh.Clone();
		if (contentStack != null)
		{
			WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(contentStack);
			if (containableProps == null)
			{
				capi.World.Logger.Error("Contents ('{0}') has no liquid properties, contents of liquid container {1} will be invisible.", contentStack.GetName(), Code);
				return meshData;
			}
			ContainerTextureSource texSource = new ContainerTextureSource(capi, contentStack, containableProps.Texture);
			Shape shape2 = (containableProps.IsOpaque ? contentShape : liquidContentShape);
			AssetLocation assetLocation = (containableProps.IsOpaque ? contentShapeLoc : liquidContentShapeLoc);
			if (shape2 == null)
			{
				shape2 = Vintagestory.API.Common.Shape.TryGet(capi, assetLocation.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));
				if (containableProps.IsOpaque)
				{
					contentShape = shape2;
				}
				else
				{
					liquidContentShape = shape2;
				}
			}
			if (shape2 == null)
			{
				capi.World.Logger.Error("Content shape {0} not found. Contents of liquid container {1} will be invisible.", assetLocation, Code);
				return meshData;
			}
			capi.Tesselator.TesselateShape(GetType().Name, shape2, out var modeldata, texSource, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ), containableProps.GlowLevel, 0, 0);
			modeldata.Translate(0f, GameMath.Min(liquidMaxYTranslate, (float)contentStack.StackSize / containableProps.ItemsPerLitre * liquidYTranslatePerLitre), 0f);
			if (containableProps.ClimateColorMap != null)
			{
				int color = ((!(forBlockPos != null)) ? capi.World.ApplyColorMapOnRgba(containableProps.ClimateColorMap, null, -1, 196, 128, flipRb: false) : capi.World.ApplyColorMapOnRgba(containableProps.ClimateColorMap, null, -1, forBlockPos.X, forBlockPos.Y, forBlockPos.Z, flipRb: false));
				byte[] array = ColorUtil.ToBGRABytes(color);
				for (int i = 0; i < modeldata.Rgba.Length; i++)
				{
					modeldata.Rgba[i] = (byte)(modeldata.Rgba[i] * array[i % 4] / 255);
				}
			}
			for (int j = 0; j < modeldata.FlagsCount; j++)
			{
				modeldata.Flags[j] = modeldata.Flags[j] & -4097;
			}
			meshData.AddMeshData(modeldata);
			if (forBlockPos != null)
			{
				meshData.CustomInts = new CustomMeshDataPartInt(meshData.FlagsCount);
				meshData.CustomInts.Count = meshData.FlagsCount;
				meshData.CustomInts.Values.Fill(268435456);
				meshData.CustomFloats = new CustomMeshDataPartFloat(meshData.FlagsCount * 2);
				meshData.CustomFloats.Count = meshData.FlagsCount * 2;
			}
		}
		return meshData;
	}

	public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos forBlockPos = null)
	{
		ItemStack content = GetContent(itemstack);
		return GenMesh(api as ICoreClientAPI, content, forBlockPos);
	}

	public string GetMeshCacheKey(ItemStack itemstack)
	{
		ItemStack content = GetContent(itemstack);
		return itemstack.Collectible.Code.ToShortString() + "-" + content?.StackSize + "x" + content?.Collectible.Code.ToShortString();
	}

	public string GetContainedInfo(ItemSlot inSlot)
	{
		float currentLitres = GetCurrentLitres(inSlot.Itemstack);
		ItemStack content = GetContent(inSlot.Itemstack);
		if (currentLitres <= 0f)
		{
			return Lang.GetWithFallback("contained-empty-container", "{0} (Empty)", inSlot.Itemstack.GetName());
		}
		string text = Lang.Get(content.Collectible.Code.Domain + ":incontainer-" + content.Class.ToString().ToLowerInvariant() + "-" + content.Collectible.Code.Path);
		return Lang.Get("contained-liquidcontainer-compact", inSlot.Itemstack.GetName(), currentLitres, text, PerishableInfoCompactContainer(api, inSlot));
	}

	public string GetContainedName(ItemSlot inSlot, int quantity)
	{
		return inSlot.Itemstack.GetName();
	}
}
