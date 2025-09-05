using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public static class ApiTaskAdditions
{
	public static void RegisterAiTask<TTask>(this ICoreServerAPI serverAPI, string code) where TTask : IAiTask
	{
		AiTaskRegistry.Register<TTask>(code);
	}
}
