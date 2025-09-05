using System;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

[ProtoContract]
public class DungeonPlaceTask
{
	[ProtoMember(1)]
	public string Code;

	[ProtoMember(2)]
	public List<TilePlaceTask> TilePlaceTasks;

	[ProtoMember(3)]
	public Cuboidi DungeonBoundaries;

	public DungeonPlaceTask GenBoundaries()
	{
		DungeonBoundaries = new Cuboidi(TilePlaceTasks[0].Pos, TilePlaceTasks[0].Pos);
		foreach (TilePlaceTask tilePlaceTask in TilePlaceTasks)
		{
			DungeonBoundaries.X1 = Math.Min(DungeonBoundaries.X1, tilePlaceTask.Pos.X);
			DungeonBoundaries.Y1 = Math.Min(DungeonBoundaries.Y1, tilePlaceTask.Pos.Y);
			DungeonBoundaries.Z1 = Math.Min(DungeonBoundaries.Z1, tilePlaceTask.Pos.Z);
			DungeonBoundaries.X2 = Math.Max(DungeonBoundaries.X2, tilePlaceTask.Pos.X + tilePlaceTask.SizeX);
			DungeonBoundaries.Y2 = Math.Max(DungeonBoundaries.Y2, tilePlaceTask.Pos.Y + tilePlaceTask.SizeY);
			DungeonBoundaries.Z2 = Math.Max(DungeonBoundaries.Z2, tilePlaceTask.Pos.Z + tilePlaceTask.SizeZ);
		}
		return this;
	}
}
