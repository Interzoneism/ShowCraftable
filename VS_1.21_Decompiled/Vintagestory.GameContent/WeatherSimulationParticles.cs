using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WeatherSimulationParticles
{
	private WeatherSystemClient ws;

	private ICoreClientAPI capi;

	private Random rand;

	private static int[,] lowResRainHeightMap;

	private static BlockPos centerPos;

	public static int waterColor;

	public static int lowStabColor;

	public int rainParticleColor;

	public static SimpleParticleProperties splashParticles;

	public static WeatherParticleProps stormDustParticles;

	public static SimpleParticleProperties stormWaterParticles;

	public static WeatherParticleProps rainParticle;

	public static WeatherParticleProps hailParticle;

	private static WeatherParticleProps snowParticle;

	private Block lblock;

	private Vec3f parentVeloSnow = new Vec3f();

	private BlockPos tmpPos = new BlockPos();

	private Vec3d particlePos = new Vec3d();

	private AmbientModifier desertStormAmbient;

	private int spawnCount;

	private float sandFinds;

	private int dustParticlesPerTick = 30;

	private float[] sandCountByBlock;

	private float[] targetFogColor = new float[3];

	private float targetFogDensity;

	private Dictionary<int, int> indicesBySandBlockId = new Dictionary<int, int>();

	private float accum;

	protected bool suppressDesertStorm;

	private Vec3f windSpeed;

	private float windSpeedIntensity;

	static WeatherSimulationParticles()
	{
		lowResRainHeightMap = new int[16, 16];
		centerPos = new BlockPos();
		waterColor = ColorUtil.ToRgba(230, 128, 178, 255);
		lowStabColor = ColorUtil.ToRgba(230, 207, 53, 10);
		splashParticles = new SimpleParticleProperties
		{
			MinPos = new Vec3d(),
			AddPos = new Vec3d(1.0, 0.25, 0.0),
			MinQuantity = 0f,
			AddQuantity = 3f,
			Color = ColorUtil.ToRgba(230, 128, 178, 200),
			GravityEffect = 1f,
			WithTerrainCollision = true,
			ParticleModel = EnumParticleModel.Quad,
			LifeLength = 0.5f,
			MinVelocity = new Vec3f(-1f, 2f, -1f),
			AddVelocity = new Vec3f(2f, 0f, 2f),
			MinSize = 0.07f,
			MaxSize = 0.2f,
			VertexFlags = 32
		};
		stormDustParticles = new WeatherParticleProps
		{
			MinPos = new Vec3d(),
			AddPos = new Vec3d(),
			MinQuantity = 0f,
			AddQuantity = 3f,
			Color = ColorUtil.ToRgba(100, 200, 200, 200),
			GravityEffect = 1f,
			WithTerrainCollision = true,
			ParticleModel = EnumParticleModel.Quad,
			LifeLength = 0.5f,
			MinVelocity = new Vec3f(-1f, 2f, -1f),
			AddVelocity = new Vec3f(2f, 0f, 2f),
			MinSize = 0.07f,
			MaxSize = 0.1f
		};
		stormWaterParticles = new SimpleParticleProperties
		{
			MinPos = new Vec3d(),
			AddPos = new Vec3d(),
			MinQuantity = 0f,
			AddQuantity = 3f,
			Color = ColorUtil.ToRgba(230, 128, 178, 200),
			GravityEffect = 1f,
			WithTerrainCollision = true,
			ParticleModel = EnumParticleModel.Quad,
			LifeLength = 0.5f,
			MinVelocity = new Vec3f(-1f, 2f, -1f),
			AddVelocity = new Vec3f(2f, 0f, 2f),
			MinSize = 0.07f,
			MaxSize = 0.2f,
			VertexFlags = 0
		};
		rainParticle = new WeatherParticleProps
		{
			MinPos = new Vec3d(),
			AddPos = new Vec3d(60.0, 9.0, 60.0),
			MinQuantity = 300f,
			AddQuantity = 25f,
			Color = waterColor,
			GravityEffect = 1f,
			WithTerrainCollision = false,
			DieOnRainHeightmap = true,
			ShouldDieInLiquid = true,
			ParticleModel = EnumParticleModel.Quad,
			LifeLength = 1.5f,
			MinVelocity = new Vec3f(-0.25f, -0.25f, -0.25f),
			AddVelocity = new Vec3f(0.5f, 0f, 0.5f),
			MinSize = 0.15f,
			MaxSize = 0.22f,
			VertexFlags = -2147483616
		};
		hailParticle = new HailParticleProps
		{
			MinPos = new Vec3d(),
			AddPos = new Vec3d(60.0, 0.0, 60.0),
			MinQuantity = 50f,
			AddQuantity = 25f,
			Color = ColorUtil.ToRgba(255, 255, 255, 255),
			GravityEffect = 1f,
			WithTerrainCollision = true,
			DieOnRainHeightmap = false,
			ShouldDieInLiquid = false,
			ShouldSwimOnLiquid = true,
			ParticleModel = EnumParticleModel.Cube,
			LifeLength = 3f,
			MinVelocity = new Vec3f(-1f, -2f, -1f),
			AddVelocity = new Vec3f(2f, 0f, 2f),
			MinSize = 0.1f,
			MaxSize = 0.14f,
			WindAffectednes = 0f,
			ParentVelocity = null,
			Bounciness = 0.3f
		};
		snowParticle = new WeatherParticleProps
		{
			MinPos = new Vec3d(),
			AddPos = new Vec3d(60.0, 0.0, 60.0),
			MinQuantity = 80f,
			AddQuantity = 15f,
			Color = ColorUtil.ToRgba(200, 255, 255, 255),
			GravityEffect = 0.003f,
			WithTerrainCollision = true,
			DieOnRainHeightmap = false,
			ShouldDieInLiquid = false,
			ParticleModel = EnumParticleModel.Quad,
			LifeLength = 5f,
			MinVelocity = new Vec3f(-3.5f, -1.25f, -0.5f),
			AddVelocity = new Vec3f(1f, 0.05f, 1f),
			MinSize = 0.1f,
			MaxSize = 0.2f
		};
		stormDustParticles.lowResRainHeightMap = lowResRainHeightMap;
		hailParticle.lowResRainHeightMap = lowResRainHeightMap;
		snowParticle.lowResRainHeightMap = lowResRainHeightMap;
		rainParticle.lowResRainHeightMap = lowResRainHeightMap;
		stormDustParticles.centerPos = centerPos;
		hailParticle.centerPos = centerPos;
		snowParticle.centerPos = centerPos;
		rainParticle.centerPos = centerPos;
	}

	public WeatherSimulationParticles(ICoreClientAPI capi, WeatherSystemClient ws)
	{
		this.capi = capi;
		this.ws = ws;
		rand = new Random(capi.World.Seed + 223123123);
		rainParticleColor = waterColor;
		desertStormAmbient = new AmbientModifier().EnsurePopulated();
		desertStormAmbient.FogDensity = new WeightedFloat();
		desertStormAmbient.FogColor = new WeightedFloatArray
		{
			Value = new float[3]
		};
		desertStormAmbient.FogMin = new WeightedFloat();
		capi.Ambient.CurrentModifiers["desertstorm"] = desertStormAmbient;
	}

	public void Initialize()
	{
		lblock = capi.World.GetBlock(new AssetLocation("water-still-7"));
		if (lblock != null)
		{
			capi.Event.RegisterAsyncParticleSpawner(asyncParticleSpawn);
			capi.Event.RegisterRenderer(new DummyRenderer
			{
				action = desertStormSim
			}, EnumRenderStage.Before);
			int num = 0;
			foreach (Block block in capi.World.Blocks)
			{
				if (block.BlockMaterial == EnumBlockMaterial.Sand)
				{
					indicesBySandBlockId[block.Id] = num++;
				}
			}
			sandCountByBlock = new float[indicesBySandBlockId.Count];
		}
		capi.Event.LevelFinalize += Event_LevelFinalize;
	}

	private void Event_LevelFinalize()
	{
		suppressDesertStorm = capi.World.Config.GetAsBool("suppressDesertStorm");
	}

	private void desertStormSim(float dt)
	{
		if (suppressDesertStorm)
		{
			return;
		}
		accum += dt;
		if (accum > 2f)
		{
			int num = spawnCount;
			float num2 = sandFinds;
			float[] array = sandCountByBlock;
			if (num > 10 && num2 > 0f)
			{
				sandCountByBlock = new float[indicesBySandBlockId.Count];
				spawnCount = 0;
				sandFinds = 0f;
				_ = ws.BlendedWeatherData;
				BlockPos asBlockPos = capi.World.Player.Entity.Pos.AsBlockPos;
				ClimateCondition climateAt = capi.World.BlockAccessor.GetClimateAt(asBlockPos);
				float num3 = (float)capi.World.BlockAccessor.GetLightLevel(asBlockPos, EnumLightLevelType.OnlySunLight) / 22f;
				float num4 = 2f * Math.Max(0f, windSpeedIntensity - 0.65f) * (1f - climateAt.WorldgenRainfall) * (1f - climateAt.Rainfall);
				BlockPos asBlockPos2 = capi.World.Player.Entity.Pos.AsBlockPos;
				targetFogColor[0] = (targetFogColor[1] = (targetFogColor[2] = 0f));
				foreach (KeyValuePair<int, int> item in indicesBySandBlockId)
				{
					float num5 = array[item.Value] / num2;
					double[] array2 = ColorUtil.ToRGBADoubles(capi.World.GetBlock(item.Key).GetColor(capi, asBlockPos2));
					targetFogColor[0] += (float)array2[2] * num5;
					targetFogColor[1] += (float)array2[1] * num5;
					targetFogColor[2] += (float)array2[0] * num5;
				}
				float num6 = (float)((double)num2 / 30.0 / (double)num) * num4 * num3;
				targetFogDensity = num6;
			}
			accum = 0f;
		}
		float num7 = dt / 3f;
		targetFogDensity = Math.Max(0f, targetFogDensity - 2f * WeatherSystemClient.CurrentEnvironmentWetness4h);
		desertStormAmbient.FogColor.Value[0] += (targetFogColor[0] - desertStormAmbient.FogColor.Value[0]) * num7;
		desertStormAmbient.FogColor.Value[1] += (targetFogColor[1] - desertStormAmbient.FogColor.Value[1]) * num7;
		desertStormAmbient.FogColor.Value[2] += (targetFogColor[2] - desertStormAmbient.FogColor.Value[2]) * num7;
		desertStormAmbient.FogDensity.Value += ((float)Math.Pow(targetFogDensity, 1.2000000476837158) - desertStormAmbient.FogDensity.Value) * num7;
		desertStormAmbient.FogDensity.Weight += (targetFogDensity - desertStormAmbient.FogDensity.Weight) * num7;
		desertStormAmbient.FogColor.Weight += (Math.Min(1f, 2f * targetFogDensity) - desertStormAmbient.FogColor.Weight) * num7;
	}

	private bool asyncParticleSpawn(float dt, IAsyncParticleManager manager)
	{
		WeatherDataSnapshot blendedWeatherData = ws.BlendedWeatherData;
		ClimateCondition clientClimateCond = ws.clientClimateCond;
		if (clientClimateCond == null || !ws.playerChunkLoaded)
		{
			return true;
		}
		EntityPos pos = capi.World.Player.Entity.Pos;
		float rainfall = clientClimateCond.Rainfall;
		float plevel = rainfall * (float)capi.Settings.Int["particleLevel"] / 100f;
		float dryness = GameMath.Clamp(1f - rainfall, 0f, 1f);
		tmpPos.Set((int)pos.X, (int)pos.Y, (int)pos.Z);
		rainfall = Math.Max(0f, rainfall - (float)Math.Max(0.0, (pos.Y - (double)capi.World.SeaLevel - 5000.0) / 10000.0));
		EnumPrecipitationType enumPrecipitationType = blendedWeatherData.BlendedPrecType;
		if (enumPrecipitationType == EnumPrecipitationType.Auto)
		{
			enumPrecipitationType = ((clientClimateCond.Temperature < blendedWeatherData.snowThresholdTemp) ? EnumPrecipitationType.Snow : EnumPrecipitationType.Rain);
		}
		int rainMapHeightAt = capi.World.BlockAccessor.GetRainMapHeightAt((int)particlePos.X, (int)particlePos.Z);
		particlePos.Set(capi.World.Player.Entity.Pos.X, rainMapHeightAt, capi.World.Player.Entity.Pos.Z);
		int color = capi.World.ApplyColorMapOnRgba(lblock.ClimateColorMapResolved, lblock.SeasonColorMapResolved, -1, (int)particlePos.X, (int)particlePos.Y, (int)particlePos.Z, flipRb: false);
		byte[] array = ColorUtil.ToBGRABytes(color);
		color = ColorUtil.ToRgba(94, array[0], array[1], array[2]);
		centerPos.Set((int)particlePos.X, 0, (int)particlePos.Z);
		for (int i = 0; i < 16; i++)
		{
			int num = (i - 8) * 4;
			for (int j = 0; j < 16; j++)
			{
				int num2 = (j - 8) * 4;
				lowResRainHeightMap[i, j] = capi.World.BlockAccessor.GetRainMapHeightAt(centerPos.X + num, centerPos.Z + num2);
			}
		}
		windSpeed = capi.World.BlockAccessor.GetWindSpeedAt(pos.XYZ).ToVec3f();
		windSpeedIntensity = windSpeed.Length();
		parentVeloSnow.X = (0f - Math.Max(0f, windSpeed.X / 2f - 0.15f)) * 2f;
		parentVeloSnow.Y = 0f;
		parentVeloSnow.Z = (0f - Math.Max(0f, windSpeed.Z / 2f - 0.15f)) * 2f;
		if (windSpeedIntensity > 0.5f)
		{
			SpawnDustParticles(manager, blendedWeatherData, pos, dryness, color);
		}
		particlePos.Y = capi.World.Player.Entity.Pos.Y;
		if ((double)rainfall <= 0.02)
		{
			return true;
		}
		switch (enumPrecipitationType)
		{
		case EnumPrecipitationType.Hail:
			SpawnHailParticles(manager, blendedWeatherData, clientClimateCond, pos, plevel);
			return true;
		case EnumPrecipitationType.Rain:
			SpawnRainParticles(manager, blendedWeatherData, clientClimateCond, pos, plevel, color);
			break;
		}
		if (enumPrecipitationType == EnumPrecipitationType.Snow)
		{
			SpawnSnowParticles(manager, blendedWeatherData, clientClimateCond, pos, plevel);
		}
		return true;
	}

	private void SpawnDustParticles(IAsyncParticleManager manager, WeatherDataSnapshot weatherData, EntityPos plrPos, float dryness, int onwaterSplashParticleColor)
	{
		float num = (float)(plrPos.Motion.X * 40.0) - 50f * windSpeed.X;
		float num2 = (float)(plrPos.Motion.Y * 40.0);
		float num3 = (float)(plrPos.Motion.Z * 40.0) - 50f * windSpeed.Z;
		double num4 = 40.0;
		float num5 = 1f - targetFogDensity;
		num4 *= (double)num5;
		float num6 = windSpeed.Length();
		stormDustParticles.MinPos.Set(particlePos.X - num4 + (double)num, particlePos.Y + 20.0 + (double)(5f * num6) + (double)num2, particlePos.Z - num4 + (double)num3);
		stormDustParticles.AddPos.Set(2.0 * num4, -30.0, 2.0 * num4);
		stormDustParticles.GravityEffect = 0.1f;
		stormDustParticles.ParticleModel = EnumParticleModel.Quad;
		stormDustParticles.LifeLength = 1f;
		stormDustParticles.DieOnRainHeightmap = true;
		stormDustParticles.WindAffectednes = 8f;
		stormDustParticles.MinQuantity = 0f;
		stormDustParticles.AddQuantity = 8f * (num6 - 0.5f) * dryness;
		stormDustParticles.MinSize = 0.2f;
		stormDustParticles.MaxSize = 0.7f;
		stormDustParticles.MinVelocity.Set(-0.025f + 12f * windSpeed.X, 0f, -0.025f + 12f * windSpeed.Z).Mul(3f);
		stormDustParticles.AddVelocity.Set(0.05f + 6f * windSpeed.X, -0.25f, 0.05f + 6f * windSpeed.Z).Mul(3f);
		float num7 = Math.Max(1f, num6 * 3f);
		int num8 = (int)((float)dustParticlesPerTick * num7);
		try
		{
			for (int i = 0; i < num8; i++)
			{
				double num9 = particlePos.X + (double)num + rand.NextDouble() * rand.NextDouble() * 60.0 * (double)(1 - 2 * rand.Next(2));
				double num10 = particlePos.Z + (double)num3 + rand.NextDouble() * rand.NextDouble() * 60.0 * (double)(1 - 2 * rand.Next(2));
				int rainMapHeightAt = capi.World.BlockAccessor.GetRainMapHeightAt((int)num9, (int)num10);
				Block blockRaw = capi.World.BlockAccessor.GetBlockRaw((int)num9, rainMapHeightAt, (int)num10);
				if (blockRaw.BlockId != 0 && capi.World.BlockAccessor.GetBlockRaw((int)num9, rainMapHeightAt, (int)num10, 2).BlockId == 0 && (blockRaw.BlockMaterial == EnumBlockMaterial.Sand || blockRaw.BlockMaterial == EnumBlockMaterial.Snow || (!(rand.NextDouble() < 0.699999988079071) && blockRaw.RenderPass != EnumChunkRenderPass.TopSoil)))
				{
					if (blockRaw.BlockMaterial == EnumBlockMaterial.Sand)
					{
						sandFinds += 1f / num7;
						sandCountByBlock[indicesBySandBlockId[blockRaw.Id]] += 1f / num7;
					}
					if (!(Math.Abs((double)rainMapHeightAt - particlePos.Y) > 15.0))
					{
						tmpPos.Set((int)num9, rainMapHeightAt, (int)num10);
						stormDustParticles.Color = ColorUtil.ReverseColorBytes(blockRaw.GetColor(capi, tmpPos));
						stormDustParticles.Color |= -16777216;
						manager.Spawn(stormDustParticles);
					}
				}
			}
		}
		catch (Exception)
		{
		}
		spawnCount++;
		if (!(num6 > 0.85f))
		{
			return;
		}
		stormWaterParticles.AddVelocity.Y = 1.5f;
		stormWaterParticles.LifeLength = 0.17f;
		stormWaterParticles.WindAffected = true;
		stormWaterParticles.WindAffectednes = 1f;
		stormWaterParticles.GravityEffect = 0.4f;
		stormWaterParticles.MinVelocity.Set(-0.025f + 4f * windSpeed.X, 1.5f, -0.025f + 4f * windSpeed.Z);
		stormWaterParticles.Color = onwaterSplashParticleColor;
		stormWaterParticles.MinQuantity = 1f;
		stormWaterParticles.AddQuantity = 5f;
		stormWaterParticles.ShouldDieInLiquid = false;
		splashParticles.WindAffected = true;
		splashParticles.WindAffectednes = 1f;
		for (int j = 0; j < 20; j++)
		{
			double num11 = particlePos.X + rand.NextDouble() * rand.NextDouble() * 40.0 * (double)(1 - 2 * rand.Next(2));
			double num12 = particlePos.Z + rand.NextDouble() * rand.NextDouble() * 40.0 * (double)(1 - 2 * rand.Next(2));
			int rainMapHeightAt2 = capi.World.BlockAccessor.GetRainMapHeightAt((int)num11, (int)num12);
			Block blockRaw2 = capi.World.BlockAccessor.GetBlockRaw((int)num11, rainMapHeightAt2, (int)num12, 2);
			if (blockRaw2.IsLiquid())
			{
				stormWaterParticles.MinPos.Set(num11, (float)rainMapHeightAt2 + blockRaw2.TopMiddlePos.Y, num12);
				stormWaterParticles.ParticleModel = EnumParticleModel.Cube;
				stormWaterParticles.MinSize = 0.4f;
				manager.Spawn(stormWaterParticles);
				splashParticles.MinPos.Set(num11, (float)rainMapHeightAt2 + blockRaw2.TopMiddlePos.Y - 0.125f, num12);
				splashParticles.MinVelocity.X = windSpeed.X * 1.5f;
				splashParticles.AddVelocity.Y = 1.5f;
				splashParticles.MinVelocity.Z = windSpeed.Z * 1.5f;
				splashParticles.LifeLength = 0.17f;
				splashParticles.Color = onwaterSplashParticleColor;
				manager.Spawn(splashParticles);
			}
		}
	}

	private void SpawnHailParticles(IAsyncParticleManager manager, WeatherDataSnapshot weatherData, ClimateCondition conds, EntityPos plrPos, float plevel)
	{
		float num = (float)(plrPos.Motion.X * 40.0) - 4f * windSpeed.X;
		float num2 = (float)(plrPos.Motion.Y * 40.0);
		float num3 = (float)(plrPos.Motion.Z * 40.0) - 4f * windSpeed.Z;
		hailParticle.MinPos.Set(particlePos.X + (double)num, particlePos.Y + 15.0 + (double)num2, particlePos.Z + (double)num3);
		hailParticle.MinSize = 0.3f * (0.5f + conds.Rainfall);
		hailParticle.MaxSize = 1f * (0.5f + conds.Rainfall);
		hailParticle.Color = ColorUtil.ToRgba(220, 210, 230, 255);
		hailParticle.MinQuantity = 100f * plevel;
		hailParticle.AddQuantity = 25f * plevel;
		hailParticle.MinVelocity.Set(-0.025f + 7.5f * windSpeed.X, -5f, -0.025f + 7.5f * windSpeed.Z);
		hailParticle.AddVelocity.Set(0.05f + 7.5f * windSpeed.X, 0.05f, 0.05f + 7.5f * windSpeed.Z);
		manager.Spawn(hailParticle);
	}

	private void SpawnRainParticles(IAsyncParticleManager manager, WeatherDataSnapshot weatherData, ClimateCondition conds, EntityPos plrPos, float plevel, int onwaterSplashParticleColor)
	{
		float num = (float)(plrPos.Motion.X * 80.0);
		float num2 = (float)(plrPos.Motion.Y * 80.0);
		float num3 = (float)(plrPos.Motion.Z * 80.0);
		rainParticle.MinPos.Set(particlePos.X - 30.0 + (double)num, particlePos.Y + 15.0 + (double)num2, particlePos.Z - 30.0 + (double)num3);
		rainParticle.WithTerrainCollision = false;
		rainParticle.MinQuantity = 1000f * plevel;
		rainParticle.LifeLength = 1f;
		rainParticle.AddQuantity = 25f * plevel;
		rainParticle.MinSize = 0.15f * (0.5f + conds.Rainfall);
		rainParticle.MaxSize = 0.22f * (0.5f + conds.Rainfall);
		rainParticle.Color = rainParticleColor;
		rainParticle.MinVelocity.Set(-0.025f + 8f * windSpeed.X, -10f, -0.025f + 8f * windSpeed.Z);
		rainParticle.AddVelocity.Set(0.05f + 8f * windSpeed.X, 0.05f, 0.05f + 8f * windSpeed.Z);
		manager.Spawn(rainParticle);
		splashParticles.MinVelocity = new Vec3f(-1f, 3f, -1f);
		splashParticles.AddVelocity = new Vec3f(2f, 0f, 2f);
		splashParticles.LifeLength = 0.1f;
		splashParticles.MinSize = 0.07f * (0.5f + 0.65f * conds.Rainfall);
		splashParticles.MaxSize = 0.2f * (0.5f + 0.65f * conds.Rainfall);
		splashParticles.ShouldSwimOnLiquid = true;
		splashParticles.Color = rainParticleColor;
		float num4 = 100f * plevel;
		for (int i = 0; (float)i < num4; i++)
		{
			double num5 = particlePos.X + rand.NextDouble() * rand.NextDouble() * 60.0 * (double)(1 - 2 * rand.Next(2));
			double num6 = particlePos.Z + rand.NextDouble() * rand.NextDouble() * 60.0 * (double)(1 - 2 * rand.Next(2));
			int rainMapHeightAt = capi.World.BlockAccessor.GetRainMapHeightAt((int)num5, (int)num6);
			Block blockRaw = capi.World.BlockAccessor.GetBlockRaw((int)num5, rainMapHeightAt, (int)num6, 2);
			if (blockRaw.IsLiquid())
			{
				splashParticles.MinPos.Set(num5, (float)rainMapHeightAt + blockRaw.TopMiddlePos.Y - 0.125f, num6);
				splashParticles.AddVelocity.Y = 1.5f;
				splashParticles.LifeLength = 0.17f;
				splashParticles.Color = onwaterSplashParticleColor;
			}
			else
			{
				if (blockRaw.BlockId == 0)
				{
					blockRaw = capi.World.BlockAccessor.GetBlockRaw((int)num5, rainMapHeightAt, (int)num6);
				}
				double num7 = 0.75 + 0.25 * rand.NextDouble();
				int num8 = 230 - rand.Next(100);
				int num9 = (int)((double)((rainParticleColor >> 16) & 0xFF) * num7);
				int num10 = (int)((double)((rainParticleColor >> 8) & 0xFF) * num7);
				int num11 = (int)((double)(rainParticleColor & 0xFF) * num7);
				splashParticles.Color = (num8 << 24) | (num9 << 16) | (num10 << 8) | num11;
				splashParticles.AddVelocity.Y = 0f;
				splashParticles.LifeLength = 0.1f;
				splashParticles.MinPos.Set(num5, (double)((float)rainMapHeightAt + blockRaw.TopMiddlePos.Y) + 0.05, num6);
			}
			manager.Spawn(splashParticles);
		}
	}

	private void SpawnSnowParticles(IAsyncParticleManager manager, WeatherDataSnapshot weatherData, ClimateCondition conds, EntityPos plrPos, float plevel)
	{
		snowParticle.WindAffected = true;
		snowParticle.WindAffectednes = 1f;
		float num = 2.5f * GameMath.Clamp(ws.clientClimateCond.Temperature + 1f, 0f, 4f) / 4f;
		float num2 = (float)plrPos.Motion.X * 60f;
		float num3 = (float)plrPos.Motion.Z * 60f;
		float num4 = (float)Math.Pow(num2 * num2 + num3 * num3, 0.25);
		float num5 = num2 - Math.Max(0f, (30f - 9f * num) * windSpeed.X - 5f * num4);
		float num6 = (float)(plrPos.Motion.Y * 60.0);
		float num7 = num3 - Math.Max(0f, (30f - 9f * num) * windSpeed.Z - 5f * num4);
		snowParticle.MinVelocity.Set(-0.5f + 10f * windSpeed.X, -1f, -0.5f + 10f * windSpeed.Z);
		snowParticle.AddVelocity.Set(1f + 10f * windSpeed.X, 0.05f, 1f + 10f * windSpeed.Z);
		snowParticle.Color = ColorUtil.ToRgba(255, 255, 255, 255);
		snowParticle.MinQuantity = 100f * plevel * (1f + num / 3f);
		snowParticle.AddQuantity = 25f * plevel * (1f + num / 3f);
		snowParticle.ParentVelocity = parentVeloSnow;
		snowParticle.ShouldDieInLiquid = true;
		snowParticle.LifeLength = Math.Max(1f, 4f - num - windSpeedIntensity);
		snowParticle.Color = ColorUtil.ColorOverlay(ColorUtil.ToRgba(255, 255, 255, 255), rainParticle.Color, num / 4f);
		snowParticle.GravityEffect = 0.005f * (1f + 20f * num);
		snowParticle.MinSize = 0.1f * conds.Rainfall;
		snowParticle.MaxSize = 0.3f * conds.Rainfall / (1f + num);
		float num8 = 20f;
		float num9 = 23f + windSpeedIntensity * 5f;
		num6 -= Math.Min(10f, num4) + windSpeedIntensity * 5f;
		snowParticle.MinVelocity.Y = -2f;
		snowParticle.MinPos.Set(particlePos.X - (double)num8 + (double)num5, particlePos.Y + (double)num9 + (double)num6, particlePos.Z - (double)num8 + (double)num7);
		snowParticle.AddPos.Set(2f * num8 + num5, -0.66f * num9 + num6, 2f * num8 + num7);
		manager.Spawn(snowParticle);
	}
}
