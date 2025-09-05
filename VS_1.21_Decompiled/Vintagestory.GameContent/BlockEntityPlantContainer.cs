using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityPlantContainer : BlockEntityContainer, ITexPositionSource, IRotatable
{
	private InventoryGeneric inv;

	private MeshData potMesh;

	private MeshData contentMesh;

	private RoomRegistry roomReg;

	private ICoreClientAPI capi;

	private ITexPositionSource contentTexSource;

	private PlantContainerProps curContProps;

	private Dictionary<string, AssetLocation> shapeTextures;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "pottedplant";

	public virtual float MeshAngle { get; set; }

	public string ContainerSize => base.Block.Attributes?["plantContainerSize"].AsString();

	private bool hasSoil => !inv[0].Empty;

	private PlantContainerProps PlantContProps => GetProps(inv[0].Itemstack);

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			AssetLocation value = null;
			if (curContProps.Textures != null && curContProps.Textures.TryGetValue(textureCode, out var value2))
			{
				value = value2.Base;
			}
			if (value == null && shapeTextures != null)
			{
				shapeTextures.TryGetValue(textureCode, out value);
			}
			int textureSubId;
			if (value != null)
			{
				TextureAtlasPosition texPos = capi.BlockTextureAtlas[value];
				if (texPos == null)
				{
					BitmapRef bmp = capi.Assets.TryGet(value.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"))?.ToBitmap(capi);
					if (bmp != null)
					{
						capi.BlockTextureAtlas.GetOrInsertTexture(value, out textureSubId, out texPos, () => bmp);
						bmp.Dispose();
					}
				}
				return texPos;
			}
			ItemStack contents = GetContents();
			if (contents.Class == EnumItemClass.Item)
			{
				value = contents.Item.Textures[textureCode].Base;
				BitmapRef bmp2 = capi.Assets.TryGet(value.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"))?.ToBitmap(capi);
				if (bmp2 != null)
				{
					capi.BlockTextureAtlas.GetOrInsertTexture(value, out textureSubId, out var texPos2, () => bmp2);
					bmp2.Dispose();
					return texPos2;
				}
			}
			return contentTexSource[textureCode];
		}
	}

	public BlockEntityPlantContainer()
	{
		inv = new InventoryGeneric(1, null, null);
		inv.OnAcquireTransitionSpeed += slotTransitionSpeed;
	}

	private float slotTransitionSpeed(EnumTransitionType transType, ItemStack stack, float mulByConfig)
	{
		return 0f;
	}

	protected override void OnTick(float dt)
	{
	}

	public ItemStack GetContents()
	{
		return inv[0].Itemstack;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		capi = api as ICoreClientAPI;
		if (api.Side == EnumAppSide.Client && potMesh == null)
		{
			genMeshes();
			MarkDirty(redrawOnClient: true);
			roomReg = api.ModLoader.GetModSystem<RoomRegistry>();
		}
	}

	public bool TryPutContents(ItemSlot fromSlot, IPlayer player)
	{
		if (!inv[0].Empty || fromSlot.Empty)
		{
			return false;
		}
		ItemStack itemstack = fromSlot.Itemstack;
		if (GetProps(itemstack) == null)
		{
			return false;
		}
		if (fromSlot.TryPutInto(Api.World, inv[0]) > 0)
		{
			if (Api.Side == EnumAppSide.Server)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/block/plant"), Pos, 0.0);
			}
			(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			fromSlot.MarkDirty();
			MarkDirty(redrawOnClient: true);
			return true;
		}
		return false;
	}

	public bool TrySetContents(ItemStack stack)
	{
		if (GetProps(stack) == null)
		{
			return false;
		}
		inv[0].Itemstack = stack;
		MarkDirty(redrawOnClient: true);
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
		if (capi != null)
		{
			genMeshes();
			MarkDirty(redrawOnClient: true);
		}
	}

	private void genMeshes()
	{
		if (base.Block.Code == null)
		{
			return;
		}
		potMesh = GenPotMesh(capi.Tesselator);
		if (potMesh != null)
		{
			potMesh = potMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, MeshAngle, 0f);
		}
		MeshData[] array = GenContentMeshes(capi.Tesselator);
		if (array != null && array.Length != 0)
		{
			contentMesh = array[GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, array.Length)];
			if (PlantContProps.RandomRotate)
			{
				float radY = (float)GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 16) * 22.5f * ((float)Math.PI / 180f);
				contentMesh = contentMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, radY, 0f);
			}
			else
			{
				contentMesh = contentMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, MeshAngle, 0f);
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetFloat("meshAngle", MeshAngle);
	}

	private MeshData GenPotMesh(ITesselatorAPI tesselator)
	{
		Dictionary<string, MeshData> orCreate = ObjectCacheUtil.GetOrCreate(Api, "plantContainerMeshes", () => new Dictionary<string, MeshData>());
		string key = base.Block.Code.ToString() + (hasSoil ? "soil" : "empty");
		if (orCreate.TryGetValue(key, out var value))
		{
			return value;
		}
		if (hasSoil && base.Block.Attributes != null)
		{
			CompositeShape compositeShape = base.Block.Attributes["filledShape"].AsObject<CompositeShape>(null, base.Block.Code.Domain);
			Shape shape = null;
			if (compositeShape != null)
			{
				shape = Shape.TryGet(Api, compositeShape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
			}
			if (shape == null)
			{
				Api.World.Logger.Error("Plant container, asset {0} not found,", compositeShape?.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
				return value;
			}
			tesselator.TesselateShape(base.Block, shape, out value);
		}
		else
		{
			value = capi.TesselatorManager.GetDefaultBlockMesh(base.Block);
		}
		return orCreate[key] = value;
	}

	private MeshData[] GenContentMeshes(ITesselatorAPI tesselator)
	{
		ItemStack contents = GetContents();
		if (contents == null)
		{
			return null;
		}
		Dictionary<string, MeshData[]> orCreate = ObjectCacheUtil.GetOrCreate(Api, "plantContainerContentMeshes", () => new Dictionary<string, MeshData[]>());
		float y = ((base.Block.Attributes == null) ? 0.4f : base.Block.Attributes["fillHeight"].AsFloat(0.4f));
		string containerSize = ContainerSize;
		string key = contents?.ToString() + "-" + containerSize + "f" + y;
		if (orCreate.TryGetValue(key, out var value))
		{
			return value;
		}
		curContProps = PlantContProps;
		if (curContProps == null)
		{
			return null;
		}
		CompositeShape compositeShape = curContProps.Shape;
		if (compositeShape == null)
		{
			compositeShape = ((contents.Class == EnumItemClass.Block) ? contents.Block.Shape : contents.Item.Shape);
		}
		ModelTransform modelTransform = curContProps.Transform;
		if (modelTransform == null)
		{
			modelTransform = new ModelTransform().EnsureDefaultValues();
			modelTransform.Translation.Y = y;
		}
		contentTexSource = ((contents.Class == EnumItemClass.Block) ? capi.Tesselator.GetTextureSource(contents.Block) : capi.Tesselator.GetTextureSource(contents.Item));
		List<IAsset> list;
		if (compositeShape.Base.Path.EndsWith('*'))
		{
			list = Api.Assets.GetManyInCategory("shapes", compositeShape.Base.Path.Substring(0, compositeShape.Base.Path.Length - 1), compositeShape.Base.Domain);
		}
		else
		{
			list = new List<IAsset>();
			list.Add(Api.Assets.TryGet(compositeShape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json")));
		}
		if (list != null && list.Count > 0)
		{
			ShapeElement.locationForLogging = compositeShape.Base;
			value = new MeshData[list.Count];
			for (int num = 0; num < list.Count; num++)
			{
				Shape shape = list[num].ToObject<Shape>();
				shapeTextures = shape.Textures;
				MeshData modeldata;
				try
				{
					byte climateColorMapId = (byte)((contents.Block?.ClimateColorMapResolved != null) ? ((byte)(contents.Block.ClimateColorMapResolved.RectIndex + 1)) : 0);
					byte seasonColorMapId = (byte)((contents.Block?.SeasonColorMapResolved != null) ? ((byte)(contents.Block.SeasonColorMapResolved.RectIndex + 1)) : 0);
					tesselator.TesselateShape("plant container content shape", shape, out modeldata, this, null, 0, climateColorMapId, seasonColorMapId);
				}
				catch (Exception ex)
				{
					Api.Logger.Error(string.Concat(ex.Message, " (when tesselating ", compositeShape.Base.WithPathPrefixOnce("shapes/"), ")"));
					Api.Logger.Error(ex);
					value = null;
					break;
				}
				modeldata.ModelTransform(modelTransform);
				value[num] = modeldata;
			}
		}
		else
		{
			Api.World.Logger.Error("Plant container, content asset {0} not found,", compositeShape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
		}
		return orCreate[key] = value;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (potMesh == null)
		{
			return false;
		}
		mesher.AddMeshData(potMesh);
		if (contentMesh != null)
		{
			if (Api.World.BlockAccessor.GetDistanceToRainFall(Pos, 6, 2) >= 20)
			{
				MeshData meshData = contentMesh.Clone();
				meshData.ClearWindFlags();
				mesher.AddMeshData(meshData);
			}
			else
			{
				mesher.AddMeshData(contentMesh);
			}
		}
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		ItemStack contents = GetContents();
		if (contents != null)
		{
			dsc.Append(Lang.Get("Planted: {0}", contents.GetName()));
		}
	}

	public PlantContainerProps GetProps(ItemStack stack)
	{
		return stack?.Collectible.Attributes?["plantContainable"]?[ContainerSize + "Container"]?.AsObject<PlantContainerProps>(null, stack.Collectible.Code.Domain);
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		MeshAngle = tree.GetFloat("meshAngle");
		MeshAngle -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngle", MeshAngle);
	}
}
