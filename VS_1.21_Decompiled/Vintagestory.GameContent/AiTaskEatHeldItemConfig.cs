using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskEatHeldItemConfig : AiTaskBaseConfig
{
	[JsonProperty]
	public float DurationSec = 1.5f;

	[JsonProperty]
	public float ChanceToUseFoodWithoutEating = 0.004f;

	[JsonProperty]
	public CreatureDiet? Diet;

	[JsonProperty]
	public bool ConsumePortion = true;

	[JsonProperty]
	public float SaturationPerPortion = 1f;

	[JsonProperty]
	public EnumHand HandToEatFrom;

	public override void Init(EntityAgent entity)
	{
		base.Init(entity);
		if (Diet == null)
		{
			Diet = entity.Properties.Attributes["creatureDiet"].AsObject<CreatureDiet>();
		}
		if (Diet == null)
		{
			entity.Api.Logger.Warning("Creature '" + entity.Code.ToShortString() + "' has AiTaskUseInventory task but no Diet specified.");
		}
	}
}
