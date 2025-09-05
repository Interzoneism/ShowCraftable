using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskTurretModeConfig : AiTaskShootAtEntityConfig
{
	[JsonProperty]
	public float AbortRange = 14f;

	[JsonProperty]
	public float FiringRangeMin = 14f;

	[JsonProperty]
	public float FiringRangeMax = 26f;

	[JsonProperty]
	public float FinishedAnimationProgress = 0.95f;

	[JsonProperty]
	public string LoadAnimation = "load";

	[JsonProperty]
	public string TurretAnimation = "turret";

	[JsonProperty]
	public string LoadFromTurretAnimation = "load-fromturretpose";

	[JsonProperty]
	public string HoldAnimation = "hold";

	[JsonProperty]
	public string FireAnimation = "fire";

	[JsonProperty]
	public string UnloadAnimation = "unload";

	[JsonProperty]
	public string ReloadAnimation = "reload";

	[JsonProperty]
	public AssetLocation? DrawSound;

	[JsonProperty]
	public AssetLocation? ReloadSound;

	public override void Init(EntityAgent entity)
	{
		base.Init(entity);
		if (DrawSound != null)
		{
			DrawSound = DrawSound.WithPathPrefixOnce("sounds/");
		}
		if (ReloadSound != null)
		{
			ReloadSound = ReloadSound.WithPathPrefixOnce("sounds/");
		}
	}
}
