namespace Vintagestory.API.Client;

public enum EnumRenderStage
{
	Before,
	Opaque,
	OIT,
	AfterOIT,
	ShadowFar,
	ShadowFarDone,
	ShadowNear,
	ShadowNearDone,
	AfterPostProcessing,
	AfterBlit,
	Ortho,
	AfterFinalComposition,
	Done
}
