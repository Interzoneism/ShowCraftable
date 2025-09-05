using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class IngotMoldRenderer : IRenderer, IDisposable
{
	private BlockPos pos;

	private ICoreClientAPI api;

	private MeshRef quadModelRef;

	private Matrixf ModelMat = new Matrixf();

	public int LevelLeft;

	public int LevelRight;

	public float TemperatureLeft;

	public float TemperatureRight;

	public AssetLocation TextureNameLeft;

	public AssetLocation TextureNameRight;

	public int QuantityMolds = 1;

	private readonly BlockEntityIngotMold entity;

	public ItemStack stack;

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public IngotMoldRenderer(BlockEntityIngotMold beim, ICoreClientAPI api)
	{
		pos = beim.Pos;
		this.api = api;
		entity = beim;
		MeshData quad = QuadMeshUtil.GetQuad();
		quad.Uv = new float[8] { 0.1875f, 0.4375f, 0f, 0.4375f, 0f, 0f, 0.1875f, 0f };
		quad.Rgba = new byte[16];
		quad.Rgba.Fill(byte.MaxValue);
		quad.Flags = new int[16];
		quadModelRef = api.Render.UploadMesh(quad);
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (LevelLeft > 0 || LevelRight > 0)
		{
			IRenderAPI render = api.Render;
			Vec3d cameraPos = api.World.Player.Entity.CameraPos;
			render.GlDisableCullFace();
			IStandardShaderProgram standardShader = render.StandardShader;
			standardShader.Use();
			standardShader.RgbaAmbientIn = render.AmbientColor;
			standardShader.RgbaFogIn = render.FogColor;
			standardShader.FogMinIn = render.FogMin;
			standardShader.FogDensityIn = render.FogDensity;
			standardShader.RgbaTint = ColorUtil.WhiteArgbVec;
			standardShader.DontWarpVertices = 0;
			standardShader.ExtraGodray = 0f;
			standardShader.AddRenderFlags = 0;
			if (stack != null)
			{
				standardShader.AverageColor = ColorUtil.ToRGBAVec4f(api.BlockTextureAtlas.GetAverageColor((stack.Item?.FirstTexture ?? stack.Block.FirstTextureInventory).Baked.TextureSubId));
				standardShader.TempGlowMode = 1;
			}
			if (LevelLeft > 0 && TextureNameLeft != null)
			{
				Vec4f lightRGBs = api.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
				float[] incandescenceColorAsColor4f = ColorUtil.GetIncandescenceColorAsColor4f((int)TemperatureLeft);
				int num = (int)GameMath.Clamp((TemperatureLeft - 550f) / 1.5f, 0f, 255f);
				standardShader.RgbaLightIn = lightRGBs;
				standardShader.RgbaGlowIn = new Vec4f(incandescenceColorAsColor4f[0], incandescenceColorAsColor4f[1], incandescenceColorAsColor4f[2], (float)num / 255f);
				standardShader.ExtraGlow = num;
				standardShader.NormalShaded = 0;
				int orLoadTexture = api.Render.GetOrLoadTexture(TextureNameLeft);
				render.BindTexture2d(orLoadTexture);
				float num2 = ((QuantityMolds > 1) ? 4.5f : 8.5f);
				ModelMat.Identity().Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z).Translate(0.5f, 0f, 0.5f)
					.RotateY(entity.MeshAngle)
					.Translate(-0.5f, 0f, -0.5f)
					.Translate(num2 / 16f, 0.0625f + (float)LevelLeft / 850f, 17f / 32f)
					.RotateX((float)Math.PI / 2f)
					.Scale(3f / 32f, 7f / 32f, 0.5f);
				standardShader.ModelMatrix = ModelMat.Values;
				standardShader.ProjectionMatrix = render.CurrentProjectionMatrix;
				standardShader.ViewMatrix = render.CameraMatrixOriginf;
				render.RenderMesh(quadModelRef);
			}
			if (LevelRight > 0 && QuantityMolds > 1 && TextureNameRight != null)
			{
				Vec4f lightRGBs2 = api.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
				float[] incandescenceColorAsColor4f2 = ColorUtil.GetIncandescenceColorAsColor4f((int)TemperatureRight);
				int num3 = (int)GameMath.Clamp((TemperatureRight - 550f) / 1.5f, 0f, 255f);
				standardShader.RgbaLightIn = lightRGBs2;
				standardShader.RgbaGlowIn = new Vec4f(incandescenceColorAsColor4f2[0], incandescenceColorAsColor4f2[1], incandescenceColorAsColor4f2[2], (float)num3 / 255f);
				standardShader.ExtraGlow = num3;
				standardShader.NormalShaded = 0;
				int orLoadTexture2 = api.Render.GetOrLoadTexture(TextureNameRight);
				render.BindTexture2d(orLoadTexture2);
				ModelMat.Identity().Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z).Translate(0.5f, 0f, 0.5f)
					.RotateY(entity.MeshAngle)
					.Translate(-0.5f, 0f, -0.5f)
					.Translate(23f / 32f, 0.0625f + (float)LevelRight / 850f, 17f / 32f)
					.RotateX((float)Math.PI / 2f)
					.Scale(3f / 32f, 7f / 32f, 0.5f);
				standardShader.ModelMatrix = ModelMat.Values;
				standardShader.ProjectionMatrix = render.CurrentProjectionMatrix;
				standardShader.ViewMatrix = render.CameraMatrixOriginf;
				render.RenderMesh(quadModelRef);
			}
			standardShader.Stop();
			render.GlEnableCullFace();
		}
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		quadModelRef?.Dispose();
	}
}
