using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class AmbientManager : IAmbientManager
{
	private OrderedDictionary<string, AmbientModifier> ambientModifiers;

	private ClientMain game;

	internal float DropShadowIntensity;

	public int ShadowQuality;

	internal AmbientModifier BaseModifier = AmbientModifier.DefaultAmbient;

	internal AmbientModifier Sunglow;

	public bool prevDynamicColourGrading;

	private float targetExtraContrastLevel;

	private float targetSepiaLevel;

	private float smoothedLightLevel = -1f;

	public Vec4f BlendedFogColor { get; set; }

	public Vec3f BlendedAmbientColor { get; set; }

	public float BlendedFogDensity { get; set; }

	public float BlendedFogMin { get; set; }

	public float BlendedFlatFogDensity { get; set; }

	public float BlendedFlatFogYOffset { get; set; }

	public float BlendedCloudBrightness { get; set; }

	public float BlendedCloudDensity { get; set; }

	public float BlendedCloudYPos { get; set; }

	public float BlendedFlatFogYPosForShader { get; set; }

	public float BlendedSceneBrightness { get; set; }

	public float BlendedFogBrightness { get; set; }

	public OrderedDictionary<string, AmbientModifier> CurrentModifiers => ambientModifiers;

	public float ViewDistance => ClientSettings.ViewDistance;

	public AmbientModifier Base => BaseModifier;

	public AmbientManager(ClientMain game)
	{
		BlendedFogColor = new Vec4f(1f, 1f, 1f, 1f);
		BlendedAmbientColor = new Vec3f();
		this.game = game;
		game.eventManager.RegisterRenderer(UpdateAmbient, EnumRenderStage.Before, "ambientmanager", 0.0);
		ambientModifiers = new OrderedDictionary<string, AmbientModifier>();
		ambientModifiers["sunglow"] = (Sunglow = new AmbientModifier
		{
			FogColor = WeightedFloatArray.New(new float[3] { 0.8f, 0.8f, 0.8f }, 0f),
			AmbientColor = WeightedFloatArray.New(new float[3] { 1f, 1f, 1f }, 0.9f),
			FogDensity = WeightedFloat.New(0f, 0f)
		}.EnsurePopulated());
		ambientModifiers["serverambient"] = new AmbientModifier().EnsurePopulated();
		ambientModifiers["night"] = new AmbientModifier().EnsurePopulated();
		ambientModifiers["water"] = new AmbientModifier
		{
			FogColor = WeightedFloatArray.New(new float[3] { 0.18f, 0.74f, 1f }, 0f),
			FogDensity = WeightedFloat.New(0.07f, 0f),
			FogMin = WeightedFloat.New(0.03f, 0f),
			AmbientColor = WeightedFloatArray.New(new float[3] { 0.18f, 0.74f, 1f }, 0f)
		}.EnsurePopulated();
		ambientModifiers["lava"] = new AmbientModifier
		{
			FogColor = WeightedFloatArray.New(new float[3]
			{
				1f,
				47f / 51f,
				27f / 85f
			}, 0f),
			FogDensity = WeightedFloat.New(0.3f, 0f),
			FogMin = WeightedFloat.New(0.5f, 0f),
			AmbientColor = WeightedFloatArray.New(new float[3]
			{
				1f,
				47f / 51f,
				27f / 85f
			}, 0f)
		}.EnsurePopulated();
		ambientModifiers["deepwater"] = new AmbientModifier
		{
			FogColor = WeightedFloatArray.New(new float[3] { 0f, 0f, 0.07f }, 0f),
			FogMin = WeightedFloat.New(0.1f, 0f),
			FogDensity = WeightedFloat.New(0.1f, 0f),
			AmbientColor = WeightedFloatArray.New(new float[3] { 0f, 0f, 0.07f }, 0f)
		}.EnsurePopulated();
		ambientModifiers["blackfogincaves"] = new AmbientModifier
		{
			FogColor = WeightedFloatArray.New(new float[3], 0f)
		}.EnsurePopulated();
		ClientSettings.Inst.AddWatcher<int>("viewDistance", OnViewDistanceChanged);
		ShadowQuality = ClientSettings.ShadowMapQuality;
		ClientSettings.Inst.AddWatcher<int>("shadowMapQuality", delegate
		{
			ShadowQuality = ClientSettings.ShadowMapQuality;
		});
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.DayLight, OnDayLightChanged);
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.EyesInWaterColorShift, OnPlayerSightBeingChangedByWater);
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.EyesInLavaColorShift, OnPlayerSightBeingChangedByLava);
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.EyesInWaterDepth, OnPlayerUnderWater);
	}

	public void LateInit()
	{
		game.api.eventapi.PlayerDimensionChanged += Eventapi_PlayerDimensionChanged;
	}

	private void Eventapi_PlayerDimensionChanged(IPlayer byPlayer)
	{
		UpdateAmbient(0f);
	}

	private void OnPlayerUnderWater(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		float num = GameMath.Clamp(newValues.EyesInWaterDepth / 70f, 0f, 1f);
		AmbientModifier ambientModifier = ambientModifiers["deepwater"];
		ambientModifier.FogColor.Weight = 0.95f * num;
		ambientModifier.AmbientColor.Weight = 0.85f * num;
	}

	private void UpdateDaylight(float dt)
	{
		if (smoothedLightLevel < 0f)
		{
			smoothedLightLevel = game.BlockAccessor.GetLightLevel(game.Player.Entity.Pos.AsBlockPos, EnumLightLevelType.OnlySunLight);
		}
		AmbientModifier ambientModifier = ambientModifiers["night"];
		float val = Math.Min(0.6f, 0f - game.Calendar.SunPositionNormalized.Y) - 0.75f * Math.Min(0.33f, game.Calendar.MoonLightStrength);
		ambientModifier.FogBrightness.Weight = GameMath.Clamp(1f - game.Calendar.DayLightStrength + GameMath.Clamp(val, 0f, 0.5f) * 0.85f, 0f, 0.88f);
		float num = GameMath.Clamp(1.5f * game.Calendar.DayLightStrength - 0.2f, 0.1f, 1f);
		ambientModifier.SceneBrightness.Weight = GameMath.Clamp(1f - num, 0f, 0.65f);
		BlockPos asBlockPos = game.player.Entity.Pos.AsBlockPos;
		int num2 = Math.Max(game.BlockAccessor.GetLightLevel(asBlockPos, EnumLightLevelType.OnlySunLight), game.BlockAccessor.GetLightLevel(asBlockPos.Up(), EnumLightLevelType.OnlySunLight));
		smoothedLightLevel += ((float)num2 - smoothedLightLevel) * dt;
		float num3 = GameMath.Clamp(3f * smoothedLightLevel / 20f, 0f, 1f);
		float num4 = (float)GameMath.Clamp(game.Player.Entity.Pos.Y / (double)game.SeaLevel, 0.0, 1.0);
		num4 *= num4;
		num3 *= num4;
		ambientModifiers["blackfogincaves"].FogColor.Weight = GameMath.Clamp(1f - num3, 0f, 1f);
	}

	private void OnDayLightChanged(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		AmbientModifier ambientModifier = ambientModifiers["night"];
		ambientModifier.FogBrightness.Value = 0f;
		ambientModifier.SceneBrightness.Value = 0f;
		OnPlayerSightBeingChangedByWater(oldValues, newValues);
	}

	private void OnPlayerSightBeingChangedByWater(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		AmbientModifier ambientModifier = ambientModifiers["water"];
		ambientModifier.FogColor.Weight = (float)(newValues.EyesInWaterColorShift * newValues.EyesInWaterColorShift) / 10000f;
		ambientModifier.AmbientColor.Weight = 0.75f * (float)newValues.EyesInWaterColorShift / 100f;
		ambientModifier.FogDensity.Weight = (float)newValues.EyesInWaterColorShift / 100f;
		ambientModifier.FogMin.Weight = (float)newValues.EyesInWaterColorShift / 100f;
		game.api.Render.ShaderUniforms.CameraUnderwater = (float)newValues.EyesInWaterColorShift / 100f;
		setWaterColors();
	}

	private void setWaterColors()
	{
		AmbientModifier ambientModifier = ambientModifiers["water"];
		float num = Math.Max(0.2f, game.Calendar.DayLightStrength);
		int num2 = game.WorldMap.ApplyColorMapOnRgba("climateWaterTint", null, -1, (int)game.EntityPlayer.Pos.X, (int)game.EntityPlayer.Pos.Y, (int)game.EntityPlayer.Pos.Z, flipRb: false);
		int[] array = ColorUtil.RgbToHsvInts(num2 & 0xFF, (num2 >> 8) & 0xFF, (num2 >> 16) & 0xFF);
		array[2] /= 2;
		array[2] = (int)((float)array[2] * num);
		int[] array2 = ColorUtil.Hsv2RgbInts(array[0], array[1], array[2]);
		float[] value = ambientModifier.FogColor.Value;
		value[0] = (float)array2[0] / 255f;
		value[1] = (float)array2[1] / 255f;
		value[2] = (float)array2[2] / 255f;
		ambientModifier.AmbientColor.Value[0] = value[0] * 2f;
		ambientModifier.AmbientColor.Value[1] = value[1] * 2f;
		ambientModifier.AmbientColor.Value[2] = value[2] * 2f;
		float num3 = game.EyesInWaterDepth();
		ambientModifier.AmbientColor.Weight = ((num3 > 0f) ? GameMath.Clamp(game.EyesInWaterDepth() / 30f, 0f, 1f) : 0f);
		array[1] /= 2;
		array2 = ColorUtil.Hsv2RgbInts(array[0], array[1], array[2]);
		game.api.Render.ShaderUniforms.WaterMurkColor = new Vec4f((float)array2[0] / 255f, (float)array2[1] / 255f, (float)array2[2] / 255f, 1f);
	}

	private void OnPlayerSightBeingChangedByLava(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		AmbientModifier ambientModifier = ambientModifiers["lava"];
		ambientModifier.FogColor.Weight = (float)(newValues.EyesInLavaColorShift * newValues.EyesInLavaColorShift) / 10000f;
		ambientModifier.AmbientColor.Weight = 0.5f * (float)newValues.EyesInLavaColorShift / 100f;
		ambientModifier.FogDensity.Weight = (float)newValues.EyesInLavaColorShift / 100f;
		ambientModifier.FogMin.Weight = (float)newValues.EyesInLavaColorShift / 100f;
	}

	private void OnViewDistanceChanged(int newValue)
	{
	}

	public void SetFogRange(float density, float min)
	{
		BaseModifier.FogDensity.Value = density;
		BaseModifier.FogMin.Value = min;
	}

	public void UpdateAmbient(float dt)
	{
		setWaterColors();
		updateColorGradingValues(dt);
		float[] array = new float[4]
		{
			BaseModifier.FogColor.Value[0],
			BaseModifier.FogColor.Value[1],
			BaseModifier.FogColor.Value[2],
			1f
		};
		float[] array2 = new float[3]
		{
			BaseModifier.AmbientColor.Value[0],
			BaseModifier.AmbientColor.Value[1],
			BaseModifier.AmbientColor.Value[2]
		};
		BlendedFogDensity = BaseModifier.FogDensity.Value;
		BlendedFogMin = BaseModifier.FogMin.Value;
		BlendedFlatFogDensity = BaseModifier.FlatFogDensity.Value;
		BlendedFlatFogYOffset = BaseModifier.FlatFogYPos.Value;
		BlendedCloudBrightness = BaseModifier.CloudBrightness.Value;
		BlendedCloudDensity = BaseModifier.CloudDensity.Value;
		BlendedSceneBrightness = BaseModifier.SceneBrightness.Value;
		BlendedFogBrightness = BaseModifier.FogBrightness.Value;
		UpdateDaylight(dt);
		float num = 0f;
		foreach (KeyValuePair<string, AmbientModifier> ambientModifier in ambientModifiers)
		{
			AmbientModifier value = ambientModifier.Value;
			num = value.FogColor.Weight;
			array[0] = num * value.FogColor.Value[0] + (1f - num) * array[0];
			array[1] = num * value.FogColor.Value[1] + (1f - num) * array[1];
			array[2] = num * value.FogColor.Value[2] + (1f - num) * array[2];
			num = value.AmbientColor.Weight;
			array2[0] = num * value.AmbientColor.Value[0] + (1f - num) * array2[0];
			array2[1] = num * value.AmbientColor.Value[1] + (1f - num) * array2[1];
			array2[2] = num * value.AmbientColor.Value[2] + (1f - num) * array2[2];
			num = value.FogDensity.Weight;
			BlendedFogDensity = num * num * value.FogDensity.Value + (1f - num) * (1f - num) * BlendedFogDensity;
			num = value.FlatFogDensity.Weight;
			BlendedFlatFogDensity = num * value.FlatFogDensity.Value + (1f - num) * BlendedFlatFogDensity;
			num = value.FogMin.Weight;
			BlendedFogMin = num * value.FogMin.Value + (1f - num) * BlendedFogMin;
			num = value.FlatFogYPos.Weight;
			BlendedFlatFogYOffset = num * value.FlatFogYPos.Value + (1f - num) * BlendedFlatFogYOffset;
			num = value.CloudBrightness.Weight;
			BlendedCloudBrightness = num * value.CloudBrightness.Value + (1f - num) * BlendedCloudBrightness;
			num = value.CloudDensity.Weight;
			BlendedCloudDensity = num * value.CloudDensity.Value + (1f - num) * BlendedCloudDensity;
			num = value.SceneBrightness.Weight;
			BlendedSceneBrightness = num * value.SceneBrightness.Value + (1f - num) * BlendedSceneBrightness;
			num = value.FogBrightness.Weight;
			BlendedFogBrightness = num * value.FogBrightness.Value + (1f - num) * BlendedFogBrightness;
		}
		array[0] *= BlendedSceneBrightness * BlendedFogBrightness;
		array[1] *= BlendedSceneBrightness * BlendedFogBrightness;
		array[2] *= BlendedSceneBrightness * BlendedFogBrightness;
		BlendedFogColor.Set(array);
		array2[0] *= BlendedSceneBrightness;
		array2[1] *= BlendedSceneBrightness;
		array2[2] *= BlendedSceneBrightness;
		BlendedAmbientColor.Set(array2);
		BlendedFlatFogYPosForShader = BlendedFlatFogYOffset + (float)game.SeaLevel;
		double num2 = Math.Max(0.0, (game.Player.Entity.Pos.Y - (double)game.SeaLevel - 5000.0) / 10000.0);
		BlendedFogMin = Math.Max(0f, BlendedFogMin - (float)num2);
		BlendedFogDensity = Math.Max(0f, BlendedFogDensity - (float)num2);
		if (float.IsNaN(BlendedFlatFogDensity))
		{
			BlendedFlatFogDensity = 0f;
		}
		else
		{
			BlendedFlatFogDensity = (float)((double)Math.Sign(BlendedFlatFogDensity) * Math.Max(0.0, (double)Math.Abs(BlendedFlatFogDensity) - num2));
		}
	}

	private void updateColorGradingValues(float dt)
	{
		DefaultShaderUniforms shaderUniforms = game.Platform.ShaderUniforms;
		if (ClientSettings.DynamicColorGrading)
		{
			if (!prevDynamicColourGrading)
			{
				prevDynamicColourGrading = true;
				shaderUniforms.ExtraContrastLevel = ClientSettings.ExtraContrastLevel;
				shaderUniforms.SepiaLevel = ClientSettings.SepiaLevel;
			}
			dt = Math.Min(0.2f, dt);
			BlockPos asBlockPos = game.player.Entity.Pos.XYZ.AsBlockPos;
			asBlockPos.Y = game.SeaLevel;
			ClimateCondition climateAt = game.World.BlockAccessor.GetClimateAt(asBlockPos);
			if (climateAt != null)
			{
				if (float.IsNaN(climateAt.Temperature) || float.IsNaN(climateAt.WorldgenRainfall))
				{
					game.Logger.Warning("Color grading: Temperature/Rainfall at {0} is {1}/{2}. Will ignore.", climateAt.Temperature, climateAt.WorldgenRainfall);
					return;
				}
				float glitchStrength = game.api.renderapi.ShaderUniforms.GlitchStrength;
				targetExtraContrastLevel = GameMath.Clamp((climateAt.Temperature + 5f) / 30f - glitchStrength, 0f, 0.5f);
				targetSepiaLevel = 0.2f + GameMath.Clamp((climateAt.Temperature + 5f) / 35f, 0f, 1f) * 0.2f;
				float num = targetExtraContrastLevel - shaderUniforms.ExtraContrastLevel;
				shaderUniforms.ExtraContrastLevel += num * dt;
				float num2 = targetSepiaLevel - shaderUniforms.SepiaLevel;
				shaderUniforms.SepiaLevel += num2 * dt;
				float val = Math.Max(0f, (climateAt.Temperature - 30f) / 20f) * Math.Max(0f, climateAt.WorldgenRainfall - 0.5f);
				game.api.Render.ShaderUniforms.AmbientBloomLevelAdd[0] = GameMath.Clamp(val, 0f, 2f);
			}
		}
		else
		{
			prevDynamicColourGrading = false;
			shaderUniforms.ExtraContrastLevel = ClientSettings.ExtraContrastLevel;
			shaderUniforms.SepiaLevel = ClientSettings.SepiaLevel;
		}
	}
}
