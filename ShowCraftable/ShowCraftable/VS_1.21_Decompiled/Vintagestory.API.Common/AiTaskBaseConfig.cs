using System;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskBaseConfig
{
	[JsonProperty]
	public string Code = "";

	[JsonProperty]
	public bool Enabled = true;

	[JsonProperty]
	public string Id = "";

	[JsonProperty]
	public float Priority;

	[JsonProperty]
	private float? priorityForCancel;

	[JsonProperty]
	public int Slot;

	[JsonProperty]
	public int MinCooldownMs;

	[JsonProperty]
	public int MaxCooldownMs = 100;

	[JsonProperty]
	public double MinCooldownHours;

	[JsonProperty]
	public double MaxCooldownHours;

	[JsonProperty]
	public int InitialMinCooldownMs;

	[JsonProperty]
	public int InitialMaxCooldownMs;

	[JsonProperty]
	private string[]? tagsAppliedToEntity = Array.Empty<string>();

	[JsonProperty]
	public bool? WhenSwimming;

	[JsonProperty]
	public bool? WhenFeetInLiquid;

	[JsonProperty]
	public string[] WhenInEmotionState = Array.Empty<string>();

	[JsonProperty]
	public string[] WhenNotInEmotionState = Array.Empty<string>();

	[JsonProperty]
	private string animation = "";

	[JsonProperty]
	private float? animationSpeed;

	[JsonProperty]
	public AssetLocation? Sound;

	[JsonProperty]
	public float SoundRange = 16f;

	[JsonProperty]
	public int SoundStartMs;

	[JsonProperty]
	public int SoundRepeatMs;

	[JsonProperty]
	public bool RandomizePitch = true;

	[JsonProperty]
	public float SoundVolume = 1f;

	[JsonProperty]
	public float SoundChance = 1f;

	[JsonProperty]
	public AssetLocation? FinishSound;

	[JsonProperty]
	public int[] EntityLightLevels = new int[2] { 0, 32 };

	[JsonProperty]
	public EnumLightLevelType EntityLightLevelType = EnumLightLevelType.MaxTimeOfDayLight;

	[JsonProperty]
	private float[][]? duringDayTimeFramesHours = Array.Empty<float[]>();

	[JsonProperty]
	public float DayTimeFrameInaccuracy = 3f;

	[JsonProperty]
	public bool StopIfOutOfDayTimeFrames;

	[JsonProperty]
	public float[]? TemperatureRange;

	[JsonProperty]
	public float ExecutionChance = 0.1f;

	[JsonProperty]
	public bool StopOnHurt;

	[JsonProperty]
	public int MinDurationMs;

	[JsonProperty]
	public int MaxDurationMs;

	[JsonProperty]
	public int RecentlyAttackedTimeoutMs = 30000;

	[JsonProperty]
	public bool DontExecuteIfRecentlyAttacked;

	public EntityTagArray TagsAppliedToEntity = EntityTagArray.Empty;

	public AnimationMetaData? AnimationMeta;

	public DayTimeFrame[] DuringDayTimeFrames = Array.Empty<DayTimeFrame>();

	protected const int maxLightLevel = 32;

	private bool initialized;

	public float PriorityForCancel => priorityForCancel ?? Priority;

	public bool Initialized => initialized;

	public virtual void Init(EntityAgent entity)
	{
		if (tagsAppliedToEntity != null)
		{
			TagsAppliedToEntity = entity.Api.TagRegistry.EntityTagsToTagArray(tagsAppliedToEntity);
		}
		if (duringDayTimeFramesHours != null)
		{
			DuringDayTimeFrames = duringDayTimeFramesHours.Select((float[] frame) => new DayTimeFrame(frame[0], frame[1])).ToArray();
		}
		duringDayTimeFramesHours = null;
		tagsAppliedToEntity = null;
		if (animation != "")
		{
			string animationCode = animation.ToLowerInvariant();
			AnimationMetaData animationMetaData = Array.Find(entity.Properties.Client.Animations, (AnimationMetaData a) => a.Code == animationCode);
			if (animationMetaData != null)
			{
				if (animationSpeed.HasValue)
				{
					AnimationMeta = animationMetaData.Clone();
					AnimationMeta.AnimationSpeed = animationSpeed.Value;
				}
				else
				{
					AnimationMeta = animationMetaData;
				}
			}
			else
			{
				AnimationMeta = new AnimationMetaData
				{
					Code = animationCode,
					Animation = animationCode,
					AnimationSpeed = (animationSpeed ?? 1f)
				}.Init();
				AnimationMeta.EaseInSpeed = 1f;
				AnimationMeta.EaseOutSpeed = 1f;
			}
		}
		if (Sound != null)
		{
			Sound = Sound.WithPathPrefixOnce("sounds/");
		}
		if (FinishSound != null)
		{
			FinishSound = FinishSound.WithPathPrefixOnce("sounds/");
		}
		initialized = true;
	}

	public virtual void Init(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig)
	{
		Init(entity);
		initialized = true;
	}
}
