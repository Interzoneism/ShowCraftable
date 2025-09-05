using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemRenderEntities : ClientSystem
{
	public override string Name => "ree";

	public SystemRenderEntities(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterRenderer(OnBeforeRender, EnumRenderStage.Before, Name, 0.4);
		game.eventManager.RegisterRenderer(OnRenderOpaque3D, EnumRenderStage.Opaque, Name, 0.4);
		game.eventManager.RegisterRenderer(OnRenderOIT, EnumRenderStage.OIT, Name, 0.4);
		game.eventManager.RegisterRenderer(OnRenderAfterOIT, EnumRenderStage.AfterOIT, Name, 0.7);
		game.eventManager.RegisterRenderer(OnRenderFrame2D, EnumRenderStage.Ortho, Name, 0.4);
		game.eventManager.RegisterRenderer(OnRenderFrameShadows, EnumRenderStage.ShadowFar, Name, 0.4);
		game.eventManager.RegisterRenderer(OnRenderFrameShadows, EnumRenderStage.ShadowNear, Name, 0.4);
	}

	private void OnBeforeRender(float dt)
	{
		int num = ClientSettings.ViewDistance * ClientSettings.ViewDistance;
		Vec3d xYZ = game.EntityPlayer.Pos.XYZ;
		int dimension = game.EntityPlayer.Pos.Dimension;
		foreach (KeyValuePair<long, EntityRenderer> entityRenderer in game.EntityRenderers)
		{
			Entity entity = entityRenderer.Value.entity;
			if (game.frustumCuller.SphereInFrustum((float)entity.Pos.X, (float)entity.Pos.InternalY, (float)entity.Pos.Z, entity.FrustumSphereRadius) && entity.Pos.Dimension == dimension && (entity.AllowOutsideLoadedRange || (xYZ.HorizontalSquareDistanceTo(entity.Pos.X, entity.Pos.Z) < (float)num && (entity == game.EntityPlayer || game.WorldMap.IsChunkRendered((int)entity.Pos.X / 32, (int)entity.Pos.InternalY / 32, (int)entity.Pos.Z / 32)))))
			{
				entity.IsRendered = true;
				entityRenderer.Value.BeforeRender(dt);
			}
			else
			{
				entity.IsRendered = false;
			}
			game.api.World.FrameProfiler.Mark("esr-beforeanim");
			try
			{
				entity.AnimManager?.OnClientFrame(dt);
			}
			catch (Exception)
			{
				game.Logger.Error("Animations error for entity " + entity.Code.ToShortString() + " at " + entity.ServerPos.AsBlockPos);
				throw;
			}
			game.api.World.FrameProfiler.Mark("esr-afteranim");
		}
	}

	public void OnRenderOpaque3D(float deltaTime)
	{
		RuntimeStats.renderedEntities = 0;
		game.GlMatrixModeModelView();
		game.Platform.GlDisableCullFace();
		game.Platform.GlToggleBlend(on: true);
		game.Platform.GlEnableDepthTest();
		foreach (KeyValuePair<long, EntityRenderer> entityRenderer in game.EntityRenderers)
		{
			if (entityRenderer.Value.entity.IsRendered)
			{
				entityRenderer.Value.DoRender3DOpaque(deltaTime, isShadowPass: false);
				RuntimeStats.renderedEntities++;
			}
		}
		ScreenManager.FrameProfiler.Mark("ree-op");
		ShaderProgramEntityanimated entityanimated = ShaderPrograms.Entityanimated;
		entityanimated.Use();
		entityanimated.RgbaAmbientIn = game.api.renderapi.AmbientColor;
		entityanimated.RgbaFogIn = game.api.renderapi.FogColor;
		entityanimated.FogMinIn = game.api.renderapi.FogMin;
		entityanimated.FogDensityIn = game.api.renderapi.FogDensity;
		entityanimated.ProjectionMatrix = game.CurrentProjectionMatrix;
		entityanimated.EntityTex2D = game.EntityAtlasManager.AtlasTextures[0].TextureId;
		entityanimated.AlphaTest = 0.05f;
		entityanimated.LightPosition = game.shUniforms.LightPosition3D;
		game.Platform.GlDisableCullFace();
		game.GlMatrixModeModelView();
		game.GlPushMatrix();
		game.GlLoadMatrix(game.MainCamera.CameraMatrixOrigin);
		game.Platform.GlToggleBlend(on: true);
		foreach (KeyValuePair<long, EntityRenderer> entityRenderer2 in game.EntityRenderers)
		{
			if (entityRenderer2.Value.entity.IsRendered)
			{
				entityRenderer2.Value.DoRender3DOpaqueBatched(deltaTime, isShadowPass: false);
			}
		}
		game.GlPopMatrix();
		entityanimated.Stop();
		ScreenManager.FrameProfiler.Mark("ree-op-b");
		game.Platform.GlToggleBlend(on: false);
	}

	private void OnRenderOIT(float dt)
	{
	}

	private void OnRenderAfterOIT(float dt)
	{
		game.GlMatrixModeModelView();
		game.Platform.GlDisableCullFace();
		game.Platform.GlToggleBlend(on: true);
		game.Platform.GlEnableDepthTest();
		foreach (KeyValuePair<long, EntityRenderer> entityRenderer in game.EntityRenderers)
		{
			if (entityRenderer.Value.entity.IsRendered)
			{
				entityRenderer.Value.DoRender3DAfterOIT(dt, isShadowPass: false);
			}
		}
	}

	private void OnRenderFrameShadows(float dt)
	{
		int dimension = game.EntityPlayer.Pos.Dimension;
		foreach (KeyValuePair<long, EntityRenderer> entityRenderer in game.EntityRenderers)
		{
			Entity entity = entityRenderer.Value.entity;
			if (game.frustumCuller.SphereInFrustum((float)entity.Pos.X, (float)entity.Pos.InternalY, (float)entity.Pos.Z, 3.0) && (entity == game.EntityPlayer || (game.WorldMap.IsValidPos((int)entity.Pos.X, (int)entity.Pos.InternalY, (int)entity.Pos.Z) && game.WorldMap.IsChunkRendered((int)entity.Pos.X / 32, (int)entity.Pos.InternalY / 32, (int)entity.Pos.Z / 32))) && entity.Pos.Dimension == dimension)
			{
				entity.IsShadowRendered = true;
				entityRenderer.Value.DoRender3DOpaque(dt, isShadowPass: true);
			}
			else
			{
				entity.IsShadowRendered = false;
			}
		}
		ShaderProgramShadowmapgeneric shaderProgramShadowmapgeneric = (ShaderProgramShadowmapgeneric)ShaderProgramBase.CurrentShaderProgram;
		shaderProgramShadowmapgeneric.Stop();
		ShaderProgramShadowmapentityanimated shadowmapentityanimated = ShaderPrograms.Shadowmapentityanimated;
		shadowmapentityanimated.Use();
		shadowmapentityanimated.ProjectionMatrix = game.CurrentProjectionMatrix;
		shadowmapentityanimated.EntityTex2D = game.EntityAtlasManager.AtlasTextures[0].TextureId;
		_ = game.api.Render;
		_ = game.api.World.Player.Entity;
		foreach (KeyValuePair<long, EntityRenderer> entityRenderer2 in game.EntityRenderers)
		{
			if (entityRenderer2.Value.entity.IsShadowRendered)
			{
				entityRenderer2.Value.DoRender3DOpaqueBatched(dt, isShadowPass: true);
			}
		}
		shadowmapentityanimated.Stop();
		shaderProgramShadowmapgeneric.Use();
		shaderProgramShadowmapgeneric.MvpMatrix = game.shadowMvpMatrix;
	}

	private void OnRenderFrame2D(float dt)
	{
		foreach (KeyValuePair<long, EntityRenderer> entityRenderer in game.EntityRenderers)
		{
			Entity entity = entityRenderer.Value.entity;
			EntityRenderer value = entityRenderer.Value;
			if (entity.IsRendered)
			{
				value.DoRender2D(dt);
			}
		}
		ScreenManager.FrameProfiler.Mark("ree2d-d");
	}

	public override void Dispose(ClientMain game)
	{
		foreach (KeyValuePair<long, EntityRenderer> entityRenderer in game.EntityRenderers)
		{
			entityRenderer.Value.Dispose();
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
