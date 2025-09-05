using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AuroraRenderer : IRenderer, IDisposable
{
	private bool renderAurora = true;

	private ICoreClientAPI capi;

	private IShaderProgram prog;

	private MeshRef quadTilesRef;

	private Matrixf mvMat = new Matrixf();

	private Vec4f col = new Vec4f(1f, 1f, 1f, 1f);

	private float quarterSecAccum;

	public ClimateCondition clientClimateCond;

	private BlockPos plrPos = new BlockPos();

	public double RenderOrder => 0.35;

	public int RenderRange => 9999;

	public AuroraRenderer(ICoreClientAPI capi, WeatherSystemClient wsys)
	{
		this.capi = capi;
		capi.Event.RegisterRenderer(this, EnumRenderStage.OIT, "aurora");
		capi.Event.ReloadShader += LoadShader;
		LoadShader();
		renderAurora = capi.Settings.Bool["renderAurora"];
		renderAurora = true;
	}

	public bool LoadShader()
	{
		InitQuads();
		prog = capi.Shader.NewShaderProgram();
		prog.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
		prog.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);
		capi.Shader.RegisterFileShaderProgram("aurora", prog);
		return prog.Compile();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (!renderAurora || prog.LoadError || capi.Render.FrameWidth == 0)
		{
			return;
		}
		Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
		quarterSecAccum += deltaTime;
		if (quarterSecAccum > 0.51f)
		{
			plrPos.X = (int)cameraPos.X;
			plrPos.Y = capi.World.SeaLevel;
			plrPos.Z = (int)cameraPos.Z;
			clientClimateCond = capi.World.BlockAccessor.GetClimateAt(plrPos, EnumGetClimateMode.WorldGenValues);
			quarterSecAccum = 0f;
		}
		if (clientClimateCond != null)
		{
			float num = GameMath.Clamp((Math.Max(0f, 0f - clientClimateCond.Temperature) - 5f) / 15f, 0f, 1f);
			col.W = GameMath.Clamp(1f - 1.5f * capi.World.Calendar.DayLightStrength, 0f, 1f) * num;
			if (!(col.W <= 0f))
			{
				prog.Use();
				prog.Uniform("color", col);
				prog.Uniform("rgbaFogIn", capi.Ambient.BlendedFogColor);
				prog.Uniform("fogMinIn", capi.Ambient.BlendedFogMin);
				prog.Uniform("fogDensityIn", capi.Ambient.BlendedFogDensity);
				prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
				prog.Uniform("flatFogDensity", capi.Ambient.BlendedFlatFogDensity);
				prog.Uniform("flatFogStart", capi.Ambient.BlendedFlatFogYPosForShader - (float)cameraPos.Y);
				float num2 = capi.World.Calendar.SpeedOfTime / 60f;
				prog.Uniform("auroraCounter", (float)((double)capi.InWorldEllapsedMilliseconds / 5000.0 * (double)num2) % 579f);
				mvMat.Set(capi.Render.MvMatrix.Top).FollowPlayer().Translate(0.0, (double)(1.1f * (float)capi.World.BlockAccessor.MapSizeY) + 0.5 - capi.World.Player.Entity.CameraPos.Y, 0.0);
				prog.UniformMatrix("modelViewMatrix", mvMat.Values);
				capi.Render.RenderMesh(quadTilesRef);
				prog.Stop();
			}
		}
	}

	public void InitQuads()
	{
		quadTilesRef?.Dispose();
		float num = 200f;
		MeshData meshData = new MeshData(4, 6, withNormals: false, withUv: true, withRgba: true, withFlags: false);
		meshData.CustomFloats = new CustomMeshDataPartFloat(4);
		meshData.CustomFloats.InterleaveStride = 4;
		meshData.CustomFloats.InterleaveOffsets = new int[1];
		meshData.CustomFloats.InterleaveSizes = new int[1] { 1 };
		Random random = new Random();
		float num2 = 1.5f;
		float num3 = 1.5f;
		float num4 = 20f * num2;
		float multiplier = 1f / num2;
		for (int i = 0; i < 15; i++)
		{
			Vec3f vec3f = new Vec3f((float)random.NextDouble() * 20f - 10f, (float)random.NextDouble() * 5f - 3f, (float)random.NextDouble() * 20f - 10f);
			vec3f.Normalize();
			vec3f.Mul(multiplier);
			float num5 = num3 * ((float)random.NextDouble() * 800f - 400f);
			float num6 = num3 * ((float)random.NextDouble() * 80f - 40f);
			float num7 = num3 * ((float)random.NextDouble() * 800f - 400f);
			for (int j = 0; (float)j < num4 + 2f; j++)
			{
				float num8 = (float)random.NextDouble() * 5f + 20f;
				float num9 = (float)random.NextDouble() * 4f + 4f;
				float num10 = (float)random.NextDouble() * 5f + 20f;
				num5 += vec3f.X * num8;
				num6 += vec3f.Y * num9;
				num7 += vec3f.Z * num10;
				int verticesCount = meshData.VerticesCount;
				meshData.AddVertex(num5, num6 + num, num7, j % 2, 1f);
				meshData.AddVertex(num5, num6, num7, j % 2, 0f);
				float num11 = (float)j / (num4 - 1f);
				meshData.CustomFloats.Add(num11, num11);
				if (j > 0 && (float)j < num4 - 1f)
				{
					meshData.AddIndex(verticesCount + 1);
					meshData.AddIndex(verticesCount + 3);
					meshData.AddIndex(verticesCount + 2);
					meshData.AddIndex(verticesCount);
					meshData.AddIndex(verticesCount + 1);
					meshData.AddIndex(verticesCount + 2);
				}
			}
		}
		quadTilesRef = capi.Render.UploadMesh(meshData);
	}

	public void Dispose()
	{
		capi.Render.DeleteMesh(quadTilesRef);
	}
}
