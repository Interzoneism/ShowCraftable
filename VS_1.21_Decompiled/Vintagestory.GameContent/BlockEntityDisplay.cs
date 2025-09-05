using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public abstract class BlockEntityDisplay : BlockEntityContainer, ITexPositionSource
{
	protected CollectibleObject nowTesselatingObj;

	protected Shape nowTesselatingShape;

	protected ICoreClientAPI capi;

	protected float[][] tfMatrices;

	public virtual string ClassCode => InventoryClassName;

	public virtual int DisplayedItems => Inventory.Count;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public virtual string AttributeTransformCode => "onDisplayTransform";

	public virtual TextureAtlasPosition this[string textureCode]
	{
		get
		{
			IDictionary<string, CompositeTexture> dictionary;
			if (!(nowTesselatingObj is Item item))
			{
				dictionary = (nowTesselatingObj as Block).Textures;
			}
			else
			{
				IDictionary<string, CompositeTexture> textures = item.Textures;
				dictionary = textures;
			}
			IDictionary<string, CompositeTexture> dictionary2 = dictionary;
			AssetLocation value = null;
			if (dictionary2.TryGetValue(textureCode, out var value2))
			{
				value = value2.Baked.BakedName;
			}
			if (value == null && dictionary2.TryGetValue("all", out value2))
			{
				value = value2.Baked.BakedName;
			}
			if (value == null)
			{
				nowTesselatingShape?.Textures.TryGetValue(textureCode, out value);
			}
			if (value == null)
			{
				value = new AssetLocation(textureCode);
			}
			return getOrCreateTexPos(value);
		}
	}

	protected Dictionary<string, MeshData> MeshCache => ObjectCacheUtil.GetOrCreate(Api, "meshesDisplay-" + ClassCode, () => new Dictionary<string, MeshData>());

	protected TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
	{
		TextureAtlasPosition texPos = capi.BlockTextureAtlas[texturePath];
		if (texPos == null && !capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out var _, out texPos))
		{
			capi.World.Logger.Warning(string.Concat("For render in block ", base.Block.Code, ", item {0} defined texture {1}, no such texture found."), nowTesselatingObj.Code, texturePath);
			return capi.BlockTextureAtlas.UnknownTexturePosition;
		}
		return texPos;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		capi = api as ICoreClientAPI;
		if (capi != null)
		{
			updateMeshes();
			api.Event.RegisterEventBusListener(OnEventBusEvent);
		}
	}

	private void OnEventBusEvent(string eventname, ref EnumHandling handling, IAttribute data)
	{
		if ((eventname != "genjsontransform" && eventname != "oncloseedittransforms" && eventname != "onapplytransforms") || capi == null || Inventory.Empty)
		{
			return;
		}
		EntityPos pos = capi.World.Player.Entity.Pos;
		if (Pos.DistanceTo(pos.X, pos.Y, pos.Z) > 20f)
		{
			return;
		}
		int displayedItems = DisplayedItems;
		for (int i = 0; i < displayedItems; i++)
		{
			ItemStack itemstack = Inventory[i].Itemstack;
			if (itemstack != null)
			{
				string meshCacheKey = getMeshCacheKey(itemstack);
				MeshCache.Remove(meshCacheKey);
			}
		}
		updateMeshes();
		MarkDirty(redrawOnClient: true);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
	}

	protected virtual void RedrawAfterReceivingTreeAttributes(IWorldAccessor worldForResolving)
	{
		if (worldForResolving.Side == EnumAppSide.Client && Api != null)
		{
			updateMeshes();
			MarkDirty(redrawOnClient: true);
		}
	}

	public virtual void updateMeshes()
	{
		if (Api == null || Api.Side == EnumAppSide.Server)
		{
			return;
		}
		int displayedItems = DisplayedItems;
		if (displayedItems != 0)
		{
			for (int i = 0; i < displayedItems; i++)
			{
				updateMesh(i);
			}
			tfMatrices = genTransformationMatrices();
		}
	}

	protected virtual void updateMesh(int index)
	{
		if (Api != null && Api.Side != EnumAppSide.Server)
		{
			ItemStack itemstack = Inventory[index].Itemstack;
			if (itemstack != null && !(itemstack.Collectible?.Code == null))
			{
				getOrCreateMesh(itemstack, index);
			}
		}
	}

	protected virtual string getMeshCacheKey(ItemStack stack)
	{
		IContainedMeshSource containedMeshSource = stack.Collectible?.GetCollectibleInterface<IContainedMeshSource>();
		if (containedMeshSource != null)
		{
			return containedMeshSource.GetMeshCacheKey(stack);
		}
		return stack.Collectible.Code.ToString();
	}

	protected MeshData getMesh(ItemStack stack)
	{
		string meshCacheKey = getMeshCacheKey(stack);
		MeshCache.TryGetValue(meshCacheKey, out var value);
		return value;
	}

	protected virtual MeshData getOrCreateMesh(ItemStack stack, int index)
	{
		MeshData meshData = getMesh(stack);
		if (meshData != null)
		{
			return meshData;
		}
		CompositeShape customShape = stack.Collectible.Attributes?["displayedShape"].AsObject<CompositeShape>(null, stack.Collectible.Code.Domain);
		if (customShape != null)
		{
			string key = "displayedShape-" + customShape.ToString();
			meshData = ObjectCacheUtil.GetOrCreate(capi, key, () => capi.TesselatorManager.CreateMesh("displayed item shape", customShape, (Shape shape, string name) => new ContainedTextureSource(capi, capi.BlockTextureAtlas, shape.Textures, $"For displayed item {stack.Collectible.Code}")));
		}
		else
		{
			IContainedMeshSource containedMeshSource = stack.Collectible?.GetCollectibleInterface<IContainedMeshSource>();
			if (containedMeshSource != null)
			{
				meshData = containedMeshSource.GenMesh(stack, capi.BlockTextureAtlas, Pos);
			}
		}
		if (meshData == null)
		{
			meshData = getDefaultMesh(stack);
		}
		applyDefaultTranforms(stack, meshData);
		string meshCacheKey = getMeshCacheKey(stack);
		MeshCache[meshCacheKey] = meshData;
		return meshData;
	}

	protected void applyDefaultTranforms(ItemStack stack, MeshData mesh)
	{
		ModelTransform modelTransform = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
		if (AttributeTransformCode == "onshelfTransform")
		{
			modelTransform = stack.Collectible.GetCollectibleInterface<IShelvable>()?.GetOnShelfTransform(stack) ?? modelTransform;
			if (modelTransform == null)
			{
				modelTransform = stack.Collectible.Attributes?["onDisplayTransform"].AsObject<ModelTransform>();
			}
		}
		if (modelTransform != null)
		{
			modelTransform.EnsureDefaultValues();
			mesh.ModelTransform(modelTransform);
		}
		if (stack.Class == EnumItemClass.Item && (stack.Item.Shape == null || stack.Item.Shape.VoxelizeTexture))
		{
			mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), (float)Math.PI / 2f, 0f, 0f);
			mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.33f, 0.33f, 0.33f);
			mesh.Translate(0f, -15f / 32f, 0f);
		}
	}

	protected MeshData getDefaultMesh(ItemStack stack)
	{
		ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
		if (stack.Class == EnumItemClass.Block)
		{
			return coreClientAPI.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
		}
		nowTesselatingObj = stack.Collectible;
		nowTesselatingShape = null;
		if (stack.Item.Shape?.Base != null)
		{
			nowTesselatingShape = coreClientAPI.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
		}
		coreClientAPI.Tesselator.TesselateItem(stack.Item, out var modeldata, this);
		modeldata.RenderPassesAndExtraBits.Fill((short)2);
		return modeldata;
	}

	protected abstract float[][] genTransformationMatrices();

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		for (int i = 0; i < DisplayedItems; i++)
		{
			ItemSlot itemSlot = Inventory[i];
			if (!itemSlot.Empty && tfMatrices != null && !(itemSlot.Itemstack.Collectible?.Code == null))
			{
				mesher.AddMeshData(getMesh(itemSlot.Itemstack), tfMatrices[i]);
			}
		}
		return base.OnTesselation(mesher, tessThreadTesselator);
	}
}
