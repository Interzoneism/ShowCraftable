using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemRenderShadowMap : ClientSystem
{
	private ShadowBox shadowBox;

	private double[] projectionMatrix = Mat4d.Create();

	private double[] lightViewMatrix = Mat4d.Create();

	private double[] projectionViewMatrix = Mat4d.Create();

	private double[] offset = createOffset();

	private double[] tmp = Mat4d.Create();

	private double[] targetPos = new double[3];

	private double[] center = new double[3];

	private double[] up = new double[3] { 0.0, 1.0, 0.0 };

	private Vec3d forward = new Vec3d();

	public override string Name => "res";

	public SystemRenderShadowMap(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterRenderer(OnRenderBefore, EnumRenderStage.Before, Name, 0.0);
		game.eventManager.RegisterRenderer(OnRenderShadowFar, EnumRenderStage.ShadowFar, Name, 0.0);
		game.eventManager.RegisterRenderer(OnRenderShadowFarDone, EnumRenderStage.ShadowFarDone, Name, 1.0);
		game.eventManager.RegisterRenderer(OnRenderShadowNear, EnumRenderStage.ShadowNear, Name, 0.0);
		game.eventManager.RegisterRenderer(OnRenderShadowNearDone, EnumRenderStage.ShadowNearDone, Name, 1.0);
		shadowBox = new ShadowBox(lightViewMatrix, game);
	}

	private void OnRenderBefore(float dt)
	{
		Vec3f sunPosition = game.Calendar.SunPosition;
		Vec3f moonPosition = game.Calendar.MoonPosition;
		AmbientModifier ambientModifier = game.AmbientManager.CurrentModifiers["weather"];
		double num = -0.1;
		if (ambientModifier != null)
		{
			num = (ambientModifier.FogDensity.Weight * ambientModifier.FogDensity.Value + ambientModifier.FogMin.Value * ambientModifier.FogMin.Weight + ambientModifier.FlatFogDensity.Weight * ambientModifier.FlatFogDensity.Value * Math.Max(0f, ambientModifier.FlatFogYPos.Weight * (float)((ambientModifier.FlatFogYPos.Value > 0f) ? 1 : 0))) * 12f;
		}
		ambientModifier = game.AmbientManager.CurrentModifiers["serverambient"];
		if (ambientModifier != null)
		{
			num += (double)((ambientModifier.FogDensity.Weight * ambientModifier.FogDensity.Value + ambientModifier.FogMin.Value * ambientModifier.FogMin.Weight + ambientModifier.FlatFogDensity.Weight * ambientModifier.FlatFogDensity.Value * Math.Max(0f, ambientModifier.FlatFogYPos.Weight * (float)((ambientModifier.FlatFogYPos.Value > 0f) ? 1 : 0))) * 12f);
		}
		float num2 = Math.Max(GameMath.Clamp(sunPosition.Y / 20f, 0f, 1f), GameMath.Clamp(moonPosition.Y / 4f, 0f, 1f) * Math.Min(1f, 2f * game.Calendar.MoonPhaseBrightness - 0.2f)) - (float)Math.Max(0.0, num);
		if ((double)num2 < 0.12)
		{
			game.shUniforms.DropShadowIntensity = (game.AmbientManager.DropShadowIntensity = Math.Max(0f, game.AmbientManager.DropShadowIntensity - dt / 4f));
		}
		else
		{
			game.shUniforms.DropShadowIntensity = (game.AmbientManager.DropShadowIntensity = num2);
		}
	}

	private void OnRenderShadowNear(float dt)
	{
		if (!((double)game.AmbientManager.DropShadowIntensity <= 0.01))
		{
			double num = 30 + 3 * (ClientSettings.ShadowMapQuality - 1);
			ShadowBox.ShadowBoxZExtend = 50f + 50f * Math.Abs(1f - game.Calendar.SunPositionNormalized.Y) + 100f;
			game.shUniforms.ShadowRangeNear = (float)num;
			game.shUniforms.ShadowZExtendNear = 0f;
			PrepareForShadowRendering(num, EnumFrameBuffer.ShadowmapNear, 16f);
			Mat4d.Mul(tmp, offset, projectionViewMatrix);
			for (int i = 0; i < 16; i++)
			{
				game.toShadowMapSpaceMatrixNear[i] = (float)tmp[i];
			}
			game.shUniforms.ToShadowMapSpaceMatrixNear = game.toShadowMapSpaceMatrixNear;
		}
	}

	private void OnRenderShadowFar(float dt)
	{
		if (!((double)game.AmbientManager.DropShadowIntensity <= 0.01))
		{
			int shadowMapQuality = ClientSettings.ShadowMapQuality;
			double num = ((shadowMapQuality != 1) ? ((double)(150 + 120 * (shadowMapQuality - 1))) : 60.0);
			ShadowBox.ShadowBoxZExtend = 100f + 60f * Math.Abs(1f - game.Calendar.SunPositionNormalized.Y);
			game.shUniforms.ShadowRangeFar = (float)num;
			PrepareForShadowRendering((shadowMapQuality > 1) ? (num / 2.0) : num, EnumFrameBuffer.ShadowmapFar, 0f);
			Mat4d.Mul(tmp, offset, projectionViewMatrix);
			for (int i = 0; i < 16; i++)
			{
				game.toShadowMapSpaceMatrixFar[i] = (float)tmp[i];
			}
			game.shUniforms.ToShadowMapSpaceMatrixFar = game.toShadowMapSpaceMatrixFar;
		}
	}

	private void PrepareForShadowRendering(double shadowDistance, EnumFrameBuffer fb, float cullExtraRange)
	{
		EntityPlayer entityPlayer = game.EntityPlayer;
		ShadowBox.SHADOW_DISTANCE = shadowDistance;
		shadowBox.calculateWidthsAndHeights();
		shadowBox.update();
		game.frustumCuller.shadowRangeX = shadowDistance + ShadowBox.ShadowBoxZExtend + (double)cullExtraRange;
		game.frustumCuller.shadowRangeZ = shadowDistance + (double)cullExtraRange;
		Vec3f vec3f = ((game.Calendar.MoonLightStrength > game.Calendar.SunLightStrength) ? game.Calendar.MoonPosition : game.Calendar.SunPosition);
		loadOrthoModeMatrix(projectionMatrix, shadowBox.Width, shadowBox.Height, shadowBox.Length);
		Mat4d.LookAt(lightViewMatrix, vec3f.ToDoubleArray(), new double[4], new double[3] { 0.0, 1.0, 0.0 });
		Mat4d.Mul(projectionViewMatrix, projectionMatrix, lightViewMatrix);
		game.Platform.LoadFrameBuffer(fb);
		game.Platform.ClearFrameBuffer(fb);
		ShaderProgramShadowmapgeneric chunkshadowmap = ShaderPrograms.Chunkshadowmap;
		chunkshadowmap.Use();
		game.PMatrix.Push(projectionMatrix);
		game.MvMatrix.Push(lightViewMatrix);
		for (int i = 0; i < 16; i++)
		{
			game.shadowMvpMatrix[i] = (float)projectionViewMatrix[i];
		}
		chunkshadowmap.MvpMatrix = game.shadowMvpMatrix;
		double[] array = Mat4d.Create();
		VectorTool.ToVectorInFixedSystem(0.0, 0.0, 0.0, entityPlayer.Pos.Pitch, (float)Math.PI / 2f - entityPlayer.Pos.Yaw, forward);
		center[0] = entityPlayer.CameraPos.X;
		center[1] = entityPlayer.CameraPos.Y;
		center[2] = entityPlayer.CameraPos.Z;
		targetPos[0] = entityPlayer.CameraPos.X + (double)vec3f.X;
		targetPos[1] = entityPlayer.CameraPos.Y + (double)vec3f.Y;
		targetPos[2] = entityPlayer.CameraPos.Z + (double)vec3f.Z;
		Mat4d.LookAt(array, targetPos, center, up);
		game.frustumCuller.CalcFrustumEquations(entityPlayer.Pos.AsBlockPos, game.PMatrix.Top, array);
	}

	private void OnRenderShadowNearDone(float dt)
	{
		ShaderProgramBase.CurrentShaderProgram.Stop();
		game.Platform.UnloadFrameBuffer(EnumFrameBuffer.ShadowmapNear);
		game.Platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
		game.PMatrix.Pop();
		game.MvMatrix.Pop();
	}

	private void OnRenderShadowFarDone(float dt)
	{
		ShaderProgramBase.CurrentShaderProgram.Stop();
		game.Platform.UnloadFrameBuffer(EnumFrameBuffer.ShadowmapFar);
		game.Platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
		game.PMatrix.Pop();
		game.MvMatrix.Pop();
	}

	private void loadOrthoModeMatrix(double[] projectionMatrix, double width, double height, double length)
	{
		Mat4d.Identity(projectionMatrix);
		projectionMatrix[0] = 2.0 / width;
		projectionMatrix[5] = 2.0 / height;
		projectionMatrix[10] = -2.0 / length;
		projectionMatrix[15] = 1.0;
	}

	private static double[] createOffset()
	{
		double[] array = Mat4d.Create();
		Mat4d.Translate(array, array, new double[3] { 0.5, 0.5, 0.5 });
		Mat4d.Scale(array, array, new double[3] { 0.5, 0.5, 0.5 });
		return array;
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
