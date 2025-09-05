using System;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

internal class SystemRenderSkyColor : ClientSystem
{
	private MeshRef skyIcosahedron;

	private BitmapRef skyTexture;

	public override string Name => "resc";

	public SystemRenderSkyColor(ClientMain game)
		: base(game)
	{
		MeshData meshData = ModelIcosahedronUtil.genIcosahedron(3, 250f);
		meshData.Uv = null;
		skyIcosahedron = game.Platform.UploadMesh(meshData);
		game.eventManager.RegisterRenderer(OnRenderFrame3D, EnumRenderStage.Opaque, Name, 0.2);
	}

	internal override void OnLevelFinalize()
	{
		skyTexture = game.Platform.CreateBitmapFromPng(game.AssetManager.Get("textures/environment/sky.png"));
		game.skyTextureId = game.Platform.LoadTexture((IBitmap)skyTexture, linearMag: false, 1, generateMipmaps: false);
		IAsset asset = ScreenManager.Platform.AssetManager.Get("textures/environment/sunlight.png");
		BitmapRef bitmapRef = game.Platform.CreateBitmapFromPng(asset);
		game.skyGlowTextureId = game.Platform.LoadTexture((IBitmap)bitmapRef, linearMag: true, 1, generateMipmaps: false);
		bitmapRef.Dispose();
	}

	public void OnRenderFrame3D(float deltaTime)
	{
		WireframeModes wireframeDebugRender = game.api.renderapi.WireframeDebugRender;
		if (game.Width != 0 && game.Height != 0 && !wireframeDebugRender.Vertex && skyTexture != null)
		{
			float num = 1.25f * GameMath.Max(game.GameWorldCalendar.DayLightStrength - game.GameWorldCalendar.MoonLightStrength / 2f, 0.05f);
			float num2 = (float)GameMath.Clamp((game.Player.Entity.Pos.Y - (double)game.SeaLevel - 1000.0) / 30000.0, 0.0, 1.0);
			num = Math.Max(0f, num * (1f - num2));
			game.shUniforms.SkyDaylight = num;
			game.shUniforms.DitherSeed = (game.frameSeed + 1) % Math.Max(1, game.Width * game.Height);
			game.shUniforms.SkyTextureId = game.skyTextureId;
			game.shUniforms.GlowTextureId = game.skyGlowTextureId;
			Vec3f sunPositionNormalized = game.GameWorldCalendar.SunPositionNormalized;
			Vec3f viewVector = EntityPos.GetViewVector(game.mouseYaw, game.mousePitch);
			game.GlMatrixModeModelView();
			ShaderProgramSky sky = ShaderPrograms.Sky;
			sky.Use();
			sky.Sky2D = game.skyTextureId;
			sky.Glow2D = game.skyGlowTextureId;
			sky.SunPosition = sunPositionNormalized;
			sky.DayLight = game.shUniforms.SkyDaylight;
			sky.PlayerPos = game.EntityPlayer.Pos.XYZ.ToVec3f();
			sky.DitherSeed = (game.frameSeed = (game.frameSeed + 1) % (game.Width * game.Height));
			sky.HorizontalResolution = game.Width;
			sky.PlayerToSealevelOffset = (float)game.EntityPlayer.Pos.Y - (float)game.SeaLevel;
			sky.RgbaFogIn = game.AmbientManager.BlendedFogColor;
			sky.RgbaAmbientIn = game.AmbientManager.BlendedAmbientColor;
			sky.FogDensityIn = game.AmbientManager.BlendedFogDensity;
			sky.FogMinIn = game.AmbientManager.BlendedFogMin;
			sky.HorizonFog = game.AmbientManager.BlendedCloudDensity;
			sky.ProjectionMatrix = game.CurrentProjectionMatrix;
			sky.SunsetMod = (game.shUniforms.SunsetMod = game.Calendar.SunsetMod);
			calcSunColor(sunPositionNormalized, viewVector);
			game.Platform.GlDisableDepthTest();
			game.GlPushMatrix();
			MatrixToolsd.MatFollowPlayer(game.MvMatrix.Top);
			sky.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(skyIcosahedron);
			game.GlPopMatrix();
			game.Reset3DProjection();
			sky.Stop();
			game.Platform.GlEnableDepthTest();
		}
	}

	protected void calcSunColor(Vec3f Sn, Vec3f viewVector)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		float num = (GameMath.Clamp(Sn.Y * 1.5f, -1f, 1f) + 1f) / 2f;
		float num2 = GameMath.Max(0f, (GameMath.Clamp((0f - Sn.Y) * 1.5f, -1f, 1f) + 0.9f) / 13f);
		SKColor pixelRel = skyTexture.GetPixelRel(num, 0.99f);
		Vec3f vec3f = game.GameWorldCalendar.ReflectColor.Clone();
		Vec3f vec3f2 = game.FogColorSky.Set((float)(int)((SKColor)(ref pixelRel)).Red / 255f + num2 / 2f, (float)(int)((SKColor)(ref pixelRel)).Green / 255f + num2 / 2f, (float)(int)((SKColor)(ref pixelRel)).Blue / 255f + num2 / 2f);
		float num3 = (vec3f.R + vec3f.G + vec3f.B) / 3f;
		float num4 = game.GameWorldCalendar.DayLightStrength - num3;
		float num5 = GameMath.Clamp(GameMath.Max(game.AmbientManager.BlendedFlatFogDensity * 40f, game.AmbientManager.BlendedCloudDensity * game.AmbientManager.BlendedCloudDensity), 0f, 1f);
		vec3f.R = num5 * num3 + (1f - num5) * vec3f.R;
		vec3f.G = num5 * num3 + (1f - num5) * vec3f.G;
		vec3f.B = num5 * num3 + (1f - num5) * vec3f.B;
		game.AmbientManager.Sunglow.AmbientColor.Value[0] = vec3f.R + num4;
		game.AmbientManager.Sunglow.AmbientColor.Value[1] = vec3f.G + num4;
		game.AmbientManager.Sunglow.AmbientColor.Value[2] = vec3f.B + num4;
		game.AmbientManager.Sunglow.AmbientColor.Weight = 1f;
		float num6 = (float)Math.Sqrt((Math.Abs(Sn.Y) + 0.2f) * (Math.Abs(Sn.Y) + 0.2f) + (viewVector.Z - Sn.Z) * (viewVector.Z - Sn.Z)) / 2f;
		game.AmbientManager.Sunglow.FogColor.Weight = 1f - num;
		float num7 = num6 * vec3f2.R + (1f - num6) * vec3f.R;
		float num8 = num6 * vec3f2.G + (1f - num6) * vec3f.G;
		float num9 = num6 * vec3f2.B + (1f - num6) * vec3f.B;
		num7 = num5 * num3 + (1f - num5) * num7;
		num8 = num5 * num3 + (1f - num5) * num8;
		num9 = num5 * num3 + (1f - num5) * num9;
		game.AmbientManager.Sunglow.FogColor.Value[0] = num7;
		game.AmbientManager.Sunglow.FogColor.Value[1] = num8;
		game.AmbientManager.Sunglow.FogColor.Value[2] = num9;
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}

	public override void Dispose(ClientMain game)
	{
		game.Platform.DeleteMesh(skyIcosahedron);
		game.Platform.GLDeleteTexture(game.skyGlowTextureId);
		game.Platform.GLDeleteTexture(game.skyTextureId);
		skyTexture?.Dispose();
	}
}
