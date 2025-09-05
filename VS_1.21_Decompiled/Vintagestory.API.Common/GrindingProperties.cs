using System;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class GrindingProperties
{
	public bool usedObsoleteNotation;

	[DocumentAsJson]
	public JsonItemStack GroundStack;

	[DocumentAsJson]
	[Obsolete("Use GroundStack instead")]
	public JsonItemStack GrindedStack
	{
		get
		{
			return GroundStack;
		}
		set
		{
			GroundStack = value;
			usedObsoleteNotation = true;
		}
	}

	public GrindingProperties Clone()
	{
		return new GrindingProperties
		{
			GroundStack = GroundStack.Clone()
		};
	}
}
