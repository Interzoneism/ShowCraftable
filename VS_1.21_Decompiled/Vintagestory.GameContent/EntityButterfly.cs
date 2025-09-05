using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class EntityButterfly : EntityAgent
{
	public double windMotion;

	private int cnt;

	private float flapPauseDt;

	public override bool IsInteractable => false;

	static EntityButterfly()
	{
		AiTaskRegistry.Register<AiTaskButterflyWander>("butterflywander");
		AiTaskRegistry.Register<AiTaskButterflyRest>("butterflyrest");
		AiTaskRegistry.Register<AiTaskButterflyChase>("butterflychase");
		AiTaskRegistry.Register<AiTaskButterflyFlee>("butterflyflee");
		AiTaskRegistry.Register<AiTaskButterflyFeedOnFlowers>("butterflyfeedonflowers");
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		if (api.Side == EnumAppSide.Client)
		{
			WatchedAttributes.RegisterModifiedListener("windWaveIntensity", delegate
			{
				(base.Properties.Client.Renderer as EntityShapeRenderer).WindWaveIntensity = WatchedAttributes.GetDouble("windWaveIntensity");
			});
		}
		if (api.World.BlockAccessor.GetClimateAt(Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, api.World.Calendar.TotalDays).Temperature < 0f)
		{
			Die(EnumDespawnReason.Removed);
		}
	}

	public override void OnGameTick(float dt)
	{
		if (World.Side == EnumAppSide.Server)
		{
			base.OnGameTick(dt);
			return;
		}
		if (FeetInLiquid)
		{
			(base.Properties.Client.Renderer as EntityShapeRenderer).AddRenderFlags |= 201326592;
		}
		else
		{
			(base.Properties.Client.Renderer as EntityShapeRenderer).AddRenderFlags &= -201326593;
		}
		if (!AnimManager.ActiveAnimationsByAnimCode.ContainsKey("feed") && !AnimManager.ActiveAnimationsByAnimCode.ContainsKey("rest"))
		{
			if (ServerPos.Y < Pos.Y - 0.05 && !base.Collided && !FeetInLiquid)
			{
				SetAnimation("glide", 1f);
			}
			if (FeetInLiquid)
			{
				StopAnimation("glide");
			}
			if ((ServerPos.Y > Pos.Y - 0.02 || base.Collided) && !FeetInLiquid)
			{
				SetAnimation("fly", 2.5f);
			}
			if (FeetInLiquid && flapPauseDt <= 0f && Api.World.Rand.NextDouble() < 0.06)
			{
				flapPauseDt = 2f + 6f * (float)Api.World.Rand.NextDouble();
				StopAnimation("fly");
			}
			if (flapPauseDt > 0f)
			{
				flapPauseDt -= dt;
				if (flapPauseDt <= 0f)
				{
					SetAnimation("fly", 2.5f);
				}
			}
			else if (FeetInLiquid)
			{
				EntityPos pos = Pos;
				double num = SelectionBox.XSize * 0.75f;
				Entity.SplashParticleProps.BasePos.Set(pos.X - num / 2.0, pos.Y - 0.05, pos.Z - num / 2.0);
				Entity.SplashParticleProps.AddPos.Set(num, 0.0, num);
				Entity.SplashParticleProps.AddVelocity.Set(0f, 0f, 0f);
				Entity.SplashParticleProps.QuantityMul = 0.01f;
				World.SpawnParticles(Entity.SplashParticleProps);
				SpawnWaterMovementParticles(1f, 0.0, 0.05);
			}
		}
		base.OnGameTick(dt);
		if (cnt++ > 30)
		{
			float num2 = ((World.BlockAccessor.GetLightLevel(base.SidedPos.XYZ.AsBlockPos, EnumLightLevelType.OnlySunLight) < 14) ? 1 : 0);
			windMotion = Api.ModLoader.GetModSystem<WeatherSystemBase>().WeatherDataSlowAccess.GetWindSpeed(base.SidedPos.XYZ) * (double)num2;
			cnt = 0;
		}
		if (AnimManager.ActiveAnimationsByAnimCode.ContainsKey("fly"))
		{
			base.SidedPos.X += Math.Max(0.0, (windMotion - 0.2) / 20.0);
		}
	}

	private void SetAnimation(string animCode, float speed)
	{
		if (!AnimManager.ActiveAnimationsByAnimCode.TryGetValue(animCode, out var value))
		{
			value = new AnimationMetaData
			{
				Code = animCode,
				Animation = animCode,
				AnimationSpeed = speed
			};
			AnimManager.ActiveAnimationsByAnimCode.Clear();
			AnimManager.ActiveAnimationsByAnimCode[value.Animation] = value;
		}
		else
		{
			value.AnimationSpeed = speed;
			UpdateDebugAttributes();
		}
	}

	public override void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
	{
		if (activeAnimationsCount == 0)
		{
			AnimManager.ActiveAnimationsByAnimCode.Clear();
			AnimManager.StartAnimation("fly");
		}
		string text = "";
		bool flag = false;
		for (int i = 0; i < activeAnimationsCount; i++)
		{
			int num = activeAnimations[i];
			for (int j = 0; j < base.Properties.Client.LoadedShape.Animations.Length; j++)
			{
				Animation animation = base.Properties.Client.LoadedShape.Animations[j];
				int num2 = int.MaxValue;
				if ((animation.CodeCrc32 & num2) != (num & num2))
				{
					continue;
				}
				if (AnimManager.ActiveAnimationsByAnimCode.ContainsKey(animation.Code))
				{
					break;
				}
				if (!(animation.Code == "glide") && !(animation.Code == "fly"))
				{
					string text2 = ((animation.Code == null) ? animation.Name.ToLowerInvariant() : animation.Code);
					text = text + ", " + text2;
					base.Properties.Client.AnimationsByMetaCode.TryGetValue(text2, out var value);
					if (value == null)
					{
						value = new AnimationMetaData
						{
							Code = text2,
							Animation = text2,
							CodeCrc32 = animation.CodeCrc32
						};
					}
					value.AnimationSpeed = activeAnimationSpeeds[i];
					AnimManager.ActiveAnimationsByAnimCode[animation.Code] = value;
					flag = true;
				}
			}
		}
		if (flag)
		{
			AnimManager.StopAnimation("fly");
			AnimManager.StopAnimation("glide");
			(base.Properties.Client.Renderer as EntityShapeRenderer).AddRenderFlags = 134217728;
			(base.Properties.Client.Renderer as EntityShapeRenderer).AddRenderFlags |= 536870912;
		}
		else
		{
			(base.Properties.Client.Renderer as EntityShapeRenderer).AddRenderFlags = 0;
		}
	}
}
