using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

internal class SystemRenderNightSky : ClientSystem
{
	private MeshRef nightSkyBox;

	private int textureId;

	private int frameSeed;

	private float[] modelMatrix = Mat4f.Create();

	private BitmapRef[] bmps;

	public override string Name => "rens";

	public SystemRenderNightSky(ClientMain game)
		: base(game)
	{
		MeshData cubeOnlyScaleXyz = CubeMeshUtil.GetCubeOnlyScaleXyz(75f, 75f, new Vec3f());
		cubeOnlyScaleXyz.Uv = null;
		cubeOnlyScaleXyz.Rgba = null;
		nightSkyBox = game.Platform.UploadMesh(cubeOnlyScaleXyz);
		game.eventManager.RegisterRenderer(OnRenderFrame3D, EnumRenderStage.Opaque, Name, 0.1);
	}

	public override void OnBlockTexturesLoaded()
	{
		bmps = new BitmapRef[6];
		TyronThreadPool.QueueTask(LoadBitMaps);
	}

	private void LoadBitMaps()
	{
		bmps[0] = game.Platform.CreateBitmapFromPng(game.AssetManager.Get("textures/environment/stars-ft.png"));
		bmps[1] = game.Platform.CreateBitmapFromPng(game.AssetManager.Get("textures/environment/stars-bg.png"));
		bmps[2] = game.Platform.CreateBitmapFromPng(game.AssetManager.Get("textures/environment/stars-up.png"));
		bmps[3] = game.Platform.CreateBitmapFromPng(game.AssetManager.Get("textures/environment/stars-dn.png"));
		bmps[4] = game.Platform.CreateBitmapFromPng(game.AssetManager.Get("textures/environment/stars-lf.png"));
		bmps[5] = game.Platform.CreateBitmapFromPng(game.AssetManager.Get("textures/environment/stars-rt.png"));
		game.EnqueueGameLaunchTask(FinishBitMaps, "nightsky");
	}

	private void FinishBitMaps()
	{
		textureId = game.Platform.Load3DTextureCube(bmps);
		BitmapRef[] array = bmps;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Dispose();
		}
	}

	public void OnRenderFrame3D(float deltaTime)
	{
		float num = 1.25f * GameMath.Max(game.GameWorldCalendar.DayLightStrength - game.GameWorldCalendar.MoonLightStrength / 2f, 0.05f);
		EntityPos pos = game.EntityPlayer.Pos;
		float num2 = (float)GameMath.Clamp((pos.Y - (double)game.SeaLevel - 1000.0) / 30000.0, 0.0, 1.0);
		num = Math.Max(0f, num * (1f - num2));
		if (game.Width != 0 && !((double)num > 0.99))
		{
			game.GlMatrixModeModelView();
			game.Platform.GlDisableCullFace();
			game.Platform.GlDisableDepthTest();
			ShaderProgramNightsky nightsky = ShaderPrograms.Nightsky;
			nightsky.Use();
			nightsky.CtexCube = textureId;
			nightsky.DayLight = num;
			nightsky.RgbaFog = game.AmbientManager.BlendedFogColor;
			nightsky.HorizonFog = game.AmbientManager.BlendedCloudDensity;
			nightsky.PlayerToSealevelOffset = (float)pos.Y - (float)game.SeaLevel;
			nightsky.DitherSeed = (frameSeed = (frameSeed + 1) % (game.Width * game.Height));
			nightsky.HorizontalResolution = game.Width;
			nightsky.ProjectionMatrix = game.CurrentProjectionMatrix;
			nightsky.FogDensityIn = game.AmbientManager.BlendedFogDensity;
			nightsky.FogMinIn = game.AmbientManager.BlendedFogMin;
			double totalDays = game.GameWorldCalendar.TotalDays;
			float yearRel = game.GameWorldCalendar.YearRel;
			float rad = (float)GameMath.Mod(totalDays - (double)yearRel, 1.0) * ((float)Math.PI * 2f);
			float num3 = (float)game.GameWorldCalendar.OnGetLatitude(pos.Z);
			float value = (float)Math.Acos(GameMath.Sin(num3 * ((float)Math.PI / 2f)) * GameMath.Sin(0.40910518f) + GameMath.Cos(num3 * ((float)Math.PI / 2f)) * GameMath.Cos(0.40910518f));
			Mat4f.Identity(modelMatrix);
			Mat4f.Rotate(modelMatrix, modelMatrix, rad, new float[3]
			{
				0f,
				0f - GameMath.Sin(value),
				GameMath.Cos(value)
			});
			nightsky.ModelMatrix = modelMatrix;
			game.GlPushMatrix();
			MatrixToolsd.MatFollowPlayer(game.MvMatrix.Top);
			nightsky.ViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(nightSkyBox);
			game.GlPopMatrix();
			nightsky.Stop();
			game.Platform.GlEnableDepthTest();
			game.Platform.UnBindTextureCubeMap();
		}
	}

	public override void Dispose(ClientMain game)
	{
		game.Platform.DeleteMesh(nightSkyBox);
		game.Platform.GLDeleteTexture(textureId);
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
