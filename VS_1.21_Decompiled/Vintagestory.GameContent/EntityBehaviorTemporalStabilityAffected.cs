using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorTemporalStabilityAffected : EntityBehavior
{
	private ILoadedSound tempStabSoundDrain;

	private ILoadedSound tempStabSoundLow;

	private ILoadedSound tempStabSoundVeryLow;

	private AmbientModifier rainfogAmbient;

	private SimpleParticleProperties rustParticles;

	private NormalizedSimplexNoise fogNoise;

	private ICoreClientAPI capi;

	private SystemTemporalStability tempStabilitySystem;

	private WeatherSimulationParticles precipParticleSys;

	private float oneSecAccum;

	private float threeSecAccum;

	private double hereTempStabChangeVelocity;

	private double glitchEffectStrength;

	private double fogEffectStrength;

	private double rustPrecipColorStrength;

	private bool requireInitSounds;

	private bool enabled = true;

	private bool isSelf;

	private bool isCommand;

	private BlockPos tmpPos = new BlockPos();

	public double stabilityOffset;

	private float jitterOffset;

	private float jitterOffsetedDuration;

	public double TempStabChangeVelocity { get; set; }

	public double GlichEffectStrength => glitchEffectStrength;

	public double OwnStability
	{
		get
		{
			return entity.WatchedAttributes.GetDouble("temporalStability");
		}
		set
		{
			entity.WatchedAttributes.SetDouble("temporalStability", value);
		}
	}

	public EntityBehaviorTemporalStabilityAffected(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		tempStabilitySystem = entity.Api.ModLoader.GetModSystem<SystemTemporalStability>();
		if (entity.Api.Side == EnumAppSide.Client)
		{
			requireInitSounds = true;
			precipParticleSys = entity.Api.ModLoader.GetModSystem<WeatherSystemClient>().simParticles;
		}
		enabled = entity.Api.World.Config.GetBool("temporalStability", defaultValue: true);
		if (!entity.WatchedAttributes.HasAttribute("temporalStability"))
		{
			OwnStability = 1.0;
		}
	}

	public override void OnEntityLoaded()
	{
		capi = entity.Api as ICoreClientAPI;
		if (capi != null && (entity as EntityPlayer)?.PlayerUID == capi.Settings.String["playeruid"])
		{
			capi.Event.RegisterEventBusListener(onChatKeyDownPre, 1.0, "chatkeydownpre");
			capi.Event.RegisterEventBusListener(onChatKeyDownPost, 1.0, "chatkeydownpost");
		}
	}

	private void onChatKeyDownPost(string eventName, ref EnumHandling handling, IAttribute data)
	{
		TreeAttribute treeAttribute = data as TreeAttribute;
		string value = (treeAttribute["text"] as StringAttribute).value;
		if (isCommand && value.Length > 0 && value[0] != '.' && value[0] != '/')
		{
			float str = (capi.Render.ShaderUniforms.GlitchStrength - 0.5f) * 2f;
			(treeAttribute["text"] as StringAttribute).value = destabilizeText(value, str);
		}
	}

	private void onChatKeyDownPre(string eventName, ref EnumHandling handling, IAttribute data)
	{
		TreeAttribute treeAttribute = data as TreeAttribute;
		int value = (treeAttribute["key"] as IntAttribute).value;
		string value2 = (treeAttribute["text"] as StringAttribute).value;
		isCommand = value2.Length > 0 && (value2[0] == '.' || value2[0] == '/');
		if (value != 53 && capi.Render.ShaderUniforms.GlitchStrength > 0.5f && (value2.Length == 0 || !isCommand))
		{
			float str = (capi.Render.ShaderUniforms.GlitchStrength - 0.5f) * 2f;
			(treeAttribute["text"] as StringAttribute).value = destabilizeText(value2, str);
		}
	}

	private string destabilizeText(string text, float str)
	{
		char[] array = new char[23]
		{
			'\u0315', '\u031b', '\u0340', '\u0341', '\u0358', '\u0321', '\u0322', '\u0327', '\u0328', '\u0334',
			'\u0335', '\u0336', '\u034f', '\u035c', '\u035d', '\u035e', '\u035f', '\u0360', '\u0362', '\u0338',
			'\u0337', '\u0361', '\u0489'
		};
		string text2 = "";
		for (int i = 0; i < text.Length; i++)
		{
			text2 += text[i];
			if (i < text.Length - 1 && array.Contains(text[i + 1]))
			{
				text2 += text[i + 1];
				i++;
			}
			else if (!array.Contains(text[i]) && capi.World.Rand.NextDouble() < (double)str)
			{
				text2 += array[capi.World.Rand.Next(array.Length)];
			}
		}
		return text2;
	}

	private void initSoundsAndEffects()
	{
		capi = entity.Api as ICoreClientAPI;
		isSelf = capi.World.Player.Entity.EntityId == entity.EntityId;
		if (isSelf)
		{
			capi.Event.RegisterAsyncParticleSpawner(asyncParticleSpawn);
			fogNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 1.0, 0.9, 123L);
			rustParticles = new SimpleParticleProperties
			{
				Color = ColorUtil.ToRgba(150, 50, 25, 15),
				ParticleModel = EnumParticleModel.Quad,
				MinSize = 0.1f,
				MaxSize = 0.5f,
				GravityEffect = 0f,
				LifeLength = 2f,
				WithTerrainCollision = false,
				ShouldDieInLiquid = false,
				RandomVelocityChange = true,
				MinVelocity = new Vec3f(-1f, -1f, -1f),
				AddVelocity = new Vec3f(2f, 2f, 2f),
				MinQuantity = 1f,
				AddQuantity = 0f
			};
			rustParticles.AddVelocity = new Vec3f(0f, 30f, 0f);
			rustParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -8f);
			float num = 0.25f;
			capi.Ambient.CurrentModifiers["brownrainandfog"] = (rainfogAmbient = new AmbientModifier
			{
				AmbientColor = new WeightedFloatArray(new float[4]
				{
					22f / 85f,
					23f / 102f,
					0.21960784f,
					1f
				}, 0f),
				FogColor = new WeightedFloatArray(new float[4]
				{
					num * 132f / 255f,
					num * 115f / 255f,
					num * 112f / 255f,
					1f
				}, 0f),
				FogDensity = new WeightedFloat(0.05f, 0f)
			}.EnsurePopulated());
			tempStabSoundDrain = capi.World.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/tempstab-drain.ogg"),
				ShouldLoop = true,
				RelativePosition = true,
				DisposeOnFinish = false,
				SoundType = EnumSoundType.SoundGlitchunaffected,
				Volume = 0f
			});
			tempStabSoundLow = capi.World.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/tempstab-low.ogg"),
				ShouldLoop = true,
				RelativePosition = true,
				DisposeOnFinish = false,
				SoundType = EnumSoundType.SoundGlitchunaffected,
				Volume = 0f
			});
			tempStabSoundVeryLow = capi.World.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/tempstab-verylow.ogg"),
				ShouldLoop = true,
				RelativePosition = true,
				DisposeOnFinish = false,
				SoundType = EnumSoundType.SoundGlitchunaffected,
				Volume = 0f
			});
		}
	}

	private bool asyncParticleSpawn(float dt, IAsyncParticleManager manager)
	{
		if (isSelf && (fogEffectStrength > 0.05 || glitchEffectStrength > 0.05))
		{
			tmpPos.Set((int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z);
			float num = (float)capi.World.BlockAccessor.GetLightLevel(tmpPos, EnumLightLevelType.OnlySunLight) / 22f;
			float num2 = Math.Min(1f, (float)glitchEffectStrength);
			double num3 = fogEffectStrength * Math.Abs(fogNoise.Noise(0.0, (float)capi.InWorldEllapsedMilliseconds / 1000f)) / 60.0;
			rainfogAmbient.FogDensity.Value = 0.05f + (float)num3;
			rainfogAmbient.AmbientColor.Weight = num2;
			rainfogAmbient.FogColor.Weight = num2;
			rainfogAmbient.FogDensity.Weight = (float)Math.Pow(num2, 2.0);
			rainfogAmbient.FogColor.Value[0] = num * 116f / 255f;
			rainfogAmbient.FogColor.Value[1] = num * 77f / 255f;
			rainfogAmbient.FogColor.Value[2] = num * 49f / 255f;
			rainfogAmbient.AmbientColor.Value[0] = 0.22745098f;
			rainfogAmbient.AmbientColor.Value[1] = 0.1509804f;
			rainfogAmbient.AmbientColor.Value[2] = 0.09607843f;
			rustParticles.Color = ColorUtil.ToRgba((int)(num2 * 150f), 50, 25, 15);
			rustParticles.MaxSize = 0.25f;
			rustParticles.RandomVelocityChange = false;
			rustParticles.MinVelocity.Set(0f, 1f, 0f);
			rustParticles.AddVelocity.Set(0f, 5f, 0f);
			rustParticles.LifeLength = 0.75f;
			Vec3d vec3d = new Vec3d();
			EntityPos pos = capi.World.Player.Entity.Pos;
			float num4 = 120f * num2;
			while (num4-- > 0f)
			{
				float num5 = (float)capi.World.Rand.NextDouble() * 24f - 12f;
				float num6 = (float)capi.World.Rand.NextDouble() * 24f - 12f;
				float num7 = (float)capi.World.Rand.NextDouble() * 24f - 12f;
				vec3d.Set(pos.X + (double)num5, pos.Y + (double)num6, pos.Z + (double)num7);
				BlockPos pos2 = new BlockPos((int)vec3d.X, (int)vec3d.Y, (int)vec3d.Z);
				if (capi.World.BlockAccessor.IsValidPos(pos2))
				{
					rustParticles.MinPos = vec3d;
					capi.World.SpawnParticles(rustParticles);
				}
			}
		}
		return true;
	}

	internal void AddStability(double amount)
	{
		OwnStability += amount;
	}

	public override string PropertyName()
	{
		return "temporalstabilityaffected";
	}

	public override void OnGameTick(float deltaTime)
	{
		if (!enabled)
		{
			return;
		}
		if (requireInitSounds)
		{
			initSoundsAndEffects();
			requireInitSounds = false;
		}
		if (entity.World.Side == EnumAppSide.Client)
		{
			if (!(entity.World.Api as ICoreClientAPI).PlayerReadyFired)
			{
				return;
			}
		}
		else if (entity.World.PlayerByUid(((EntityPlayer)entity).PlayerUID) is IServerPlayer { ConnectionState: not EnumClientState.Playing })
		{
			return;
		}
		deltaTime = GameMath.Min(0.5f, deltaTime);
		float num = deltaTime / 3f;
		double num2 = stabilityOffset + (double)tempStabilitySystem.GetTemporalStability(entity.SidedPos.X, entity.SidedPos.Y, entity.SidedPos.Z);
		entity.Attributes.SetDouble("tempStabChangeVelocity", TempStabChangeVelocity);
		double num3 = ((TempStabChangeVelocity > 0.0) ? (TempStabChangeVelocity / 200.0) : (TempStabChangeVelocity / 800.0));
		OwnStability = GameMath.Clamp(OwnStability + num3, 0.0, 1.0);
		double ownStability = OwnStability;
		TempStabChangeVelocity = (hereTempStabChangeVelocity - TempStabChangeVelocity) * (double)deltaTime;
		float glitchEffectExtraStrength = tempStabilitySystem.GetGlitchEffectExtraStrength();
		double num4 = Math.Max(0.0, Math.Max(0.0, (0.20000000298023224 - ownStability) * 1.0 / 0.20000000298023224) + (double)glitchEffectExtraStrength);
		glitchEffectStrength += (num4 - glitchEffectStrength) * (double)num;
		glitchEffectStrength = GameMath.Clamp(glitchEffectStrength, 0.0, 1.100000023841858);
		double num5 = Math.Max(0.0, Math.Max(0.0, (0.30000001192092896 - ownStability) * 1.0 / 0.30000001192092896) + (double)glitchEffectExtraStrength);
		fogEffectStrength += (num5 - fogEffectStrength) * (double)num;
		fogEffectStrength = GameMath.Clamp(fogEffectStrength, 0.0, 0.8999999761581421);
		double num6 = Math.Max(0.0, Math.Max(0.0, (0.30000001192092896 - ownStability) * 1.0 / 0.30000001192092896) + (double)glitchEffectExtraStrength);
		rustPrecipColorStrength += (num6 - rustPrecipColorStrength) * (double)num;
		rustPrecipColorStrength = GameMath.Clamp(rustPrecipColorStrength, 0.0, 1.0);
		if (precipParticleSys != null)
		{
			precipParticleSys.rainParticleColor = ColorUtil.ColorOverlay(WeatherSimulationParticles.waterColor, WeatherSimulationParticles.lowStabColor, (float)rustPrecipColorStrength);
		}
		hereTempStabChangeVelocity = num2 - 1.0;
		oneSecAccum += deltaTime;
		if (oneSecAccum > 1f)
		{
			oneSecAccum = 0f;
			updateSoundsAndEffects(num2, Math.Max(0.0, ownStability - (double)(1.5f * glitchEffectExtraStrength)));
		}
		threeSecAccum += deltaTime;
		if (threeSecAccum > 4f)
		{
			threeSecAccum = 0f;
			if (entity.World.Side == EnumAppSide.Server && ownStability < 0.13)
			{
				entity.ReceiveDamage(new DamageSource
				{
					DamageTier = 0,
					Source = EnumDamageSource.Machine,
					Type = EnumDamageType.Poison
				}, (float)(0.15 - ownStability));
			}
		}
		if (isSelf)
		{
			capi.Render.ShaderUniforms.GlitchStrength = 0f;
		}
		if (!isSelf || (!(fogEffectStrength > 0.05) && !(glitchEffectStrength > 0.05)))
		{
			return;
		}
		float num7 = capi.Settings.Float["instabilityWavingStrength"];
		capi.Render.ShaderUniforms.GlitchStrength = (float)glitchEffectStrength;
		capi.Render.ShaderUniforms.GlitchWaviness = (float)glitchEffectStrength * num7;
		capi.Render.ShaderUniforms.GlobalWorldWarp = (float)((capi.World.Rand.NextDouble() < 0.015) ? (Math.Max(0.0, glitchEffectStrength - 0.05000000074505806) * capi.World.Rand.NextDouble() * capi.World.Rand.NextDouble()) : 0.0) * num7;
		float num8 = 9f;
		if (capi.Settings.Float.Exists("tempStormJitterStrength"))
		{
			num8 = capi.Settings.Float["tempStormJitterStrength"];
		}
		if (capi.World.Rand.NextDouble() < 0.015 && jitterOffset == 0f)
		{
			jitterOffset = num8 * (float)capi.World.Rand.NextDouble() + 3f;
			jitterOffsetedDuration = 0.25f + (float)capi.World.Rand.NextDouble() / 2f;
			capi.Render.ShaderUniforms.WindWaveCounter += jitterOffset;
			capi.Render.ShaderUniforms.WaterWaveCounter += jitterOffset;
		}
		if (jitterOffset > 0f)
		{
			capi.Render.ShaderUniforms.WindWaveCounter += (float)capi.World.Rand.NextDouble() / 2f - 0.25f;
			jitterOffsetedDuration -= deltaTime;
			if (jitterOffsetedDuration <= 0f)
			{
				jitterOffset = 0f;
			}
		}
		if (capi.World.Rand.NextDouble() < 0.002 && capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
		{
			capi.Input.MouseYaw += (float)capi.World.Rand.NextDouble() * 0.125f - 0.0625f;
			capi.Input.MousePitch += (float)capi.World.Rand.NextDouble() * 0.125f - 0.0625f;
		}
		double num9 = fogEffectStrength * Math.Abs(fogNoise.Noise(0.0, (float)capi.InWorldEllapsedMilliseconds / 1000f)) / 60.0;
		rainfogAmbient.FogDensity.Value = 0.05f + (float)num9;
	}

	private void updateSoundsAndEffects(double hereStability, double ownStability)
	{
		if (!isSelf || tempStabSoundDrain == null)
		{
			return;
		}
		float num = 3f;
		if (hereStability < 0.949999988079071 && ownStability < 0.6499999761581421)
		{
			if (!tempStabSoundDrain.IsPlaying)
			{
				tempStabSoundDrain.Start();
			}
			tempStabSoundDrain.FadeTo(Math.Min(1.0, 3.0 * (1.0 - hereStability)), 0.95f * num, delegate
			{
			});
		}
		else
		{
			tempStabSoundDrain.FadeTo(0.0, 0.95f * num, delegate
			{
				tempStabSoundDrain.Stop();
			});
		}
		SurfaceMusicTrack.ShouldPlayMusic = ownStability > 0.44999998807907104;
		CaveMusicTrack.ShouldPlayCaveMusic = ownStability > 0.20000000298023224;
		if (ownStability < 0.4000000059604645)
		{
			if (!tempStabSoundLow.IsPlaying)
			{
				tempStabSoundLow.Start();
			}
			float val = (0.4f - (float)ownStability) * 1f / 0.4f;
			tempStabSoundLow.FadeTo(Math.Min(1f, val), 0.95f * num, delegate
			{
			});
		}
		else
		{
			tempStabSoundLow.FadeTo(0.0, 0.95f * num, delegate
			{
				tempStabSoundLow.Stop();
			});
		}
		if (ownStability < 0.25)
		{
			if (!tempStabSoundVeryLow.IsPlaying)
			{
				tempStabSoundVeryLow.Start();
			}
			float val2 = (0.25f - (float)ownStability) * 1f / 0.25f;
			tempStabSoundVeryLow.FadeTo(Math.Min(1f, val2) / 5f, 0.95f * num, delegate
			{
			});
		}
		else
		{
			tempStabSoundVeryLow.FadeTo(0.0, 0.95f * num, delegate
			{
				tempStabSoundVeryLow.Stop();
			});
		}
	}
}
