using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class AiRuntimeConfig : ModSystem
{
	public static bool RunAiTasks = true;

	public static bool RunAiActivities = true;

	private ICoreServerAPI? serverApi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		serverApi = api;
		api.Event.RegisterGameTickListener(onTick250ms, 250, 31);
	}

	private void onTick250ms(float obj)
	{
		RunAiTasks = serverApi?.World.Config.GetAsBool("runAiTasks", defaultValue: true) ?? true;
		RunAiActivities = serverApi?.World.Config.GetAsBool("runAiActivities", defaultValue: true) ?? true;
	}
}
