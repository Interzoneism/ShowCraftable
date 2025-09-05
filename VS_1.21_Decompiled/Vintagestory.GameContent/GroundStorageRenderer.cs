using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GroundStorageRenderer : IRenderer, IDisposable
{
	private readonly ICoreClientAPI capi;

	private readonly BlockEntityGroundStorage groundStorage;

	public Matrixf ModelMat = new Matrixf();

	private int[] itemTemps;

	private float accumDelta;

	private bool check500;

	private bool check450;

	public double RenderOrder => 0.5;

	public int RenderRange => 30;

	public GroundStorageRenderer(ICoreClientAPI capi, BlockEntityGroundStorage groundStorage)
	{
		this.capi = capi;
		this.groundStorage = groundStorage;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque);
		itemTemps = new int[groundStorage.Inventory.Count];
		UpdateTemps();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		accumDelta += deltaTime;
		EntityPos pos = capi.World.Player.Entity.Pos;
		float num = groundStorage.Pos.DistanceSqTo(pos.X, pos.Y, pos.Z);
		bool flag = (float)(RenderRange * RenderRange) < num;
		if (accumDelta > 1f)
		{
			UpdateTemps();
		}
		if (!groundStorage.UseRenderer || groundStorage.Inventory.Empty || flag)
		{
			return;
		}
		IRenderAPI render = capi.Render;
		Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
		IStandardShaderProgram standardShaderProgram = render.PreparedStandardShader(groundStorage.Pos.X, groundStorage.Pos.Y, groundStorage.Pos.Z);
		Vec3f[] array = new Vec3f[groundStorage.DisplayedItems];
		groundStorage.GetLayoutOffset(array);
		Vec4f lightRGBs = capi.World.BlockAccessor.GetLightRGBs(groundStorage.Pos.X, groundStorage.Pos.Y, groundStorage.Pos.Z);
		render.GlDisableCullFace();
		render.GlToggleBlend(blend: true);
		standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
		standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
		MultiTextureMeshRef[] meshRefs = groundStorage.MeshRefs;
		for (int i = 0; i < meshRefs.Length; i++)
		{
			ItemStack itemStack = groundStorage.Inventory[i]?.Itemstack;
			MultiTextureMeshRef multiTextureMeshRef = groundStorage.MeshRefs[i];
			if (itemStack != null && multiTextureMeshRef != null && !multiTextureMeshRef.Disposed)
			{
				float[] incandescenceColorAsColor4f = ColorUtil.GetIncandescenceColorAsColor4f(itemTemps[i]);
				int num2 = GameMath.Clamp((itemTemps[i] - 500) / 3, 0, 255);
				ModelMat.Identity().Translate((double)groundStorage.Pos.X - cameraPos.X, (double)groundStorage.Pos.Y - cameraPos.Y, (double)groundStorage.Pos.Z - cameraPos.Z).Translate(0.5f, 0.5f, 0.5f)
					.RotateY(groundStorage.MeshAngle)
					.Translate(-0.5f, -0.5f, -0.5f)
					.Translate(array[i].X, array[i].Y, array[i].Z);
				ModelTransform modelTransform = groundStorage.ModelTransformsRenderer[i];
				if (modelTransform != null)
				{
					ModelMat.Translate(0.5f, 0.5f, 0.5f).RotateY(modelTransform.Rotation.Y).Translate(-0.5f, -0.5f, -0.5f)
						.Translate(0.5f, 0f, 0.5f)
						.Scale(modelTransform.ScaleXYZ.X, modelTransform.ScaleXYZ.Y, modelTransform.ScaleXYZ.Z)
						.Translate(-0.5f, -0f, -0.5f);
				}
				standardShaderProgram.ModelMatrix = ModelMat.Values;
				standardShaderProgram.TempGlowMode = 1;
				standardShaderProgram.RgbaLightIn = lightRGBs;
				standardShaderProgram.RgbaGlowIn = new Vec4f(incandescenceColorAsColor4f[0], incandescenceColorAsColor4f[1], incandescenceColorAsColor4f[2], (float)num2 / 255f);
				standardShaderProgram.ExtraGlow = num2;
				standardShaderProgram.AverageColor = ColorUtil.ToRGBAVec4f(capi.BlockTextureAtlas.GetAverageColor((itemStack.Item?.FirstTexture ?? itemStack.Block.FirstTextureInventory).Baked.TextureSubId));
				render.RenderMultiTextureMesh(multiTextureMeshRef, "tex");
			}
		}
		standardShaderProgram.TempGlowMode = 0;
		standardShaderProgram.Stop();
	}

	public void UpdateTemps()
	{
		accumDelta = 0f;
		float num = 0f;
		for (int i = 0; i < groundStorage.Inventory.Count; i++)
		{
			ItemStack itemstack = groundStorage.Inventory[i].Itemstack;
			itemTemps[i] = (int)(itemstack?.Collectible.GetTemperature(capi.World, itemstack) ?? 0f);
			num = Math.Max(num, itemTemps[i]);
		}
		if (!groundStorage.NeedsRetesselation)
		{
			if (num < 500f && !check500)
			{
				check500 = true;
				groundStorage.NeedsRetesselation = true;
				groundStorage.MarkDirty(redrawOnClient: true);
			}
			if (num < 450f && !check450)
			{
				check450 = true;
				groundStorage.NeedsRetesselation = true;
				groundStorage.MarkDirty(redrawOnClient: true);
			}
		}
		if (num > 500f && (check500 || check450))
		{
			check500 = false;
			check450 = false;
		}
	}

	public void Dispose()
	{
		capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
	}
}
