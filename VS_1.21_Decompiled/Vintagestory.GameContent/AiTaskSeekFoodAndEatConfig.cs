using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskSeekFoodAndEatConfig : AiTaskBaseConfig
{
	[JsonProperty]
	public float MoveSpeed = 0.02f;

	[JsonProperty]
	public float ExtraTargetDistance = 0.6f;

	[JsonProperty]
	public int PoiSearchCooldown = 15000;

	[JsonProperty]
	public float ChanceToSeekFoodWithoutEating = 0.004f;

	[JsonProperty]
	public bool EatLooseItems = true;

	[JsonProperty]
	public bool EatFoodSources = true;

	[JsonProperty]
	public AssetLocation? EatSound;

	[JsonProperty]
	public float EatTimeSoundSec = 1.125f;

	[JsonProperty]
	public float EatTimeSec = 1.5f;

	[JsonProperty]
	public float EatSoundRange = 16f;

	[JsonProperty]
	public float EatSoundVolume = 1f;

	[JsonProperty]
	public float LooseItemsSearchRange = 10f;

	[JsonProperty]
	public float PoiSearchRange = 48f;

	[JsonProperty]
	public string PoiType = "food";

	[JsonProperty]
	public int SeekPoiRetryCooldown = 60000;

	[JsonProperty]
	public int SeekPoiMaxAttempts = 4;

	[JsonProperty]
	public EnumAICreatureType? AiCreatureType = EnumAICreatureType.Default;

	[JsonProperty]
	private string? eatAnimation;

	[JsonProperty]
	private float eatAnimationSpeed = 1f;

	[JsonProperty]
	private string? eatAnimationLooseItems;

	[JsonProperty]
	private float eatAnimationSpeedLooseItems = 1f;

	[JsonProperty]
	public bool DoConsumePortion = true;

	[JsonProperty]
	public CreatureDiet? Diet;

	[JsonProperty]
	public float SaturationPerPortion = 1f;

	public AnimationMetaData? EatAnimationMeta;

	public AnimationMetaData? EatAnimationMetaLooseItems;

	public override void Init(EntityAgent entity)
	{
		base.Init(entity);
		if (eatAnimation != null)
		{
			EatAnimationMeta = new AnimationMetaData
			{
				Code = eatAnimation.ToLowerInvariant(),
				Animation = eatAnimation.ToLowerInvariant(),
				AnimationSpeed = eatAnimationSpeed
			}.Init();
		}
		if (eatAnimationLooseItems != null)
		{
			EatAnimationMetaLooseItems = new AnimationMetaData
			{
				Code = eatAnimationLooseItems.ToLowerInvariant(),
				Animation = eatAnimationLooseItems.ToLowerInvariant(),
				AnimationSpeed = eatAnimationSpeedLooseItems
			}.Init();
		}
		Sound = Sound?.WithPathPrefix("sounds/");
		if (Diet == null)
		{
			Diet = entity.Properties.Attributes["creatureDiet"].AsObject<CreatureDiet>();
		}
		if (Diet == null)
		{
			entity.Api.Logger.Warning("Creature '" + entity.Code.ToShortString() + "' has SeekFoodAndEat task but no Diet specified.");
		}
		if (EatSound != null)
		{
			EatSound = EatSound.WithPathPrefixOnce("sounds/");
		}
	}
}
