namespace Vintagestory.API.Client;

[DocumentAsJson]
public enum EnumFaceCullMode
{
	Default = 0,
	NeverCull = 1,
	Merge = 2,
	Callback = 7,
	Collapse = 3,
	MergeMaterial = 4,
	CollapseMaterial = 5,
	Liquid = 6,
	MergeSnowLayer = 8,
	FlushExceptTop = 9,
	Stairs = 10
}
