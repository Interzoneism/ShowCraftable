using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class GenTerra : ModStdWorldGen
{
	private struct ThreadLocalTempData
	{
		public double[] LerpedAmplitudes;

		public double[] LerpedThresholds;

		public float[] landformWeights;
	}

	private struct WeightedTaper
	{
		public float TerrainYPos;

		public float Weight;
	}

	private struct ColumnResult
	{
		public BitArray ColumnBlockSolidities;

		public int WaterBlockID;
	}

	private struct VectorXZ
	{
		public double X;

		public double Z;

		public static VectorXZ operator *(VectorXZ a, double b)
		{
			return new VectorXZ
			{
				X = a.X * b,
				Z = a.Z * b
			};
		}
	}

	private ICoreServerAPI api;

	private const double terrainDistortionMultiplier = 4.0;

	private const double terrainDistortionThreshold = 40.0;

	private const double geoDistortionMultiplier = 10.0;

	private const double geoDistortionThreshold = 10.0;

	private const double maxDistortionAmount = 190.91883092036784;

	private int maxThreads;

	private LandformsWorldProperty landforms;

	private float[][] terrainYThresholds;

	private Dictionary<int, LerpedWeightedIndex2DMap> LandformMapByRegion = new Dictionary<int, LerpedWeightedIndex2DMap>(10);

	private int regionMapSize;

	private float noiseScale;

	private int terrainGenOctaves = 9;

	private NewNormalizedSimplexFractalNoise terrainNoise;

	private SimplexNoise distort2dx;

	private SimplexNoise distort2dz;

	private NormalizedSimplexNoise geoUpheavalNoise;

	private WeightedTaper[] taperMap;

	private ThreadLocal<ThreadLocalTempData> tempDataThreadLocal;

	private ColumnResult[] columnResults;

	private bool[] layerFullySolid;

	private bool[] layerFullyEmpty;

	private int[] borderIndicesByCardinal;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.0;
	}

	public override void AssetsFinalize(ICoreAPI coreApi)
	{
		api = (ICoreServerAPI)coreApi;
		if (!(api.WorldManager.SaveGame.WorldType != "standard"))
		{
			TerraGenConfig.seaLevel = (int)(0.4313725490196078 * (double)api.WorldManager.MapSizeY);
			api.WorldManager.SetSeaLevel(TerraGenConfig.seaLevel);
			Climate.Sealevel = TerraGenConfig.seaLevel;
		}
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		api.Event.InitWorldGenerator(initWorldGen, "standard");
		api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Terrain, "standard");
	}

	public void initWorldGen()
	{
		LoadGlobalConfig(api);
		LandformMapByRegion.Clear();
		maxThreads = Math.Clamp(Environment.ProcessorCount - (api.Server.IsDedicated ? 4 : 6), 1, api.Server.Config.HostedMode ? 4 : 10);
		if (api.Server.ReducedServerThreads && maxThreads > 1)
		{
			maxThreads = 2;
		}
		regionMapSize = (int)Math.Ceiling((double)api.WorldManager.MapSizeX / (double)api.WorldManager.RegionSize);
		noiseScale = Math.Max(1f, (float)api.WorldManager.MapSizeY / 256f);
		terrainGenOctaves = TerraGenConfig.GetTerrainOctaveCount(api.WorldManager.MapSizeY);
		terrainNoise = NewNormalizedSimplexFractalNoise.FromDefaultOctaves(terrainGenOctaves, 0.00030618621784789723 / (double)noiseScale, 0.9, api.WorldManager.Seed);
		distort2dx = new SimplexNoise(new double[4] { 55.0, 40.0, 30.0, 10.0 }, scaleAdjustedFreqs(new double[4] { 0.2, 0.4, 0.8, 1.5384615384615383 }, noiseScale), api.World.Seed + 9876);
		distort2dz = new SimplexNoise(new double[4] { 55.0, 40.0, 30.0, 10.0 }, scaleAdjustedFreqs(new double[4] { 0.2, 0.4, 0.8, 1.5384615384615383 }, noiseScale), api.World.Seed + 9876 + 2);
		geoUpheavalNoise = new NormalizedSimplexNoise(new double[6] { 55.0, 40.0, 30.0, 15.0, 7.0, 4.0 }, scaleAdjustedFreqs(new double[6]
		{
			0.18181818181818182,
			0.4,
			48.0 / 55.0,
			1.6783216783216783,
			2.6666666666666665,
			4.8
		}, noiseScale), api.World.Seed + 9876 + 1);
		tempDataThreadLocal = new ThreadLocal<ThreadLocalTempData>(() => new ThreadLocalTempData
		{
			LerpedAmplitudes = new double[terrainGenOctaves],
			LerpedThresholds = new double[terrainGenOctaves],
			landformWeights = new float[NoiseLandforms.landforms.LandFormsByIndex.Length]
		});
		columnResults = new ColumnResult[1024];
		layerFullyEmpty = new bool[api.WorldManager.MapSizeY];
		layerFullySolid = new bool[api.WorldManager.MapSizeY];
		taperMap = new WeightedTaper[1024];
		for (int num = 0; num < 1024; num++)
		{
			columnResults[num].ColumnBlockSolidities = new BitArray(api.WorldManager.MapSizeY);
		}
		borderIndicesByCardinal = new int[8];
		borderIndicesByCardinal[(int)Cardinal.NorthEast] = 992;
		borderIndicesByCardinal[(int)Cardinal.SouthEast] = 0;
		borderIndicesByCardinal[(int)Cardinal.SouthWest] = 31;
		borderIndicesByCardinal[(int)Cardinal.NorthWest] = 1023;
		landforms = null;
	}

	private double[] scaleAdjustedFreqs(double[] vs, float horizontalScale)
	{
		for (int i = 0; i < vs.Length; i++)
		{
			vs[i] /= horizontalScale;
		}
		return vs;
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		if (request.RequiresChunkBorderSmoothing)
		{
			ushort[][] neighbourTerrainHeight = request.NeighbourTerrainHeight;
			if (neighbourTerrainHeight[(int)Cardinal.North] != null)
			{
				neighbourTerrainHeight[(int)Cardinal.NorthEast] = null;
				neighbourTerrainHeight[(int)Cardinal.NorthWest] = null;
			}
			if (neighbourTerrainHeight[(int)Cardinal.East] != null)
			{
				neighbourTerrainHeight[(int)Cardinal.NorthEast] = null;
				neighbourTerrainHeight[(int)Cardinal.SouthEast] = null;
			}
			if (neighbourTerrainHeight[(int)Cardinal.South] != null)
			{
				neighbourTerrainHeight[(int)Cardinal.SouthWest] = null;
				neighbourTerrainHeight[(int)Cardinal.SouthEast] = null;
			}
			if (neighbourTerrainHeight[(int)Cardinal.West] != null)
			{
				neighbourTerrainHeight[(int)Cardinal.SouthWest] = null;
				neighbourTerrainHeight[(int)Cardinal.NorthWest] = null;
			}
			for (int i = 0; i < 32; i++)
			{
				borderIndicesByCardinal[(int)Cardinal.North] = 992 + i;
				borderIndicesByCardinal[(int)Cardinal.South] = i;
				for (int j = 0; j < 32; j++)
				{
					double num = 0.0;
					double num2 = 0.0;
					float num3 = 0f;
					borderIndicesByCardinal[(int)Cardinal.East] = j * 32;
					borderIndicesByCardinal[(int)Cardinal.West] = j * 32 + 32 - 1;
					for (int k = 0; k < Cardinal.ALL.Length; k++)
					{
						ushort[] array = neighbourTerrainHeight[k];
						if (array != null)
						{
							float num4 = 0f;
							switch (k)
							{
							case 0:
								num4 = (float)j / 32f;
								break;
							case 1:
								num4 = 1f - ((float)i + 1f) / 32f + (float)j / 32f;
								break;
							case 2:
								num4 = 1f - ((float)i + 1f) / 32f;
								break;
							case 3:
								num4 = 1f - ((float)i + 1f) / 32f + 1f - ((float)j + 1f) / 32f;
								break;
							case 4:
								num4 = 1f - ((float)j + 1f) / 32f;
								break;
							case 5:
								num4 = (float)i / 32f + 1f - ((float)j + 1f) / 32f;
								break;
							case 6:
								num4 = (float)i / 32f;
								break;
							case 7:
								num4 = (float)i / 32f + (float)j / 32f;
								break;
							}
							float num5 = Math.Max(0f, 1f - num4);
							float num6 = num5 * num5;
							float num7 = (float)(int)array[borderIndicesByCardinal[k]] + 0.5f;
							num2 += (double)num7 * Math.Max(0.0001, num6);
							num += (double)num6;
							num3 = Math.Max(num3, num6);
						}
					}
					taperMap[j * 32 + i] = new WeightedTaper
					{
						TerrainYPos = (float)(num2 / Math.Max(0.0001, num)),
						Weight = num3
					};
				}
			}
		}
		if (landforms == null)
		{
			landforms = NoiseLandforms.landforms;
			terrainYThresholds = new float[landforms.LandFormsByIndex.Length][];
			for (int l = 0; l < landforms.LandFormsByIndex.Length; l++)
			{
				terrainYThresholds[l] = landforms.LandFormsByIndex[l].TerrainYThresholds;
			}
		}
		generate(request.Chunks, request.ChunkX, request.ChunkZ, request.RequiresChunkBorderSmoothing);
	}

	private void generate(IServerChunk[] chunks, int chunkX, int chunkZ, bool requiresChunkBorderSmoothing)
	{
		IMapChunk mapChunk = chunks[0].MapChunk;
		int upheavalMapUpLeft = 0;
		int upheavalMapUpRight = 0;
		int upheavalMapBotLeft = 0;
		int upheavalMapBotRight = 0;
		IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
		IntDataMap2D oceanMap = chunks[0].MapChunk.MapRegion.OceanMap;
		int num = api.WorldManager.RegionSize / 32;
		float num2 = (float)climateMap.InnerSize / (float)num;
		int num3 = chunkX % num;
		int num4 = chunkZ % num;
		int unpaddedInt = climateMap.GetUnpaddedInt((int)((float)num3 * num2), (int)((float)num4 * num2));
		int unpaddedInt2 = climateMap.GetUnpaddedInt((int)((float)num3 * num2 + num2), (int)((float)num4 * num2));
		int unpaddedInt3 = climateMap.GetUnpaddedInt((int)((float)num3 * num2), (int)((float)num4 * num2 + num2));
		int unpaddedInt4 = climateMap.GetUnpaddedInt((int)((float)num3 * num2 + num2), (int)((float)num4 * num2 + num2));
		int oceanUpLeft = 0;
		int oceanUpRight = 0;
		int oceanBotLeft = 0;
		int oceanBotRight = 0;
		if (oceanMap != null && oceanMap.Data.Length != 0)
		{
			float num5 = (float)oceanMap.InnerSize / (float)num;
			oceanUpLeft = oceanMap.GetUnpaddedInt((int)((float)num3 * num5), (int)((float)num4 * num5));
			oceanUpRight = oceanMap.GetUnpaddedInt((int)((float)num3 * num5 + num5), (int)((float)num4 * num5));
			oceanBotLeft = oceanMap.GetUnpaddedInt((int)((float)num3 * num5), (int)((float)num4 * num5 + num5));
			oceanBotRight = oceanMap.GetUnpaddedInt((int)((float)num3 * num5 + num5), (int)((float)num4 * num5 + num5));
		}
		IntDataMap2D upheavelMap = chunks[0].MapChunk.MapRegion.UpheavelMap;
		if (upheavelMap != null)
		{
			float num6 = (float)upheavelMap.InnerSize / (float)num;
			upheavalMapUpLeft = upheavelMap.GetUnpaddedInt((int)((float)num3 * num6), (int)((float)num4 * num6));
			upheavalMapUpRight = upheavelMap.GetUnpaddedInt((int)((float)num3 * num6 + num6), (int)((float)num4 * num6));
			upheavalMapBotLeft = upheavelMap.GetUnpaddedInt((int)((float)num3 * num6), (int)((float)num4 * num6 + num6));
			upheavalMapBotRight = upheavelMap.GetUnpaddedInt((int)((float)num3 * num6 + num6), (int)((float)num4 * num6 + num6));
		}
		int defaultRockId = GlobalConfig.defaultRockId;
		float oceanicityFac = (float)(api.WorldManager.MapSizeY / 256) * 0.33333f;
		float num7 = mapChunk.MapRegion.LandformMap.InnerSize / num;
		float baseX = (float)(chunkX % num) * num7;
		float baseZ = (float)(chunkZ % num) * num7;
		LerpedWeightedIndex2DMap landLerpMap = GetOrLoadLerpedLandformMap(mapChunk, chunkX / num, chunkZ / num);
		float[] landformWeights = tempDataThreadLocal.Value.landformWeights;
		GetInterpolatedOctaves(landLerpMap.WeightsAt(baseX, baseZ, landformWeights), out var octNoiseX0, out var octThX0);
		GetInterpolatedOctaves(landLerpMap.WeightsAt(baseX + num7, baseZ, landformWeights), out var octNoiseX1, out var octThX1);
		GetInterpolatedOctaves(landLerpMap.WeightsAt(baseX, baseZ + num7, landformWeights), out var octNoiseX2, out var octThX2);
		GetInterpolatedOctaves(landLerpMap.WeightsAt(baseX + num7, baseZ + num7, landformWeights), out var octNoiseX3, out var octThX3);
		float[][] terrainYThresholds = this.terrainYThresholds;
		ushort[] rainHeightMap = chunks[0].MapChunk.RainHeightMap;
		ushort[] worldGenTerrainHeightMap = chunks[0].MapChunk.WorldGenTerrainHeightMap;
		int mapsizeY = api.WorldManager.MapSizeY;
		int mapsizeYm2 = api.WorldManager.MapSizeY - 2;
		int taperThreshold = (int)((float)mapsizeY * 0.9f);
		double geoUpheavalAmplitude = 255.0;
		float chunkPixelBlockStep = num7 * (1f / 32f);
		double verticalNoiseRelativeFrequency = 0.5 / TerraGenConfig.terrainNoiseVerticalScale;
		for (int i = 0; i < layerFullySolid.Length; i++)
		{
			layerFullySolid[i] = true;
		}
		for (int j = 0; j < layerFullyEmpty.Length; j++)
		{
			layerFullyEmpty[j] = true;
		}
		layerFullyEmpty[mapsizeY - 1] = false;
		Parallel.For(0, 1024, new ParallelOptions
		{
			MaxDegreeOfParallelism = maxThreads
		}, delegate(int chunkIndex2d)
		{
			int num20 = chunkIndex2d % 32;
			int num21 = chunkIndex2d / 32;
			int num22 = chunkX * 32 + num20;
			int num23 = chunkZ * 32 + num21;
			BitArray columnBlockSolidities = columnResults[chunkIndex2d].ColumnBlockSolidities;
			columnBlockSolidities.SetAll(value: false);
			double[] lerpedAmplitudes = tempDataThreadLocal.Value.LerpedAmplitudes;
			double[] lerpedThresholds = tempDataThreadLocal.Value.LerpedThresholds;
			float[] landformWeights2 = tempDataThreadLocal.Value.landformWeights;
			landLerpMap.WeightsAt(baseX + (float)num20 * chunkPixelBlockStep, baseZ + (float)num21 * chunkPixelBlockStep, landformWeights2);
			for (int k = 0; k < lerpedAmplitudes.Length; k++)
			{
				lerpedAmplitudes[k] = GameMath.BiLerp(octNoiseX0[k], octNoiseX1[k], octNoiseX2[k], octNoiseX3[k], (float)num20 * (1f / 32f), (float)num21 * (1f / 32f));
				lerpedThresholds[k] = GameMath.BiLerp(octThX0[k], octThX1[k], octThX2[k], octThX3[k], (float)num20 * (1f / 32f), (float)num21 * (1f / 32f));
			}
			VectorXZ vectorXZ = NewDistortionNoise(num22, num23);
			VectorXZ vectorXZ2 = ApplyIsotropicDistortionThreshold(vectorXZ * 4.0, 40.0, 763.6753236814714);
			float upheavalStrength = GameMath.BiLerp(upheavalMapUpLeft, upheavalMapUpRight, upheavalMapBotLeft, upheavalMapBotRight, (float)num20 * (1f / 32f), (float)num21 * (1f / 32f));
			float num24 = GameMath.BiLerp(oceanUpLeft, oceanUpRight, oceanBotLeft, oceanBotRight, (float)num20 * (1f / 32f), (float)num21 * (1f / 32f)) * oceanicityFac;
			VectorXZ distGeo = ApplyIsotropicDistortionThreshold(vectorXZ * 10.0, 10.0, 1909.1883092036783);
			float num25 = num24 + ComputeOceanAndUpheavalDistY(upheavalStrength, num22, num23, distGeo);
			columnResults[chunkIndex2d].WaterBlockID = ((num24 > 1f) ? GlobalConfig.saltWaterBlockId : GlobalConfig.waterBlockId);
			NewNormalizedSimplexFractalNoise.ColumnNoise columnNoise = terrainNoise.ForColumn(verticalNoiseRelativeFrequency, lerpedAmplitudes, lerpedThresholds, (double)num22 + vectorXZ2.X, (double)num23 + vectorXZ2.Z);
			double boundMin = columnNoise.BoundMin;
			double boundMax = columnNoise.BoundMax;
			WeightedTaper weightedTaper = taperMap[chunkIndex2d];
			float ySlide = num25 - (float)(int)Math.Floor(num25);
			for (int l = 1; l <= mapsizeYm2; l++)
			{
				StartSampleDisplacedYThreshold((float)l + num25, mapsizeYm2, out var yBase);
				double threshold = 0.0;
				for (int m = 0; m < landformWeights2.Length; m++)
				{
					float num26 = landformWeights2[m];
					if (num26 != 0f)
					{
						threshold += (double)(num26 * ContinueSampleDisplacedYThreshold(yBase, ySlide, terrainYThresholds[m]));
					}
				}
				ComputeGeoUpheavalTaper(l, num25, taperThreshold, geoUpheavalAmplitude, mapsizeY, ref threshold);
				if (requiresChunkBorderSmoothing)
				{
					double num27 = (((float)l > weightedTaper.TerrainYPos) ? 1 : (-1));
					float num28 = Math.Abs((float)l - weightedTaper.TerrainYPos);
					double num29 = ((num28 > 10f) ? 0.0 : (distort2dx.Noise((double)(-(chunkX * 32 + num20)) / 10.0, (double)l / 10.0, (double)(-(chunkZ * 32 + num21)) / 10.0) / Math.Max(1.0, (double)num28 / 2.0)));
					num29 *= (double)GameMath.Clamp(2f * (1f - weightedTaper.Weight), 0f, 1f) * 0.1;
					threshold = GameMath.Lerp(threshold, num27 + num29, weightedTaper.Weight);
				}
				if (threshold <= boundMin)
				{
					columnBlockSolidities[l] = true;
					layerFullyEmpty[l] = false;
				}
				else
				{
					if (!(threshold < boundMax))
					{
						layerFullySolid[l] = false;
						for (int n = l + 1; n <= mapsizeYm2; n++)
						{
							layerFullySolid[n] = false;
						}
						break;
					}
					double inverseCurvedThresholder = 0.0 - NormalizedSimplexNoise.NoiseValueCurveInverse(threshold);
					inverseCurvedThresholder = columnNoise.NoiseSign(l, inverseCurvedThresholder);
					if (inverseCurvedThresholder > 0.0)
					{
						columnBlockSolidities[l] = true;
						layerFullyEmpty[l] = false;
					}
					else
					{
						layerFullySolid[l] = false;
					}
				}
			}
		});
		IChunkBlocks data = chunks[0].Data;
		data.SetBlockBulk(0, 32, 32, GlobalConfig.mantleBlockId);
		int num8;
		for (num8 = 1; num8 < mapsizeY - 1 && layerFullySolid[num8]; num8++)
		{
			if (num8 % 32 == 0)
			{
				data = chunks[num8 / 32].Data;
			}
			data.SetBlockBulk(num8 % 32 * 32 * 32, 32, 32, defaultRockId);
		}
		int seaLevel = TerraGenConfig.seaLevel;
		int num9 = 0;
		int num10 = mapsizeY - 2;
		while (num10 >= num8 && layerFullyEmpty[num10])
		{
			num10--;
		}
		if (num10 < seaLevel)
		{
			num10 = seaLevel;
		}
		num10++;
		for (int num11 = 0; num11 < 32; num11++)
		{
			int num12 = chunkZ * 32 + num11;
			int num13 = ChunkIndex2d(0, num11);
			for (int num14 = 0; num14 < 32; num14++)
			{
				ColumnResult columnResult = columnResults[num13];
				int waterBlockID = columnResult.WaterBlockID;
				num9 = waterBlockID;
				if (num8 < seaLevel && waterBlockID != GlobalConfig.saltWaterBlockId && !columnResult.ColumnBlockSolidities[seaLevel - 1])
				{
					int unscaledTemp = (GameMath.BiLerpRgbColor((float)num14 * (1f / 32f), (float)num11 * (1f / 32f), unpaddedInt, unpaddedInt2, unpaddedInt3, unpaddedInt4) >> 16) & 0xFF;
					float num15 = (float)distort2dx.Noise(chunkX * 32 + num14, num12) / 20f;
					if (Climate.GetScaledAdjustedTemperatureFloat(unscaledTemp, 0) + num15 < (float)TerraGenConfig.WaterFreezingTempOnGen)
					{
						num9 = GlobalConfig.lakeIceBlockId;
					}
				}
				worldGenTerrainHeightMap[num13] = (ushort)(num8 - 1);
				rainHeightMap[num13] = (ushort)(num8 - 1);
				data = chunks[num8 / 32].Data;
				for (int num16 = num8; num16 < num10; num16++)
				{
					int num17 = num16 % 32;
					if (columnResult.ColumnBlockSolidities[num16])
					{
						worldGenTerrainHeightMap[num13] = (ushort)num16;
						rainHeightMap[num13] = (ushort)num16;
						data[ChunkIndex3d(num14, num17, num11)] = defaultRockId;
					}
					else if (num16 < seaLevel)
					{
						int value;
						if (num16 == seaLevel - 1)
						{
							rainHeightMap[num13] = (ushort)num16;
							value = num9;
						}
						else
						{
							value = waterBlockID;
						}
						data.SetFluid(ChunkIndex3d(num14, num17, num11), value);
					}
					if (num17 == 31)
					{
						data = chunks[(num16 + 1) / 32].Data;
					}
				}
				num13++;
			}
		}
		ushort num18 = 0;
		for (int num19 = 0; num19 < rainHeightMap.Length; num19++)
		{
			num18 = Math.Max(num18, rainHeightMap[num19]);
		}
		chunks[0].MapChunk.YMax = num18;
	}

	private LerpedWeightedIndex2DMap GetOrLoadLerpedLandformMap(IMapChunk mapchunk, int regionX, int regionZ)
	{
		LandformMapByRegion.TryGetValue(regionZ * regionMapSize + regionX, out var value);
		if (value != null)
		{
			return value;
		}
		IntDataMap2D landformMap = mapchunk.MapRegion.LandformMap;
		return LandformMapByRegion[regionZ * regionMapSize + regionX] = new LerpedWeightedIndex2DMap(landformMap.Data, landformMap.Size, TerraGenConfig.landFormSmoothingRadius, landformMap.TopLeftPadding, landformMap.BottomRightPadding);
	}

	private void GetInterpolatedOctaves(float[] indices, out double[] amps, out double[] thresholds)
	{
		amps = new double[terrainGenOctaves];
		thresholds = new double[terrainGenOctaves];
		for (int i = 0; i < terrainGenOctaves; i++)
		{
			double num = 0.0;
			double num2 = 0.0;
			for (int j = 0; j < indices.Length; j++)
			{
				float num3 = indices[j];
				if (num3 != 0f)
				{
					LandformVariant landformVariant = landforms.LandFormsByIndex[j];
					num += landformVariant.TerrainOctaves[i] * (double)num3;
					num2 += landformVariant.TerrainOctaveThresholds[i] * (double)num3;
				}
			}
			amps[i] = num;
			thresholds[i] = num2;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void StartSampleDisplacedYThreshold(float distortedPosY, int mapSizeYm2, out int yBase)
	{
		yBase = GameMath.Clamp((int)Math.Floor(distortedPosY), 0, mapSizeYm2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private float ContinueSampleDisplacedYThreshold(int yBase, float ySlide, float[] thresholds)
	{
		return GameMath.Lerp(thresholds[yBase], thresholds[yBase + 1], ySlide);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private float ComputeOceanAndUpheavalDistY(float upheavalStrength, double worldX, double worldZ, VectorXZ distGeo)
	{
		float num = (float)geoUpheavalNoise.Noise((worldX + distGeo.X) / 400.0, (worldZ + distGeo.Z) / 400.0) * 0.9f;
		float num2 = Math.Min(0f, 0.5f - num);
		return upheavalStrength * num2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ComputeGeoUpheavalTaper(double posY, double distY, double taperThreshold, double geoUpheavalAmplitude, double mapSizeY, ref double threshold)
	{
		if (posY > taperThreshold && distY < -2.0)
		{
			double num = GameMath.Clamp(0.0 - distY, posY - mapSizeY, posY);
			double num2 = posY - taperThreshold;
			threshold += num2 * num / (40.0 * geoUpheavalAmplitude);
		}
	}

	private VectorXZ NewDistortionNoise(double worldX, double worldZ)
	{
		double x = worldX / 400.0;
		double y = worldZ / 400.0;
		SimplexNoise.NoiseFairWarpVector(distort2dx, distort2dz, x, y, out var distX, out var distY);
		return new VectorXZ
		{
			X = distX,
			Z = distY
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private VectorXZ ApplyIsotropicDistortionThreshold(VectorXZ dist, double threshold, double maximum)
	{
		double num = dist.X * dist.X + dist.Z * dist.Z;
		double num2 = threshold * threshold;
		if (num <= num2)
		{
			dist.X = (dist.Z = 0.0);
		}
		else
		{
			double num3 = (num - num2) / num;
			double num4 = maximum * maximum;
			double num5 = num4 / (num4 - num2);
			double num6 = num3 * num5;
			double num7 = num6 * num6;
			double num8 = maximum - threshold;
			double num9 = num7 * (num8 / maximum);
			dist *= num9;
		}
		return dist;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ChunkIndex3d(int x, int y, int z)
	{
		return (y * 32 + z) * 32 + x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ChunkIndex2d(int x, int z)
	{
		return z * 32 + x;
	}
}
