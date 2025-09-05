using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemStructureLocator : ModSystem
{
	private ICoreServerAPI sapi;

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
	}

	public GeneratedStructure GetStructure(StructureLocation loc)
	{
		IMapRegion mapRegion = sapi.World.BlockAccessor.GetMapRegion(loc.RegionX, loc.RegionZ);
		GeneratedStructure result = null;
		if (loc.Position != null)
		{
			result = mapRegion?.GeneratedStructures.Find((GeneratedStructure s) => s.Location.X1 == loc.Position.X && s.Location.Y1 == loc.Position.Y && s.Location.Z1 == loc.Position.Z);
		}
		else if (loc.StructureIndex >= 0 && loc.StructureIndex < mapRegion?.GeneratedStructures.Count)
		{
			result = mapRegion.GeneratedStructures[loc.StructureIndex];
		}
		return result;
	}

	public StructureLocation FindFreshStructureLocation(string code, BlockPos nearPos, int searchRange)
	{
		return FindStructureLocation(delegate(GeneratedStructure struc, int index, IMapRegion region)
		{
			if (struc.Code.Split('/')[0] == code)
			{
				int[] moddata = region.GetModdata<int[]>("consumedStructureLocations");
				List<Vec3i> moddata2 = region.GetModdata<List<Vec3i>>("consumedStrucLocPos");
				bool result = moddata == null || !moddata.Contains(index);
				if (moddata2 != null && moddata2.Contains(struc.Location.Start))
				{
					result = false;
				}
				return result;
			}
			return false;
		}, nearPos, searchRange);
	}

	public StructureLocation FindStructureLocation(ActionBoolReturn<GeneratedStructure, int, IMapRegion> matcher, BlockPos pos, int searchRange)
	{
		int regionSize = sapi.WorldManager.RegionSize;
		int num = (pos.X - searchRange) / regionSize;
		int num2 = (pos.X + searchRange) / regionSize;
		int num3 = (pos.Z - searchRange) / regionSize;
		int num4 = (pos.Z + searchRange) / regionSize;
		for (int i = num; i <= num2; i++)
		{
			for (int j = num3; j <= num4; j++)
			{
				IMapRegion mapRegion = sapi.World.BlockAccessor.GetMapRegion(i, j);
				if (mapRegion == null)
				{
					continue;
				}
				for (int k = 0; k < mapRegion.GeneratedStructures.Count; k++)
				{
					GeneratedStructure generatedStructure = mapRegion.GeneratedStructures[k];
					if (generatedStructure.Location.ShortestDistanceFrom(pos.X, pos.Y, pos.Z) < (double)searchRange && matcher(generatedStructure, k, mapRegion))
					{
						return new StructureLocation
						{
							Position = generatedStructure.Location.Start,
							RegionX = i,
							RegionZ = j
						};
					}
				}
			}
		}
		return null;
	}

	public void ConsumeStructureLocation(StructureLocation strucLoc)
	{
		IMapRegion mapRegion = sapi.World.BlockAccessor.GetMapRegion(strucLoc.RegionX, strucLoc.RegionZ);
		List<Vec3i> list = mapRegion.GetModdata<List<Vec3i>>("consumedStrucLocPos") ?? new List<Vec3i>();
		list.Add(strucLoc.Position);
		mapRegion.SetModdata("consumedStrucLocPos", list);
	}
}
