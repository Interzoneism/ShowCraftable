using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class AnimationMetaData
{
	[JsonProperty]
	public string Code;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject Attributes;

	[JsonProperty]
	public string Animation;

	[JsonProperty]
	public float Weight = 1f;

	[JsonProperty]
	public Dictionary<string, float> ElementWeight = new Dictionary<string, float>();

	[JsonProperty]
	public float AnimationSpeed = 1f;

	[JsonProperty]
	public bool MulWithWalkSpeed;

	[JsonProperty]
	public float WeightCapFactor;

	[JsonProperty]
	public float EaseInSpeed = 10f;

	[JsonProperty]
	public float EaseOutSpeed = 10f;

	[JsonProperty]
	public AnimationTrigger TriggeredBy;

	[JsonProperty]
	public EnumAnimationBlendMode BlendMode;

	[JsonProperty]
	public Dictionary<string, EnumAnimationBlendMode> ElementBlendMode = new Dictionary<string, EnumAnimationBlendMode>(StringComparer.OrdinalIgnoreCase);

	[JsonProperty]
	public bool SupressDefaultAnimation;

	[JsonProperty]
	public float HoldEyePosAfterEasein = 99f;

	[JsonProperty]
	public bool ClientSide;

	[JsonProperty]
	public bool WithFpVariant;

	[JsonProperty]
	public AnimationSound AnimationSound;

	public AnimationMetaData FpVariant;

	public float StartFrameOnce;

	private int withActivitiesMerged;

	public uint CodeCrc32;

	public bool WasStartedFromTrigger;

	[JsonProperty]
	public bool AdjustCollisionBox { get; set; }

	public float GetCurrentAnimationSpeed(float walkspeed)
	{
		return AnimationSpeed * (MulWithWalkSpeed ? walkspeed : 1f) * GlobalConstants.OverallSpeedMultiplier;
	}

	public AnimationMetaData Init()
	{
		withActivitiesMerged = 0;
		EnumEntityActivity[] array = TriggeredBy?.OnControls;
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				withActivitiesMerged |= (int)array[i];
			}
		}
		CodeCrc32 = GetCrc32(Code);
		if (WithFpVariant)
		{
			FpVariant = Clone();
			FpVariant.WithFpVariant = false;
			FpVariant.Animation += "-fp";
			FpVariant.Code += "-fp";
			FpVariant.Init();
		}
		if (AnimationSound != null)
		{
			AnimationSound.Location.WithPathPrefixOnce("sounds/");
		}
		return this;
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Animation = Animation?.ToLowerInvariant() ?? "";
		if (Code == null)
		{
			Code = Animation;
		}
		CodeCrc32 = GetCrc32(Code);
	}

	public static uint GetCrc32(string animcode)
	{
		int num = int.MaxValue;
		return (uint)(GameMath.Crc32(animcode.ToLowerInvariant()) & num);
	}

	public bool Matches(int currentActivities)
	{
		AnimationTrigger triggeredBy = TriggeredBy;
		bool flag;
		if (triggeredBy != null && triggeredBy.MatchExact)
		{
			flag = currentActivities == withActivitiesMerged;
		}
		else
		{
			flag = (currentActivities & withActivitiesMerged) > 0;
			if (flag && (withActivitiesMerged & 0x10) != 0 && (currentActivities & 0x4000) != 0)
			{
				flag = false;
			}
		}
		return flag;
	}

	public AnimationMetaData Clone()
	{
		return new AnimationMetaData
		{
			Code = Code,
			Animation = Animation,
			AnimationSound = AnimationSound,
			Weight = Weight,
			Attributes = Attributes,
			ClientSide = ClientSide,
			ElementWeight = ElementWeight,
			AnimationSpeed = AnimationSpeed,
			MulWithWalkSpeed = MulWithWalkSpeed,
			EaseInSpeed = EaseInSpeed,
			EaseOutSpeed = EaseOutSpeed,
			TriggeredBy = TriggeredBy,
			AdjustCollisionBox = AdjustCollisionBox,
			BlendMode = BlendMode,
			ElementBlendMode = ElementBlendMode,
			withActivitiesMerged = withActivitiesMerged,
			CodeCrc32 = CodeCrc32,
			WasStartedFromTrigger = WasStartedFromTrigger,
			HoldEyePosAfterEasein = HoldEyePosAfterEasein,
			StartFrameOnce = StartFrameOnce,
			SupressDefaultAnimation = SupressDefaultAnimation,
			WeightCapFactor = WeightCapFactor
		};
	}

	public override bool Equals(object obj)
	{
		if (obj is AnimationMetaData animationMetaData && animationMetaData.Animation == Animation && animationMetaData.AnimationSpeed == AnimationSpeed)
		{
			return animationMetaData.BlendMode == BlendMode;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Animation.GetHashCode() ^ AnimationSpeed.GetHashCode() ^ BlendMode.GetHashCode();
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(Code);
		writer.Write(Animation);
		writer.Write(Weight);
		writer.Write(ElementWeight.Count);
		foreach (KeyValuePair<string, float> item in ElementWeight)
		{
			writer.Write(item.Key);
			writer.Write(item.Value);
		}
		writer.Write(AnimationSpeed);
		writer.Write(EaseInSpeed);
		writer.Write(EaseOutSpeed);
		writer.Write(TriggeredBy != null);
		if (TriggeredBy != null)
		{
			writer.Write(TriggeredBy.MatchExact);
			EnumEntityActivity[] onControls = TriggeredBy.OnControls;
			if (onControls != null)
			{
				writer.Write(onControls.Length);
				for (int i = 0; i < onControls.Length; i++)
				{
					writer.Write((int)onControls[i]);
				}
			}
			else
			{
				writer.Write(0);
			}
			writer.Write(TriggeredBy.DefaultAnim);
		}
		writer.Write((int)BlendMode);
		writer.Write(ElementBlendMode.Count);
		foreach (KeyValuePair<string, EnumAnimationBlendMode> item2 in ElementBlendMode)
		{
			writer.Write(item2.Key);
			writer.Write((int)item2.Value);
		}
		writer.Write(MulWithWalkSpeed);
		writer.Write(StartFrameOnce);
		writer.Write(HoldEyePosAfterEasein);
		writer.Write(ClientSide);
		writer.Write(Attributes?.ToString() ?? "");
		writer.Write(WeightCapFactor);
		writer.Write(AnimationSound != null);
		if (AnimationSound != null)
		{
			writer.Write(AnimationSound.Location.ToShortString());
			writer.Write(AnimationSound.Range);
			writer.Write(AnimationSound.Frame);
			writer.Write(AnimationSound.RandomizePitch);
		}
		writer.Write(AdjustCollisionBox);
	}

	public static AnimationMetaData FromBytes(BinaryReader reader, string version)
	{
		AnimationMetaData animationMetaData = new AnimationMetaData();
		animationMetaData.Code = reader.ReadString().DeDuplicate();
		animationMetaData.Animation = reader.ReadString();
		animationMetaData.Weight = reader.ReadSingle();
		int num = reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			animationMetaData.ElementWeight[reader.ReadString().DeDuplicate()] = reader.ReadSingle();
		}
		animationMetaData.AnimationSpeed = reader.ReadSingle();
		animationMetaData.EaseInSpeed = reader.ReadSingle();
		animationMetaData.EaseOutSpeed = reader.ReadSingle();
		if (reader.ReadBoolean())
		{
			animationMetaData.TriggeredBy = new AnimationTrigger();
			animationMetaData.TriggeredBy.MatchExact = reader.ReadBoolean();
			num = reader.ReadInt32();
			animationMetaData.TriggeredBy.OnControls = new EnumEntityActivity[num];
			for (int j = 0; j < num; j++)
			{
				animationMetaData.TriggeredBy.OnControls[j] = (EnumEntityActivity)reader.ReadInt32();
			}
			animationMetaData.TriggeredBy.DefaultAnim = reader.ReadBoolean();
		}
		animationMetaData.BlendMode = (EnumAnimationBlendMode)reader.ReadInt32();
		num = reader.ReadInt32();
		for (int k = 0; k < num; k++)
		{
			animationMetaData.ElementBlendMode[reader.ReadString().DeDuplicate()] = (EnumAnimationBlendMode)reader.ReadInt32();
		}
		animationMetaData.MulWithWalkSpeed = reader.ReadBoolean();
		if (GameVersion.IsAtLeastVersion(version, "1.12.5-dev.1"))
		{
			animationMetaData.StartFrameOnce = reader.ReadSingle();
		}
		if (GameVersion.IsAtLeastVersion(version, "1.13.0-dev.3"))
		{
			animationMetaData.HoldEyePosAfterEasein = reader.ReadSingle();
		}
		if (GameVersion.IsAtLeastVersion(version, "1.17.0-dev.18"))
		{
			animationMetaData.ClientSide = reader.ReadBoolean();
		}
		if (GameVersion.IsAtLeastVersion(version, "1.19.0-dev.20"))
		{
			string text = reader.ReadString();
			if (text != "")
			{
				animationMetaData.Attributes = new JsonObject(JToken.Parse(text));
			}
			else
			{
				animationMetaData.Attributes = new JsonObject(JToken.Parse("{}"));
			}
		}
		if (GameVersion.IsAtLeastVersion(version, "1.19.0-rc.6"))
		{
			animationMetaData.WeightCapFactor = reader.ReadSingle();
		}
		if (GameVersion.IsAtLeastVersion(version, "1.20.0-dev.13") && reader.ReadBoolean())
		{
			animationMetaData.AnimationSound = new AnimationSound
			{
				Location = AssetLocation.Create(reader.ReadString()),
				Range = reader.ReadSingle(),
				Frame = reader.ReadInt32(),
				RandomizePitch = reader.ReadBoolean()
			};
		}
		if (GameVersion.IsAtLeastVersion(version, "1.21.0-dev.1"))
		{
			animationMetaData.AdjustCollisionBox = reader.ReadBoolean();
		}
		animationMetaData.Init();
		return animationMetaData;
	}

	internal void DeDuplicate()
	{
		Code = Code.DeDuplicate();
		Dictionary<string, float> dictionary = new Dictionary<string, float>(ElementWeight.Count);
		foreach (KeyValuePair<string, float> item in ElementWeight)
		{
			dictionary[item.Key.DeDuplicate()] = item.Value;
		}
		ElementWeight = dictionary;
		Dictionary<string, EnumAnimationBlendMode> dictionary2 = new Dictionary<string, EnumAnimationBlendMode>(ElementBlendMode.Count);
		foreach (KeyValuePair<string, EnumAnimationBlendMode> item2 in ElementBlendMode)
		{
			dictionary2[item2.Key.DeDuplicate()] = item2.Value;
		}
		ElementBlendMode = dictionary2;
	}
}
