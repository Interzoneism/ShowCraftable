namespace Vintagestory.API.Common;

[DocumentAsJson]
public class CombustibleProperties
{
	[DocumentAsJson]
	public int BurnTemperature;

	[DocumentAsJson]
	public float BurnDuration;

	[DocumentAsJson]
	public int HeatResistance = 500;

	[DocumentAsJson]
	public int MeltingPoint;

	[DocumentAsJson]
	public int MaxTemperature;

	[DocumentAsJson]
	public float MeltingDuration;

	[DocumentAsJson]
	public float SmokeLevel = 1f;

	[DocumentAsJson]
	public int SmeltedRatio = 1;

	[DocumentAsJson]
	public EnumSmeltType SmeltingType;

	[DocumentAsJson]
	public JsonItemStack SmeltedStack;

	[DocumentAsJson]
	public bool RequiresContainer = true;

	public CombustibleProperties Clone()
	{
		CombustibleProperties combustibleProperties = new CombustibleProperties();
		combustibleProperties.BurnDuration = BurnDuration;
		combustibleProperties.BurnTemperature = BurnTemperature;
		combustibleProperties.HeatResistance = HeatResistance;
		combustibleProperties.MeltingDuration = MeltingDuration;
		combustibleProperties.MeltingPoint = MeltingPoint;
		combustibleProperties.SmokeLevel = SmokeLevel;
		combustibleProperties.SmeltedRatio = SmeltedRatio;
		combustibleProperties.RequiresContainer = RequiresContainer;
		combustibleProperties.SmeltingType = SmeltingType;
		combustibleProperties.MaxTemperature = MaxTemperature;
		if (SmeltedStack != null)
		{
			combustibleProperties.SmeltedStack = SmeltedStack.Clone();
		}
		return combustibleProperties;
	}
}
