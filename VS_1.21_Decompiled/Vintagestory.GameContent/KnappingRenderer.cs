using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class KnappingRenderer : IRenderer, IDisposable
{
	protected ICoreClientAPI api;

	protected BlockPos pos;

	protected MeshRef workItemMeshRef;

	protected MeshRef recipeOutlineMeshRef;

	protected ItemStack workItem;

	protected int texId;

	public string Material;

	protected Matrixf ModelMat = new Matrixf();

	protected Vec4f outLineColorMul = new Vec4f(1f, 1f, 1f, 1f);

	protected Vec3f origin = new Vec3f(0f, 0f, 0f);

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public KnappingRenderer(BlockPos pos, ICoreClientAPI capi)
	{
		this.pos = pos;
		api = capi;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "knappingsurface");
		capi.Event.RegisterRenderer(this, EnumRenderStage.AfterFinalComposition, "knappingsurface");
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (workItemMeshRef != null)
		{
			if (stage == EnumRenderStage.AfterFinalComposition)
			{
				RenderRecipeOutLine();
				return;
			}
			IRenderAPI render = api.Render;
			Vec3d cameraPos = api.World.Player.Entity.CameraPos;
			render.GlDisableCullFace();
			IStandardShaderProgram standardShaderProgram = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
			render.BindTexture2d(texId);
			standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
			standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
			standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z).Values;
			render.RenderMesh(workItemMeshRef);
			standardShaderProgram.ModelMatrix = render.CurrentModelviewMatrix;
			standardShaderProgram.Stop();
		}
	}

	private void RenderRecipeOutLine()
	{
		if (recipeOutlineMeshRef != null && !api.HideGuis)
		{
			IRenderAPI render = api.Render;
			IClientWorldAccessor world = api.World;
			EntityPos entityPos = world.Player.Entity.Pos;
			Vec3d cameraPos = world.Player.Entity.CameraPos;
			outLineColorMul.A = 1f - GameMath.Clamp((float)Math.Sqrt(entityPos.SquareDistanceTo(pos.X, pos.Y, pos.Z)) / 5f - 1f, 0f, 1f);
			ModelMat.Set(render.CameraMatrixOriginf).Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z);
			float num = (render.LineWidth = api.Settings.Float["wireframethickness"]);
			render.GLEnableDepthTest();
			render.GlToggleBlend(blend: true);
			IShaderProgram engineShader = render.GetEngineShader(EnumShaderProgram.Wireframe);
			engineShader.Use();
			engineShader.Uniform("origin", origin);
			engineShader.Uniform("colorIn", outLineColorMul);
			engineShader.UniformMatrix("projectionMatrix", render.CurrentProjectionMatrix);
			engineShader.UniformMatrix("modelViewMatrix", ModelMat.Values);
			render.RenderMesh(recipeOutlineMeshRef);
			engineShader.Stop();
			if (num != 1.6f)
			{
				render.LineWidth = 1.6f;
			}
			render.GLDepthMask(on: false);
		}
	}

	public void RegenMesh(bool[,] Voxels, KnappingRecipe recipeToOutline)
	{
		workItemMeshRef?.Dispose();
		workItemMeshRef = null;
		workItem = new ItemStack(api.World.GetBlock(new AssetLocation("knappingsurface")));
		if (workItem?.Block == null)
		{
			return;
		}
		if (recipeToOutline != null)
		{
			RegenOutlineMesh(recipeToOutline, Voxels);
		}
		MeshData meshData = new MeshData(24, 36);
		float subPixelPaddingX = api.BlockTextureAtlas.SubPixelPaddingX;
		float subPixelPaddingY = api.BlockTextureAtlas.SubPixelPaddingY;
		TextureAtlasPosition position = api.BlockTextureAtlas.GetPosition(workItem.Block, Material);
		MeshData cubeOnlyScaleXyz = CubeMeshUtil.GetCubeOnlyScaleXyz(1f / 32f, 1f / 32f, new Vec3f(1f / 32f, 1f / 32f, 1f / 32f));
		cubeOnlyScaleXyz.Rgba = new byte[96].Fill(byte.MaxValue);
		CubeMeshUtil.SetXyzFacesAndPacketNormals(cubeOnlyScaleXyz);
		texId = position.atlasTextureId;
		for (int i = 0; i < cubeOnlyScaleXyz.Uv.Length; i += 2)
		{
			cubeOnlyScaleXyz.Uv[i] = position.x1 + cubeOnlyScaleXyz.Uv[i] * 2f / (float)api.BlockTextureAtlas.Size.Width - subPixelPaddingX;
			cubeOnlyScaleXyz.Uv[i + 1] = position.y1 + cubeOnlyScaleXyz.Uv[i + 1] * 2f / (float)api.BlockTextureAtlas.Size.Height - subPixelPaddingY;
		}
		cubeOnlyScaleXyz.XyzFaces = (byte[])CubeMeshUtil.CubeFaceIndices.Clone();
		cubeOnlyScaleXyz.XyzFacesCount = 6;
		cubeOnlyScaleXyz.ClimateColorMapIds = new byte[6];
		cubeOnlyScaleXyz.SeasonColorMapIds = new byte[6];
		cubeOnlyScaleXyz.ColorMapIdsCount = 6;
		MeshData meshData2 = cubeOnlyScaleXyz.Clone();
		for (int j = 0; j < 16; j++)
		{
			for (int k = 0; k < 16; k++)
			{
				if (Voxels[j, k])
				{
					float num = (float)j / 16f;
					float num2 = (float)k / 16f;
					for (int l = 0; l < cubeOnlyScaleXyz.xyz.Length; l += 3)
					{
						meshData2.xyz[l] = num + cubeOnlyScaleXyz.xyz[l];
						meshData2.xyz[l + 1] = cubeOnlyScaleXyz.xyz[l + 1];
						meshData2.xyz[l + 2] = num2 + cubeOnlyScaleXyz.xyz[l + 2];
					}
					float num3 = num * 32f / (float)api.BlockTextureAtlas.Size.Width;
					float num4 = num2 * 32f / (float)api.BlockTextureAtlas.Size.Height;
					for (int m = 0; m < cubeOnlyScaleXyz.Uv.Length; m += 2)
					{
						meshData2.Uv[m] = cubeOnlyScaleXyz.Uv[m] + num3;
						meshData2.Uv[m + 1] = cubeOnlyScaleXyz.Uv[m + 1] + num4;
					}
					meshData.AddMeshData(meshData2);
				}
			}
		}
		workItemMeshRef = api.Render.UploadMesh(meshData);
	}

	private void RegenOutlineMesh(KnappingRecipe recipeToOutline, bool[,] Voxels)
	{
		MeshData meshData = new MeshData(24, 36, withNormals: false, withUv: false, withRgba: true, withFlags: false);
		meshData.SetMode(EnumDrawMode.Lines);
		int color = api.ColorPreset.GetColor("voxelColorGreen");
		int color2 = api.ColorPreset.GetColor("voxelColorOrange");
		MeshData cube = LineMeshUtil.GetCube(color);
		MeshData cube2 = LineMeshUtil.GetCube(color2);
		for (int i = 0; i < cube.xyz.Length; i++)
		{
			cube.xyz[i] = cube.xyz[i] / 32f + 1f / 32f;
			cube2.xyz[i] = cube2.xyz[i] / 32f + 1f / 32f;
		}
		MeshData meshData2 = cube.Clone();
		for (int j = 0; j < 16; j++)
		{
			for (int k = 0; k < 16; k++)
			{
				bool flag = recipeToOutline.Voxels[j, 0, k];
				bool flag2 = Voxels[j, k];
				if (flag != flag2)
				{
					float num = (float)j / 16f;
					float num2 = 0.001f;
					float num3 = (float)k / 16f;
					for (int l = 0; l < cube.xyz.Length; l += 3)
					{
						meshData2.xyz[l] = num + cube.xyz[l];
						meshData2.xyz[l + 1] = num2 + cube.xyz[l + 1];
						meshData2.xyz[l + 2] = num3 + cube.xyz[l + 2];
					}
					meshData2.Rgba = ((flag && !flag2) ? cube.Rgba : cube2.Rgba);
					meshData.AddMeshData(meshData2);
				}
			}
		}
		recipeOutlineMeshRef?.Dispose();
		recipeOutlineMeshRef = null;
		if (meshData.VerticesCount > 0)
		{
			recipeOutlineMeshRef = api.Render.UploadMesh(meshData);
		}
	}

	public void Dispose()
	{
		api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		api.Event.UnregisterRenderer(this, EnumRenderStage.AfterFinalComposition);
		recipeOutlineMeshRef?.Dispose();
		workItemMeshRef?.Dispose();
	}
}
