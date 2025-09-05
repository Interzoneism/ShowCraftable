using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class SystemRenderSunMoon : ClientSystem
{
	private const float maxMoonProximityFactor = 0.2f;

	private MeshRef quadModel;

	private int suntextureId;

	private int[] moontextureIds;

	internal int ImageSize;

	public Matrixf ModelMat = new Matrixf();

	private int occlQueryId;

	private bool nowQuerying;

	private Matrix4 sunmat;

	private Matrix4 moonmat;

	private float targetSunSpec;

	private bool firstTickDone;

	public float sunScale = 0.04f;

	public float moonScale = 0.023100002f;

	private static Vec3f YAxis = new Vec3f(0f, 1f, 0f);

	public override string Name => "resm";

	public SystemRenderSunMoon(ClientMain game)
		: base(game)
	{
		suntextureId = -1;
		moontextureIds = new int[8];
		moontextureIds.Fill(-1);
		ImageSize = 256;
		MeshData customQuadModelData = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, ImageSize, ImageSize, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		customQuadModelData.Flags = new int[4];
		quadModel = game.Platform.UploadMesh(customQuadModelData);
		game.eventManager.RegisterRenderer(OnRenderFrame3D, EnumRenderStage.Opaque, Name, 0.3);
		game.eventManager.RegisterRenderer(OnRenderFrame3DPost, EnumRenderStage.Opaque, Name, 999.0);
		GL.GenQueries(1, ref occlQueryId);
	}

	private void OnRenderFrame3DPost(float obj)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		ClientPlatformAbstract platform = game.Platform;
		platform.GlEnableDepthTest();
		platform.GlToggleBlend(on: true);
		platform.GlDisableCullFace();
		platform.GlDepthMask(flag: false);
		GL.ColorMask(false, false, false, false);
		Vec3f sunPosition = game.Calendar.SunPosition;
		Quaternion val = CreateLookRotation(new Vector3(sunPosition.X, sunPosition.Y, sunPosition.Z));
		sunmat = Matrix4.CreateTranslation((float)(-ImageSize / 2), (float)(-ImageSize), (float)(-ImageSize / 2)) * Matrix4.CreateScale(sunScale, sunScale * 7f, sunScale) * Matrix4.CreateFromQuaternion(val) * Matrix4.CreateTranslation(new Vector3(sunPosition.X, sunPosition.Y, sunPosition.Z));
		ShaderProgramStandard standard = ShaderPrograms.Standard;
		standard.Use();
		standard.RgbaTint = ColorUtil.WhiteArgbVec;
		standard.RgbaAmbientIn = ColorUtil.WhiteRgbVec;
		standard.RgbaLightIn = new Vec4f(0f, 0f, 0f, (float)Math.Sin((double)game.ElapsedMilliseconds / 1000.0) / 2f + 0.5f);
		standard.RgbaFogIn = game.AmbientManager.BlendedFogColor;
		standard.ExtraGlow = 0;
		standard.FogMinIn = game.AmbientManager.BlendedFogMin;
		standard.FogDensityIn = game.AmbientManager.BlendedFogDensity;
		standard.DontWarpVertices = 0;
		standard.AddRenderFlags = 0;
		standard.ExtraZOffset = 0f;
		standard.NormalShaded = 0;
		standard.OverlayOpacity = 0f;
		standard.ExtraGodray = 1f / 3f;
		standard.UniformMatrix("modelMatrix", ref sunmat);
		standard.ViewMatrix = game.api.renderapi.CameraMatrixOriginf;
		standard.ProjectionMatrix = game.api.renderapi.CurrentProjectionMatrix;
		standard.Tex2D = suntextureId;
		if (firstTickDone)
		{
			int num = default(int);
			GL.GetQueryObject(occlQueryId, (GetQueryObjectParam)34919, ref num);
			if (num > 0)
			{
				int num2 = default(int);
				GL.GetQueryObject(occlQueryId, (GetQueryObjectParam)34918, ref num2);
				targetSunSpec = GameMath.Clamp((float)num2 / 1500f, 0f, 1f);
				nowQuerying = false;
			}
		}
		firstTickDone = true;
		bool flag = false;
		if (!nowQuerying)
		{
			GL.BeginQuery((QueryTarget)35092, occlQueryId);
			nowQuerying = true;
			flag = true;
		}
		platform.RenderMesh(quadModel);
		standard.Stop();
		if (flag)
		{
			GL.EndQuery((QueryTarget)35092);
		}
		platform.GlDepthMask(flag: true);
		GL.ColorMask(true, true, true, true);
	}

	public void OnRenderFrame3D(float dt)
	{
		ClientPlatformAbstract platform = game.Platform;
		if (suntextureId == -1)
		{
			suntextureId = game.GetOrLoadCachedTexture(new AssetLocation("environment/sun.png"));
			for (int i = 0; i < 8; i++)
			{
				moontextureIds[i] = game.GetOrLoadCachedTexture(new AssetLocation("environment/moon/" + i + ".png"));
			}
		}
		Vec3f moonPosition = game.Calendar.MoonPosition;
		Vec3f sunPositionNormalized = game.GameWorldCalendar.SunPositionNormalized;
		game.shUniforms.SunPosition3D = sunPositionNormalized;
		Vec3f vec3f = moonPosition.Clone().Normalize();
		float moonLightStrength = game.GameWorldCalendar.MoonLightStrength;
		float sunLightStrength = game.GameWorldCalendar.SunLightStrength;
		float t = GameMath.Clamp(50f * (moonLightStrength - sunLightStrength), 0f, 1f);
		game.shUniforms.LightPosition3D.Set(GameMath.Lerp(sunPositionNormalized.X, vec3f.X, t), GameMath.Lerp(sunPositionNormalized.Y, vec3f.Y, t), GameMath.Lerp(sunPositionNormalized.Z, vec3f.Z, t));
		if (sunPositionNormalized.Y < -0.05f)
		{
			double[] top = game.PMatrix.Top;
			double[] cameraMatrixOrigin = game.api.renderapi.CameraMatrixOrigin;
			double[] array = new double[16];
			Mat4d.Mul(array, cameraMatrixOrigin, ModelMat.ValuesAsDouble);
			Vec3d vec3d = MatrixToolsd.Project(new Vec3d((float)ImageSize / 4f, (float)(-ImageSize) / 4f, 0.0), top, array, game.Width, game.Height);
			Vec3f sunPositionScreen = new Vec3f((float)vec3d.X / (float)game.Width * 2f - 1f, (float)vec3d.Y / (float)game.Height * 2f - 1f, (float)vec3d.Z);
			game.shUniforms.SunPositionScreen = sunPositionScreen;
		}
		platform.GlToggleBlend(on: true);
		platform.GlDisableCullFace();
		platform.GlDisableDepthTest();
		prepareSunMat();
		Vec3f vec3f2 = game.Calendar.SunColor.Clone();
		float num = (vec3f2.R + vec3f2.G + vec3f2.B) / 3f;
		float num2 = GameMath.Clamp(GameMath.Max(game.AmbientManager.BlendedFlatFogDensity * 40f, game.AmbientManager.BlendedCloudDensity * game.AmbientManager.BlendedCloudDensity), 0f, 1f);
		vec3f2.R = num2 * num + (1f - num2) * vec3f2.R;
		vec3f2.G = num2 * num + (1f - num2) * vec3f2.G;
		vec3f2.B = num2 * num + (1f - num2) * vec3f2.B;
		ShaderProgramStandard standard = ShaderPrograms.Standard;
		standard.Use();
		standard.Uniform("skyShaded", 1);
		Vec4f vec4f = new Vec4f(1f, 1f, 1f, 1f);
		DefaultShaderUniforms shaderUniforms = game.api.renderapi.ShaderUniforms;
		if (shaderUniforms.FogSphereQuantity > 0)
		{
			for (int j = 0; j < shaderUniforms.FogSphereQuantity; j++)
			{
				float num3 = shaderUniforms.FogSpheres[j * 8];
				float num4 = shaderUniforms.FogSpheres[j * 8 + 1];
				float num5 = shaderUniforms.FogSpheres[j * 8 + 2];
				float num6 = shaderUniforms.FogSpheres[j * 8 + 3];
				float num7 = shaderUniforms.FogSpheres[j * 8 + 4];
				double num8 = Math.Sqrt(num3 * num3 + num4 * num4 + num5 * num5);
				double num9 = (1.0 - num8 / (double)num6) * (double)num6 * (double)num7;
				vec4f.A = (float)GameMath.Clamp((double)vec4f.A - num9, 0.0, 1.0);
			}
		}
		standard.FadeFromSpheresFog = 1;
		standard.RgbaTint = vec4f;
		standard.RgbaAmbientIn = ColorUtil.WhiteRgbVec;
		standard.RgbaLightIn = new Vec4f(vec3f2.R, vec3f2.G, vec3f2.B, 1f);
		standard.RgbaFogIn = game.AmbientManager.BlendedFogColor;
		standard.ExtraGlow = 0;
		standard.FogMinIn = game.AmbientManager.BlendedFogMin;
		standard.FogDensityIn = game.AmbientManager.BlendedFogDensity;
		standard.DontWarpVertices = 0;
		standard.AddRenderFlags = 0;
		standard.ExtraZOffset = 0f;
		standard.NormalShaded = 0;
		standard.OverlayOpacity = 0f;
		standard.ExtraGodray = 1f / 3f;
		standard.ShadowIntensity = 0f;
		standard.ApplySsao = 0;
		standard.AlphaTest = 0.01f;
		standard.UniformMatrix("modelMatrix", ref sunmat);
		standard.ViewMatrix = game.api.renderapi.CameraMatrixOriginf;
		standard.ProjectionMatrix = game.api.renderapi.CurrentProjectionMatrix;
		standard.Tex2D = suntextureId;
		platform.RenderMesh(quadModel);
		standard.Uniform("skyShaded", 0);
		standard.ExtraGodray = 0f;
		standard.ApplySsao = 1;
		standard.FadeFromSpheresFog = 0;
		standard.Stop();
		if (sunPositionNormalized.Y >= -0.05f)
		{
			double[] top2 = game.PMatrix.Top;
			double[] cameraMatrixOrigin2 = game.api.renderapi.CameraMatrixOrigin;
			double[] array2 = new double[16];
			Mat4d.Mul(array2, cameraMatrixOrigin2, new double[16]
			{
				((Matrix4)(ref sunmat)).M11,
				((Matrix4)(ref sunmat)).M12,
				((Matrix4)(ref sunmat)).M13,
				((Matrix4)(ref sunmat)).M14,
				((Matrix4)(ref sunmat)).M21,
				((Matrix4)(ref sunmat)).M22,
				((Matrix4)(ref sunmat)).M23,
				((Matrix4)(ref sunmat)).M24,
				((Matrix4)(ref sunmat)).M31,
				((Matrix4)(ref sunmat)).M32,
				((Matrix4)(ref sunmat)).M33,
				((Matrix4)(ref sunmat)).M34,
				((Matrix4)(ref sunmat)).M41,
				((Matrix4)(ref sunmat)).M42,
				((Matrix4)(ref sunmat)).M43,
				((Matrix4)(ref sunmat)).M44
			});
			Vec3d vec3d2 = MatrixToolsd.Project(new Vec3d((float)ImageSize / 2f, (float)ImageSize / 2f, 0.0), top2, array2, game.Width, game.Height);
			Vec3f sunPositionScreen2 = new Vec3f((float)vec3d2.X / (float)game.Width * 2f - 1f, (float)vec3d2.Y / (float)game.Height * 2f - 1f, (float)vec3d2.Z);
			game.shUniforms.SunPositionScreen = sunPositionScreen2;
		}
		game.shUniforms.SunSpecularIntensity = GameMath.Clamp(game.shUniforms.SunSpecularIntensity + (targetSunSpec - game.shUniforms.SunSpecularIntensity) * dt * 20f, 0f, 1f);
		prepareMoonMat(moonPosition, vec3f);
		float angleSunFromMoon = getAngleSunFromMoon(sunPositionNormalized.Clone().Sub(vec3f), vec3f);
		ShaderProgramCelestialobject celestialobject = ShaderPrograms.Celestialobject;
		celestialobject.Use();
		celestialobject.Sky2D = game.skyTextureId;
		celestialobject.Glow2D = game.skyGlowTextureId;
		celestialobject.SunPosition = sunPositionNormalized;
		celestialobject.Uniform("moonPosition", vec3f);
		celestialobject.Uniform("moonSunAngle", angleSunFromMoon);
		celestialobject.DayLight = game.shUniforms.SkyDaylight;
		celestialobject.WeirdMathToMakeMoonLookNicer = 1;
		celestialobject.DitherSeed = (game.frameSeed + 1) % Math.Max(1, game.Width * game.Height);
		celestialobject.HorizontalResolution = game.Width;
		celestialobject.PlayerToSealevelOffset = (float)game.EntityPlayer.Pos.Y - (float)game.SeaLevel;
		celestialobject.RgbaFogIn = game.AmbientManager.BlendedFogColor;
		celestialobject.FogMinIn = game.AmbientManager.BlendedFogMin;
		celestialobject.FogDensityIn = game.AmbientManager.BlendedFogDensity;
		celestialobject.HorizonFog = game.AmbientManager.BlendedCloudDensity;
		celestialobject.ExtraGlow = 0;
		celestialobject.ExtraGodray = 0.5f;
		celestialobject.UniformMatrix("modelMatrix", ref moonmat);
		celestialobject.ViewMatrix = game.api.renderapi.CameraMatrixOriginf;
		celestialobject.ProjectionMatrix = game.api.renderapi.CurrentProjectionMatrix;
		celestialobject.Tex2D = moontextureIds[4];
		platform.RenderMesh(quadModel);
		celestialobject.WeirdMathToMakeMoonLookNicer = 0;
		celestialobject.Stop();
		platform.GlToggleBlend(on: false);
		platform.GlEnableDepthTest();
	}

	public void prepareSunMat()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		Vec3f sunPosition = game.Calendar.SunPosition;
		float num = sunPosition.Y + (float)game.EntityPlayer.LocalEyePos.Y - ((float)game.EntityPlayer.Pos.Y - (float)game.SeaLevel) / 10000f;
		Quaternion val = CreateLookRotation(new Vector3(sunPosition.X, num, sunPosition.Z));
		sunmat = Matrix4.CreateTranslation((float)(-ImageSize / 2), (float)(-ImageSize / 2), (float)(-ImageSize / 2)) * Matrix4.CreateScale(sunScale) * Matrix4.CreateFromQuaternion(val) * Matrix4.CreateTranslation(new Vector3(sunPosition.X, num, sunPosition.Z));
	}

	public void prepareMoonMat(Vec3f moonPos, Vec3f moonPosNormalised)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		float num = GameMath.Clamp((game.Calendar.SunPositionNormalized.Dot(moonPosNormalised) - 0.99f) * 40f, 0f, 0.2f);
		float num2 = moonPos.Y + (float)game.EntityPlayer.LocalEyePos.Y - ((float)game.EntityPlayer.Pos.Y - (float)game.SeaLevel) / 10000f;
		bool flip = (int)game.Calendar.MoonPhaseExact > 4;
		Quaternion val = CreateLookRotationMoon(new Vector3(moonPos.X, num2, moonPos.Z), game.Calendar.SunPositionNormalized.Clone().Sub(moonPosNormalised), moonPosNormalised, flip, num);
		moonmat = Matrix4.CreateTranslation((float)(-ImageSize / 2), (float)(-ImageSize / 2), (float)(-ImageSize / 2)) * Matrix4.CreateScale(moonScale * (1.1f + num)) * Matrix4.CreateFromQuaternion(val) * Matrix4.CreateTranslation(new Vector3(moonPos.X, num2, moonPos.Z));
	}

	public static Quaternion CreateLookRotation(Vector3 direction)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = new Vector3(direction.X, 0f, direction.Z);
		Vector3 val2 = ((Vector3)(ref val)).Normalized();
		double num = Math.Atan2(val2.X, val2.Z);
		Quaternion val3 = Quaternion.FromAxisAngle(Vector3.UnitY, (float)num);
		Vector2 val4 = new Vector2(direction.X, direction.Z);
		float length = ((Vector2)(ref val4)).Length;
		val = new Vector3(0f, direction.Y, length);
		Vector3 val5 = ((Vector3)(ref val)).Normalized();
		double num2 = Math.Atan2(val5.Y, val5.Z);
		Quaternion val6 = Quaternion.FromAxisAngle(-Vector3.UnitX, (float)num2);
		return val3 * val6;
	}

	public static Quaternion CreateLookRotationMoon(Vector3 direction, Vec3f sunVecRel, Vec3f moonVec, bool flip, float proximityFactor)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = new Vector3(direction.X, 0f, direction.Z);
		Vector3 val2 = ((Vector3)(ref val)).Normalized();
		double num = Math.Atan2(val2.X, val2.Z);
		Quaternion val3 = Quaternion.FromAxisAngle(Vector3.UnitY, (float)num);
		Vector2 val4 = new Vector2(direction.X, direction.Z);
		float length = ((Vector2)(ref val4)).Length;
		val = new Vector3(0f, direction.Y, length);
		Vector3 val5 = ((Vector3)(ref val)).Normalized();
		double num2 = Math.Atan2(val5.Y, val5.Z);
		Quaternion val6 = Quaternion.FromAxisAngle(-Vector3.UnitX, (float)num2);
		return val3 * val6;
	}

	private float getAngleSunFromMoon(Vec3f sunVecRel, Vec3f moonVec)
	{
		sunVecRel.Sub(moonVec.Clone().Mul(moonVec.Dot(sunVecRel)));
		Vec3f vec3f = moonVec.Cross(YAxis);
		float num = sunVecRel.Length() * vec3f.Length();
		if (num == 0f)
		{
			return 0f;
		}
		return (float)Math.Acos(sunVecRel.Dot(vec3f) / num);
	}

	public override void Dispose(ClientMain game)
	{
		game.Platform.DeleteMesh(quadModel);
		game.Platform.GLDeleteTexture(suntextureId);
		for (int i = 0; i < moontextureIds.Length; i++)
		{
			game.Platform.GLDeleteTexture(moontextureIds[i]);
		}
		GL.DeleteQuery(occlQueryId);
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
