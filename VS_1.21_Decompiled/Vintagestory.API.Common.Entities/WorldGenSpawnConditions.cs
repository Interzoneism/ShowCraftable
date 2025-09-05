using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

[DocumentAsJson]
public class WorldGenSpawnConditions : BaseSpawnConditions
{
	[DocumentAsJson]
	public NatFloat TriesPerChunk = NatFloat.Zero;

	public WorldGenSpawnConditions Clone()
	{
		return new WorldGenSpawnConditions
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
			ClimateValueMode = ClimateValueMode,
			MinForest = MinForest,
			MaxForest = MaxForest,
			MinShrubs = MinShrubs,
			MaxShrubs = MaxShrubs,
			MinForestOrShrubs = MinForestOrShrubs,
			TriesPerChunk = TriesPerChunk?.Clone()
		};
	}
}
