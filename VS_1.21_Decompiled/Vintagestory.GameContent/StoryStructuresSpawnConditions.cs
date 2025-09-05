using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class StoryStructuresSpawnConditions : ModSystem
{
	private ICoreServerAPI sapi;

	private ICoreAPI api;

	private Cuboidi[] structureLocations;

	private List<GeneratedStructure> storyStructuresClient = new List<GeneratedStructure>();

	private Vec3d tmpPos = new Vec3d();

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		api.ModLoader.GetModSystem<SystemTemporalStability>().OnGetTemporalStability += ResoArchivesSpawnConditions_OnGetTemporalStability;
		this.api = api;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		structureLocations = Array.Empty<Cuboidi>();
		api.Event.MapRegionLoaded += Event_MapRegionLoaded;
		api.Event.MapRegionUnloaded += Event_MapRegionUnloaded;
	}

	private void Event_MapRegionUnloaded(Vec2i mapCoord, IMapRegion region)
	{
		foreach (GeneratedStructure generatedStructure in region.GeneratedStructures)
		{
			if (generatedStructure.Group == "storystructure")
			{
				storyStructuresClient.Remove(generatedStructure);
			}
		}
		structureLocations = storyStructuresClient.Select((GeneratedStructure val) => val.Location).ToArray();
	}

	private void Event_MapRegionLoaded(Vec2i mapCoord, IMapRegion region)
	{
		foreach (GeneratedStructure generatedStructure in region.GeneratedStructures)
		{
			if (generatedStructure.Group == "storystructure")
			{
				storyStructuresClient.Add(generatedStructure);
			}
		}
		structureLocations = storyStructuresClient.Select((GeneratedStructure val) => val.Location).ToArray();
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		sapi.Event.OnTrySpawnEntity += Event_OnTrySpawnEntity;
	}

	private float ResoArchivesSpawnConditions_OnGetTemporalStability(float stability, double x, double y, double z)
	{
		if (isInStoryStructure(tmpPos.Set(x, y, z)))
		{
			return 1f;
		}
		return stability;
	}

	private bool Event_OnTrySpawnEntity(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d spawnPosition, long herdId)
	{
		if (properties.Server.SpawnConditions?.Runtime == null)
		{
			return true;
		}
		if (properties.Server.SpawnConditions.Runtime.Group == "hostile" && isInStoryStructure(spawnPosition))
		{
			return false;
		}
		return true;
	}

	private void loadLocations()
	{
		if (sapi == null)
		{
			return;
		}
		GenStoryStructures modSystem = sapi.ModLoader.GetModSystem<GenStoryStructures>();
		if (modSystem == null)
		{
			return;
		}
		List<Cuboidi> list = new List<Cuboidi>();
		foreach (StoryStructureLocation value in modSystem.storyStructureInstances.Values)
		{
			list.Add(value.Location);
		}
		structureLocations = list.ToArray();
	}

	private bool isInStoryStructure(Vec3d position)
	{
		if (structureLocations == null)
		{
			loadLocations();
		}
		if (structureLocations == null)
		{
			return false;
		}
		for (int i = 0; i < structureLocations.Length; i++)
		{
			if (structureLocations[i].Contains(position))
			{
				return true;
			}
		}
		return false;
	}

	public GeneratedStructure GetStoryStructureAt(BlockPos pos)
	{
		int regionSize = api.World.BlockAccessor.RegionSize;
		IMapRegion mapRegion = api.World.BlockAccessor.GetMapRegion(pos.X / regionSize, pos.Z / regionSize);
		if (mapRegion?.GeneratedStructures == null)
		{
			return null;
		}
		foreach (GeneratedStructure generatedStructure in mapRegion.GeneratedStructures)
		{
			if (generatedStructure?.Location != null && generatedStructure.Group == "storystructure" && generatedStructure.Location.Contains(pos))
			{
				return generatedStructure;
			}
		}
		return null;
	}
}
