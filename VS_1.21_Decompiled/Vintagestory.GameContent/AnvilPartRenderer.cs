using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AnvilPartRenderer : IRenderer, IDisposable
{
	private ICoreClientAPI capi;

	private BlockEntityAnvilPart beAnvil;

	public Matrixf ModelMat = new Matrixf();

	public double RenderOrder => 0.5;

	public int RenderRange => 25;

	public AnvilPartRenderer(ICoreClientAPI capi, BlockEntityAnvilPart beAnvil)
	{
		this.capi = capi;
		this.beAnvil = beAnvil;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque);
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (!beAnvil.Inventory[0].Empty)
		{
			IRenderAPI render = capi.Render;
			Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
			ItemStack itemstack = beAnvil.Inventory[0].Itemstack;
			int num = (int)itemstack.Collectible.GetTemperature(capi.World, itemstack);
			Vec4f lightRGBs = capi.World.BlockAccessor.GetLightRGBs(beAnvil.Pos.X, beAnvil.Pos.Y, beAnvil.Pos.Z);
			float[] incandescenceColorAsColor4f = ColorUtil.GetIncandescenceColorAsColor4f(num);
			int num2 = GameMath.Clamp((num - 550) / 2, 0, 255);
			render.GlDisableCullFace();
			render.GlToggleBlend(blend: true);
			IStandardShaderProgram standardShaderProgram = render.PreparedStandardShader(beAnvil.Pos.X, beAnvil.Pos.Y, beAnvil.Pos.Z);
			standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)beAnvil.Pos.X - cameraPos.X, (double)beAnvil.Pos.Y - cameraPos.Y, (double)beAnvil.Pos.Z - cameraPos.Z).Values;
			standardShaderProgram.RgbaLightIn = lightRGBs;
			standardShaderProgram.RgbaGlowIn = new Vec4f(incandescenceColorAsColor4f[0], incandescenceColorAsColor4f[1], incandescenceColorAsColor4f[2], (float)num2 / 255f);
			standardShaderProgram.ExtraGlow = num2;
			standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
			standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
			standardShaderProgram.AverageColor = ColorUtil.ToRGBAVec4f(capi.BlockTextureAtlas.GetAverageColor((itemstack.Item?.FirstTexture ?? itemstack.Block.FirstTextureInventory).Baked.TextureSubId));
			standardShaderProgram.TempGlowMode = itemstack.ItemAttributes?["tempGlowMode"].AsInt() ?? 0;
			if (beAnvil.BaseMeshRef != null && !beAnvil.BaseMeshRef.Disposed)
			{
				render.RenderMultiTextureMesh(beAnvil.BaseMeshRef, "tex");
			}
			if (beAnvil.FluxMeshRef != null && !beAnvil.FluxMeshRef.Disposed)
			{
				standardShaderProgram.ExtraGlow = 0;
				render.RenderMultiTextureMesh(beAnvil.FluxMeshRef, "tex");
			}
			if (beAnvil.TopMeshRef != null && !beAnvil.TopMeshRef.Disposed)
			{
				int num3 = (int)beAnvil.Inventory[2].Itemstack.Collectible.GetTemperature(capi.World, beAnvil.Inventory[2].Itemstack);
				lightRGBs = capi.World.BlockAccessor.GetLightRGBs(beAnvil.Pos.X, beAnvil.Pos.Y, beAnvil.Pos.Z);
				incandescenceColorAsColor4f = ColorUtil.GetIncandescenceColorAsColor4f(num3);
				num2 = GameMath.Clamp((num3 - 550) / 2, 0, 255);
				standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)beAnvil.Pos.X - cameraPos.X, (double)beAnvil.Pos.Y - cameraPos.Y - (double)((float)beAnvil.hammerHits / 250f), (double)beAnvil.Pos.Z - cameraPos.Z).Values;
				standardShaderProgram.RgbaLightIn = lightRGBs;
				standardShaderProgram.RgbaGlowIn = new Vec4f(incandescenceColorAsColor4f[0], incandescenceColorAsColor4f[1], incandescenceColorAsColor4f[2], (float)num2 / 255f);
				standardShaderProgram.ExtraGlow = num2;
				render.RenderMultiTextureMesh(beAnvil.TopMeshRef, "tex");
			}
			standardShaderProgram.Stop();
		}
	}

	public void Dispose()
	{
		capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
	}
}
