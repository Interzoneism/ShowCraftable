using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BEBehaviorCropProp : BlockEntityBehavior, ITexPositionSource
{
	public int Stage = 1;

	public string Type;

	private MeshData mesh;

	private CropPropConfig config;

	private ICoreClientAPI capi;

	private Shape nowTesselatingShape;

	private bool dead;

	private Block cropBlock;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			int textureSubId;
			if (config.Textures != null && config.Textures.ContainsKey(textureCode))
			{
				capi.BlockTextureAtlas.GetOrInsertTexture(config.Textures[textureCode], out textureSubId, out var texPos);
				return texPos;
			}
			capi.BlockTextureAtlas.GetOrInsertTexture(nowTesselatingShape.Textures[textureCode], out textureSubId, out var texPos2);
			return texPos2;
		}
	}

	public BEBehaviorCropProp(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		if (Api.Side == EnumAppSide.Server)
		{
			Blockentity.RegisterGameTickListener(onTick8s, 800, Api.World.Rand.Next(100));
			if (Type != null)
			{
				loadConfig();
				onTick8s(0f);
				mesh = null;
			}
		}
		else if (Type != null)
		{
			loadConfig();
			mesh = null;
		}
	}

	private void loadConfig()
	{
		if (Type == null)
		{
			return;
		}
		config = base.Block.Attributes["types"][dead ? "dead" : Type].AsObject<CropPropConfig>();
		if (config.Shape != null)
		{
			config.Shape.Base.Path = config.Shape.Base.Path.Replace("{stage}", Stage.ToString() ?? "").Replace("{type}", Type);
		}
		if (config.Textures == null)
		{
			return;
		}
		foreach (CompositeTexture value in config.Textures.Values)
		{
			value.Base.Path = value.Base.Path.Replace("{stage}", Stage.ToString() ?? "").Replace("{type}", Type);
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		if (byItemStack != null)
		{
			Type = byItemStack.Attributes.GetString("type");
		}
		loadConfig();
		onTick8s(0f);
		mesh = null;
	}

	private void loadMesh()
	{
		if (Api == null || Api.Side != EnumAppSide.Client)
		{
			return;
		}
		capi = Api as ICoreClientAPI;
		if (Type != null)
		{
			cropBlock = Api.World.GetBlock(new AssetLocation("crop-" + Type + "-" + Stage));
			string cacheKey = getCacheKey();
			Dictionary<string, MeshData> orCreate = ObjectCacheUtil.GetOrCreate(Api, "croppropmeshes", () => new Dictionary<string, MeshData>());
			if (orCreate.TryGetValue(cacheKey, out var value))
			{
				mesh = value;
				return;
			}
			MeshData meshData = genMesh(cropBlock);
			cacheKey = getCacheKey();
			MeshData meshData2 = (orCreate[cacheKey] = meshData);
			mesh = meshData2;
		}
	}

	private string getCacheKey()
	{
		if (config.BakedAlternatesLength < 0)
		{
			return cropBlock.Id + "--1";
		}
		int num = GameMath.MurmurHash3Mod(base.Pos.X, base.Pos.Y, base.Pos.Z, config.BakedAlternatesLength);
		return cropBlock.Id + "-" + num;
	}

	private MeshData genMesh(Block cropBlock)
	{
		CompositeShape compositeShape = config.Shape;
		if (compositeShape == null)
		{
			if (cropBlock.Shape.Alternates == null)
			{
				mesh = capi.TesselatorManager.GetDefaultBlockMesh(cropBlock).Clone();
				mesh.Translate(0f, -0.0625f, 0f);
				return mesh;
			}
			compositeShape = cropBlock.Shape;
		}
		else
		{
			compositeShape.LoadAlternates(capi.Assets, capi.Logger);
		}
		if (compositeShape.BakedAlternates != null)
		{
			config.BakedAlternatesLength = compositeShape.BakedAlternates.Length;
			compositeShape = compositeShape.BakedAlternates[GameMath.MurmurHash3Mod(base.Pos.X, base.Pos.Y, base.Pos.Z, compositeShape.BakedAlternates.Length)];
		}
		nowTesselatingShape = capi.Assets.TryGet(compositeShape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json")).ToObject<Shape>();
		capi.Tesselator.TesselateShape("croprop", base.Block.Code, compositeShape, out mesh, this, 0, 0, 0);
		mesh.Translate(0f, -0.0625f, 0f);
		return mesh;
	}

	private void onTick8s(float dt)
	{
		if (config != null)
		{
			float yearRel = Api.World.Calendar.YearRel;
			float num = (config.MonthEnd - config.MonthStart) / 12f;
			int num2 = GameMath.Clamp((int)((yearRel - (config.MonthStart - 1f) / 12f) / num * (float)config.Stages), 1, config.Stages);
			float temperature = Api.World.BlockAccessor.GetClimateAt(base.Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays).Temperature;
			bool flag = !dead && temperature < -2f;
			bool flag2 = dead && temperature > 15f;
			if (flag)
			{
				dead = true;
			}
			if (flag2)
			{
				dead = false;
			}
			if (Stage != num2 || flag || flag2)
			{
				Stage = num2;
				loadConfig();
				loadMesh();
				Blockentity.MarkDirty(redrawOnClient: true);
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		int stage = Stage;
		bool flag = dead;
		Type = tree.GetString("code");
		Stage = tree.GetInt("stage");
		dead = tree.GetBool("dead");
		if (Stage != stage || dead != flag)
		{
			loadConfig();
			loadMesh();
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetString("code", Type);
		tree.SetInt("stage", Stage);
		tree.SetBool("dead", dead);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (mesh == null)
		{
			loadMesh();
		}
		Block block = cropBlock;
		float[] tfMatrix = ((block != null && block.RandomizeRotations) ? TesselationMetaData.randomRotMatrices[GameMath.MurmurHash3Mod(-base.Pos.X, (cropBlock.RandomizeAxes == EnumRandomizeAxes.XYZ) ? base.Pos.Y : 0, base.Pos.Z, TesselationMetaData.randomRotations.Length)] : null);
		mesher.AddMeshData(mesh, tfMatrix);
		return true;
	}
}
