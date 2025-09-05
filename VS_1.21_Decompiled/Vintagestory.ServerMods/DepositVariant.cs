using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class DepositVariant : WorldPropertyVariant
{
	public string fromFile;

	[JsonProperty]
	public new string Code;

	[JsonProperty]
	public float TriesPerChunk;

	[JsonProperty]
	public string Generator;

	[JsonProperty]
	public bool WithOreMap;

	[JsonProperty]
	public float OreMapScale = 1f;

	[JsonProperty]
	public float OreMapContrast = 1f;

	[JsonProperty]
	public float OreMapSub;

	[JsonProperty]
	public string HandbookPageCode;

	[JsonProperty]
	public bool WithBlockCallback;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject Attributes;

	[JsonProperty]
	public ClimateConditions Climate;

	[JsonProperty]
	public DepositVariant[] ChildDeposits;

	[JsonProperty]
	public bool ScaleWithWorldheight = true;

	public DepositGeneratorBase GeneratorInst;

	public MapLayerBase OreMapLayer;

	private int noiseSizeOre;

	private int regionSize;

	private const int chunksize = 32;

	private ICoreServerAPI api;

	internal DepositVariant parentDeposit;

	public bool addHandbookAttributes;

	public void InitWithoutGenerator(ICoreServerAPI api)
	{
		this.api = api;
		regionSize = api.WorldManager.RegionSize;
		noiseSizeOre = regionSize / TerraGenConfig.oreMapScale;
	}

	public void Init(ICoreServerAPI api, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
	{
		this.api = api;
		InitWithoutGenerator(api);
		if (Generator == null)
		{
			api.World.Logger.Error("Error in deposit variant in file {0}: No generator defined! Must define a generator.", fromFile, Generator);
		}
		else
		{
			GeneratorInst = DepositGeneratorRegistry.CreateGenerator(Generator, Attributes, api, this, depositRand, noiseGen);
			if (GeneratorInst == null)
			{
				api.World.Logger.Error("Error in deposit variant in file {0}: No generator with code '{1}' found!", fromFile, Generator);
			}
		}
		if (Code == null)
		{
			api.World.Logger.Error("Error in deposit variant in file {0}: Deposit has no code! Defaulting to 'unknown'", fromFile);
			Code = "unknown";
		}
	}

	public void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ)
	{
		if (OreMapLayer != null && !mapRegion.OreMaps.ContainsKey(Code))
		{
			IntDataMap2D intDataMap2D = new IntDataMap2D();
			intDataMap2D.Size = noiseSizeOre + 1;
			intDataMap2D.BottomRightPadding = 1;
			intDataMap2D.Data = OreMapLayer.GenLayer(regionX * noiseSizeOre, regionZ * noiseSizeOre, noiseSizeOre + 1, noiseSizeOre + 1);
			mapRegion.OreMaps[Code] = intDataMap2D;
		}
		if (ChildDeposits == null)
		{
			return;
		}
		for (int i = 0; i < ChildDeposits.Length; i++)
		{
			DepositVariant depositVariant = ChildDeposits[i];
			if (depositVariant.OreMapLayer != null && !mapRegion.OreMaps.ContainsKey(depositVariant.Code))
			{
				IntDataMap2D intDataMap2D = new IntDataMap2D();
				intDataMap2D.Size = noiseSizeOre + 1;
				intDataMap2D.BottomRightPadding = 1;
				intDataMap2D.Data = depositVariant.OreMapLayer.GenLayer(regionX * noiseSizeOre, regionZ * noiseSizeOre, noiseSizeOre + 1, noiseSizeOre + 1);
				mapRegion.OreMaps[depositVariant.Code] = intDataMap2D;
			}
		}
	}

	public float GetOreMapFactor(int chunkx, int chunkz)
	{
		IMapRegion mapRegion = api?.WorldManager.GetMapRegion(chunkx * 32 / regionSize, chunkz * 32 / regionSize);
		if (mapRegion == null)
		{
			return 0f;
		}
		int num = (chunkx * 32 + 16) % regionSize;
		int num2 = (chunkz * 32 + 16) % regionSize;
		mapRegion.OreMaps.TryGetValue(Code, out var value);
		if (value != null)
		{
			float x = GameMath.Clamp((float)num / (float)regionSize * (float)noiseSizeOre, 0f, noiseSizeOre - 1);
			float z = GameMath.Clamp((float)num2 / (float)regionSize * (float)noiseSizeOre, 0f, noiseSizeOre - 1);
			return (float)(value.GetUnpaddedColorLerped(x, z) & 0xFF) / 255f;
		}
		return 0f;
	}

	public DepositVariant Clone()
	{
		DepositVariant depositVariant = new DepositVariant
		{
			fromFile = fromFile,
			Code = Code,
			TriesPerChunk = TriesPerChunk,
			Generator = Generator,
			WithOreMap = WithOreMap,
			WithBlockCallback = WithBlockCallback,
			Attributes = Attributes?.Clone(),
			Climate = Climate?.Clone(),
			ChildDeposits = ((ChildDeposits == null) ? null : ((DepositVariant[])ChildDeposits.Clone())),
			OreMapLayer = OreMapLayer,
			ScaleWithWorldheight = ScaleWithWorldheight
		};
		DepositVariant[] childDeposits = ChildDeposits;
		for (int i = 0; i < childDeposits.Length; i++)
		{
			childDeposits[i].parentDeposit = depositVariant;
		}
		depositVariant.GeneratorInst = DepositGeneratorRegistry.CreateGenerator(Generator, Attributes, api, depositVariant, GeneratorInst.DepositRand, GeneratorInst.DistortNoiseGen);
		return depositVariant;
	}

	public virtual void GetPropickReading(BlockPos pos, int oreDist, int[] blockColumn, out double ppt, out double totalFactor)
	{
		GeneratorInst.GetPropickReading(pos, oreDist, blockColumn, out ppt, out totalFactor);
	}
}
