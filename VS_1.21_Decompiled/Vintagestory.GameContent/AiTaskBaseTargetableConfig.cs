using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using VSEssentialsMod.Entity.AI.Task;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class AiTaskBaseTargetableConfig : AiTaskBaseConfig
{
	[JsonProperty]
	public EnumEntitySearchType SearchType;

	[JsonProperty]
	public float SeekingRange = 25f;

	[JsonProperty]
	public float MinTargetWeight;

	[JsonProperty]
	public float MaxTargetWeight;

	[JsonProperty]
	private List<List<string>>? entityTags = new List<List<string>>();

	[JsonProperty]
	private List<List<string>>? skipEntityTags = new List<List<string>>();

	[JsonProperty]
	public bool ReverseTagsCheck;

	[JsonProperty]
	public bool ReverseSkipTagsCheck;

	[JsonProperty]
	private string[]? entityCodes = Array.Empty<string>();

	[JsonProperty]
	public AssetLocation[] SkipEntityCodes = Array.Empty<AssetLocation>();

	[JsonProperty]
	public float TamingGenerations = 10f;

	[JsonProperty]
	public bool UseFearReductionFactor;

	[JsonProperty]
	public bool SeekingRangeAffectedByPlayerStat = true;

	[JsonProperty]
	public float SneakRangeReduction = 1f;

	[JsonProperty]
	public string? TriggerEmotionState;

	[JsonProperty]
	public EnumCreatureHostility CreatureHostility;

	[JsonProperty]
	public bool IgnoreDeadEntities = true;

	[JsonProperty]
	public bool FriendlyTarget;

	[JsonProperty]
	public bool RetaliateAttacks = true;

	[JsonProperty]
	public bool TargetEntitiesWithSameHerdId;

	[JsonProperty]
	public bool TargetEntitiesWithDifferentHerdId;

	[JsonProperty]
	public bool TargetOnlyInteractableEntities = true;

	[JsonProperty]
	public int[] TargetLightLevels = new int[4] { 0, 0, 32, 32 };

	[JsonProperty]
	public EnumLightLevelType TargetLightLevelType = EnumLightLevelType.MaxTimeOfDayLight;

	[JsonProperty]
	public int TargetSearchCooldownMs = 2000;

	[JsonProperty]
	public bool TargetPlayerInAllGameModes;

	public EntityTagRule[] EntityTags = Array.Empty<EntityTagRule>();

	public EntityTagRule[] SkipEntityTags = Array.Empty<EntityTagRule>();

	public string[] TargetEntityCodesBeginsWith = Array.Empty<string>();

	public string[] TargetEntityCodesExact = Array.Empty<string>();

	public string TargetEntityFirstLetters = "";

	public bool NoEntityCodes
	{
		get
		{
			if (TargetEntityCodesExact.Length == 0)
			{
				return TargetEntityCodesBeginsWith.Length == 0;
			}
			return false;
		}
	}

	public bool NoTags
	{
		get
		{
			if (EntityTags.Length == 0)
			{
				return SkipEntityTags.Length == 0;
			}
			return false;
		}
	}

	public bool TargetEverything
	{
		get
		{
			if (NoEntityCodes && NoTags)
			{
				return NoEntityWeight;
			}
			return false;
		}
	}

	public bool NoEntityWeight
	{
		get
		{
			if (MaxTargetWeight <= 0f)
			{
				return MinTargetWeight <= 0f;
			}
			return false;
		}
	}

	public bool IgnoreTargetLightLevel
	{
		get
		{
			if (TargetLightLevels[0] == 0 && TargetLightLevels[1] == 0 && TargetLightLevels[2] == 32)
			{
				return TargetLightLevels[3] == 32;
			}
			return false;
		}
	}

	public override void Init(EntityAgent entity)
	{
		base.Init(entity);
		if (entityTags != null)
		{
			EntityTags = entityTags.Select((List<string> tagList) => new EntityTagRule(entity.Api, tagList)).ToArray();
			entityTags = null;
		}
		if (skipEntityTags != null)
		{
			SkipEntityTags = skipEntityTags.Select((List<string> tagList) => new EntityTagRule(entity.Api, tagList)).ToArray();
			skipEntityTags = null;
		}
		if (entityCodes != null)
		{
			InitializeTargetCodes(entityCodes, out TargetEntityCodesExact, out TargetEntityCodesBeginsWith, out TargetEntityFirstLetters);
			entityCodes = null;
		}
		if (TargetLightLevels.Length != 4)
		{
			entity.Api.Logger.Error($"Invalid 'targetLightLevels' value (array length: {TargetLightLevels.Length}, should be 4) in AI task '{Code}' for entity '{entity.Code}'");
			throw new ArgumentException($"Invalid 'targetLightLevels' value (array length: {TargetLightLevels.Length}, should be 4) in AI task '{Code}' for entity '{entity.Code}'");
		}
		if (TargetLightLevels[0] > TargetLightLevels[1] || TargetLightLevels[1] > TargetLightLevels[2] || TargetLightLevels[2] > TargetLightLevels[3])
		{
			entity.Api.Logger.Error($"Invalid 'targetLightLevels' value: [{TargetLightLevels[0]},{TargetLightLevels[1]},{TargetLightLevels[2]},{TargetLightLevels[3]}], in AI task '{Code}' for entity '{entity.Code}'");
			throw new ArgumentException($"Invalid 'targetLightLevels' value: [{TargetLightLevels[0]},{TargetLightLevels[1]},{TargetLightLevels[2]},{TargetLightLevels[3]}], in AI task '{Code}' for entity '{entity.Code}'");
		}
	}

	protected static void InitializeTargetCodes(string[] codes, out string[] targetEntityCodesExact, out string[] targetEntityCodesBeginsWith, out string targetEntityFirstLetters)
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		targetEntityFirstLetters = "";
		foreach (string text in codes)
		{
			if (text.EndsWith('*'))
			{
				string text2 = text;
				list2.Add(text2.Substring(0, text2.Length - 1));
			}
			else
			{
				list.Add(text);
			}
		}
		targetEntityCodesBeginsWith = list2.ToArray();
		targetEntityCodesExact = new string[list.Count];
		int num = 0;
		foreach (string item in list)
		{
			if (item.Length != 0)
			{
				targetEntityCodesExact[num++] = item;
				char c = item[0];
				if (targetEntityFirstLetters.IndexOf(c) < 0)
				{
					targetEntityFirstLetters += c;
				}
			}
		}
		string[] array = targetEntityCodesBeginsWith;
		foreach (string text3 in array)
		{
			if (text3.Length == 0)
			{
				targetEntityFirstLetters = "";
				break;
			}
			char c2 = text3[0];
			if (targetEntityFirstLetters.IndexOf(c2) < 0)
			{
				targetEntityFirstLetters += c2;
			}
		}
	}
}
