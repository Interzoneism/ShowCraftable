using System;

namespace Vintagestory.API.Client;

public enum EnumItemRenderTarget
{
	Gui,
	[Obsolete("Use HandTp instead")]
	HandFp,
	HandTp,
	HandTpOff,
	Ground
}
