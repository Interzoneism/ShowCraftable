using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Vintagestory.ServerMods.NoObf;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class GeologicProvinceVariant
{
	public int Index;

	public int ColorInt;

	[JsonProperty]
	public string Code;

	[JsonProperty]
	public string Hexcolor;

	[JsonProperty]
	public int Weight;

	[JsonProperty]
	public Dictionary<string, GeologicProvinceRockStrata> Rockstrata;

	public GeologicProvinceRockStrata[] RockStrataIndexed;

	public void init(int mapsizey)
	{
		float num = (float)mapsizey / 256f;
		RockStrataIndexed = new GeologicProvinceRockStrata[Enum.GetValues(typeof(EnumRockGroup)).Length];
		foreach (object value in Enum.GetValues(typeof(EnumRockGroup)))
		{
			RockStrataIndexed[(int)value] = new GeologicProvinceRockStrata();
			if (Rockstrata.ContainsKey(value?.ToString() ?? ""))
			{
				GeologicProvinceRockStrata geologicProvinceRockStrata = (RockStrataIndexed[(int)value] = Rockstrata[value?.ToString() ?? ""]);
				geologicProvinceRockStrata.ScaledMaxThickness = num * geologicProvinceRockStrata.MaxThickness;
			}
		}
	}
}
