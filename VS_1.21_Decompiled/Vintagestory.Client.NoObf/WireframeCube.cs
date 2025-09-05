using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class WireframeCube
{
	private MeshRef modelRef;

	private Matrixf mvMat = new Matrixf();

	public static WireframeCube CreateUnitCube(ICoreClientAPI capi, int color = int.MinValue)
	{
		WireframeCube wireframeCube = new WireframeCube();
		MeshData cube = LineMeshUtil.GetCube(color);
		cube.Scale(new Vec3f(), 0.5f, 0.5f, 0.5f);
		cube.Translate(0.5f, 0.5f, 0.5f);
		cube.Flags = new int[cube.VerticesCount];
		for (int i = 0; i < cube.Flags.Length; i++)
		{
			cube.Flags[i] = 256;
		}
		wireframeCube.modelRef = capi.Render.UploadMesh(cube);
		return wireframeCube;
	}

	public static WireframeCube CreateCenterOriginCube(ICoreClientAPI capi, int color = int.MinValue)
	{
		WireframeCube wireframeCube = new WireframeCube();
		MeshData cube = LineMeshUtil.GetCube(color);
		cube.Flags = new int[cube.VerticesCount];
		for (int i = 0; i < cube.Flags.Length; i++)
		{
			cube.Flags[i] = 256;
		}
		wireframeCube.modelRef = capi.Render.UploadMesh(cube);
		return wireframeCube;
	}

	public void Render(ICoreClientAPI capi, double posx, double posy, double posz, float scalex, float scaley, float scalez, float lineWidth = 1.6f, Vec4f color = null)
	{
		EntityPlayer entity = capi.World.Player.Entity;
		mvMat.Identity().Set(capi.Render.CameraMatrixOrigin).Translate(posx - entity.CameraPos.X, posy - entity.CameraPos.Y, posz - entity.CameraPos.Z)
			.Scale(scalex, scaley, scalez);
		Render(capi, mvMat, lineWidth, color);
	}

	public void Render(ICoreClientAPI capi, Matrixf mat, float lineWidth = 1.6f, Vec4f color = null)
	{
		IShaderProgram program = capi.Shader.GetProgram(25);
		program.Use();
		capi.Render.LineWidth = lineWidth;
		capi.Render.GLEnableDepthTest();
		capi.Render.GLDepthMask(on: false);
		capi.Render.GlToggleBlend(blend: true);
		program.Uniform("origin", new Vec3f(0f, 0f, 0f));
		program.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
		program.UniformMatrix("modelViewMatrix", mat.Values);
		program.Uniform("colorIn", color ?? ColorUtil.WhiteArgbVec);
		capi.Render.RenderMesh(modelRef);
		program.Stop();
		if (lineWidth != 1.6f)
		{
			capi.Render.LineWidth = 1.6f;
		}
		if (RuntimeEnv.OS != OS.Mac)
		{
			capi.Render.GLDepthMask(on: true);
		}
	}

	public void Dispose()
	{
		modelRef?.Dispose();
	}
}
