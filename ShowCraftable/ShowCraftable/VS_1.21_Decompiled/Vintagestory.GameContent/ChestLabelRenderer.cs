using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ChestLabelRenderer : BlockEntitySignRenderer
{
	public ChestLabelRenderer(BlockPos pos, ICoreClientAPI api)
		: base(pos, api, null)
	{
		TextWidth = 200;
	}

	public void SetRotation(float radY)
	{
		rotY = radY;
	}

	public override void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (loadedTexture != null)
		{
			IRenderAPI render = api.Render;
			Vec3d cameraPos = api.World.Player.Entity.CameraPos;
			if (!(cameraPos.SquareDistanceTo(pos.X, pos.Y, pos.Z) > 400f))
			{
				render.GlDisableCullFace();
				render.GlToggleBlend(blend: true, EnumBlendMode.PremultipliedAlpha);
				IStandardShaderProgram standardShaderProgram = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
				standardShaderProgram.Tex2D = loadedTexture.TextureId;
				standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z).Translate(0.5f, 0.5f, 0.5f)
					.RotateY(rotY + (float)Math.PI)
					.Translate(-0.5, -0.5, -0.5)
					.Translate(0.5f, 0.35f, 0.0925f)
					.Scale(0.45f * QuadWidth, 0.45f * QuadHeight, 0.45f * QuadWidth)
					.Values;
				standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
				standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
				standardShaderProgram.NormalShaded = 0;
				standardShaderProgram.ExtraGodray = 0f;
				standardShaderProgram.SsaoAttn = 0f;
				standardShaderProgram.AlphaTest = 0.05f;
				standardShaderProgram.OverlayOpacity = 0f;
				standardShaderProgram.AddRenderFlags = 0;
				render.RenderMesh(quadModelRef);
				standardShaderProgram.Stop();
				render.GlToggleBlend(blend: true);
			}
		}
	}
}
