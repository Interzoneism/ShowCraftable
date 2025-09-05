using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace Vintagestory.GameContent;

public class QuernTopRenderer : IRenderer, IDisposable
{
	internal bool ShouldRender;

	internal bool ShouldRotateManual;

	internal bool ShouldRotateAutomated;

	public BEBehaviorMPConsumer mechPowerPart;

	private ICoreClientAPI api;

	private BlockPos pos;

	private MeshRef meshref;

	public Matrixf ModelMat = new Matrixf();

	public float AngleRad;

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public QuernTopRenderer(ICoreClientAPI coreClientAPI, BlockPos pos, MeshData mesh)
	{
		api = coreClientAPI;
		this.pos = pos;
		meshref = coreClientAPI.Render.UploadMesh(mesh);
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (meshref != null && ShouldRender)
		{
			IRenderAPI render = api.Render;
			Vec3d cameraPos = api.World.Player.Entity.CameraPos;
			render.GlDisableCullFace();
			render.GlToggleBlend(blend: true);
			IStandardShaderProgram standardShaderProgram = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
			standardShaderProgram.Tex2D = api.BlockTextureAtlas.AtlasTextures[0].TextureId;
			standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z).Translate(0.5f, 0.6875f, 0.5f)
				.RotateY(AngleRad)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
			standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
			standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
			render.RenderMesh(meshref);
			standardShaderProgram.Stop();
			if (ShouldRotateManual)
			{
				AngleRad += deltaTime * 40f * ((float)Math.PI / 180f);
			}
			if (ShouldRotateAutomated)
			{
				AngleRad = mechPowerPart.AngleRad;
			}
		}
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		meshref.Dispose();
	}
}
