using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class FruitpressContentsRenderer : IRenderer, IDisposable, ITexPositionSource
{
	private ICoreClientAPI api;

	private BlockPos pos;

	private Matrixf ModelMat = new Matrixf();

	private MultiTextureMeshRef mashMeshref;

	private BlockEntityFruitPress befruitpress;

	private AssetLocation textureLocation;

	private TextureAtlasPosition texPos;

	public TextureAtlasPosition juiceTexPos;

	public double RenderOrder => 0.65;

	public int RenderRange => 48;

	public Size2i AtlasSize => api.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			AssetLocation assetLocation = textureLocation;
			TextureAtlasPosition textureAtlasPosition = ((!(assetLocation == null)) ? api.BlockTextureAtlas[assetLocation] : texPos);
			if (textureAtlasPosition == null)
			{
				IAsset asset = api.Assets.TryGet(assetLocation.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
				if (asset != null)
				{
					BitmapRef bmp = asset.ToBitmap(api);
					api.BlockTextureAtlas.GetOrInsertTexture(assetLocation, out var _, out textureAtlasPosition, () => bmp);
				}
				else
				{
					textureAtlasPosition = api.BlockTextureAtlas.UnknownTexturePosition;
				}
			}
			return textureAtlasPosition;
		}
	}

	public FruitpressContentsRenderer(ICoreClientAPI api, BlockPos pos, BlockEntityFruitPress befruitpress)
	{
		this.api = api;
		this.pos = pos;
		this.befruitpress = befruitpress;
	}

	public void reloadMeshes(JuiceableProperties props, bool mustReload)
	{
		if (befruitpress.Inventory.Empty)
		{
			mashMeshref = null;
		}
		else
		{
			if (!mustReload && mashMeshref != null)
			{
				return;
			}
			mashMeshref?.Dispose();
			ItemStack itemstack = befruitpress.Inventory[0].Itemstack;
			if (itemstack == null)
			{
				return;
			}
			int num;
			if (itemstack.Collectible.Code.Path == "rot")
			{
				textureLocation = new AssetLocation("block/wood/barrel/rot");
				num = GameMath.Clamp(itemstack.StackSize / 2, 1, 9);
			}
			else
			{
				textureLocation = props.PressedStack.ResolvedItemstack.Item.Textures.First().Value.Base;
				num = ((!itemstack.Attributes.HasAttribute("juiceableLitresLeft")) ? GameMath.Clamp(itemstack.StackSize, 1, 9) : ((int)GameMath.Clamp((float)itemstack.Attributes.GetDecimal("juiceableLitresLeft") + (float)itemstack.Attributes.GetDecimal("juiceableLitresTransfered"), 1f, 9f)));
			}
			Shape shapeBase = Shape.TryGet(api, "shapes/block/wood/fruitpress/part-mash-" + num + ".json");
			api.Tesselator.TesselateShape("fruitpress-mash", shapeBase, out var modeldata, this, null, 0, 0, 0);
			juiceTexPos = api.BlockTextureAtlas[textureLocation];
			if (itemstack.Collectible.Code.Path != "rot")
			{
				Shape.TryGet(api, "shapes/block/wood/fruitpress/part-juice.json");
				AssetLocation itemCode = AssetLocation.Create("juiceportion-" + itemstack.Collectible.Variant["fruit"], itemstack.Collectible.Code.Domain);
				Item item = api.World.GetItem(itemCode);
				textureLocation = null;
				if (item?.FirstTexture.Baked == null)
				{
					texPos = api.BlockTextureAtlas.UnknownTexturePosition;
				}
				else
				{
					texPos = api.BlockTextureAtlas.Positions[item.FirstTexture.Baked.TextureSubId];
				}
			}
			mashMeshref = api.Render.UploadMultiTextureMesh(modeldata);
		}
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (mashMeshref != null && !mashMeshref.Disposed)
		{
			IRenderAPI render = api.Render;
			Vec3d cameraPos = api.World.Player.Entity.CameraPos;
			render.GlDisableCullFace();
			render.GlToggleBlend(blend: true);
			IStandardShaderProgram standardShader = render.StandardShader;
			standardShader.Use();
			standardShader.DontWarpVertices = 0;
			standardShader.AddRenderFlags = 0;
			standardShader.RgbaAmbientIn = render.AmbientColor;
			standardShader.RgbaFogIn = render.FogColor;
			standardShader.FogMinIn = render.FogMin;
			standardShader.FogDensityIn = render.FogDensity;
			standardShader.RgbaTint = ColorUtil.WhiteArgbVec;
			standardShader.NormalShaded = 1;
			standardShader.ExtraGodray = 0f;
			standardShader.ExtraGlow = 0;
			standardShader.SsaoAttn = 0f;
			standardShader.AlphaTest = 0.05f;
			standardShader.OverlayOpacity = 0f;
			Vec4f lightRGBs = api.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
			standardShader.RgbaLightIn = lightRGBs;
			double num = befruitpress.MashSlot.Itemstack?.Attributes?.GetDouble("squeezeRel", 1.0) ?? 1.0;
			standardShader.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z).Translate(0f, 0.8f, 0f)
				.Scale(1f, (float)num, 1f)
				.Values;
			standardShader.ViewMatrix = render.CameraMatrixOriginf;
			standardShader.ProjectionMatrix = render.CurrentProjectionMatrix;
			render.RenderMultiTextureMesh(mashMeshref, "tex");
			standardShader.Stop();
		}
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		mashMeshref?.Dispose();
	}
}
