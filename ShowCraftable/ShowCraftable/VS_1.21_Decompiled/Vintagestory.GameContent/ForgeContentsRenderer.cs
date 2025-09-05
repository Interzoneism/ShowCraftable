using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ForgeContentsRenderer : IRenderer, IDisposable, ITexPositionSource
{
	private ICoreClientAPI capi;

	private BlockPos pos;

	private MeshRef workItemMeshRef;

	private MeshRef emberQuadRef;

	private MeshRef coalQuadRef;

	private ItemStack stack;

	private float fuelLevel;

	private bool burning;

	private TextureAtlasPosition coaltexpos;

	private TextureAtlasPosition embertexpos;

	private int textureId;

	private string tmpMetal;

	private ITexPositionSource tmpTextureSource;

	private Matrixf ModelMat = new Matrixf();

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode] => tmpTextureSource[tmpMetal];

	public ForgeContentsRenderer(BlockPos pos, ICoreClientAPI capi)
	{
		this.pos = pos;
		this.capi = capi;
		Block block = capi.World.GetBlock(new AssetLocation("forge"));
		coaltexpos = capi.BlockTextureAtlas.GetPosition(block, "coal");
		embertexpos = capi.BlockTextureAtlas.GetPosition(block, "ember");
		MeshData customQuadHorizontal = QuadMeshUtil.GetCustomQuadHorizontal(0.1875f, 0f, 0.1875f, 0.625f, 0.625f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		for (int i = 0; i < customQuadHorizontal.Uv.Length; i += 2)
		{
			customQuadHorizontal.Uv[i] = embertexpos.x1 + customQuadHorizontal.Uv[i] * 32f / (float)AtlasSize.Width;
			customQuadHorizontal.Uv[i + 1] = embertexpos.y1 + customQuadHorizontal.Uv[i + 1] * 32f / (float)AtlasSize.Height;
		}
		customQuadHorizontal.Flags = new int[4] { 128, 128, 128, 128 };
		MeshData customQuadHorizontal2 = QuadMeshUtil.GetCustomQuadHorizontal(0.1875f, 0f, 0.1875f, 0.625f, 0.625f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		for (int j = 0; j < customQuadHorizontal2.Uv.Length; j += 2)
		{
			customQuadHorizontal2.Uv[j] = coaltexpos.x1 + customQuadHorizontal2.Uv[j] * 32f / (float)AtlasSize.Width;
			customQuadHorizontal2.Uv[j + 1] = coaltexpos.y1 + customQuadHorizontal2.Uv[j + 1] * 32f / (float)AtlasSize.Height;
		}
		emberQuadRef = capi.Render.UploadMesh(customQuadHorizontal);
		coalQuadRef = capi.Render.UploadMesh(customQuadHorizontal2);
	}

	public void SetContents(ItemStack stack, float fuelLevel, bool burning, bool regen)
	{
		this.stack = stack;
		this.fuelLevel = fuelLevel;
		this.burning = burning;
		if (regen)
		{
			RegenMesh();
		}
	}

	public void RegenMesh()
	{
		workItemMeshRef?.Dispose();
		workItemMeshRef = null;
		if (stack == null)
		{
			return;
		}
		tmpMetal = stack.Collectible.LastCodePart();
		MeshData modeldata = null;
		switch (stack.Collectible.FirstCodePart())
		{
		case "metalplate":
		{
			tmpTextureSource = capi.Tesselator.GetTextureSource(capi.World.GetBlock(new AssetLocation("platepile")));
			Shape shapeBase = Shape.TryGet(capi, "shapes/block/stone/forge/platepile.json");
			textureId = tmpTextureSource[tmpMetal].atlasTextureId;
			capi.Tesselator.TesselateShape("block-fcr", shapeBase, out modeldata, this, null, 0, 0, 0, stack.StackSize);
			break;
		}
		case "workitem":
		{
			MeshData meshData = ItemWorkItem.GenMesh(capi, stack, ItemWorkItem.GetVoxels(stack), out textureId);
			if (meshData != null)
			{
				meshData.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.75f, 0.75f, 0.75f);
				meshData.Translate(0f, -0.5625f, 0f);
				workItemMeshRef = capi.Render.UploadMesh(meshData);
			}
			break;
		}
		case "ingot":
		{
			tmpTextureSource = capi.Tesselator.GetTextureSource(capi.World.GetBlock(new AssetLocation("ingotpile")));
			Shape shapeBase = Shape.TryGet(capi, "shapes/block/stone/forge/ingotpile.json");
			textureId = tmpTextureSource[tmpMetal].atlasTextureId;
			capi.Tesselator.TesselateShape("block-fcr", shapeBase, out modeldata, this, null, 0, 0, 0, stack.StackSize);
			break;
		}
		default:
		{
			JsonObject attributes = stack.Collectible.Attributes;
			if (attributes != null && attributes.IsTrue("forgable"))
			{
				if (stack.Class == EnumItemClass.Block)
				{
					modeldata = capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
					textureId = capi.BlockTextureAtlas.AtlasTextures[0].TextureId;
				}
				else
				{
					capi.Tesselator.TesselateItem(stack.Item, out modeldata);
					textureId = capi.ItemTextureAtlas.AtlasTextures[0].TextureId;
				}
				ModelTransform modelTransform = stack.Collectible.Attributes["inForgeTransform"].AsObject<ModelTransform>();
				if (modelTransform != null)
				{
					modelTransform.EnsureDefaultValues();
					modeldata.ModelTransform(modelTransform);
				}
			}
			break;
		}
		}
		if (modeldata != null)
		{
			workItemMeshRef = capi.Render.UploadMesh(modeldata);
		}
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (stack == null && fuelLevel == 0f)
		{
			return;
		}
		IRenderAPI render = capi.Render;
		Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
		render.GlDisableCullFace();
		IStandardShaderProgram standardShader = render.StandardShader;
		standardShader.Use();
		standardShader.RgbaAmbientIn = render.AmbientColor;
		standardShader.RgbaFogIn = render.FogColor;
		standardShader.FogMinIn = render.FogMin;
		standardShader.FogDensityIn = render.FogDensity;
		standardShader.RgbaTint = ColorUtil.WhiteArgbVec;
		standardShader.DontWarpVertices = 0;
		standardShader.AddRenderFlags = 0;
		standardShader.ExtraGodray = 0f;
		standardShader.OverlayOpacity = 0f;
		if (stack != null && workItemMeshRef != null)
		{
			int num = (int)stack.Collectible.GetTemperature(capi.World, stack);
			Vec4f lightRGBs = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
			float[] incandescenceColorAsColor4f = ColorUtil.GetIncandescenceColorAsColor4f(num);
			int num2 = GameMath.Clamp((num - 550) / 2, 0, 255);
			standardShader.NormalShaded = 1;
			standardShader.RgbaLightIn = lightRGBs;
			standardShader.RgbaGlowIn = new Vec4f(incandescenceColorAsColor4f[0], incandescenceColorAsColor4f[1], incandescenceColorAsColor4f[2], (float)num2 / 255f);
			standardShader.ExtraGlow = num2;
			standardShader.Tex2D = textureId;
			standardShader.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y + 0.625 + (double)(fuelLevel * 0.65f), (double)pos.Z - cameraPos.Z).Values;
			standardShader.ViewMatrix = render.CameraMatrixOriginf;
			standardShader.ProjectionMatrix = render.CurrentProjectionMatrix;
			render.RenderMesh(workItemMeshRef);
		}
		if (fuelLevel > 0f)
		{
			Vec4f lightRGBs2 = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
			long num3 = capi.World.ElapsedMilliseconds + pos.GetHashCode();
			float num4 = (float)(Math.Sin((double)num3 / 40.0) * 0.20000000298023224 + Math.Sin((double)num3 / 220.0) * 0.6000000238418579 + Math.Sin((double)num3 / 100.0) + 1.0) / 2f;
			if (burning)
			{
				float[] incandescenceColorAsColor4f2 = ColorUtil.GetIncandescenceColorAsColor4f(1200);
				incandescenceColorAsColor4f2[0] *= 1f - num4 * 0.15f;
				incandescenceColorAsColor4f2[1] *= 1f - num4 * 0.15f;
				incandescenceColorAsColor4f2[2] *= 1f - num4 * 0.15f;
				standardShader.RgbaGlowIn = new Vec4f(incandescenceColorAsColor4f2[0], incandescenceColorAsColor4f2[1], incandescenceColorAsColor4f2[2], 1f);
			}
			else
			{
				standardShader.RgbaGlowIn = new Vec4f(0f, 0f, 0f, 0f);
			}
			standardShader.NormalShaded = 0;
			standardShader.RgbaLightIn = lightRGBs2;
			standardShader.TempGlowMode = 1;
			int num5 = 255 - (int)(num4 * 50f);
			standardShader.ExtraGlow = (burning ? num5 : 0);
			render.BindTexture2d(burning ? embertexpos.atlasTextureId : coaltexpos.atlasTextureId);
			standardShader.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y + 0.625 + (double)(fuelLevel * 0.65f), (double)pos.Z - cameraPos.Z).Values;
			standardShader.ViewMatrix = render.CameraMatrixOriginf;
			standardShader.ProjectionMatrix = render.CurrentProjectionMatrix;
			render.RenderMesh(burning ? emberQuadRef : coalQuadRef);
		}
		standardShader.Stop();
	}

	public void Dispose()
	{
		capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		emberQuadRef?.Dispose();
		coalQuadRef?.Dispose();
		workItemMeshRef?.Dispose();
	}
}
