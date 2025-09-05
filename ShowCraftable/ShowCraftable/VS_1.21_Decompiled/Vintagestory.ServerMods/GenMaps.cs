using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Datastructures;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class GenMaps : ModSystem
{
	private ICoreServerAPI sapi;

	private ICoreClientAPI capi;

	public MapLayerBase upheavelGen;

	public MapLayerBase oceanGen;

	public MapLayerBase climateGen;

	public MapLayerBase flowerGen;

	public MapLayerBase bushGen;

	public MapLayerBase forestGen;

	public MapLayerBase beachGen;

	public MapLayerBase geologicprovinceGen;

	public MapLayerBase landformsGen;

	public int noiseSizeUpheavel;

	public int noiseSizeOcean;

	public int noiseSizeClimate;

	public int noiseSizeForest;

	public int noiseSizeBeach;

	public int noiseSizeShrubs;

	public int noiseSizeGeoProv;

	public int noiseSizeLandform;

	private LatitudeData latdata = new LatitudeData();

	private List<ForceLandform> forceLandforms = new List<ForceLandform>();

	private List<ForceClimate> forceClimate = new List<ForceClimate>();

	private NormalizedSimplexNoise noisegenX;

	private NormalizedSimplexNoise noisegenZ;

	public static float upheavelCommonness;

	public List<XZ> requireLandAt = new List<XZ>();

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("latitudedata").RegisterMessageType(typeof(LatitudeData));
	}

	public void ForceClimateAt(ForceClimate climate)
	{
		forceClimate.Add(climate);
	}

	public void ForceLandformAt(ForceLandform landform)
	{
		forceLandforms.Add(landform);
		ForceLandAt(landform);
		LandformVariant[] landFormsByIndex = NoiseLandforms.landforms.LandFormsByIndex;
		for (int i = 0; i < landFormsByIndex.Length; i++)
		{
			if (landFormsByIndex[i].Code.Path == landform.LandformCode)
			{
				landform.landFormIndex = i;
				return;
			}
		}
		throw new ArgumentException("No landform with code " + landform.LandformCode + " found.");
	}

	public void ForceLandAt(ForceLandform fl)
	{
		if (GameVersion.IsLowerVersionThan(sapi.WorldManager.SaveGame.CreatedGameVersion, "1.20.0-pre.14"))
		{
			int regionSize = sapi.WorldManager.RegionSize;
			int radius = fl.Radius;
			int num = (fl.CenterPos.X - radius) * noiseSizeOcean / regionSize;
			int num2 = (fl.CenterPos.Z - radius) * noiseSizeOcean / regionSize;
			int num3 = (fl.CenterPos.X + radius) * noiseSizeOcean / regionSize;
			int num4 = (fl.CenterPos.Z + radius) * noiseSizeOcean / regionSize;
			for (int i = num; i <= num3; i++)
			{
				for (int j = num2; j < num4; j++)
				{
					requireLandAt.Add(new XZ(i, j));
				}
			}
		}
		else
		{
			int radius2 = fl.Radius + sapi.WorldManager.ChunkSize;
			ForceRandomLandArea(fl.CenterPos.X, fl.CenterPos.Z, radius2);
		}
	}

	private void ForceRandomLandArea(int positionX, int positionZ, int radius)
	{
		int regionSize = sapi.WorldManager.RegionSize;
		int num = (positionX - radius) * noiseSizeOcean / regionSize;
		int num2 = (positionZ - radius) * noiseSizeOcean / regionSize;
		int num3 = (positionX + radius) * noiseSizeOcean / regionSize;
		int num4 = (positionZ + radius) * noiseSizeOcean / regionSize;
		LCGRandom lCGRandom = new LCGRandom(sapi.World.Seed);
		lCGRandom.InitPositionSeed(positionX, positionZ);
		NaturalShape naturalShape = new NaturalShape(lCGRandom);
		int num5 = num3 - num;
		int num6 = num4 - num2;
		naturalShape.InitSquare(num5, num6);
		naturalShape.Grow(num5 * num6);
		foreach (Vec2i position in naturalShape.GetPositions())
		{
			requireLandAt.Add(new XZ(num + position.X, num2 + position.Y));
		}
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		api.Network.GetChannel("latitudedata").SetMessageHandler<LatitudeData>(onLatitudeDataReceived);
		api.Event.LevelFinalize += Event_LevelFinalize;
		capi = api;
	}

	private void Event_LevelFinalize()
	{
		capi.World.Calendar.OnGetLatitude = getLatitude;
	}

	private void onLatitudeDataReceived(LatitudeData latdata)
	{
		this.latdata = latdata;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.InitWorldGenerator(initWorldGen, "standard");
		api.Event.InitWorldGenerator(initWorldGen, "superflat");
		api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
		api.Event.MapRegionGeneration(OnMapRegionGen, "superflat");
		api.Event.PlayerJoin += delegate(IServerPlayer plr)
		{
			api.Network.GetChannel("latitudedata").SendPacket(latdata, plr);
		};
	}

	public void initWorldGen()
	{
		requireLandAt.Clear();
		forceLandforms.Clear();
		long num = sapi.WorldManager.Seed;
		noiseSizeOcean = sapi.WorldManager.RegionSize / TerraGenConfig.oceanMapScale;
		noiseSizeUpheavel = sapi.WorldManager.RegionSize / TerraGenConfig.climateMapScale;
		noiseSizeClimate = sapi.WorldManager.RegionSize / TerraGenConfig.climateMapScale;
		noiseSizeForest = sapi.WorldManager.RegionSize / TerraGenConfig.forestMapScale;
		noiseSizeShrubs = sapi.WorldManager.RegionSize / TerraGenConfig.shrubMapScale;
		noiseSizeGeoProv = sapi.WorldManager.RegionSize / TerraGenConfig.geoProvMapScale;
		noiseSizeLandform = sapi.WorldManager.RegionSize / TerraGenConfig.landformMapScale;
		noiseSizeBeach = sapi.WorldManager.RegionSize / TerraGenConfig.beachMapScale;
		ITreeAttribute worldConfiguration = sapi.WorldManager.SaveGame.WorldConfiguration;
		string text = worldConfiguration.GetString("worldClimate", "realistic");
		float tempMul = worldConfiguration.GetString("globalTemperature", "1").ToFloat(1f);
		float rainMul = worldConfiguration.GetString("globalPrecipitation", "1").ToFloat(1f);
		latdata.polarEquatorDistance = worldConfiguration.GetString("polarEquatorDistance", "50000").ToInt(50000);
		upheavelCommonness = worldConfiguration.GetString("upheavelCommonness", "0.3").ToFloat(0.3f);
		float landcover = worldConfiguration.GetString("landcover", "1").ToFloat(1f);
		float oceanScaleMul = worldConfiguration.GetString("oceanscale", "1").ToFloat(1f);
		float landformScale = worldConfiguration.GetString("landformScale", "1.0").ToFloat(1f);
		NoiseClimate noiseClimate;
		if (text == "realistic")
		{
			int spawnMinTemp = 6;
			int spawnMaxTemp = 14;
			switch (worldConfiguration.GetString("startingClimate"))
			{
			case "hot":
				spawnMinTemp = 28;
				spawnMaxTemp = 32;
				break;
			case "warm":
				spawnMinTemp = 19;
				spawnMaxTemp = 23;
				break;
			case "cool":
				spawnMinTemp = -5;
				spawnMaxTemp = 1;
				break;
			case "icy":
				spawnMinTemp = -15;
				spawnMaxTemp = -10;
				break;
			}
			noiseClimate = new NoiseClimateRealistic(num, (double)sapi.WorldManager.MapSizeZ / (double)TerraGenConfig.climateMapScale / (double)TerraGenConfig.climateMapSubScale, latdata.polarEquatorDistance, spawnMinTemp, spawnMaxTemp);
			(noiseClimate as NoiseClimateRealistic).GeologicActivityStrength = worldConfiguration.GetString("geologicActivity").ToFloat(0.05f);
			latdata.isRealisticClimate = true;
			latdata.ZOffset = (noiseClimate as NoiseClimateRealistic).ZOffset;
		}
		else
		{
			noiseClimate = new NoiseClimatePatchy(num);
		}
		noiseClimate.rainMul = rainMul;
		noiseClimate.tempMul = tempMul;
		bool flag = GameVersion.IsLowerVersionThan(sapi.WorldManager.SaveGame.CreatedGameVersion, "1.20.0-pre.14");
		if (flag)
		{
			int num2 = sapi.WorldManager.MapSizeX / sapi.WorldManager.RegionSize / 2;
			int num3 = sapi.WorldManager.MapSizeZ / sapi.WorldManager.RegionSize / 2;
			requireLandAt.Add(new XZ(num2 * noiseSizeOcean, num3 * noiseSizeOcean));
		}
		else
		{
			int chunkSize = sapi.WorldManager.ChunkSize;
			int radius = 4 * chunkSize;
			int positionX = (sapi.WorldManager.MapSizeX + chunkSize) / 2;
			int positionZ = (sapi.WorldManager.MapSizeZ + chunkSize) / 2;
			ForceRandomLandArea(positionX, positionZ, radius);
		}
		climateGen = GetClimateMapGen(num + 1, noiseClimate);
		upheavelGen = GetGeoUpheavelMapGen(num + 873, TerraGenConfig.geoUpheavelMapScale);
		oceanGen = GetOceanMapGen(num + 1873, landcover, TerraGenConfig.oceanMapScale, oceanScaleMul, requireLandAt, flag);
		forestGen = GetForestMapGen(num + 2, TerraGenConfig.forestMapScale);
		bushGen = GetForestMapGen(num + 109, TerraGenConfig.shrubMapScale);
		flowerGen = GetForestMapGen(num + 223, TerraGenConfig.forestMapScale);
		beachGen = GetBeachMapGen(num + 2273, TerraGenConfig.beachMapScale);
		geologicprovinceGen = GetGeologicProvinceMapGen(num + 3, sapi);
		landformsGen = GetLandformMapGen(num + 4, noiseClimate, sapi, landformScale);
		sapi.World.Calendar.OnGetLatitude = getLatitude;
		int quantityOctaves = 2;
		float num4 = 2f * (float)TerraGenConfig.landformMapScale;
		float num5 = 0.9f;
		noisegenX = NormalizedSimplexNoise.FromDefaultOctaves(quantityOctaves, 1f / num4, num5, num + 2);
		noisegenZ = NormalizedSimplexNoise.FromDefaultOctaves(quantityOctaves, 1f / num4, num5, num + 1231296);
	}

	private double getLatitude(double posZ)
	{
		if (!latdata.isRealisticClimate)
		{
			return 0.5;
		}
		double num = (double)latdata.polarEquatorDistance / (double)TerraGenConfig.climateMapScale / (double)TerraGenConfig.climateMapSubScale;
		double num2 = 2.0;
		double num3 = num;
		double num4 = posZ / (double)TerraGenConfig.climateMapScale / (double)TerraGenConfig.climateMapSubScale + latdata.ZOffset;
		return num2 / num3 * (num3 - Math.Abs(Math.Abs(num4 / 2.0 - num3) % (2.0 * num3) - num3)) - 1.0;
	}

	private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams = null)
	{
		int geoProvMapPadding = TerraGenConfig.geoProvMapPadding;
		mapRegion.GeologicProvinceMap.Data = geologicprovinceGen.GenLayer(regionX * noiseSizeGeoProv - geoProvMapPadding, regionZ * noiseSizeGeoProv - geoProvMapPadding, noiseSizeGeoProv + 2 * geoProvMapPadding, noiseSizeGeoProv + 2 * geoProvMapPadding);
		mapRegion.GeologicProvinceMap.Size = noiseSizeGeoProv + 2 * geoProvMapPadding;
		mapRegion.GeologicProvinceMap.TopLeftPadding = (mapRegion.GeologicProvinceMap.BottomRightPadding = geoProvMapPadding);
		geoProvMapPadding = 2;
		mapRegion.ClimateMap.Data = climateGen.GenLayer(regionX * noiseSizeClimate - geoProvMapPadding, regionZ * noiseSizeClimate - geoProvMapPadding, noiseSizeClimate + 2 * geoProvMapPadding, noiseSizeClimate + 2 * geoProvMapPadding);
		mapRegion.ClimateMap.Size = noiseSizeClimate + 2 * geoProvMapPadding;
		mapRegion.ClimateMap.TopLeftPadding = (mapRegion.ClimateMap.BottomRightPadding = geoProvMapPadding);
		mapRegion.ForestMap.Size = noiseSizeForest + 1;
		mapRegion.ForestMap.BottomRightPadding = 1;
		forestGen.SetInputMap(mapRegion.ClimateMap, mapRegion.ForestMap);
		mapRegion.ForestMap.Data = forestGen.GenLayer(regionX * noiseSizeForest, regionZ * noiseSizeForest, noiseSizeForest + 1, noiseSizeForest + 1);
		int num = 3;
		mapRegion.UpheavelMap.Size = noiseSizeUpheavel + 2 * num;
		mapRegion.UpheavelMap.TopLeftPadding = num;
		mapRegion.UpheavelMap.BottomRightPadding = num;
		mapRegion.UpheavelMap.Data = upheavelGen.GenLayer(regionX * noiseSizeUpheavel - num, regionZ * noiseSizeUpheavel - num, noiseSizeUpheavel + 2 * num, noiseSizeUpheavel + 2 * num);
		int num2 = 5;
		mapRegion.OceanMap.Size = noiseSizeOcean + 2 * num2;
		mapRegion.OceanMap.TopLeftPadding = num2;
		mapRegion.OceanMap.BottomRightPadding = num2;
		mapRegion.OceanMap.Data = oceanGen.GenLayer(regionX * noiseSizeOcean - num2, regionZ * noiseSizeOcean - num2, noiseSizeOcean + 2 * num2, noiseSizeOcean + 2 * num2);
		mapRegion.BeachMap.Size = noiseSizeBeach + 1;
		mapRegion.BeachMap.BottomRightPadding = 1;
		mapRegion.BeachMap.Data = beachGen.GenLayer(regionX * noiseSizeBeach, regionZ * noiseSizeBeach, noiseSizeBeach + 1, noiseSizeBeach + 1);
		mapRegion.ShrubMap.Size = noiseSizeShrubs + 1;
		mapRegion.ShrubMap.BottomRightPadding = 1;
		bushGen.SetInputMap(mapRegion.ClimateMap, mapRegion.ShrubMap);
		mapRegion.ShrubMap.Data = bushGen.GenLayer(regionX * noiseSizeShrubs, regionZ * noiseSizeShrubs, noiseSizeShrubs + 1, noiseSizeShrubs + 1);
		mapRegion.FlowerMap.Size = noiseSizeForest + 1;
		mapRegion.FlowerMap.BottomRightPadding = 1;
		flowerGen.SetInputMap(mapRegion.ClimateMap, mapRegion.FlowerMap);
		mapRegion.FlowerMap.Data = flowerGen.GenLayer(regionX * noiseSizeForest, regionZ * noiseSizeForest, noiseSizeForest + 1, noiseSizeForest + 1);
		geoProvMapPadding = TerraGenConfig.landformMapPadding;
		mapRegion.LandformMap.Data = landformsGen.GenLayer(regionX * noiseSizeLandform - geoProvMapPadding, regionZ * noiseSizeLandform - geoProvMapPadding, noiseSizeLandform + 2 * geoProvMapPadding, noiseSizeLandform + 2 * geoProvMapPadding);
		mapRegion.LandformMap.Size = noiseSizeLandform + 2 * geoProvMapPadding;
		mapRegion.LandformMap.TopLeftPadding = (mapRegion.LandformMap.BottomRightPadding = geoProvMapPadding);
		if (chunkGenParams != null && chunkGenParams.HasAttribute("forceLandform"))
		{
			int num3 = chunkGenParams.GetInt("forceLandform");
			for (int i = 0; i < mapRegion.LandformMap.Data.Length; i++)
			{
				mapRegion.LandformMap.Data[i] = num3;
			}
		}
		int regionSize = sapi.WorldManager.RegionSize;
		foreach (ForceLandform forceLandform in forceLandforms)
		{
			this.forceLandform(mapRegion, regionX, regionZ, geoProvMapPadding, regionSize, forceLandform);
			forceNoUpheavel(mapRegion, regionX, regionZ, num, regionSize, forceLandform);
		}
		foreach (ForceClimate item in forceClimate)
		{
			ForceClimate(mapRegion, regionX, regionZ, geoProvMapPadding, regionSize, item);
		}
		mapRegion.DirtyForSaving = true;
	}

	private void forceNoUpheavel(IMapRegion mapRegion, int regionX, int regionZ, int pad, int regionsize, ForceLandform fl)
	{
		IntDataMap2D upheavelMap = mapRegion.UpheavelMap;
		int innerSize = upheavelMap.InnerSize;
		float num = 80f;
		float num2 = (float)pad / (float)noiseSizeUpheavel + num / (float)regionsize;
		float num3 = 0f - num2;
		float num4 = 1f + num2;
		int num5 = fl.Radius + 100;
		float num6 = (float)(fl.CenterPos.X - num5) / (float)regionsize - (float)regionX;
		float num7 = (float)(fl.CenterPos.X + num5) / (float)regionsize - (float)regionX;
		float num8 = (float)(fl.CenterPos.Z - num5) / (float)regionsize - (float)regionZ;
		float num9 = (float)(fl.CenterPos.Z + num5) / (float)regionsize - (float)regionZ;
		if (!(num7 >= num3) || !(num6 <= num4) || !(num9 >= num3) || !(num8 <= num4))
		{
			return;
		}
		double num10 = Math.Pow((double)num5 / (double)regionsize * (double)innerSize, 2.0);
		double num11 = (double)fl.CenterPos.X / (double)regionsize;
		double num12 = (double)fl.CenterPos.Z / (double)regionsize;
		double num13 = num11 - (double)regionX;
		double num14 = num12 - (double)regionZ;
		num13 *= (double)innerSize;
		num14 *= (double)innerSize;
		num6 = GameMath.Clamp(num6, num3, num4) * (float)innerSize - (float)pad;
		num7 = GameMath.Clamp(num7, num3, num4) * (float)innerSize + (float)pad;
		num8 = GameMath.Clamp(num8, num3, num4) * (float)innerSize - (float)pad;
		num9 = GameMath.Clamp(num9, num3, num4) * (float)innerSize + (float)pad;
		for (int i = (int)num6; (float)i < num7; i++)
		{
			for (int j = (int)num8; (float)j < num9; j++)
			{
				double num15 = Math.Pow((double)i - num13, 2.0) + Math.Pow((double)j - num14, 2.0);
				if (!(num15 >= num10))
				{
					double num16 = Math.Pow(1.0 - num15 / num10, 3.0) * 512.0;
					int num17 = i + pad;
					int num18 = j + pad;
					if (num17 >= 0 && num17 < upheavelMap.Size && num18 >= 0 && num18 < upheavelMap.Size)
					{
						upheavelMap.SetInt(num17, num18, (int)Math.Max(0.0, (double)upheavelMap.GetInt(num17, num18) - num16));
					}
				}
			}
		}
	}

	private void ForceClimate(IMapRegion mapRegion, int regionX, int regionZ, int pad, int regionsize, ForceClimate fl)
	{
		IntDataMap2D climateMap = mapRegion.ClimateMap;
		int innerSize = climateMap.InnerSize;
		float num = 80f;
		float num2 = (float)pad / (float)noiseSizeClimate + num / (float)regionsize;
		float num3 = 0f - num2;
		float num4 = 1f + num2;
		float num5 = 300f;
		float num6 = (float)fl.Radius + num5;
		float num7 = ((float)fl.CenterPos.X - num6) / (float)regionsize - (float)regionX;
		float num8 = ((float)fl.CenterPos.X + num6) / (float)regionsize - (float)regionX;
		float num9 = ((float)fl.CenterPos.Z - num6) / (float)regionsize - (float)regionZ;
		float num10 = ((float)fl.CenterPos.Z + num6) / (float)regionsize - (float)regionZ;
		if (!(num8 >= num3) || !(num7 <= num4) || !(num10 >= num3) || !(num9 <= num4))
		{
			return;
		}
		double num11 = Math.Pow((double)num6 / (double)regionsize * (double)innerSize, 2.0);
		double d = Math.Pow((double)num5 / (double)regionsize * (double)innerSize, 2.0);
		double num12 = Math.Sqrt(num11) - Math.Sqrt(d);
		double num13 = (double)fl.CenterPos.X / (double)regionsize;
		double num14 = (double)fl.CenterPos.Z / (double)regionsize;
		double num15 = num13 - (double)regionX;
		double num16 = num14 - (double)regionZ;
		num15 *= (double)innerSize;
		num16 *= (double)innerSize;
		num7 = GameMath.Clamp(num7, num3, num4) * (float)innerSize - (float)pad;
		num8 = GameMath.Clamp(num8, num3, num4) * (float)innerSize + (float)pad;
		num9 = GameMath.Clamp(num9, num3, num4) * (float)innerSize - (float)pad;
		num10 = GameMath.Clamp(num10, num3, num4) * (float)innerSize + (float)pad;
		int num17 = (fl.Climate >> 8) & 0xFF;
		int num18 = (fl.Climate >> 16) & 0xFF;
		for (int i = (int)num7; (float)i < num8; i++)
		{
			for (int j = (int)num9; (float)j < num10; j++)
			{
				double num19 = Math.Pow((double)i - num15, 2.0) + Math.Pow((double)j - num16, 2.0);
				if (!(num19 >= num11))
				{
					int num20 = i + pad;
					int num21 = j + pad;
					if (num20 >= 0 && num20 < climateMap.Size && num21 >= 0 && num21 < climateMap.Size)
					{
						int num22 = climateMap.GetInt(num20, num21);
						int num23 = num22 & 0xFF;
						int num24 = (num22 >> 8) & 0xFF;
						int num25 = (num22 >> 16) & 0xFF;
						double num26 = Math.Sqrt(num19);
						double num27 = Math.Max(0.0, num26 - num12);
						double t = Math.Min(1.0, num27 / num12);
						int num28 = (int)GameMath.Lerp(num18, num25, t);
						int num29 = (int)GameMath.Lerp(num17, num24, t);
						int value = (num28 << 16) + (num29 << 8) + num23;
						climateMap.SetInt(num20, num21, value);
					}
				}
			}
		}
	}

	private void forceLandform(IMapRegion mapRegion, int regionX, int regionZ, int pad, int regionsize, ForceLandform fl)
	{
		int innerSize = mapRegion.LandformMap.InnerSize;
		float num = 80f;
		float num2 = num / (float)regionsize * (float)innerSize;
		float num3 = (float)pad / (float)noiseSizeLandform + num / (float)regionsize;
		float num4 = 0f - num3;
		float num5 = 1f + num3;
		int radius = fl.Radius;
		float num6 = (float)(fl.CenterPos.X - radius) / (float)regionsize - (float)regionX;
		float num7 = (float)(fl.CenterPos.X + radius) / (float)regionsize - (float)regionX;
		float num8 = (float)(fl.CenterPos.Z - radius) / (float)regionsize - (float)regionZ;
		float num9 = (float)(fl.CenterPos.Z + radius) / (float)regionsize - (float)regionZ;
		if (!(num7 >= num4) || !(num6 <= num5) || !(num9 >= num4) || !(num8 <= num5))
		{
			return;
		}
		num6 = GameMath.Clamp(num6, num4, num5) * (float)innerSize - (float)pad;
		num7 = GameMath.Clamp(num7, num4, num5) * (float)innerSize + (float)pad;
		num8 = GameMath.Clamp(num8, num4, num5) * (float)innerSize - (float)pad;
		num9 = GameMath.Clamp(num9, num4, num5) * (float)innerSize + (float)pad;
		double num10 = Math.Pow((double)radius / (double)regionsize * (double)innerSize, 2.0);
		double num11 = (double)fl.CenterPos.X / (double)regionsize;
		double num12 = (double)fl.CenterPos.Z / (double)regionsize;
		double num13 = num11 - (double)regionX;
		double num14 = num12 - (double)regionZ;
		num13 *= (double)innerSize;
		num14 *= (double)innerSize;
		for (int i = (int)num6; (float)i < num7; i++)
		{
			for (int j = (int)num8; (float)j < num9; j++)
			{
				if (!(Math.Pow((double)i - num13, 2.0) + Math.Pow((double)j - num14, 2.0) >= num10))
				{
					double x = i + regionX * innerSize;
					double y = j + regionZ * innerSize;
					int num15 = (int)((double)num2 * noisegenX.Noise(x, y));
					int num16 = (int)((double)num2 * noisegenZ.Noise(x, y));
					int num17 = i + num15 + pad;
					int num18 = j + num16 + pad;
					if (num17 >= 0 && num17 < mapRegion.LandformMap.Size && num18 >= 0 && num18 < mapRegion.LandformMap.Size)
					{
						mapRegion.LandformMap.SetInt(num17, num18, fl.landFormIndex);
					}
				}
			}
		}
	}

	public static MapLayerBase GetDebugWindMap(long seed)
	{
		MapLayerDebugWind mapLayerDebugWind = new MapLayerDebugWind(seed + 1);
		mapLayerDebugWind.DebugDrawBitmap(DebugDrawMode.RGB, 0, 0, "Wind 1 - Wind");
		return mapLayerDebugWind;
	}

	public static MapLayerBase GetClimateMapGen(long seed, NoiseClimate climateNoise)
	{
		MapLayerBase mapLayerBase = new MapLayerClimate(seed + 1, climateNoise);
		mapLayerBase.DebugDrawBitmap(DebugDrawMode.RGB, 0, 0, "Climate 1 - Noise");
		mapLayerBase = new MapLayerPerlinWobble(seed + 2, mapLayerBase, 6, 0.7f, TerraGenConfig.climateMapWobbleScale, (float)TerraGenConfig.climateMapWobbleScale * 0.15f);
		mapLayerBase.DebugDrawBitmap(DebugDrawMode.RGB, 0, 0, "Climate 2 - Perlin Wobble");
		return mapLayerBase;
	}

	public static MapLayerBase GetOreMap(long seed, NoiseOre oreNoise, float scaleMul, float contrast, float sub)
	{
		MapLayerBase mapLayerBase = new MapLayerOre(seed + 1, oreNoise, scaleMul, contrast, sub);
		mapLayerBase.DebugDrawBitmap(DebugDrawMode.RGB, 0, 0, 512, "Ore 1 - Noise");
		mapLayerBase = new MapLayerPerlinWobble(seed + 2, mapLayerBase, 5, 0.85f, TerraGenConfig.oreMapWobbleScale, (float)TerraGenConfig.oreMapWobbleScale * 0.15f);
		mapLayerBase.DebugDrawBitmap(DebugDrawMode.RGB, 0, 0, 512, "Ore 1 - Perlin Wobble");
		return mapLayerBase;
	}

	public static MapLayerBase GetDepositVerticalDistort(long seed)
	{
		double[] thresholds = new double[4] { 0.1, 0.1, 0.1, 0.1 };
		MapLayerPerlin mapLayerPerlin = new MapLayerPerlin(seed + 1, 4, 0.8f, 25 * TerraGenConfig.depositVerticalDistortScale, 40, thresholds);
		mapLayerPerlin.DebugDrawBitmap(DebugDrawMode.RGB, 0, 0, "Vertical Distort");
		return mapLayerPerlin;
	}

	public static MapLayerBase GetForestMapGen(long seed, int scale)
	{
		return new MapLayerWobbledForest(seed + 1, 3, 0.9f, scale, 600f, -100);
	}

	public static MapLayerBase GetGeoUpheavelMapGen(long seed, int scale)
	{
		MapLayerPerlinUpheavel parent = new MapLayerPerlinUpheavel(seed, upheavelCommonness, scale, 600f, -300);
		return new MapLayerBlur(0L, parent, 3);
	}

	public static MapLayerBase GetOceanMapGen(long seed, float landcover, int oceanMapScale, float oceanScaleMul, List<XZ> requireLandAt, bool requiresSpawnOffset)
	{
		MapLayerOceans parent = new MapLayerOceans(seed, (float)oceanMapScale * oceanScaleMul, landcover, requireLandAt, requiresSpawnOffset);
		return new MapLayerBlur(0L, parent, 5);
	}

	public static MapLayerBase GetBeachMapGen(long seed, int scale)
	{
		MapLayerPerlin parent = new MapLayerPerlin(seed + 1, 6, 0.9f, scale / 3, 255, new double[6] { 0.20000000298023224, 0.20000000298023224, 0.20000000298023224, 0.20000000298023224, 0.20000000298023224, 0.20000000298023224 });
		return new MapLayerPerlinWobble(seed + 986876, parent, 4, 0.9f, scale / 2);
	}

	public static MapLayerBase GetGeologicProvinceMapGen(long seed, ICoreServerAPI api)
	{
		MapLayerGeoProvince mapLayerGeoProvince = new MapLayerGeoProvince(seed + 5, api);
		mapLayerGeoProvince.DebugDrawBitmap(DebugDrawMode.ProvinceRGB, 0, 0, "Geologic Province 1 - WobbleProvinces");
		return mapLayerGeoProvince;
	}

	public static MapLayerBase GetLandformMapGen(long seed, NoiseClimate climateNoise, ICoreServerAPI api, float landformScale)
	{
		MapLayerLandforms mapLayerLandforms = new MapLayerLandforms(seed + 12, climateNoise, api, landformScale);
		mapLayerLandforms.DebugDrawBitmap(DebugDrawMode.LandformRGB, 0, 0, "Landforms 1 - Wobble Landforms");
		return mapLayerLandforms;
	}
}
