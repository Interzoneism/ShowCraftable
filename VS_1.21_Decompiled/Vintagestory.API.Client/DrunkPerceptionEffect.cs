using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class DrunkPerceptionEffect : PerceptionEffect
{
	private NormalizedSimplexNoise noisegen = NormalizedSimplexNoise.FromDefaultOctaves(4, 1.0, 0.9, 123L);

	private float accum;

	private float accum1s;

	private float targetIntensity;

	public DrunkPerceptionEffect(ICoreClientAPI capi)
		: base(capi)
	{
	}

	public override void OnBeforeGameRender(float dt)
	{
		if (!capi.IsGamePaused && capi.World.Player.Entity.AnimManager.HeadController != null)
		{
			capi.Render.ShaderUniforms.PerceptionEffectIntensity = Intensity;
			accum1s += dt;
			if (accum1s > 1f)
			{
				accum1s = 0f;
				targetIntensity = capi.World.Player.Entity.WatchedAttributes.GetFloat("intoxication");
			}
			Intensity += (targetIntensity - Intensity) * dt / 3f;
			accum = (float)((double)capi.InWorldEllapsedMilliseconds / 3000.0 % 100.0 * Math.PI);
			float num = Intensity / 250f;
			float num2 = (float)(Math.Cos((double)accum / 1.15) + Math.Cos(accum / 1.35f)) * num / 2f;
			capi.World.Player.Entity.Pos.Pitch += num2;
			capi.Input.MousePitch += num2;
			capi.Input.MouseYaw += (float)(Math.Sin((double)accum / 1.1) + Math.Sin(accum / 1.5f) + Math.Sin(accum / 5f) * 0.20000000298023224) * num;
			if (!capi.Input.MouseGrabbed)
			{
				capi.World.Player.Entity.Pos.Yaw = capi.Input.MouseYaw;
			}
			EntityHeadController headController = capi.World.Player.Entity.AnimManager.HeadController;
			headController.yawOffset = (float)(Math.Cos((double)accum / 1.12) + Math.Cos(accum / 1.2f) + Math.Cos(accum / 4f) * 0.20000000298023224) * num * 60f;
			accum /= 2f;
			headController.pitchOffset = (float)(Math.Sin((double)accum / 1.12) + Math.Sin(accum / 1.2f) + Math.Sin(accum / 4f) * 0.20000000298023224) * num * 30f;
			headController.pitchOffset = (float)(Math.Sin((double)accum / 1.12) + Math.Sin(accum / 1.2f) + Math.Sin(accum / 4f) * 0.20000000298023224) * num * 30f;
			double num3 = (float)((double)capi.InWorldEllapsedMilliseconds / 9000.0 % 100.0 * Math.PI);
			float perceptionEffectIntensity = capi.Render.ShaderUniforms.PerceptionEffectIntensity;
			capi.Render.ShaderUniforms.AmbientBloomLevelAdd[1] = GameMath.Clamp((float)Math.Abs(Math.Cos(num3 / 1.12) + Math.Sin(num3 / 2.2) + Math.Cos(num3 * 2.3)) * perceptionEffectIntensity * 2f, perceptionEffectIntensity / 3f, 1.8f);
		}
	}

	public override void ApplyToFpHand(Matrixf modelMat)
	{
		float num = Intensity / 10f;
		modelMat.Translate(GameMath.Sin(accum) * num, (double)GameMath.Sin(accum) * 1.2 * (double)num, 0.0);
		modelMat.RotateX(GameMath.Cos(accum * 0.8f) * num);
		modelMat.RotateZ(GameMath.Cos(accum * 1.1f) * num);
	}

	public override void ApplyToTpPlayer(EntityPlayer entityPlr, float[] modelMatrix, float? playerIntensity = null)
	{
		if (entityPlr.Player is IClientPlayer clientPlayer && entityPlr.AnimManager.Animator != null && (clientPlayer.CameraMode != EnumCameraMode.FirstPerson || clientPlayer.ImmersiveFpMode))
		{
			float num = ((!playerIntensity.HasValue) ? Intensity : playerIntensity.Value);
			ElementPose posebyName = entityPlr.AnimManager.Animator.GetPosebyName("root");
			posebyName.degOffX = GameMath.Sin(accum) / 5f * num * (180f / (float)Math.PI);
			posebyName.degOffZ = GameMath.Sin(accum * 1.2f) / 5f * num * (180f / (float)Math.PI);
		}
	}

	public override void NowActive(float intensity)
	{
		base.NowActive(intensity);
		capi.Render.ShaderUniforms.PerceptionEffectId = 2;
	}
}
