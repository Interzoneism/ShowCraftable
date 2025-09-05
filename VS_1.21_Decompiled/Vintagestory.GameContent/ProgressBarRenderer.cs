using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ProgressBarRenderer : IRenderer, IDisposable, IProgressBar
{
	private MeshRef whiteRectangleRef;

	private MeshRef progressQuadRef;

	private ICoreClientAPI capi;

	private Matrixf mvMatrix = new Matrixf();

	public float Progress { get; set; }

	public double RenderOrder => 0.0;

	public int RenderRange => 10;

	public ProgressBarRenderer(ICoreClientAPI api)
	{
		capi = api;
		MeshData rectangle = LineMeshUtil.GetRectangle(-1);
		whiteRectangleRef = api.Render.UploadMesh(rectangle);
		progressQuadRef = api.Render.UploadMesh(QuadMeshUtil.GetQuad());
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		IShaderProgram currentActiveShader = capi.Render.CurrentActiveShader;
		Vec4f value = new Vec4f(1f, 1f, 1f, 1f);
		currentActiveShader.Uniform("rgbaIn", value);
		currentActiveShader.Uniform("extraGlow", 0);
		currentActiveShader.Uniform("applyColor", 0);
		currentActiveShader.Uniform("tex2d", 0);
		currentActiveShader.Uniform("noTexture", 1f);
		int frameWidth = capi.Render.FrameWidth;
		int frameHeight = capi.Render.FrameHeight;
		mvMatrix.Set(capi.Render.CurrentModelviewMatrix).Translate(frameWidth / 2 - 50, frameHeight / 2 + 15, 50f).Scale(100f, 20f, 0f)
			.Translate(0.5f, 0.5f, 0f)
			.Scale(0.5f, 0.5f, 0f);
		currentActiveShader.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
		currentActiveShader.UniformMatrix("modelViewMatrix", mvMatrix.Values);
		capi.Render.RenderMesh(whiteRectangleRef);
		float x = Progress * 100f;
		mvMatrix.Set(capi.Render.CurrentModelviewMatrix).Translate(frameWidth / 2 - 50, frameHeight / 2 + 15, 50f).Scale(x, 20f, 0f)
			.Translate(0.5f, 0.5f, 0f)
			.Scale(0.5f, 0.5f, 0f);
		currentActiveShader.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
		currentActiveShader.UniformMatrix("modelViewMatrix", mvMatrix.Values);
		capi.Render.RenderMesh(progressQuadRef);
	}

	public void Dispose()
	{
		capi.Render.DeleteMesh(whiteRectangleRef);
		capi.Render.DeleteMesh(progressQuadRef);
	}
}
