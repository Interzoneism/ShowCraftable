using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskBellAlarmConfig : AiTaskBaseTargetableConfig
{
	[JsonProperty]
	public int SpawnRange = 12;

	[JsonProperty]
	public int SpawnIntervalMinMs = 1000;

	[JsonProperty]
	public int SpawnIntervalMaxMs = 5000;

	[JsonProperty]
	public int SpawnMaxQuantity = 6;

	[JsonProperty]
	public float PlayerScalingFactor = 1f;

	[JsonProperty]
	public float PlayerSpawnScaleRange = 15f;

	[JsonProperty]
	private AssetLocation[]? entitiesToSpawn = Array.Empty<AssetLocation>();

	[JsonProperty]
	public AssetLocation? RepeatSound;

	[JsonProperty]
	public string ListenAiTaskId = "listen";

	[JsonProperty]
	public float NotListeningRangeReduction = 0.5f;

	[JsonProperty]
	public float SilentSoundRangeReduction = 0.25f;

	[JsonProperty]
	public float QuietSoundRangeReduction = 0.5f;

	[JsonProperty]
	public float MaxDistanceToTarget = 20f;

	[JsonProperty]
	public string Origin = "bellalarm";

	public EntityProperties[] EntitiesToSpawn = Array.Empty<EntityProperties>();

	public override void Init(EntityAgent entity)
	{
		base.Init(entity);
		if (entitiesToSpawn != null)
		{
			List<EntityProperties> list = new List<EntityProperties>();
			AssetLocation[] array = entitiesToSpawn;
			foreach (AssetLocation assetLocation in array)
			{
				EntityProperties entityType = entity.World.GetEntityType(assetLocation);
				if (entityType == null)
				{
					entity.World.Logger.Warning($"AiTaskBellAlarm specified '{assetLocation}' in 'EntitiesToSpawn', but no such entity type found, will ignore.");
				}
				else
				{
					list.Add(entityType);
				}
			}
			EntitiesToSpawn = list.ToArray();
			entitiesToSpawn = null;
		}
		if (RepeatSound != null)
		{
			RepeatSound = RepeatSound.WithPathPrefixOnce("sounds/");
		}
	}
}
