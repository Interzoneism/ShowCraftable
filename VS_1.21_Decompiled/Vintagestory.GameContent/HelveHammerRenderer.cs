using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace Vintagestory.GameContent;

public class HelveHammerRenderer : IRenderer, IDisposable
{
	internal bool ShouldRender;

	internal bool ShouldRotateManual;

	internal bool ShouldRotateAutomated;

	private BEHelveHammer be;

	private ICoreClientAPI api;

	private BlockPos pos;

	private MultiTextureMeshRef meshref;

	public Matrixf ModelMat = new Matrixf();

	public float AngleRad;

	internal bool Obstructed;

	private Matrixf shadowMvpMat = new Matrixf();

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public HelveHammerRenderer(ICoreClientAPI coreClientAPI, BEHelveHammer be, BlockPos pos, MeshData mesh)
	{
		api = coreClientAPI;
		this.pos = pos;
		this.be = be;
		meshref = coreClientAPI.Render.UploadMultiTextureMesh(mesh);
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (meshref != null && be.HammerStack != null)
		{
			IRenderAPI render = api.Render;
			Vec3d cameraPos = api.World.Player.Entity.CameraPos;
			render.GlDisableCullFace();
			float num = be.facing.HorizontalAngleIndex * 90;
			float num2 = ((be.facing == BlockFacing.NORTH || be.facing == BlockFacing.WEST) ? (-0.0625f) : 1.0625f);
			ModelMat.Identity().Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z).RotateYDeg(num)
				.Translate(num2, 25f / 32f, 0.5f)
				.RotateZ(AngleRad)
				.Translate(0f - num2, -25f / 32f, -0.5f)
				.RotateYDeg(0f - num);
			if (stage == EnumRenderStage.Opaque)
			{
				IStandardShaderProgram standardShaderProgram = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
				standardShaderProgram.ModelMatrix = ModelMat.Values;
				standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
				standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
				render.RenderMultiTextureMesh(meshref, "tex");
				standardShaderProgram.Stop();
				AngleRad = be.Angle;
			}
			else
			{
				IRenderAPI render2 = api.Render;
				shadowMvpMat.Set(render2.CurrentProjectionMatrix).Mul(render2.CurrentModelviewMatrix).Mul(ModelMat.Values);
				render2.CurrentActiveShader.UniformMatrix("mvpMatrix", shadowMvpMat.Values);
				render2.CurrentActiveShader.Uniform("origin", new Vec3f());
				render.RenderMultiTextureMesh(meshref, "tex2d");
			}
		}
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		api.Event.UnregisterRenderer(this, EnumRenderStage.ShadowFar);
		api.Event.UnregisterRenderer(this, EnumRenderStage.ShadowNear);
		meshref.Dispose();
	}
}
