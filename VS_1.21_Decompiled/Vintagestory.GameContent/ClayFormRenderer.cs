using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ClayFormRenderer : IRenderer, IDisposable
{
	private ICoreClientAPI api;

	private BlockPos pos;

	private MeshRef workItemMeshRef;

	private MeshRef recipeOutlineMeshRef;

	private ItemStack workItem;

	private int texId;

	private Matrixf ModelMat = new Matrixf();

	private Vec4f outLineColorMul = new Vec4f(1f, 1f, 1f, 1f);

	protected Vec3f origin = new Vec3f(0f, 0f, 0f);

	public double RenderOrder => 0.5;

	public int RenderRange => 24;

	public ClayFormRenderer(BlockPos pos, ICoreClientAPI capi)
	{
		this.pos = pos;
		api = capi;
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
			standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z).Values;
			standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
			standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
			render.RenderMesh(workItemMeshRef);
			standardShaderProgram.ModelMatrix = render.CurrentModelviewMatrix;
			standardShaderProgram.Stop();
		}
	}

	private void RenderRecipeOutLine()
	{
		if (recipeOutlineMeshRef != null && !api.HideGuis && api.World.Player?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Collectible is ItemClay)
		{
			IRenderAPI render = api.Render;
			IClientWorldAccessor world = api.World;
			EntityPos entityPos = world.Player.Entity.Pos;
			Vec3d cameraPos = world.Player.Entity.CameraPos;
			ModelMat.Set(render.CameraMatrixOriginf).Translate((double)pos.X - cameraPos.X, (double)pos.Y - cameraPos.Y, (double)pos.Z - cameraPos.Z);
			outLineColorMul.A = 1f - GameMath.Clamp((float)Math.Sqrt(entityPos.SquareDistanceTo(pos.X, pos.Y, pos.Z)) / 5f - 1f, 0f, 1f);
			float num = (render.LineWidth = api.Settings.Float["wireframethickness"]);
			render.GLEnableDepthTest();
			render.GlToggleBlend(blend: true);
			IShaderProgram engineShader = render.GetEngineShader(EnumShaderProgram.Wireframe);
			engineShader.Use();
			engineShader.Uniform("origin", origin);
			engineShader.UniformMatrix("projectionMatrix", render.CurrentProjectionMatrix);
			engineShader.UniformMatrix("modelViewMatrix", ModelMat.Values);
			engineShader.Uniform("colorIn", outLineColorMul);
			render.RenderMesh(recipeOutlineMeshRef);
			engineShader.Stop();
			if (num != 1.6f)
			{
				render.LineWidth = 1.6f;
			}
			render.GLDepthMask(on: false);
		}
	}

	public void RegenMesh(ItemStack workitem, bool[,,] Voxels, ClayFormingRecipe recipeToOutline, int recipeLayer)
	{
		workItemMeshRef?.Dispose();
		workItemMeshRef = null;
		if (workitem == null)
		{
			return;
		}
		if (recipeToOutline != null)
		{
			RegenOutlineMesh(recipeToOutline, Voxels, recipeLayer);
		}
		workItem = workitem;
		MeshData meshData = new MeshData(24, 36);
		float subPixelPaddingX = api.BlockTextureAtlas.SubPixelPaddingX;
		float subPixelPaddingY = api.BlockTextureAtlas.SubPixelPaddingY;
		TextureAtlasPosition position = api.BlockTextureAtlas.GetPosition(api.World.GetBlock(new AssetLocation("clayform")), workitem.Collectible.Code.ToShortString());
		MeshData cubeOnlyScaleXyz = CubeMeshUtil.GetCubeOnlyScaleXyz(1f / 32f, 1f / 32f, new Vec3f(1f / 32f, 1f / 32f, 1f / 32f));
		cubeOnlyScaleXyz.Rgba = new byte[96].Fill(byte.MaxValue);
		CubeMeshUtil.SetXyzFacesAndPacketNormals(cubeOnlyScaleXyz);
		texId = position.atlasTextureId;
		for (int i = 0; i < cubeOnlyScaleXyz.Uv.Length; i++)
		{
			if (i % 2 > 0)
			{
				cubeOnlyScaleXyz.Uv[i] = position.y1 + cubeOnlyScaleXyz.Uv[i] * 2f / (float)api.BlockTextureAtlas.Size.Height - subPixelPaddingY;
			}
			else
			{
				cubeOnlyScaleXyz.Uv[i] = position.x1 + cubeOnlyScaleXyz.Uv[i] * 2f / (float)api.BlockTextureAtlas.Size.Width - subPixelPaddingX;
			}
		}
		cubeOnlyScaleXyz.XyzFaces = (byte[])CubeMeshUtil.CubeFaceIndices.Clone();
		cubeOnlyScaleXyz.XyzFacesCount = 6;
		cubeOnlyScaleXyz.SeasonColorMapIds = new byte[6];
		cubeOnlyScaleXyz.ClimateColorMapIds = new byte[6];
		cubeOnlyScaleXyz.ColorMapIdsCount = 6;
		MeshData meshData2 = cubeOnlyScaleXyz.Clone();
		int width = api.BlockTextureAtlas.Size.Width;
		int height = api.BlockTextureAtlas.Size.Height;
		float[] xyz = cubeOnlyScaleXyz.xyz;
		float[] xyz2 = meshData2.xyz;
		for (int j = 0; j < 16; j++)
		{
			for (int k = 0; k < 16; k++)
			{
				for (int l = 0; l < 16; l++)
				{
					if (Voxels[j, k, l])
					{
						float num = (float)j / 16f;
						float num2 = (float)k / 16f;
						float num3 = (float)l / 16f;
						for (int m = 0; m < xyz.Length; m += 3)
						{
							xyz2[m] = num + xyz[m];
							xyz2[m + 1] = num2 + xyz[m + 1];
							xyz2[m + 2] = num3 + xyz[m + 2];
						}
						float num4 = (float)(j + 4 * k) % 16f / 16f * 32f / (float)width;
						float num5 = num3 * 32f / (float)height;
						for (int n = 0; n < cubeOnlyScaleXyz.Uv.Length; n += 2)
						{
							meshData2.Uv[n] = cubeOnlyScaleXyz.Uv[n] + num4;
							meshData2.Uv[n + 1] = cubeOnlyScaleXyz.Uv[n + 1] + num5;
						}
						meshData.AddMeshData(meshData2);
					}
				}
			}
		}
		workItemMeshRef = api.Render.UploadMesh(meshData);
	}

	private void RegenOutlineMesh(ClayFormingRecipe recipeToOutline, bool[,,] Voxels, int recipeLayer)
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
				bool flag = recipeToOutline.Voxels[j, recipeLayer, k];
				bool flag2 = Voxels[j, recipeLayer, k];
				if (flag != flag2)
				{
					float num = (float)j / 16f;
					float num2 = (float)recipeLayer / 16f + 0.001f;
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
