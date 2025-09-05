namespace Vintagestory.API.Common.Entities;

[DocumentAsJson]
public class RuntimeSpawnConditions : BaseSpawnConditions
{
	[DocumentAsJson]
	public double Chance = 1.0;

	[DocumentAsJson]
	public int MaxQuantity = 20;

	[DocumentAsJson]
	public QuantityByGroup MaxQuantityByGroup;

	[DocumentAsJson]
	public float SpawnCapPlayerScaling = 1f;

	[DocumentAsJson]
	public int MinDistanceToPlayer = 18;

	public bool doneInitialLoad;

	public RuntimeSpawnConditions Clone()
	{
		return new RuntimeSpawnConditions
		{
			Group = Group,
			MinLightLevel = MinLightLevel,
			MaxLightLevel = MaxLightLevel,
			LightLevelType = LightLevelType,
			HerdSize = HerdSize?.Clone(),
			Companions = (Companions?.Clone() as AssetLocation[]),
			InsideBlockCodes = (InsideBlockCodes?.Clone() as AssetLocation[]),
			RequireSolidGround = RequireSolidGround,
			TryOnlySurface = TryOnlySurface,
			MinTemp = MinTemp,
			MaxTemp = MaxTemp,
			MinRain = MinRain,
			MaxRain = MaxRain,
			MinForest = MinForest,
			MaxForest = MaxForest,
			MinShrubs = MinShrubs,
			MaxShrubs = MaxShrubs,
			ClimateValueMode = ClimateValueMode,
			MinForestOrShrubs = MinForestOrShrubs,
			Chance = Chance,
			MaxQuantity = MaxQuantity,
			MinDistanceToPlayer = MinDistanceToPlayer,
			MaxQuantityByGroup = MaxQuantityByGroup,
			SpawnCapPlayerScaling = SpawnCapPlayerScaling
		};
	}
}
