using System.Runtime.Serialization;

namespace Vintagestory.API.Common.Entities;

[DocumentAsJson]
public class SpawnConditions
{
	[DocumentAsJson]
	public ClimateSpawnCondition Climate;

	[DocumentAsJson]
	public RuntimeSpawnConditions Runtime;

	[DocumentAsJson]
	public WorldGenSpawnConditions Worldgen;

	public SpawnConditions Clone()
	{
		return new SpawnConditions
		{
			Runtime = Runtime?.Clone(),
			Worldgen = Worldgen?.Clone()
		};
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		if (Climate != null)
		{
			Runtime?.SetFrom(Climate);
			Worldgen?.SetFrom(Climate);
		}
	}
}
