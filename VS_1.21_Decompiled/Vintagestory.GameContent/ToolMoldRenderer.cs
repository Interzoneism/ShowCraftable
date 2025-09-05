using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ToolMoldRenderer : IRenderer, IDisposable
{
	private BlockPos pos;

	private ICoreClientAPI api;

	private MeshRef[] quadModelRefs;

	public Matrixf ModelMat = new Matrixf();

	public float Level;

	public float Temperature;

	public AssetLocation TextureName;

	private readonly BlockEntityToolMold entity;

	internal Cuboidf[] fillQuadsByLevel;

	public ItemStack stack;

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public ToolMoldRenderer(BlockEntityToolMold betm, ICoreClientAPI api, Cuboidf[] fillQuadsByLevel = null)
	{
		pos = betm.Pos;
		this.api = api;
		entity = betm;
		this.fillQuadsByLevel = fillQuadsByLevel;
		quadModelRefs = new MeshRef[fillQuadsByLevel.Length];
		MeshData quad = QuadMeshUtil.GetQuad();
		quad.Rgba = new byte[16];
		quad.Rgba.Fill(byte.MaxValue);
		quad.Flags = new int[16];
		for (int i = 0; i < quadModelRefs.Length; i++)
		{
			Cuboidf cuboidf = fillQuadsByLevel[i];
			quad.Uv = new float[8]
			{
				cuboidf.X2 / 16f,
				cuboidf.Z2 / 16f,
				cuboidf.X1 / 16f,
				cuboidf.Z2 / 16f,
				cuboidf.X1 / 16f,
				cuboidf.Z1 / 16f,
				cuboidf.X2 / 16f,
				cuboidf.Z1 / 16f
			};
			quadModelRefs[i] = api.Render.UploadMesh(quad);
		}
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (!(Level <= 0f) && !(TextureName == null))
		{
			int num = (int)GameMath.Clamp(Level, 0f, fillQuadsByLevel.Length - 1);
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
			standardShader.AddRenderFlags = 0;
			standardShader.ExtraGodray = 0f;
			standardShader.NormalShaded = 0;
			if (stack != null)
			{
				standardShader.AverageColor = ColorUtil.ToRGBAVec4f(api.BlockTextureAtlas.GetAverageColor((stack.Item?.FirstTexture ?? stack.Block.FirstTextureInventory).Baked.TextureSubId));
				standardShader.TempGlowMode = 1;
			}
			Vec4f lightRGBs = api.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
			float[] incandescenceColorAsColor4f = ColorUtil.GetIncandescenceColorAsColor4f((int)Temperature);
			int num2 = (int)GameMath.Clamp((Temperature - 550f) / 2f, 0f, 255f);
			standardShader.RgbaLightIn = lightRGBs;
			standardShader.RgbaGlowIn = new Vec4f(incandescenceColorAsColor4f[0], incandescenceColorAsColor4f[1], incandescenceColorAsColor4f[2], (float)num2 / 255f);
			standardShader.ExtraGlow = num2;
			int orLoadTexture = api.Render.GetOrLoadTexture(TextureName);
			Cuboidf cuboidf = fillQuadsByLevel[num];
			render.BindTexture2d(orLoadTexture);
			standardShader.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z).Translate(0.5f, 0f, 0.5f)
				.RotateY(entity.MeshAngle)
				.Translate(-0.5f, 0f, -0.5f)
				.Translate(1f - cuboidf.X1 / 16f, 0.063125f + Math.Max(0f, Level / 16f - 1f / 48f), 1f - cuboidf.Z1 / 16f)
				.RotateX((float)Math.PI / 2f)
				.Scale(0.5f * cuboidf.Width / 16f, 0.5f * cuboidf.Length / 16f, 0.5f)
				.Translate(-1f, -1f, 0f)
				.Values;
			standardShader.ViewMatrix = render.CameraMatrixOriginf;
			standardShader.ProjectionMatrix = render.CurrentProjectionMatrix;
			render.RenderMesh(quadModelRefs[num]);
			standardShader.Stop();
			render.GlEnableCullFace();
		}
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		for (int i = 0; i < quadModelRefs.Length; i++)
		{
			quadModelRefs[i]?.Dispose();
		}
	}
}
