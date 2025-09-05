using Vintagestory.API;

namespace Vintagestory.ServerMods.NoObf;

[DocumentAsJson]
public enum EnumJsonPatchOp
{
	Add,
	AddEach,
	Remove,
	Replace,
	Copy,
	Move,
	AddMerge
}
