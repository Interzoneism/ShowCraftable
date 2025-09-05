using Vintagestory.API;

namespace Vintagestory.ServerMods.NoObf;

[DocumentAsJson]
public class PatchModDependence
{
	[DocumentAsJson]
	public string modid;

	[DocumentAsJson]
	public bool invert;
}
