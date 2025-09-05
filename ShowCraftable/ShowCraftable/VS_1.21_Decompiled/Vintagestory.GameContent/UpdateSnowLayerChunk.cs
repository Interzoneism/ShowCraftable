using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class UpdateSnowLayerChunk : IEquatable<UpdateSnowLayerChunk>
{
	public Vec2i Coords;

	public double LastSnowAccumUpdateTotalHours;

	public Dictionary<int, BlockIdAndSnowLevel> SetBlocks = new Dictionary<int, BlockIdAndSnowLevel>();

	public bool Equals(UpdateSnowLayerChunk other)
	{
		return other.Coords.Equals(Coords);
	}

	public override bool Equals(object obj)
	{
		if (!(obj is UpdateSnowLayerChunk { Coords: var coords }))
		{
			return false;
		}
		if (Coords.X == coords.X)
		{
			return Coords.Y == coords.Y;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (17 * 23 + Coords.X.GetHashCode()) * 23 + Coords.Y.GetHashCode();
	}
}
