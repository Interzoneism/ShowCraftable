using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods;

public class GenDeposits : GenPartial
{
	public DepositVariant[] Deposits;

	public int depositChunkRange = 3;

	private int regionSize;

	private float chanceMultiplier;

	private IBlockAccessor blockAccessor;

	public LCGRandom depositRand;

	private BlockPos tmpPos = new BlockPos();

	private NormalizedSimplexNoise depositShapeDistortNoise;

	private Dictionary<BlockPos, DepositVariant> subDepositsToPlace = new Dictionary<BlockPos, DepositVariant>();

	private MapLayerBase verticalDistortTop;

	private MapLayerBase verticalDistortBottom;

	public bool addHandbookAttributes = true;

	protected override int chunkRange => depositChunkRange;

	public override double ExecuteOrder()
	{
		return 0.2;
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	internal void setApi(ICoreServerAPI api)
	{
		base.api = api;
		blockAccessor = api.World.BlockAccessor;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.ChunkColumnGeneration(GenChunkColumn, EnumWorldGenPass.TerrainFeatures, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
			api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
		}
	}

	public override void AssetsFinalize(ICoreAPI api)
	{
		initAssets(api as ICoreServerAPI, blockCallbacks: true);
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		blockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: true);
	}

	public void reloadWorldGen()
	{
		initAssets(api, blockCallbacks: true);
		initWorldGen();
	}

	public void initAssets(ICoreServerAPI api, bool blockCallbacks)
	{
		chanceMultiplier = api.Assets.Get("worldgen/deposits.json").ToObject<Deposits>().ChanceMultiplier;
		IOrderedEnumerable<KeyValuePair<AssetLocation, DepositVariant[]>> orderedEnumerable = from d in api.Assets.GetMany<DepositVariant[]>(api.World.Logger, "worldgen/deposits/")
			orderby d.Key.ToString()
			select d;
		List<DepositVariant> list = new List<DepositVariant>();
		foreach (KeyValuePair<AssetLocation, DepositVariant[]> item in orderedEnumerable)
		{
			DepositVariant[] value = item.Value;
			foreach (DepositVariant depositVariant in value)
			{
				depositVariant.fromFile = item.Key.ToString();
				depositVariant.WithBlockCallback &= blockCallbacks;
				list.Add(depositVariant);
				if (depositVariant.ChildDeposits != null)
				{
					DepositVariant[] childDeposits = depositVariant.ChildDeposits;
					foreach (DepositVariant obj in childDeposits)
					{
						obj.fromFile = item.Key.ToString();
						obj.parentDeposit = depositVariant;
						obj.WithBlockCallback &= blockCallbacks;
					}
				}
			}
		}
		Deposits = list.ToArray();
		depositShapeDistortNoise = NormalizedSimplexNoise.FromDefaultOctaves(3, 0.10000000149011612, 0.8999999761581421, 1L);
		regionSize = api.WorldManager.RegionSize;
		depositRand = new LCGRandom(api.WorldManager.Seed + 34613);
		for (int num3 = 0; num3 < Deposits.Length; num3++)
		{
			DepositVariant obj2 = Deposits[num3];
			obj2.addHandbookAttributes = addHandbookAttributes;
			obj2.Init(api, depositRand, depositShapeDistortNoise);
		}
	}

	public override void initWorldGen()
	{
		base.initWorldGen();
		int seed = api.WorldManager.Seed;
		Dictionary<string, MapLayerBase> maplayersByCode = new Dictionary<string, MapLayerBase>();
		for (int i = 0; i < Deposits.Length; i++)
		{
			DepositVariant depositVariant = Deposits[i];
			if (depositVariant.WithOreMap)
			{
				depositVariant.OreMapLayer = getOrCreateMapLayer(seed, depositVariant.Code, maplayersByCode, depositVariant.OreMapScale, depositVariant.OreMapContrast, depositVariant.OreMapSub);
			}
			if (depositVariant.ChildDeposits == null)
			{
				continue;
			}
			for (int j = 0; j < depositVariant.ChildDeposits.Length; j++)
			{
				DepositVariant depositVariant2 = depositVariant.ChildDeposits[j];
				if (depositVariant2.WithOreMap)
				{
					depositVariant2.OreMapLayer = getOrCreateMapLayer(seed, depositVariant2.Code, maplayersByCode, depositVariant.OreMapScale, depositVariant.OreMapContrast, depositVariant.OreMapSub);
				}
			}
		}
		verticalDistortBottom = GenMaps.GetDepositVerticalDistort(seed + 12);
		verticalDistortTop = GenMaps.GetDepositVerticalDistort(seed + 28);
		api.Logger?.VerboseDebug("Initialised GenDeposits");
	}

	private MapLayerBase getOrCreateMapLayer(int seed, string oremapCode, Dictionary<string, MapLayerBase> maplayersByCode, float scaleMul, float contrastMul, float sub)
	{
		if (!maplayersByCode.TryGetValue(oremapCode, out var value))
		{
			NoiseOre oreNoise = new NoiseOre(seed + oremapCode.GetNonRandomizedHashCode());
			value = (maplayersByCode[oremapCode] = GenMaps.GetOreMap(seed + oremapCode.GetNonRandomizedHashCode() + 1, oreNoise, scaleMul, contrastMul, sub));
		}
		return value;
	}

	public void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams = null)
	{
		int num = 2;
		TerraGenConfig.depositVerticalDistortScale = 2;
		int num2 = api.WorldManager.RegionSize / TerraGenConfig.depositVerticalDistortScale;
		IntDataMap2D oreMapVerticalDistortBottom = mapRegion.OreMapVerticalDistortBottom;
		oreMapVerticalDistortBottom.Size = num2 + 2 * num;
		oreMapVerticalDistortBottom.BottomRightPadding = (oreMapVerticalDistortBottom.TopLeftPadding = num);
		oreMapVerticalDistortBottom.Data = verticalDistortBottom.GenLayer(regionX * num2 - num, regionZ * num2 - num, num2 + 2 * num, num2 + 2 * num);
		IntDataMap2D oreMapVerticalDistortTop = mapRegion.OreMapVerticalDistortTop;
		oreMapVerticalDistortTop.Size = num2 + 2 * num;
		oreMapVerticalDistortTop.BottomRightPadding = (oreMapVerticalDistortTop.TopLeftPadding = num);
		oreMapVerticalDistortTop.Data = verticalDistortTop.GenLayer(regionX * num2 - num, regionZ * num2 - num, num2 + 2 * num, num2 + 2 * num);
		for (int i = 0; i < Deposits.Length; i++)
		{
			Deposits[i].OnMapRegionGen(mapRegion, regionX, regionZ);
		}
	}

	protected override void GenChunkColumn(IChunkColumnGenerateRequest request)
	{
		if (blockAccessor is IWorldGenBlockAccessor worldGenBlockAccessor)
		{
			worldGenBlockAccessor.BeginColumn();
		}
		base.GenChunkColumn(request);
	}

	public override void GeneratePartial(IServerChunk[] chunks, int chunkX, int chunkZ, int chunkdX, int chunkdZ)
	{
		LCGRandom lCGRandom = chunkRand;
		int num = chunkX + chunkdX;
		int num2 = chunkZ + chunkdZ;
		int num3 = num * 32;
		int num4 = num2 * 32;
		subDepositsToPlace.Clear();
		float num5 = (float)api.WorldManager.MapSizeY / 256f;
		for (int i = 0; i < Deposits.Length; i++)
		{
			DepositVariant depositVariant = Deposits[i];
			float num6 = (depositVariant.WithOreMap ? depositVariant.GetOreMapFactor(num, num2) : 1f);
			float num7 = depositVariant.TriesPerChunk * num6 * chanceMultiplier * (depositVariant.ScaleWithWorldheight ? num5 : 1f);
			int num8 = (int)num7;
			num8 += (((float)lCGRandom.NextInt(100) < 100f * (num7 - (float)num8)) ? 1 : 0);
			while (num8-- > 0)
			{
				tmpPos.Set(num3 + lCGRandom.NextInt(32), -99, num4 + lCGRandom.NextInt(32));
				long worldSeed = lCGRandom.NextInt(10000000);
				depositRand.SetWorldSeed(worldSeed);
				depositRand.InitPositionSeed(num, num2);
				GenDeposit(chunks, chunkX, chunkZ, tmpPos, depositVariant);
			}
		}
		foreach (KeyValuePair<BlockPos, DepositVariant> item in subDepositsToPlace)
		{
			depositRand.SetWorldSeed(lCGRandom.NextInt(10000000));
			depositRand.InitPositionSeed(num, num2);
			item.Value.GeneratorInst.GenDeposit(blockAccessor, chunks, chunkX, chunkZ, item.Key, ref subDepositsToPlace);
		}
	}

	public virtual void GenDeposit(IServerChunk[] chunks, int chunkX, int chunkZ, BlockPos depoCenterPos, DepositVariant variant)
	{
		int num = GameMath.Mod(depoCenterPos.X, 32);
		int num2 = GameMath.Mod(depoCenterPos.Z, 32);
		if (variant.Climate != null)
		{
			IMapChunk mapChunk = api.WorldManager.GetMapChunk(depoCenterPos.X / 32, depoCenterPos.Z / 32);
			if (mapChunk == null)
			{
				return;
			}
			depoCenterPos.Y = mapChunk.RainHeightMap[num2 * 32 + num];
			IntDataMap2D intDataMap2D = blockAccessor.GetMapRegion(depoCenterPos.X / regionSize, depoCenterPos.Z / regionSize)?.ClimateMap;
			if (intDataMap2D == null)
			{
				return;
			}
			float x = (float)((double)depoCenterPos.X / (double)regionSize % 1.0);
			float z = (float)((double)depoCenterPos.Z / (double)regionSize % 1.0);
			int unpaddedColorLerpedForNormalizedPos = intDataMap2D.GetUnpaddedColorLerpedForNormalizedPos(x, z);
			float num3 = (float)Climate.GetRainFall((unpaddedColorLerpedForNormalizedPos >> 8) & 0xFF, depoCenterPos.Y) / 255f;
			if (num3 < variant.Climate.MinRain || num3 > variant.Climate.MaxRain)
			{
				return;
			}
			float scaledAdjustedTemperatureFloat = Climate.GetScaledAdjustedTemperatureFloat((unpaddedColorLerpedForNormalizedPos >> 16) & 0xFF, depoCenterPos.Y - TerraGenConfig.seaLevel);
			if (scaledAdjustedTemperatureFloat < variant.Climate.MinTemp || scaledAdjustedTemperatureFloat > variant.Climate.MaxTemp)
			{
				return;
			}
			double num4 = TerraGenConfig.seaLevel;
			double num5 = (((double)depoCenterPos.Y > num4) ? (1.0 + ((double)depoCenterPos.Y - num4) / ((double)api.World.BlockAccessor.MapSizeY - num4)) : ((double)depoCenterPos.Y / num4));
			if (num5 < (double)variant.Climate.MinY || num5 > (double)variant.Climate.MaxY)
			{
				return;
			}
		}
		variant.GeneratorInst?.GenDeposit(blockAccessor, chunks, chunkX, chunkZ, depoCenterPos, ref subDepositsToPlace);
	}
}
