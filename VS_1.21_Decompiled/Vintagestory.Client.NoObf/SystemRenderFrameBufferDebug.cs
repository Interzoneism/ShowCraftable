using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

internal class SystemRenderFrameBufferDebug : ClientSystem
{
	private bool framebufferDebug;

	private MeshRef coloredPlanesRef;

	private MeshRef quadModel;

	private LoadedTexture[] labels;

	public override string Name => "debwt";

	public SystemRenderFrameBufferDebug(ClientMain game)
		: base(game)
	{
		game.api.ChatCommands.GetOrCreate("debug").BeginSubCommand("fbdeb").WithDescription("Toggle Framebuffer/WOIT Debug mode")
			.HandleWith(CmdWoit)
			.EndSubCommand();
		float gUIScale = RuntimeEnv.GUIScale;
		MeshData customQuadModelData = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, gUIScale * 10f, gUIScale * 10f, 220, 0, 0, 191);
		MeshData customQuadModelData2 = QuadMeshUtilExt.GetCustomQuadModelData(gUIScale * 2f, gUIScale * 2f, gUIScale * 2f, gUIScale * 10f, gUIScale * 10f, 220, 220, 0, 191);
		MeshData customQuadModelData3 = QuadMeshUtilExt.GetCustomQuadModelData(gUIScale * 4f, gUIScale * 4f, gUIScale * 4f, gUIScale * 10f, gUIScale * 10f, 0, 0, 220, 191);
		MeshData meshData = new MeshData(12, 12);
		meshData.AddMeshData(customQuadModelData);
		meshData.AddMeshData(customQuadModelData2);
		meshData.AddMeshData(customQuadModelData3);
		meshData.Uv = null;
		coloredPlanesRef = game.Platform.UploadMesh(meshData);
		quadModel = game.Platform.UploadMesh(QuadMeshUtilExt.GetQuadModelData());
		game.eventManager.RegisterRenderer(OnRenderFrame3DTransparent, EnumRenderStage.OIT, "debwt-oit", 0.2);
		game.eventManager.RegisterRenderer(OnRenderFrame2DOverlay, EnumRenderStage.Ortho, "debwt-ortho", 0.2);
		CairoFont font = CairoFont.WhiteDetailText();
		TextBackground background = new TextBackground
		{
			FillColor = new double[4] { 0.2, 0.2, 0.2, 0.3 },
			Padding = (int)(gUIScale * 2f)
		};
		labels = new LoadedTexture[14]
		{
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Shadow Map Far", font, background),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("WOIT Accum", font, background),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("WOIT Reveal", font, background),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Findbright (A.Bloom)", font, background),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB Color", font, background),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB Depth", font, background),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB Depthlinear", font, background),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Luma", font, background),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Glow (red=bloom,green=godray)", font, background),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Shadow Map Near", font, background),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB GNormal", font, background),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Primary FB GPosition", font, background),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("SSAO", font, background),
			game.api.Gui.TextTexture.GenUnscaledTextTexture("Liquid depth", font, background)
		};
	}

	public void OnRenderFrame3DTransparent(float deltaTime)
	{
		if (framebufferDebug)
		{
			game.Platform.GlDisableDepthTest();
			ShaderProgramWoittest woittest = ShaderPrograms.Woittest;
			woittest.Use();
			woittest.ProjectionMatrix = game.CurrentProjectionMatrix;
			game.GlMatrixModeModelView();
			game.GlPushMatrix();
			game.GlTranslate(5000.0, 120.0, 5000.0);
			woittest.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(coloredPlanesRef);
			game.GlPopMatrix();
			woittest.Stop();
			game.Platform.GlEnableDepthTest();
		}
	}

	public void OnRenderFrame2DOverlay(float deltaTime)
	{
		if (framebufferDebug)
		{
			game.Platform.GlToggleBlend(on: true, EnumBlendMode.PremultipliedAlpha);
			float gUIScale = RuntimeEnv.GUIScale;
			FrameBufferRef frameBufferRef = game.Platform.FrameBuffers[1];
			game.Render2DTextureFlipped(frameBufferRef.ColorTextureIds[0], gUIScale * 10f, gUIScale * 10f, gUIScale * 150f, gUIScale * 150f);
			game.Render2DLoadedTexture(labels[1], gUIScale * 10f, gUIScale * 10f);
			game.Render2DTextureFlipped(frameBufferRef.ColorTextureIds[1], gUIScale * 10f, gUIScale * 160f, gUIScale * 150f, gUIScale * 150f);
			game.Render2DLoadedTexture(labels[2], gUIScale * 10f, gUIScale * 160f);
			frameBufferRef = game.Platform.FrameBuffers[10];
			game.Render2DTextureFlipped(frameBufferRef.ColorTextureIds[0], gUIScale * 10f, gUIScale * 310f, gUIScale * 150f, gUIScale * 150f);
			game.Render2DLoadedTexture(labels[7], gUIScale * 10f, gUIScale * 310f);
			frameBufferRef = game.Platform.FrameBuffers[4];
			game.Render2DTextureFlipped(frameBufferRef.ColorTextureIds[0], gUIScale * 10f, gUIScale * 460f, gUIScale * 150f, gUIScale * 150f);
			game.Render2DLoadedTexture(labels[3], gUIScale * 10f, gUIScale * 460f);
			frameBufferRef = game.Platform.FrameBuffers[0];
			game.Render2DTextureFlipped(frameBufferRef.ColorTextureIds[1], gUIScale * 10f, gUIScale * 610f, gUIScale * 150f, gUIScale * 150f);
			game.Render2DLoadedTexture(labels[8], gUIScale * 10f, gUIScale * 610f);
			frameBufferRef = game.Platform.FrameBuffers[0];
			int num = 10;
			game.Render2DTextureFlipped(frameBufferRef.ColorTextureIds[0], (float)game.Width - gUIScale * 160f, gUIScale * (float)num, gUIScale * 150f, gUIScale * 150f);
			game.Render2DLoadedTexture(labels[4], (float)game.Width - gUIScale * 160f, gUIScale * (float)num);
			num += 155;
			if (ClientSettings.SSAOQuality > 0)
			{
				game.Render2DTextureFlipped(game.Platform.FrameBuffers[13].ColorTextureIds[0], (float)game.Width - gUIScale * 320f, gUIScale * 10f, gUIScale * 150f, gUIScale * 150f);
				game.Render2DLoadedTexture(labels[12], (float)game.Width - gUIScale * 320f, gUIScale * 10f);
				game.Render2DTextureFlipped(frameBufferRef.ColorTextureIds[2], (float)game.Width - gUIScale * 160f, gUIScale * (float)num, gUIScale * 150f, gUIScale * 150f);
				game.Render2DLoadedTexture(labels[10], (float)game.Width - gUIScale * 160f, gUIScale * (float)num);
				num += 155;
				game.Render2DTextureFlipped(frameBufferRef.ColorTextureIds[3], (float)game.Width - gUIScale * 160f, gUIScale * (float)num, gUIScale * 150f, gUIScale * 150f);
				game.Render2DLoadedTexture(labels[11], (float)game.Width - gUIScale * 160f, gUIScale * (float)num);
				num += 155;
			}
			game.Render2DTextureFlipped(frameBufferRef.DepthTextureId, (float)game.Width - gUIScale * 160f, gUIScale * (float)num, gUIScale * 150f, gUIScale * 150f);
			game.Render2DLoadedTexture(labels[5], (float)game.Width - gUIScale * 160f, gUIScale * (float)num);
			num += 155;
			game.guiShaderProg.Stop();
			ShaderProgramDebugdepthbuffer debugdepthbuffer = ShaderPrograms.Debugdepthbuffer;
			debugdepthbuffer.Use();
			debugdepthbuffer.DepthSampler2D = frameBufferRef.DepthTextureId;
			game.GlPushMatrix();
			game.GlTranslate((float)game.Width - gUIScale * 160f, gUIScale * (float)num, gUIScale * 50f);
			game.GlScale(gUIScale * 150f, gUIScale * 150f, 0.0);
			game.GlScale(0.5, 0.5, 0.0);
			game.GlTranslate(1.0, 1.0, 0.0);
			game.GlRotate(180f, 1.0, 0.0, 0.0);
			debugdepthbuffer.ProjectionMatrix = game.CurrentProjectionMatrix;
			debugdepthbuffer.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(quadModel);
			game.GlPopMatrix();
			int shadowMapQuality = ClientSettings.ShadowMapQuality;
			if (shadowMapQuality > 0)
			{
				frameBufferRef = game.Platform.FrameBuffers[11];
				debugdepthbuffer.DepthSampler2D = frameBufferRef.DepthTextureId;
				GL.TexParameter((TextureTarget)3553, (TextureParameterName)34892, 0);
				game.GlPushMatrix();
				game.GlTranslate(gUIScale * 170f, gUIScale * 10f, gUIScale * 50f);
				game.GlScale(gUIScale * 300f, gUIScale * 300f, 0.0);
				game.GlScale(0.5, 0.5, 0.0);
				game.GlTranslate(1.0, 1.0, 0.0);
				game.GlRotate(180f, 1.0, 0.0, 0.0);
				debugdepthbuffer.ProjectionMatrix = game.CurrentProjectionMatrix;
				debugdepthbuffer.ModelViewMatrix = game.CurrentModelViewMatrix;
				game.Platform.RenderMesh(quadModel);
				game.GlPopMatrix();
				GL.TexParameter((TextureTarget)3553, (TextureParameterName)34892, 34894);
			}
			if (shadowMapQuality > 1)
			{
				frameBufferRef = game.Platform.FrameBuffers[12];
				debugdepthbuffer.DepthSampler2D = frameBufferRef.DepthTextureId;
				GL.TexParameter((TextureTarget)3553, (TextureParameterName)34892, 0);
				game.GlPushMatrix();
				game.GlTranslate(gUIScale * 170f, gUIScale * 320f, gUIScale * 50f);
				game.GlScale(gUIScale * 300f, gUIScale * 300f, 0.0);
				game.GlScale(0.5, 0.5, 0.0);
				game.GlTranslate(1.0, 1.0, 0.0);
				game.GlRotate(180f, 1.0, 0.0, 0.0);
				debugdepthbuffer.ProjectionMatrix = game.CurrentProjectionMatrix;
				debugdepthbuffer.ModelViewMatrix = game.CurrentModelViewMatrix;
				game.Platform.RenderMesh(quadModel);
				game.GlPopMatrix();
				GL.TexParameter((TextureTarget)3553, (TextureParameterName)34892, 34894);
			}
			frameBufferRef = game.Platform.FrameBuffers[5];
			debugdepthbuffer.DepthSampler2D = frameBufferRef.DepthTextureId;
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)34892, 0);
			game.GlPushMatrix();
			game.GlTranslate(gUIScale * 170f, gUIScale * 630f, gUIScale * 50f);
			game.GlScale(gUIScale * 300f, gUIScale * 300f, 0.0);
			game.GlScale(0.5, 0.5, 0.0);
			game.GlTranslate(1.0, 1.0, 0.0);
			game.GlRotate(180f, 1.0, 0.0, 0.0);
			debugdepthbuffer.ProjectionMatrix = game.CurrentProjectionMatrix;
			debugdepthbuffer.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(quadModel);
			game.GlPopMatrix();
			GL.TexParameter((TextureTarget)3553, (TextureParameterName)34892, 34894);
			debugdepthbuffer.Stop();
			game.guiShaderProg.Use();
			game.Platform.GlDisableDepthTest();
			game.Render2DLoadedTexture(labels[13], gUIScale * 170f, gUIScale * 630f);
			if (shadowMapQuality > 0)
			{
				game.Render2DLoadedTexture(labels[0], gUIScale * 170f, gUIScale * 10f);
			}
			if (shadowMapQuality > 1)
			{
				game.Render2DLoadedTexture(labels[9], gUIScale * 170f, gUIScale * 320f);
			}
			game.Platform.GlEnableDepthTest();
			game.Render2DLoadedTexture(labels[6], (float)game.Width - gUIScale * 170f, gUIScale * (float)num);
			game.Platform.GlToggleBlend(on: true);
		}
	}

	private TextCommandResult CmdWoit(TextCommandCallingArgs textCommandCallingArgs)
	{
		framebufferDebug = !framebufferDebug;
		return TextCommandResult.Success();
	}

	public override void Dispose(ClientMain game)
	{
		for (int i = 0; i < labels.Length; i++)
		{
			labels[i]?.Dispose();
		}
		quadModel?.Dispose();
		coloredPlanesRef?.Dispose();
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
