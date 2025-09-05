namespace Vintagestory.API.Common.Entities;

[DocumentAsJson]
public class ClimateSpawnCondition
{
	[DocumentAsJson]
	public float MinTemp = -40f;

	[DocumentAsJson]
	public float MaxTemp = 40f;

	[DocumentAsJson]
	public float MinRain;

	[DocumentAsJson]
	public float MaxRain = 1f;

	[DocumentAsJson]
	public float MinForest;

	[DocumentAsJson]
	public float MaxForest = 1f;

	[DocumentAsJson]
	public float MinShrubs;

	[DocumentAsJson]
	public float MaxShrubs = 1f;

	[DocumentAsJson]
	public float MinY;

	[DocumentAsJson]
	public float MaxY = 2f;

	[DocumentAsJson]
	public float MinForestOrShrubs;

	public void SetFrom(ClimateSpawnCondition conds)
	{
		MinTemp = conds.MinTemp;
		MaxTemp = conds.MaxTemp;
		MinRain = conds.MinRain;
		MaxRain = conds.MaxRain;
		MinForest = conds.MinForest;
		MaxForest = conds.MaxForest;
		MinShrubs = conds.MinShrubs;
		MaxShrubs = conds.MaxShrubs;
		MinY = conds.MinY;
		MaxY = conds.MaxY;
		MinForestOrShrubs = conds.MinForestOrShrubs;
	}
}
