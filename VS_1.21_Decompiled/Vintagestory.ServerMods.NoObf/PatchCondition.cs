using Vintagestory.API;

namespace Vintagestory.ServerMods.NoObf;

[DocumentAsJson]
public class PatchCondition
{
	[DocumentAsJson]
	public string When;

	[DocumentAsJson]
	public string IsValue;

	[DocumentAsJson]
	public bool useValue;
}
