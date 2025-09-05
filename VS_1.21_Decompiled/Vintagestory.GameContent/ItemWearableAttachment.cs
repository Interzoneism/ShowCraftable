using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemWearableAttachment : Item, IContainedMeshSource, ITexPositionSource
{
	private bool attachableToEntity;

	private ITextureAtlasAPI curAtlas;

	private Shape nowTesselatingShape;

	public Size2i AtlasSize => curAtlas.Size;

	public virtual TextureAtlasPosition this[string textureCode]
	{
		get
		{
			AssetLocation value = null;
			if (Textures.TryGetValue(textureCode, out var value2))
			{
				value = value2.Baked.BakedName;
			}
			if (value == null && Textures.TryGetValue("all", out value2))
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

	public override void OnLoaded(ICoreAPI api)
	{
		attachableToEntity = IAttachableToEntity.FromCollectible(this) != null;
		base.OnLoaded(api);
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		Dictionary<string, MultiTextureMeshRef> dictionary = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "wearableAttachmentMeshRefs");
		if (dictionary != null && dictionary.Count > 0)
		{
			foreach (MultiTextureMeshRef value in dictionary.Values)
			{
				value?.Dispose();
			}
			ObjectCacheUtil.Delete(api, "wearableAttachmentMeshRefs");
		}
		base.OnUnloaded(api);
	}

	protected TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
	{
		ICoreClientAPI capi = api as ICoreClientAPI;
		curAtlas.GetOrInsertTexture(texturePath, out var _, out var texPos, delegate
		{
			IAsset asset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
			if (asset != null)
			{
				return asset.ToBitmap(capi);
			}
			capi.World.Logger.Warning("Item {0} defined texture {1}, not no such texture found.", Code, texturePath);
			return (IBitmap)null;
		}, 0.1f);
		return texPos;
	}

	public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos forBlockPos = null)
	{
		ICoreClientAPI coreClientAPI = api as ICoreClientAPI;
		if (targetAtlas == coreClientAPI.ItemTextureAtlas)
		{
			ITexPositionSource textureSource = coreClientAPI.Tesselator.GetTextureSource(itemstack.Item);
			return genMesh(coreClientAPI, itemstack, textureSource);
		}
		curAtlas = targetAtlas;
		MeshData meshData = genMesh(api as ICoreClientAPI, itemstack, this);
		meshData.RenderPassesAndExtraBits.Fill((short)1);
		return meshData;
	}

	public virtual string GetMeshCacheKey(ItemStack itemstack)
	{
		return "wearableAttachmentModelRef-" + itemstack.Collectible.Code.ToString();
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		if (attachableToEntity)
		{
			Dictionary<string, MultiTextureMeshRef> orCreate = ObjectCacheUtil.GetOrCreate(capi, "wearableAttachmentMeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());
			string meshCacheKey = GetMeshCacheKey(itemstack);
			if (!orCreate.TryGetValue(meshCacheKey, out renderinfo.ModelRef))
			{
				ITexPositionSource textureSource = capi.Tesselator.GetTextureSource(itemstack.Item);
				MeshData meshData = genMesh(capi, itemstack, textureSource);
				ItemRenderInfo obj = renderinfo;
				MultiTextureMeshRef modelRef = (orCreate[meshCacheKey] = ((meshData == null) ? renderinfo.ModelRef : capi.Render.UploadMultiTextureMesh(meshData)));
				obj.ModelRef = modelRef;
			}
			if (Attributes["visibleDamageEffect"].AsBool())
			{
				renderinfo.DamageEffect = Math.Max(0f, 1f - (float)GetRemainingDurability(itemstack) / (float)GetMaxDurability(itemstack) * 1.1f);
			}
		}
	}

	protected MeshData genMesh(ICoreClientAPI capi, ItemStack itemstack, ITexPositionSource texSource)
	{
		JsonObject attributes = itemstack.Collectible.Attributes;
		EntityProperties entityType = capi.World.GetEntityType(new AssetLocation(attributes?["wearerEntityCode"].ToString() ?? "player"));
		Shape loadedShape = entityType.Client.LoadedShape;
		AssetLocation assetLocation = entityType.Client.Shape.Base;
		Shape shape = (attachableToEntity ? new Shape
		{
			Elements = loadedShape.CloneElements(),
			Animations = loadedShape.CloneAnimations(),
			AnimationsByCrc32 = loadedShape.AnimationsByCrc32,
			JointsById = loadedShape.JointsById,
			TextureWidth = loadedShape.TextureWidth,
			TextureHeight = loadedShape.TextureHeight,
			Textures = null
		} : loadedShape);
		MeshData modeldata;
		if (attributes["wearableInvShape"].Exists)
		{
			AssetLocation shapePath = new AssetLocation("shapes/" + attributes["wearableInvShape"]?.ToString() + ".json");
			Shape shape2 = Vintagestory.API.Common.Shape.TryGet(capi, shapePath);
			capi.Tesselator.TesselateShape(itemstack.Collectible, shape2, out modeldata);
		}
		else
		{
			CompositeShape compositeShape = (attributes["attachShape"].Exists ? attributes["attachShape"].AsObject<CompositeShape>(null, itemstack.Collectible.Code.Domain) : ((itemstack.Class == EnumItemClass.Item) ? itemstack.Item.Shape : itemstack.Block.Shape));
			if (compositeShape == null)
			{
				capi.World.Logger.Warning("Wearable shape {0} {1} does not define a shape through either the shape property or the attachShape Attribute. Item will be invisible.", itemstack.Class, itemstack.Collectible.Code);
				return null;
			}
			AssetLocation assetLocation2 = compositeShape.Base.CopyWithPath("shapes/" + compositeShape.Base.Path + ".json");
			Shape shape3 = Vintagestory.API.Common.Shape.TryGet(capi, assetLocation2);
			if (shape3 == null)
			{
				capi.World.Logger.Warning("Wearable shape {0} defined in {1} {2} not found or errored, was supposed to be at {3}. Item will be invisible.", compositeShape.Base, itemstack.Class, itemstack.Collectible.Code, assetLocation2);
				return null;
			}
			shape.StepParentShape(shape3, assetLocation2.ToShortString(), assetLocation.ToShortString(), capi.Logger, delegate
			{
			});
			if (compositeShape.Overlays != null)
			{
				CompositeShape[] overlays = compositeShape.Overlays;
				foreach (CompositeShape compositeShape2 in overlays)
				{
					Shape shape4 = Vintagestory.API.Common.Shape.TryGet(capi, compositeShape2.Base.CopyWithPath("shapes/" + compositeShape2.Base.Path + ".json"));
					if (shape4 == null)
					{
						capi.World.Logger.Warning("Wearable shape {0} overlay {4} defined in {1} {2} not found or errored, was supposed to be at {3}. Item will be invisible.", compositeShape.Base, itemstack.Class, itemstack.Collectible.Code, assetLocation2, compositeShape2.Base);
					}
					else
					{
						shape.StepParentShape(shape4, compositeShape2.Base.ToShortString(), assetLocation.ToShortString(), capi.Logger, delegate
						{
						});
					}
				}
			}
			nowTesselatingShape = shape;
			capi.Tesselator.TesselateShapeWithJointIds("entity", shape, out modeldata, texSource, new Vec3f());
			nowTesselatingShape = null;
		}
		return modeldata;
	}
}
